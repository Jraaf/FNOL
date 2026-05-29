using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Events;

namespace ClaimsModule.Domain.Entities;

public class ClaimReserveComponent : AggregateRoot
{
    public Guid ClaimId { get; set; }
    public ReserveComponentType ComponentType { get; set; }
    public decimal CurrentAmount { get; set; }
    public ReserveApprovalStatus ApprovalStatus { get; set; }
    public bool ManagerOverrideFlag { get; set; }
    public int ChangeSequence { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? LastApprovedAt { get; set; }
    public string? LastApprovedBy { get; set; }

    public List<ReserveHistory> History { get; set; } = new();

    public const decimal AutoApproveThreshold = 10_000m;
    public const decimal SupervisorThreshold = 100_000m;

    public static ClaimReserveComponent Open(
        Guid claimId,
        Guid organizationId,
        ReserveComponentType componentType,
        decimal amount,
        string userId,
        DateTimeOffset now)
    {
        if (amount <= 0)
            throw new DomainException("BR-R-01", "Reserve amount must be greater than zero.");

        var approval = ResolveApprovalStatus(amount);
        var reserve = new ClaimReserveComponent
        {
            ClaimId = claimId,
            OrganizationEntityId = organizationId,
            ComponentType = componentType,
            CurrentAmount = amount,
            ApprovalStatus = approval,
            ChangeSequence = 1,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        };

        if (approval == ReserveApprovalStatus.AutoApproved)
        {
            reserve.LastApprovedAt = now;
            reserve.LastApprovedBy = "SYSTEM";
        }

        reserve.History.Add(new ReserveHistory
        {
            OrganizationEntityId = organizationId,
            ReserveComponentId = reserve.Id,
            ChangeSequence = 1,
            PreviousAmount = 0m,
            NewAmount = amount,
            ChangeReason = "Initial reserve opening",
            ChangedBy = userId,
            ChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        });

        reserve.RaiseDomainEvent(new ReserveOpenedEvent(
            claimId, reserve.Id, componentType, amount, approval, reserve.ChangeSequence));
        return reserve;
    }

    public void Adjust(decimal newAmount, string changeReason, string userId, DateTimeOffset now)
    {
        if (newAmount <= 0)
            throw new DomainException("BR-R-01", "Reserve amount must be greater than zero.");
        if (ApprovalStatus == ReserveApprovalStatus.Rejected)
            throw new DomainException("RESERVE_REJECTED", "Rejected reserves must be re-submitted as a new reserve.");
        if (string.IsNullOrWhiteSpace(changeReason))
            throw new DomainException("RESERVE_CHANGE_REASON_REQUIRED", "Change reason is required when adjusting a reserve.");

        var previous = CurrentAmount;
        CurrentAmount = newAmount;
        ChangeSequence += 1;
        ApprovalStatus = ResolveApprovalStatus(newAmount);
        UpdatedAt = now;
        UserModified = userId;

        if (ApprovalStatus == ReserveApprovalStatus.AutoApproved)
        {
            LastApprovedAt = now;
            LastApprovedBy = "SYSTEM";
        }

        History.Add(new ReserveHistory
        {
            OrganizationEntityId = OrganizationEntityId,
            ReserveComponentId = Id,
            ChangeSequence = ChangeSequence,
            PreviousAmount = previous,
            NewAmount = newAmount,
            ChangeReason = changeReason,
            ChangedBy = userId,
            ChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            UserCreated = userId,
            UserModified = userId
        });

        RaiseDomainEvent(new ReserveAdjustedEvent(
            ClaimId, Id, previous, newAmount, ApprovalStatus, ChangeSequence, changeReason));
    }

    public void Approve(string approverRole, string userId, DateTimeOffset now)
    {
        var required = ApprovalStatus switch
        {
            ReserveApprovalStatus.PendingSupervisorApproval => new[] { "Supervisor", "Manager" },
            ReserveApprovalStatus.PendingManagerApproval => new[] { "Manager" },
            _ => Array.Empty<string>()
        };

        if (required.Length == 0)
            throw new DomainException("RESERVE_NOT_PENDING", "Reserve is not awaiting approval.");
        if (!required.Contains(approverRole, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("RESERVE_INSUFFICIENT_AUTHORITY",
                $"Reserve requires {string.Join(" or ", required)} approval; user role '{approverRole}' is not authorized.");

        ApprovalStatus = ReserveApprovalStatus.Approved;
        LastApprovedAt = now;
        LastApprovedBy = userId;
        UpdatedAt = now;
        UserModified = userId;

        RaiseDomainEvent(new ReserveApprovedEvent(ClaimId, Id, CurrentAmount, ChangeSequence, userId));
    }

    public void Reject(string rejectionReason, string approverRole, string userId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("RESERVE_REJECTION_REASON_REQUIRED", "Rejection reason is required.");
        if (ApprovalStatus != ReserveApprovalStatus.PendingSupervisorApproval
            && ApprovalStatus != ReserveApprovalStatus.PendingManagerApproval)
            throw new DomainException("RESERVE_NOT_PENDING", "Reserve is not awaiting approval.");

        var allowed = ApprovalStatus == ReserveApprovalStatus.PendingManagerApproval
            ? new[] { "Manager" }
            : new[] { "Supervisor", "Manager" };
        if (!allowed.Contains(approverRole, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("RESERVE_INSUFFICIENT_AUTHORITY",
                $"Reserve rejection requires {string.Join(" or ", allowed)}; user role '{approverRole}' is not authorized.");

        ApprovalStatus = ReserveApprovalStatus.Rejected;
        RejectionReason = rejectionReason;
        UpdatedAt = now;
        UserModified = userId;

        RaiseDomainEvent(new ReserveRejectedEvent(ClaimId, Id, rejectionReason, userId));
    }

    public string IdempotencyKeyForCurrentChange() => $"Reserve:{Id:N}:Change:{ChangeSequence}";

    public static ReserveApprovalStatus ResolveApprovalStatus(decimal amount) =>
        amount switch
        {
            <= AutoApproveThreshold => ReserveApprovalStatus.AutoApproved,
            <= SupervisorThreshold => ReserveApprovalStatus.PendingSupervisorApproval,
            _ => ReserveApprovalStatus.PendingManagerApproval
        };
}
