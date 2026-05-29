using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.OpenReserve;

public class OpenReserveValidator : AbstractValidator<OpenReserveCommand>
{
    public OpenReserveValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ComponentType).IsInEnum();
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithErrorCode("BR-R-01")
            .WithMessage("Reserve amount must be greater than zero.");
    }
}
