using Experion.Backend.Models;

namespace Experion.Backend.Services;

public interface IDomSanitizer
{
    Task<DomSanitizeResult> SanitizeAsync(
        string rawDom,
        string userPrompt,
        CancellationToken cancellationToken);
}

public interface IIntentRouter
{
    ExperionContextMode DecideContext(ExperionContextInput input);
}

public interface ITokenEstimator
{
    int Estimate(string text);
}

public interface IOpenAiSuggestionClient
{
    Task<OpenAiSuggestionResult> GenerateSuggestionAsync(
        string userPrompt,
        string cleanedDom,
        CancellationToken cancellationToken);
}

public interface IActionKnowledgeBaseRepository
{
    Task<IReadOnlyList<ActionKnowledgeEntry>> GetActiveEntriesAsync(
        string productCode,
        CancellationToken cancellationToken);
}

public interface IActionExecutor
{
    Task<ActionExecutionResult> ExecuteAsync(
        string actionCode,
        string productCode,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken);
}

public interface IConversationMemoryRepository
{
    Task SaveAsync(ConversationMemoryRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationThreadSummary>> GetConversationSummaryAsync(
        string userId,
        string productCode,
        string workspaceId,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationHistoryMessage>> GetConversationHistoryAsync(
        string userId,
        string productCode,
        string workspaceId,
        Guid? conversationId,
        int take,
        CancellationToken cancellationToken);
}

public interface ILearningCache
{
    Task<LearningCacheRecord?> TryGetAsync(string cacheKey, CancellationToken cancellationToken);

    Task StoreAsync(LearningCacheRecord record, CancellationToken cancellationToken);
}

public interface IExperionOrchestrator
{
    Task<ExperionBootstrapResponse> BootstrapAsync(
        ExperionBootstrapRequest request,
        CancellationToken cancellationToken);

    Task<ExperionContextResolveResponse> TriggerContextAsync(
        ExperionContextResolveRequest request,
        CancellationToken cancellationToken);

    Task<ExperionMessageResponse> SendMessageAsync(
        ExperionMessageRequest request,
        CancellationToken cancellationToken);

    Task<ExperionHistoryResponse> GetHistoryAsync(
        string userId,
        string productCode,
        string workspaceId,
        Guid? conversationId,
        int conversationTake,
        int messageTake,
        CancellationToken cancellationToken);

    Task<ExperionActionExecutionResponse> PublishFacebookPostAsync(
        FacebookPublishRequest request,
        CancellationToken cancellationToken);
}
