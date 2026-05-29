using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Events;

public sealed record ClaimStatusChangedEvent(
    Guid ClaimId,
    ClaimStatus FromStatus,
    ClaimStatus ToStatus,
    string ChangedBy) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
