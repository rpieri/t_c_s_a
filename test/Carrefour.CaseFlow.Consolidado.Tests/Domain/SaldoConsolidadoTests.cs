using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using FluentAssertions;

namespace Carrefour.CaseFlow.Consolidado.Tests.Domain;

public class SaldoConsolidadoTests
{
    [Fact]
    public void Constructor_DeveInicializarPropriedadesCorretamente()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);
        var saldoInicial = 1000m;

        // Act
        var saldo = new SaldoConsolidado(data, saldoInicial);

        // Assert
        saldo.Id.Should().NotBeEmpty();
        saldo.Data.Should().Be(data.Date);
        saldo.SaldoInicial.Should().Be(saldoInicial);
        saldo.TotalCreditos.Should().Be(0);
        saldo.TotalDebitos.Should().Be(0);
        saldo.SaldoFinal.Should().Be(saldoInicial);
        saldo.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_SemSaldoInicial_DeveInicializarComZero()
    {
        // Arrange
        var data = new DateTime(2025, 1, 15);

        // Act
        var saldo = new SaldoConsolidado(data);

        // Assert
        saldo.SaldoInicial.Should().Be(0);
        saldo.SaldoFinal.Should().Be(0);
    }
    


    [Fact]
    public void RemoverCredito_DeveSubtrairDoTotalCreditos()
    {
        // Arrange
        var saldo = new SaldoConsolidado(DateTime.Today, 500);
        saldo.AdicionarCredito(300);
        var updateTimeBefore = saldo.UpdatedAt;

        // Act
        saldo.RemoverCredito(100);

        // Assert
        saldo.TotalCreditos.Should().Be(200);
        saldo.SaldoFinal.Should().Be(700); // 500 + 200
        saldo.UpdatedAt.Should().BeAfter(updateTimeBefore);
    }

    [Fact]
    public void RemoverDebito_DeveSubtrairDoTotalDebitos()
    {
        // Arrange
        var saldo = new SaldoConsolidado(DateTime.Today, 1000);
        saldo.AdicionarDebito(300);
        var updateTimeBefore = saldo.UpdatedAt;

        // Act
        saldo.RemoverDebito(100);

        // Assert
        saldo.TotalDebitos.Should().Be(200);
        saldo.SaldoFinal.Should().Be(800); // 1000 - 200
        saldo.UpdatedAt.Should().BeAfter(updateTimeBefore);
    }

    [Fact]
    public void OperacoesMultiplas_DeveCalcularSaldoFinalCorretamente()
    {
        // Arrange
        var saldo = new SaldoConsolidado(DateTime.Today, 1000);

        // Act
        saldo.AdicionarCredito(500);
        saldo.AdicionarCredito(200);
        saldo.AdicionarDebito(300);
        saldo.AdicionarDebito(100);

        // Assert
        saldo.TotalCreditos.Should().Be(700);
        saldo.TotalDebitos.Should().Be(400);
        saldo.SaldoFinal.Should().Be(1300); // 1000 + 700 - 400
    }

    [Fact]
    public void Data_DeveSerSempreDataSemHora()
    {
        // Arrange
        var dataComHora = new DateTime(2025, 1, 15, 14, 30, 0);

        // Act
        var saldo = new SaldoConsolidado(dataComHora);

        // Assert
        saldo.Data.Should().Be(new DateTime(2025, 1, 15, 0, 0, 0));
        saldo.Data.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RecalcularSaldoFinal_DeveFuncionarComSaldoNegativo()
    {
        // Arrange
        var saldo = new SaldoConsolidado(DateTime.Today, 100);

        // Act
        saldo.AdicionarDebito(200);

        // Assert
        saldo.SaldoFinal.Should().Be(-100);
    }
}