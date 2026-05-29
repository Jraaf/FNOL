using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.ListClaims;

public class ListClaimsHandler : IRequestHandler<ListClaimsQuery, PagedResult<ClaimSummaryDto>>
{
    private readonly IClaimsDbContext _db;
    private readonly IMapper _mapper;

    public ListClaimsHandler(IClaimsDbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task<PagedResult<ClaimSummaryDto>> Handle(ListClaimsQuery request, CancellationToken ct)
    {
        var query = _db.Claims.AsNoTracking().Include(c => c.Reserves).AsQueryable();

        if (request.Status.HasValue) query = query.Where(c => c.Status == request.Status.Value);
        if (request.LossDateFrom.HasValue) query = query.Where(c => c.LossDate >= request.LossDateFrom.Value);
        if (request.LossDateTo.HasValue) query = query.Where(c => c.LossDate <= request.LossDateTo.Value);
        if (!string.IsNullOrWhiteSpace(request.AssignedHandlerUserId))
            query = query.Where(c => c.AssignedHandlerUserId == request.AssignedHandlerUserId);
        if (!string.IsNullOrWhiteSpace(request.CauseOfLossCode))
            query = query.Where(c => c.CauseOfLossCode == request.CauseOfLossCode);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(c => c.LastTouchedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<ClaimSummaryDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<ClaimSummaryDto>(items, total, page, pageSize);
    }
}
