using Experion.Backend.Models;
using Experion.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Experion.Backend.Controllers;

[ApiController]
[Route("api/experion-module")]
public sealed class ExperionModuleController(IExperionOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("session/bootstrap")]
    public async Task<ActionResult<ExperionBootstrapResponse>> Bootstrap(
        [FromBody] ExperionBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        ApplyRequestContext(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Unauthorized(LoginRequired());
        }

        var response = await orchestrator.BootstrapAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("context/resolve")]
    public async Task<ActionResult<ExperionContextResolveResponse>> ResolveContext(
        [FromBody] ExperionContextResolveRequest request,
        CancellationToken cancellationToken)
    {
        ApplyRequestContext(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Unauthorized(LoginRequired());
        }

        var response = await orchestrator.TriggerContextAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("conversation/message")]
    public async Task<ActionResult<ExperionMessageResponse>> SendMessage(
        [FromBody] ExperionMessageRequest request,
        CancellationToken cancellationToken)
    {
        ApplyRequestContext(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Unauthorized(LoginRequired());
        }

        var response = await orchestrator.SendMessageAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conversation/history")]
    [HttpGet("history")]
    public async Task<ActionResult<ExperionHistoryResponse>> GetHistory(
        [FromQuery] string userId,
        [FromQuery] string productCode,
        [FromQuery] string workspaceId,
        [FromQuery] Guid? conversationId,
        [FromQuery] int conversationTake = 20,
        [FromQuery] int messageTake = 120,
        CancellationToken cancellationToken = default)
    {
        userId = string.IsNullOrWhiteSpace(userId) ? HeaderValue("X-User-Id") : userId;
        productCode = string.IsNullOrWhiteSpace(productCode) ? HeaderValue("X-Product-Code") : productCode;
        workspaceId = string.IsNullOrWhiteSpace(workspaceId) ? HeaderValue("X-Workspace-Id") : workspaceId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(LoginRequired());
        }

        var response = await orchestrator.GetHistoryAsync(
            userId,
            productCode,
            workspaceId,
            conversationId,
            conversationTake,
            messageTake,
            cancellationToken);
        return Ok(response);
    }

    [HttpPost("actions/facebook-post")]
    public async Task<ActionResult<ExperionActionExecutionResponse>> PublishFacebookPost(
        [FromBody] FacebookPublishRequest request,
        CancellationToken cancellationToken)
    {
        ApplyRequestContext(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Unauthorized(LoginRequired());
        }

        var response = await orchestrator.PublishFacebookPostAsync(request, cancellationToken);
        return Ok(response);
    }

    private void ApplyRequestContext(ExperionBootstrapRequest request)
    {
        request.UserId = ResolveOrDefault(request.UserId, "X-User-Id");
        request.ProductCode = ResolveOrDefault(request.ProductCode, "X-Product-Code", "work");
        request.WorkspaceId = ResolveOrDefault(request.WorkspaceId, "X-Workspace-Id", "default");
    }

    private void ApplyRequestContext(ExperionContextResolveRequest request)
    {
        request.UserId = ResolveOrDefault(request.UserId, "X-User-Id");
        request.ProductCode = ResolveOrDefault(request.ProductCode, "X-Product-Code", "work");
        request.WorkspaceId = ResolveOrDefault(request.WorkspaceId, "X-Workspace-Id", "default");
    }

    private void ApplyRequestContext(ExperionMessageRequest request)
    {
        request.UserId = ResolveOrDefault(request.UserId, "X-User-Id");
        request.ProductCode = ResolveOrDefault(request.ProductCode, "X-Product-Code", "work");
        request.WorkspaceId = ResolveOrDefault(request.WorkspaceId, "X-Workspace-Id", "default");
    }

    private void ApplyRequestContext(FacebookPublishRequest request)
    {
        request.UserId = ResolveOrDefault(request.UserId, "X-User-Id");
        request.ProductCode = ResolveOrDefault(request.ProductCode, "X-Product-Code", "work");
        request.WorkspaceId = ResolveOrDefault(request.WorkspaceId, "X-Workspace-Id", "default");
    }

    private string ResolveOrDefault(string current, string headerName, string fallback = "")
    {
        var header = HeaderValue(headerName);
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            return current.Trim();
        }

        return fallback;
    }

    private string HeaderValue(string headerName)
    {
        if (Request.Headers.TryGetValue(headerName, out var values))
        {
            return values.ToString().Trim();
        }

        return string.Empty;
    }

    private object LoginRequired()
    {
        return new
        {
            code = "login_required",
            message = "Logged-in user is required.",
            traceId = HttpContext.TraceIdentifier
        };
    }
}

