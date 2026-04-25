using AGONECompliance.Domain;
using AGONECompliance.Options;
using AGONECompliance.Shared;
using Azure;
using Azure.Search.Documents;
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
    private const string ExperionConversationIndexName = "agone-experion-conversations";

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

    public async Task EnsureExperionConversationIndexExistsAsync(CancellationToken cancellationToken)
    {
        if (!IsSearchConfigured())
        {
            logger.LogInformation("Azure AI Search is not configured. Experion conversation indexing is disabled.");
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
                if (string.Equals(indexName, ExperionConversationIndexName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var definition = new SearchIndex(ExperionConversationIndexName)
            {
                Fields =
                [
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SimpleField("conversationId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("sessionId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("productCode", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("workspaceId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("userId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SearchableField("userPrompt") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                    new SearchableField("assistantResponse") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                    new SimpleField("responseLayer", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SimpleField("cacheKey", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("occurredAtUtc", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true }
                ]
            };

            await indexClient.CreateIndexAsync(definition, cancellationToken);
            logger.LogInformation("Created Experion conversation index: {IndexName}", ExperionConversationIndexName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create Experion conversation index. Continuing without it.");
        }
    }

    public async Task IndexExperionConversationAsync(
        ExperionConversationIndexDocument document,
        CancellationToken cancellationToken)
    {
        if (!IsSearchConfigured())
        {
            return;
        }

        try
        {
            var endpoint = new Uri(_options.AiSearch.Endpoint);
            var credential = new AzureKeyCredential(_options.AiSearch.ApiKey);
            var searchClient = new Azure.Search.Documents.SearchClient(endpoint, ExperionConversationIndexName, credential);

            await searchClient.UploadDocumentsAsync(
                [
                    new
                    {
                        id = document.Id,
                        conversationId = document.ConversationId,
                        sessionId = document.SessionId,
                        productCode = document.ProductCode,
                        workspaceId = document.WorkspaceId,
                        userId = document.UserId,
                        userPrompt = document.UserPrompt,
                        assistantResponse = document.AssistantResponse,
                        responseLayer = document.ResponseLayer,
                        cacheKey = document.CacheKey,
                        occurredAtUtc = document.OccurredAtUtc
                    }
                ],
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index Experion conversation record {RecordId}.", document.Id);
        }
    }

    public async Task<IReadOnlyList<ExperionConversationIndexDocument>> SearchExperionConversationsAsync(
        string productCode,
        string workspaceId,
        string userId,
        int take,
        CancellationToken cancellationToken)
    {
        if (!IsSearchConfigured())
        {
            return [];
        }

        try
        {
            var endpoint = new Uri(_options.AiSearch.Endpoint);
            var credential = new AzureKeyCredential(_options.AiSearch.ApiKey);
            var searchClient = new Azure.Search.Documents.SearchClient(endpoint, ExperionConversationIndexName, credential);

            var normalizedTake = Math.Clamp(take, 1, 200);
            var filter =
                $"productCode eq '{EscapeODataValue(productCode)}' and workspaceId eq '{EscapeODataValue(workspaceId)}' and userId eq '{EscapeODataValue(userId)}'";
            var options = new SearchOptions
            {
                Filter = filter,
                Size = normalizedTake,
            };
            options.OrderBy.Add("occurredAtUtc desc");

            var response = await searchClient.SearchAsync<SearchDocument>(
                "*",
                options,
                cancellationToken);
            var items = new List<ExperionConversationIndexDocument>();
            await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
            {
                var doc = result.Document;
                items.Add(new ExperionConversationIndexDocument
                {
                    Id = doc.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                    ConversationId = doc.TryGetValue("conversationId", out var conversationId) ? conversationId?.ToString() ?? string.Empty : string.Empty,
                    SessionId = doc.TryGetValue("sessionId", out var sessionId) ? sessionId?.ToString() ?? string.Empty : string.Empty,
                    ProductCode = doc.TryGetValue("productCode", out var product) ? product?.ToString() ?? string.Empty : string.Empty,
                    WorkspaceId = doc.TryGetValue("workspaceId", out var workspace) ? workspace?.ToString() ?? string.Empty : string.Empty,
                    UserId = doc.TryGetValue("userId", out var usr) ? usr?.ToString() ?? string.Empty : string.Empty,
                    UserPrompt = doc.TryGetValue("userPrompt", out var prompt) ? prompt?.ToString() ?? string.Empty : string.Empty,
                    AssistantResponse = doc.TryGetValue("assistantResponse", out var assistant) ? assistant?.ToString() ?? string.Empty : string.Empty,
                    ResponseLayer = doc.TryGetValue("responseLayer", out var layer) ? layer?.ToString() ?? string.Empty : string.Empty,
                    CacheKey = doc.TryGetValue("cacheKey", out var cache) ? cache?.ToString() ?? string.Empty : string.Empty,
                    OccurredAtUtc = TryReadDateTimeOffset(doc, "occurredAtUtc")
                });
            }

            return items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to search Experion conversations.");
            return [];
        }
    }

    private bool IsSearchConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.AiSearch.Endpoint)
               && !string.IsNullOrWhiteSpace(_options.AiSearch.ApiKey)
               && !string.IsNullOrWhiteSpace(_options.AiSearch.IndexName);
    }

    private static string EscapeODataValue(string value)
    {
        return (value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal);
    }

    private static DateTimeOffset TryReadDateTimeOffset(SearchDocument document, string key)
    {
        if (!document.TryGetValue(key, out var raw) || raw is null)
        {
            return DateTimeOffset.UtcNow;
        }

        if (raw is DateTimeOffset dto)
        {
            return dto;
        }

        if (raw is DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime());
        }

        if (DateTimeOffset.TryParse(raw.ToString(), out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private async Task<List<object>> BuildSearchDocumentsAsync(UploadedDocument document, CancellationToken cancellationToken)
    {
        var items = new List<object>();
        var fullText = await ResolveFullTextAsync(document, cancellationToken);
        var parsedJson = string.Empty;
        if (!string.IsNullOrWhiteSpace(document.ParsedJsonBlobPath))
        {
            try
            {
                parsedJson = await blobStorageService.DownloadTextAsync(
                                 document.ParsedJsonBlobPath,
                                 cancellationToken)
                             ?? string.Empty;
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
                fullText = fullText,
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

    private async Task<string> ResolveFullTextAsync(UploadedDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.FullTextBlobPath))
        {
            return string.Empty;
        }

        try
        {
            return await blobStorageService.DownloadTextAsync(document.FullTextBlobPath, cancellationToken) ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read full text blob for document {DocumentId}.", document.Id);
            return string.Empty;
        }
    }

    private sealed record ParsedPage(int PageNumber, string Content);
}
