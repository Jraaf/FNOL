namespace ClaimsModule.Application.Common.Interfaces;

public record PolicySummary(
    Guid PolicyId,
    string PolicyNumber,
    string ClientName,
    DateTimeOffset EffectiveDate,
    DateTimeOffset ExpirationDate,
    string Status);

public record PolicyCoverage(
    string CoverageCode,
    string CoverageName,
    decimal LimitAmount,
    decimal DeductibleAmount);

public interface IPolicyService
{
    Task<IReadOnlyCollection<PolicySummary>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<PolicySummary?> GetByIdAsync(Guid policyId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PolicyCoverage>> GetCoveragesAsync(Guid policyId, CancellationToken cancellationToken);
}
