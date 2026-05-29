using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Events;

public sealed record ReserveOpenedEvent(
    Guid ClaimId,
    Guid ReserveComponentId,
    ReserveComponentType ComponentType,
    decimal Amount,
    ReserveApprovalStatus ApprovalStatus,
    int ChangeSequence) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
