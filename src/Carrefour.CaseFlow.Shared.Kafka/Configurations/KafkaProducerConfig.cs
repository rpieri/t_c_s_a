using Confluent.Kafka;

namespace Carrefour.CaseFlow.Shared.Kafka.Configurations;

public class KafkaProducerConfig : ProducerConfig
{
    public const string SectionName = "Kafka:Producer";
    public KafkaProducerConfig()
    {
        BootstrapServers = "localhost:9092";
        Acks = Confluent.Kafka.Acks.All;
        EnableIdempotence = true;
        MessageTimeoutMs = 30000;
        CompressionType = Confluent.Kafka.CompressionType.Snappy;
        SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext;
    }
}