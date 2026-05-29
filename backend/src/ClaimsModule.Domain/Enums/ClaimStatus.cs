namespace ClaimsModule.Domain.Enums;

public enum ClaimStatus
{
    Draft = 0,
    Open = 1,
    UnderInvestigation = 2,
    PendingPayment = 3,
    Closed = 4,
    Reopened = 5,
    Withdrawn = 6,
    SlaBreached = 7
}
