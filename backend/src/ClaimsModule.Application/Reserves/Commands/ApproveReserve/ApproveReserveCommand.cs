using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.ApproveReserve;

public record ApproveReserveCommand(Guid ClaimId, Guid ReserveId, string? Comment)
    : IRequest<ReserveComponentDto>;
