using Consumer.Shared.Models;
using Consumer1.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.Key));
builder.Services.AddHostedService<ConsumerA>();

var host = builder.Build();
host.Run();