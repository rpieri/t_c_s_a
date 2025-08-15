using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Commands;

public record GetLancamentosByPeriodoQuery(DateTime Inicio, DateTime Fim) : IRequest<IEnumerable<LancamentoDto>>;
