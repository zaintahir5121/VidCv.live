using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Services;

public sealed class EvaluationOrchestrator(
    ComplianceDbContext dbContext,
    IComplianceAiService aiService,
    IComplianceSearchService searchService,
    ILogger<EvaluationOrchestrator> logger) : IEvaluationOrchestrator
{
    public async Task<Guid> QueueEvaluationAsync(
        Guid evaluationWorkspaceId,
        Guid prospectusDocumentId,
        IReadOnlyCollection<Guid> ruleIds,
        CancellationToken cancellationToken)
    {
        var hasWorkspace = await dbContext.EvaluationWorkspaces.AnyAsync(
            x => x.Id == evaluationWorkspaceId,
            cancellationToken);
        if (!hasWorkspace)
        {
            throw new InvalidOperationException("Evaluation workspace not found.");
        }

        var hasProspectus = await dbContext.UploadedDocuments.AnyAsync(
            x => x.Id == prospectusDocumentId
                 && x.EvaluationWorkspaceId == evaluationWorkspaceId
                 && x.Type == DocumentType.Prospectus,
            cancellationToken);

        if (!hasProspectus)
        {
            throw new InvalidOperationException("Prospectus document not found.");
        }

        var ruleSet = await dbContext.ComplianceRules
            .Where(x => x.EvaluationWorkspaceId == evaluationWorkspaceId
                        && x.IsActive
                        && ruleIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (ruleSet.Count == 0)
        {
            throw new InvalidOperationException("No active rules selected.");
        }

        var run = new EvaluationRun
        {
            EvaluationWorkspaceId = evaluationWorkspaceId,
            ProspectusDocumentId = prospectusDocumentId,
            Status = "Queued"
        };

        dbContext.EvaluationRuns.Add(run);
        dbContext.EvaluationRunRules.AddRange(ruleSet.Select(ruleId => new EvaluationRunRule
        {
            EvaluationRun = run,
            RuleId = ruleId
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return run.Id;
    }

    public async Task ProcessNextPendingRunAsync(CancellationToken cancellationToken)
    {
        var run = await dbContext.EvaluationRuns
            .Where(x => x.Status == "Queued")
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (run is null)
        {
            return;
        }

        try
        {
            run.Status = "Processing";
            run.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var prospectus = await dbContext.UploadedDocuments
                .FirstOrDefaultAsync(
                    x => x.Id == run.ProspectusDocumentId && x.EvaluationWorkspaceId == run.EvaluationWorkspaceId,
                    cancellationToken);
            if (prospectus is null || string.IsNullOrWhiteSpace(prospectus.FullText))
            {
                throw new InvalidOperationException("Prospectus text is not available for evaluation.");
            }

            var selectedRuleIds = await dbContext.EvaluationRunRules
                .Where(x => x.EvaluationRunId == run.Id)
                .Select(x => x.RuleId)
                .ToListAsync(cancellationToken);

            var rules = await dbContext.ComplianceRules
                .Where(x => x.EvaluationWorkspaceId == run.EvaluationWorkspaceId && selectedRuleIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            var assessments = await aiService.EvaluateProspectusAsync(prospectus.FullText, rules, cancellationToken);

            var existing = await dbContext.EvaluationResults
                .Where(x => x.EvaluationRunId == run.Id)
                .ToListAsync(cancellationToken);
            dbContext.EvaluationResults.RemoveRange(existing);

            dbContext.EvaluationResults.AddRange(assessments.Select(item => new EvaluationResult
            {
                EvaluationRunId = run.Id,
                RuleId = item.RuleId,
                Status = item.Status,
                Reason = item.Reason,
                EvidenceExcerpt = item.EvidenceExcerpt,
                EvidenceLocation = item.PageNumber is null
                    ? "Prospectus (location not identified)"
                    : $"Prospectus page {item.PageNumber}",
                PageNumber = item.PageNumber,
                ConfidenceScore = item.ConfidenceScore
            }));

            run.Status = "Completed";
            run.CompletedAtUtc = DateTimeOffset.UtcNow;
            run.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await searchService.IndexDocumentAsync(prospectus, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process evaluation run {RunId}.", run.Id);
            run.Status = "Failed";
            run.FailureReason = ex.Message;
            run.CompletedAtUtc = DateTimeOffset.UtcNow;
            run.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
