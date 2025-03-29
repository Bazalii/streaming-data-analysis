using System.Text.Json.Serialization;
using StocksProcessing.Trader.Models.Enums;

namespace StocksProcessing.Trader.Models.Events;

public sealed record CurrencyRatesStatisticsEvent
{
    public required string StockExchangeName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Currency Currency { get; init; }

    public required decimal RateInRubles { get; init; }
}