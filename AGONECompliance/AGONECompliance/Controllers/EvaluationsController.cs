using AGONECompliance.Data;
using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EvaluationsController(
    ComplianceDbContext dbContext,
    IEvaluationOrchestrator orchestrator,
    IEvaluationReportPdfBuilder reportPdfBuilder) : ControllerBase
{
    static EvaluationsController()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpPost("start")]
    public async Task<ActionResult<EvaluationRunDto>> Start(
        [FromBody] StartEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EvaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        if (request.RuleIds.Count == 0)
        {
            return BadRequest("Select at least one rule.");
        }

        var runId = await orchestrator.QueueEvaluationAsync(
            request.EvaluationWorkspaceId,
            request.ProspectusDocumentId,
            request.RuleIds,
            cancellationToken);

        var dto = await GetRunInternalAsync(request.EvaluationWorkspaceId, runId, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EvaluationRunDto>> GetById(
        Guid id,
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var dto = await GetRunInternalAsync(evaluationWorkspaceId, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvaluationRunDto>>> GetAll(
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var runs = await dbContext.EvaluationRuns
            .Include(x => x.ProspectusDocument)
            .Where(x => x.EvaluationWorkspaceId == evaluationWorkspaceId)
            .OrderByDescending(x => x.Id)
            .Take(25)
            .Select(x => new EvaluationRunDto
            {
                Id = x.Id,
                EvaluationWorkspaceId = x.EvaluationWorkspaceId,
                ProspectusDocumentId = x.ProspectusDocumentId,
                ProspectusBlobPath = x.ProspectusDocument.BlobPath,
                Status = x.Status,
                FailureReason = x.FailureReason,
                CreatedAtUtc = x.CreatedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(runs);
    }

    [HttpGet("{id:guid}/report")]
    public async Task<ActionResult<ComplianceReportDto>> GetReport(
        Guid id,
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var dto = await GetRunInternalAsync(evaluationWorkspaceId, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var workspaceName = await dbContext.EvaluationWorkspaces
            .Where(x => x.Id == evaluationWorkspaceId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown Workspace";
        var generatedAtUtc = DateTimeOffset.UtcNow;

        return Ok(new ComplianceReportDto
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            EvaluationRunId = dto.Id,
            WorkspaceName = workspaceName,
            RunLabel = dto.Id.ToString("N")[..8],
            HeaderTitle = "AG ONE Compliance Evaluation Report",
            FooterText = "Aventra Group",
            GeneratedAtUtc = generatedAtUtc,
            TotalRules = dto.Results.Count,
            CompliantCount = dto.Results.Count(x => x.Status == ComplianceStatus.Compliant),
            NonCompliantCount = dto.Results.Count(x => x.Status == ComplianceStatus.NonCompliant),
            NeedsReviewCount = dto.Results.Count(x => x.Status == ComplianceStatus.NeedsReview),
            Items = dto.Results
        });
    }

    [HttpGet("{id:guid}/report/pdf")]
    [HttpGet("{id:guid}/report-pdf")]
    public async Task<IActionResult> GetReportPdf(
        Guid id,
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var dto = await GetRunInternalAsync(evaluationWorkspaceId, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var workspaceName = await dbContext.EvaluationWorkspaces
            .Where(x => x.Id == evaluationWorkspaceId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown Workspace";
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var runLabel = dto.Id.ToString("N")[..8];
        var report = new ComplianceReportDto
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            EvaluationRunId = dto.Id,
            WorkspaceName = workspaceName,
            RunLabel = runLabel,
            HeaderTitle = "AG ONE Compliance Evaluation Report",
            FooterText = "Aventra Group",
            GeneratedAtUtc = generatedAtUtc,
            TotalRules = dto.Results.Count,
            CompliantCount = dto.Results.Count(x => x.Status == ComplianceStatus.Compliant),
            NonCompliantCount = dto.Results.Count(x => x.Status == ComplianceStatus.NonCompliant),
            NeedsReviewCount = dto.Results.Count(x => x.Status == ComplianceStatus.NeedsReview),
            Items = dto.Results
        };

        var bytes = reportPdfBuilder.Build(report, workspaceName, runLabel);
        return File(
            bytes,
            "application/pdf",
            $"compliance-report-{report.EvaluationRunId:N}.pdf");
    }

    private async Task<EvaluationRunDto?> GetRunInternalAsync(
        Guid evaluationWorkspaceId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.EvaluationRuns
            .Include(x => x.ProspectusDocument)
            .Include(x => x.Results)
            .ThenInclude(x => x.Rule)
            .FirstOrDefaultAsync(
                x => x.Id == runId && x.EvaluationWorkspaceId == evaluationWorkspaceId,
                cancellationToken);
        if (run is null)
        {
            return null;
        }

        return new EvaluationRunDto
        {
            Id = run.Id,
            EvaluationWorkspaceId = run.EvaluationWorkspaceId,
            ProspectusDocumentId = run.ProspectusDocumentId,
            ProspectusBlobPath = run.ProspectusDocument.BlobPath,
            Status = run.Status,
            FailureReason = run.FailureReason,
            CreatedAtUtc = run.CreatedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            Results = run.Results
                .OrderBy(x => x.Rule.Code)
                .Select(x => new EvaluationResultItemDto
                {
                    RuleId = x.RuleId,
                    RuleCode = x.Rule.Code,
                    RuleTitle = x.Rule.Title,
                    RuleCategory = x.Rule.ClassificationCategory,
                    RuleActionParty = x.Rule.ActionParty,
                    GuideReference = x.Rule.Reference,
                    Status = x.Status,
                    Reason = x.Reason,
                    EvidenceExcerpt = x.EvidenceExcerpt,
                    EvidenceLocation = x.EvidenceLocation,
                    PageNumber = x.PageNumber,
                    EvidenceLink = $"/api/documents/{run.ProspectusDocumentId}/content#page={x.PageNumber ?? 1}",
                    ConfidenceScore = x.ConfidenceScore
                }).ToList()
        };
    }
}
