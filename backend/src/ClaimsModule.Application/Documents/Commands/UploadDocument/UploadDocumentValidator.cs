using FluentValidation;

namespace ClaimsModule.Application.Documents.Commands.UploadDocument;

public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Content).NotNull();
    }
}
