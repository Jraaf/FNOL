using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Queries.ListReserves;

public class ListReservesHandler : IRequestHandler<ListReservesQuery, IReadOnlyCollection<ReserveComponentDto>>
{
    private readonly IClaimsDbContext _db;
    private readonly IMapper _mapper;

    public ListReservesHandler(IClaimsDbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ReserveComponentDto>> Handle(ListReservesQuery request, CancellationToken ct)
    {
        return await _db.ClaimReserveComponents.AsNoTracking()
            .Where(r => r.ClaimId == request.ClaimId)
            .Include(r => r.History)
            .OrderBy(r => r.ComponentType)
            .ProjectTo<ReserveComponentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
