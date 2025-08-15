using Carrefour.CaseFlow.Lancamentos.Domain.Entities;

namespace Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;

public interface ILancamentoRepository
{
    Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lancamento>> GetAllAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lancamento>> GetByPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
    Task<Lancamento> AddAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}