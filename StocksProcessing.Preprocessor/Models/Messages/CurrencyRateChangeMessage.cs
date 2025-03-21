using System.Text.Json.Serialization;
using StocksProcessing.Generator.Models.Enums;

namespace StocksProcessing.Preprocessor.Models.Messages;

public sealed record CurrencyRateChangeMessage
{
    public required string StockExchangeName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Currency Currency { get; init; }

    public required decimal RateInRubles { get; init; }
}