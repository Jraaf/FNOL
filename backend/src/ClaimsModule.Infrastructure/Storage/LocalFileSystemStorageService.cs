using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

public class LocalFileSystemStorageService : IStorageService
{
    private readonly StorageOptions _options;

    public LocalFileSystemStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.LocalRootPath);
    }

    public async Task<UploadedBlob> UploadAsync(string container, string path, Stream content, string contentType,
        CancellationToken cancellationToken)
    {
        var full = Path.Combine(_options.LocalRootPath, container, path);
        var directory = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(directory);

        await using (var fs = File.Create(full))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var info = new FileInfo(full);
        return new UploadedBlob($"local://{container}/{path}", info.Length);
    }

    public Task<Stream> DownloadAsync(string blobReference, CancellationToken cancellationToken)
    {
        var path = ResolvePath(blobReference);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task<Uri> CreateReadSasUriAsync(string blobReference, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var path = ResolvePath(blobReference);
        var fileUri = new Uri(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            var rel = blobReference.StartsWith("local://", StringComparison.Ordinal)
                ? blobReference["local://".Length..]
                : blobReference;
            return Task.FromResult(new Uri(new Uri(_options.PublicBaseUrl), rel));
        }
        return Task.FromResult(fileUri);
    }

    private string ResolvePath(string blobReference)
    {
        var rel = blobReference.StartsWith("local://", StringComparison.Ordinal)
            ? blobReference["local://".Length..]
            : blobReference;
        return Path.Combine(_options.LocalRootPath, rel);
    }
}
