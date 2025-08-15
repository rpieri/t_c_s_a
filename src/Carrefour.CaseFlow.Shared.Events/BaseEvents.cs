namespace Carrefour.CaseFlow.Shared.Events;

public abstract record BaseEvents(Guid EventId, DateTime OcurredAt, string CorrelationId);