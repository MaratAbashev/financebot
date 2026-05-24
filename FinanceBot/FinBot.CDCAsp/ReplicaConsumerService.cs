using System.Text.Json;
using Confluent.Kafka;
using FinBot.Dal.DbContexts;
using Npgsql;

namespace FinBot.CDCAsp;

public class ReplicaConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<ReplicaConsumerService> _logger;
    private readonly PostgresHelper _pgHelper;
    private readonly MetricsTracker _metrics;
    private readonly string _bootstrapServers;
    
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;
    
    public ReplicaConsumerService(
        IConfiguration configuration,
        ILogger<ReplicaConsumerService> logger,
        ILogger<PostgresHelper> postgresHelperLogger)
    {
        _logger = logger;
        _metrics = new MetricsTracker();
        _bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new ArgumentNullException("Kafka:BootstrapServers");
        
        var connectionString = configuration.GetConnectionString(nameof(ReplicaDbContext)) 
            ?? throw new ArgumentNullException("ReplicaDb connection string not found");
        
        _pgHelper = new PostgresHelper(connectionString, 
            postgresHelperLogger); 
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "replica-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
            EnablePartitionEof = false,
            TopicMetadataRefreshIntervalMs = 10000
        };
        
        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => 
            {
                if (e.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Topic not available yet: {Reason}", e.Reason);
                }
                else
                {
                    _logger.LogError("Kafka error: {Reason}, Code: {Code}", e.Reason, e.Code);
                }
            })
            .Build();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Replica consumer service starting...");

        await WaitForTopicsAsync(stoppingToken);
        
        _consumer.Subscribe("postgres.all-changes");
        
        _logger.LogInformation("Subscribed to Debezium topics");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<string, string>? consumeResult;
                    
                    try
                    {
                        consumeResult = _consumer.Consume(stoppingToken);
                    }
                    catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        _logger.LogWarning("Topics not available. Waiting...");
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                    
                    if (consumeResult?.Message?.Value != null)
                    {
                        var result = await ProcessMessageWithRetry(consumeResult);
                        
                        if (result.Success)
                        {
                            _consumer.Commit(consumeResult);
                            _metrics.RecordSuccess();
                        }
                        else
                        {
                            _metrics.RecordFailure();
                            _logger.LogError(
                                "Failed: Topic={Topic}, Offset={Offset}, Error={Error}",
                                consumeResult.Topic, consumeResult.Offset, result.ErrorMessage);
                            _consumer.Commit(consumeResult);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in consumer loop");
                    await Task.Delay(5000, stoppingToken);
                }
                
                if (_metrics.TotalProcessed % 1000 == 0 && _metrics.TotalProcessed > 0)
                {
                    _metrics.LogMetrics(_logger);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Replica consumer service stopping...");
            _metrics.LogMetrics(_logger);
            
            try { _consumer.Close(); }
            catch (Exception ex) { _logger.LogError(ex, "Error closing consumer"); }
        }
    }
    
    private async Task WaitForTopicsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Waiting for Debezium topics...");
        
        var maxWaitTime = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var adminClient = new AdminClientBuilder(
                    new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
                
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                
                var debeziumTopics = metadata.Topics
                    .Where(t => t.Topic == "postgres.all-changes")
                    .ToList();
                
                if (debeziumTopics.Any())
                {
                    _logger.LogInformation(
                        "Found {Count} Debezium topics: {Topics}",
                        debeziumTopics.Count,
                        string.Join(", ", debeziumTopics.Select(t => t.Topic)));
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get metadata");
            }
            
            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                throw new TimeoutException($"Timeout waiting for Debezium topics after {maxWaitTime}");
            }
            
            await Task.Delay(5000, stoppingToken);
        }
    }
    
    private async Task<ProcessedMessageResult> ProcessMessageWithRetry(
        ConsumeResult<string, string> consumeResult)
    {
        var result = new ProcessedMessageResult
        {
            Offset = consumeResult.Offset.Value,
            Partition = consumeResult.Partition.Value
        };
        
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                result = await ProcessSingleMessage(consumeResult);
                if (result.Success) return result;
                
                if (attempt < MaxRetries)
                {
                    _logger.LogWarning("Retry {Attempt}/{MaxRetries} for offset {Offset}",
                        attempt, MaxRetries, consumeResult.Offset);
                    await Task.Delay(RetryDelayMs * attempt);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Attempt {Attempt}/{MaxRetries} failed", 
                    attempt, MaxRetries);
                
                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelayMs * attempt);
            }
        }
        
        return result;
    }
    
    private async Task<ProcessedMessageResult> ProcessSingleMessage(
        ConsumeResult<string, string> consumeResult)
    {
        var result = new ProcessedMessageResult
        {
            Success = false,
            Offset = consumeResult.Offset.Value,
            Partition = consumeResult.Partition.Value
        };
        
        try
        {
            var jsonMessage = consumeResult.Message.Value;
            _logger.LogInformation("JSON MESSAGE: "+jsonMessage);
            var message = JsonSerializer.Deserialize<DebeziumMessage>(jsonMessage);
            
            if (message?.Source == null)
            {
                result.ErrorMessage = "Empty message or source";
                return result;
            }
            
            var source = message.Source;
            var op = ParseOperation(message.Op);
            var schema = source.Schema ?? "public";
            var table = source.Table;
            
            result.Operation = op;
            result.Schema = schema;
            result.Table = table;
            result.Lsn = source.Lsn;
            
            if (string.IsNullOrEmpty(table))
            {
                result.ErrorMessage = "Table name is empty";
                return result;
            }
            
            _logger.LogDebug(
                "Processing {Op} on {Schema}.{Table}, LSN: {Lsn}, Snapshot: {IsSnapshot}",
                op, schema, table, source.Lsn, source.IsSnapshot);
            
            await using var connection = await _pgHelper.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                switch (op)
                {
                    case OperationType.Create:
                    case OperationType.Read:
                        await InsertRow(connection, transaction, schema, table, message.After);
                        break;
                        
                    case OperationType.Update:
                        await UpdateRow(connection, transaction, schema, table, 
                            message.Before, message.After);
                        break;
                        
                    case OperationType.Delete:
                        await DeleteRow(connection, transaction, schema, table, message.Before);
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown operation: {Op}", message.Op);
                        break;
                }
                
                await _pgHelper.UpdateSequences(connection, schema, table, transaction);
                
                await transaction.CommitAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception(
                    $"Failed to process {op} on {schema}.{table}: {ex.Message}", ex);
            }
        }
        catch (JsonException ex)
        {
            result.ErrorMessage = $"JSON error: {ex.Message}";
            _logger.LogError(ex, "JSON deserialization failed at offset {Offset}", 
                consumeResult.Offset);
        }
        catch (PostgresException ex)
        {
            result.ErrorMessage = $"PostgreSQL error: {ex.MessageText} (Code: {ex.SqlState})";
            _logger.LogError(ex, "PostgreSQL error at offset {Offset}", consumeResult.Offset);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error at offset {Offset}", consumeResult.Offset);
        }
        
        return result;
    }

    private async Task InsertRow(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        JsonElement? data)
    {
        var row = _pgHelper.ParseData(data);
        if (row == null || !row.Any())
        {
            _logger.LogWarning("Empty INSERT data for {Schema}.{Table}", schema, table);
            return;
        }
        
        var columnTypes = await _pgHelper.GetColumnTypes(connection, schema, table);

        var columns = row.Keys.ToList();
        var safeColumns = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var paramNames = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

        var sql = $"INSERT INTO {schema}.\"{table}\" ({safeColumns}) VALUES ({paramNames}) " +
                  $"ON CONFLICT DO NOTHING";

        await using var cmd = new NpgsqlCommand(sql, connection, transaction);

        for (int i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i];
            var element = row[columnName];
            var dataType = columnTypes.GetValueOrDefault(columnName, "text");
        
            var value = _pgHelper.ConvertValueForColumn(element, dataType);
            var param = new NpgsqlParameter($"@p{i}", value);
      
            if (dataType == "jsonb")
            {
                param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            }
            cmd.Parameters.Add(param);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateRow(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        JsonElement? beforeData,
        JsonElement? afterData)
    {
        var before = _pgHelper.ParseData(beforeData);
        var after = _pgHelper.ParseData(afterData);
        
        if (after == null || !after.Any()) 
        {
            _logger.LogWarning("Empty AFTER data for UPDATE on {Schema}.{Table}", schema, table);
            return;
        }
        
        var columnTypes = await _pgHelper.GetColumnTypes(connection, schema, table);
        
        var whereColumns = before ?? after;
        
        var setClause = string.Join(", ", 
            after.Keys.Select((c, i) => $"\"{c}\" = @s{i}"));
        
        var whereClause = string.Join(" AND ", 
            whereColumns.Keys.Select((c, i) => $"\"{c}\" = @w{i}"));
        
        var sql = $"UPDATE {schema}.\"{table}\" SET {setClause} WHERE {whereClause}";

        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        
        var idx = 0;
        foreach (var col in after)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@s{idx}", value);
      
            if (dataType == "jsonb")
            {
                param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            }
            cmd.Parameters.Add(param);
            idx++;
        }
        
        idx = 0;
        foreach (var col in whereColumns)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@w{idx}", value);
      
            if (dataType == "jsonb")
            {
                param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            }
            cmd.Parameters.Add(param);
            idx++;
        }
        
        try
        {
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                _logger.LogWarning(
                    "UPDATE affected 0 rows on {Schema}.{Table}, converting to INSERT. " +
                    "Before: {@Before}, After: {@After}", 
                    schema, table, before, after);
                
                await InsertRow(connection, transaction, schema, table, afterData);
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42804")
        {
            _logger.LogError(ex, 
                "Type mismatch in UPDATE for {Schema}.{Table}. " +
                "Before: {@Before}, After: {@After}", 
                schema, table, before, after);
            throw;
        }
    }

    private async Task DeleteRow(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        JsonElement? beforeData)
    {
        var before = _pgHelper.ParseData(beforeData);
        if (before == null || !before.Any())
        {
            _logger.LogWarning("Empty BEFORE data for DELETE on {Schema}.{Table}", schema, table);
            return;
        }
        
        var columnTypes = await _pgHelper.GetColumnTypes(connection, schema, table);
        
        var whereClause = string.Join(" AND ", 
            before.Keys.Select((c, i) => $"\"{c}\" = @p{i}"));
        
        var sql = $"DELETE FROM {schema}.\"{table}\" WHERE {whereClause}";

        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        
        var idx = 0;
        foreach (var col in before)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@p{idx}", value);
      
            if (dataType == "jsonb")
            {
                param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            }
            cmd.Parameters.Add(param);
            idx++;
        }
        
        try
        {
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            
            if (rowsAffected == 0)
            {
                _logger.LogWarning(
                    "DELETE affected 0 rows on {Schema}.{Table}. Data: {@Before}", 
                    schema, table, before);
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42804")
        {
            _logger.LogError(ex, 
                "Type mismatch in DELETE for {Schema}.{Table}. Data: {@Before}", 
                schema, table, before);
            throw;
        }
    }
    
    private OperationType ParseOperation(string? op)
    {
        return op switch
        {
            "c" => OperationType.Create,
            "r" => OperationType.Read,
            "u" => OperationType.Update,
            "d" => OperationType.Delete,
            _ => OperationType.Unknown
        };
    }
    
    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class MetricsTracker
{
    private long _successCount;
    private long _failureCount;
    private long _totalProcessed;
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    public long TotalProcessed => _totalProcessed;
    
    public void RecordSuccess()
    {
        Interlocked.Increment(ref _successCount);
        Interlocked.Increment(ref _totalProcessed);
    }
    
    public void RecordFailure()
    {
        Interlocked.Increment(ref _failureCount);
        Interlocked.Increment(ref _totalProcessed);
    }
    
    public void LogMetrics(ILogger logger)
    {
        var elapsed = DateTime.UtcNow - _startTime;
        var rate = elapsed.TotalSeconds > 0 ? _totalProcessed / elapsed.TotalSeconds : 0;
        
        logger.LogInformation(
            "CDC Metrics: Processed={Total}, Success={Success}, Failed={Failed}, Rate={Rate:F2} msg/sec, Uptime={Uptime}",
            _totalProcessed, _successCount, _failureCount, rate, elapsed);
    }
}