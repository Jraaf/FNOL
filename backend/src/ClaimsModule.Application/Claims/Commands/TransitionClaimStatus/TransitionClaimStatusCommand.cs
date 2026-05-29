using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public record TransitionClaimStatusCommand(Guid ClaimId, ClaimStatus ToStatus, string? Reason)
    : IRequest<TransitionClaimStatusResult>;

public record TransitionClaimStatusResult(Guid ClaimId, ClaimStatus Status);
