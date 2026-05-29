using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Persistence.Sequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimsModule.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddClaimsPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("ClaimsDb")
            ?? throw new InvalidOperationException("ConnectionStrings:ClaimsDb is not configured.");

        services.AddDbContext<ClaimsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ClaimsDbContext).Assembly.FullName);
            }));

        services.AddScoped<IClaimsDbContext>(sp => sp.GetRequiredService<ClaimsDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();

        return services;
    }
}
