using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.AdjustReserve;

public record AdjustReserveCommand(Guid ClaimId, Guid ReserveId, decimal NewAmount, string ChangeReason)
    : IRequest<ReserveComponentDto>;
