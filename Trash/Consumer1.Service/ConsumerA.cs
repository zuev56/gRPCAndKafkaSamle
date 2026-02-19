using Consumer.Shared;
using Consumer.Shared.Models;
using Microsoft.Extensions.Options;

namespace Consumer1.Service;

internal sealed class ConsumerA : Worker
{
    public ConsumerA(IOptions<KafkaSettings> kafkaSettings, ILogger<ConsumerA> logger)
        : base(kafkaSettings, logger)
    {
    }
}