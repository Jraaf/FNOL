using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

public class SlaMonitorJob
{
    public const string RecurringJobId = "claims-sla-monitor";

    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SlaMonitorJob> _logger;

    public SlaMonitorJob(IClaimsDbContext db, IUnitOfWork uow, IDateTimeProvider clock, ILogger<SlaMonitorJob> logger)
    {
        _db = db; _uow = uow; _clock = clock; _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var cutoff = now.AddHours(-48);
        var stale = await _db.Claims
            .IgnoreQueryFilters()
            .Where(c => (c.Status == ClaimStatus.Draft || c.Status == ClaimStatus.Open)
                        && c.LastTouchedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            _logger.LogInformation("SLA monitor found no stale claims.");
            return;
        }

        foreach (var claim in stale)
        {
            var staleFor = now - claim.LastTouchedAt;
            claim.MarkSlaBreached("sla-monitor", now, staleFor);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SLA monitor flagged {Count} claim(s).", stale.Count);
    }
}
