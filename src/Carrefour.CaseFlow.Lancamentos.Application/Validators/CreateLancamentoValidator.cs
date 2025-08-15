using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using FluentValidation;

namespace Carrefour.CaseFlow.Lancamentos.Application.Validators;

public class CreateLancamentoValidator: AbstractValidator<CreateLancamentoCommand>
{
    public CreateLancamentoValidator()
    {
        RuleFor(x => x.Lancamento.DataLancamento)
            .NotEmpty()
            .WithMessage("Data do lançamento é obrigatória");

        RuleFor(x => x.Lancamento.Valor)
            .GreaterThan(0)
            .WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.Lancamento.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de lançamento inválido");

        RuleFor(x => x.Lancamento.Descricao)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Descrição é obrigatória e deve ter no máximo 500 caracteres");

        RuleFor(x => x.Lancamento.Categoria)
            .MaximumLength(100)
            .WithMessage("Categoria deve ter no máximo 100 caracteres");
    }
}