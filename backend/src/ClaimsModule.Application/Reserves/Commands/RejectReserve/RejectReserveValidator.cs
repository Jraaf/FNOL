using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public class RejectReserveValidator : AbstractValidator<RejectReserveCommand>
{
    public RejectReserveValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ReserveId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(1000);
    }
}
