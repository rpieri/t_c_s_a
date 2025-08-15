using Carrefour.CaseFlow.Lancamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carrefour.CaseFlow.Lancamentos.Infrastructure.Configurations;

public class LancamentoConfiguration: IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();
            
        builder.Property(x => x.DataLancamento)
            .HasColumnName("data_lancamento")
            .IsRequired();
            
        builder.Property(x => x.Valor)
            .HasColumnName("valor")
            .HasPrecision(15, 2)
            .IsRequired();
            
        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(x => x.Categoria)
            .HasColumnName("categoria")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.DataLancamento)
            .HasDatabaseName("ix_lancamentos_data_lancamento");
            
        builder.HasIndex(x => x.Tipo)
            .HasDatabaseName("ix_lancamentos_tipo");
    }
}