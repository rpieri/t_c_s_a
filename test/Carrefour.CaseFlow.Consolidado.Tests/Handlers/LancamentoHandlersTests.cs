using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Application.Handlers;
using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Shared.Events;
using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using FluentAssertions;
using Moq;

namespace Carrefour.CaseFlow.Consolidado.Tests.Handlers;

public class LancamentoHandlersTests
{
    private readonly Mock<ILancamentoRepository> _mockRepository;
    private readonly Mock<IKafkaEventPublisher> _mockEventPublisher;

    public LancamentoHandlersTests()
    {
        _mockRepository = new Mock<ILancamentoRepository>();
        _mockEventPublisher = new Mock<IKafkaEventPublisher>();
    }

    [Fact]
    public async Task CreateLancamentoHandler_DeveAdicionarLancamentoEPublicarEvento()
    {
        // Arrange
        var handler = new CreateLancamentoHandler(_mockRepository.Object, _mockEventPublisher.Object);
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100.50m,
            TipoLancamento.Credito,
            "Teste de lançamento",
            "Receita"
        );
        var command = new CreateLancamentoCommand(dto);

        var lancamentoSalvo = new Lancamento(dto.DataLancamento, dto.Valor, dto.Tipo, dto.Descricao, dto.Categoria);
        
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamentoSalvo);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DataLancamento.Should().Be(dto.DataLancamento);
        result.Valor.Should().Be(dto.Valor);
        result.Tipo.Should().Be(dto.Tipo);
        result.Descricao.Should().Be(dto.Descricao);
        result.Categoria.Should().Be(dto.Categoria);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(
            "lancamento-events",
            It.IsAny<string>(),
            It.IsAny<LancamentoCriado>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLancamentoByIdHandler_ComIdValido_DeveRetornarLancamento()
    {
        // Arrange
        var handler = new GetLancamentoByIdHandler(_mockRepository.Object);
        var id = Guid.NewGuid();
        var lancamento = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Teste", "Categoria");
        var query = new GetLancamentoByIdQuery(id);

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamento);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Valor.Should().Be(100m);
        result.Tipo.Should().Be(TipoLancamento.Credito);
        result.Descricao.Should().Be("Teste");
        result.Categoria.Should().Be("Categoria");

        _mockRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLancamentoByIdHandler_ComIdInvalido_DeveRetornarNull()
    {
        // Arrange
        var handler = new GetLancamentoByIdHandler(_mockRepository.Object);
        var id = Guid.NewGuid();
        var query = new GetLancamentoByIdQuery(id);

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Lancamento?)null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllLancamentosHandler_DeveRetornarListaPaginada()
    {
        // Arrange
        var handler = new GetAllLancamentosHandler(_mockRepository.Object);
        var page = 1;
        var pageSize = 10;
        var query = new GetAllLancamentosQuery(page, pageSize);

        var lancamentos = new List<Lancamento>
        {
            new(DateTime.Today, 100m, TipoLancamento.Credito, "Teste 1", "Cat1"),
            new(DateTime.Today, 200m, TipoLancamento.Debito, "Teste 2", "Cat2")
        };

        _mockRepository.Setup(r => r.GetAllAsync(page, pageSize, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamentos);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Valor.Should().Be(100m);
        resultList[1].Valor.Should().Be(200m);

        _mockRepository.Verify(r => r.GetAllAsync(page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLancamentosByPeriodoHandler_DeveRetornarLancamentosDoPeriodo()
    {
        // Arrange
        var handler = new GetLancamentosByPeriodoHandler(_mockRepository.Object);
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 31);
        var query = new GetLancamentosByPeriodoQuery(inicio, fim);

        var lancamentos = new List<Lancamento>
        {
            new(inicio, 100m, TipoLancamento.Credito, "Janeiro 1", "Receita"),
            new(fim, 200m, TipoLancamento.Debito, "Janeiro 31", "Despesa")
        };

        _mockRepository.Setup(r => r.GetByPeriodoAsync(inicio, fim, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamentos);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].DataLancamento.Should().Be(inicio);
        resultList[1].DataLancamento.Should().Be(fim);

        _mockRepository.Verify(r => r.GetByPeriodoAsync(inicio, fim, It.IsAny<CancellationToken>()), Times.Once);
    }



    [Fact]
    public async Task UpdateLancamentoHandler_ComLancamentoInexistente_DeveRetornarNull()
    {
        // Arrange
        var handler = new UpdateLancamentoHandler(_mockRepository.Object, _mockEventPublisher.Object);
        var id = Guid.NewGuid();
        var dto = new UpdateLancamentoDto(id, DateTime.Today, 200m, TipoLancamento.Debito, "Teste", "Categoria");
        var command = new UpdateLancamentoCommand(dto);

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Lancamento?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLancamentoHandler_SemAlteracaoSignificativa_NaoDevePublicarEvento()
    {
        // Arrange
        var handler = new UpdateLancamentoHandler(_mockRepository.Object, _mockEventPublisher.Object);
        var id = Guid.NewGuid();
        var dto = new UpdateLancamentoDto(
            id,
            DateTime.Today,
            100m, // Mesmo valor
            TipoLancamento.Credito, // Mesmo tipo
            "Nova descrição", // Apenas descrição mudou
            "Nova categoria" // Apenas categoria mudou
        );
        var command = new UpdateLancamentoCommand(dto);

        var lancamentoExistente = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Descrição antiga", "Categoria antiga");

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamentoExistente);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLancamentoHandler_ComLancamentoExistente_DeveDeletarEPublicarEvento()
    {
        // Arrange
        var handler = new DeleteLancamentoHandler(_mockRepository.Object, _mockEventPublisher.Object);
        var id = Guid.NewGuid();
        var command = new DeleteLancamentoCommand(id);

        var lancamentoExistente = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Teste", "Categoria");

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lancamentoExistente);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(p => p.PublishAsync(
            "lancamento-events",
            It.IsAny<string>(),
            It.IsAny<LancamentoRemovido>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLancamentoHandler_ComLancamentoInexistente_DeveRetornarFalse()
    {
        // Arrange
        var handler = new DeleteLancamentoHandler(_mockRepository.Object, _mockEventPublisher.Object);
        var id = Guid.NewGuid();
        var command = new DeleteLancamentoCommand(id);

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Lancamento?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}