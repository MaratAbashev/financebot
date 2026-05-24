namespace FinBot.BankService.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; }
    public string NewTransactionsTopic { get; set; } = "finbot.bank.new-transactions";
}