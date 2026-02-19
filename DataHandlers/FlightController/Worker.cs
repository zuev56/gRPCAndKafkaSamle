using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FlightController.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Coordinates;

namespace FlightController;

public sealed class Worker : BackgroundService
{
    private const int LogInterval = 1000;

    private IConsumer<int, string>? _consumer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<Worker> _logger;

    private Point _theHighestPoint;
    private int _theHighestUnitId;
    private Point _theFurthestPoint;
    private int _theFurthestUnitId;

    public static bool IsRunning;

    public Worker(IOptions<KafkaSettings> kafkaSettings, ILogger<Worker> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
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

        _consumer = new ConsumerBuilder<int, string>(config).Build();
        _consumer.Subscribe(_kafkaSettings.Topic);

        _logger.LogInformation("Kafka consumer started and subscribed to topic: {Topic}", _kafkaSettings.Topic);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processedCount = 0;
        var sw = Stopwatch.StartNew();
        var lastCheckPointMs = 0d;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsRunning)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            try
            {
                var result = _consumer?.Consume(stoppingToken);
                if (result == null || string.IsNullOrEmpty(result.Message.Value))
                    continue;
                if (result.IsPartitionEOF)
                    _logger.LogInformation("The highest unit is {UnitId} [{X},{Y}]", _theHighestUnitId, _theHighestPoint.X, _theHighestPoint.Y);

                UnitMovedEvent unitMovedEvent;
                try
                {
                    unitMovedEvent = JsonSerializer.Deserialize(result.Message.Value, CoordinatesJsonSerializerContext.Default.UnitMovedEvent);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize message: {Message}", result.Message.Value);
                    continue;
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Point [{X},{Y}] for {UnitId} created at {Timestamp}", unitMovedEvent.Coordinates.X, unitMovedEvent.Coordinates.Y, unitMovedEvent.Id, unitMovedEvent.Date);

                HandleMoving(unitMovedEvent);

                if (++processedCount % LogInterval == 0)
                {
                    _logger.LogInformation("Processed {Count} messages in {ms}ms (+{shift}ms), {GetTotalMemory}, {CollectionsCount}. The highest: {UnitId} [{X},{Y}], The furthest: {Unit} [{X},{Y}]",
                        processedCount, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds-lastCheckPointMs, GcInfo.GetTotalMemoryFormatted(), GcInfo.GetCollectionCount(),
                        _theHighestUnitId, _theHighestPoint.X, _theHighestPoint.Y,
                        _theFurthestUnitId, _theFurthestPoint.X, _theFurthestPoint.Y);
                    lastCheckPointMs = sw.ElapsedMilliseconds;
                }
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

    private void HandleMoving(UnitMovedEvent unitMovedEvent)
    {
        var currentPoint = unitMovedEvent.Coordinates;

        // Только для локальных логов.
        if (_theFurthestPoint.X < currentPoint.X)
        {
            _theFurthestPoint = currentPoint;
            _theFurthestUnitId = unitMovedEvent.Id;
        }

        // Только для локальных логов.
        if (_theHighestPoint.Y < currentPoint.Y)
        {
            _theHighestPoint = currentPoint;
            _theHighestUnitId = unitMovedEvent.Id;
        }

        // Данные, которые подхватит UI.
        UnitInfoProvider.UnitIdToLastPoint[unitMovedEvent.Id] = currentPoint;
        // TODO: _theFurthestPoint, _theHighestPoint
        // Скорость обработки данных в ms
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