namespace ClaimsModule.Application.Common.Interfaces;

public interface IBackgroundJobScheduler
{
    string EnqueueGlPosting(Guid claimId, Guid reserveComponentId, int changeSequence, string idempotencyKey);
}
