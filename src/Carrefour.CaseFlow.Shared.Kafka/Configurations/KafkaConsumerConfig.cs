using Confluent.Kafka;

namespace Carrefour.CaseFlow.Shared.Kafka.Configurations;

public class KafkaConsumerConfig: ConsumerConfig
{
    public const string SectionName = "Kafka:Consumer";
    
    public KafkaConsumerConfig()
    {
        BootstrapServers = "localhost:9092";
        GroupId = "consolidado-service";
        AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
        EnableAutoCommit = false;
        SessionTimeoutMs = 30000;
        MaxPollIntervalMs = 300000;
        SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext;
        EnableAutoOffsetStore = false;
        IsolationLevel = Confluent.Kafka.IsolationLevel.ReadCommitted;
    }
}