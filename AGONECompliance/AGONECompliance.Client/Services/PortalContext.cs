using AGONECompliance.Client.Models;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace AGONECompliance.Client.Services;

public sealed class PortalContext(ApiClient apiClient)
{
    public PortalState State { get; } = new();
    public SystemConfigDto? SystemConfig { get; private set; }
    public string StatusMessage { get; private set; } = "Upload guideline and prospectus files to begin.";
    public bool IsBusy { get; private set; }
    public ComplianceReportDto? LatestReport { get; private set; }

    public async Task RefreshAllAsync()
    {
        State.Workspaces = await apiClient.GetEvaluationWorkspacesAsync();
        State.SelectedEvaluationWorkspaceId ??= State.Workspaces.FirstOrDefault()?.Id;

        if (State.SelectedEvaluationWorkspaceId is null)
        {
            State.Documents = [];
            State.Rules = [];
            State.Runs = [];
            State.BackgroundJobs = [];
            State.SelectedGuideDocumentId = null;
            State.SelectedAppendixDocumentId = null;
            State.SelectedProspectusDocumentId = null;
            State.SelectedRuleIds.Clear();
            State.PromptTemplates = await apiClient.GetPromptTemplatesAsync();
            SystemConfig = await apiClient.GetSystemConfigurationAsync();
            return;
        }

        var workspaceId = State.SelectedEvaluationWorkspaceId.Value;
        State.Documents = await apiClient.GetDocumentsAsync(workspaceId);
        State.Rules = await apiClient.GetRulesAsync(workspaceId);
        State.Runs = await apiClient.GetEvaluationsAsync(workspaceId);
        State.BackgroundJobs = await apiClient.GetBackgroundJobsAsync(workspaceId);
        State.PromptTemplates = await apiClient.GetPromptTemplatesAsync();
        SystemConfig = await apiClient.GetSystemConfigurationAsync();

        State.SelectedGuideDocumentId ??= State.Documents.FirstOrDefault(x => x.Type == DocumentType.Guide)?.Id;
        State.SelectedAppendixDocumentId ??= State.Documents.FirstOrDefault(x => x.Type == DocumentType.Appendix)?.Id;
        State.SelectedProspectusDocumentId ??= State.Documents.FirstOrDefault(x => x.Type == DocumentType.Prospectus)?.Id;
        State.SelectedRuleIds = State.SelectedRuleIds
            .Where(x => State.Rules.Any(r => r.Id == x && r.IsActive))
            .ToHashSet();
    }

    public async Task CreateEvaluationWorkspaceAsync(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Evaluation workspace name is required.";
            return;
        }

        try
        {
            IsBusy = true;
            var workspace = await apiClient.CreateEvaluationWorkspaceAsync(new CreateEvaluationWorkspaceRequest
            {
                Name = name.Trim(),
                Description = description.Trim()
            });
            State.SelectedEvaluationWorkspaceId = workspace.Id;
            State.SelectedGuideDocumentId = null;
            State.SelectedAppendixDocumentId = null;
            State.SelectedProspectusDocumentId = null;
            State.SelectedRuleIds.Clear();
            LatestReport = null;
            await RefreshAllAsync();
            StatusMessage = $"New evaluation workspace '{workspace.Name}' created.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ChangeEvaluationWorkspaceAsync(Guid? workspaceId)
    {
        State.SelectedEvaluationWorkspaceId = workspaceId;
        State.SelectedGuideDocumentId = null;
        State.SelectedAppendixDocumentId = null;
        State.SelectedProspectusDocumentId = null;
        State.SelectedRuleIds.Clear();
        LatestReport = null;
        await RefreshAllAsync();
        StatusMessage = workspaceId is null
            ? "No evaluation workspace selected."
            : "Evaluation workspace changed.";
    }

    public async Task UploadDocumentAsync(DocumentType type, IBrowserFile file)
    {
        if (State.SelectedEvaluationWorkspaceId is null)
        {
            StatusMessage = "Select or create an evaluation workspace first.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Uploading {type} document...";
            var result = await apiClient.UploadDocumentAsync(State.SelectedEvaluationWorkspaceId.Value, type, file);
            StatusMessage = result.Message;
            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task GenerateRulesAsync()
    {
        if (State.SelectedEvaluationWorkspaceId is null)
        {
            StatusMessage = "Select or create an evaluation workspace first.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Generating rules from Guide + Appendix...";
            await apiClient.GenerateRulesAsync(
                State.SelectedEvaluationWorkspaceId.Value,
                State.SelectedGuideDocumentId,
                State.SelectedAppendixDocumentId);
            await RefreshAllAsync();
            StatusMessage = "Rules generated successfully. Select checks to evaluate.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rule generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartEvaluationAsync()
    {
        if (State.SelectedEvaluationWorkspaceId is null)
        {
            StatusMessage = "Select or create an evaluation workspace first.";
            return;
        }

        if (State.SelectedProspectusDocumentId is null || State.SelectedRuleIds.Count == 0)
        {
            StatusMessage = "Choose a prospectus and at least one rule.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Evaluation queued. Worker processes it shortly...";
            var workspaceId = State.SelectedEvaluationWorkspaceId.Value;
            var run = await apiClient.StartEvaluationAsync(workspaceId, State.SelectedProspectusDocumentId.Value, State.SelectedRuleIds);
            await PollRunAsync(run.Id);
            LatestReport = await apiClient.GetEvaluationReportAsync(workspaceId, run.Id);
            await RefreshAllAsync();
            StatusMessage = "Evaluation completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Evaluation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SavePromptAsync(PromptTemplateDto template)
    {
        await apiClient.UpdatePromptTemplateAsync(template.Id, new UpdatePromptTemplateRequest
        {
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            SystemPrompt = template.SystemPrompt,
            UserPromptFormat = template.UserPromptFormat
        });
        StatusMessage = $"Prompt template '{template.TemplateType}' saved.";
    }

    public async Task ToggleRuleActiveAsync(ComplianceRuleDto rule, bool isActive)
    {
        if (State.SelectedEvaluationWorkspaceId is null)
        {
            StatusMessage = "Select or create an evaluation workspace first.";
            return;
        }

        var updated = await apiClient.SetRuleActiveAsync(State.SelectedEvaluationWorkspaceId.Value, rule.Id, isActive);
        var target = State.Rules.FirstOrDefault(x => x.Id == updated.Id);
        if (target is not null)
        {
            target.IsActive = updated.IsActive;
        }

        if (!updated.IsActive)
        {
            State.SelectedRuleIds.Remove(updated.Id);
        }
    }

    public void ToggleRuleSelection(Guid ruleId, bool selected)
    {
        if (selected)
        {
            State.SelectedRuleIds.Add(ruleId);
        }
        else
        {
            State.SelectedRuleIds.Remove(ruleId);
        }
    }

    public void SelectAllRules()
    {
        State.SelectedRuleIds = State.Rules.Where(x => x.IsActive).Select(x => x.Id).ToHashSet();
    }

    public void ClearRuleSelection()
    {
        State.SelectedRuleIds.Clear();
    }

    private async Task PollRunAsync(Guid runId)
    {
        if (State.SelectedEvaluationWorkspaceId is null)
        {
            return;
        }

        var workspaceId = State.SelectedEvaluationWorkspaceId.Value;
        for (var i = 0; i < 24; i++)
        {
            var run = await apiClient.GetEvaluationAsync(workspaceId, runId);
            if (run is not null && (run.Status == "Completed" || run.Status == "Failed"))
            {
                return;
            }

            await Task.Delay(2000);
        }
    }
}
