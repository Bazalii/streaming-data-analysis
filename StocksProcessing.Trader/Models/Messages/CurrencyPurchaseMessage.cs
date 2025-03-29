using System.Text.Json.Serialization;
using StocksProcessing.Trader.Models.Enums;

namespace StocksProcessing.Trader.Models.Messages;

public sealed record CurrencyPurchaseMessage
{
    public required string BuyingStockExchangeName { get; init; }
    public required string SellingStockExchangeName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Currency Currency { get; init; }

    public required decimal Profit { get; init; }
}