using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

public record CreateClaimPartyInput(
    PartyType PartyType,
    string FirstName,
    string LastName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine);

public record CreateClaimRiskObjectInput(
    string InsuredAssetType,
    string AssetReference,
    string? DamageDescription,
    decimal? EstimatedDamageAmount);

public record CreateClaimInitialReserveInput(
    ReserveComponentType ComponentType,
    decimal Amount);

public record CreateClaimCommand(
    Guid PolicyId,
    DateTimeOffset LossDate,
    string CauseOfLossCode,
    string LossLocation,
    string LossDescription,
    IReadOnlyCollection<CreateClaimPartyInput> Parties,
    IReadOnlyCollection<CreateClaimRiskObjectInput> RiskObjects,
    CreateClaimInitialReserveInput? InitialReserve) : IRequest<CreateClaimResult>;

public record CreateClaimResult(Guid ClaimId, string ClaimNumber, ClaimStatus Status, bool LossDateOutsidePolicyPeriod);
