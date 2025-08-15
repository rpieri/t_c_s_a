using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using FluentValidation;

namespace Carrefour.CaseFlow.Lancamentos.Application.Validators;

public class GetLancamentosByPeriodoValidator: AbstractValidator<GetLancamentosByPeriodoQuery>
{
    public GetLancamentosByPeriodoValidator()
    {
        RuleFor(x => x.Inicio)
            .NotEmpty()
            .WithMessage("Data de início é obrigatória");

        RuleFor(x => x.Fim)
            .NotEmpty()
            .WithMessage("Data de fim é obrigatória")
            .GreaterThanOrEqualTo(x => x.Inicio)
            .WithMessage("Data de fim deve ser maior ou igual à data de início");

        RuleFor(x => x)
            .Must(x => (x.Fim - x.Inicio).TotalDays <= 365)
            .WithMessage("Período não pode ser maior que 365 dias");
    }
}