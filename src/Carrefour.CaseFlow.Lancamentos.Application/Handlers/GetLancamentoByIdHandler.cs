using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class GetLancamentoByIdHandler(ILancamentoRepository repository): IRequestHandler<GetLancamentoByIdQuery, LancamentoDto?>
{
    public async Task<LancamentoDto?> Handle(GetLancamentoByIdQuery request, CancellationToken cancellationToken)
    {
        var lancamento = await repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (lancamento == null)
            return null;

        return new LancamentoDto(
            lancamento.Id,
            lancamento.DataLancamento,
            lancamento.Valor,
            lancamento.Tipo,
            lancamento.Descricao,
            lancamento.Categoria,
            lancamento.CreatedAt,
            lancamento.UpdatedAt
        );
    }
}