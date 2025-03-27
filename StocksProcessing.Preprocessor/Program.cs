using Serilog;
using Serilog.Events;
using StocksProcessing.Preprocessor.Preprocessors;

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
    .AddHostedService<StocksPreprocessor>();

var app = builder.Build();

app.MapHealthChecks("/ping");

await app.RunAsync("http://*:5001");