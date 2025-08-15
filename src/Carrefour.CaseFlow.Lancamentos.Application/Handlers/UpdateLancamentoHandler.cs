using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Shared.Events;
using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class UpdateLancamentoHandler(ILancamentoRepository repository, IKafkaEventPublisher eventPublisher): IRequestHandler<UpdateLancamentoCommand, LancamentoDto?>
{
    public async Task<LancamentoDto?> Handle(UpdateLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = await repository.GetByIdAsync(request.Lancamento.Id, cancellationToken);
        if (lancamento == null)
            return null;
        
        var valorAnterior = lancamento.Valor;
        var tipoAnterior = lancamento.Tipo;
        var dataAnterior = lancamento.DataLancamento;
        
        lancamento.Atualizar(
            request.Lancamento.DataLancamento,
            request.Lancamento.Valor,
            request.Lancamento.Tipo,
            request.Lancamento.Descricao,
            request.Lancamento.Categoria
        );
        
        await repository.UpdateAsync(lancamento, cancellationToken);
        
        if (valorAnterior != lancamento.Valor || tipoAnterior != lancamento.Tipo || dataAnterior != lancamento.DataLancamento)
        {
            var eventData = new LancamentoAtualizado(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid().ToString(),
                lancamento.Id,
                lancamento.DataLancamento,
                lancamento.Valor,
                lancamento.Tipo,
                lancamento.Descricao,
                lancamento.Categoria,
                valorAnterior
            );

            await eventPublisher.PublishAsync("lancamento-events", lancamento.Id.ToString(), eventData, cancellationToken);
        }
        
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