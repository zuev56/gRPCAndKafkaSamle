namespace Producer.Shared.Models;

public sealed class KafkaSettings
{
    public const string Key = "Kafka";
    public string BootstrapServers { get; set; } = null!;
    public string Topic { get; set; } = null!;
}