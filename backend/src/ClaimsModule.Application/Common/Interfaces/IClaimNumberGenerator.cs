namespace ClaimsModule.Application.Common.Interfaces;

public interface IClaimNumberGenerator
{
    Task<string> NextAsync(Guid organizationId, int year, CancellationToken cancellationToken);
}
