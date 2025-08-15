using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Shared.Events;
using FluentAssertions;

namespace Carrefour.CaseFlow.Consolidado.Tests.Domain;

public class LancamentoTests
{
    [Fact]
    public void Constructor_DeveInicializarPropriedadesCorretamente()
    {
        // Arrange
        var dataLancamento = new DateTime(2025, 1, 15, 10, 30, 0);
        var valor = 150.75m;
        var tipo = TipoLancamento.Credito;
        var descricao = "Teste de lançamento";
        var categoria = "Receita";

        // Act
        var lancamento = new Lancamento(dataLancamento, valor, tipo, descricao, categoria);

        // Assert
        lancamento.Id.Should().NotBeEmpty();
        lancamento.DataLancamento.Should().Be(dataLancamento.Date);
        lancamento.Valor.Should().Be(valor);
        lancamento.Tipo.Should().Be(tipo);
        lancamento.Descricao.Should().Be(descricao);
        lancamento.Categoria.Should().Be(categoria);
        lancamento.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        lancamento.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public void Constructor_ComValorInvalido_DeveLancarExcecao(decimal valorInvalido)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Lancamento(DateTime.Today, valorInvalido, TipoLancamento.Credito, "Descrição", "Categoria"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ComDescricaoInvalida_DeveLancarExcecao(string descricaoInvalida)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, descricaoInvalida, "Categoria"));
    }

    [Fact]
    public void Constructor_ComCategoriaNull_DeveInicializarComStringVazia()
    {
        // Arrange & Act
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, "Descrição", null);

        // Assert
        lancamento.Categoria.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_DeveRemoverEspacosEmBranco()
    {
        // Arrange
        var descricao = "  Descrição com espaços  ";
        var categoria = "  Categoria com espaços  ";

        // Act
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, descricao, categoria);

        // Assert
        lancamento.Descricao.Should().Be("Descrição com espaços");
        lancamento.Categoria.Should().Be("Categoria com espaços");
    }

    [Fact]
    public void Atualizar_DeveModificarPropriedadesCorretamente()
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Debito, "Descrição Original", "Categoria Original");
        var novaData = DateTime.Today.AddDays(1);
        var novoValor = 200.50m;
        var novoTipo = TipoLancamento.Credito;
        var novaDescricao = "Nova Descrição";
        var novaCategoria = "Nova Categoria";
        var updatedAtOriginal = lancamento.UpdatedAt;

        // Act
        lancamento.Atualizar(novaData, novoValor, novoTipo, novaDescricao, novaCategoria);

        // Assert
        lancamento.DataLancamento.Should().Be(novaData.Date);
        lancamento.Valor.Should().Be(novoValor);
        lancamento.Tipo.Should().Be(novoTipo);
        lancamento.Descricao.Should().Be(novaDescricao);
        lancamento.Categoria.Should().Be(novaCategoria);
        lancamento.UpdatedAt.Should().BeAfter(updatedAtOriginal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Atualizar_ComValorInvalido_DeveLancarExcecao(decimal valorInvalido)
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, "Descrição", "Categoria");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            lancamento.Atualizar(DateTime.Today, valorInvalido, TipoLancamento.Credito, "Nova Descrição", "Nova Categoria"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Atualizar_ComDescricaoInvalida_DeveLancarExcecao(string descricaoInvalida)
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, "Descrição", "Categoria");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            lancamento.Atualizar(DateTime.Today, 200, TipoLancamento.Credito, descricaoInvalida, "Nova Categoria"));
    }

    [Fact]
    public void DataLancamento_DeveSempreSerApenasSemHora()
    {
        // Arrange
        var dataComHora = new DateTime(2025, 1, 15, 14, 30, 45);

        // Act
        var lancamento = new Lancamento(dataComHora, 100, TipoLancamento.Credito, "Descrição", "Categoria");

        // Assert
        lancamento.DataLancamento.Should().Be(new DateTime(2025, 1, 15, 0, 0, 0));
        lancamento.DataLancamento.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public void Constructor_ComTiposValidos_DeveAceitar(TipoLancamento tipo)
    {
        // Arrange & Act
        var lancamento = new Lancamento(DateTime.Today, 100, tipo, "Descrição", "Categoria");

        // Assert
        lancamento.Tipo.Should().Be(tipo);
    }

    [Fact]
    public void Atualizar_DeveManterCreatedAtInerado()
    {
        // Arrange
        var lancamento = new Lancamento(DateTime.Today, 100, TipoLancamento.Credito, "Descrição", "Categoria");
        var createdAtOriginal = lancamento.CreatedAt;

        // Act
        Thread.Sleep(10); // Garante diferença de tempo
        lancamento.Atualizar(DateTime.Today.AddDays(1), 200, TipoLancamento.Debito, "Nova Descrição", "Nova Categoria");

        // Assert
        lancamento.CreatedAt.Should().Be(createdAtOriginal);
    }
}