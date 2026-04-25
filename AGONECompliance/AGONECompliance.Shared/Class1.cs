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

public sealed class DocumentPageEvidenceDto
{
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string PdfPageLink { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
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

public sealed class ExperionSessionBootstrapRequest
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public string ScreenContext { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public string UserTimezone { get; set; } = "UTC";
}

public sealed class ExperionRequestContext
{
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}

public sealed class ExperionConsentFlagsDto
{
    public bool ProfileDataAllowed { get; set; }
    public bool ProductDataAllowed { get; set; }
    public bool DocumentDataAllowed { get; set; }
}

public sealed class ExperionSessionBootstrapResponse
{
    public Guid SessionId { get; set; }
    public Guid ConversationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public bool RequiresCriticalConfirmation { get; set; } = true;
    public int TtlSeconds { get; set; } = 1800;
    public List<string> AllowedActions { get; set; } = [];
    public ExperionConsentFlagsDto ConsentFlags { get; set; } = new();
}

public sealed class ExperionContextTriggerRequest
{
    public Guid SessionId { get; set; }
    public string TriggerType { get; set; } = "manual";
    public string PageUrl { get; set; } = string.Empty;
    public string DomContext { get; set; } = string.Empty;
    public string SelectionText { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string ScreenshotRef { get; set; } = string.Empty;
}

public sealed class ExperionContextTriggerResponse
{
    public string DetectedIntent { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string UiMode { get; set; } = "expanded";
    public List<string> SuggestedPrompts { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];
}

public sealed class ExperionConversationMessageRequest
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ContextVersion { get; set; } = 1;
}

public sealed class ExperionProposedActionDto
{
    public string ActionName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
}

public sealed class ExperionConversationMessageResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool RequiresConfirmation { get; set; }
    public List<string> MissingInputs { get; set; } = [];
    public List<ExperionProposedActionDto> ProposedActions { get; set; } = [];
}

public sealed class ExperionActionExecuteRequest
{
    public Guid SessionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string ActionPayload { get; set; } = "{}";
    public string ConfirmationToken { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ExperionActionExecuteResponse
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProgressMessage { get; set; } = string.Empty;
    public string UndoToken { get; set; } = string.Empty;
}

public sealed class ExperionActionStatusDto
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Step { get; set; }
    public int StepCount { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public string AuditRef { get; set; } = string.Empty;
}

public sealed class ExperionAuditEventDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

public sealed class ExperionErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public bool IsRetryable { get; set; }
    public string ResolutionHint { get; set; } = string.Empty;
}
