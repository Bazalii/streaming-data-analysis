using System.Text.Json;
using Confluent.Kafka;
using StocksProcessing.Trader.Models.Messages;

namespace StocksProcessing.Trader.Producers;

public class TradingEventsProducer
{
    private static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private const string Topic = "currency_purchases";

    private readonly IProducer<Null, string> _producer;

    private readonly ILogger<TradingEventsProducer> _logger;

    public TradingEventsProducer(
        IConfiguration configuration,
        ILogger<TradingEventsProducer> logger)
    {
        var kafkaConnectionString = configuration["Kafka:ConnectionString"];

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        
        _logger = logger;
    }

    public void SendCurrencyRateChangeMessage(CurrencyPurchaseMessage message)
    {
        try
        {
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