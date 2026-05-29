using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Dtos;

public record ClaimSummaryDto(
    Guid Id,
    string ClaimNumber,
    string PolicyNumber,
    string ClientName,
    DateTimeOffset LossDate,
    ClaimStatus Status,
    string? AssignedHandlerName,
    decimal ReserveTotal,
    DateTimeOffset LastTouchedAt);

public record ClaimPartyDto(
    Guid Id,
    PartyType PartyType,
    string FirstName,
    string LastName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine);

public record ClaimRiskObjectDto(
    Guid Id,
    string InsuredAssetType,
    string AssetReference,
    string? DamageDescription,
    decimal? EstimatedDamageAmount);

public record ReserveHistoryDto(
    int ChangeSequence,
    decimal PreviousAmount,
    decimal NewAmount,
    string ChangeReason,
    string ChangedBy,
    DateTimeOffset ChangedAt);

public record ReserveComponentDto(
    Guid Id,
    ReserveComponentType ComponentType,
    decimal CurrentAmount,
    ReserveApprovalStatus ApprovalStatus,
    string? RejectionReason,
    int ChangeSequence,
    DateTimeOffset? LastApprovedAt,
    string? LastApprovedBy,
    IReadOnlyCollection<ReserveHistoryDto> History);

public record ClaimDocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentType DocumentType,
    DateTimeOffset UploadedAt,
    string UploadedBy);

public record ClaimAuditEntryDto(
    Guid Id,
    string EventType,
    string? OldValues,
    string? NewValues,
    string? Description,
    string TriggeredBy,
    DateTimeOffset OccurredAt);

public record ClaimDetailDto(
    Guid Id,
    string ClaimNumber,
    string PolicyNumber,
    string ClientName,
    Guid PolicyId,
    DateTimeOffset LossDate,
    DateTimeOffset ReportedAt,
    ClaimStatus Status,
    string CauseOfLossCode,
    string LossLocation,
    string LossDescription,
    string? AssignedHandlerName,
    bool LossDateOutsidePolicyPeriod,
    bool ManagerOverrideFlag,
    IReadOnlyCollection<ClaimPartyDto> Parties,
    IReadOnlyCollection<ClaimRiskObjectDto> RiskObjects,
    IReadOnlyCollection<ReserveComponentDto> Reserves,
    IReadOnlyCollection<ClaimDocumentDto> Documents);
