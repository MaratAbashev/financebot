using System.Diagnostics.Metrics;

namespace FinBot.Observability.Metrics;

public sealed class BusinessMetrics
{
    public Histogram<double> ExpenseAddDuration { get; }
    public Histogram<double> DialogCompletionDuration { get; }
    public Histogram<double> ReportGenerationDuration { get; }
    public Histogram<double> LlmCompletionDuration { get; }
    public Counter<long> TelegramUpdatesTotal { get; }
    public Counter<long> KafkaMessagesProduced { get; }
    public Counter<long> KafkaMessagesConsumed { get; }
    public Counter<long> ExcelOutcomeTotal { get; }
    public Counter<long> LlmOutcomeTotal { get; }

    public BusinessMetrics()
    {
        var meter = FinBotMeter.Instance;

        ExpenseAddDuration = meter.CreateHistogram<double>(
            name: "finbot.expense.add.duration",
            unit: "ms",
            description: "Длительность добавления расхода через бизнес-сервис.");

        DialogCompletionDuration = meter.CreateHistogram<double>(
            name: "finbot.dialog.completion.duration",
            unit: "ms",
            description: "Длительность завершения диалога. Тег dialog_type.");

        ReportGenerationDuration = meter.CreateHistogram<double>(
            name: "finbot.report.generation.duration",
            unit: "ms",
            description: "Длительность генерации отчёта (Excel/график).");

        LlmCompletionDuration = meter.CreateHistogram<double>(
            name: "finbot.llm.completion.duration",
            unit: "ms",
            description: "Длительность ответа LLM.");

        TelegramUpdatesTotal = meter.CreateCounter<long>(
            name: "finbot.telegram.updates.total",
            unit: "{update}",
            description: "Количество обработанных апдейтов Telegram. Тег update_type.");

        KafkaMessagesProduced = meter.CreateCounter<long>(
            name: "finbot.kafka.produced.total",
            unit: "{message}",
            description: "Количество отправленных Kafka-сообщений. Тег topic.");

        KafkaMessagesConsumed = meter.CreateCounter<long>(
            name: "finbot.kafka.consumed.total",
            unit: "{message}",
            description: "Количество обработанных Kafka-сообщений. Теги topic, success.");

        ExcelOutcomeTotal = meter.CreateCounter<long>(
            name: "finbot.excel.outcome.total",
            unit: "{report}",
            description: "Исходы генерации Excel-отчётов. Тег outcome (generated/not_found/error).");

        LlmOutcomeTotal = meter.CreateCounter<long>(
            name: "finbot.llm.outcome.total",
            unit: "{analysis}",
            description: "Исходы LLM-анализа. Тег outcome (generated/not_found/error).");
    }
}