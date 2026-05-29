using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reference.Queries.GetCauseCodes;

public record CauseCodeDto(string Code, string Description, string PerilCategory);

public record GetCauseCodesQuery(string? PerilCategory) : IRequest<IReadOnlyCollection<CauseCodeDto>>;

public class GetCauseCodesHandler : IRequestHandler<GetCauseCodesQuery, IReadOnlyCollection<CauseCodeDto>>
{
    private readonly IClaimsDbContext _db;
    public GetCauseCodesHandler(IClaimsDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<CauseCodeDto>> Handle(GetCauseCodesQuery request, CancellationToken ct)
    {
        var q = _db.CauseOfLossCodes.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(request.PerilCategory))
            q = q.Where(c => c.PerilCategory == request.PerilCategory);
        return await q.OrderBy(c => c.Code)
            .Select(c => new CauseCodeDto(c.Code, c.Description, c.PerilCategory))
            .ToListAsync(ct);
    }
}
