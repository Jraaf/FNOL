namespace ClaimsModule.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string EntityName { get; }
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
        EntityName = entityName;
    }
}
