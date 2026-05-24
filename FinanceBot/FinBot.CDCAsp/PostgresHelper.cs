using System.Globalization;
using System.Text.Json;
using Npgsql;

namespace FinBot.CDCAsp;

public class PostgresHelper(string connectionString, ILogger<PostgresHelper> logger = null)
{
    private Dictionary<string, string>? _columnTypesCache;
    private string? _cacheKey;
    
    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
    
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(ct);
        return connection;
    }
    
    public Dictionary<string, JsonElement>? ParseData(JsonElement? data)
    {
        if (data == null || data.Value.ValueKind == JsonValueKind.Null)
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                data.Value.GetRawText());
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to parse JSON data");
            return null;
        }
    }
    
    /// <summary>
    /// Обновить сиквенсы для конкретной таблицы
    /// </summary>
    public async Task UpdateSequences(
        NpgsqlConnection connection, 
        string schema, 
        string table,
        NpgsqlTransaction? transaction = null)
    {
        try
        {
            var sequences = await GetSequencesForTable(connection, schema, table);
            
            foreach (var seq in sequences)
            {
                var sql = $@"
                    SELECT setval(
                        @sequenceName,
                        COALESCE((
                            SELECT MAX(""{seq.ColumnName}"") 
                            FROM {schema}.""{table}""
                        ), 1),
                        true
                    )";

                await using var cmd = new NpgsqlCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@sequenceName", seq.SequenceName);
                
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to update sequences for {Schema}.{Table}", schema, table);
        }
    }
    
    /// <summary>
    /// Обновить все сиквенсы в схеме
    /// </summary>
    public async Task UpdateAllSequences(NpgsqlConnection connection, string schema = "public")
    {
        var sql = @"
            SELECT DISTINCT
                c.table_schema,
                c.table_name,
                c.column_name,
                pg_get_serial_sequence(
                    format('%I.%I', c.table_schema, c.table_name), 
                    c.column_name
                ) as sequence_name
            FROM information_schema.columns c
            JOIN pg_tables t ON c.table_name = t.tablename 
                AND c.table_schema = t.schemaname
            WHERE c.table_schema = @schema
              AND (c.is_identity = 'YES' 
                   OR c.column_default LIKE 'nextval%')
              AND t.tableowner != 'postgres'";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);

        await using var reader = await cmd.ExecuteReaderAsync();
        
        var sequences = new List<(string Schema, string Table, string Column, string Sequence)>();
        while (await reader.ReadAsync())
        {
            var seqName = reader.IsDBNull(3) ? null : reader.GetString(3);
            if (!string.IsNullOrEmpty(seqName))
            {
                sequences.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    seqName
                ));
            }
        }
        await reader.CloseAsync();
        
        foreach (var (sch, tbl, col, seq) in sequences)
        {
            try
            {
                var updateSql = $@"
                    SELECT setval(
                        @seqName,
                        COALESCE((SELECT MAX(""{col}"") FROM {sch}.""{tbl}""), 1),
                        true
                    )";

                await using var updateCmd = new NpgsqlCommand(updateSql, connection);
                updateCmd.Parameters.AddWithValue("@seqName", seq);
                await updateCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to update sequence {Sequence}", seq);
            }
        }
    }
    
    private async Task<List<SequenceInfo>> GetSequencesForTable(
        NpgsqlConnection connection, string schema, string table)
    {
        var sequences = new List<SequenceInfo>();
        
        var sql = @"
            SELECT 
                c.column_name,
                pg_get_serial_sequence(
                    format('%I.%I', c.table_schema, c.table_name), 
                    c.column_name
                ) as sequence_name,
                c.data_type
            FROM information_schema.columns c
            WHERE c.table_schema = @schema 
              AND c.table_name = @table
              AND (c.is_identity = 'YES' 
                   OR c.column_default LIKE 'nextval%')";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var seqName = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (!string.IsNullOrEmpty(seqName))
            {
                sequences.Add(new SequenceInfo
                {
                    ColumnName = reader.GetString(0),
                    SequenceName = seqName,
                    DataType = reader.GetString(2)
                });
            }
        }
        
        return sequences;
    }

    /// <summary>
    /// Получить типы колонок для таблицы
    /// </summary>
    public async Task<Dictionary<string, string>> GetColumnTypes(
        NpgsqlConnection connection, string schema, string table)
    {
        var cacheKey = $"{schema}.{table}";
        
        if (_cacheKey == cacheKey && _columnTypesCache != null)
            return _columnTypesCache;
        
        var types = new Dictionary<string, string>();
        
        var sql = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_schema = @schema AND table_name = @table";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            types[reader.GetString(0)] = reader.GetString(1);
        }
        
        _columnTypesCache = types;
        _cacheKey = cacheKey;
        
        return types;
    }

    /// <summary>
    /// Конвертировать значение с учетом типа колонки
    /// </summary>
    public object ConvertValueForColumn(JsonElement element, string dataType)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            return DBNull.Value;
        
        switch (dataType.ToLower())
        {
            case "uuid":
                if (element.ValueKind == JsonValueKind.String)
                {
                    var str = element.GetString();
                    if (Guid.TryParse(str, out var guid))
                        return guid;
                    if (str!.Length == 32 && Guid.TryParseExact(str, "N", out var guid2))
                        return guid2;
                }
                throw new InvalidCastException($"Cannot convert {element} to UUID");
                
            case "integer":
            case "int":
            case "int4":
                if (element.TryGetInt32(out int i))
                    return i;
                if (element.TryGetInt64(out long l))
                    return (int)l;
                return element.GetInt32();
                
            case "bigint":
            case "int8":
                if (element.TryGetInt64(out long bl))
                    return bl;
                return element.GetInt64();
                
            case "smallint":
            case "int2":
                if (element.TryGetInt16(out short s))
                    return s;
                return element.GetInt16();
                
            case "boolean":
            case "bool":
                if (element.ValueKind == JsonValueKind.True) return true;
                if (element.ValueKind == JsonValueKind.False) return false;
                if (element.ValueKind == JsonValueKind.String)
                {
                    var boolStr = element.GetString()?.ToLower();
                    return boolStr == "true" || boolStr == "1";
                }
                return element.GetBoolean();
                
            case "numeric":
            case "decimal":
                return ConvertDecimal(element);
                
            case "double precision":
            case "float8":
                return element.GetDouble();
                
            case "real":
            case "float4":
                return (float)element.GetDouble();
                
            case "json":
            case "jsonb":
                return element.GetRawText();
                
            case "timestamp without time zone":
            case "timestamp with time zone":
            case "timestamptz":
                if (element.TryGetDateTime(out DateTime dt))
                    return dt;
                return DateTime.Parse(element.GetString()!);
                
            case "date":
                if (element.TryGetDateTime(out DateTime d))
                    return d.Date;
                return DateTime.Parse(element.GetString()!).Date;
            
            default:
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString()!,
                    _ => element.GetRawText()
                };
        }
    }
    
    private decimal DecodeKafkaDecimal(string base64Value, int scale)
    {
        if (string.IsNullOrEmpty(base64Value))
            return 0m;
    
        var bytes = Convert.FromBase64String(base64Value);
    
        if (bytes.Length == 0)
            return 0m;

        var value = new System.Numerics.BigInteger(bytes, isUnsigned: false, isBigEndian: true);

        return (decimal)value / (decimal)Math.Pow(10, scale);
    }
    
    /// <summary>
    /// Конвертирует numeric/decimal значение из Debezium
    /// Поддерживает два формата:
    /// 1. Старый: "Ag0=" (Base64 строка, scale по умолчанию 2)
    /// 2. Новый: {"scale":2, "value":"BAE="} (объект с scale и value)
    /// 3. Обычное число: "100.50" или 100.50
    /// </summary>
    private object ConvertDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            int scale = 2; 
        
            if (element.TryGetProperty("scale", out var scaleElement))
            {
                if (scaleElement.ValueKind == JsonValueKind.String)
                    scale = int.Parse(scaleElement.GetString()!);
                else
                    scale = scaleElement.GetInt32();
            }
        
            if (element.TryGetProperty("value", out var valueElement))
            {
                var base64 = valueElement.GetString() ?? "AA==";
                return DecodeKafkaDecimal(base64, scale);
            }
        
            return 0m;
        }
        
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString() ?? "AA==";
            
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                return dec;
            
            try
            {
                return DecodeKafkaDecimal(str, scale: 2);
            }
            catch
            {
                return 0m;
            }
        }
        
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetDecimal(out var d))
                return d;
            return element.GetDecimal();
        }
    
        return 0m;
    }
}