using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Data;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Repositories;
using Carrefour.CaseFlow.Shared.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Carrefour.CaseFlow.Consolidado.Tests.Repositories;

public class LancamentoRepositoryTests : IDisposable
{
    private readonly LancamentoDbContext _context;
    private readonly LancamentoRepository _repository;

    public LancamentoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LancamentoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LancamentoDbContext(options);
        _repository = new LancamentoRepository(_context);
    }

    [Fact]
    public async Task AddAsync_DeveAdicionarLancamentoCorretamente()
    {
        // Arrange
        var lancamento = new Lancamento(
            DateTime.Today,
            150.75m,
            TipoLancamento.Credito,
            "Teste de adição",
            "Receita"
        );

        // Act
        var result = await _repository.AddAsync(lancamento);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Valor.Should().Be(150.75m);

        var lancamentoNoBanco = await _context.Lancamentos.FirstOrDefaultAsync(l => l.Id == result.Id);
        lancamentoNoBanco.Should().NotBeNull();
        lancamentoNoBanco!.Valor.Should().Be(150.75m);
    }

    [Fact]
    public async Task GetByIdAsync_ComIdExistente_DeveRetornarLancamento()
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Teste", "Categoria");
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(lancamento.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(lancamento.Id);
        result.Valor.Should().Be(100m);
        result.Descricao.Should().Be("Teste");
    }

    [Fact]
    public async Task GetByIdAsync_ComIdInexistente_DeveRetornarNull()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(idInexistente);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_DevePaginarResultados()
    {
        // Arrange
        var lancamentos = new List<Lancamento>
        {
            new(DateTime.Today.AddDays(-3), 100m, TipoLancamento.Credito, "Teste 1", "Cat1"),
            new(DateTime.Today.AddDays(-2), 200m, TipoLancamento.Debito, "Teste 2", "Cat2"),
            new(DateTime.Today.AddDays(-1), 300m, TipoLancamento.Credito, "Teste 3", "Cat3"),
            new(DateTime.Today, 400m, TipoLancamento.Debito, "Teste 4", "Cat4")
        };

        await _context.Lancamentos.AddRangeAsync(lancamentos);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        // Deve retornar ordenado por data decrescente
        resultList[0].DataLancamento.Should().Be(DateTime.Today);
        resultList[1].DataLancamento.Should().Be(DateTime.Today.AddDays(-1));
    }

    [Fact]
    public async Task GetAllAsync_ComSegundaPagina_DeveRetornarProximosResultados()
    {
        // Arrange
        var lancamentos = new List<Lancamento>
        {
            new(DateTime.Today.AddDays(-3), 100m, TipoLancamento.Credito, "Teste 1", "Cat1"),
            new(DateTime.Today.AddDays(-2), 200m, TipoLancamento.Debito, "Teste 2", "Cat2"),
            new(DateTime.Today.AddDays(-1), 300m, TipoLancamento.Credito, "Teste 3", "Cat3"),
            new(DateTime.Today, 400m, TipoLancamento.Debito, "Teste 4", "Cat4")
        };

        await _context.Lancamentos.AddRangeAsync(lancamentos);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].DataLancamento.Should().Be(DateTime.Today.AddDays(-2));
        resultList[1].DataLancamento.Should().Be(DateTime.Today.AddDays(-3));
    }

    [Fact]
    public async Task GetByPeriodoAsync_DeveFiltrarPorPeriodo()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 10);
        var fim = new DateTime(2025, 1, 20);

        var lancamentos = new List<Lancamento>
        {
            new(new DateTime(2025, 1, 5), 100m, TipoLancamento.Credito, "Antes do período", "Cat1"),
            new(new DateTime(2025, 1, 15), 200m, TipoLancamento.Debito, "Dentro do período", "Cat2"),
            new(new DateTime(2025, 1, 18), 300m, TipoLancamento.Credito, "Dentro do período 2", "Cat3"),
            new(new DateTime(2025, 1, 25), 400m, TipoLancamento.Debito, "Após o período", "Cat4")
        };

        await _context.Lancamentos.AddRangeAsync(lancamentos);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPeriodoAsync(inicio, fim);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList.Should().OnlyContain(l => l.DataLancamento >= inicio && l.DataLancamento <= fim);
        // Deve estar ordenado por data decrescente
        resultList[0].DataLancamento.Should().Be(new DateTime(2025, 1, 18));
        resultList[1].DataLancamento.Should().Be(new DateTime(2025, 1, 15));
    }

    [Fact]
    public async Task UpdateAsync_DeveAtualizarLancamentoExistente()
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Descrição original", "Categoria original");
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();

        // Act
        lancamento.Atualizar(DateTime.Today.AddDays(1), 200m, TipoLancamento.Debito, "Nova descrição", "Nova categoria");
        await _repository.UpdateAsync(lancamento);

        // Assert
        var lancamentoAtualizado = await _context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        lancamentoAtualizado.Should().NotBeNull();
        lancamentoAtualizado!.Valor.Should().Be(200m);
        lancamentoAtualizado.Tipo.Should().Be(TipoLancamento.Debito);
        lancamentoAtualizado.Descricao.Should().Be("Nova descrição");
        lancamentoAtualizado.Categoria.Should().Be("Nova categoria");
        lancamentoAtualizado.DataLancamento.Should().Be(DateTime.Today.AddDays(1));
    }

    [Fact]
    public async Task DeleteAsync_ComLancamentoExistente_DeveRemoverLancamento()
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100m, TipoLancamento.Credito, "Teste", "Categoria");
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(lancamento.Id);

        // Assert
        var lancamentoDeletado = await _context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        lancamentoDeletado.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ComIdInexistente_NaoDeveLancarExcecao()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act & Assert
        await _repository.DeleteAsync(idInexistente); // Não deve lançar exceção
    }

    [Fact]
    public async Task GetAllAsync_ComParametrosPadrao_DeveRetornarPrimeiraPagina()
    {
        // Arrange
        var lancamentos = Enumerable.Range(1, 15)
            .Select(i => new Lancamento(DateTime.Today.AddDays(-i), i * 10m, TipoLancamento.Credito, $"Teste {i}", "Categoria"))
            .ToList();

        await _context.Lancamentos.AddRangeAsync(lancamentos);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(); // Usa parâmetros padrão: page=1, pageSize=10

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(10);
        // Deve estar ordenado por data decrescente (mais recente primeiro)
        resultList[0].DataLancamento.Should().Be(DateTime.Today.AddDays(-1));
        resultList[9].DataLancamento.Should().Be(DateTime.Today.AddDays(-10));
    }

    [Fact]
    public async Task GetByPeriodoAsync_ComPeriodoSemLancamentos_DeveRetornarListaVazia()
    {
        // Arrange
        var inicio = new DateTime(2025, 6, 1);
        var fim = new DateTime(2025, 6, 30);

        var lancamento = new Lancamento(new DateTime(2025, 1, 15), 100m, TipoLancamento.Credito, "Teste", "Categoria");
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPeriodoAsync(inicio, fim);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPeriodoAsync_ComDataExata_DeveIncluirLancamentoNaDataLimite()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);
        var lancamento = new Lancamento(data, 100m, TipoLancamento.Credito, "Teste", "Categoria");
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPeriodoAsync(data, data);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(1);
        resultList[0].DataLancamento.Should().Be(data);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}