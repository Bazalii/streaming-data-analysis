using System.Net.WebSockets;
using System.Text.Json;
using Confluent.Kafka;
using StocksProcessing.Trader.Models.Events;
using StocksProcessing.Trader.Traders;

namespace StocksProcessing.Trader.Consumers;

public class CurrencyStatisticsConsumer(
    IConfiguration configuration,
    CurrenciesTrader trader,
    ILogger<CurrencyStatisticsConsumer> logger)
    : BackgroundService
{
    private readonly string _websocketUrl = configuration["CurrenciesStatisticsWebsocketUrl"]!;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ProcessCurrencyStatisticsEventsAsync(stoppingToken);
    }

    private async Task ProcessCurrencyStatisticsEventsAsync(CancellationToken cancellation)
    {
        using var webSocket = new ClientWebSocket();

        try
        {
            await webSocket.ConnectAsync(new Uri(_websocketUrl), cancellation);
        }
        catch (Exception exception)
        {
            logger.LogCritical($"Unable to connect to websocket: {exception.Message}");

            throw;
        }

        while (cancellation.IsCancellationRequested is false)
        {
            var buffer = WebSocket.CreateServerBuffer(200);

            await webSocket.ReceiveAsync(buffer, cancellation);

            var utf8Reader = new Utf8JsonReader(buffer);
            var rateChangeEvent = JsonSerializer.Deserialize<CurrencyRatesStatisticsEvent>(ref utf8Reader)!;

            HandleCurrencyStatisticsEvent(rateChangeEvent);
        }
    }

    private void HandleCurrencyStatisticsEvent(CurrencyRatesStatisticsEvent statisticsEvent)
    {
        trader.ProcessStatisticsEvent(statisticsEvent);
    }
}