using System.Text;
using Carrefour.CaseFlow.Shared.Events;
using Carrefour.CaseFlow.Shared.Kafka.Services;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Carrefour.CaseFlow.Consolidado.Tests.Kafka;

public class KafkaEventPublisherTests
{
    private readonly Mock<IProducer<string, string>> _mockProducer;
    private readonly Mock<ILogger<KafkaEventPublisher>> _mockLogger;
    private readonly KafkaEventPublisher _publisher;

    public KafkaEventPublisherTests()
    {
        _mockProducer = new Mock<IProducer<string, string>>();
        _mockLogger = new Mock<ILogger<KafkaEventPublisher>>();
        _publisher = new KafkaEventPublisher(_mockProducer.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_ComEventoValido_DevePublicarCorretamente()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        _mockProducer.Verify(p => p.ProduceAsync(
            topic,
            It.Is<Message<string, string>>(m => 
                m.Key == key && 
                m.Value.Contains("lancamentoId") &&
                m.Headers.Any(h => h.Key == "EventType")),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockProducer.Verify(p => p.Flush(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DeveSerializarEventoComCamelCase()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        Message<string, string>? capturedMessage = null;
        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, ct) => capturedMessage = m)
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Value.Should().Contain("lancamentoId"); // Propriedade em camelCase
        capturedMessage.Value.Should().Contain("dataLancamento"); // Propriedade em camelCase
        capturedMessage.Value.Should().NotContain("LancamentoId"); // Não deve conter PascalCase
    }

    [Fact]
    public async Task PublishAsync_DeveAdicionarHeadersCorretos()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        Message<string, string>? capturedMessage = null;
        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, ct) => capturedMessage = m)
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Headers.Should().NotBeNull();
        
        var headers = capturedMessage.Headers.ToDictionary(h => h.Key, h => Encoding.UTF8.GetString(h.GetValueBytes()));
        
        headers.Should().ContainKey("EventType");
        headers["EventType"].Should().Be("LancamentoCriado");
        
        headers.Should().ContainKey("Timestamp");
        headers.Should().ContainKey("CorrelationId");
        headers.Should().ContainKey("ContentType");
        headers["ContentType"].Should().Be("application/json");
    }

    [Fact]
    public async Task PublishAsync_QuandoOcorreExcecao_DeveLogarErroERelançar()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<string, string>(
                new Error(ErrorCode.Local_MsgTimedOut, "Timeout"), 
                new DeliveryResult<string, string>()));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProduceException<string, string>>(
            () => _publisher.PublishAsync(topic, key, evento));

        exception.Should().NotBeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ComSucesso_DeveLogarInformacao()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event published successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ComDiferentesTiposDeEventos_DeveDefinirEventTypeCorreto()
    {
        // Arrange
        var topic = "test-topic";
        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        var eventoAtualizado = new LancamentoAtualizado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            200m,
            TipoLancamento.Debito,
            "Teste Atualizado",
            "Categoria",
            150m
        );

        Message<string, string>? capturedMessage = null;
        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, ct) => capturedMessage = m)
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, "key", eventoAtualizado);

        // Assert
        capturedMessage.Should().NotBeNull();
        var eventTypeHeader = capturedMessage!.Headers.FirstOrDefault(h => h.Key == "EventType");
        eventTypeHeader.Should().NotBeNull();
        
        var eventType = Encoding.UTF8.GetString(eventTypeHeader!.GetValueBytes());
        eventType.Should().Be("LancamentoAtualizado");
    }

    [Fact]
    public async Task PublishAsync_ComEventoRemovido_DevePublicarCorretamente()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoRemovido(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        _mockProducer.Verify(p => p.ProduceAsync(
            topic,
            It.Is<Message<string, string>>(m => 
                m.Key == key && 
                m.Value.Contains("lancamentoId")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_DeveDisporProducer()
    {
        // Act
        _publisher.Dispose();

        // Assert
        _mockProducer.Verify(p => p.Dispose(), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DeveUsarCorrelationIdDoActivity()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        Message<string, string>? capturedMessage = null;
        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, ct) => capturedMessage = m)
            .ReturnsAsync(deliveryResult);

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        capturedMessage.Should().NotBeNull();
        var correlationIdHeader = capturedMessage!.Headers.FirstOrDefault(h => h.Key == "CorrelationId");
        correlationIdHeader.Should().NotBeNull();
        
        var correlationId = Encoding.UTF8.GetString(correlationIdHeader!.GetValueBytes());
        correlationId.Should().NotBeNullOrEmpty();
        // Deve ser um GUID válido ou um ID de Activity
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_DeveDefinirTimestampCorreto()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123
        };

        Message<string, string>? capturedMessage = null;
        _mockProducer.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((t, m, ct) => capturedMessage = m)
            .ReturnsAsync(deliveryResult);

        var timestampAntes = DateTimeOffset.UtcNow;

        // Act
        await _publisher.PublishAsync(topic, key, evento);

        // Assert
        capturedMessage.Should().NotBeNull();
        var timestampHeader = capturedMessage!.Headers.FirstOrDefault(h => h.Key == "Timestamp");
        timestampHeader.Should().NotBeNull();
        
        var timestampString = Encoding.UTF8.GetString(timestampHeader!.GetValueBytes());
        DateTimeOffset.TryParse(timestampString, out var timestamp).Should().BeTrue();
        
        timestamp.Should().BeCloseTo(timestampAntes, TimeSpan.FromSeconds(1));
    }
}