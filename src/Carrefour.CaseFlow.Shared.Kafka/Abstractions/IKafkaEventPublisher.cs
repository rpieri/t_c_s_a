namespace Carrefour.CaseFlow.Shared.Kafka.Abstractions;

public interface IKafkaEventPublisher
{
    Task PublishAsync<T>(string topic, string key, T eventData, CancellationToken cancellationToken = default);
}