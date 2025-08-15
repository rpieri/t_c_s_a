
using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Carrefour.CaseFlow.Consolidado.Service.Data;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options)
    {
    }

    public DbSet<SaldoConsolidado> SaldosConsolidados => Set<SaldoConsolidado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SaldoConsolidado>(entity =>
        {
            entity.ToTable("saldos_consolidados");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Data)
                .HasColumnName("data")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.SaldoInicial)
                .HasColumnName("saldo_inicial")
                .HasPrecision(15, 2);

            entity.Property(e => e.TotalCreditos)
                .HasColumnName("total_creditos")
                .HasPrecision(15, 2);

            entity.Property(e => e.TotalDebitos)
                .HasColumnName("total_debitos")
                .HasPrecision(15, 2);

            entity.Property(e => e.SaldoFinal)
                .HasColumnName("saldo_final")
                .HasPrecision(15, 2);

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(e => e.Data)
                .IsUnique()
                .HasDatabaseName("ix_saldos_consolidados_data");
        });
    }
}