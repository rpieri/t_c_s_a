using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using FluentValidation;

namespace Carrefour.CaseFlow.Lancamentos.Application.Validators;

public class DeleteLancamentoValidator: AbstractValidator<DeleteLancamentoCommand>
{
    public DeleteLancamentoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID é obrigatório");
    }
}