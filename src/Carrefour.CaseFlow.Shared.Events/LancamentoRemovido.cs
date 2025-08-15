namespace Carrefour.CaseFlow.Shared.Events;

public record LancamentoRemovido(
    Guid EventId,
    DateTime OcurredAt,
    string CorrelationId,
    Guid LancamentoId,
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo
) : BaseEvents(EventId, OcurredAt, CorrelationId);