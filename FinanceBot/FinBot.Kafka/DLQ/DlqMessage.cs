
namespace FinBot.Kafka.DLQ;

/// <summary>
/// Сообщение для DLQ
/// </summary>
/// <param name="ExceptionMessage">сообщение об ошибке</param>
/// <param name="Offset">оффсет на котором произошел сбой</param>
/// <param name="TopicName">название топика на котором произошел сбой</param>
/// <param name="Partition">партиция на которой произошел сбой</param>
/// <param name="Key">сериализованный ключ</param>
/// <param name="Value">сериализованное значение</param>
/// <remarks>
/// не трогать, уже внедрено по умолчанию
/// </remarks>
public record DlqMessage(
    string ExceptionMessage, 
    long? Offset,
    string? TopicName,
    int? Partition,
    string Key,
    string Value);