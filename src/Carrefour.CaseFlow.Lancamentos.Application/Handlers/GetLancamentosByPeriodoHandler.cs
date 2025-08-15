using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class GetLancamentosByPeriodoHandler(ILancamentoRepository repository): IRequestHandler<GetLancamentosByPeriodoQuery, IEnumerable<LancamentoDto>>
{
    public async Task<IEnumerable<LancamentoDto>> Handle(GetLancamentosByPeriodoQuery request, CancellationToken cancellationToken)
    {
        var lancamentos = await repository.GetByPeriodoAsync(request.Inicio, request.Fim, cancellationToken);
        
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