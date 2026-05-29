using System.Net;
using System.Security.Claims;
using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace ClaimsModule.API.Hangfire;

/// <summary>
/// Role-gated Hangfire dashboard authorization filter.
/// Grants access to the Hangfire dashboard if any of the following is true:
///   1. The request originates from the loopback interface (local PC admin).
///   2. The authenticated principal carries one of the configured allowed roles
///      (defaults to Supervisor / Manager).
///   3. The request supplies the role via the <c>X-Mock-Role</c> header
///      (Postman / Swagger / curl flow that uses the mock auth middleware).
///   4. The request supplies the role via the <c>?role=Supervisor</c> query
///      parameter (browser flow when the user is logged in via the SPA toolbar).
/// </summary>
public class HangfireRoleDashboardFilter : IDashboardAuthorizationFilter, IDashboardAsyncAuthorizationFilter
{
    private readonly HashSet<string> _allowedRoles;
    private readonly bool _allowLocalRequests;

    public HangfireRoleDashboardFilter(IEnumerable<string> allowedRoles, bool allowLocalRequests = true)
    {
        _allowedRoles = new HashSet<string>(allowedRoles, StringComparer.OrdinalIgnoreCase);
        _allowLocalRequests = allowLocalRequests;
    }

    public bool Authorize([NotNull] DashboardContext context) => IsAuthorized(context);

    public Task<bool> AuthorizeAsync([NotNull] DashboardContext context) => Task.FromResult(IsAuthorized(context));

    private bool IsAuthorized(DashboardContext context)
    {
        var http = context.GetHttpContext();
        if (http == null) return false;

        if (_allowLocalRequests && IsLocalRequest(http)) return true;

        if (PrincipalHasAllowedRole(http.User)) return true;

        var headerRole = http.Request.Headers["X-Mock-Role"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerRole) && _allowedRoles.Contains(headerRole)) return true;

        var queryRole = http.Request.Query["role"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryRole) && _allowedRoles.Contains(queryRole)) return true;

        return false;
    }

    private bool PrincipalHasAllowedRole(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return false;
        foreach (var claim in principal.Claims)
        {
            if ((claim.Type == ClaimTypes.Role || claim.Type == "role" || claim.Type == "roles")
                && _allowedRoles.Contains(claim.Value))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsLocalRequest(HttpContext http)
    {
        var conn = http.Connection;
        if (conn.RemoteIpAddress == null) return true; // in-process call
        if (conn.LocalIpAddress != null) return conn.RemoteIpAddress.Equals(conn.LocalIpAddress);
        return IPAddress.IsLoopback(conn.RemoteIpAddress);
    }
}
