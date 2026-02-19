using Consumer.Shared;
using Consumer.Shared.Models;
using Microsoft.Extensions.Options;

namespace Consumer2.Service;

internal sealed class ConsumerB : Worker
{
    public ConsumerB(IOptions<KafkaSettings> kafkaSettings, ILogger<ConsumerB> logger)
        : base(kafkaSettings, logger)
    {
    }
}