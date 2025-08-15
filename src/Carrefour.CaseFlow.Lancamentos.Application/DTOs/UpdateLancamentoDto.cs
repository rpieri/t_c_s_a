using Carrefour.CaseFlow.Shared.Events;

namespace Carrefour.CaseFlow.Lancamentos.Application.DTOs;

public record UpdateLancamentoDto(
    Guid Id,
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao,
    string Categoria
);