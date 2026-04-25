using AGONECompliance.Domain;
using AGONECompliance.Shared;

namespace AGONECompliance.Services;

public sealed class ProcessedDocument
{
    public string FullText { get; set; } = string.Empty;
    public string ParsedJson { get; set; } = string.Empty;
    public IReadOnlyList<ProcessedDocumentPage> Pages { get; set; } = [];
}

public sealed class ProcessedDocumentPage
{
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class RuleAssessment
{
    public Guid RuleId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string GuideReference { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EvidenceExcerpt { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public decimal ConfidenceScore { get; set; }
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken,
        string? folderPath = null);
    Task<(Stream Stream, string ContentType)> DownloadAsync(
        string blobPath,
        string fallbackContentType,
        CancellationToken cancellationToken);
    Task<string?> DownloadTextAsync(
        string? blobPath,
        CancellationToken cancellationToken);
}

public sealed record PageTextItem(int PageNumber, string Content);

public interface IDocumentIntelligenceService
{
    Task<ProcessedDocument> ExtractTextAsync(Stream stream, string contentType, CancellationToken cancellationToken);
    List<Services.PageTextItem> ParsePages(string parsedJson);
}

public interface IComplianceAiService
{
    Task<List<ComplianceRule>> GenerateRulesAsync(string appendixText, CancellationToken cancellationToken);
    Task<List<RuleAssessment>> EvaluateProspectusAsync(
        string prospectusText,
        IReadOnlyCollection<ComplianceRule> selectedRules,
        IReadOnlyDictionary<Guid, string>? guideContextsByRuleId,
        CancellationToken cancellationToken);
}

public interface IComplianceSearchService
{
    Task EnsureIndexExistsAsync(CancellationToken cancellationToken);
    Task IndexDocumentAsync(UploadedDocument document, CancellationToken cancellationToken);
}

public interface IEvaluationOrchestrator
{
    Task<Guid> QueueEvaluationAsync(
        Guid evaluationWorkspaceId,
        Guid prospectusDocumentId,
        IReadOnlyCollection<Guid> ruleIds,
        CancellationToken cancellationToken);
    Task ProcessNextPendingRunAsync(CancellationToken cancellationToken);
}

public interface IDocumentProcessingOrchestrator
{
    Task<Guid> QueueDocumentProcessingAsync(
        Guid evaluationWorkspaceId,
        Guid documentId,
        CancellationToken cancellationToken);
    Task ProcessNextPendingJobAsync(CancellationToken cancellationToken);
}

public interface IRuleGenerationOrchestrator
{
    Task<Guid> QueueRuleGenerationAsync(
        GenerateRulesRequest request,
        CancellationToken cancellationToken);
    Task ProcessNextPendingJobAsync(CancellationToken cancellationToken);
}

public interface IExperionService
{
    Task<ExperionSessionBootstrapResponse> BootstrapSessionAsync(
        ExperionSessionBootstrapRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken);

    Task<ExperionContextTriggerResponse> TriggerContextAsync(
        ExperionContextTriggerRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken);

    Task<ExperionConversationMessageResponse> SendMessageAsync(
        ExperionConversationMessageRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken);

    Task<ExperionActionExecuteResponse> ExecuteActionAsync(
        ExperionActionExecuteRequest request,
        ExperionRequestContext context,
        CancellationToken cancellationToken);

    Task<ExperionActionStatusDto> GetActionStatusAsync(
        Guid executionId,
        ExperionRequestContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExperionAuditEventDto>> GetAuditEventsAsync(
        Guid sessionId,
        ExperionRequestContext context,
        CancellationToken cancellationToken);
}

