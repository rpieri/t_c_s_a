using Carrefour.CaseFlow.Shared.Events;

namespace Carrefour.CaseFlow.Lancamentos.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; private set; }
    public DateTime DataLancamento { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public string Categoria { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private Lancamento() { } 
    
    public Lancamento(DateTime dataLancamento, decimal valor, TipoLancamento tipo, string descricao, string categoria)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));
        
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        Id = Guid.NewGuid();
        DataLancamento = dataLancamento.Date;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao.Trim();
        Categoria = categoria?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Atualizar(DateTime dataLancamento, decimal valor, TipoLancamento tipo, string descricao, string categoria)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));
        
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        DataLancamento = dataLancamento.Date;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao.Trim();
        Categoria = categoria?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}