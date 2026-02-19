namespace RocketCoordGenerator;

public sealed class KafkaSettings
{
    public const string Key = "Kafka";
    public string BootstrapServers { get; set; } = null!;
    public string Topic { get; set; } = null!;
}