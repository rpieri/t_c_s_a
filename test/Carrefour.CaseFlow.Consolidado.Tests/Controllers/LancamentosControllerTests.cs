using Carrefour.CaseFlow.Lancamentos.API.Controllers;
using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Shared.Events;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Carrefour.CaseFlow.Consolidado.Tests.Controllers;

public class LancamentosControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<LancamentosController>> _mockLogger;
    private readonly LancamentosController _controller;

    public LancamentosControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<LancamentosController>>();
        _controller = new LancamentosController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Create_ComDadosValidos_DeveRetornarCreated()
    {
        // Arrange
        var request = new CreateLancamentoDto(
            DateTime.Today,
            100.50m,
            TipoLancamento.Credito,
            "Teste de lançamento",
            "Receita"
        );

        var expectedResult = new LancamentoDto(
            Guid.NewGuid(),
            request.DataLancamento,
            request.Valor,
            request.Tipo,
            request.Descricao,
            request.Categoria,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(expectedResult);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        
        _mockMediator.Verify(m => m.Send(It.Is<CreateLancamentoCommand>(c => c.Lancamento == request), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_QuandoOcorreExcecao_DeveRetornarInternalServerError()
    {
        // Arrange
        var request = new CreateLancamentoDto(DateTime.Today, 100m, TipoLancamento.Credito, "Teste", "Categoria");
        _mockMediator.Setup(m => m.Send(It.IsAny<CreateLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Erro de teste"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetById_ComIdValido_DeveRetornarOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedResult = new LancamentoDto(
            id,
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockMediator.Setup(m => m.Send(It.IsAny<GetLancamentoByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResult);
        
        _mockMediator.Verify(m => m.Send(It.Is<GetLancamentoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_QuandoNaoEncontrado_DeveRetornarNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockMediator.Setup(m => m.Send(It.IsAny<GetLancamentoByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((LancamentoDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ComParametrosValidos_DeveRetornarOk()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var expectedResult = new List<LancamentoDto>
        {
            new(Guid.NewGuid(), DateTime.Today, 100m, TipoLancamento.Credito, "Teste 1", "Cat1", DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), DateTime.Today, 200m, TipoLancamento.Debito, "Teste 2", "Cat2", DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllLancamentosQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(page, pageSize, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        _mockMediator.Verify(m => m.Send(It.Is<GetAllLancamentosQuery>(q => q.Page == page && q.PageSize == pageSize), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 10, 1, 10)] // page inválido
    [InlineData(-1, 10, 1, 10)] // page negativo
    [InlineData(1, 0, 1, 10)] // pageSize inválido
    [InlineData(1, 150, 1, 10)] // pageSize muito grande
    public async Task GetAll_ComParametrosInvalidos_DeveCorrigirParametros(int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllLancamentosQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<LancamentoDto>());

        // Act
        await _controller.GetAll(inputPage, inputPageSize, CancellationToken.None);

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<GetAllLancamentosQuery>(q => q.Page == expectedPage && q.PageSize == expectedPageSize), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByPeriodo_ComPeriodoValido_DeveRetornarOk()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 31);
        var expectedResult = new List<LancamentoDto>
        {
            new(Guid.NewGuid(), inicio, 100m, TipoLancamento.Credito, "Teste", "Cat", DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetLancamentosByPeriodoQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetByPeriodo(inicio, fim, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        _mockMediator.Verify(m => m.Send(It.Is<GetLancamentosByPeriodoQuery>(q => q.Inicio == inicio && q.Fim == fim), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ComDadosValidos_DeveRetornarOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateLancamentoDto(
            id,
            DateTime.Today,
            200m,
            TipoLancamento.Debito,
            "Lançamento atualizado",
            "Nova categoria"
        );

        var expectedResult = new LancamentoDto(
            id,
            request.DataLancamento,
            request.Valor,
            request.Tipo,
            request.Descricao,
            request.Categoria,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow
        );

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResult);
        
        _mockMediator.Verify(m => m.Send(It.Is<UpdateLancamentoCommand>(c => c.Lancamento == request), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ComIdDiferente_DeveRetornarBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateLancamentoDto(
            Guid.NewGuid(), // ID diferente
            DateTime.Today,
            200m,
            TipoLancamento.Debito,
            "Teste",
            "Categoria"
        );

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("ID do parâmetro não coincide com ID do body");
    }

    [Fact]
    public async Task Update_QuandoNaoEncontrado_DeveRetornarNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateLancamentoDto(id, DateTime.Today, 200m, TipoLancamento.Debito, "Teste", "Categoria");

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((LancamentoDto?)null);

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Lançamento com ID {id} não encontrado");
    }

    [Fact]
    public async Task Delete_ComIdValido_DeveRetornarNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        _mockMediator.Verify(m => m.Send(It.Is<DeleteLancamentoCommand>(c => c.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_QuandoNaoEncontrado_DeveRetornarNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteLancamentoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Lançamento com ID {id} não encontrado");
    }
}