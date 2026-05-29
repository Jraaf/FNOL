using System.Security.Claims;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Persistence.Seeding;

namespace ClaimsModule.API.Auth;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal Principal => _accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    public string UserName => Principal.FindFirstValue(ClaimTypes.Name) ?? "system";
    public Guid OrganizationId =>
        Guid.TryParse(Principal.FindFirstValue("org"), out var g) ? g : ReferenceDataSeeder.DefaultOrganizationId;
    public IReadOnlyCollection<string> Roles =>
        Principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    public bool HasRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    public string PrimaryRole => Roles.FirstOrDefault() ?? "Handler";
}
