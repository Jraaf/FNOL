using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public record RejectReserveCommand(Guid ClaimId, Guid ReserveId, string RejectionReason)
    : IRequest<ReserveComponentDto>;
