using System.Text.Json.Serialization;
using StocksProcessing.Streamer.Models.Enums;

namespace StocksProcessing.Streamer.Models.Messages;

public sealed record CurrencyRatesStatisticsMessage
{
    public required string StockExchangeName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Currency Currency { get; init; }

    public required decimal RateInRubles { get; init; }
}