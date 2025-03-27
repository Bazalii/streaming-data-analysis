using Serilog;
using Serilog.Events;
using StocksProcessing.Generator.Generators;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console();
});

builder.Services.AddSingleton<CurrencyRateChangeEventsGenerator>();
builder.Services.AddControllers();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/ping");

app.UseWebSockets();
app.MapControllers();

await app.RunAsync("http://*:5000");