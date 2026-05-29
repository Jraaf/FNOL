using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

public class GlPostingJob
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<GlPostingJob> _logger;

    public GlPostingJob(IClaimsDbContext db, IUnitOfWork uow, IDateTimeProvider clock, ILogger<GlPostingJob> logger)
    {
        _db = db; _uow = uow; _clock = clock; _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
    public async Task ExecuteAsync(Guid claimId, Guid reserveComponentId, int changeSequence, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        // Idempotency: do nothing if we've already logged this key
        var alreadyLogged = await _db.ClaimAuditLogs
            .IgnoreQueryFilters()
            .AnyAsync(a => a.ClaimId == claimId
                && a.EventType == AuditEventType.GlPostingSimulated
                && a.NewValues != null
                && a.NewValues.Contains(idempotencyKey), cancellationToken);
        if (alreadyLogged)
        {
            _logger.LogInformation("GL posting {Key} already recorded — skipping.", idempotencyKey);
            return;
        }

        var reserve = await _db.ClaimReserveComponents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == reserveComponentId, cancellationToken);
        if (reserve == null)
        {
            _logger.LogWarning("Reserve {ReserveId} not found for GL posting", reserveComponentId);
            return;
        }
        if (reserve.ChangeSequence != changeSequence)
        {
            _logger.LogInformation("Reserve {ReserveId} sequence advanced past {Seq}; skipping stale GL post.",
                reserveComponentId, changeSequence);
            return;
        }
        if (reserve.ApprovalStatus is not (ReserveApprovalStatus.AutoApproved or ReserveApprovalStatus.Approved))
        {
            _logger.LogInformation("Reserve {ReserveId} no longer approved; skipping GL post.", reserveComponentId);
            return;
        }

        var claim = await _db.Claims.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken);
        if (claim == null) return;

        var now = _clock.UtcNow;
        var summary = $"key={idempotencyKey};reserve={reserveComponentId:N};amount={reserve.CurrentAmount:F2};" +
                      $"DR Change in Outstanding Reserves={reserve.CurrentAmount:F2};" +
                      $"CR Outstanding Loss Reserves={reserve.CurrentAmount:F2}";
        claim.AddAudit(AuditEventType.GlPostingSimulated, "hangfire", now,
            newValues: summary,
            description: "Simulated GL journal entry posted.");
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Posted GL entry for reserve {ReserveId} (key {Key})", reserveComponentId, idempotencyKey);
    }
}
