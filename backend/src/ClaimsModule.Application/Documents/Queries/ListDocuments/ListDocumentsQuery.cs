using MediatR;

namespace ClaimsModule.Application.Documents.Queries.ListDocuments;

public record DocumentLink(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DocumentType,
    DateTimeOffset UploadedAt,
    string UploadedBy,
    Uri DownloadUrl);

public record ListDocumentsQuery(Guid ClaimId) : IRequest<IReadOnlyCollection<DocumentLink>>;
