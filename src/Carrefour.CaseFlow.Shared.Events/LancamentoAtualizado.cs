namespace Carrefour.CaseFlow.Shared.Events;

public record LancamentoAtualizado(
    Guid EventId,
    DateTime OcurredAt,
    string CorrelationId,
    Guid LancamentoId,
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao,
    string Categoria,
    decimal ValorAnterior
) : BaseEvents(EventId, OcurredAt, CorrelationId);