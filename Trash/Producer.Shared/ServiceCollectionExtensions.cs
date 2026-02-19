using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Producer.Shared.Models;
using Shared;

namespace Producer.Shared;

public static class ServiceCollectionExtensions
{
    public static void AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.Key));

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = settings.BootstrapServers,
                //Debug = "broker,protocol,metadata",
                Acks = Acks.All,            // Wait for all replicas to acknowledge
                EnableIdempotence = true,   // Ensure exactly-once semantics
                MessageSendMaxRetries = 3,  // Retry 3 times
                RetryBackoffMs = 100        // Wait 100ms between retries
            };

            return new ProducerBuilder<Guid, string>(producerConfig)
                .SetKeySerializer(GuidSerializer.Serializer)
                .Build();
        });
    }
}