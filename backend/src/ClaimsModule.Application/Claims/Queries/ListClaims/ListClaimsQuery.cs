using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Queries.ListClaims;

public record ListClaimsQuery(
    ClaimStatus? Status,
    DateTimeOffset? LossDateFrom,
    DateTimeOffset? LossDateTo,
    string? AssignedHandlerUserId,
    string? CauseOfLossCode,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<ClaimSummaryDto>>;
