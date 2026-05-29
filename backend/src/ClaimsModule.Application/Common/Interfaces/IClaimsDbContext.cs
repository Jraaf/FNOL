using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Common.Interfaces;

public interface IClaimsDbContext
{
    DbSet<Claim> Claims { get; }
    DbSet<LossEvent> LossEvents { get; }
    DbSet<ClaimParty> ClaimParties { get; }
    DbSet<ClaimRiskObject> ClaimRiskObjects { get; }
    DbSet<ClaimReserveComponent> ClaimReserveComponents { get; }
    DbSet<ReserveHistory> ReserveHistories { get; }
    DbSet<ClaimDocument> ClaimDocuments { get; }
    DbSet<ClaimAuditLog> ClaimAuditLogs { get; }
    DbSet<CauseOfLossCode> CauseOfLossCodes { get; }
    DbSet<ClaimStatusTransition> ClaimStatusTransitions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
