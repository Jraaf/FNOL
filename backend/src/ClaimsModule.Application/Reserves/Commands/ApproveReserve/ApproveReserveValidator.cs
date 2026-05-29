using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.ApproveReserve;

public class ApproveReserveValidator : AbstractValidator<ApproveReserveCommand>
{
    public ApproveReserveValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ReserveId).NotEmpty();
    }
}
