using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Events;

public sealed record ReserveRejectedEvent(
    Guid ClaimId,
    Guid ReserveComponentId,
    string RejectionReason,
    string RejectedBy) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
