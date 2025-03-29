package org.example.models.messages;

import org.apache.flink.shaded.jackson2.com.fasterxml.jackson.annotation.JsonProperty;
import org.example.models.Currency;

public class CurrencyRateChangeMessage {
    @JsonProperty("stock_exchange_name")
    public String stockExchangeName;

    @JsonProperty("currency")
    public Currency currency;

    @JsonProperty("rate_in_rubles")
    public Double rateInRubles;
}
