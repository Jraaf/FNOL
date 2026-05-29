using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClaimsModule.Persistence.DesignTime;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("CLAIMSDB_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ClaimsModule;Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer(connStr, sql => sql.MigrationsAssembly(typeof(ClaimsDbContext).Assembly.FullName))
            .Options;
        return new ClaimsDbContext(options);
    }
}
