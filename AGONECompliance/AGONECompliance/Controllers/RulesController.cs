using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RulesController(
    ComplianceDbContext dbContext,
    IRuleGenerationOrchestrator ruleGenerationOrchestrator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComplianceRuleDto>>> GetAll(
        [FromQuery] Guid evaluationWorkspaceId,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var items = await dbContext.ComplianceRules
            .Where(x => x.EvaluationWorkspaceId == evaluationWorkspaceId)
            .OrderBy(x => x.Code)
            .Select(x => new ComplianceRuleDto
            {
                Id = x.Id,
                EvaluationWorkspaceId = x.EvaluationWorkspaceId,
                Code = x.Code,
                Title = x.Title,
                Reference = x.Reference,
                RequirementText = x.RequirementText,
                ClassificationCategory = x.ClassificationCategory,
                ActionParty = x.ActionParty,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<IReadOnlyList<ComplianceRuleDto>>> Generate(
        [FromBody] GenerateRulesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EvaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        if (!await dbContext.EvaluationWorkspaces.AnyAsync(x => x.Id == request.EvaluationWorkspaceId, cancellationToken))
        {
            return NotFound("Evaluation workspace not found.");
        }

        var jobId = await ruleGenerationOrchestrator.QueueRuleGenerationAsync(
            request,
            cancellationToken);
        return Accepted(new GenerateRulesResponse
        {
            Message = "Rule generation queued in background job.",
            BackgroundJobId = jobId
        });
    }

    [HttpPut("{id:guid}/active")]
    public async Task<ActionResult<ComplianceRuleDto>> SetRuleActive(
        Guid id,
        [FromQuery] Guid evaluationWorkspaceId,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
    {
        if (evaluationWorkspaceId == Guid.Empty)
        {
            return BadRequest("evaluationWorkspaceId is required.");
        }

        var rule = await dbContext.ComplianceRules.FirstOrDefaultAsync(
            x => x.Id == id && x.EvaluationWorkspaceId == evaluationWorkspaceId,
            cancellationToken);
        if (rule is null)
        {
            return NotFound();
        }

        rule.IsActive = isActive;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ComplianceRuleDto
        {
            Id = rule.Id,
            EvaluationWorkspaceId = rule.EvaluationWorkspaceId,
            Code = rule.Code,
            Title = rule.Title,
            Reference = rule.Reference,
            RequirementText = rule.RequirementText,
            ClassificationCategory = rule.ClassificationCategory,
            ActionParty = rule.ActionParty,
            IsActive = rule.IsActive
        });
    }
}
