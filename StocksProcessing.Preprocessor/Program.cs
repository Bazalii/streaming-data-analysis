using StocksProcessing.Preprocessor.Preprocessors;

const string kafkaConnectionString = "localhost:9092,localhost:9094";
const string currencyRateChangesWebsocket = "ws://localhost:5000/ws/stocks/currencies";

var stocksPreprocessor = new StocksPreprocessor(kafkaConnectionString, currencyRateChangesWebsocket);

await stocksPreprocessor.ProcessCurrencyRatesChangeEventsAsync();