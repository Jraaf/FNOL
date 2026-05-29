using ClaimsModule.Application.Common.Interfaces;

namespace ClaimsModule.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
