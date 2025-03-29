package org.example.models.keys;

import org.example.models.Currency;

import java.util.Objects;

public class CurrenciesKey {
    public String stockExchangeName;
    public Currency currency;

    public CurrenciesKey(String stockExchangeName, Currency currency){
        this.stockExchangeName = stockExchangeName;
        this.currency = currency;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == null) {
            return false;
        }

        if (obj.getClass() != this.getClass()) {
            return false;
        }

        final CurrenciesKey other = (CurrenciesKey) obj;

        if (this.stockExchangeName != other.stockExchangeName) {
            return false;
        }

        return this.currency == other.currency;
    }

    @Override
    public int hashCode() {
        return Objects.hash(stockExchangeName, currency);
    }
}
