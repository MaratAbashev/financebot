using System.Text.Json;
using Confluent.Kafka;
using FinBot.Dal.DbContexts;
using Npgsql;
using Quartz;

namespace FinBot.CDCAsp;

[DisallowConcurrentExecution]
public class ScheduledCdcJob : IJob
{
    private readonly ILogger<ScheduledCdcJob> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly PostgresHelper _pgHelper;
    private readonly string _bootstrapServers;
    private readonly MetricsTracker _metrics;
    
    private const int BatchSize = 500;
    private const int MaxBatchTimeMs = 30000; 
    private const int ConsumerTimeoutMs = 2000;
    
    public ScheduledCdcJob(
        IConfiguration configuration,
        ILogger<ScheduledCdcJob> logger,
        ILogger<PostgresHelper> postgresHelperLogger)
    {
        _logger = logger;
        _metrics = new MetricsTracker();
        
        _bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new ArgumentNullException("Kafka:BootstrapServers");
        
        var connectionString = configuration.GetConnectionString(nameof(ReadDbContext)) 
                                ?? throw new ArgumentNullException("ReadOnlyDb connection string not found");
        
        _pgHelper = new PostgresHelper(connectionString, postgresHelperLogger);
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "scheduled-cdc-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 600000,
            SessionTimeoutMs = 60000,
            EnablePartitionEof = false,
            TopicMetadataRefreshIntervalMs = 10000
        };
            
        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => 
            {
                if (e.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Topic not available: {Reason}", e.Reason);
                }
                else
                {
                    _logger.LogError("Kafka error in scheduled job: {Reason}", e.Reason);
                }
            })
            .Build();
        _consumer.Subscribe("postgres.all-changes");
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        var jobId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["JobId"] = jobId,
            ["JobType"] = "ScheduledCdc"
        }))
        {
            _logger.LogInformation(
                "Scheduled CDC job started [JobId: {JobId}] at {Time}", 
                jobId, startTime);
            
            try
            {
                if (!WaitForTopics())
                {
                    return;
                }
                
                var messages = new List<ConsumeResult<string, string>>();
                var batchStartTime = DateTime.UtcNow;

                _logger.LogInformation("Collecting batch of messages (max {BatchSize})...", BatchSize);

                try
                {
                    while (messages.Count < BatchSize && 
                           (DateTime.UtcNow - batchStartTime).TotalMilliseconds < MaxBatchTimeMs)
                    {
                        var message = _consumer.Consume(TimeSpan.FromMilliseconds(ConsumerTimeoutMs));
                        
                        if (message?.Message?.Value != null)
                        {
                            messages.Add(message);
                        }
                        else
                        {
                            break;
                        }
                        
                        if (context.CancellationToken.IsCancellationRequested)
                            break;
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Topics not available yet. Skipping this run.");
                    context.JobDetail.JobDataMap["LastResult"] = "NoTopics";
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Batch collection cancelled");
                }
                
                _logger.LogInformation(
                    "Collected {Count} messages in {ElapsedMs}ms",
                    messages.Count, 
                    (DateTime.UtcNow - batchStartTime).TotalMilliseconds);
                
                if (messages.Any())
                {
                    var processedCount = await ProcessBatch(messages, context.CancellationToken);
                    
                    if (processedCount > 0)
                    {
                        try
                        {
                            var offsetsToCommit = messages
                                .Take(processedCount)
                                .GroupBy(m => new { m.Topic, m.Partition })
                                .Select(g => new TopicPartitionOffset(
                                    g.Key.Topic,
                                    g.Key.Partition.Value,
                                    g.Max(m => m.Offset.Value) + 1))
                                .ToList();
                            
                            _consumer.Commit(offsetsToCommit);
                            _logger.LogInformation(
                                "Committed offsets for {ProcessedCount} messages", processedCount);
                        }
                        catch (KafkaException ex)
                        {
                            _logger.LogError(ex, "Failed to commit offsets");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No messages to process in this batch");
                }
                
                _metrics.LogMetrics(_logger);
                
                context.JobDetail.JobDataMap["LastRunTime"] = DateTime.UtcNow.ToString("O");
                context.JobDetail.JobDataMap["LastMessageCount"] = messages.Count.ToString();
                context.JobDetail.JobDataMap["LastProcessedCount"] = _metrics.TotalProcessed.ToString();
                context.JobDetail.JobDataMap["LastResult"] = "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in scheduled CDC job [JobId: {JobId}]", jobId);
                context.JobDetail.JobDataMap["LastResult"] = "Error";
                context.JobDetail.JobDataMap["LastError"] = ex.Message;
                throw new JobExecutionException(ex, false);
            }
        }
    }
    
    private async Task<int> ProcessBatch(
        List<ConsumeResult<string, string>> messages,
        CancellationToken ct)
    {
        var processedCount = 0;
        
        var orderedMessages = messages.OrderBy(m => m.Offset.Value).ToList();
        
        await using var connection = await _pgHelper.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        
        var columnTypesCache = new Dictionary<string, Dictionary<string, string>>();
        
        try
        {
            foreach (var msg in orderedMessages)
            {
                if (ct.IsCancellationRequested) break;
                
                try
                {
                    var parsed = JsonSerializer.Deserialize<DebeziumMessage>(msg.Message.Value);
                    if (parsed?.Source?.Table == null)
                    {
                        _logger.LogWarning("Message without table info at offset {Offset}", msg.Offset);
                        continue;
                    }
                    
                    var schema = parsed.Source.Schema ?? "public";
                    var table = parsed.Source.Table;
                    var tableKey = $"{schema}.{table}";
                    
                    if (!columnTypesCache.TryGetValue(tableKey, out var columnTypes))
                    {
                        columnTypes = await _pgHelper.GetColumnTypes(connection, schema, table);
                        columnTypesCache[tableKey] = columnTypes;
                    }
                    
                    var op = ParseOperation(parsed.Op);
                    
                    switch (op)
                    {
                        case OperationType.Create:
                        case OperationType.Read:
                            await InsertRow(connection, transaction, schema, table, 
                                parsed.After, columnTypes);
                            break;
                            
                        case OperationType.Update:
                            await UpdateRow(connection, transaction, schema, table, 
                                parsed.Before, parsed.After, columnTypes);
                            break;
                            
                        case OperationType.Delete:
                            await DeleteRow(connection, transaction, schema, table, 
                                parsed.Before, columnTypes);
                            break;
                    }
                    
                    _metrics.RecordSuccess();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _metrics.RecordFailure();
                    _logger.LogError(ex, 
                        "Failed to process message at offset {Offset}, rolling back entire batch",
                        msg.Offset);
                    throw;
                }
            }
            
            foreach (var tableKey in columnTypesCache.Keys)
            {
                var parts = tableKey.Split('.');
                var schema = parts[0];
                var table = parts[1];
                await _pgHelper.UpdateSequences(connection, schema, table, transaction);
            }
            
            await transaction.CommitAsync(ct);
            _logger.LogInformation(
                "Batch committed successfully: {ProcessedCount} operations processed", 
                processedCount);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning("Batch rolled back due to error");
            throw;
        }
        
        return processedCount;
    }
    
    private async Task InsertRow(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        JsonElement? data,
        Dictionary<string, string> columnTypes)
    {
        var row = _pgHelper.ParseData(data);
        if (row == null || !row.Any())
        {
            _logger.LogWarning("Empty INSERT data for {Schema}.{Table}", schema, table);
            return;
        }

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
                param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            
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
        JsonElement? afterData,
        Dictionary<string, string> columnTypes)
    {
        var before = _pgHelper.ParseData(beforeData);
        var after = _pgHelper.ParseData(afterData);
        
        if (after == null || !after.Any())
        {
            _logger.LogWarning("Empty AFTER data for UPDATE on {Schema}.{Table}", schema, table);
            return;
        }
        
        var whereColumns = before ?? after;
        
        var setClause = string.Join(", ", 
            after.Keys.Select((c, i) => $"\"{c}\" = @s{i}"));
        var whereClause = string.Join(" AND ", 
            whereColumns.Keys.Select((c, i) => $"\"{c}\" = @w{i}"));
        
        var sql = $"UPDATE {schema}.\"{table}\" SET {setClause} WHERE {whereClause}";

        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        
        int idx = 0;
        foreach (var col in after)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@s{idx}", value);
            if (dataType == "jsonb") param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            cmd.Parameters.Add(param);
            idx++;
        }
        
        idx = 0;
        foreach (var col in whereColumns)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@w{idx}", value);
            if (dataType == "jsonb") param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            cmd.Parameters.Add(param);
            idx++;
        }
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            _logger.LogWarning(
                "UPDATE affected 0 rows on {Schema}.{Table}, converting to INSERT",
                schema, table);
            await InsertRow(connection, transaction, schema, table, afterData, columnTypes);
        }
    }

    private async Task DeleteRow(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        JsonElement? beforeData,
        Dictionary<string, string> columnTypes)
    {
        var before = _pgHelper.ParseData(beforeData);
        if (before == null || !before.Any())
        {
            _logger.LogWarning("Empty BEFORE data for DELETE on {Schema}.{Table}", schema, table);
            return;
        }
        
        var whereClause = string.Join(" AND ", 
            before.Keys.Select((c, i) => $"\"{c}\" = @p{i}"));
        
        var sql = $"DELETE FROM {schema}.\"{table}\" WHERE {whereClause}";

        await using var cmd = new NpgsqlCommand(sql, connection, transaction);
        
        int idx = 0;
        foreach (var col in before)
        {
            var dataType = columnTypes.GetValueOrDefault(col.Key, "text");
            var value = _pgHelper.ConvertValueForColumn(col.Value, dataType);
            var param = new NpgsqlParameter($"@p{idx}", value);
            if (dataType == "jsonb") param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            cmd.Parameters.Add(param);
            idx++;
        }
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        
        if (rowsAffected == 0)
        {
            _logger.LogWarning(
                "DELETE affected 0 rows on {Schema}.{Table}", schema, table);
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
    
    private bool WaitForTopics()
    {
        _logger.LogInformation("Waiting for Debezium topics...");
        
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
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get metadata");
        }

        return false;
    }
}