using System.Text.Json.Serialization;
using StocksProcessing.Generator.Models.Enums;

namespace StocksProcessing.Generator.Models.Events;

public sealed record CurrencyRateChangeEvent
{
    public required string StockExchangeName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Currency Currency { get; init; }

    public required decimal RateInRubles { get; init; }
}