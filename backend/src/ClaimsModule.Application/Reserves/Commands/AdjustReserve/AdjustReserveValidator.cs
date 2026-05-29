using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.AdjustReserve;

public class AdjustReserveValidator : AbstractValidator<AdjustReserveCommand>
{
    public AdjustReserveValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ReserveId).NotEmpty();
        RuleFor(x => x.NewAmount).GreaterThan(0).WithErrorCode("BR-R-01");
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);
    }
}
