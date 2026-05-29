using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Documents.Commands.UploadDocument;

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, ClaimDocumentDto>
{
    private readonly IClaimsDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IStorageService _storage;
    private readonly IMapper _mapper;

    public UploadDocumentHandler(IClaimsDbContext db, IUnitOfWork uow, ICurrentUser user,
        IDateTimeProvider clock, IStorageService storage, IMapper mapper)
    {
        _db = db; _uow = uow; _user = user; _clock = clock; _storage = storage; _mapper = mapper;
    }

    public async Task<ClaimDocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct)
            ?? throw new NotFoundException("Claim", request.ClaimId);

        var safeName = Path.GetFileName(request.FileName);
        var path = $"{_user.OrganizationId:N}/{claim.Id:N}/{Guid.NewGuid():N}-{safeName}";
        var uploaded = await _storage.UploadAsync("claim-documents", path, request.Content, request.ContentType, ct);

        var doc = claim.AddDocument(safeName, uploaded.BlobReference, request.ContentType,
            uploaded.SizeBytes, request.DocumentType, _user.UserId, _clock.UtcNow);

        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<ClaimDocumentDto>(doc);
    }
}
