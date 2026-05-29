using FluentValidation;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public class TransitionClaimStatusValidator : AbstractValidator<TransitionClaimStatusCommand>
{
    public TransitionClaimStatusValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ToStatus).IsInEnum();
    }
}
