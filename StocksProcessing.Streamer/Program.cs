using Serilog;
using Serilog.Events;
using StocksProcessing.Streamer;
using StocksProcessing.Streamer.Consumers;
using StocksProcessing.Streamer.Streamers;

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
    .AddHostedService<ApplicationInitializer>()
    .AddSingleton<CurrenciesStatisticsConsumer>()
    .AddSingleton<CurrenciesStatisticsProducer>()
    .AddControllers();

var app = builder.Build();

app.MapHealthChecks("/ping");

app.UseWebSockets();
app.MapControllers();

await app.RunAsync("http://*:5002");