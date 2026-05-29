using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Events;

public sealed record SlaBreachDetectedEvent(Guid ClaimId, string ClaimNumber, TimeSpan StaleFor) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
