namespace ClaimsModule.Domain.Enums;

public static class AuditEventType
{
    public const string ClaimCreated = "CLAIM_CREATED";
    public const string ClaimStatusChanged = "CLAIM_STATUS_CHANGED";
    public const string LossDateOutsidePolicyPeriod = "LOSS_DATE_OUTSIDE_POLICY_PERIOD";
    public const string ReserveOpened = "RESERVE_OPENED";
    public const string ReserveAdjusted = "RESERVE_ADJUSTED";
    public const string ReserveApproved = "RESERVE_APPROVED";
    public const string ReserveRejected = "RESERVE_REJECTED";
    public const string ReserveAutoApproved = "RESERVE_AUTO_APPROVED";
    public const string GlPostingSimulated = "GL_POSTING_SIMULATED";
    public const string SlaBreachDetected = "SLA_BREACH_DETECTED";
    public const string DocumentUploaded = "DOCUMENT_UPLOADED";
    public const string ManagerOverrideRequired = "MANAGER_OVERRIDE_REQUIRED";
}
