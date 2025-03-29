using Serilog;
using Serilog.Events;
using StocksProcessing.Trader;
using StocksProcessing.Trader.Consumers;
using StocksProcessing.Trader.Producers;
using StocksProcessing.Trader.Traders;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console();
});

builder.Services.AddHealthChecks();
builder.Services
    .AddHostedService<CurrencyStatisticsConsumer>()
    .AddSingleton<TradingEventsProducer>()
    .AddSingleton<CurrenciesTrader>();

var app = builder.Build();

app.MapHealthChecks("/ping");

await app.RunAsync("http://*:5003");