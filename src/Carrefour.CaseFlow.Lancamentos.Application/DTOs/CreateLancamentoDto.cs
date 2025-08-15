using Carrefour.CaseFlow.Shared.Events;

namespace Carrefour.CaseFlow.Lancamentos.Application.DTOs;

public record CreateLancamentoDto(
    DateTime DataLancamento,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao,
    string Categoria
);