using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Services;

public sealed class RuleGenerationOrchestrator(
    ComplianceDbContext dbContext,
    IBlobStorageService blobStorageService,
    IComplianceAiService aiService,
    ILogger<RuleGenerationOrchestrator> logger) : IRuleGenerationOrchestrator
{
    private const string RuleGenerationType = "RuleGeneration";

    public async Task<Guid> QueueRuleGenerationAsync(
        GenerateRulesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EvaluationWorkspaceId == Guid.Empty)
        {
            throw new InvalidOperationException("evaluationWorkspaceId is required.");
        }

        var workspaceExists = await dbContext.EvaluationWorkspaces.AnyAsync(
            x => x.Id == request.EvaluationWorkspaceId,
            cancellationToken);
        if (!workspaceExists)
        {
            throw new InvalidOperationException("Evaluation workspace not found.");
        }

        var job = new BackgroundJobRun
        {
            EvaluationWorkspaceId = request.EvaluationWorkspaceId,
            JobType = RuleGenerationType,
            Status = "Queued",
            Message = $"Rule generation queued for workspace {request.EvaluationWorkspaceId:N}",
            RelatedDocumentId = request.GuideDocumentId,
            RelatedRuleGenerationRequestId = request.AppendixDocumentId
        };

        dbContext.BackgroundJobRuns.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task ProcessNextPendingJobAsync(CancellationToken cancellationToken)
    {
        var job = await dbContext.BackgroundJobRuns
            .Where(x => x.JobType == RuleGenerationType && x.Status == "Queued")
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = "Running";
        job.StartedAtUtc = DateTimeOffset.UtcNow;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            string guideText = string.Empty;
            if (job.RelatedDocumentId.HasValue)
            {
                var guidePath = await dbContext.UploadedDocuments
                    .Where(x => x.Id == job.RelatedDocumentId.Value
                                && x.EvaluationWorkspaceId == job.EvaluationWorkspaceId
                                && x.Type == DocumentType.Guide)
                    .Select(x => x.FullTextBlobPath)
                    .FirstOrDefaultAsync(cancellationToken);
                guideText = await blobStorageService.DownloadTextAsync(guidePath, cancellationToken) ?? string.Empty;
            }

            string appendixText = string.Empty;
            if (job.RelatedRuleGenerationRequestId.HasValue)
            {
                var appendixPath = await dbContext.UploadedDocuments
                    .Where(x => x.Id == job.RelatedRuleGenerationRequestId.Value
                                && x.EvaluationWorkspaceId == job.EvaluationWorkspaceId
                                && x.Type == DocumentType.Appendix)
                    .Select(x => x.FullTextBlobPath)
                    .FirstOrDefaultAsync(cancellationToken);
                appendixText = await blobStorageService.DownloadTextAsync(appendixPath, cancellationToken) ?? string.Empty;
            }

            var generated = await aiService.GenerateRulesAsync(guideText, appendixText, cancellationToken);
            if (generated.Count == 0)
            {
                throw new InvalidOperationException("No rules were generated from the supplied documents.");
            }

            var existingCodes = await dbContext.ComplianceRules
                .Where(x => x.EvaluationWorkspaceId == job.EvaluationWorkspaceId)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);
            var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in generated)
            {
                var baseCode = rule.Code;
                var counter = 1;
                while (existingSet.Contains(rule.Code))
                {
                    rule.Code = $"{baseCode}-{counter++}";
                }

                rule.EvaluationWorkspaceId = job.EvaluationWorkspaceId;
                existingSet.Add(rule.Code);
                dbContext.ComplianceRules.Add(rule);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            job.Status = "Completed";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.Message = $"Rule generation completed. Added {generated.Count} rules.";
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing rule generation job {JobId}.", job.Id);
            job.Status = "Failed";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.FailureReason = ex.Message;
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
