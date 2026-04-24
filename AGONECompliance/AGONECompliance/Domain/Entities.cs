using AGONECompliance.Shared;

namespace AGONECompliance.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EvaluationWorkspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public ICollection<UploadedDocument> Documents { get; set; } = [];
    public ICollection<ComplianceRule> Rules { get; set; } = [];
    public ICollection<EvaluationRun> EvaluationRuns { get; set; } = [];
}

public sealed class UploadedDocument : BaseEntity
{
    public Guid EvaluationWorkspaceId { get; set; }
    public EvaluationWorkspace EvaluationWorkspace { get; set; } = null!;
    public DocumentType Type { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string? FullTextBlobPath { get; set; }
    public string? ParsedJsonBlobPath { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
}

public sealed class ComplianceRule : BaseEntity
{
    public Guid EvaluationWorkspaceId { get; set; }
    public EvaluationWorkspace EvaluationWorkspace { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string RequirementText { get; set; } = string.Empty;
    public string ClassificationCategory { get; set; } = "Requirement";
    public string ActionParty { get; set; } = "Onsite";
    public bool IsActive { get; set; } = true;
}

public sealed class PromptTemplate : BaseEntity
{
    public string TemplateType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; } = true;
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptFormat { get; set; } = string.Empty;
}

public sealed class EvaluationRun : BaseEntity
{
    public Guid EvaluationWorkspaceId { get; set; }
    public EvaluationWorkspace EvaluationWorkspace { get; set; } = null!;
    public Guid ProspectusDocumentId { get; set; }
    public UploadedDocument ProspectusDocument { get; set; } = null!;
    public string Status { get; set; } = "Queued";
    public string? FailureReason { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public ICollection<EvaluationResult> Results { get; set; } = [];
}

public sealed class EvaluationResult : BaseEntity
{
    public Guid EvaluationRunId { get; set; }
    public EvaluationRun EvaluationRun { get; set; } = null!;
    public Guid RuleId { get; set; }
    public ComplianceRule Rule { get; set; } = null!;
    public ComplianceStatus Status { get; set; } = ComplianceStatus.Unknown;
    public string Reason { get; set; } = string.Empty;
    public string EvidenceExcerpt { get; set; } = string.Empty;
    public string EvidenceLocation { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public decimal ConfidenceScore { get; set; }
}

public sealed class EvaluationRunRule : BaseEntity
{
    public Guid EvaluationRunId { get; set; }
    public EvaluationRun EvaluationRun { get; set; } = null!;
    public Guid RuleId { get; set; }
}
