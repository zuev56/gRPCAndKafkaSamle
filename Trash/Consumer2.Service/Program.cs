using Consumer.Shared.Models;
using Consumer2.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.Key));
builder.Services.AddHostedService<ConsumerB>();

var host = builder.Build();
host.Run();