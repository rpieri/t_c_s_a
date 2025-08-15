using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Commands;

public record DeleteLancamentoCommand(Guid Id) : IRequest<bool>;