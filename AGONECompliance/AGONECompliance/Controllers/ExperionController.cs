using AGONECompliance.Services;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExperionController(IExperionService experionService) : ControllerBase
{
    private ExperionRequestContext BuildContext()
    {
        var headerUserId = Request.Headers["X-AGONE-UserId"].FirstOrDefault();
        var claimUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User?.Identity?.Name;
        var hasSourceToken = !string.IsNullOrWhiteSpace(Request.Headers["X-AGONE-SourceToken"].FirstOrDefault());
        var hasAuthenticatedPrincipal = User?.Identity?.IsAuthenticated == true;
        var resolvedUserId = hasAuthenticatedPrincipal
            ? (claimUserId ?? userName ?? string.Empty)
            : (hasSourceToken ? (headerUserId ?? string.Empty) : string.Empty);

        return new ExperionRequestContext
        {
            ProductCode = Request.Headers["X-AGONE-Product"].FirstOrDefault() ?? "unknown",
            WorkspaceId = Request.Headers["X-AGONE-WorkspaceId"].FirstOrDefault() ?? string.Empty,
            Source = Request.Headers["Origin"].FirstOrDefault() ?? Request.Host.Value,
            UserId = resolvedUserId,
            TraceId = HttpContext.TraceIdentifier
        };
    }

    private bool TryGetAuthenticatedContext(out ExperionRequestContext context, out ActionResult? unauthorizedResult)
    {
        context = BuildContext();
        if (!string.IsNullOrWhiteSpace(context.UserId))
        {
            unauthorizedResult = null;
            return true;
        }

        unauthorizedResult = Unauthorized(new ExperionErrorResponse
        {
            Code = "experion_auth_required",
            Message = "Experion is available only for logged-in users.",
            TraceId = HttpContext.TraceIdentifier,
            IsRetryable = false,
            ResolutionHint = "Sign in to AG ONE and provide authenticated identity or source token with X-AGONE-UserId."
        });
        return false;
    }

    [HttpPost("session/bootstrap")]
    public async Task<ActionResult<ExperionSessionBootstrapResponse>> Bootstrap(
        [FromBody] ExperionSessionBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (string.IsNullOrWhiteSpace(request.PageUrl))
        {
            return BadRequest("pageUrl is required.");
        }

        var response = await experionService.BootstrapSessionAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpPost("context/trigger")]
    public async Task<ActionResult<ExperionContextTriggerResponse>> Trigger(
        [FromBody] ExperionContextTriggerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await experionService.TriggerContextAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpPost("conversation/message")]
    public async Task<ActionResult<ExperionConversationMessageResponse>> Message(
        [FromBody] ExperionConversationMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("message is required.");
        }

        var response = await experionService.SendMessageAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conversation/history")]
    public async Task<ActionResult<ExperionConversationHistoryResponse>> GetConversationHistory(
        [FromQuery] Guid? conversationId,
        [FromQuery] int conversationTake = 20,
        [FromQuery] int messageTake = 100,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        var response = await experionService.GetConversationHistoryAsync(
            new ExperionConversationHistoryRequest
            {
                ConversationId = conversationId,
                ConversationTake = conversationTake,
                MessageTake = messageTake
            },
            context,
            cancellationToken);
        return Ok(response);
    }

    [HttpPost("action/execute")]
    public async Task<ActionResult<ExperionActionExecuteResponse>> ExecuteAction(
        [FromBody] ExperionActionExecuteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (request.SessionId == Guid.Empty)
        {
            return BadRequest("sessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ActionName))
        {
            return BadRequest("actionName is required.");
        }

        var response = await experionService.ExecuteActionAsync(request, context, cancellationToken);
        return Ok(response);
    }

    [HttpGet("action/{executionId}/status")]
    public async Task<ActionResult<ExperionActionStatusDto>> GetActionStatus(
        string executionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (string.IsNullOrWhiteSpace(executionId))
        {
            return BadRequest("executionId is required.");
        }

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
        if (!TryGetAuthenticatedContext(out var context, out var unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("sessionId is required.");
        }

        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            return BadRequest("sessionId must be a valid guid.");
        }

        var events = await experionService.GetAuditEventsAsync(parsedSessionId, context, cancellationToken);
        return Ok(events);
    }
}
