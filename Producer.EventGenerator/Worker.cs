using System.Diagnostics;
using Producer.Shared;
using Producer.Shared.Models;
using Shared;

namespace Producer.EventGenerator;

public sealed class Worker : BackgroundService
{
    private readonly OrderService _orderService;
    private readonly ILogger<Worker> _logger;

    public Worker(OrderService orderService, ILogger<Worker> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: на  заданном количестве (10 млн) элементов проверить:
        //       - потребление памяти в случае с Reference Type и Value Type
        //       - потребление памяти в случае, когда список OrderItemDto определён с Capacity и без
        //       - скорость выполнения на всех этих этапах
        //       - сделать замеры на стороне консьюмеров

        const int targetItemsCount = 20_000;
        _logger.LogInformation("START of producing of {TargetItemsCount} items", targetItemsCount);


        var sw = Stopwatch.StartNew();
        var sw2 = Stopwatch.StartNew();
        var producedItems = -1;

        while (++producedItems < targetItemsCount && !stoppingToken.IsCancellationRequested)
        {
            if (producedItems % 1000 == 0)
            {
                _logger.LogInformation("Produced {Count} items in {time}ms, {TotalMemoryMB}...", producedItems, sw2.ElapsedMilliseconds, GcInfo.GetTotalMemoryFormatted());
                sw2.Restart();
            }

            var items = GenerateOrderItems(Random.Shared.Next(1, 1000));

            await _orderService.PlaceOrderAsync(new PlaceOrderRequest
            {
                UserId = Random.Shared.Next(0, 100_000_000).ToString(),
                Total = items.Sum(i => i.Quantity),
                Items = items
            }, stoppingToken);
        }

        _logger.LogInformation("FINISH. Produced {Count} items in {SwElapsedMilliseconds}ms, {TotalMemoryMB}", targetItemsCount, sw.ElapsedMilliseconds, GcInfo.GetTotalMemoryFormatted());
    }

    private static List<OrderItemDto> GenerateOrderItems(int total)
    {
        var items = new List<OrderItemDto>(total);
        for (int i = 0; i < total; i++)
        {
            items.Add(new OrderItemDto
            {
                ProductId = Guid.NewGuid().ToString(),
                Quantity = Random.Shared.Next(1, 100)
            });
        }

        return items;
    }
}