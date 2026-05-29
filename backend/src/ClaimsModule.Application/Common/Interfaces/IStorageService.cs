namespace ClaimsModule.Application.Common.Interfaces;

public record UploadedBlob(string BlobReference, long SizeBytes);

public interface IStorageService
{
    Task<UploadedBlob> UploadAsync(string container, string path, Stream content, string contentType,
        CancellationToken cancellationToken);
    Task<Stream> DownloadAsync(string blobReference, CancellationToken cancellationToken);
    Task<Uri> CreateReadSasUriAsync(string blobReference, TimeSpan ttl, CancellationToken cancellationToken);
}
