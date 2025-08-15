using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class GetAllLancamentosHandler(ILancamentoRepository repository) : IRequestHandler<GetAllLancamentosQuery, IEnumerable<LancamentoDto>>
{
    public async Task<IEnumerable<LancamentoDto>> Handle(GetAllLancamentosQuery request, CancellationToken cancellationToken)
    {
        var lancamentos = await repository.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        
        return lancamentos.Select(l => new LancamentoDto(
            l.Id,
            l.DataLancamento,
            l.Valor,
            l.Tipo,
            l.Descricao,
            l.Categoria,
            l.CreatedAt,
            l.UpdatedAt
        ));
    }
}