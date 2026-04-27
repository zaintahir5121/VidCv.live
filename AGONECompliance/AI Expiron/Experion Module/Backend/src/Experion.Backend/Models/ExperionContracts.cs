namespace Experion.Backend.Models;

public enum ExperionContextMode
{
    Llm = 1,
    Action = 2
}

public sealed class DomSnapshotDto
{
    public string SourceHtml { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string FocalElementText { get; set; } = string.Empty;
    public string FocalElementHtml { get; set; } = string.Empty;
}

public sealed class ExperionBootstrapRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "work";
    public string WorkspaceId { get; set; } = "default";
    public Guid? ConversationId { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public string UserTimezone { get; set; } = "UTC";
}

public sealed class ExperionBootstrapResponse
{
    public Guid SessionId { get; set; }
    public Guid ConversationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public List<ExperionConversationThreadDto> Threads { get; set; } = [];
    public List<ExperionConversationMessageDto> Messages { get; set; } = [];
}

public sealed class ExperionContextResolveRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "work";
    public string WorkspaceId { get; set; } = "default";
    public Guid SessionId { get; set; }
    public Guid? ConversationId { get; set; }
    public string TriggerType { get; set; } = "manual";
    public string SelectionText { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public DomSnapshotDto DomSnapshot { get; set; } = new();
}

public sealed class ExperionContextResolveResponse
{
    public Guid ConversationId { get; set; }
    public string CleanDomSummary { get; set; } = string.Empty;
    public ExperionContextMode ContextMode { get; set; } = ExperionContextMode.Llm;
    public List<string> SuggestedPrompts { get; set; } = [];
    public List<string> SuggestedActions { get; set; } = [];
}

public sealed class ExperionMessageRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "work";
    public string WorkspaceId { get; set; } = "default";
    public Guid SessionId { get; set; }
    public Guid? ConversationId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public DomSnapshotDto DomSnapshot { get; set; } = new();
}

public sealed class ExperionMessageResponse
{
    public Guid ConversationId { get; set; }
    public Guid SessionId { get; set; }
    public ExperionContextMode ContextMode { get; set; } = ExperionContextMode.Llm;
    public string ResponseLayer { get; set; } = "llm";
    public bool IsCacheHit { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public string AssistantMessage { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int DomTokens { get; set; }
    public int CompletionTokens { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ExperionActionSuggestionDto> SuggestedActions { get; set; } = [];
}

public sealed class ExperionActionSuggestionDto
{
    public string ActionCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public decimal Confidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionResult { get; set; } = string.Empty;
}

public sealed class ExperionHistoryResponse
{
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public Guid? ActiveConversationId { get; set; }
    public List<ExperionConversationThreadDto> Threads { get; set; } = [];
    public List<ExperionConversationMessageDto> Messages { get; set; } = [];
}

public sealed class ExperionConversationThreadDto
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTimeOffset LastOccurredAtUtc { get; set; }
}

public sealed class ExperionConversationMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "assistant";
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class FacebookPublishRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "work";
    public string WorkspaceId { get; set; } = "default";
    public Guid SessionId { get; set; }
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DomSnapshotDto DomSnapshot { get; set; } = new();
}

public sealed class ExperionActionExecutionResponse
{
    public string ActionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionResult { get; set; } = string.Empty;
}
