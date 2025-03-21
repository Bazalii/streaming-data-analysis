using StocksProcessing.Generator.Models.Generators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CurrencyRateChangeEventsGenerator>();
builder.Services.AddControllers();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseWebSockets();

app.MapHealthChecks("/ping");
app.MapControllers();

await app.RunAsync("http://*:5000");