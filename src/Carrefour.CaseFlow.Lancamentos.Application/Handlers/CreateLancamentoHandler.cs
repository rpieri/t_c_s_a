using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Shared.Events;
using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class CreateLancamentoHandler(ILancamentoRepository repository, IKafkaEventPublisher eventPublisher): IRequestHandler<CreateLancamentoCommand, LancamentoDto>
{
    public async Task<LancamentoDto> Handle(CreateLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = new Lancamento(
            request.Lancamento.DataLancamento,
            request.Lancamento.Valor,
            request.Lancamento.Tipo,
            request.Lancamento.Descricao,
            request.Lancamento.Categoria
        );
        
        var savedLancamento = await repository.AddAsync(lancamento, cancellationToken);
        
        var eventData = new LancamentoCriado(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid().ToString(),
            savedLancamento.Id,
            savedLancamento.DataLancamento,
            savedLancamento.Valor,
            savedLancamento.Tipo,
            savedLancamento.Descricao,
            savedLancamento.Categoria
        );
        
        await eventPublisher.PublishAsync("lancamento-events", savedLancamento.Id.ToString(), eventData, cancellationToken);
        
        return new LancamentoDto(
            savedLancamento.Id,
            savedLancamento.DataLancamento,
            savedLancamento.Valor,
            savedLancamento.Tipo,
            savedLancamento.Descricao,
            savedLancamento.Categoria,
            savedLancamento.CreatedAt,
            savedLancamento.UpdatedAt
        );
    }
}