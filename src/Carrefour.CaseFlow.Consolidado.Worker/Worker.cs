using System.Text.Json;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using Carrefour.CaseFlow.Shared.Events;
using Confluent.Kafka;

namespace Carrefour.CaseFlow.Consolidado.Worker;

public class Worker(IConsumer<string, string> consumer, 
    IServiceProvider serviceProvider,ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("lancamento-events");
        logger.LogInformation("Kafka consumer started, listening to topic: lancamento-events");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessage(consumeResult.Message);
                        consumer.Commit(consumeResult);
                        
                        logger.LogDebug("Processed and committed message with key: {Key}", consumeResult.Message.Key);
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Kafka consumer stopped");
        }
    }
    
    private async Task ProcessMessage(Message<string, string> message)
    {
        using var scope = serviceProvider.CreateScope();
        var consolidacaoService = scope.ServiceProvider.GetRequiredService<IConsolidacaoService>();

        try
        {
            var eventType = GetEventType(message.Headers);
            
            logger.LogInformation("Processing message: {EventType} with key: {Key}", eventType, message.Key);

            switch (eventType)
            {
                case "LancamentoCriado":
                    var lancamentoCriado = JsonSerializer.Deserialize<LancamentoCriado>(message.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (lancamentoCriado != null)
                        await consolidacaoService.ProcessarLancamentoCriado(lancamentoCriado);
                    break;
                    
                case "LancamentoAtualizado":
                    var lancamentoAtualizado = JsonSerializer.Deserialize<LancamentoAtualizado>(message.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (lancamentoAtualizado != null)
                        await consolidacaoService.ProcessarLancamentoAtualizado(lancamentoAtualizado);
                    break;
                    
                case "LancamentoRemovido":
                    var lancamentoRemovido = JsonSerializer.Deserialize<LancamentoRemovido>(message.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (lancamentoRemovido != null)
                        await consolidacaoService.ProcessarLancamentoRemovido(lancamentoRemovido);
                    break;
                    
                default:
                    logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }

            logger.LogInformation("Successfully processed message: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message.Value);
            throw; 
        }
    }
    
    private string GetEventType(Headers headers)
    {
        var eventTypeHeader = headers.FirstOrDefault(h => h.Key == "EventType");
        return eventTypeHeader?.GetValueBytes() != null 
            ? System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes()) 
            : "Unknown";
    }

    public override void Dispose()
    {
        consumer?.Dispose();
        base.Dispose();
    }
}