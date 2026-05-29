using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.GetClaimAudit;

public class GetClaimAuditHandler : IRequestHandler<GetClaimAuditQuery, IReadOnlyCollection<ClaimAuditEntryDto>>
{
    private readonly IClaimsDbContext _db;
    private readonly IMapper _mapper;

    public GetClaimAuditHandler(IClaimsDbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ClaimAuditEntryDto>> Handle(GetClaimAuditQuery request, CancellationToken ct)
    {
        return await _db.ClaimAuditLogs.AsNoTracking()
            .Where(a => a.ClaimId == request.ClaimId)
            .OrderByDescending(a => a.OccurredAt)
            .ProjectTo<ClaimAuditEntryDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
