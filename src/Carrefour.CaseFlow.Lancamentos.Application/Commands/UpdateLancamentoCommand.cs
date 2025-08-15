using Carrefour.CaseFlow.Lancamentos.Application.DTOs;
using MediatR;

namespace Carrefour.CaseFlow.Lancamentos.Application.Commands;

public record UpdateLancamentoCommand(UpdateLancamentoDto Lancamento) : IRequest<LancamentoDto?>;