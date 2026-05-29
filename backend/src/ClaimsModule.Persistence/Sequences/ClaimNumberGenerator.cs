using ClaimsModule.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Sequences;

public class ClaimNumberGenerator : IClaimNumberGenerator
{
    private readonly ClaimsDbContext _db;

    public ClaimNumberGenerator(ClaimsDbContext db) => _db = db;

    public async Task<string> NextAsync(Guid organizationId, int year, CancellationToken cancellationToken)
    {
        // NOTE: The connection returned by GetDbContext().Database.GetDbConnection() is owned by
        // EF Core. Do NOT wrap it in `using` — disposing it clears its ConnectionString and breaks
        // any subsequent EF operation (e.g. SaveChangesAsync's transaction begin).
        var connection = _db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR dbo.ClaimNumberSequence;";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var next = Convert.ToInt64(result);
            return $"CLM-{year:D4}-{next:D7}";
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }
}
