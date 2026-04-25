using System.Text;
using System.Text.Json;
using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Services;

public sealed class DocumentProcessingOrchestrator(
    ComplianceDbContext dbContext,
    IBlobStorageService blobStorageService,
    IDocumentIntelligenceService documentIntelligenceService,
    IComplianceSearchService searchService,
    ILogger<DocumentProcessingOrchestrator> logger) : IDocumentProcessingOrchestrator
{
    private const string ProcessingType = "DocumentParsing";

    public async Task<Guid> QueueDocumentProcessingAsync(
        Guid evaluationWorkspaceId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await dbContext.UploadedDocuments.FirstOrDefaultAsync(
            x => x.Id == documentId && x.EvaluationWorkspaceId == evaluationWorkspaceId,
            cancellationToken);
        if (document is null)
        {
            throw new InvalidOperationException("Uploaded document not found for workspace.");
        }

        document.IsProcessed = false;
        document.ProcessingError = null;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var job = new BackgroundJobRun
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            JobType = ProcessingType,
            Status = "Queued",
            RelatedDocumentId = documentId,
            Message = $"Queued parsing for {document.OriginalFileName}"
        };

        dbContext.BackgroundJobRuns.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task ProcessNextPendingJobAsync(CancellationToken cancellationToken)
    {
        var job = await dbContext.BackgroundJobRuns
            .Where(x => x.JobType == ProcessingType && x.Status == "Queued")
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return;
        }

        job.Status = "Running";
        job.StartedAtUtc = DateTimeOffset.UtcNow;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (job.RelatedDocumentId is null)
            {
                throw new InvalidOperationException("Background job is missing a valid document identifier.");
            }
            var documentId = job.RelatedDocumentId.Value;

            var document = await dbContext.UploadedDocuments.FirstOrDefaultAsync(
                x => x.Id == documentId && x.EvaluationWorkspaceId == job.EvaluationWorkspaceId,
                cancellationToken);
            if (document is null)
            {
                throw new InvalidOperationException("Document no longer exists.");
            }

            var (pdfStream, contentType) = await blobStorageService.DownloadAsync(
                document.BlobPath,
                document.ContentType,
                cancellationToken);
            await using (pdfStream.ConfigureAwait(false))
            {
                var processed = await documentIntelligenceService.ExtractTextAsync(
                    pdfStream,
                    contentType,
                    cancellationToken);

                var fullTextBlobPath = string.Empty;
                if (!string.IsNullOrWhiteSpace(processed.FullText))
                {
                    await using var fullTextStream = new MemoryStream(Encoding.UTF8.GetBytes(processed.FullText));
                    var fullTextLogicalName = $"doc-{document.Id.ToString("N")[..8]}-fulltext.txt";
                    fullTextBlobPath = await blobStorageService.UploadAsync(
                        fullTextStream,
                        "text/plain",
                        fullTextLogicalName,
                        cancellationToken,
                        folderPath: $"processed-text/ws-{document.EvaluationWorkspaceId:N}");
                }

                var parsedJsonBlobPath = string.Empty;
                if (!string.IsNullOrWhiteSpace(processed.ParsedJson))
                {
                    await using var parsedJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(processed.ParsedJson));
                    var parsedJsonLogicalName = $"doc-{document.Id.ToString("N")[..8]}-layout.json";
                    parsedJsonBlobPath = await blobStorageService.UploadAsync(
                        parsedJsonStream,
                        "application/json",
                        parsedJsonLogicalName,
                        cancellationToken,
                        folderPath: $"parsed-json/ws-{document.EvaluationWorkspaceId:N}");
                }

                document.FullTextBlobPath = fullTextBlobPath;
                document.ParsedJsonBlobPath = parsedJsonBlobPath;
                document.IsProcessed = true;
                document.ProcessingError = null;
                document.UpdatedAtUtc = DateTimeOffset.UtcNow;

                await PersistPageBlobsAsync(document, processed, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var updatedDocument = await dbContext.UploadedDocuments
                .FirstAsync(x => x.Id == documentId, cancellationToken);
            await searchService.IndexDocumentAsync(updatedDocument, cancellationToken);

            job.Status = "Completed";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.Message = "Document parsing and indexing completed.";
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing document job {JobId}.", job.Id);
            job.Status = "Failed";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.FailureReason = ex.Message;
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (job.RelatedDocumentId is Guid failedDocumentId)
            {
                var document = await dbContext.UploadedDocuments.FirstOrDefaultAsync(
                    x => x.Id == failedDocumentId && x.EvaluationWorkspaceId == job.EvaluationWorkspaceId,
                    cancellationToken);
                if (document is not null)
                {
                    document.IsProcessed = false;
                    document.ProcessingError = ex.Message;
                    document.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PersistPageBlobsAsync(
        UploadedDocument document,
        ProcessedDocument processed,
        CancellationToken cancellationToken)
    {
        if (document.Type != DocumentType.Prospectus)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(processed.ParsedJson))
        {
            return;
        }

        var pageItems = ParsePages(processed.ParsedJson);
        if (pageItems.Count == 0)
        {
            return;
        }

        var existingPageBlobs = await dbContext.DocumentPageBlobs
            .Where(x => x.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        if (existingPageBlobs.Count > 0)
        {
            dbContext.DocumentPageBlobs.RemoveRange(existingPageBlobs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var page in pageItems)
        {
            if (string.IsNullOrWhiteSpace(page.Content))
            {
                continue;
            }

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(page.Content));
            var logicalName = $"doc-{document.Id:N}-p{page.PageNumber:D4}.txt";
            var blobPath = await blobStorageService.UploadAsync(
                stream,
                "text/plain",
                logicalName,
                cancellationToken,
                folderPath: $"processed-pages/ws-{document.EvaluationWorkspaceId:N}/doc-{document.Id:N}");

            dbContext.DocumentPageBlobs.Add(new DocumentPageBlob
            {
                EvaluationWorkspaceId = document.EvaluationWorkspaceId,
                DocumentId = document.Id,
                PageNumber = page.PageNumber,
                BlobPath = blobPath,
                ExtractedText = page.Content
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<PageTextItem> ParsePages(string parsedJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(parsedJson);
            if (!doc.RootElement.TryGetProperty("pages", out var pages)
                || pages.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var output = new List<PageTextItem>();
            foreach (var page in pages.EnumerateArray())
            {
                var pageNumber = page.TryGetProperty("pageNumber", out var numberElement)
                    && numberElement.TryGetInt32(out var parsed)
                    ? parsed
                    : 1;
                var content = page.TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : string.Empty;
                output.Add(new PageTextItem(pageNumber, content));
            }

            return output;
        }
        catch
        {
            return [];
        }
    }

    private sealed record PageTextItem(int PageNumber, string Content);
}
