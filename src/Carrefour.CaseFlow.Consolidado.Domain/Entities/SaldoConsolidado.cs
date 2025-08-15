namespace Carrefour.CaseFlow.Consolidado.Domain.Entities;

public class SaldoConsolidado
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal TotalCreditos { get; set; }
    public decimal TotalDebitos { get; set; }
    public decimal SaldoFinal { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SaldoConsolidado() { }

    public SaldoConsolidado(DateTime data, decimal saldoInicial = 0)
    {
        Id = Guid.NewGuid();
        Data = data.Date;
        SaldoInicial = saldoInicial;
        TotalCreditos = 0;
        TotalDebitos = 0;
        SaldoFinal = saldoInicial;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdicionarCredito(decimal valor)
    {
        TotalCreditos += valor;
        RecalcularSaldoFinal();
    }

    public void AdicionarDebito(decimal valor)
    {
        TotalDebitos += valor;
        RecalcularSaldoFinal();
    }

    public void RemoverCredito(decimal valor)
    {
        TotalCreditos -= valor;
        RecalcularSaldoFinal();
    }

    public void RemoverDebito(decimal valor)
    {
        TotalDebitos -= valor;
        RecalcularSaldoFinal();
    }

    private void RecalcularSaldoFinal()
    {
        SaldoFinal = SaldoInicial + TotalCreditos - TotalDebitos;
        UpdatedAt = DateTime.UtcNow;
    }
}