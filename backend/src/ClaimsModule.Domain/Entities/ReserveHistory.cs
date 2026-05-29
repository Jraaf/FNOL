using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class ReserveHistory : BaseEntity
{
    public Guid ReserveComponentId { get; set; }
    public int ChangeSequence { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
}
