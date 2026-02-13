using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Consumer.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Consumer.Shared;

public abstract class Worker : BackgroundService
{
    private IConsumer<Guid, string>? _consumer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<Worker> _logger;

    protected Worker(IOptions<KafkaSettings> kafkaSettings, ILogger<Worker> logger)
    {
        _logger = logger;
        _kafkaSettings = kafkaSettings.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest, // Start from the beginning of the topic
            EnableAutoCommit = true                     // Commit offsets automatically
        };

        _consumer = new ConsumerBuilder<Guid, string>(config)
            .SetKeyDeserializer(GuidSerializer.Deserializer)
            .Build();
        _consumer.Subscribe(_kafkaSettings.Topic);

        _logger.LogInformation("Kafka consumer started and subscribed to topic: {Topic}", _kafkaSettings.Topic);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensures method runs asynchronously

        const int processedItemsLogInterval = 1000;
        var processedCount = -1;
        var sw = Stopwatch.StartNew();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (++processedCount >= processedItemsLogInterval)
            {
                _logger.LogInformation("Processed {Count} messages in {ms}ms, {GetTotalMemory}...", processedCount, sw.ElapsedMilliseconds, GcInfo.GetTotalMemoryFormatted());
                processedCount = -1;
                sw.Restart();
            }

            try
            {
                var result = _consumer?.Consume(stoppingToken);
                if (result == null || string.IsNullOrEmpty(result.Message.Value))
                    continue;

                OrderPlacedEvent? orderPlacedEvent;
                try
                {
                    orderPlacedEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(result.Message.Value);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize message: {Message}", result.Message.Value);
                    continue;
                }

                if (orderPlacedEvent == null)
                {
                    _logger.LogWarning("Received null or malformed order event");
                    continue;
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Order received: {OrderId} at {Timestamp}", orderPlacedEvent.OrderId, orderPlacedEvent.Timestamp);

                await HandleOrderAsync(orderPlacedEvent, stoppingToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }
    }

    private Task HandleOrderAsync(OrderPlacedEvent orderPlacedEvent, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Order received: {OrderId} at {Timestamp}", orderPlacedEvent.OrderId, orderPlacedEvent.Timestamp);

        foreach (var item in orderPlacedEvent.Items)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(" - Product: {ProductId}, Quantity: {Quantity}", item.ProductId, item.Quantity);
        }

        //await Task.Delay(10, cancellationToken);
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Order inventory updated: {OrderId}", orderPlacedEvent.OrderId);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumer != null)
        {
            _logger.LogInformation("Closing Kafka consumer...");
            _consumer.Close();
            _consumer.Dispose();
            _consumer = null;
        }

        return base.StopAsync(cancellationToken);
    }
}