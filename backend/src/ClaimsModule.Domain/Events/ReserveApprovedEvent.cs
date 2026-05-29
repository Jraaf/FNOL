using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Events;

public sealed record ReserveApprovedEvent(
    Guid ClaimId,
    Guid ReserveComponentId,
    decimal Amount,
    int ChangeSequence,
    string ApprovedBy) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
