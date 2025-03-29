using System.Text.Json;
using Confluent.Kafka;
using StocksProcessing.Streamer.Models.Messages;
using StocksProcessing.Streamer.Streamers;

namespace StocksProcessing.Streamer.Consumers;

public class CurrenciesStatisticsConsumer : IDisposable
{
    private static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private const string TopicName = "currency_statistics";

    private readonly IConsumer<Null, string> _consumer;

    private readonly CurrenciesStatisticsProducer _producer;

    private readonly ILogger<CurrenciesStatisticsConsumer> _logger;

    public CurrenciesStatisticsConsumer(
        IConfiguration configuration,
        CurrenciesStatisticsProducer producer,
        ILogger<CurrenciesStatisticsConsumer> logger)
    {
        _producer = producer;
        _logger = logger;

        var kafkaConnectionString = configuration["Kafka:ConnectionString"];

        var producerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConnectionString,
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Latest,
        };

        _consumer = new ConsumerBuilder<Null, string>(producerConfig).Build();
    }

    public void Init(CancellationToken cancellation)
    {
        Task.Factory.StartNew(
            () => ConsumeMessagesAsync(cancellation),
            TaskCreationOptions.LongRunning);
    }

    private async Task ConsumeMessagesAsync(CancellationToken cancellation)
    {
        try
        {
            _consumer.Subscribe(TopicName);
        }
        catch (Exception exception)
        {
            _logger.LogCritical($"Subscription to topic {TopicName} failed: {exception.Message}");
            return;
        }

        try
        {
            while (cancellation.IsCancellationRequested is false)
            {
                var consumeResult = _consumer.Consume(cancellation);
                if (consumeResult is null)
                {
                    continue;
                }

                try
                {
                    var message = JsonSerializer.Deserialize<CurrencyRatesStatisticsMessage>(
                        consumeResult.Message.Value, SnakeCaseSerializerOptions);
                    if (message is null)
                    {
                        continue;
                    }

                    await _producer.SendCurrencyStatisticsEventAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error handling message: {ex.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical($"Fatal error: {exception.Message}");
        }
        finally
        {
            _consumer.Close();
        }
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}