using Carrefour.CaseFlow.Shared.Events;

namespace Carrefour.CaseFlow.Lancamentos.Application.DTOs;

public record LancamentoDto(
    Guid Id,
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao,
    string Categoria,
    DateTime CreatedAt,
    DateTime UpdatedAt
);