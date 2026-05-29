using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.GetClaimDetail;

public class GetClaimDetailHandler : IRequestHandler<GetClaimDetailQuery, ClaimDetailDto>
{
    private readonly IClaimsDbContext _db;
    private readonly IMapper _mapper;

    public GetClaimDetailHandler(IClaimsDbContext db, IMapper mapper)
    {
        _db = db; _mapper = mapper;
    }

    public async Task<ClaimDetailDto> Handle(GetClaimDetailQuery request, CancellationToken ct)
    {
        var claim = await _db.Claims.AsNoTracking()
            .Include(c => c.LossEvent)
            .Include(c => c.Parties)
            .Include(c => c.RiskObjects)
            .Include(c => c.Reserves).ThenInclude(r => r.History)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct)
            ?? throw new NotFoundException("Claim", request.ClaimId);

        return _mapper.Map<ClaimDetailDto>(claim);
    }
}
