using AGONECompliance.Shared;

namespace AGONECompliance.Client.Models;

public sealed class PortalState
{
    public List<EvaluationWorkspaceDto> Workspaces { get; set; } = [];
    public Guid? SelectedEvaluationWorkspaceId { get; set; }
    public List<DocumentDto> Documents { get; set; } = [];
    public List<ComplianceRuleDto> Rules { get; set; } = [];
    public List<EvaluationRunDto> Runs { get; set; } = [];
    public List<PromptTemplateDto> PromptTemplates { get; set; } = [];
    public Guid? SelectedGuideDocumentId { get; set; }
    public Guid? SelectedAppendixDocumentId { get; set; }
    public Guid? SelectedProspectusDocumentId { get; set; }
    public HashSet<Guid> SelectedRuleIds { get; set; } = [];
}
