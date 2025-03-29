using StocksProcessing.Trader.Models.Enums;
using StocksProcessing.Trader.Models.Events;
using StocksProcessing.Trader.Models.Messages;
using StocksProcessing.Trader.Producers;

namespace StocksProcessing.Trader.Traders;

public class CurrenciesTrader(
    TradingEventsProducer producer)
{
    private readonly Dictionary<Currency, List<(string StockExchangeName, decimal RateInRubles)>>
        _currencyRates = new();

    public void ProcessStatisticsEvent(CurrencyRatesStatisticsEvent statisticsEvent)
    {
        var currentMessageCurrency = statisticsEvent.Currency;
        if (_currencyRates.TryGetValue(currentMessageCurrency, out var currencyStatistics) is false)
        {
            _currencyRates[currentMessageCurrency] =
            [
                (statisticsEvent.StockExchangeName, statisticsEvent.RateInRubles)
            ];

            return;
        }

        var currentMessageStockExchange = statisticsEvent.StockExchangeName;
        
        var currentExchangeStatistics = currencyStatistics.FirstOrDefault(
            pair => pair.StockExchangeName == currentMessageStockExchange);

        if (currentExchangeStatistics != default)
        {
            currencyStatistics.Remove(currentExchangeStatistics);
        }
        
        currencyStatistics.Add((currentMessageStockExchange, statisticsEvent.RateInRubles));

        if (currencyStatistics.Count < 2)
        {
            return;
        }

        var lowestRateExchange = currencyStatistics
            .MinBy(pair => pair.RateInRubles);

        var highestRateExchange = currencyStatistics
            .MaxBy(pair => pair.RateInRubles);

        var purchaseMessage = new CurrencyPurchaseMessage
        {
            BuyingStockExchangeName = lowestRateExchange.StockExchangeName,
            SellingStockExchangeName = highestRateExchange.StockExchangeName,
            Currency = currentMessageCurrency,
            Profit = highestRateExchange.RateInRubles - lowestRateExchange.RateInRubles,
        };

        producer.SendCurrencyRateChangeMessage(purchaseMessage);
    }
}