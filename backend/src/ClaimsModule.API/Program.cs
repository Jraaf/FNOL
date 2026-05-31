using ClaimsModule.API.Auth;
using ClaimsModule.API.Hangfire;
using ClaimsModule.API.Middleware;
using ClaimsModule.Application;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Infrastructure;
using ClaimsModule.Persistence;
using ClaimsModule.Persistence.Seeding;
using Hangfire;
using Microsoft.OpenApi.Models;

// Load a .env file (if present) into process environment variables BEFORE the host's
// configuration is built, so the default Environment Variables provider folds them in.
// A single .env can therefore drive local `dotnet run`, local Docker, and Azure.
//   - NoClobber():   real environment variables (e.g. those injected by Azure Container
//                    Apps / docker --env-file) always win over the file, so platform
//                    settings are never silently overridden by a stray local .env.
//   - TraversePath(): walk up from the working directory so the repo-root .env is found
//                    regardless of where the app is launched from.
// The file is optional and is NOT part of the published image (gitignored).
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Layer in appsettings.Local.json (gitignored) and appsettings.{Env}.Local.json so secrets
// — DB connection strings, Azure storage account keys, etc. — stay out of source control.
// Default ASP.NET Core chain: appsettings.json -> appsettings.{Env}.json -> User Secrets (Dev)
// -> Env vars -> CLI args. We insert the Local files between appsettings.{Env}.json and
// User Secrets so explicit env vars / CLI args still win.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddClaimsApplication();
builder.Services.AddClaimsPersistence(builder.Configuration);
builder.Services.AddClaimsInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Claims Module API",
        Version = "v1",
        Description = "FNOL Intake + Reserve Management (DICEUS assessment)"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Mock JWT or use X-Mock-Role header (Handler|Supervisor|Manager)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var corsPolicy = "ClaimsCors";
builder.Services.AddCors(o => o.AddPolicy(corsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" })
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await ReferenceDataSeeder.SeedAsync(db, CancellationToken.None);
    InfrastructureServiceCollectionExtensions.ConfigureRecurringJobs(scope.ServiceProvider);
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors(corsPolicy);
app.UseMiddleware<MockJwtMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// In container deployments the Angular SPA's production bundle is copied into wwwroot,
// so a single image serves both the API (/api, /swagger, /hangfire) and the SPA shell
// (everything else, with index.html fallback for client-side routing).
var spaIndex = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
var spaPresent = File.Exists(spaIndex);
if (spaPresent)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

var allowedDashboardRoles = builder.Configuration
    .GetSection("Hangfire:Dashboard:AllowedRoles").Get<string[]>()
    ?? new[] { "Supervisor", "Manager" };
var allowLocalDashboardRequests = builder.Configuration
    .GetValue<bool?>("Hangfire:Dashboard:AllowLocalRequests") ?? true;
var dashboardFilter = new HangfireRoleDashboardFilter(allowedDashboardRoles, allowLocalDashboardRequests);

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { dashboardFilter },
    AsyncAuthorization = new[] { dashboardFilter },
    IgnoreAntiforgeryToken = true,
    DisplayStorageConnectionString = false,
    DashboardTitle = "Claims Module — Hangfire"
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow }));

if (spaPresent)
{
    // SPA client-side routes (e.g. /claims/{guid}) fall back to index.html.
    // /api, /swagger, /hangfire, /health are matched first by their respective endpoints
    // and never hit the fallback.
    app.MapFallbackToFile("index.html");
}
else
{
    // API-only mode (e.g. `dotnet run` without the SPA bundle): land users on Swagger.
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();
