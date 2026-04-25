using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExperionController(IExperionService experionService) : ControllerBase
{
    private ExperionRequestContext BuildContext()
    {
        return new ExperionRequestContext
        {
            ProductCode = Request.Headers["X-AGONE-Product"].FirstOrDefault() ?? "unknown",
            WorkspaceId = Request.Headers["X-AGONE-WorkspaceId"].FirstOrDefault() ?? string.Empty,
            Source = Request.Headers["Origin"].FirstOrDefault() ?? Request.Host.Value,
            UserId = User?.Identity?.Name ?? "anonymous",
            TraceId = HttpContext.TraceIdentifier
        };
    }

    [HttpPost("session/bootstrap")]
    public async Task<ActionResult<ExperionSessionBootstrapResponse>> Bootstrap(
        [FromBody] ExperionSessionBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PageUrl))
        {
            return BadRequest("pageUrl is required.");
        }

        var context = BuildContext();
        var response = await experionService.BootstrapSessionAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpPost("context/trigger")]
    public async Task<ActionResult<ExperionContextTriggerResponse>> Trigger(
        [FromBody] ExperionContextTriggerRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        var context = BuildContext();
        var response = await experionService.TriggerContextAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpPost("conversation/message")]
    public async Task<ActionResult<ExperionConversationMessageResponse>> Message(
        [FromBody] ExperionConversationMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("message is required.");
        }

        var context = BuildContext();
        var response = await experionService.SendMessageAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpPost("action/execute")]
    public async Task<ActionResult<ExperionActionExecuteResponse>> ExecuteAction(
        [FromBody] ExperionActionExecuteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ActionName))
        {
            return BadRequest("actionName is required.");
        }

        var context = BuildContext();
        var response = await experionService.ExecuteActionAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpGet("action/{executionId}/status")]
    public async Task<ActionResult<ExperionActionStatusDto>> GetActionStatus(
        string executionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            return BadRequest("executionId is required.");
        }

        var context = BuildContext();
        if (!Guid.TryParse(executionId, out var parsedExecutionId))
        {
            return BadRequest("executionId must be a valid guid.");
        }

        var status = await experionService.GetActionStatusAsync(parsedExecutionId, context, cancellationToken);
        return Ok(status);
    }

    [HttpGet("audit")]
    public async Task<ActionResult<IReadOnlyList<ExperionAuditEventDto>>> GetAudit(
        [FromQuery] string sessionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("sessionId is required.");
        }

        var context = BuildContext();
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            return BadRequest("sessionId must be a valid guid.");
        }

        var events = await experionService.GetAuditEventsAsync(parsedSessionId, context, cancellationToken);
        return Ok(events);
    }
}
