using System.Net.WebSockets;
using System.Text.Json;
using Confluent.Kafka;
using StocksProcessing.Preprocessor.Models.Events;
using StocksProcessing.Preprocessor.Models.Messages;

namespace StocksProcessing.Preprocessor.Preprocessors;

public class StocksPreprocessor : BackgroundService
{
    private static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private const string Topic = "currency_rates_changes";

    private readonly string _websocketUrl;
    private readonly IProducer<Null, string> _producer;

    private readonly ILogger<StocksPreprocessor> _logger;

    public StocksPreprocessor(
        IConfiguration configuration,
        ILogger<StocksPreprocessor> logger)
    {
        var kafkaConnectionString = configuration["Kafka:ConnectionString"]!;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        _websocketUrl = configuration["CurrencyRateChangesWebsocket"]!;

        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ProcessCurrencyRatesChangeEventsAsync(stoppingToken);
    }

    private async Task ProcessCurrencyRatesChangeEventsAsync(CancellationToken cancellation)
    {
        using var webSocket = new ClientWebSocket();

        try
        {
            await webSocket.ConnectAsync(new Uri(_websocketUrl), cancellation);
        }
        catch (Exception exception)
        {
            _logger.LogCritical($"Unable to connect to websocket: {exception.Message}");

            throw;
        }

        while (cancellation.IsCancellationRequested is false)
        {
            try
            {
                var buffer = WebSocket.CreateServerBuffer(200);

                await webSocket.ReceiveAsync(buffer, cancellation);

                var utf8Reader = new Utf8JsonReader(buffer);
                var rateChangeEvent = JsonSerializer.Deserialize<CurrencyRateChangeEvent>(ref utf8Reader)!;

                var message = CreateMessage(rateChangeEvent);
                var serializedMessage = JsonSerializer.Serialize(message, SnakeCaseSerializerOptions);

                _producer.Produce(
                    Topic,
                    new Message<Null, string>
                    {
                        Value = serializedMessage
                    });
            }
            catch (ProduceException<Null, string> exception)
            {
                _logger.LogError($"Delivery failed: {exception.Error.Reason}");
            }
            catch (Exception exception)
            {
                _logger.LogError($"Unexpected exception: {exception.Message}");
            }
        }
    }

    private static CurrencyRateChangeMessage CreateMessage(CurrencyRateChangeEvent currencyRateChangeEvent)
    {
        return new CurrencyRateChangeMessage
        {
            StockExchangeName = currencyRateChangeEvent.StockExchangeName,
            Currency = currencyRateChangeEvent.Currency,
            RateInRubles = currencyRateChangeEvent.RateInRubles,
        };
    }

    public override void Dispose()
    {
        _producer.Dispose();
        base.Dispose();
    }
}