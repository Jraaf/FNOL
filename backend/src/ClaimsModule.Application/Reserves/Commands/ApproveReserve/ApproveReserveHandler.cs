using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.ApproveReserve;

public class ApproveReserveHandler : IRequestHandler<ApproveReserveCommand, ReserveComponentDto>
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IBackgroundJobScheduler _jobs;
    private readonly IMapper _mapper;

    public ApproveReserveHandler(IClaimsDbContext db, IUnitOfWork uow, ICurrentUser user,
        IDateTimeProvider clock, IBackgroundJobScheduler jobs, IMapper mapper)
    {
        _db = db; _uow = uow; _user = user; _clock = clock; _jobs = jobs; _mapper = mapper;
    }

    public async Task<ReserveComponentDto> Handle(ApproveReserveCommand request, CancellationToken ct)
    {
        var claim = await _db.Claims
            .Include(c => c.Reserves).ThenInclude(r => r.History)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct)
            ?? throw new NotFoundException("Claim", request.ClaimId);

        var reserve = claim.ApproveReserve(request.ReserveId, _user.PrimaryRole, _user.UserId, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);

        _jobs.EnqueueGlPosting(claim.Id, reserve.Id, reserve.ChangeSequence,
            reserve.IdempotencyKeyForCurrentChange());

        return _mapper.Map<ReserveComponentDto>(reserve);
    }
}
