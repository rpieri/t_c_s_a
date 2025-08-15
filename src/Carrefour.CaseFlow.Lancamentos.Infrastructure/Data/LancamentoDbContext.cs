using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Carrefour.CaseFlow.Lancamentos.Infrastructure.Data;

public class LancamentoDbContext : DbContext
{
    public LancamentoDbContext(DbContextOptions<LancamentoDbContext> options) : base(options) { }
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new LancamentoConfiguration());
    }
}