using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class ClaimAuditLog : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string? Description { get; set; }
}
