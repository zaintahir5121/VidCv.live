using AGONECompliance.Data;
using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentEvidenceController(
    ComplianceDbContext dbContext,
    IBlobStorageService blobStorageService) : ControllerBase
{
    [HttpGet("{id:guid}/pages/{pageNumber:int}")]
    public async Task<ActionResult<DocumentPageEvidenceDto>> GetPageEvidence(
        Guid id,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0)
        {
            return BadRequest("pageNumber must be greater than zero.");
        }

        var document = await dbContext.UploadedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound("Document not found.");
        }

        var pageBlob = await dbContext.DocumentPageBlobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DocumentId == id && x.PageNumber == pageNumber,
                cancellationToken);
        if (pageBlob is null)
        {
            return NotFound("Page evidence not found.");
        }

        var pageText = pageBlob.ExtractedText;
        if (string.IsNullOrWhiteSpace(pageText))
        {
            pageText = await blobStorageService.DownloadTextAsync(pageBlob.BlobPath, cancellationToken) ?? string.Empty;
        }

        return Ok(new DocumentPageEvidenceDto
        {
            DocumentId = document.Id,
            PageNumber = pageNumber,
            BlobPath = pageBlob.BlobPath,
            OriginalFileName = document.OriginalFileName,
            PdfPageLink = $"/api/documents/{document.Id}/content#page={pageNumber}",
            ExtractedText = pageText
        });
    }

    [HttpGet("{id:guid}/pages/{pageNumber:int}/content")]
    public async Task<IActionResult> GetPageTextContent(
        Guid id,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0)
        {
            return BadRequest("pageNumber must be greater than zero.");
        }

        var pageBlob = await dbContext.DocumentPageBlobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DocumentId == id && x.PageNumber == pageNumber,
                cancellationToken);
        if (pageBlob is null)
        {
            return NotFound("Page content not found.");
        }

        var (stream, contentType) = await blobStorageService.DownloadAsync(
            pageBlob.BlobPath,
            "text/plain",
            cancellationToken);
        return File(stream, contentType);
    }
}
