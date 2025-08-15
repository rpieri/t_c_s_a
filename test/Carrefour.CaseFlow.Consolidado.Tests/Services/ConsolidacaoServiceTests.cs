using System.Text.Json;
using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Carrefour.CaseFlow.Consolidado.Service.Data;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using Carrefour.CaseFlow.Shared.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Carrefour.CaseFlow.Consolidado.Tests.Services;

public class ConsolidacaoServiceTests : IDisposable
{
    private readonly ConsolidadoDbContext _context;
    private readonly Mock<IDatabase> _mockRedis;
    private readonly Mock<ILogger<ConsolidacaoService>> _mockLogger;
    private readonly ConsolidacaoService _service;

    public ConsolidacaoServiceTests()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ConsolidadoDbContext(options);
        _mockRedis = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<ConsolidacaoService>>();
        _service = new ConsolidacaoService(_context, _mockRedis.Object, _mockLogger.Object);
    }



    [Fact]
    public async Task ProcessarLancamentoCriado_ComDebito_DeveAtualizarSaldoCorretamente()
    {
        // Arrange
        var saldoExistente = new SaldoConsolidado(DateTime.Today, 1000m);
        await _context.SaldosConsolidados.AddAsync(saldoExistente);
        await _context.SaveChangesAsync();

        var evento = new LancamentoRemovido(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            300.25m,
            TipoLancamento.Debito
        );

        // Act
        await _service.ProcessarLancamentoRemovido(evento);

        // Assert
        var saldo = await _context.SaldosConsolidados.FirstOrDefaultAsync(s => s.Data.Date == DateTime.Today);
        saldo.Should().NotBeNull();
        saldo!.TotalDebitos.Should().Be(-300.25m); // RemoverDebito subtrai do total
        saldo.SaldoFinal.Should().Be(1300.25m); // 1000 - (-300.25)
    }

    [Fact]
    public async Task ProcessarLancamentoAtualizado_DeveAtualizarValoresCorretamente()
    {
        // Arrange
        var saldoExistente = new SaldoConsolidado(DateTime.Today, 1000m);
        saldoExistente.AdicionarCredito(500m);
        await _context.SaldosConsolidados.AddAsync(saldoExistente);
        await _context.SaveChangesAsync();

        var evento = new LancamentoAtualizado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            DateTime.Today,
            800m, // Novo valor
            TipoLancamento.Credito,
            "Descrição atualizada",
            "Nova categoria",
            500m // Valor anterior
        );

        // Act
        await _service.ProcessarLancamentoAtualizado(evento);

        // Assert
        var saldo = await _context.SaldosConsolidados.FirstOrDefaultAsync(s => s.Data.Date == DateTime.Today);
        saldo.Should().NotBeNull();
        saldo!.TotalCreditos.Should().Be(800m); // Removeu 500 e adicionou 800
        saldo.SaldoFinal.Should().Be(1800m); // 1000 + 800
    }
    
    [Fact]
    public async Task ObterSaldoAtual_SemSaldoHoje_DeveRetornarUltimoSaldo()
    {
        // Arrange
        var ontem = DateTime.UtcNow.Date.AddDays(-1);
        var saldoOntem = new SaldoConsolidado(ontem, 800m);
        saldoOntem.AdicionarCredito(200m);
        await _context.SaldosConsolidados.AddAsync(saldoOntem);
        await _context.SaveChangesAsync();

        _mockRedis.Setup(r => r.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                 .ReturnsAsync(RedisValue.Null);

        // Act
        var resultado = await _service.ObterSaldoAtual();

        // Assert
        resultado.Should().Be(1000m);
    }

    [Fact]
    public async Task ObterSaldosPorPeriodo_DeveRetornarSaldosOrdenados()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fim = new DateTime(2025, 1, 3);

        var saldos = new[]
        {
            new SaldoConsolidado(inicio.AddDays(1), 500m), // 02/01
            new SaldoConsolidado(fim, 700m),               // 03/01
            new SaldoConsolidado(inicio, 300m)             // 01/01
        };

        await _context.SaldosConsolidados.AddRangeAsync(saldos);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _service.ObterSaldosPorPeriodo(inicio, fim);

        // Assert
        var saldosList = resultado.ToList();
        saldosList.Should().HaveCount(3);
        saldosList[0].Data.Should().Be(inicio);           // 01/01
        saldosList[1].Data.Should().Be(inicio.AddDays(1)); // 02/01
        saldosList[2].Data.Should().Be(fim);              // 03/01
    }


    [Fact]
    public async Task ObterOuCriarSaldoDiario_QuandoNaoExiste_DeveCriarComSaldoAnterior()
    {
        // Arrange
        var ontem = DateTime.Today.AddDays(-1);
        var hoje = DateTime.Today;
        
        var saldoOntem = new SaldoConsolidado(ontem, 500m);
        saldoOntem.AdicionarCredito(300m);
        await _context.SaldosConsolidados.AddAsync(saldoOntem);
        await _context.SaveChangesAsync();

        var evento = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "test-correlation",
            Guid.NewGuid(),
            hoje,
            100m,
            TipoLancamento.Credito,
            "Teste",
            "Categoria"
        );

        // Act
        await _service.ProcessarLancamentoCriado(evento);

        // Assert
        var saldoHoje = await _context.SaldosConsolidados.FirstOrDefaultAsync(s => s.Data.Date == hoje);
        saldoHoje.Should().NotBeNull();
        saldoHoje!.SaldoInicial.Should().Be(800m); // Saldo final de ontem (500 + 300)
        saldoHoje.TotalCreditos.Should().Be(100m);
        saldoHoje.SaldoFinal.Should().Be(900m);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}