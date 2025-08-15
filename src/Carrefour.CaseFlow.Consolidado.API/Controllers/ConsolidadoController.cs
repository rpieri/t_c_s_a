using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Carrefour.CaseFlow.Consolidado.API.Controllers;

public class ConsolidadoController(IConsolidacaoService consolidacaoService, ILogger<ConsolidadoController> logger): ControllerBase
{
    // <summary>
    /// Obtém o saldo atual (hoje)
    /// </summary>
    [HttpGet("saldo/atual")]
    public async Task<ActionResult<object>> GetSaldoAtual()
    {
        try
        {
            var saldo = await consolidacaoService.ObterSaldoAtual();
            return Ok(new 
            { 
                saldoAtual = saldo, 
                data = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current balance");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("saldo/data/{data:datetime}")]
    public async Task<ActionResult<SaldoConsolidado>> GetSaldoPorData(DateTime data)
    {
        try
        {
            var saldo = await consolidacaoService.ObterSaldoPorData(data);
            
            if (saldo == null)
                return NotFound(new 
                { 
                    message = $"Saldo não encontrado para a data {data:yyyy-MM-dd}",
                    data = data.ToString("yyyy-MM-dd")
                });
                
            return Ok(saldo);
        }
        catch (Exception ex)
        { logger.LogError(ex, "Error getting balance for date {Data}", data);
            return StatusCode(500, "Internal server error");
        }
    }
    
    // <summary>
    /// Obtém relatório de saldos consolidados por período
    /// </summary>
    [HttpGet("relatorio/periodo")]
    public async Task<ActionResult<object>> GetRelatorioPorPeriodo(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim)
    {
        try
        {
            if (inicio > fim)
                return BadRequest(new { message = "Data de início deve ser menor ou igual à data de fim" });

            if ((fim - inicio).TotalDays > 365)
                return BadRequest(new { message = "Período não pode ser maior que 365 dias" });

            var saldos = await consolidacaoService.ObterSaldosPorPeriodo(inicio, fim);

            var relatorio = new
            {
                periodo = new
                {
                    inicio = inicio.ToString("yyyy-MM-dd"),
                    fim = fim.ToString("yyyy-MM-dd"),
                    dias = (fim - inicio).TotalDays + 1
                },
                resumo = new
                {
                    totalDias = saldos.Count(),
                    saldoInicial = saldos.FirstOrDefault()?.SaldoInicial ?? 0,
                    saldoFinal = saldos.LastOrDefault()?.SaldoFinal ?? 0,
                    totalCreditos = saldos.Sum(s => s.TotalCreditos),
                    totalDebitos = saldos.Sum(s => s.TotalDebitos)
                },
                saldos = saldos.Select(s => new
                {
                    data = s.Data.ToString("yyyy-MM-dd"),
                    saldoInicial = s.SaldoInicial,
                    totalCreditos = s.TotalCreditos,
                    totalDebitos = s.TotalDebitos,
                    saldoFinal = s.SaldoFinal,
                    updatedAt = s.UpdatedAt
                }).ToList()
            };

            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting balance report for period {Inicio} to {Fim}", inicio, fim);
            return StatusCode(500, "Internal server error");
        }
    }
}