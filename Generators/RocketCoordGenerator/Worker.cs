using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Coordinates;

namespace RocketCoordGenerator;

public sealed class Worker : BackgroundService
{
    private const int RocketsCount = 10;
    private const int EventsLimit = 5_000_000;
    private const int LogInterval = 10000;

    private readonly IProducer<int, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<Worker> _logger;

    public static bool IsRunning;

    public Worker(IProducer<int, string> producer, IOptions<KafkaSettings> kafkaSettings, ILogger<Worker> logger)
    {
        _producer = producer;
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();

        // TODO: Получить из БД или API
        var rockets = Enumerable.Range(1, RocketsCount)
            .Select(id => new Rocket{ Id = id })
            .ToArray();

        var sw = Stopwatch.StartNew();
        var eventsSent = 0;
        var lastCheckPointMs = 0d;
        var faults = 0;

        while (eventsSent < EventsLimit && !stoppingToken.IsCancellationRequested)
        {
            if (!IsRunning)
            {
                await Task.Delay(1000, stoppingToken);
                sw.Restart();
                continue;
            }
            var rocketIndex = random.Next(0, rockets.Length);

            var newX = rockets[rocketIndex].Coordinates.X + random.Next(0, 2);
            var newY = rockets[rocketIndex].Coordinates.Y + random.Next(0, 3);
            rockets[rocketIndex].Coordinates = new Point(newX, newY);

            var rocketMovedEvent = new UnitMovedEvent
            {
                Id = rockets[rocketIndex].Id,
                Coordinates = new Point(newX, newY),
                Date = DateTimeOffset.Now
            };

            var tries = 0;
            if (!TrySend(ref rocketMovedEvent, ref tries))
                faults++;

            if (++eventsSent % LogInterval == 0)
            {
                _logger.LogInformation("Sent {Count} events in {Elapsed}ms (+{shift}ms), {TotalMemoryMB}, {CollectionsCount}", eventsSent, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds-lastCheckPointMs, GcInfo.GetTotalMemoryFormatted(), GcInfo.GetCollectionCount());
                lastCheckPointMs = sw.ElapsedMilliseconds;
            }
        }

        _logger.LogInformation("FINISH. Sent {Events} events with {Faults} faults in {Elapsed}ms, {TotalMemoryMB}, {CollectionsCount}", eventsSent, faults, sw.ElapsedMilliseconds, GcInfo.GetTotalMemoryFormatted(), GcInfo.GetCollectionCount());
    }

    private bool TrySend(ref UnitMovedEvent rocketMovedEvent, ref int tries)
    {
        tries++;
        try
        {
            _producer.Produce(_kafkaSettings.Topic, new Message<int, string>
            {
                Key = rocketMovedEvent.Id,
                Value = JsonSerializer.Serialize(rocketMovedEvent, CoordinatesJsonSerializerContext.Default.UnitMovedEvent)
            });
            return true;
        }
        catch (Exception ex)
        {
            if (tries > 4)
            {
                _logger.LogInformation("Sending error: {Message}, tries: {tries}", ex.Message, tries);
                return false;
            }

            if (tries > 2)
            {
                _logger.LogInformation("Try send {tries} times: {Message}", tries, ex.Message);
                Thread.Sleep(10);
            }

            Thread.Sleep(1);
            return TrySend(ref rocketMovedEvent, ref tries);
        }
    }
}