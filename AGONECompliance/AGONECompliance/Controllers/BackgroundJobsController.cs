using AGONECompliance.Data;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/background-jobs")]
public sealed class BackgroundJobsController(ComplianceDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BackgroundJobDto>>> GetAll(
        [FromQuery] Guid? evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BackgroundJobRuns
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsQueryable();

        if (evaluationWorkspaceId.HasValue && evaluationWorkspaceId.Value != Guid.Empty)
        {
            query = query.Where(x => x.EvaluationWorkspaceId == evaluationWorkspaceId.Value);
        }

        var items = await query
            .Take(100)
            .Select(x => new BackgroundJobDto
            {
                Id = x.Id,
                EvaluationWorkspaceId = x.EvaluationWorkspaceId,
                JobType = x.JobType,
                Status = x.Status,
                RelatedDocumentId = x.RelatedDocumentId,
                RelatedEvaluationRunId = x.RelatedEvaluationRunId,
                Message = x.Message,
                FailureReason = x.FailureReason,
                CreatedAtUtc = x.CreatedAtUtc,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
