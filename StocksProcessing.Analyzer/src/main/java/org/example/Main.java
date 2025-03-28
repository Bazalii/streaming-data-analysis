package org.example;

import org.apache.flink.api.common.eventtime.WatermarkStrategy;
import org.apache.flink.connector.kafka.sink.KafkaRecordSerializationSchema;
import org.apache.flink.connector.kafka.sink.KafkaSink;
import org.apache.flink.connector.kafka.source.KafkaSource;
import org.apache.flink.connector.kafka.source.enumerator.initializer.OffsetsInitializer;
import org.apache.flink.formats.json.JsonDeserializationSchema;
import org.apache.flink.formats.json.JsonSerializationSchema;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.flink.streaming.api.windowing.assigners.SlidingProcessingTimeWindows;
import org.apache.flink.streaming.api.windowing.time.Time;
import org.apache.flink.streaming.api.windowing.windows.TimeWindow;
import org.apache.kafka.clients.consumer.OffsetResetStrategy;

import org.apache.flink.streaming.api.functions.windowing.ProcessWindowFunction;
import org.apache.flink.util.Collector;
import org.example.models.keys.CurrenciesKey;
import org.example.models.messages.CurrencyRateChangeMessage;
import org.example.models.messages.CurrencyRatesStatisticsMessage;

public class Main {
    private static final String bootstrapServers = "localhost:9092,localhost:9094";
    private static final String groupId = "analyzer";
    private static final String inputTopic = "currency_rates_changes";
    private static final String outputTopic = "currency_statistics";

    public static void main(String[] args) throws Exception {
        var currencyRateChangesDeserializer = new JsonDeserializationSchema<>(CurrencyRateChangeMessage.class);
        var currencyStatisticsJsonSerializer = new JsonSerializationSchema<CurrencyRatesStatisticsMessage>();
        var currencyStatisticsKafkaSerializer = KafkaRecordSerializationSchema.builder()
                .setValueSerializationSchema(currencyStatisticsJsonSerializer)
                .setTopic(outputTopic)
                .build();

        var env = StreamExecutionEnvironment.getExecutionEnvironment();

        KafkaSource<CurrencyRateChangeMessage> source = KafkaSource.<CurrencyRateChangeMessage>builder()
                .setBootstrapServers(bootstrapServers)
                .setTopics(inputTopic)
                .setGroupId(groupId)
                .setStartingOffsets(OffsetsInitializer.committedOffsets(OffsetResetStrategy.LATEST))
                .setValueOnlyDeserializer(currencyRateChangesDeserializer)
                .build();

        var kafkaSink = KafkaSink.<CurrencyRatesStatisticsMessage>builder()
                .setBootstrapServers(bootstrapServers)
                .setRecordSerializer(currencyStatisticsKafkaSerializer)
                .build();

        var stream = env
                .fromSource(source, WatermarkStrategy.noWatermarks(), "Kafka Source")
                .keyBy(message -> new CurrenciesKey(message.stockExchangeName, message.currency))
                .window(SlidingProcessingTimeWindows.of(Time.seconds(5), Time.seconds(1)))
                .process(new GetAverageRateFunction())
                .sinkTo(kafkaSink);

        env.execute();
    }

    public static class GetAverageRateFunction
            extends ProcessWindowFunction<CurrencyRateChangeMessage, CurrencyRatesStatisticsMessage, CurrenciesKey, TimeWindow> {

        @Override
        public void process(
                CurrenciesKey id,
                Context ctx,
                Iterable<CurrencyRateChangeMessage> messages,
                Collector<CurrencyRatesStatisticsMessage> out) {

            var ratesSummary = 0d;
            var numberOfRates = 0;

            for (CurrencyRateChangeMessage message : messages) {
                ratesSummary += message.rateInRubles;
                numberOfRates++;
            }

            var averageCurrencyRate = ratesSummary / numberOfRates;

            out.collect(new CurrencyRatesStatisticsMessage(
                    id.stockExchangeName,
                    id.currency,
                    averageCurrencyRate));
        }
    }
}