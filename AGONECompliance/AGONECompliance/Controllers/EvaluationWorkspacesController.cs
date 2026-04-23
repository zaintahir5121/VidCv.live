using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/evaluation-workspaces")]
public sealed class EvaluationWorkspacesController(ComplianceDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvaluationWorkspaceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await dbContext.EvaluationWorkspaces
            .OrderByDescending(x => x.Id)
            .Select(x => new EvaluationWorkspaceDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<EvaluationWorkspaceDto>> Create(
        [FromBody] CreateEvaluationWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Evaluation workspace name is required.");
        }

        var workspace = new EvaluationWorkspace
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Status = "Draft"
        };
        dbContext.EvaluationWorkspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new EvaluationWorkspaceDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            Status = workspace.Status,
            CreatedAtUtc = workspace.CreatedAtUtc
        });
    }
}
