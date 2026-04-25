using System.Security.Cryptography;
using System.Text;
using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AGONECompliance.Services;

public sealed class ExperionService(
    ComplianceDbContext dbContext,
    IMemoryCache memoryCache) : IExperionService
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
        var trimmed = string.IsNullOrWhiteSpace(request.Message)
            ? "I can help you with this page."
            : request.Message.Trim();

        var cacheKey = BuildCacheKey(context, trimmed);
        if (TryGetMemoryCache(cacheKey, out var memoryHit))
        {
            return Task.FromResult(memoryHit);
        }

        var sqlHit = dbContext.ExperionMemoryEntries
            .AsNoTracking()
            .Where(x => x.MemoryKey == cacheKey && x.LastAccessedAtUtc > DateTimeOffset.UtcNow.AddHours(-4))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();
        if (sqlHit is not null)
        {
            var cachedResponse = BuildCachedResponse(sqlHit);
            StoreInMemoryCache(cacheKey, cachedResponse, DateTimeOffset.UtcNow.AddMinutes(20));
            return Task.FromResult(cachedResponse);
        }

        var llmSuggestion = BuildLlmSuggestion(trimmed, context);
        var actionAdvice = BuildApplicationActionGuidance(context);
        var response = new ExperionConversationMessageResponse
        {
            AssistantMessage = llmSuggestion,
            Explanation = "Layer flow: Memory cache miss -> SQL cache miss -> LLM suggestion + ApplicationAction guidance.",
            ResponseLayer = "llm+application-action",
            IsCacheHit = false,
            CacheKey = cacheKey,
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
                },
                new ExperionProposedActionDto
                {
                    ActionName = actionAdvice,
                    Label = "Invoke product API through ApplicationAction layer.",
                    IsCritical = true
                }
            ]
        };

        PersistSqlCache(cacheKey, context, trimmed, response);
        StoreInMemoryCache(cacheKey, response, DateTimeOffset.UtcNow.AddMinutes(20));
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

    private static string BuildCacheKey(ExperionRequestContext context, string message)
    {
        var input = $"{NormalizeProductCode(context.ProductCode)}|{context.WorkspaceId}|{message.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private bool TryGetMemoryCache(string cacheKey, out ExperionConversationMessageResponse response)
    {
        if (memoryCache.TryGetValue(cacheKey, out ExperionConversationMessageResponse? cached) && cached is not null)
        {
            response = new ExperionConversationMessageResponse
            {
                AssistantMessage = cached.AssistantMessage,
                Explanation = cached.Explanation,
                RequiresConfirmation = cached.RequiresConfirmation,
                MissingInputs = [..cached.MissingInputs],
                ProposedActions = [..cached.ProposedActions],
                ResponseLayer = "memory-cache",
                IsCacheHit = true,
                CacheKey = cacheKey
            };
            return true;
        }

        response = null!;
        return false;
    }

    private static ExperionConversationMessageResponse BuildCachedResponse(ExperionMemoryEntry cache)
    {
        return new ExperionConversationMessageResponse
        {
            AssistantMessage = cache.AssistantResponse,
            Explanation = "Returned from SQL memory cache.",
            RequiresConfirmation = true,
            MissingInputs = [],
            ResponseLayer = "sql-cache",
            IsCacheHit = true,
            CacheKey = cache.MemoryKey,
            ProposedActions =
            [
                new ExperionProposedActionDto
                {
                    ActionName = "prepare-action-plan",
                    Label = "Use cached guidance and continue.",
                    IsCritical = false
                }
            ]
        };
    }

    private static string BuildLlmSuggestion(string message, ExperionRequestContext context)
    {
        var product = NormalizeProductCode(context.ProductCode);
        return $"[LLM Suggestion] For {product}, I recommend validating dependencies and confirmations before executing: \"{message}\".";
    }

    private static string BuildApplicationActionGuidance(ExperionRequestContext context)
    {
        var product = NormalizeProductCode(context.ProductCode);
        return $"{product}.application-action.execute";
    }

    private void PersistSqlCache(
        string cacheKey,
        ExperionRequestContext context,
        string message,
        ExperionConversationMessageResponse response)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new ExperionMemoryEntry
        {
            MemoryKey = cacheKey,
            ProductCode = NormalizeProductCode(context.ProductCode),
            WorkspaceId = string.IsNullOrWhiteSpace(context.WorkspaceId) ? "default" : context.WorkspaceId.Trim(),
            UserPrompt = message,
            AssistantResponse = response.AssistantMessage,
            LayerSource = "llm+application-action",
            LastAccessedAtUtc = now
        };

        dbContext.ExperionMemoryEntries.Add(record);
        dbContext.SaveChanges();
    }

    private void StoreInMemoryCache(
        string cacheKey,
        ExperionConversationMessageResponse response,
        DateTimeOffset expiresAtUtc)
    {
        var ttl = expiresAtUtc - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromMinutes(1);
        }

        memoryCache.Set(cacheKey, response, ttl);
    }
}
