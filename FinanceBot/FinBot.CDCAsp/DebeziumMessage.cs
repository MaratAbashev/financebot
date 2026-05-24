using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinBot.CDCAsp;

/// <summary>
/// Полное Debezium сообщение с unwrap трансформацией
/// </summary>
public class DebeziumMessage
{
    [JsonPropertyName("before")]
    public JsonElement? Before { get; set; }
    
    [JsonPropertyName("after")]
    public JsonElement? After { get; set; }
    
    [JsonPropertyName("source")]
    public SourceInfo? Source { get; set; }
    
    [JsonPropertyName("op")]
    public string? Op { get; set; }
    
    [JsonPropertyName("ts_ms")]
    public long TimestampMs { get; set; }
    
    [JsonPropertyName("transaction")]
    public TransactionInfo? Transaction { get; set; }
}

public class SourceInfo
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }
    
    [JsonPropertyName("connector")]
    public string? Connector { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("ts_ms")]
    public long TimestampMs { get; set; }
    
    [JsonPropertyName("snapshot")]
    public string? Snapshot { get; set; }
    
    [JsonPropertyName("db")]
    public string? Database { get; set; }
    
    [JsonPropertyName("sequence")]
    public string? Sequence { get; set; }
    
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }
    
    [JsonPropertyName("table")]
    public string? Table { get; set; }
    
    [JsonPropertyName("txId")]
    public long TransactionId { get; set; }
    
    [JsonPropertyName("lsn")]
    public long Lsn { get; set; }
    
    [JsonPropertyName("xmin")]
    public string? Xmin { get; set; }
    
    /// <summary>
    /// Это снепшот или реальное изменение
    /// </summary>
    public bool IsSnapshot => Snapshot == "true" || Snapshot == "last";
}

public class TransactionInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("total_order")]
    public long TotalOrder { get; set; }
    
    [JsonPropertyName("data_collection_order")]
    public long DataCollectionOrder { get; set; }
}

public enum OperationType
{
    Create,  // c
    Read,    // r (snapshot)
    Update,  // u
    Delete,  // d
    Unknown
}

/// <summary>
/// Результат обработки сообщения
/// </summary>
public class ProcessedMessageResult
{
    public bool Success { get; set; }
    public string? Schema { get; set; }
    public string? Table { get; set; }
    public OperationType Operation { get; set; }
    public string? ErrorMessage { get; set; }
    public long Offset { get; set; }
    public int Partition { get; set; }
    public long Lsn { get; set; }
}

/// <summary>
/// Информация о сиквенсе
/// </summary>
public class SequenceInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string SequenceName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}