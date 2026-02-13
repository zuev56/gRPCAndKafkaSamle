using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Producer.Shared.Models;
using Shared;

namespace Producer.Shared;

public sealed class OrderService
{
    private readonly IProducer<Guid, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IProducer<Guid, string> producer, IOptions<KafkaSettings> kafkaOptions, ILogger<OrderService> logger)
    {
        _producer = producer;
        _kafkaSettings = kafkaOptions.Value;
        _logger = logger;
    }

    public async Task PlaceOrderAsync(PlaceOrderRequest placeOrderRequest, CancellationToken cancellationToken)
    {
        if (placeOrderRequest == null!)
        {
            _logger.LogError("request is null");
            return;
        }

        var orderGuid = Guid.NewGuid();

        var orderPlacedEvent = new OrderPlacedEvent
        {
            OrderId = orderGuid.ToString(),
            UserId = placeOrderRequest.UserId,
            Total = placeOrderRequest.Total,
            Items = placeOrderRequest.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList(),
            Timestamp = DateTime.UtcNow,
            PaymentId = Guid.NewGuid().ToString()
        };

        await _producer.ProduceAsync(_kafkaSettings.Topic, new Message<Guid, string>
        {
            Key = orderGuid,
            Value = JsonSerializer.Serialize(orderPlacedEvent)
        }, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Order {OrderGuid} placed event sent to Kafka", orderGuid);
    }
}