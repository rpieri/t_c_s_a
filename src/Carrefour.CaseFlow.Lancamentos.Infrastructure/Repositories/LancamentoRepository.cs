using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Carrefour.CaseFlow.Lancamentos.Infrastructure.Repositories;

public class LancamentoRepository(LancamentoDbContext context): ILancamentoRepository
{
    public async Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => 
        await context.Lancamentos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<Lancamento>> GetAllAsync(int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        await context.Lancamentos
            .OrderByDescending(x => x.DataLancamento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Lancamento>> GetByPeriodoAsync(DateTime inicio, DateTime fim,
        CancellationToken cancellationToken = default) =>
        await context.Lancamentos
            .Where(x => x.DataLancamento >= inicio.Date && x.DataLancamento <= fim.Date)
            .OrderByDescending(x => x.DataLancamento)
            .ToListAsync(cancellationToken);

    public async Task<Lancamento> AddAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
    {
        context.Lancamentos.Add(lancamento);
        await context.SaveChangesAsync(cancellationToken);
        return lancamento;
    }

    public async Task UpdateAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
    {
        context.Lancamentos.Update(lancamento);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lancamento = await GetByIdAsync(id, cancellationToken);
        if (lancamento is not null)
        {
            context.Lancamentos.Remove(lancamento);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

}