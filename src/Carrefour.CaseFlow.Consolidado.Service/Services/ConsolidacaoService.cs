using System.Text.Json;
using Carrefour.CaseFlow.Consolidado.Domain.Entities;
using Carrefour.CaseFlow.Consolidado.Service.Data;
using Carrefour.CaseFlow.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Carrefour.CaseFlow.Consolidado.Service.Services;

public class ConsolidacaoService(ConsolidadoDbContext context, IDatabase redis, ILogger<ConsolidacaoService> logger) : IConsolidacaoService
{
    public async Task ProcessarLancamentoCriado(LancamentoCriado evento)
    {
        try
        {
            logger.LogInformation("Processing LancamentoCriado for {Data}: {Tipo} {Valor}", 
                evento.DataLancamento, evento.Tipo, evento.Valor);

            var saldoConsolidado = await ObterOuCriarSaldoDiario(evento.DataLancamento);

            if (evento.Tipo == TipoLancamento.Credito)
                saldoConsolidado.AdicionarCredito(evento.Valor);
            else
                saldoConsolidado.AdicionarDebito(evento.Valor);

            context.SaldosConsolidados.Update(saldoConsolidado);
            await context.SaveChangesAsync();

            // Update cache
            await AtualizarCache(saldoConsolidado);

            logger.LogInformation("Successfully processed LancamentoCriado for {Data}", evento.DataLancamento);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing LancamentoCriado: {EventId}", evento.EventId);
            throw;
        }
    }

    public async Task ProcessarLancamentoAtualizado(LancamentoAtualizado evento)
    {
        try
        {
            logger.LogInformation("Processing LancamentoAtualizado for {Data}: {Tipo} {Valor} (was {ValorAnterior})", 
                evento.DataLancamento, evento.Tipo, evento.Valor, evento.ValorAnterior);

            var saldoConsolidado = await ObterOuCriarSaldoDiario(evento.DataLancamento);

            // Remove valor anterior
            if (evento.Tipo == TipoLancamento.Credito)
                saldoConsolidado.RemoverCredito(evento.ValorAnterior);
            else
                saldoConsolidado.RemoverDebito(evento.ValorAnterior);

            // Adiciona novo valor
            if (evento.Tipo == TipoLancamento.Credito)
                saldoConsolidado.AdicionarCredito(evento.Valor);
            else
                saldoConsolidado.AdicionarDebito(evento.Valor);

            context.SaldosConsolidados.Update(saldoConsolidado);
            await context.SaveChangesAsync();

            await AtualizarCache(saldoConsolidado);

            logger.LogInformation("Successfully processed LancamentoAtualizado for {Data}", evento.DataLancamento);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing LancamentoAtualizado: {EventId}", evento.EventId);
            throw;
        }
    }

    public async Task ProcessarLancamentoRemovido(LancamentoRemovido evento)
    {
        try
        {
            logger.LogInformation("Processing LancamentoRemovido for {Data}: {Tipo} {Valor}", 
                evento.DataLancamento, evento.Tipo, evento.Valor);

            var saldoConsolidado = await ObterOuCriarSaldoDiario(evento.DataLancamento);

            // Remove o valor
            if (evento.Tipo == TipoLancamento.Credito)
                saldoConsolidado.RemoverCredito(evento.Valor);
            else
                saldoConsolidado.RemoverDebito(evento.Valor);

            context.SaldosConsolidados.Update(saldoConsolidado);
            await context.SaveChangesAsync();

            await AtualizarCache(saldoConsolidado);

            logger.LogInformation("Successfully processed LancamentoRemovido for {Data}", evento.DataLancamento);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing LancamentoRemovido: {EventId}", evento.EventId);
            throw;
        }
    }

    public async Task<SaldoConsolidado?> ObterSaldoPorData(DateTime data)
    {
        var cacheKey = $"saldo:{data:yyyy-MM-dd}";
        
        try
        {
            var cachedSaldo = await redis.StringGetAsync(cacheKey);
            
            if (cachedSaldo.HasValue)
            {
                logger.LogDebug("Saldo found in cache for {Data}", data);
                return JsonSerializer.Deserialize<SaldoConsolidado>(cachedSaldo!);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error reading from cache for {Data}", data);
        }

        var saldo = await context.SaldosConsolidados
            .FirstOrDefaultAsync(s => s.Data.Date == data.Date);

        if (saldo != null)
        {
            await AtualizarCache(saldo);
        }

        return saldo;
    }

    public async Task<decimal> ObterSaldoAtual()
    {
        var hoje = DateTime.UtcNow;
        var saldoHoje = await ObterSaldoPorData(hoje);
        
        if (saldoHoje != null)
            return saldoHoje.SaldoFinal;

        // Se não tem saldo para hoje, busca o último saldo disponível
        var ultimoSaldo = await context.SaldosConsolidados
            .Where(s => s.Data < hoje)
            .OrderByDescending(s => s.Data)
            .FirstOrDefaultAsync();

        return ultimoSaldo?.SaldoFinal ?? 0;
    }

    public async Task<IEnumerable<SaldoConsolidado>> ObterSaldosPorPeriodo(DateTime inicio, DateTime fim)
    {
        return await context.SaldosConsolidados
            .Where(s => s.Data >= inicio.Date && s.Data <= fim.Date)
            .OrderBy(s => s.Data)
            .ToListAsync();
    }

    private async Task<SaldoConsolidado> ObterOuCriarSaldoDiario(DateTime data)
    {
        var saldo = await context.SaldosConsolidados
            .FirstOrDefaultAsync(s => s.Data.Date == data.Date);

        if (saldo == null)
        {
            // Obter saldo do dia anterior
            var saldoAnterior = await context.SaldosConsolidados
                .Where(s => s.Data < data.Date)
                .OrderByDescending(s => s.Data)
                .FirstOrDefaultAsync();

            var saldoInicial = saldoAnterior?.SaldoFinal ?? 0;
            saldo = new SaldoConsolidado(data, saldoInicial);
            
            await context.SaldosConsolidados.AddAsync(saldo);
            
            await context.SaveChangesAsync();
            
            logger.LogInformation("Created new saldo consolidado for {Data} with initial balance {SaldoInicial}", 
                data, saldoInicial);
        }

        return saldo;
    }

    private async Task AtualizarCache(SaldoConsolidado saldo)
    {
        try
        {
            var cacheKey = $"saldo:{saldo.Data:yyyy-MM-dd}";
            var serializedSaldo = JsonSerializer.Serialize(saldo, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await redis.StringSetAsync(cacheKey, serializedSaldo, TimeSpan.FromHours(24));
            
            logger.LogDebug("Updated cache for {Data}", saldo.Data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error updating cache for {Data}", saldo.Data);
        }
    }
}