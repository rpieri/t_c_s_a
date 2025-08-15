
using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Carrefour.CaseFlow.Shared.Events;

namespace Carrefour.CaseFlow.Consolidado.Service.Services;

public interface IConsolidacaoService
{
    Task ProcessarLancamentoCriado(LancamentoCriado evento);
    Task ProcessarLancamentoAtualizado(LancamentoAtualizado evento);
    Task ProcessarLancamentoRemovido(LancamentoRemovido evento);
    Task<SaldoConsolidado?> ObterSaldoPorData(DateTime data);
    Task<decimal> ObterSaldoAtual();
    Task<IEnumerable<SaldoConsolidado>> ObterSaldosPorPeriodo(DateTime inicio, DateTime fim);
}