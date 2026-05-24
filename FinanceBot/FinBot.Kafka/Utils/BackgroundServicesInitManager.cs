namespace FinBot.Kafka.Utils;

public class BackgroundServicesInitManager
{
    public TopicCreationStatus IsTopicsCreated { get; set; } = TopicCreationStatus.Pending;
    public Exception? Error { get; set; }
}

public enum TopicCreationStatus
{
    Created,
    Pending,
    Error
}