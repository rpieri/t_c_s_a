using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using Carrefour.CaseFlow.Shared.Kafka.Configurations;
using Carrefour.CaseFlow.Shared.Kafka.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Carrefour.CaseFlow.Shared.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaProducerConfig>(configuration.GetSection(KafkaProducerConfig.SectionName));
        
        services.AddSingleton<IProducer<string, string>>(provider =>
        {
            var config = new KafkaProducerConfig();
            configuration.GetSection(KafkaProducerConfig.SectionName).Bind(config);
            
            var logger = provider.GetRequiredService<ILogger<IProducer<string, string>>>();
            
            return new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => logger.LogError("Kafka Producer Error: {Reason}", e.Reason))
                .SetLogHandler((_, logMessage) => logger.LogDebug("Kafka Producer Log: {Message}", logMessage.Message))
                .Build();
        });

        services.AddScoped<IKafkaEventPublisher, KafkaEventPublisher>();
        
        return services;
    }

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, IConfiguration configuration, string groupId)
    {
        services.Configure<KafkaConsumerConfig>(configuration.GetSection(KafkaConsumerConfig.SectionName));
        
        services.AddSingleton<IConsumer<string, string>>(provider =>
        {
            var config = new KafkaConsumerConfig { GroupId = groupId };
            configuration.GetSection(KafkaConsumerConfig.SectionName).Bind(config);
            
            var logger = provider.GetRequiredService<ILogger<IConsumer<string, string>>>();
            
            return new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => logger.LogError("Kafka Consumer Error: {Reason}", e.Reason))
                .SetLogHandler((_, logMessage) => logger.LogDebug("Kafka Consumer Log: {Message}", logMessage.Message))
                .SetPartitionsAssignedHandler((_, partitions) =>
                {
                    logger.LogInformation("Assigned partitions: [{Partitions}]", string.Join(", ", partitions));
                })
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    logger.LogInformation("Revoked partitions: [{Partitions}]", string.Join(", ", partitions));
                })
                .Build();
        });
        
        return services;
    }
}