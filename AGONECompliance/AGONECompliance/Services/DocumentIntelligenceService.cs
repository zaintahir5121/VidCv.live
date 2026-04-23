using AGONECompliance.Options;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AGONECompliance.Services;

public sealed class DocumentIntelligenceService(
    IOptions<AzureOptions> azureOptions,
    ILogger<DocumentIntelligenceService> logger) : IDocumentIntelligenceService
{
    private readonly AzureOptions _options = azureOptions.Value;

    public async Task<ProcessedDocument> ExtractTextAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    || contentType.Equals("application/x-pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            throw new InvalidOperationException("Only PDF files are supported for extraction.");
        }

        if (string.IsNullOrWhiteSpace(_options.DocumentIntelligence.Endpoint)
            || string.IsNullOrWhiteSpace(_options.DocumentIntelligence.ApiKey))
        {
            return await FallbackExtractAsync(stream, cancellationToken);
        }

        try
        {
            var client = new DocumentIntelligenceClient(
                new Uri(_options.DocumentIntelligence.Endpoint),
                new AzureKeyCredential(_options.DocumentIntelligence.ApiKey));

            var binaryData = await BinaryData.FromStreamAsync(stream, cancellationToken);
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                _options.DocumentIntelligence.ModelId,
                binaryData,
                cancellationToken: cancellationToken);

            var result = operation.Value;
            var pageBlocks = new List<object>();
            var textBuilder = new StringBuilder();

            foreach (var page in result.Pages)
            {
                textBuilder.AppendLine($"[Page {page.PageNumber}]");
                var pageText = string.Join(
                    Environment.NewLine,
                    page.Lines.Select(x => x.Content).Where(x => !string.IsNullOrWhiteSpace(x)));

                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();

                pageBlocks.Add(new
                {
                    pageNumber = page.PageNumber,
                    content = pageText
                });
            }

            return new ProcessedDocument
            {
                FullText = textBuilder.ToString(),
                ParsedJson = JsonSerializer.Serialize(new { pages = pageBlocks })
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document Intelligence failed for scanned PDF OCR extraction, falling back to placeholder extraction.");
            stream.Position = 0;
            return await FallbackExtractAsync(stream, cancellationToken);
        }
    }

    private static async Task<ProcessedDocument> FallbackExtractAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var placeholder =
            $"OCR extraction unavailable in fallback mode. Uploaded PDF binary length: {bytes.Length} bytes. Configure Azure Document Intelligence to extract scanned-image PDF text.";

        return new ProcessedDocument
        {
            FullText = placeholder,
            ParsedJson = JsonSerializer.Serialize(new
            {
                pages = new[]
                {
                    new
                    {
                        pageNumber = 1,
                        content = placeholder
                    }
                }
            })
        };
    }
}
