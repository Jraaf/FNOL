using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reference.Queries.GetClaimStatuses;

public record ClaimStatusTransitionDto(ClaimStatus From, ClaimStatus To, string? RequiredPermission);

public record ClaimStatusDescriptor(ClaimStatus Status, string Name, IReadOnlyCollection<ClaimStatusTransitionDto> AllowedTransitions);

public record GetClaimStatusesQuery() : IRequest<IReadOnlyCollection<ClaimStatusDescriptor>>;

public class GetClaimStatusesHandler : IRequestHandler<GetClaimStatusesQuery, IReadOnlyCollection<ClaimStatusDescriptor>>
{
    private readonly IClaimsDbContext _db;
    public GetClaimStatusesHandler(IClaimsDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<ClaimStatusDescriptor>> Handle(GetClaimStatusesQuery request, CancellationToken ct)
    {
        var transitions = await _db.ClaimStatusTransitions.AsNoTracking()
            .Where(t => t.IsAllowed)
            .ToListAsync(ct);
        var lookup = transitions.GroupBy(t => t.FromStatus)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<ClaimStatusTransitionDto>)g
                .Select(t => new ClaimStatusTransitionDto(t.FromStatus, t.ToStatus, t.RequiredPermission))
                .ToList());

        return Enum.GetValues<ClaimStatus>()
            .Select(s => new ClaimStatusDescriptor(
                s,
                s.ToString(),
                lookup.TryGetValue(s, out var ts) ? ts : Array.Empty<ClaimStatusTransitionDto>()))
            .ToList();
    }
}
