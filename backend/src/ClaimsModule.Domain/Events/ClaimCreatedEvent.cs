using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Events;

public sealed record ClaimCreatedEvent(Guid ClaimId, string ClaimNumber, Guid PolicyId, string CreatedBy)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
