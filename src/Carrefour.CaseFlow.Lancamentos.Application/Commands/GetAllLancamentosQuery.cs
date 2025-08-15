using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Commands;

public record GetAllLancamentosQuery(int Page, int PageSize) : IRequest<IEnumerable<LancamentoDto>>;