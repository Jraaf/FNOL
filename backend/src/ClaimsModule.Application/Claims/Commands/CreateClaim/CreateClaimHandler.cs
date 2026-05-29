using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

public class CreateClaimHandler : IRequestHandler<CreateClaimCommand, CreateClaimResult>
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IPolicyService _policies;
    private readonly IClaimNumberGenerator _claimNumbers;
    private readonly IBackgroundJobScheduler _jobs;

    public CreateClaimHandler(
        IClaimsDbContext db,
        IUnitOfWork uow,
        ICurrentUser user,
        IDateTimeProvider clock,
        IPolicyService policies,
        IClaimNumberGenerator claimNumbers,
        IBackgroundJobScheduler jobs)
    {
        _db = db;
        _uow = uow;
        _user = user;
        _clock = clock;
        _policies = policies;
        _claimNumbers = claimNumbers;
        _jobs = jobs;
    }

    public async Task<CreateClaimResult> Handle(CreateClaimCommand request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.PolicyId, ct)
            ?? throw new NotFoundException("Policy", request.PolicyId);

        // BR-C-02 — loss date in policy effective window (warning only)
        var outsidePolicy = request.LossDate < policy.EffectiveDate || request.LossDate > policy.ExpirationDate;

        var now = _clock.UtcNow;
        var claimNumber = await _claimNumbers.NextAsync(_user.OrganizationId, now.Year, ct);

        var claim = Claim.CreateFnol(
            claimNumber,
            _user.OrganizationId,
            policy.PolicyId,
            policy.PolicyNumber,
            policy.ClientName,
            request.LossDate,
            request.CauseOfLossCode,
            request.LossLocation,
            request.LossDescription,
            _user.UserId,
            now,
            outsidePolicy);

        foreach (var p in request.Parties)
        {
            claim.AddParty(new ClaimParty
            {
                PartyType = p.PartyType,
                FirstName = p.FirstName,
                LastName = p.LastName,
                ContactEmail = p.ContactEmail,
                ContactPhone = p.ContactPhone,
                AddressLine = p.AddressLine
            }, _user.UserId, now);
        }

        foreach (var r in request.RiskObjects ?? Array.Empty<CreateClaimRiskObjectInput>())
        {
            claim.AddRiskObject(new ClaimRiskObject
            {
                InsuredAssetType = r.InsuredAssetType,
                AssetReference = r.AssetReference,
                DamageDescription = r.DamageDescription,
                EstimatedDamageAmount = r.EstimatedDamageAmount
            }, _user.UserId, now);
        }

        // BR-C-03 enforced again at domain level as a safety net
        claim.EnsureHasClaimant();

        ClaimReserveComponent? initialReserve = null;
        if (request.InitialReserve is { } ir)
        {
            initialReserve = claim.OpenReserve(ir.ComponentType, ir.Amount, _user.UserId, now);
        }

        _db.Claims.Add(claim);
        await _uow.SaveChangesAsync(ct);

        // BR-R-02: auto-approved reserves enqueue GL posting
        if (initialReserve is { ApprovalStatus: ReserveApprovalStatus.AutoApproved })
        {
            _jobs.EnqueueGlPosting(claim.Id, initialReserve.Id, initialReserve.ChangeSequence,
                initialReserve.IdempotencyKeyForCurrentChange());
        }

        return new CreateClaimResult(claim.Id, claim.ClaimNumber, claim.Status, outsidePolicy);
    }
}
