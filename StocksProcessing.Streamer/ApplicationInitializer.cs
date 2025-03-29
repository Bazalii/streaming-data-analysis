using StocksProcessing.Streamer.Consumers;

namespace StocksProcessing.Streamer;

public class ApplicationInitializer(
    CurrenciesStatisticsConsumer consumer)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellation)
    {
        consumer.Init(cancellation);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }
}