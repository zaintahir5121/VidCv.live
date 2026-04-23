using AGONECompliance.Domain;
using AGONECompliance.Options;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;

namespace AGONECompliance.Services;

public sealed class ComplianceSearchService(
    IOptions<AzureOptions> azureOptions,
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
                    new SimpleField("type", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                    new SearchableField("fileName") { IsFilterable = true, IsSortable = true },
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

            var payload = new[]
            {
                new
                {
                    id = document.Id.ToString(),
                    type = document.Type.ToString(),
                    fileName = document.OriginalFileName,
                    fullText = document.FullText ?? string.Empty,
                    uploadedAtUtc = document.CreatedAtUtc
                }
            };

            await searchClient.UploadDocumentsAsync(payload, cancellationToken: cancellationToken);
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
}
