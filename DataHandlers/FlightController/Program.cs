using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlightController;
using FlightController.Services;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<KafkaSettings>(hostContext.Configuration.GetSection(KafkaSettings.Key));
        services.AddGrpc();
        services.AddHostedService<Worker>();
    })
    .ConfigureWebHostDefaults(webHostBuilder =>
    {
        webHostBuilder.Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UnitInfoProvider>();
                endpoints.MapGet("/",
                    () =>
                        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

                endpoints.MapPost("/start", () => { Worker.IsRunning = true; });
                endpoints.MapPost("/stop", () => { Worker.IsRunning = false; });
            });
        });
    });

var host = builder.Build();
host.Run();