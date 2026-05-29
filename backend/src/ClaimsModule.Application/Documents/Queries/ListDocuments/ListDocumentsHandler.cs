using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Documents.Queries.ListDocuments;

public class ListDocumentsHandler : IRequestHandler<ListDocumentsQuery, IReadOnlyCollection<DocumentLink>>
{
    private readonly IClaimsDbContext _db;
    private readonly IStorageService _storage;

    public ListDocumentsHandler(IClaimsDbContext db, IStorageService storage)
    {
        _db = db; _storage = storage;
    }

    public async Task<IReadOnlyCollection<DocumentLink>> Handle(ListDocumentsQuery request, CancellationToken ct)
    {
        var docs = await _db.ClaimDocuments.AsNoTracking()
            .Where(d => d.ClaimId == request.ClaimId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        var links = new List<DocumentLink>(docs.Count);
        foreach (var d in docs)
        {
            var url = await _storage.CreateReadSasUriAsync(d.BlobReference, TimeSpan.FromHours(1), ct);
            links.Add(new DocumentLink(d.Id, d.FileName, d.ContentType, d.SizeBytes,
                d.DocumentType.ToString(), d.UploadedAt, d.UploadedBy, url));
        }
        return links;
    }
}
