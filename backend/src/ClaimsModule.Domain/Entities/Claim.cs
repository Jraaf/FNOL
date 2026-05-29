using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Events;

namespace ClaimsModule.Domain.Entities;

public class Claim : AggregateRoot
{
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClaimType { get; set; } = "FirstParty";
    public string CauseOfLossCode { get; set; } = string.Empty;
    public DateTimeOffset LossDate { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public string? AssignedHandlerUserId { get; set; }
    public string? AssignedHandlerName { get; set; }
    public bool LossDateOutsidePolicyPeriod { get; set; }
    public bool ManagerOverrideFlag { get; set; }
    public DateTimeOffset LastTouchedAt { get; set; }

    public LossEvent LossEvent { get; set; } = default!;
    public List<ClaimParty> Parties { get; set; } = new();
    public List<ClaimRiskObject> RiskObjects { get; set; } = new();
    public List<ClaimReserveComponent> Reserves { get; set; } = new();
    public List<ClaimDocument> Documents { get; set; } = new();
    public List<ClaimAuditLog> AuditLog { get; set; } = new();

    public const decimal TotalReserveOverrideThreshold = 10_000_000m;

    public static Claim CreateFnol(
        string claimNumber,
        Guid organizationId,
        Guid policyId,
        string policyNumber,
        string clientName,
        DateTimeOffset lossDate,
        string causeOfLossCode,
        string lossLocation,
        string lossDescription,
        string userId,
        DateTimeOffset now,
        bool lossDateOutsidePolicyPeriod)
    {
        if (lossDate > now)
            throw new DomainException("BR-C-01", "Loss date must not be in the future.");

        var claim = new Claim
        {
            ClaimNumber = claimNumber,
            OrganizationEntityId = organizationId,
            PolicyId = policyId,
            PolicyNumber = policyNumber,
            ClientName = clientName,
            LossDate = lossDate,
            ReportedAt = now,
            CauseOfLossCode = causeOfLossCode,
            Status = ClaimStatus.Draft,
            LossDateOutsidePolicyPeriod = lossDateOutsidePolicyPeriod,
            LastTouchedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        };

        claim.LossEvent = new LossEvent
        {
            OrganizationEntityId = organizationId,
            ClaimId = claim.Id,
            LossDate = lossDate,
            LossLocation = lossLocation,
            LossDescription = lossDescription,
            CauseOfLossCode = causeOfLossCode,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        };

        claim.AddAudit(AuditEventType.ClaimCreated, userId, now, description: $"Claim {claimNumber} created");
        if (lossDateOutsidePolicyPeriod)
        {
            claim.AddAudit(AuditEventType.LossDateOutsidePolicyPeriod, userId, now,
                description: "Loss date is outside policy effective/expiration window (BR-C-02 warning).");
        }

        claim.RaiseDomainEvent(new ClaimCreatedEvent(claim.Id, claimNumber, policyId, userId));
        return claim;
    }

    public void AddParty(ClaimParty party, string userId, DateTimeOffset now)
    {
        party.ClaimId = Id;
        party.OrganizationEntityId = OrganizationEntityId;
        party.CreatedAt = now;
        party.UpdatedAt = now;
        party.UserCreated = userId;
        party.UserModified = userId;
        Parties.Add(party);
        TouchedNow(now, userId);
    }

    public void AddRiskObject(ClaimRiskObject risk, string userId, DateTimeOffset now)
    {
        risk.ClaimId = Id;
        risk.OrganizationEntityId = OrganizationEntityId;
        risk.CreatedAt = now;
        risk.UpdatedAt = now;
        risk.UserCreated = userId;
        risk.UserModified = userId;
        RiskObjects.Add(risk);
        TouchedNow(now, userId);
    }

    public void EnsureHasClaimant()
    {
        if (!Parties.Any(p => p.PartyType == PartyType.Claimant))
            throw new DomainException("BR-C-03", "Claim must have at least one Claimant party.");
    }

    public void TransitionStatus(ClaimStatus toStatus, IReadOnlyCollection<ClaimStatusTransition> allowedTransitions,
        string userId, DateTimeOffset now)
    {
        var from = Status;
        if (from == toStatus) return;

        var allowed = allowedTransitions.Any(t => t.FromStatus == from && t.ToStatus == toStatus && t.IsAllowed);
        if (!allowed)
            throw new DomainException("BR-C-06",
                $"Transition {from} -> {toStatus} is not allowed by the workflow definition.");

        Status = toStatus;
        TouchedNow(now, userId);
        AddAudit(AuditEventType.ClaimStatusChanged, userId, now,
            oldValues: from.ToString(), newValues: toStatus.ToString(),
            description: $"Status changed {from} -> {toStatus}");
        RaiseDomainEvent(new ClaimStatusChangedEvent(Id, from, toStatus, userId));
    }

    public ClaimReserveComponent OpenReserve(
        ReserveComponentType componentType, decimal amount, string userId, DateTimeOffset now)
    {
        var reserve = ClaimReserveComponent.Open(Id, OrganizationEntityId, componentType, amount, userId, now);
        Reserves.Add(reserve);

        var eventType = reserve.ApprovalStatus == ReserveApprovalStatus.AutoApproved
            ? AuditEventType.ReserveAutoApproved
            : AuditEventType.ReserveOpened;
        AddAudit(eventType, userId, now,
            newValues: $"{componentType}={amount:F2} ({reserve.ApprovalStatus})",
            description: $"Reserve {componentType} opened at {amount:F2}");

        EnforceTotalReserveLimit(userId, now);
        TouchedNow(now, userId);
        return reserve;
    }

    public void AdjustReserve(Guid reserveId, decimal newAmount, string changeReason, string userId, DateTimeOffset now)
    {
        var reserve = Reserves.FirstOrDefault(r => r.Id == reserveId)
            ?? throw new DomainException("RESERVE_NOT_FOUND", "Reserve component not found on this claim.");
        var previous = reserve.CurrentAmount;
        reserve.Adjust(newAmount, changeReason, userId, now);
        AddAudit(AuditEventType.ReserveAdjusted, userId, now,
            oldValues: previous.ToString("F2"), newValues: newAmount.ToString("F2"),
            description: $"Reserve {reserve.ComponentType} adjusted: {previous:F2} -> {newAmount:F2}");
        EnforceTotalReserveLimit(userId, now);
        TouchedNow(now, userId);
    }

    public ClaimReserveComponent ApproveReserve(Guid reserveId, string approverRole, string userId, DateTimeOffset now)
    {
        var reserve = Reserves.FirstOrDefault(r => r.Id == reserveId)
            ?? throw new DomainException("RESERVE_NOT_FOUND", "Reserve component not found on this claim.");
        reserve.Approve(approverRole, userId, now);
        AddAudit(AuditEventType.ReserveApproved, userId, now,
            newValues: reserve.CurrentAmount.ToString("F2"),
            description: $"Reserve {reserve.ComponentType} approved at {reserve.CurrentAmount:F2}");
        // BR-R-07 — re-check the cumulative cap whenever a reserve becomes approved.
        EnforceTotalReserveLimit(userId, now);
        TouchedNow(now, userId);
        return reserve;
    }

    public ClaimReserveComponent RejectReserve(Guid reserveId, string rejectionReason, string approverRole,
        string userId, DateTimeOffset now)
    {
        var reserve = Reserves.FirstOrDefault(r => r.Id == reserveId)
            ?? throw new DomainException("RESERVE_NOT_FOUND", "Reserve component not found on this claim.");
        reserve.Reject(rejectionReason, approverRole, userId, now);
        AddAudit(AuditEventType.ReserveRejected, userId, now,
            description: $"Reserve {reserve.ComponentType} rejected: {rejectionReason}");
        TouchedNow(now, userId);
        return reserve;
    }

    public ClaimDocument AddDocument(string fileName, string blobReference, string contentType, long size,
        DocumentType docType, string userId, DateTimeOffset now)
    {
        var doc = new ClaimDocument
        {
            OrganizationEntityId = OrganizationEntityId,
            ClaimId = Id,
            FileName = fileName,
            BlobReference = blobReference,
            ContentType = contentType,
            SizeBytes = size,
            DocumentType = docType,
            UploadedAt = now,
            UploadedBy = userId,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        };
        Documents.Add(doc);
        AddAudit(AuditEventType.DocumentUploaded, userId, now,
            newValues: fileName, description: $"Document uploaded: {fileName}");
        RaiseDomainEvent(new DocumentUploadedEvent(Id, doc.Id, fileName, userId));
        TouchedNow(now, userId);
        return doc;
    }

    public void MarkSlaBreached(string actorUserId, DateTimeOffset now, TimeSpan staleFor)
    {
        if (Status is not (ClaimStatus.Draft or ClaimStatus.Open)) return;
        if (Status == ClaimStatus.SlaBreached) return;
        var previous = Status;
        Status = ClaimStatus.SlaBreached;
        AddAudit(AuditEventType.SlaBreachDetected, actorUserId, now,
            oldValues: previous.ToString(), newValues: Status.ToString(),
            description: $"SLA breach detected after {staleFor.TotalHours:F1}h without update.");
        TouchedNow(now, actorUserId);
        RaiseDomainEvent(new SlaBreachDetectedEvent(Id, ClaimNumber, staleFor));
    }

    public void AddAudit(string eventType, string userId, DateTimeOffset now,
        string? oldValues = null, string? newValues = null, string? description = null)
    {
        AuditLog.Add(new ClaimAuditLog
        {
            OrganizationEntityId = OrganizationEntityId,
            ClaimId = Id,
            EventType = eventType,
            OldValues = oldValues,
            NewValues = newValues,
            TriggeredBy = userId,
            OccurredAt = now,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        });
    }

    private void TouchedNow(DateTimeOffset now, string userId)
    {
        LastTouchedAt = now;
        UpdatedAt = now;
        UserModified = userId;
    }

    private void EnforceTotalReserveLimit(string userId, DateTimeOffset now)
    {
        var approvedTotal = Reserves
            .Where(r => r.ApprovalStatus is ReserveApprovalStatus.AutoApproved or ReserveApprovalStatus.Approved)
            .Sum(r => r.CurrentAmount);
        if (approvedTotal > TotalReserveOverrideThreshold && !ManagerOverrideFlag)
        {
            AddAudit(AuditEventType.ManagerOverrideRequired, userId, now,
                newValues: approvedTotal.ToString("F2"),
                description: $"BR-R-07 warning: approved reserves total {approvedTotal:F2} exceeds 10M without manager override.");
        }
    }
}
