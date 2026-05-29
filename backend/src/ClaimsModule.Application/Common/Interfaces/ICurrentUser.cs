namespace ClaimsModule.Application.Common.Interfaces;

public interface ICurrentUser
{
    string UserId { get; }
    string UserName { get; }
    Guid OrganizationId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool HasRole(string role);
    string PrimaryRole { get; }
}
