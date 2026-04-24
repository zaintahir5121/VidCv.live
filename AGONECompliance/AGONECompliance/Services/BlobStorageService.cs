using AGONECompliance.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace AGONECompliance.Services;

public sealed class BlobStorageService(IOptions<AzureOptions> azureOptions, ILogger<BlobStorageService> logger) : IBlobStorageService
{
    private readonly AzureOptions _options = azureOptions.Value;
    private readonly string _localUploadsRoot = Path.Combine(AppContext.BaseDirectory, "local-uploads");

    public async Task<string> UploadAsync(
        Stream stream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken,
        string? folderPath = null)
    {
        var safeName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{fileName}";
        var blobName = string.IsNullOrWhiteSpace(folderPath)
            ? safeName
            : $"{folderPath.Trim('/').Replace("\\", "/")}/{safeName}";

        if (string.IsNullOrWhiteSpace(_options.BlobStorage.ConnectionString))
        {
            Directory.CreateDirectory(_localUploadsRoot);
            var filePath = Path.Combine(_localUploadsRoot, blobName.Replace('/', Path.DirectorySeparatorChar));
            var parent = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            await using var output = File.Create(filePath);
            await stream.CopyToAsync(output, cancellationToken);
            logger.LogInformation("Stored file in local uploads fallback: {FilePath}", filePath);
            return filePath;
        }

        var serviceClient = new BlobServiceClient(_options.BlobStorage.ConnectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_options.BlobStorage.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
        {
            ContentType = contentType
        }, cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task<(Stream Stream, string ContentType)> DownloadAsync(
        string blobPath,
        string fallbackContentType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BlobStorage.ConnectionString))
        {
            var normalizedRelativePath = blobPath;
            if (Path.IsPathRooted(normalizedRelativePath))
            {
                normalizedRelativePath = Path.GetRelativePath(_localUploadsRoot, normalizedRelativePath);
            }

            var localPath = Path.Combine(
                _localUploadsRoot,
                normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException("Local blob fallback file not found.", localPath);
            }

            var stream = File.OpenRead(localPath);
            return (stream, fallbackContentType);
        }

        var serviceClient = new BlobServiceClient(_options.BlobStorage.ConnectionString);
        var blobUri = new Uri(blobPath);
        var blobName = string.Concat(blobUri.Segments.Skip(2));
        var containerClient = serviceClient.GetBlobContainerClient(_options.BlobStorage.ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        var bytes = response.Value.Content.ToArray();
        var streamOut = new MemoryStream(bytes);
        var contentType = response.Value.Details.ContentType ?? fallbackContentType;
        return (streamOut, contentType);
    }
}
