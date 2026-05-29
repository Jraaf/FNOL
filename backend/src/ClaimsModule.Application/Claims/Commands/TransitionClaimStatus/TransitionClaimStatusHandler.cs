using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public class TransitionClaimStatusHandler : IRequestHandler<TransitionClaimStatusCommand, TransitionClaimStatusResult>
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;

    public TransitionClaimStatusHandler(IClaimsDbContext db, IUnitOfWork uow,
        ICurrentUser user, IDateTimeProvider clock)
    {
        _db = db; _uow = uow; _user = user; _clock = clock;
    }

    public async Task<TransitionClaimStatusResult> Handle(TransitionClaimStatusCommand request, CancellationToken ct)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct)
            ?? throw new NotFoundException("Claim", request.ClaimId);

        var transitions = await _db.ClaimStatusTransitions.AsNoTracking().ToListAsync(ct);

        var required = transitions
            .FirstOrDefault(t => t.FromStatus == claim.Status && t.ToStatus == request.ToStatus && t.IsAllowed)
            ?.RequiredPermission;
        if (!string.IsNullOrWhiteSpace(required) && !_user.HasRole(required))
            throw new ForbiddenException($"Transition requires role '{required}'.");

        claim.TransitionStatus(request.ToStatus, transitions, _user.UserId, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
        return new TransitionClaimStatusResult(claim.Id, claim.Status);
    }
}
