using System.Net.WebSockets;
using System.Text.Json;
using StocksProcessing.Generator.Models.Enums;
using StocksProcessing.Generator.Models.Events;
using Timer = System.Timers.Timer;

namespace StocksProcessing.Generator.Models.Generators;

public class CurrencyRateChangeEventsGenerator : IAsyncDisposable
{
    private readonly Random _random = new();
    private readonly string[] _stockNames = ["MoscowStockExchange", "SpbStockExchange"];

    private readonly ReaderWriterLockSlim _socketsLock = new();
    private readonly List<WebSocket> _webSockets = [];

    private readonly Timer _timer;

    private readonly ILogger<CurrencyRateChangeEventsGenerator> _logger;

    public CurrencyRateChangeEventsGenerator(
        ILogger<CurrencyRateChangeEventsGenerator> logger)
    {
        _logger = logger;

        var metricsCollectionInterval = TimeSpan.FromSeconds(1);
        _timer = new Timer(metricsCollectionInterval);

        _timer.Elapsed += async (_, _) => { await SendCurrencyRateChangeEventsAsync(); };

        _timer.AutoReset = true;
        _timer.Enabled = true;
    }


    public async Task SubscribeToCurrencyChangeEventsAsync(WebSocket webSocket)
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

            _logger.LogInformation($"Received unexpected message from websocket: {message.MessageType}");
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

    private async Task SendCurrencyRateChangeEventsAsync()
    {
        var dollarChangeEvents = CreateCurrencyRateChangeEvents(Currency.Dollar);
        var euroChangeEvents = CreateCurrencyRateChangeEvents(Currency.Euro);

        var events = dollarChangeEvents
            .Concat(euroChangeEvents)
            .ToArray();

        try
        {
            _socketsLock.EnterReadLock();

            foreach (var webSocket in _webSockets)
            {
                if (webSocket.State is WebSocketState.CloseReceived)
                {
                    continue;
                }

                foreach (var currencyChangeEvent in events)
                {
                    var encodedMessage = JsonSerializer.SerializeToUtf8Bytes(currencyChangeEvent);

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                        WebSocketMessageType.Binary,
                        endOfMessage: true,
                        CancellationToken.None);
                }
            }
        }
        finally
        {
            _socketsLock.ExitReadLock();
        }
    }

    private CurrencyRateChangeEvent[] CreateCurrencyRateChangeEvents(
        Currency currency)
    {
        return _stockNames
            .Select(stockExchangeName => new CurrencyRateChangeEvent
            {
                StockExchangeName = stockExchangeName,
                Currency = currency,
                RateInRubles = GetExchangeRate(currency)
            })
            .ToArray();
    }

    private decimal GetExchangeRate(Currency currency)
    {
        return currency switch
        {
            Currency.Dollar => _random.Next(minValue: 9000, maxValue: 9500) / 100m,
            Currency.Euro => _random.Next(minValue: 10000, maxValue: 10500) / 100m,
            _ => throw new ArgumentException("Unknown currency value!", nameof(currency))
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
        await CastAndDispose(_timer);

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