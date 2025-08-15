using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Shared.Events;
using Carrefour.CaseFlow.Shared.Kafka.Abstractions;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Handlers;

public class DeleteLancamentoHandler(ILancamentoRepository repository, IKafkaEventPublisher eventPublisher): IRequestHandler<DeleteLancamentoCommand, bool>
{
    public async Task<bool> Handle(DeleteLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (lancamento == null)
            return false;
        
        var eventData = new LancamentoRemovido(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid().ToString(),
            lancamento.Id,
            lancamento.DataLancamento,
            lancamento.Valor,
            lancamento.Tipo
        );
        
        await repository.DeleteAsync(request.Id, cancellationToken);
        
        await eventPublisher.PublishAsync("lancamento-events", lancamento.Id.ToString(), eventData, cancellationToken);

        return true;
    }
}