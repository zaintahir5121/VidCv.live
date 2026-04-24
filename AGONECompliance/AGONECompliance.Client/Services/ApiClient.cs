using System.Net.Http.Json;
using AGONECompliance.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace AGONECompliance.Client.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<List<BackgroundJobDto>> GetBackgroundJobsAsync(
        Guid evaluationWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<BackgroundJobDto>>(
                   $"api/background-jobs?evaluationWorkspaceId={evaluationWorkspaceId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<List<EvaluationWorkspaceDto>> GetEvaluationWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<EvaluationWorkspaceDto>>("api/evaluation-workspaces", cancellationToken) ?? [];
    }

    public async Task<EvaluationWorkspaceDto> CreateEvaluationWorkspaceAsync(
        CreateEvaluationWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/evaluation-workspaces", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EvaluationWorkspaceDto>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Could not create evaluation workspace.");
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync(Guid evaluationWorkspaceId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<DocumentDto>>(
                   $"api/documents?evaluationWorkspaceId={evaluationWorkspaceId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<UploadDocumentResponse> UploadDocumentAsync(
        Guid evaluationWorkspaceId,
        DocumentType type,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 500L * 1024 * 1024, cancellationToken);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await httpClient.PostAsync(
            $"api/documents/upload?evaluationWorkspaceId={evaluationWorkspaceId}&type={type}",
            content,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UploadDocumentResponse>(cancellationToken: cancellationToken)
               ?? new UploadDocumentResponse { Message = "Upload completed." };
    }

    public async Task<List<ComplianceRuleDto>> GetRulesAsync(Guid evaluationWorkspaceId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<ComplianceRuleDto>>(
                   $"api/rules?evaluationWorkspaceId={evaluationWorkspaceId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<List<ComplianceRuleDto>> GenerateRulesAsync(
        Guid evaluationWorkspaceId,
        Guid? guideId,
        Guid? appendixId,
        CancellationToken cancellationToken = default)
    {
        var payload = new GenerateRulesRequest
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            GuideDocumentId = guideId,
            AppendixDocumentId = appendixId,
            ReplaceExistingRules = false
        };

        var response = await httpClient.PostAsJsonAsync("api/rules/generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ComplianceRuleDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<ComplianceRuleDto> SetRuleActiveAsync(
        Guid evaluationWorkspaceId,
        Guid ruleId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsync(
            $"api/rules/{ruleId}/active?evaluationWorkspaceId={evaluationWorkspaceId}&isActive={isActive}",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComplianceRuleDto>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Could not update rule state.");
    }

    public async Task<EvaluationRunDto> StartEvaluationAsync(
        Guid evaluationWorkspaceId,
        Guid prospectusDocumentId,
        IReadOnlyCollection<Guid> ruleIds,
        CancellationToken cancellationToken = default)
    {
        var payload = new StartEvaluationRequest
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            ProspectusDocumentId = prospectusDocumentId,
            RuleIds = ruleIds.ToList()
        };
        var response = await httpClient.PostAsJsonAsync("api/evaluations/start", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EvaluationRunDto>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Could not start evaluation.");
    }

    public async Task<EvaluationRunDto?> GetEvaluationAsync(
        Guid evaluationWorkspaceId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<EvaluationRunDto>(
            $"api/evaluations/{runId}?evaluationWorkspaceId={evaluationWorkspaceId}",
            cancellationToken);
    }

    public async Task<List<EvaluationRunDto>> GetEvaluationsAsync(
        Guid evaluationWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<EvaluationRunDto>>(
                   $"api/evaluations?evaluationWorkspaceId={evaluationWorkspaceId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<ComplianceReportDto?> GetEvaluationReportAsync(
        Guid evaluationWorkspaceId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ComplianceReportDto>(
            $"api/evaluations/{runId}/report?evaluationWorkspaceId={evaluationWorkspaceId}",
            cancellationToken);
    }

    public async Task<List<PromptTemplateDto>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<PromptTemplateDto>>("api/prompttemplates", cancellationToken) ?? [];
    }

    public async Task<PromptTemplateDto> UpdatePromptTemplateAsync(
        Guid id,
        UpdatePromptTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/prompttemplates/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PromptTemplateDto>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Could not update prompt template.");
    }

    public async Task<SystemConfigDto> GetSystemConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SystemConfigDto>("api/system/configuration", cancellationToken)
               ?? new SystemConfigDto();
    }
}
