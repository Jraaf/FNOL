using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class ClaimStatusTransition : BaseEntity
{
    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public string? RequiredPermission { get; set; }
    public bool IsAllowed { get; set; } = true;
}
