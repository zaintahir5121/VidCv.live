namespace Experion.Backend.Models;

public enum ContextType
{
    Llm = 1,
    Action = 2
}

public sealed class ActionKnowledgeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string ApiRoute { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string TokenPattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ConversationMemoryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string CleanedDom { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string ResponseLayer { get; set; } = "llm";
    public string? ActionCode { get; set; }
    public string? ActionPayloadJson { get; set; }
    public string? ActionResultJson { get; set; }
    public ExperionContextMode ContextType { get; set; } = ExperionContextMode.Llm;
    public string CacheKey { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int DomTokens { get; set; }
    public int CompletionTokens { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LearningCacheRecord
{
    public string CacheKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public ExperionContextMode ContextType { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string CleanedDom { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string? ActionCode { get; set; }
    public string? ActionPayload { get; set; }
    public string? ActionResult { get; set; }
    public int CompletionTokens { get; set; }
    public DateTimeOffset CachedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LearningCacheEntry
{
    public string CacheKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string CleanedDom { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string? ActionCode { get; set; }
    public string? ActionPayload { get; set; }
    public string? ActionResult { get; set; }
    public int PromptTokens { get; set; }
    public int DomTokens { get; set; }
    public int CompletionTokens { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastAccessedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConversationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string CleanedDom { get; set; } = string.Empty;
    public string ResponseType { get; set; } = "llm";
    public string Suggestion { get; set; } = string.Empty;
    public string ActionPlanJson { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int CacheHitCount { get; set; }
    public string? ActionType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OpenAiSuggestionResult
{
    public string Suggestion { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}

public sealed class DomSanitizeResult
{
    public string CleanedDom { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public string DomSummary { get; set; } = string.Empty;
}

public sealed class ExperionContextInput
{
    public string UserPrompt { get; set; } = string.Empty;
    public string CleanedDom { get; set; } = string.Empty;
}

public sealed class ActionExecutionRequest
{
    public string ActionCode { get; set; } = string.Empty;
    public string ApiRoute { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string PayloadJson { get; set; } = "{}";
}

public sealed class ActionExecutionResult
{
    public string ActionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionResult { get; set; } = string.Empty;
}
