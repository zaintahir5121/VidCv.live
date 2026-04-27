using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Experion.Backend.Models;

namespace Experion.Backend.Services;

public sealed class ExperionOrchestrator(
    IDomSanitizer domSanitizer,
    IIntentRouter intentRouter,
    ITokenEstimator tokenEstimator,
    IActionKnowledgeBaseRepository actionKnowledgeBaseRepository,
    IActionExecutor actionExecutor,
    IOpenAiSuggestionClient openAiSuggestionClient,
    ILearningCache learningCache,
    IConversationMemoryRepository conversationMemoryRepository) : IExperionOrchestrator
{
    public async Task<ExperionBootstrapResponse> BootstrapAsync(
        ExperionBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = request.ConversationId.GetValueOrDefault(Guid.NewGuid());
        var sessionId = Guid.NewGuid();

        var threads = await conversationMemoryRepository.GetConversationSummaryAsync(
            request.UserId,
            request.ProductCode,
            request.WorkspaceId,
            30,
            cancellationToken);

        var messages = await conversationMemoryRepository.GetConversationHistoryAsync(
            request.UserId,
            request.ProductCode,
            request.WorkspaceId,
            conversationId,
            120,
            cancellationToken);

        return new ExperionBootstrapResponse
        {
            SessionId = sessionId,
            ConversationId = conversationId,
            ProductCode = request.ProductCode,
            WorkspaceId = request.WorkspaceId,
            Threads = threads.Select(MapThread).ToList(),
            Messages = messages.Select(MapMessage).ToList()
        };
    }

    public async Task<ExperionContextResolveResponse> TriggerContextAsync(
        ExperionContextResolveRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = request.ConversationId.GetValueOrDefault(Guid.NewGuid());
        var domRaw = BuildDomRaw(request.DomSnapshot);
        var sanitized = await domSanitizer.SanitizeAsync(domRaw, request.SelectionText, cancellationToken);
        var mode = intentRouter.DecideContext(new ExperionContextInput
        {
            UserPrompt = request.SelectionText,
            CleanedDom = sanitized.CleanedDom
        });

        var suggestions = new List<string>
        {
            "Summarize what I circled.",
            "What action can I take here?",
            "Create a step-by-step recommendation."
        };
        var actions = new List<string>();

        if (mode == ExperionContextMode.Action)
        {
            var entries = await actionKnowledgeBaseRepository.GetActiveEntriesAsync(request.ProductCode, cancellationToken);
            actions.AddRange(entries.Select(x => x.ActionCode).Distinct(StringComparer.OrdinalIgnoreCase).Take(5));
            if (actions.Count > 0)
            {
                suggestions.Insert(0, $"Execute action: {actions[0]}");
            }
        }

        return new ExperionContextResolveResponse
        {
            ConversationId = conversationId,
            CleanDomSummary = sanitized.DomSummary,
            ContextMode = mode,
            SuggestedPrompts = suggestions,
            SuggestedActions = actions
        };
    }

    public async Task<ExperionMessageResponse> SendMessageAsync(
        ExperionMessageRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = request.ConversationId.GetValueOrDefault(Guid.NewGuid());
        var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;
        var domRaw = BuildDomRaw(request.DomSnapshot);
        var sanitized = await domSanitizer.SanitizeAsync(domRaw, request.UserPrompt, cancellationToken);

        var promptTokens = tokenEstimator.Estimate(request.UserPrompt);
        var domTokens = tokenEstimator.Estimate(sanitized.NormalizedText);
        var cacheKey = BuildCacheKey(request.ProductCode, request.WorkspaceId, request.UserId, request.UserPrompt, sanitized.CleanedDom);

        var cached = await learningCache.TryGetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var cachedResponse = new ExperionMessageResponse
            {
                ConversationId = conversationId,
                SessionId = sessionId,
                ContextMode = cached.ContextType,
                ResponseLayer = "cache",
                IsCacheHit = true,
                CacheKey = cacheKey,
                AssistantMessage = cached.AssistantResponse,
                PromptTokens = promptTokens,
                DomTokens = domTokens,
                CompletionTokens = cached.CompletionTokens,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                SuggestedActions = BuildCachedActionSuggestions(cached)
            };

            await SaveConversationAsync(
                request,
                conversationId,
                sessionId,
                sanitized.CleanedDom,
                cachedResponse,
                cached.ActionCode,
                cached.ActionPayload,
                cached.ActionResult,
                cancellationToken);

            return cachedResponse;
        }

        var mode = intentRouter.DecideContext(new ExperionContextInput
        {
            UserPrompt = request.UserPrompt,
            CleanedDom = sanitized.CleanedDom
        });

        var response = new ExperionMessageResponse
        {
            ConversationId = conversationId,
            SessionId = sessionId,
            ContextMode = mode,
            ResponseLayer = mode == ExperionContextMode.Action ? "action" : "llm",
            IsCacheHit = false,
            CacheKey = cacheKey,
            PromptTokens = promptTokens,
            DomTokens = domTokens,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        string? actionCode = null;
        string? actionPayload = null;
        string? actionResult = null;

        if (mode == ExperionContextMode.Action)
        {
            var match = await ResolveBestActionAsync(request.ProductCode, $"{request.UserPrompt} {sanitized.NormalizedText}", cancellationToken);
            if (match is not null)
            {
                actionCode = match.ActionCode;
                var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["message"] = request.UserPrompt,
                    ["workspaceId"] = request.WorkspaceId,
                    ["userId"] = request.UserId,
                    ["productCode"] = request.ProductCode,
                    ["cleanDom"] = sanitized.CleanedDom
                };

                var execution = await actionExecutor.ExecuteAsync(actionCode, request.ProductCode, payload, cancellationToken);
                actionPayload = JsonSerializer.Serialize(payload);
                actionResult = execution.ActionResult;

                response.AssistantMessage = execution.Message;
                response.CompletionTokens = tokenEstimator.Estimate(response.AssistantMessage);
                response.SuggestedActions =
                [
                    new ExperionActionSuggestionDto
                    {
                        ActionCode = actionCode,
                        DisplayName = match.Description,
                        Endpoint = match.ApiRoute,
                        HttpMethod = match.HttpMethod,
                        Confidence = 0.91m,
                        Status = execution.Status,
                        Message = execution.Message,
                        ActionResult = execution.ActionResult
                    }
                ];
            }
            else
            {
                mode = ExperionContextMode.Llm;
                response.ContextMode = mode;
                response.ResponseLayer = "llm";
            }
        }

        if (mode == ExperionContextMode.Llm)
        {
            var suggestion = await openAiSuggestionClient.GenerateSuggestionAsync(
                request.UserPrompt,
                sanitized.CleanedDom,
                cancellationToken);
            response.AssistantMessage = suggestion.Suggestion;
            response.CompletionTokens = suggestion.CompletionTokens;
        }

        var cacheRecord = new LearningCacheRecord
        {
            CacheKey = cacheKey,
            UserId = request.UserId,
            ProductCode = request.ProductCode,
            WorkspaceId = request.WorkspaceId,
            ContextType = response.ContextMode,
            Prompt = request.UserPrompt,
            CleanedDom = sanitized.CleanedDom,
            AssistantResponse = response.AssistantMessage,
            ActionCode = actionCode,
            ActionPayload = actionPayload,
            ActionResult = actionResult,
            CompletionTokens = response.CompletionTokens,
            CachedAtUtc = DateTimeOffset.UtcNow
        };
        await learningCache.StoreAsync(cacheRecord, cancellationToken);

        await SaveConversationAsync(
            request,
            conversationId,
            sessionId,
            sanitized.CleanedDom,
            response,
            actionCode,
            actionPayload,
            actionResult,
            cancellationToken);

        return response;
    }

    public async Task<ExperionHistoryResponse> GetHistoryAsync(
        string userId,
        string productCode,
        string workspaceId,
        Guid? conversationId,
        int conversationTake,
        int messageTake,
        CancellationToken cancellationToken)
    {
        var normalizedProduct = string.IsNullOrWhiteSpace(productCode) ? "work" : productCode.Trim().ToLowerInvariant();
        var normalizedWorkspace = string.IsNullOrWhiteSpace(workspaceId) ? "default" : workspaceId.Trim();

        var threads = await conversationMemoryRepository.GetConversationSummaryAsync(
            userId,
            normalizedProduct,
            normalizedWorkspace,
            Math.Clamp(conversationTake, 1, 100),
            cancellationToken);

        var activeConversationId = conversationId;
        if (!activeConversationId.HasValue || activeConversationId.Value == Guid.Empty)
        {
            activeConversationId = threads.FirstOrDefault()?.ConversationId;
        }

        var messages = await conversationMemoryRepository.GetConversationHistoryAsync(
            userId,
            normalizedProduct,
            normalizedWorkspace,
            activeConversationId,
            Math.Clamp(messageTake, 1, 400),
            cancellationToken);

        return new ExperionHistoryResponse
        {
            UserId = userId,
            ProductCode = normalizedProduct,
            WorkspaceId = normalizedWorkspace,
            ActiveConversationId = activeConversationId,
            Threads = threads.Select(MapThread).ToList(),
            Messages = messages.Select(MapMessage).ToList()
        };
    }

    public async Task<ExperionActionExecutionResponse> PublishFacebookPostAsync(
        FacebookPublishRequest request,
        CancellationToken cancellationToken)
    {
        var result = await actionExecutor.ExecuteAsync(
            "work.social.post-facebook",
            request.ProductCode,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["message"] = request.Message,
                ["userId"] = request.UserId,
                ["workspaceId"] = request.WorkspaceId
            },
            cancellationToken);

        var response = new ExperionActionExecutionResponse
        {
            ActionCode = result.ActionCode,
            Status = result.Status,
            Message = result.Message,
            ActionResult = result.ActionResult
        };

        var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;
        var conversationId = request.ConversationId.GetValueOrDefault(Guid.NewGuid());
        var promptTokens = tokenEstimator.Estimate(request.Message);
        var completionTokens = tokenEstimator.Estimate(result.Message);
        var cacheKey = BuildCacheKey(request.ProductCode, request.WorkspaceId, request.UserId, request.Message, string.Empty);

        await conversationMemoryRepository.SaveAsync(
            new ConversationMemoryRecord
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SessionId = sessionId,
                UserId = request.UserId,
                ProductCode = request.ProductCode,
                WorkspaceId = request.WorkspaceId,
                UserPrompt = request.Message,
                CleanedDom = request.DomSnapshot?.SourceText ?? string.Empty,
                AssistantResponse = result.Message,
                ResponseLayer = "action",
                ActionCode = result.ActionCode,
                ActionPayloadJson = JsonSerializer.Serialize(new { request.Message }),
                ActionResultJson = result.ActionResult,
                ContextType = ExperionContextMode.Action,
                CacheKey = cacheKey,
                PromptTokens = promptTokens,
                DomTokens = 0,
                CompletionTokens = completionTokens,
                OccurredAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);

        return response;
    }

    private async Task SaveConversationAsync(
        ExperionMessageRequest request,
        Guid conversationId,
        Guid sessionId,
        string cleanedDom,
        ExperionMessageResponse response,
        string? actionCode,
        string? actionPayload,
        string? actionResult,
        CancellationToken cancellationToken)
    {
        await conversationMemoryRepository.SaveAsync(
            new ConversationMemoryRecord
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SessionId = sessionId,
                UserId = request.UserId,
                ProductCode = request.ProductCode,
                WorkspaceId = request.WorkspaceId,
                UserPrompt = request.UserPrompt,
                CleanedDom = cleanedDom,
                AssistantResponse = response.AssistantMessage,
                ResponseLayer = response.ResponseLayer,
                ActionCode = actionCode,
                ActionPayloadJson = actionPayload,
                ActionResultJson = actionResult,
                ContextType = response.ContextMode,
                CacheKey = response.CacheKey,
                PromptTokens = response.PromptTokens,
                DomTokens = response.DomTokens,
                CompletionTokens = response.CompletionTokens,
                OccurredAtUtc = response.OccurredAtUtc
            },
            cancellationToken);
    }

    private async Task<ActionKnowledgeEntry?> ResolveBestActionAsync(
        string productCode,
        string source,
        CancellationToken cancellationToken)
    {
        var entries = await actionKnowledgeBaseRepository.GetActiveEntriesAsync(productCode, cancellationToken);
        if (entries.Count == 0)
        {
            return null;
        }

        var sourceTokens = Tokenize(source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sourceTokens.Count == 0)
        {
            return null;
        }

        ActionKnowledgeEntry? best = null;
        var bestScore = 0;
        foreach (var entry in entries)
        {
            var patternTokens = Tokenize(entry.TokenPattern);
            var score = patternTokens.Count(sourceTokens.Contains);
            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static IEnumerable<string> Tokenize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]+")
            .Select(x => x.Value);
    }

    private static string BuildDomRaw(DomSnapshotDto? snapshot)
    {
        if (snapshot is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.SourceHtml))
        {
            return snapshot.SourceHtml;
        }

        return string.Join('\n', new[]
        {
            snapshot.SourceText,
            snapshot.FocalElementText,
            snapshot.FocalElementHtml
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static List<ExperionActionSuggestionDto> BuildCachedActionSuggestions(LearningCacheRecord cached)
    {
        if (string.IsNullOrWhiteSpace(cached.ActionCode))
        {
            return [];
        }

        return
        [
            new ExperionActionSuggestionDto
            {
                ActionCode = cached.ActionCode,
                DisplayName = cached.ActionCode,
                Endpoint = string.Empty,
                HttpMethod = "POST",
                Confidence = 0.95m,
                Status = "cache",
                Message = "Loaded from learned cache",
                ActionResult = cached.ActionResult ?? string.Empty
            }
        ];
    }

    private static ExperionConversationThreadDto MapThread(ConversationThreadSummary source)
    {
        return new ExperionConversationThreadDto
        {
            ConversationId = source.ConversationId,
            Title = source.Title,
            MessageCount = source.MessageCount,
            LastOccurredAtUtc = source.LastOccurredAtUtc
        };
    }

    private static ExperionConversationMessageDto MapMessage(ConversationHistoryMessage source)
    {
        return new ExperionConversationMessageDto
        {
            Id = source.Id,
            ConversationId = source.ConversationId,
            SessionId = source.SessionId,
            Role = source.Role,
            Content = source.Content,
            OccurredAtUtc = source.OccurredAtUtc
        };
    }

    private static string BuildCacheKey(
        string productCode,
        string workspaceId,
        string userId,
        string prompt,
        string cleanDom)
    {
        var normalized = $"{productCode}|{workspaceId}|{userId}|{prompt}|{cleanDom}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
