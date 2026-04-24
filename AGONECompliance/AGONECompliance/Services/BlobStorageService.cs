using AGONECompliance.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace AGONECompliance.Services;

public sealed class BlobStorageService(IOptions<AzureOptions> azureOptions, ILogger<BlobStorageService> logger) : IBlobStorageService
{
    private readonly AzureOptions _options = azureOptions.Value;
    private readonly string _localUploadsRoot = Path.Combine(AppContext.BaseDirectory, "local-uploads");
    private static readonly Regex SafeTokenRegex = new("[^a-z0-9-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<string> UploadAsync(
        Stream stream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken,
        string? folderPath = null)
    {
        var safeName = BuildSafeFileName(fileName, contentType);
        var blobName = string.IsNullOrWhiteSpace(folderPath)
            ? safeName
            : $"{NormalizeFolderPath(folderPath)}/{safeName}";

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

    private static string BuildSafeFileName(string logicalName, string contentType)
    {
        var rawBaseName = Path.GetFileNameWithoutExtension(logicalName);
        var normalizedBaseName = NormalizeToken(rawBaseName, 24, "file");
        var extension = ResolveExtension(logicalName, contentType);
        var uniqueSuffix = $"{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        return $"{normalizedBaseName}-{uniqueSuffix}{extension}";
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        var segments = folderPath
            .Replace("\\", "/", StringComparison.Ordinal)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => NormalizeToken(segment, 32, "folder"));

        return string.Join('/', segments);
    }

    private static string NormalizeToken(string value, int maxLength, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var ascii = new string(value
            .ToLowerInvariant()
            .Select(ch => ch <= 127 ? ch : '-')
            .ToArray());

        var replaced = ascii
            .Replace("_", "-", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal);
        var safe = SafeTokenRegex.Replace(replaced, "-");

        var sb = new StringBuilder(safe.Length);
        var previousDash = false;
        foreach (var ch in safe)
        {
            if (ch == '-')
            {
                if (!previousDash)
                {
                    sb.Append(ch);
                }

                previousDash = true;
                continue;
            }

            sb.Append(ch);
            previousDash = false;
        }

        var compact = sb.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(compact))
        {
            compact = fallback;
        }

        if (compact.Length <= maxLength)
        {
            return compact;
        }

        var shortened = compact[..maxLength].Trim('-');
        return string.IsNullOrWhiteSpace(shortened) ? fallback : shortened;
    }

    private static string ResolveExtension(string logicalName, string contentType)
    {
        var extension = Path.GetExtension(logicalName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var safe = Regex.Replace(extension.ToLowerInvariant(), "[^a-z0-9.]", string.Empty);
            if (safe.StartsWith(".", StringComparison.Ordinal) && safe.Length is > 1 and <= 10)
            {
                return safe;
            }
        }

        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "application/json" => ".json",
            "text/plain" => ".txt",
            _ => ".bin"
        };
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

    public async Task<string?> DownloadTextAsync(
        string? blobPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        var (stream, _) = await DownloadAsync(blobPath, "text/plain", cancellationToken);
        using (stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
