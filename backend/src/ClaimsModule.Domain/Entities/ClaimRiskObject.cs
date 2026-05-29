using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class ClaimRiskObject : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string InsuredAssetType { get; set; } = string.Empty;
    public string AssetReference { get; set; } = string.Empty;
    public string? DamageDescription { get; set; }
    public decimal? EstimatedDamageAmount { get; set; }
}
