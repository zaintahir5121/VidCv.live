using AGONECompliance.Domain;
using AGONECompliance.Shared;

namespace AGONECompliance.Services;

public sealed class ProcessedDocument
{
    public string FullText { get; set; } = string.Empty;
    public string ParsedJson { get; set; } = string.Empty;
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
}

public interface IDocumentIntelligenceService
{
    Task<ProcessedDocument> ExtractTextAsync(Stream stream, string contentType, CancellationToken cancellationToken);
}

public interface IComplianceAiService
{
    Task<List<ComplianceRule>> GenerateRulesAsync(string guideText, string appendixText, CancellationToken cancellationToken);
    Task<List<RuleAssessment>> EvaluateProspectusAsync(
        string prospectusText,
        IReadOnlyCollection<ComplianceRule> selectedRules,
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

