namespace AGONECompliance.Shared;

public enum DocumentType
{
    Guide = 1,
    Appendix = 2,
    Prospectus = 3
}

public enum ComplianceStatus
{
    Unknown = 0,
    Compliant = 1,
    NonCompliant = 2,
    NeedsReview = 3
}

public sealed class EvaluationWorkspaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class CreateEvaluationWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class DocumentDto
{
    public Guid Id { get; set; }
    public Guid EvaluationWorkspaceId { get; set; }
    public DocumentType Type { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
    public bool IsProcessed { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? ProcessingError { get; set; }
    public string BlobPath { get; set; } = string.Empty;
}

public sealed class UploadDocumentResponse
{
    public Guid DocumentId { get; set; }
    public Guid EvaluationWorkspaceId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ComplianceRuleDto
{
    public Guid Id { get; set; }
    public Guid EvaluationWorkspaceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string RequirementText { get; set; } = string.Empty;
    public string ClassificationCategory { get; set; } = string.Empty;
    public string ActionParty { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class GenerateRulesRequest
{
    public Guid EvaluationWorkspaceId { get; set; }
    public Guid? GuideDocumentId { get; set; }
    public Guid? AppendixDocumentId { get; set; }
    public bool ReplaceExistingRules { get; set; }
}

public sealed class GenerateRulesResponse
{
    public Guid BackgroundJobId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class StartEvaluationRequest
{
    public Guid EvaluationWorkspaceId { get; set; }
    public Guid ProspectusDocumentId { get; set; }
    public List<Guid> RuleIds { get; set; } = [];
}

public sealed class EvaluationResultItemDto
{
    public Guid RuleId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleTitle { get; set; } = string.Empty;
    public string RuleCategory { get; set; } = string.Empty;
    public string RuleActionParty { get; set; } = string.Empty;
    public string GuideReference { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EvidenceExcerpt { get; set; } = string.Empty;
    public string EvidenceLocation { get; set; } = string.Empty;
    public string EvidenceLink { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public decimal ConfidenceScore { get; set; }
}

public sealed class EvaluationRunDto
{
    public Guid Id { get; set; }
    public Guid EvaluationWorkspaceId { get; set; }
    public Guid ProspectusDocumentId { get; set; }
    public string ProspectusBlobPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public List<EvaluationResultItemDto> Results { get; set; } = [];
}

public sealed class ComplianceReportDto
{
    public Guid EvaluationWorkspaceId { get; set; }
    public Guid EvaluationRunId { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public string RunLabel { get; set; } = string.Empty;
    public string HeaderTitle { get; set; } = "AG ONE Compliance Evaluation Report";
    public string FooterText { get; set; } = "Aventra Group";
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int TotalRules { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int NeedsReviewCount { get; set; }
    public List<EvaluationResultItemDto> Items { get; set; } = [];
}

public sealed class BackgroundJobDto
{
    public Guid Id { get; set; }
    public Guid? EvaluationWorkspaceId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? RelatedDocumentId { get; set; }
    public Guid? RelatedEvaluationRunId { get; set; }
    public string? Message { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class PromptTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptFormat { get; set; } = string.Empty;
}

public sealed class UpdatePromptTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptFormat { get; set; } = string.Empty;
}
