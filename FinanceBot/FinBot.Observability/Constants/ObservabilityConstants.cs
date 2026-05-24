namespace FinBot.Observability.Constants;

public static class ObservabilityConstants
{
    public const string ActivitySourceName = "FinBot";
    public const string MeterName = "FinBot";

    public const string ConfigurationSectionName = "Observability";

    public static class LogProperties
    {
        public const string TraceId = "trace_id";
        public const string Facility = "facility";
        public const string MachineName = "machine_name";
        public const string Endpoint = "endpoint";
        public const string UniqueIdFacility = "unique_id_facility";
        public const string ExceptionMessage = "exception_message";
    }

    public static class HealthEndpoints
    {
        public const string Health = "/health";
        public const string Live = "/health/live";
        public const string Ready = "/health/ready";
    }

    public static class MetricsEndpoint
    {
        public const string Path = "/metrics";
    }

    public static class Tags
    {
        public const string DialogType = "dialog.type";
        public const string DialogStep = "dialog.step.name";

        public const string TelegramUpdateId = "tg.update.id";
        public const string TelegramUserId = "tg.user.id";
        public const string TelegramChatId = "tg.chat.id";

        public const string MessagingSystem = "messaging.system";
        public const string MessagingDestination = "messaging.destination";
        public const string MessagingKafkaPartition = "messaging.kafka.partition";
        public const string MessagingKafkaOffset = "messaging.kafka.offset";
        public const string MessagingKafkaConsumerGroup = "messaging.kafka.consumer.group";
    }
}