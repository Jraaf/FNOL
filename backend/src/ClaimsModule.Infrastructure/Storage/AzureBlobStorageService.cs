using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _client;
    private readonly StorageOptions _options;

    public AzureBlobStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.AzureConnectionString))
            throw new InvalidOperationException("Storage:AzureConnectionString is required when Provider=AzureBlob.");
        _client = new BlobServiceClient(_options.AzureConnectionString);
    }

    public async Task<UploadedBlob> UploadAsync(string container, string path, Stream content, string contentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(container) ? _options.ContainerName : container;
        var blobContainer = _client.GetBlobContainerClient(name);
        await blobContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = blobContainer.GetBlobClient(path);
        var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType };
        var response = await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = headers
        }, cancellationToken);
        var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new UploadedBlob($"{name}/{path}", props.Value.ContentLength);
    }

    public async Task<Stream> DownloadAsync(string blobReference, CancellationToken cancellationToken)
    {
        var (container, path) = SplitReference(blobReference);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(path);
        var stream = new MemoryStream();
        await blob.DownloadToAsync(stream, cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public Task<Uri> CreateReadSasUriAsync(string blobReference, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var (container, path) = SplitReference(blobReference);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(path);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "Azure Blob client cannot generate SAS — connect with a key-based connection string.");
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl));
        return Task.FromResult(blob.GenerateSasUri(sas));
    }

    private static (string Container, string Path) SplitReference(string blobReference)
    {
        var idx = blobReference.IndexOf('/');
        if (idx <= 0) throw new ArgumentException("Invalid blob reference format.", nameof(blobReference));
        return (blobReference[..idx], blobReference[(idx + 1)..]);
    }
}
