using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Application.Validators;
using Carrefour.CaseFlow.Shared.Events;
using FluentAssertions;

namespace Carrefour.CaseFlow.Consolidado.Tests.Validators;

public class ValidatorsTests
{
    [Fact]
    public void CreateLancamentoValidator_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100.50m,
            TipoLancamento.Credito,
            "Descrição válida",
            "Categoria válida"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateLancamentoValidator_ComDataVazia_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var dto = new CreateLancamentoDto(
            default(DateTime),
            100m,
            TipoLancamento.Credito,
            "Descrição",
            "Categoria"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Data do lançamento é obrigatória");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public void CreateLancamentoValidator_ComValorInvalido_DeveFalharNaValidacao(decimal valorInvalido)
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            valorInvalido,
            TipoLancamento.Credito,
            "Descrição",
            "Categoria"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Valor deve ser maior que zero");
    }



    [Fact]
    public void CreateLancamentoValidator_ComDescricaoMuitoLonga_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var descricaoLonga = new string('A', 501); // 501 caracteres
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            descricaoLonga,
            "Categoria"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Descrição é obrigatória e deve ter no máximo 500 caracteres");
    }

    [Fact]
    public void CreateLancamentoValidator_ComCategoriaMuitoLonga_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var categoriaLonga = new string('B', 101); // 101 caracteres
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Descrição",
            categoriaLonga
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Categoria deve ter no máximo 100 caracteres");
    }

    [Fact]
    public void UpdateLancamentoValidator_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var validator = new UpdateLancamentoValidator();
        var dto = new UpdateLancamentoDto(
            Guid.NewGuid(),
            DateTime.Today,
            200.75m,
            TipoLancamento.Debito,
            "Descrição atualizada",
            "Nova categoria"
        );
        var command = new UpdateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UpdateLancamentoValidator_ComIdVazio_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new UpdateLancamentoValidator();
        var dto = new UpdateLancamentoDto(
            Guid.Empty,
            DateTime.Today,
            100m,
            TipoLancamento.Credito,
            "Descrição",
            "Categoria"
        );
        var command = new UpdateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID é obrigatório");
    }

    [Fact]
    public void DeleteLancamentoValidator_ComIdValido_DevePassarNaValidacao()
    {
        // Arrange
        var validator = new DeleteLancamentoValidator();
        var command = new DeleteLancamentoCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void DeleteLancamentoValidator_ComIdVazio_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new DeleteLancamentoValidator();
        var command = new DeleteLancamentoCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID é obrigatório");
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComPeriodoValido_DevePassarNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var query = new GetLancamentosByPeriodoQuery(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComInicioVazio_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var query = new GetLancamentosByPeriodoQuery(
            default(DateTime),
            new DateTime(2025, 1, 31)
        );

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Data de início é obrigatória");
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComFimVazio_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var query = new GetLancamentosByPeriodoQuery(
            new DateTime(2025, 1, 1),
            default(DateTime)
        );

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Data de fim é obrigatória");
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComFimMenorQueInicio_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var query = new GetLancamentosByPeriodoQuery(
            new DateTime(2025, 1, 31),
            new DateTime(2025, 1, 1)
        );

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Data de fim deve ser maior ou igual à data de início");
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComPeriodoMaiorQue365Dias_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var query = new GetLancamentosByPeriodoQuery(
            new DateTime(2025, 1, 1),
            new DateTime(2026, 1, 2) // Mais de 365 dias
        );

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Período não pode ser maior que 365 dias");
    }

    [Fact]
    public void GetLancamentosByPeriodoValidator_ComExatos365Dias_DevePassarNaValidacao()
    {
        // Arrange
        var validator = new GetLancamentosByPeriodoValidator();
        var inicio = new DateTime(2025, 1, 1);
        var fim = inicio.AddDays(365); // Exatamente 365 dias
        var query = new GetLancamentosByPeriodoQuery(inicio, fim);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public void CreateLancamentoValidator_ComTiposValidos_DevePassarNaValidacao(TipoLancamento tipo)
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100m,
            tipo,
            "Descrição",
            "Categoria"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateLancamentoValidator_ComTipoInvalido_DeveFalharNaValidacao()
    {
        // Arrange
        var validator = new CreateLancamentoValidator();
        var dto = new CreateLancamentoDto(
            DateTime.Today,
            100m,
            (TipoLancamento)999, // Valor inválido
            "Descrição",
            "Categoria"
        );
        var command = new CreateLancamentoCommand(dto);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Tipo de lançamento inválido");
    }
}