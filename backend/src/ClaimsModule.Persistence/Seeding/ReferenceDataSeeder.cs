using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Seeding;

public static class ReferenceDataSeeder
{
    public static readonly Guid DefaultOrganizationId = new("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(ClaimsDbContext db, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        await SeedCauseOfLossCodes(db, ct);
        await SeedStatusTransitions(db, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCauseOfLossCodes(ClaimsDbContext db, CancellationToken ct)
    {
        if (await db.CauseOfLossCodes.IgnoreQueryFilters().AnyAsync(ct)) return;
        var now = DateTimeOffset.UtcNow;
        var rows = new[]
        {
            ("COLLISION", "Vehicle Collision", "Auto"),
            ("THEFT", "Theft / Burglary", "Property"),
            ("FIRE", "Fire", "Property"),
            ("FLOOD", "Flood", "Property"),
            ("WIND", "Windstorm / Hail", "Property"),
            ("WATER", "Water Damage (non-flood)", "Property"),
            ("LIAB-BI", "Bodily Injury Liability", "Liability"),
            ("LIAB-PD", "Property Damage Liability", "Liability"),
            ("VANDALISM", "Vandalism", "Property"),
            ("MED-EXP", "Medical Expense", "Health")
        };
        foreach (var (code, desc, peril) in rows)
        {
            db.CauseOfLossCodes.Add(new CauseOfLossCode
            {
                OrganizationEntityId = DefaultOrganizationId,
                Code = code,
                Description = desc,
                PerilCategory = peril,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                UserCreated = "seed",
                UserModified = "seed"
            });
        }
    }

    private static async Task SeedStatusTransitions(ClaimsDbContext db, CancellationToken ct)
    {
        if (await db.ClaimStatusTransitions.IgnoreQueryFilters().AnyAsync(ct)) return;
        var now = DateTimeOffset.UtcNow;
        var transitions = new (ClaimStatus From, ClaimStatus To, string? Perm)[]
        {
            (ClaimStatus.Draft, ClaimStatus.Open, null),
            (ClaimStatus.Draft, ClaimStatus.Withdrawn, null),
            (ClaimStatus.Open, ClaimStatus.UnderInvestigation, null),
            (ClaimStatus.Open, ClaimStatus.Closed, null),
            (ClaimStatus.Open, ClaimStatus.Withdrawn, null),
            (ClaimStatus.Open, ClaimStatus.SlaBreached, "System"),
            (ClaimStatus.UnderInvestigation, ClaimStatus.PendingPayment, null),
            (ClaimStatus.UnderInvestigation, ClaimStatus.Closed, null),
            (ClaimStatus.PendingPayment, ClaimStatus.Closed, null),
            (ClaimStatus.Closed, ClaimStatus.Reopened, "Supervisor"),
            (ClaimStatus.Reopened, ClaimStatus.UnderInvestigation, null),
            (ClaimStatus.Reopened, ClaimStatus.PendingPayment, null),
            (ClaimStatus.SlaBreached, ClaimStatus.UnderInvestigation, null),
            (ClaimStatus.SlaBreached, ClaimStatus.Open, null)
        };
        foreach (var (from, to, perm) in transitions)
        {
            db.ClaimStatusTransitions.Add(new ClaimStatusTransition
            {
                OrganizationEntityId = DefaultOrganizationId,
                FromStatus = from,
                ToStatus = to,
                RequiredPermission = perm,
                IsAllowed = true,
                CreatedAt = now,
                UpdatedAt = now,
                UserCreated = "seed",
                UserModified = "seed"
            });
        }
    }
}
