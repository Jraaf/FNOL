using ClaimsModule.Application.Common.Interfaces;
using Hangfire;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _client;
    public HangfireBackgroundJobScheduler(IBackgroundJobClient client) => _client = client;

    public string EnqueueGlPosting(Guid claimId, Guid reserveComponentId, int changeSequence, string idempotencyKey)
        => _client.Enqueue<GlPostingJob>(job =>
            job.ExecuteAsync(claimId, reserveComponentId, changeSequence, idempotencyKey, CancellationToken.None));
}
