using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClaimsModule.Persistence.Seeding;

namespace ClaimsModule.API.Auth;

public class MockJwtMiddleware
{
    private readonly RequestDelegate _next;
    public MockJwtMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var auth = context.Request.Headers["Authorization"].ToString();
        ClaimsIdentity identity = new("Mock");
        string? userId = null, userName = null, role = null;

        if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["Bearer ".Length..].Trim();
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
                userName = jwt.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name)?.Value;
                role = jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value;
            }
            catch
            {
                // Fall back to header-based mock identity below.
            }
        }

        userId ??= context.Request.Headers["X-Mock-UserId"].FirstOrDefault() ?? "handler-1";
        userName ??= context.Request.Headers["X-Mock-UserName"].FirstOrDefault() ?? "Demo Handler";
        role ??= context.Request.Headers["X-Mock-Role"].FirstOrDefault() ?? "Handler";

        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Name, userName));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role));
        identity.AddClaim(new System.Security.Claims.Claim("org", ReferenceDataSeeder.DefaultOrganizationId.ToString()));

        context.User = new ClaimsPrincipal(identity);
        await _next(context);
    }
}
