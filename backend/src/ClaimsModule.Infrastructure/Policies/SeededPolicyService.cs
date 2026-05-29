using ClaimsModule.Application.Common.Interfaces;

namespace ClaimsModule.Infrastructure.Policies;

public class SeededPolicyService : IPolicyService
{
    private static readonly Guid Policy1 = new("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid Policy2 = new("aaaaaaaa-2222-2222-2222-222222222222");
    private static readonly Guid Policy3 = new("aaaaaaaa-3333-3333-3333-333333333333");
    private static readonly Guid Policy4 = new("aaaaaaaa-4444-4444-4444-444444444444");

    private static readonly List<PolicySummary> Policies = new()
    {
        new(Policy1, "POL-2026-0000001", "Acme Logistics Ltd",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero), "Active"),
        new(Policy2, "POL-2026-0000002", "Northwind Trading Co",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 28, 23, 59, 59, TimeSpan.Zero), "Active"),
        new(Policy3, "POL-2025-0000150", "Globex Manufacturing",
            new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero), "Active"),
        new(Policy4, "POL-2024-0000099", "Initech Software Inc",
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero), "Expired")
    };

    private static readonly Dictionary<Guid, List<PolicyCoverage>> Coverages = new()
    {
        [Policy1] = new()
        {
            new("CARGO", "Cargo Loss", 500_000m, 1_000m),
            new("AUTO-LIAB", "Auto Liability", 1_000_000m, 2_500m),
            new("AUTO-PD", "Auto Physical Damage", 250_000m, 1_000m)
        },
        [Policy2] = new()
        {
            new("PROP", "Commercial Property", 5_000_000m, 5_000m),
            new("GEN-LIAB", "General Liability", 2_000_000m, 2_500m),
            new("EQUIP", "Equipment Breakdown", 1_000_000m, 1_000m)
        },
        [Policy3] = new()
        {
            new("PROP", "Commercial Property", 10_000_000m, 10_000m),
            new("PROD-LIAB", "Product Liability", 5_000_000m, 25_000m)
        },
        [Policy4] = new()
        {
            new("CYBER", "Cyber Liability", 1_000_000m, 5_000m)
        }
    };

    public Task<IReadOnlyCollection<PolicySummary>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        IEnumerable<PolicySummary> results = Policies;
        if (!string.IsNullOrWhiteSpace(query))
        {
            results = Policies.Where(p =>
                p.PolicyNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                || p.ClientName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyCollection<PolicySummary>>(results.ToList());
    }

    public Task<PolicySummary?> GetByIdAsync(Guid policyId, CancellationToken cancellationToken)
        => Task.FromResult(Policies.FirstOrDefault(p => p.PolicyId == policyId));

    public Task<IReadOnlyCollection<PolicyCoverage>> GetCoveragesAsync(Guid policyId, CancellationToken cancellationToken)
    {
        if (Coverages.TryGetValue(policyId, out var c))
            return Task.FromResult<IReadOnlyCollection<PolicyCoverage>>(c);
        return Task.FromResult<IReadOnlyCollection<PolicyCoverage>>(Array.Empty<PolicyCoverage>());
    }
}
