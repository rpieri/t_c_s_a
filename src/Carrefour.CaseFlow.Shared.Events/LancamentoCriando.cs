namespace Carrefour.CaseFlow.Shared.Events;

public record LancamentoCriado(
    Guid EventId, 
    DateTime OcurredAt, 
    string CorrelationId, 
    Guid LancamentoId,
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao,
    string Categoria
    ): BaseEvents(EventId, OcurredAt, CorrelationId);