using AGONECompliance.Data;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PromptTemplatesController(ComplianceDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromptTemplateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var templates = await dbContext.PromptTemplates
            .OrderBy(x => x.TemplateType)
            .ThenByDescending(x => x.Version)
            .Select(x => new PromptTemplateDto
            {
                Id = x.Id,
                TemplateType = x.TemplateType,
                Name = x.Name,
                Description = x.Description,
                Version = x.Version,
                IsActive = x.IsActive,
                SystemPrompt = x.SystemPrompt,
                UserPromptFormat = x.UserPromptFormat
            })
            .ToListAsync(cancellationToken);

        return Ok(templates);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PromptTemplateDto>> Update(
        Guid id,
        [FromBody] UpdatePromptTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.PromptTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        template.Name = request.Name.Trim();
        template.Description = request.Description.Trim();
        template.IsActive = request.IsActive;
        template.SystemPrompt = request.SystemPrompt.Trim();
        template.UserPromptFormat = request.UserPromptFormat.Trim();
        template.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new PromptTemplateDto
        {
            Id = template.Id,
            TemplateType = template.TemplateType,
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            IsActive = template.IsActive,
            SystemPrompt = template.SystemPrompt,
            UserPromptFormat = template.UserPromptFormat
        });
    }
}
