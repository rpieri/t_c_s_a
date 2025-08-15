using Carrefour.CaseFlow.Consolidado.API.Controllers;
using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Carrefour.CaseFlow.Consolidado.Tests.Controllers;

public class ConsolidadoControllerTests
{
    private readonly Mock<IConsolidacaoService> _mockService;
    private readonly Mock<ILogger<ConsolidadoController>> _mockLogger;
    private readonly ConsolidadoController _controller;

    public ConsolidadoControllerTests()
    {
        _mockService = new Mock<IConsolidacaoService>();
        _mockLogger = new Mock<ILogger<ConsolidadoController>>();
        _controller = new ConsolidadoController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSaldoAtual_DeveRetornarOkComSaldo()
    {
        // Arrange
        var saldoEsperado = 1500.75m;
        _mockService.Setup(s => s.ObterSaldoAtual())
                   .ReturnsAsync(saldoEsperado);

        // Act
        var result = await _controller.GetSaldoAtual();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responseValue = okResult.Value;
        
        responseValue.Should().NotBeNull();
        var saldoAtual = responseValue.GetType().GetProperty("saldoAtual")?.GetValue(responseValue);
        saldoAtual.Should().Be(saldoEsperado);
        
        _mockService.Verify(s => s.ObterSaldoAtual(), Times.Once);
    }

    [Fact]
    public async Task GetSaldoAtual_QuandoOcorreExcecao_DeveRetornarInternalServerError()
    {
        // Arrange
        _mockService.Setup(s => s.ObterSaldoAtual())
                   .ThrowsAsync(new Exception("Erro de teste"));

        // Act
        var result = await _controller.GetSaldoAtual();

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSaldoPorData_ComDataValida_DeveRetornarOkComSaldo()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);
        var saldoEsperado = new SaldoConsolidado(data, 1000);
        saldoEsperado.AdicionarCredito(500);
        
        _mockService.Setup(s => s.ObterSaldoPorData(data))
                   .ReturnsAsync(saldoEsperado);

        // Act
        var result = await _controller.GetSaldoPorData(data);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(saldoEsperado);
        
        _mockService.Verify(s => s.ObterSaldoPorData(data), Times.Once);
    }

    [Fact]
    public async Task GetSaldoPorData_QuandoSaldoNaoEncontrado_DeveRetornarNotFound()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);
        _mockService.Setup(s => s.ObterSaldoPorData(data))
                   .ReturnsAsync((SaldoConsolidado?)null);

        // Act
        var result = await _controller.GetSaldoPorData(data);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var responseValue = notFoundResult.Value;
        
        responseValue.Should().NotBeNull();
        var message = responseValue.GetType().GetProperty("message")?.GetValue(responseValue);
        message.Should().Be($"Saldo não encontrado para a data {data:yyyy-MM-dd}");
    }

    [Fact]
    public async Task GetSaldoPorData_QuandoOcorreExcecao_DeveRetornarInternalServerError()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);
        _mockService.Setup(s => s.ObterSaldoPorData(data))
                   .ThrowsAsync(new Exception("Erro de teste"));

        // Act
        var result = await _controller.GetSaldoPorData(data);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetRelatorioPorPeriodo_ComPeriodoValido_DeveRetornarOkComRelatorio()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 31);
        var saldos = new List<SaldoConsolidado>
        {
            new(inicio, 1000) { TotalCreditos = 500, TotalDebitos = 200 },
            new(inicio.AddDays(1), 1300) { TotalCreditos = 300, TotalDebitos = 100 },
            new(fim, 1500) { TotalCreditos = 200, TotalDebitos = 0 }
        };
        
        _mockService.Setup(s => s.ObterSaldosPorPeriodo(inicio, fim))
                   .ReturnsAsync(saldos);

        // Act
        var result = await _controller.GetRelatorioPorPeriodo(inicio, fim);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        _mockService.Verify(s => s.ObterSaldosPorPeriodo(inicio, fim), Times.Once);
    }

    [Fact]
    public async Task GetRelatorioPorPeriodo_ComInicioMaiorQueFim_DeveRetornarBadRequest()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 31);
        var fim = new DateTime(2025, 1, 1);

        // Act
        var result = await _controller.GetRelatorioPorPeriodo(inicio, fim);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var responseValue = badRequestResult.Value;
        
        responseValue.Should().NotBeNull();
        var message = responseValue.GetType().GetProperty("message")?.GetValue(responseValue);
        message.Should().Be("Data de início deve ser menor ou igual à data de fim");
    }

    [Fact]
    public async Task GetRelatorioPorPeriodo_ComPeriodoMaiorQue365Dias_DeveRetornarBadRequest()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2026, 1, 2); // Mais de 365 dias

        // Act
        var result = await _controller.GetRelatorioPorPeriodo(inicio, fim);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var responseValue = badRequestResult.Value;
        
        responseValue.Should().NotBeNull();
        var message = responseValue.GetType().GetProperty("message")?.GetValue(responseValue);
        message.Should().Be("Período não pode ser maior que 365 dias");
    }

    [Fact]
    public async Task GetRelatorioPorPeriodo_QuandoOcorreExcecao_DeveRetornarInternalServerError()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 31);
        _mockService.Setup(s => s.ObterSaldosPorPeriodo(inicio, fim))
                   .ThrowsAsync(new Exception("Erro de teste"));

        // Act
        var result = await _controller.GetRelatorioPorPeriodo(inicio, fim);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetRelatorioPorPeriodo_DeveCalcularResumoCorretamente()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 3);
        var saldos = new List<SaldoConsolidado>
        {
            new(inicio, 1000) { TotalCreditos = 500, TotalDebitos = 200 }, // SaldoFinal = 1300
            new(inicio.AddDays(1), 1300) { TotalCreditos = 300, TotalDebitos = 100 }, // SaldoFinal = 1500
            new(fim, 1500) { TotalCreditos = 200, TotalDebitos = 50 } // SaldoFinal = 1650
        };
        
        _mockService.Setup(s => s.ObterSaldosPorPeriodo(inicio, fim))
                   .ReturnsAsync(saldos);

        // Act
        var result = await _controller.GetRelatorioPorPeriodo(inicio, fim);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var relatorio = okResult.Value;
        
        relatorio.Should().NotBeNull();
        
        // Verificar resumo calculado
        var resumo = relatorio!.GetType().GetProperty("resumo")?.GetValue(relatorio);
        resumo.Should().NotBeNull();
        
        var totalCreditos = resumo!.GetType().GetProperty("totalCreditos")?.GetValue(resumo);
        var totalDebitos = resumo.GetType().GetProperty("totalDebitos")?.GetValue(resumo);
        
        totalCreditos.Should().Be(1000m); // 500 + 300 + 200
        totalDebitos.Should().Be(350m); // 200 + 100 + 50
    }
}