namespace ClaimsModule.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "LocalFileSystem"; // or "AzureBlob"
    public string? AzureConnectionString { get; set; }
    public string LocalRootPath { get; set; } = "App_Data/uploads";
    public string ContainerName { get; set; } = "claim-documents";
    public string? PublicBaseUrl { get; set; }
}
