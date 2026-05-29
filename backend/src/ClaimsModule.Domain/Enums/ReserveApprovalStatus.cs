namespace ClaimsModule.Domain.Enums;

public enum ReserveApprovalStatus
{
    AutoApproved = 0,
    PendingSupervisorApproval = 1,
    PendingManagerApproval = 2,
    Approved = 3,
    Rejected = 4
}
