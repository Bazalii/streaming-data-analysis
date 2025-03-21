using System.Text.Json;
using Confluent.Kafka;
using StocksProcessing.Preprocessor.Models.Events;
using StocksProcessing.Preprocessor.Models.Messages;
using Websocket.Client;

namespace StocksProcessing.Preprocessor.Preprocessors;

public class StocksPreprocessor : IDisposable
{
    private static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private const string Topic = "currency_rates_changes";

    private readonly string _websocketUrl;
    private readonly IProducer<Null, string> _producer;

    public StocksPreprocessor(
        string kafkaConnectionString,
        string websocketUrl)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        _websocketUrl = websocketUrl;
    }

    public async Task ProcessCurrencyRatesChangeEventsAsync()
    {
        var websocketUri = new Uri(_websocketUrl);
        var exitEvent = new ManualResetEvent(false);

        using var client = new WebsocketClient(websocketUri);
        client.MessageReceived.Subscribe(SendCurrencyRateChangeMessage);

        await client.Start();

        exitEvent.WaitOne();
    }

    private void SendCurrencyRateChangeMessage(ResponseMessage responseMessage)
    {
        try
        {
            var utf8Reader = new Utf8JsonReader(responseMessage.Binary);
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
            Console.WriteLine($"Delivery failed: {exception.Error.Reason}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Unexpected exception: {exception.Message}");
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

    public void Dispose()
    {
        _producer.Dispose();
    }
}