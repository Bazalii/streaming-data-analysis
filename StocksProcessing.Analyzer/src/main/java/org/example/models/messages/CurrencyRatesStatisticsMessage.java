package org.example.models.messages;

import org.apache.flink.shaded.jackson2.com.fasterxml.jackson.annotation.JsonProperty;
import org.example.models.Currency;

public class CurrencyRatesStatisticsMessage {
    @JsonProperty("stock_exchange_name")
    public String stockExchangeName;

    @JsonProperty("currency")
    public Currency currency;

    @JsonProperty("rate_in_rubles")
    public Double rateInRubles;

    public CurrencyRatesStatisticsMessage(
            String stockExchangeName,
            Currency currency,
            Double rateInRubles
    ) {
        this.stockExchangeName = stockExchangeName;
        this.currency = currency;
        this.rateInRubles = rateInRubles;
    }
}
