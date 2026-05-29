using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Events;

public sealed record ReserveAdjustedEvent(
    Guid ClaimId,
    Guid ReserveComponentId,
    decimal PreviousAmount,
    decimal NewAmount,
    ReserveApprovalStatus NewApprovalStatus,
    int ChangeSequence,
    string ChangeReason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
