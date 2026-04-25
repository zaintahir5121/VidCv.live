using AGONECompliance.Shared;

namespace AGONECompliance.Services;

public sealed class ExperionService : IExperionService
{
    private static readonly string[] DefaultSuggestedPrompts =
    [
        "Help me complete this step",
        "Explain what this section means",
        "What can you do on this page?"
    ];

    public Task<ExperionSessionBootstrapResponse> BootstrapSessionAsync(
        ExperionSessionBootstrapRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var productCode = NormalizeProductCode(context.ProductCode);
        var workspaceId = string.IsNullOrWhiteSpace(context.WorkspaceId)
            ? "default"
            : context.WorkspaceId.Trim();

        var response = new ExperionSessionBootstrapResponse
        {
            SessionId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            ProductCode = productCode,
            WorkspaceId = workspaceId,
            TtlSeconds = 1800,
            RequiresCriticalConfirmation = true,
            ConsentFlags = new ExperionConsentFlagsDto
            {
                ProfileDataAllowed = true,
                ProductDataAllowed = true,
                DocumentDataAllowed = true
            },
            AllowedActions =
            [
                $"{productCode}.assist",
                $"{productCode}.explain",
                $"{productCode}.propose-action"
            ],
        };

        return Task.FromResult(response);
    }

    public Task<ExperionContextTriggerResponse> TriggerContextAsync(
        ExperionContextTriggerRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        var area = string.IsNullOrWhiteSpace(request.SelectionText)
            ? "this area"
            : request.SelectionText.Trim();

        var response = new ExperionContextTriggerResponse
        {
            DetectedIntent = "contextual-assistance",
            Confidence = 0.86m,
            UiMode = "expanded",
            SuggestedPrompts =
            [
                $"I noticed you circled \"{area}\". What do you want to do here?",
                "Can you summarize this section?",
                "Can you complete this flow for me?"
            ],
            RecommendedActions =
            [
                "explain-context",
                "guide-next-step",
                "prepare-action-plan"
            ]
        };

        return Task.FromResult(response);
    }

    public Task<ExperionConversationMessageResponse> SendMessageAsync(
        ExperionConversationMessageRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        var trimmed = string.IsNullOrWhiteSpace(request.Message)
            ? "I can help you with this page."
            : request.Message.Trim();

        var response = new ExperionConversationMessageResponse
        {
            AssistantMessage = "Understood. I analyzed your page context and can guide or execute this journey step-by-step.",
            Explanation = "For critical actions, I will ask for your confirmation before executing.",
            RequiresConfirmation = true,
            MissingInputs = [],
            ProposedActions =
            [
                new ExperionProposedActionDto
                {
                    ActionName = "prepare-action-plan",
                    Label = $"Create safe execution plan for: {trimmed}",
                    IsCritical = false
                },
                new ExperionProposedActionDto
                {
                    ActionName = "execute-approved-step",
                    Label = "Execute selected step after your confirmation.",
                    IsCritical = true
                }
            ]
        };

        return Task.FromResult(response);
    }

    public Task<ExperionActionExecuteResponse> ExecuteActionAsync(
        ExperionActionExecuteRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        var executionId = Guid.NewGuid();
        var status = string.IsNullOrWhiteSpace(request.ConfirmationToken) && IsCriticalAction(request.ActionName)
            ? "pending_confirmation"
            : "accepted";

        var response = new ExperionActionExecuteResponse
        {
            ExecutionId = executionId,
            Status = status,
            ProgressMessage = status == "pending_confirmation"
                ? "Confirmation is required before critical action execution."
                : $"Action '{request.ActionName}' accepted and queued.",
            UndoToken = status == "accepted" ? Guid.NewGuid().ToString("N") : string.Empty
        };

        return Task.FromResult(response);
    }

    public Task<ExperionActionStatusDto> GetActionStatusAsync(
        Guid executionId,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = cancellationToken;
        var response = new ExperionActionStatusDto
        {
            ExecutionId = executionId,
            Status = "running",
            Step = 2,
            StepCount = 4,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            AuditRef = $"audit-{executionId:N}"
        };
        return Task.FromResult(response);
    }

    public Task<IReadOnlyList<ExperionAuditEventDto>> GetAuditEventsAsync(
        Guid sessionId,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<ExperionAuditEventDto> events =
        [
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                EventType = "session_bootstrap",
                CreatedAtUtc = now.AddSeconds(-20),
                Message = "Session created with product source validation.",
                TraceId = context.TraceId
            },
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                EventType = "context_triggered",
                CreatedAtUtc = now.AddSeconds(-15),
                Message = "Circle trigger received with UI context.",
                TraceId = context.TraceId
            },
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                EventType = "assistant_response",
                CreatedAtUtc = now.AddSeconds(-10),
                Message = "Assistant proposed guided actions with confirmation guardrail.",
                TraceId = context.TraceId
            }
        ];

        return Task.FromResult(events);
    }

    private static string NormalizeProductCode(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return "unknown";
        }

        return productCode.Trim().ToLowerInvariant();
    }

    private static bool IsCriticalAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return false;
        }

        return actionName.Contains("execute", StringComparison.OrdinalIgnoreCase)
               || actionName.Contains("submit", StringComparison.OrdinalIgnoreCase)
               || actionName.Contains("delete", StringComparison.OrdinalIgnoreCase);
    }
}
