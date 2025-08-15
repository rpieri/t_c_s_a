using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Carrefour.CaseFlow.Shared.Kafka.Services;

public class KafkaEventPublisher(IProducer<string,string> producer, ILogger<KafkaEventPublisher> logger): IKafkaEventPublisher, IDisposable
{
    
    public async Task PublishAsync<T>(string topic, string key, T eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var message = new Message<string, string>
            {
                Key = key,
                Value = serializedData,
                Headers = new Headers
                {
                    { "EventType", Encoding.UTF8.GetBytes(typeof(T).Name) },
                    { "Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) },
                    { "CorrelationId", Encoding.UTF8.GetBytes(Activity.Current?.Id ?? Guid.NewGuid().ToString()) },
                    { "ContentType", "application/json"u8.ToArray() }
                }
            };
            
            var deliveryResult = await producer.ProduceAsync(topic, message, cancellationToken);
            producer.Flush(cancellationToken);
            
            logger.LogInformation("Event published successfully to topic {Topic}, partition {Partition}, offset {Offset}",
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
            
            
        }catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event to topic {Topic} with key {Key}", topic, key);
            throw;
        }
    }

    public void Dispose() => producer?.Dispose();
}