using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Claims.Queries.GetClaimAudit;

public record GetClaimAuditQuery(Guid ClaimId) : IRequest<IReadOnlyCollection<ClaimAuditEntryDto>>;
