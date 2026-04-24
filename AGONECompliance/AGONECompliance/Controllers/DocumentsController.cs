using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController(
    ComplianceDbContext dbContext,
    IBlobStorageService blobStorageService,
    IDocumentIntelligenceService documentIntelligenceService,
    IComplianceSearchService searchService) : ControllerBase
{
    private static readonly HashSet<string> AllowedPdfContentTypes =
    [
        "application/pdf",
        "application/x-pdf"
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetAll(
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var items = await dbContext.UploadedDocuments
            .Where(x => x.EvaluationWorkspaceId == evaluationWorkspaceId)
            .OrderByDescending(x => x.Id)
            .Select(x => new DocumentDto
            {
                Id = x.Id,
                EvaluationWorkspaceId = x.EvaluationWorkspaceId,
                Type = x.Type,
                OriginalFileName = x.OriginalFileName,
                ContentType = x.ContentType,
                SizeBytes = x.SizeBytes,
                UploadedAtUtc = x.CreatedAtUtc,
                IsProcessed = x.IsProcessed,
                BlobPath = x.BlobPath
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(
        [FromQuery] Guid evaluationWorkspaceId,
        [FromQuery] DocumentType type,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var workspaceExists = await dbContext.EvaluationWorkspaces.AnyAsync(
            x => x.Id == evaluationWorkspaceId,
            cancellationToken);
        if (!workspaceExists)
        {
            return NotFound("Evaluation workspace not found.");
        }

        if (file.Length == 0)
        {
            return BadRequest("Uploaded file is empty.");
        }

        var isPdfByName = file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var normalizedType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        var isPdfByType = AllowedPdfContentTypes.Contains(normalizedType);
        var hasGenericType = string.IsNullOrWhiteSpace(normalizedType) || normalizedType == "application/octet-stream";
        if (!isPdfByName || (!isPdfByType && !hasGenericType))
        {
            return BadRequest("Only PDF files are allowed. Scanned/image PDFs are supported.");
        }

        await using var sourceStream = file.OpenReadStream();
        await using var storageStream = new MemoryStream();
        await sourceStream.CopyToAsync(storageStream, cancellationToken);
        storageStream.Position = 0;

        var effectiveContentType = "application/pdf";
        var blobPath = await blobStorageService.UploadAsync(
            storageStream,
            effectiveContentType,
            file.FileName,
            cancellationToken);

        storageStream.Position = 0;
        var processed = await documentIntelligenceService.ExtractTextAsync(
            storageStream,
            effectiveContentType,
            cancellationToken);

        var parsedJsonBlobPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(processed.ParsedJson))
        {
            await using var parsedJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(processed.ParsedJson));
            parsedJsonBlobPath = await blobStorageService.UploadAsync(
                parsedJsonStream,
                "application/json",
                $"{Path.GetFileNameWithoutExtension(file.FileName)}-parsed.json",
                cancellationToken,
                folderPath: $"parsed-json/{evaluationWorkspaceId:N}");
        }

        var document = new UploadedDocument
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            Type = type,
            OriginalFileName = file.FileName,
            ContentType = effectiveContentType,
            SizeBytes = file.Length,
            BlobPath = blobPath,
            FullText = processed.FullText,
            ParsedJsonBlobPath = parsedJsonBlobPath,
            IsProcessed = true
        };

        dbContext.UploadedDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await searchService.IndexDocumentAsync(document, cancellationToken);

        return Ok(new UploadDocumentResponse
        {
            DocumentId = document.Id,
            EvaluationWorkspaceId = document.EvaluationWorkspaceId,
            Message = $"{type} document uploaded and parsed successfully."
        });
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.UploadedDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var (stream, contentType) = await blobStorageService.DownloadAsync(
            document.BlobPath,
            string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType,
            cancellationToken);

        return File(stream, contentType, document.OriginalFileName, enableRangeProcessing: true);
    }
}
