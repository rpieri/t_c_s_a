using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Carrefour.CaseFlow.Lancamentos.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LancamentosController(IMediator mediator, ILogger<LancamentosController> logger) : ControllerBase
{
    /// <summary>
    /// Cria um novo lançamento
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LancamentoDto>> Create([FromBody] CreateLancamentoDto request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Creating lancamento for {DataLancamento} with value {Valor}", 
                request.DataLancamento, request.Valor);

            var command = new CreateLancamentoCommand(request);
            var result = await mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating lancamento");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Busca lançamento por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LancamentoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetLancamentoByIdQuery(id);
            var result = await mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound();
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lancamento {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    // <summary>
    /// Lista todos os lançamentos com paginação
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LancamentoDto>>> GetAll(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = new GetAllLancamentosQuery(page, pageSize);
            var result = await mediator.Send(query, cancellationToken);
            
            return Ok(new
            {
                data = result,
                page,
                pageSize,
                hasMore = result.Count() == pageSize
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all lancamentos");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Busca lançamentos por período
    /// </summary>
    [HttpGet("periodo")]
    public async Task<ActionResult<IEnumerable<LancamentoDto>>> GetByPeriodo(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetLancamentosByPeriodoQuery(inicio, fim);
            var result = await mediator.Send(query, cancellationToken);
            
            return Ok(new
            {
                data = result,
                periodo = new { inicio, fim },
                total = result.Count()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lancamentos by periodo {Inicio} to {Fim}", inicio, fim);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LancamentoDto>> Update(Guid id, [FromBody] UpdateLancamentoDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (id != request.Id)
                return BadRequest("ID do parâmetro não coincide com ID do body");

            var command = new UpdateLancamentoCommand(request);
            var result = await mediator.Send(command, cancellationToken);
            
            if (result == null)
                return NotFound($"Lançamento com ID {id} não encontrado");
                
            logger.LogInformation("Updated lancamento {Id}", id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating lancamento {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteLancamentoCommand(id);
            var result = await mediator.Send(command, cancellationToken);
            
            if (!result)
                return NotFound($"Lançamento com ID {id} não encontrado");
                
            logger.LogInformation("Deleted lancamento {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting lancamento {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}