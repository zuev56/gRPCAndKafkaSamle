using Grpc.Net.Client;

namespace GrpcClient;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5123");
        var client = new Greeter.GreeterClient(channel);

        while (!stoppingToken.IsCancellationRequested)
        {
            var name = $"User_{Random.Shared.Next(0, 100)}";
            var reply = await client.SayHelloAsync(new HelloRequest { Name = name }, cancellationToken: stoppingToken);

            _logger.LogInformation("Reply from Server: {message}", reply.Message);

            await Task.Delay(1000, stoppingToken);
        }
    }
}