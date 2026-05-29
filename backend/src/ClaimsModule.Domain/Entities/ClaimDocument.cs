using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class ClaimDocument : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobReference { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.Other;
    public DateTimeOffset UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}
