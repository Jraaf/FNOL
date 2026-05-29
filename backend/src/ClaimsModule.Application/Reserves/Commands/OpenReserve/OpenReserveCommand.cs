using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.OpenReserve;

public record OpenReserveCommand(Guid ClaimId, ReserveComponentType ComponentType, decimal Amount, string? Notes)
    : IRequest<ReserveComponentDto>;
