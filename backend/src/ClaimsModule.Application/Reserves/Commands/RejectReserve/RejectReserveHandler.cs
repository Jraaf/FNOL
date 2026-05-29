using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public class RejectReserveHandler : IRequestHandler<RejectReserveCommand, ReserveComponentDto>
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IMapper _mapper;

    public RejectReserveHandler(IClaimsDbContext db, IUnitOfWork uow, ICurrentUser user,
        IDateTimeProvider clock, IMapper mapper)
    {
        _db = db; _uow = uow; _user = user; _clock = clock; _mapper = mapper;
    }

    public async Task<ReserveComponentDto> Handle(RejectReserveCommand request, CancellationToken ct)
    {
        var claim = await _db.Claims
            .Include(c => c.Reserves).ThenInclude(r => r.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct)
            ?? throw new NotFoundException("Claim", request.ClaimId);

        var reserve = claim.RejectReserve(request.ReserveId, request.RejectionReason,
            _user.PrimaryRole, _user.UserId, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<ReserveComponentDto>(reserve);
    }
}
