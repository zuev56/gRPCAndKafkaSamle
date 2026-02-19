using Producer.EventGenerator;
using Producer.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddSingleton<OrderService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();