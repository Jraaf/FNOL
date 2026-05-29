using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

public class CreateClaimValidator : AbstractValidator<CreateClaimCommand>
{
    private readonly IClaimsDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _user;

    public CreateClaimValidator(IClaimsDbContext db, IDateTimeProvider clock, ICurrentUser user)
    {
        _db = db;
        _clock = clock;
        _user = user;

        RuleFor(x => x.PolicyId).NotEmpty();
        RuleFor(x => x.CauseOfLossCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LossLocation).NotEmpty().MaximumLength(255);
        RuleFor(x => x.LossDescription).NotEmpty();

        // BR-C-01
        RuleFor(x => x.LossDate)
            .Must(d => d <= _clock.UtcNow)
            .WithErrorCode("BR-C-01")
            .WithMessage("Loss date must not be in the future.");

        // BR-C-03
        RuleFor(x => x.Parties)
            .Must(p => p != null && p.Any(party => party.PartyType == PartyType.Claimant))
            .WithErrorCode("BR-C-03")
            .WithMessage("A claim must include at least one Claimant party.");

        RuleForEach(x => x.Parties).ChildRules(p =>
        {
            p.RuleFor(x => x.FirstName).NotEmpty().MaximumLength(255);
            p.RuleFor(x => x.LastName).NotEmpty().MaximumLength(255);
            p.RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        });

        RuleForEach(x => x.RiskObjects).ChildRules(r =>
        {
            r.RuleFor(x => x.InsuredAssetType).NotEmpty().MaximumLength(100);
            r.RuleFor(x => x.AssetReference).NotEmpty().MaximumLength(255);
            r.RuleFor(x => x.EstimatedDamageAmount).GreaterThan(0)
                .When(x => x.EstimatedDamageAmount.HasValue);
        });

        When(x => x.InitialReserve != null, () =>
        {
            RuleFor(x => x.InitialReserve!.Amount)
                .GreaterThan(0)
                .WithErrorCode("BR-R-01")
                .WithMessage("Initial reserve amount must be greater than zero.");
        });

        // BR-C-05
        RuleFor(x => x.CauseOfLossCode)
            .MustAsync(CauseCodeExistsAndActive)
            .WithErrorCode("BR-C-05")
            .WithMessage("Cause of loss code must exist and be active for the organization.");
    }

    private async Task<bool> CauseCodeExistsAndActive(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return await _db.CauseOfLossCodes
            .AnyAsync(c => c.OrganizationEntityId == _user.OrganizationId
                        && c.Code == code
                        && c.IsActive, ct);
    }
}
