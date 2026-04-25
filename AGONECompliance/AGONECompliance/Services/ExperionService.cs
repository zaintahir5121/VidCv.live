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
    IMemoryCache memoryCache,
    IComplianceSearchService complianceSearchService,
    ILogger<ExperionService> logger) : IExperionService
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
        var workspaceId = NormalizeWorkspaceId(context.WorkspaceId);
        var conversationId = request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty
            ? request.ConversationId.Value
            : Guid.NewGuid();

        var response = new ExperionSessionBootstrapResponse
        {
            SessionId = Guid.NewGuid(),
            ConversationId = conversationId,
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

    public async Task<ExperionConversationMessageResponse> SendMessageAsync(
        ExperionConversationMessageRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var trimmed = string.IsNullOrWhiteSpace(request.Message)
            ? "I can help you with this page."
            : request.Message.Trim();
        var conversationId = request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty
            ? request.ConversationId.Value
            : Guid.NewGuid();

        var cacheKey = BuildCacheKey(context, trimmed);
        ExperionConversationMessageResponse response;
        if (TryGetMemoryCache(cacheKey, out var memoryHit))
        {
            response = memoryHit;
            await TouchSqlMemoryEntryAsync(cacheKey, cancellationToken);
        }
        else
        {
            var sqlHit = await dbContext.ExperionMemoryEntries
                .AsNoTracking()
                .Where(x => x.MemoryKey == cacheKey && x.LastAccessedAtUtc > DateTimeOffset.UtcNow.AddHours(-4))
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (sqlHit is not null)
            {
                response = BuildCachedResponse(sqlHit);
                StoreInMemoryCache(cacheKey, response, DateTimeOffset.UtcNow.AddMinutes(20));
                await TouchSqlMemoryEntryAsync(cacheKey, cancellationToken);
            }
            else
            {
                var llmSuggestion = BuildLlmSuggestion(trimmed, context);
                var actionAdvice = BuildApplicationActionGuidance(context);
                response = new ExperionConversationMessageResponse
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

                await PersistSqlCacheAsync(cacheKey, context, trimmed, response, cancellationToken);
                StoreInMemoryCache(cacheKey, response, DateTimeOffset.UtcNow.AddMinutes(20));
            }
        }

        response.ConversationId = conversationId;
        response.OccurredAtUtc = now;
        await PersistConversationAsync(
            conversationId,
            request.SessionId,
            context,
            trimmed,
            response,
            cancellationToken);
        return response;
    }

    public async Task<ExperionConversationHistoryResponse> GetConversationHistoryAsync(
        ExperionConversationHistoryRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = request ?? new ExperionConversationHistoryRequest();
        var conversationTake = Math.Clamp(normalizedRequest.ConversationTake, 1, 100);
        var messageTake = Math.Clamp(normalizedRequest.MessageTake, 1, 200);
        var userId = NormalizeUserId(context.UserId);
        var productCode = NormalizeProductCode(context.ProductCode);
        var workspaceId = NormalizeWorkspaceId(context.WorkspaceId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ExperionConversationHistoryResponse
            {
                UserId = string.Empty,
                ProductCode = productCode,
                WorkspaceId = workspaceId,
                ActiveConversationId = null,
                Conversations = [],
                Messages = []
            };
        }

        var entries = await dbContext.ExperionConversationEntries
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && x.ProductCode == productCode
                && x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(2000)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            var searchRecords = await complianceSearchService.SearchExperionConversationsAsync(
                productCode,
                workspaceId,
                userId,
                Math.Max(messageTake, conversationTake * 5),
                cancellationToken);
            if (searchRecords.Count > 0)
            {
                var searchMessages = searchRecords
                    .OrderBy(x => x.OccurredAtUtc)
                    .TakeLast(messageTake)
                    .SelectMany(x =>
                    {
                        var ids = BuildMessageIds(x.Id);
                        return new[]
                        {
                            new ExperionConversationHistoryItemDto
                            {
                                Id = ids.UserMessageId,
                                ConversationId = Guid.TryParse(x.ConversationId, out var searchConversationId) ? searchConversationId : Guid.Empty,
                                SessionId = Guid.TryParse(x.SessionId, out var searchSessionId) ? searchSessionId : Guid.Empty,
                                Role = "user",
                                Content = x.UserPrompt,
                                ResponseLayer = x.ResponseLayer,
                                CacheKey = x.CacheKey,
                                OccurredAtUtc = x.OccurredAtUtc
                            },
                            new ExperionConversationHistoryItemDto
                            {
                                Id = ids.AssistantMessageId,
                                ConversationId = Guid.TryParse(x.ConversationId, out var searchConversation) ? searchConversation : Guid.Empty,
                                SessionId = Guid.TryParse(x.SessionId, out var searchSession) ? searchSession : Guid.Empty,
                                Role = "assistant",
                                Content = x.AssistantResponse,
                                ResponseLayer = x.ResponseLayer,
                                CacheKey = x.CacheKey,
                                OccurredAtUtc = x.OccurredAtUtc
                            }
                        };
                    })
                    .ToList();

                return new ExperionConversationHistoryResponse
                {
                    UserId = userId,
                    ProductCode = productCode,
                    WorkspaceId = workspaceId,
                    ActiveConversationId = searchMessages.FirstOrDefault()?.ConversationId,
                    Conversations = [],
                    Messages = searchMessages
                };
            }

            return new ExperionConversationHistoryResponse
            {
                UserId = userId,
                ProductCode = productCode,
                WorkspaceId = workspaceId,
                ActiveConversationId = null,
                Conversations = [],
                Messages = []
            };
        }

        var threads = entries
            .GroupBy(x => x.ConversationId)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(x => x.OccurredAtUtc).ToList();
                var latest = ordered[0];
                return new ExperionConversationThreadDto
                {
                    ConversationId = g.Key,
                    LastSessionId = latest.SessionId,
                    Title = BuildConversationTitle(latest.UserPrompt),
                    LastPromptPreview = ToPreview(latest.UserPrompt, 80),
                    LastResponsePreview = ToPreview(latest.AssistantResponse, 120),
                    MessageCount = g.Count() * 2,
                    LastOccurredAtUtc = latest.OccurredAtUtc
                };
            })
            .OrderByDescending(x => x.LastOccurredAtUtc)
            .Take(conversationTake)
            .ToList();

        var activeConversationId = normalizedRequest.ConversationId.HasValue
                                   && normalizedRequest.ConversationId.Value != Guid.Empty
                                   && threads.Any(x => x.ConversationId == normalizedRequest.ConversationId.Value)
            ? normalizedRequest.ConversationId
            : threads.FirstOrDefault()?.ConversationId;

        var selectedMessages = new List<ExperionConversationHistoryItemDto>();
        if (activeConversationId.HasValue && activeConversationId.Value != Guid.Empty)
        {
            var selectedEntries = entries
                .Where(x => x.ConversationId == activeConversationId.Value)
                .OrderBy(x => x.OccurredAtUtc)
                .ToList();

            if (selectedEntries.Count > messageTake)
            {
                selectedEntries = selectedEntries.Skip(selectedEntries.Count - messageTake).ToList();
            }

            foreach (var entry in selectedEntries)
            {
                selectedMessages.Add(new ExperionConversationHistoryItemDto
                {
                    Id = entry.Id,
                    ConversationId = entry.ConversationId,
                    SessionId = entry.SessionId,
                    Role = "user",
                    Content = entry.UserPrompt,
                    ResponseLayer = entry.ResponseLayer,
                    CacheKey = entry.CacheKey,
                    OccurredAtUtc = entry.OccurredAtUtc
                });
                selectedMessages.Add(new ExperionConversationHistoryItemDto
                {
                    Id = DeriveGuid(entry.Id, 91),
                    ConversationId = entry.ConversationId,
                    SessionId = entry.SessionId,
                    Role = "assistant",
                    Content = entry.AssistantResponse,
                    ResponseLayer = entry.ResponseLayer,
                    CacheKey = entry.CacheKey,
                    OccurredAtUtc = entry.OccurredAtUtc
                });
            }
        }

        return new ExperionConversationHistoryResponse
        {
            UserId = userId,
            ProductCode = productCode,
            WorkspaceId = workspaceId,
            ActiveConversationId = activeConversationId,
            Conversations = threads,
            Messages = selectedMessages
        };
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

    private static string NormalizeWorkspaceId(string workspaceId)
    {
        return string.IsNullOrWhiteSpace(workspaceId)
            ? "default"
            : workspaceId.Trim();
    }

    private static string NormalizeUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return string.Empty;
        }

        return userId.Trim().ToLowerInvariant();
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
        var input = $"{NormalizeProductCode(context.ProductCode)}|{NormalizeWorkspaceId(context.WorkspaceId)}|{NormalizeUserId(context.UserId)}|{message.Trim().ToLowerInvariant()}";
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
                CacheKey = cacheKey,
                OccurredAtUtc = DateTimeOffset.UtcNow
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
            OccurredAtUtc = DateTimeOffset.UtcNow,
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

    private async Task PersistSqlCacheAsync(
        string cacheKey,
        ExperionRequestContext context,
        string message,
        ExperionConversationMessageResponse response,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedProductCode = NormalizeProductCode(context.ProductCode);
        var normalizedWorkspaceId = NormalizeWorkspaceId(context.WorkspaceId);
        var record = await dbContext.ExperionMemoryEntries
            .FirstOrDefaultAsync(x => x.MemoryKey == cacheKey, cancellationToken);
        if (record is null)
        {
            record = new ExperionMemoryEntry
            {
                MemoryKey = cacheKey,
                ProductCode = normalizedProductCode,
                WorkspaceId = normalizedWorkspaceId,
                UserPrompt = message,
                AssistantResponse = response.AssistantMessage,
                LayerSource = response.ResponseLayer,
                LastAccessedAtUtc = now,
                HitCount = 1
            };
            dbContext.ExperionMemoryEntries.Add(record);
        }
        else
        {
            record.ProductCode = normalizedProductCode;
            record.WorkspaceId = normalizedWorkspaceId;
            record.UserPrompt = message;
            record.AssistantResponse = response.AssistantMessage;
            record.LayerSource = response.ResponseLayer;
            record.LastAccessedAtUtc = now;
            record.HitCount = Math.Max(record.HitCount, 0) + 1;
            record.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TouchSqlMemoryEntryAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ExperionMemoryEntries
            .FirstOrDefaultAsync(x => x.MemoryKey == cacheKey, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.LastAccessedAtUtc = DateTimeOffset.UtcNow;
        existing.HitCount = Math.Max(existing.HitCount, 0) + 1;
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistConversationAsync(
        Guid conversationId,
        Guid sessionId,
        ExperionRequestContext context,
        string prompt,
        ExperionConversationMessageResponse response,
        CancellationToken cancellationToken)
    {
        var userId = NormalizeUserId(context.UserId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var entry = new ExperionConversationEntry
        {
            ConversationId = conversationId,
            SessionId = sessionId,
            UserId = userId,
            ProductCode = NormalizeProductCode(context.ProductCode),
            WorkspaceId = NormalizeWorkspaceId(context.WorkspaceId),
            UserPrompt = prompt,
            AssistantResponse = response.AssistantMessage,
            ResponseLayer = response.ResponseLayer,
            CacheKey = response.CacheKey,
            OccurredAtUtc = response.OccurredAtUtc,
            CreatedAtUtc = response.OccurredAtUtc,
            UpdatedAtUtc = response.OccurredAtUtc
        };

        dbContext.ExperionConversationEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var searchDocument = new ExperionConversationIndexDocument
            {
                Id = entry.Id.ToString("N"),
                ConversationId = entry.ConversationId.ToString(),
                SessionId = entry.SessionId.ToString(),
                ProductCode = entry.ProductCode,
                WorkspaceId = entry.WorkspaceId,
                UserId = entry.UserId,
                UserPrompt = entry.UserPrompt,
                AssistantResponse = entry.AssistantResponse,
                ResponseLayer = entry.ResponseLayer,
                CacheKey = entry.CacheKey,
                OccurredAtUtc = entry.OccurredAtUtc
            };
            await complianceSearchService.IndexExperionConversationAsync(searchDocument, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index conversation {ConversationId}.", conversationId);
        }
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

    private static string BuildConversationTitle(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "New chat";
        }

        var normalized = prompt.ReplaceLineEndings(" ").Trim();
        return normalized.Length > 56 ? $"{normalized[..56].Trim()}..." : normalized;
    }

    private static string ToPreview(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length > maxLength ? $"{normalized[..maxLength].Trim()}..." : normalized;
    }

    private static Guid DeriveGuid(Guid baseId, byte salt)
    {
        var bytes = baseId.ToByteArray();
        bytes[0] = (byte)(bytes[0] ^ salt);
        return new Guid(bytes);
    }

    private static (Guid UserMessageId, Guid AssistantMessageId) BuildMessageIds(string sourceId)
    {
        if (!Guid.TryParse(sourceId, out var parsed))
        {
            parsed = Guid.NewGuid();
        }

        return (parsed, DeriveGuid(parsed, 33));
    }
}
