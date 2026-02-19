using Producer.Shared;
using Producer.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKafka(builder.Configuration);
builder.Services.AddSingleton<OrderService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapPostEndpoint("/placeorder");

app.Run();