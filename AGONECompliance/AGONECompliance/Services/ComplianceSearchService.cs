using AGONECompliance.Domain;
using AGONECompliance.Options;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;

namespace AGONECompliance.Services;

public sealed class ComplianceSearchService(
    IOptions<AzureOptions> azureOptions,
    IBlobStorageService blobStorageService,
    ILogger<ComplianceSearchService> logger) : IComplianceSearchService
{
    private readonly AzureOptions _options = azureOptions.Value;

    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken)
    {
        if (!IsSearchConfigured())
        {
            logger.LogInformation("Azure AI Search is not configured. Using no-op local mode.");
            return;
        }

        try
        {
            var endpoint = new Uri(_options.AiSearch.Endpoint);
            var credential = new AzureKeyCredential(_options.AiSearch.ApiKey);
            var indexClient = new SearchIndexClient(endpoint, credential);

            var indexNames = indexClient.GetIndexNamesAsync(cancellationToken);
            await foreach (var indexName in indexNames.WithCancellation(cancellationToken))
            {
                if (string.Equals(indexName, _options.AiSearch.IndexName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var definition = new SearchIndex(_options.AiSearch.IndexName)
            {
                Fields =
                [
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SimpleField("workspaceId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                    new SimpleField("type", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SearchableField("fileName") { IsFilterable = true, IsSortable = true },
                    new SearchableField("ruleReference") { IsFilterable = true, IsSortable = true },
                    new SearchableField("fullText") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                    new SimpleField("uploadedAtUtc", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true }
                ]
            };

            await indexClient.CreateIndexAsync(definition, cancellationToken);
            logger.LogInformation("Created AI Search index: {IndexName}", _options.AiSearch.IndexName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create AI Search index. Continuing in degraded mode.");
        }
    }

    public async Task IndexDocumentAsync(UploadedDocument document, CancellationToken cancellationToken)
    {
        if (!IsSearchConfigured())
        {
            return;
        }

        try
        {
            var endpoint = new Uri(_options.AiSearch.Endpoint);
            var credential = new AzureKeyCredential(_options.AiSearch.ApiKey);
            var searchClient = new Azure.Search.Documents.SearchClient(endpoint, _options.AiSearch.IndexName, credential);

            var documents = await BuildSearchDocumentsAsync(document, cancellationToken);
            if (documents.Count == 0)
            {
                return;
            }

            await searchClient.UploadDocumentsAsync(documents, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index document {DocumentId} in AI Search.", document.Id);
        }
    }

    private bool IsSearchConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.AiSearch.Endpoint)
               && !string.IsNullOrWhiteSpace(_options.AiSearch.ApiKey)
               && !string.IsNullOrWhiteSpace(_options.AiSearch.IndexName);
    }

    private async Task<List<object>> BuildSearchDocumentsAsync(UploadedDocument document, CancellationToken cancellationToken)
    {
        var items = new List<object>();
        string? parsedJson = null;
        if (string.IsNullOrWhiteSpace(parsedJson) && !string.IsNullOrWhiteSpace(document.ParsedJsonBlobPath))
        {
            try
            {
                var (stream, _) = await blobStorageService.DownloadAsync(
                    document.ParsedJsonBlobPath,
                    "application/json",
                    cancellationToken);
                using (stream)
                {
                    using var reader = new StreamReader(stream);
                    parsedJson = await reader.ReadToEndAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read parsed JSON blob for document {DocumentId}.", document.Id);
            }
        }

        var parsedPages = TryExtractPages(parsedJson);
        if (parsedPages.Count == 0)
        {
            items.Add(new
            {
                id = document.Id.ToString(),
                workspaceId = document.EvaluationWorkspaceId.ToString(),
                pageNumber = 1,
                type = document.Type.ToString(),
                fileName = document.OriginalFileName,
                ruleReference = string.Empty,
                fullText = document.FullText ?? string.Empty,
                uploadedAtUtc = document.CreatedAtUtc
            });
            return items;
        }

        foreach (var page in parsedPages)
        {
            items.Add(new
            {
                id = $"{document.Id:N}-p{page.PageNumber}",
                workspaceId = document.EvaluationWorkspaceId.ToString(),
                pageNumber = page.PageNumber,
                type = document.Type.ToString(),
                fileName = document.OriginalFileName,
                ruleReference = string.Empty,
                fullText = page.Content,
                uploadedAtUtc = document.CreatedAtUtc
            });
        }

        return items;
    }

    private static List<ParsedPage> TryExtractPages(string? parsedJson)
    {
        if (string.IsNullOrWhiteSpace(parsedJson))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(parsedJson);
            if (!doc.RootElement.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<ParsedPage>();
            foreach (var page in pages.EnumerateArray())
            {
                var pageNumber = page.TryGetProperty("pageNumber", out var numberElement) && numberElement.TryGetInt32(out var n)
                    ? n
                    : 1;
                var content = page.TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : string.Empty;
                items.Add(new ParsedPage(pageNumber, content));
            }

            return items;
        }
        catch
        {
            return [];
        }
    }

    private sealed record ParsedPage(int PageNumber, string Content);
}
