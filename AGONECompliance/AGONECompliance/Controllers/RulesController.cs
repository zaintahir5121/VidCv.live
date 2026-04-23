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
    IComplianceAiService aiService) : ControllerBase
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

        var guideText = string.Empty;
        if (request.GuideDocumentId.HasValue)
        {
            guideText = await dbContext.UploadedDocuments
                .Where(x =>
                    x.Id == request.GuideDocumentId.Value
                    && x.EvaluationWorkspaceId == request.EvaluationWorkspaceId
                    && x.Type == DocumentType.Guide)
                .Select(x => x.FullText ?? string.Empty)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var appendixText = string.Empty;
        if (request.AppendixDocumentId.HasValue)
        {
            appendixText = await dbContext.UploadedDocuments
                .Where(x =>
                    x.Id == request.AppendixDocumentId.Value
                    && x.EvaluationWorkspaceId == request.EvaluationWorkspaceId
                    && x.Type == DocumentType.Appendix)
                .Select(x => x.FullText ?? string.Empty)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var generated = await aiService.GenerateRulesAsync(
            guideText ?? string.Empty,
            appendixText ?? string.Empty,
            cancellationToken);
        if (generated.Count == 0)
        {
            return BadRequest("No rules were generated from the supplied documents.");
        }

        if (request.ReplaceExistingRules)
        {
            var existingRules = await dbContext.ComplianceRules
                .Where(x => x.EvaluationWorkspaceId == request.EvaluationWorkspaceId)
                .ToListAsync(cancellationToken);
            dbContext.ComplianceRules.RemoveRange(existingRules);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingCodes = await dbContext.ComplianceRules
            .Where(x => x.EvaluationWorkspaceId == request.EvaluationWorkspaceId)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in generated)
        {
            var baseCode = rule.Code;
            var counter = 1;
            while (existingSet.Contains(rule.Code))
            {
                rule.Code = $"{baseCode}-{counter++}";
            }

            rule.EvaluationWorkspaceId = request.EvaluationWorkspaceId;
            existingSet.Add(rule.Code);
            dbContext.ComplianceRules.Add(rule);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var output = generated.Select(x => new ComplianceRuleDto
        {
            Id = x.Id,
            EvaluationWorkspaceId = x.EvaluationWorkspaceId,
            Code = x.Code,
            Title = x.Title,
            Reference = x.Reference,
            RequirementText = x.RequirementText,
            IsActive = x.IsActive
        }).ToList();

        return Ok(output);
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
            IsActive = rule.IsActive
        });
    }
}
