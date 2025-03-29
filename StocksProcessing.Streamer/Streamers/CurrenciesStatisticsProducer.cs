using System.Net.WebSockets;
using System.Text.Json;
using StocksProcessing.Streamer.Models.Events;
using StocksProcessing.Streamer.Models.Messages;

namespace StocksProcessing.Streamer.Streamers;

public class CurrenciesStatisticsProducer(
    ILogger<CurrenciesStatisticsProducer> logger)
    : IAsyncDisposable
{
    private readonly ReaderWriterLockSlim _socketsLock = new();
    private readonly List<WebSocket> _webSockets = [];

    public async Task SubscribeToCurrencyStatisticsEventsAsync(WebSocket webSocket)
    {
        try
        {
            _socketsLock.EnterWriteLock();
            _webSockets.Add(webSocket);
        }
        finally
        {
            _socketsLock.ExitWriteLock();
        }

        var buffer = WebSocket.CreateServerBuffer(100);

        while (true)
        {
            var message = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (message.MessageType is WebSocketMessageType.Close)
            {
                await RemoveSocketConnectionAsync(webSocket);

                return;
            }

            logger.LogInformation($"Received unexpected message from websocket: {message.MessageType}");
        }
    }

    private async Task RemoveSocketConnectionAsync(WebSocket webSocket)
    {
        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Connection closed by client!",
            CancellationToken.None);

        try
        {
            _socketsLock.EnterWriteLock();
            _webSockets.Remove(webSocket);
        }
        finally
        {
            _socketsLock.ExitWriteLock();
        }
    }

    public async Task SendCurrencyStatisticsEventAsync(
        CurrencyRatesStatisticsMessage message)
    {
        var statisticsEvent = CreateCurrencyStatisticsEvent(message);

        try
        {
            _socketsLock.EnterReadLock();

            foreach (var webSocket in _webSockets)
            {
                if (webSocket.State is not (WebSocketState.Open or WebSocketState.Connecting))
                {
                    await RemoveSocketConnectionAsync(webSocket);

                    continue;
                }

                var encodedMessage = JsonSerializer.SerializeToUtf8Bytes(statisticsEvent);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                    WebSocketMessageType.Binary,
                    endOfMessage: true,
                    CancellationToken.None);
            }
        }
        finally
        {
            _socketsLock.ExitReadLock();
        }
    }

    private CurrencyRatesStatisticsEvent CreateCurrencyStatisticsEvent(
        CurrencyRatesStatisticsMessage message)
    {
        return new CurrencyRatesStatisticsEvent
        {
            StockExchangeName = message.StockExchangeName,
            Currency = message.Currency,
            RateInRubles = message.RateInRubles,
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var webSocket in _webSockets)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed by client!",
                CancellationToken.None);
        }

        await CastAndDispose(_socketsLock);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}