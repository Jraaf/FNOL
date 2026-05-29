using FluentValidation.Results;

namespace ClaimsModule.Application.Common.Exceptions;

public record ValidationErrorDetail(string Message, string? Code);

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public IReadOnlyDictionary<string, ValidationErrorDetail[]> ErrorDetails { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
        ErrorDetails = new Dictionary<string, ValidationErrorDetail[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        var list = failures.ToList();
        Errors = list
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
        ErrorDetails = list
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new ValidationErrorDetail(f.ErrorMessage, f.ErrorCode)).ToArray());
    }
}
