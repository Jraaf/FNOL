using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class LossEvent : BaseEntity
{
    public Guid ClaimId { get; set; }
    public DateTimeOffset LossDate { get; set; }
    public string LossLocation { get; set; } = string.Empty;
    public string LossDescription { get; set; } = string.Empty;
    public string CauseOfLossCode { get; set; } = string.Empty;
}
