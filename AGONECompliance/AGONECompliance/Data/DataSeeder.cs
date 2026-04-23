using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ComplianceDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaultWorkspace = await dbContext.EvaluationWorkspaces.FirstOrDefaultAsync(
            x => x.Name == "IPO Submission Workspace A",
            cancellationToken);
        if (defaultWorkspace is null)
        {
            defaultWorkspace = new EvaluationWorkspace
            {
                Name = "IPO Submission Workspace A",
                Description = "Primary workspace for IPO guideline and prospectus validation.",
                Status = "Active"
            };
            dbContext.EvaluationWorkspaces.Add(defaultWorkspace);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.ComplianceRules.AnyAsync(
                x => x.EvaluationWorkspaceId == defaultWorkspace.Id,
                cancellationToken))
        {
            dbContext.ComplianceRules.AddRange(
                new ComplianceRule
                {
                    EvaluationWorkspaceId = defaultWorkspace.Id,
                    Code = "PG-1.09-1.18-REDACTION",
                    Title = "Prospectus Exposure Redaction",
                    Reference = "Part B 1.09, Part E 1.18",
                    RequirementText =
                        "For prospectus exposure, the electronic copy may redact pricing of securities and related disclosures, indicative listing timetable, and salient terms of underwriting/cornerstone agreements."
                },
                new ComplianceRule
                {
                    EvaluationWorkspaceId = defaultWorkspace.Id,
                    Code = "PG-4.11-DISCLOSURES",
                    Title = "Promoter/Director/Key Personnel Disclosures",
                    Reference = "PG 4.11",
                    RequirementText =
                        "Disclose involvement of each promoter, director, key senior management, or key technical personnel in bankruptcy, disqualification, criminal cases, judgments, civil allegations, injunctions, reprimands, and unsatisfied judgments over the last 10 years."
                },
                new ComplianceRule
                {
                    EvaluationWorkspaceId = defaultWorkspace.Id,
                    Code = "PG-1.06-DIRECTORY",
                    Title = "Directory Completeness",
                    Reference = "PG 1.06",
                    RequirementText =
                        "Directory must include details for directors, company secretary qualifications, office contact channels, key advisers, reporting accountant, experts, and stock exchange listing details where applicable."
                });
        }

        if (!await dbContext.PromptTemplates.AnyAsync(cancellationToken))
        {
            dbContext.PromptTemplates.AddRange(
                new PromptTemplate
                {
                    TemplateType = "rule-extraction",
                    Name = "Rule Extraction from Guide and Appendix",
                    Description = "Derive structured checks from guideline/appendix content",
                    Version = 1,
                    IsActive = true,
                    SystemPrompt =
                        "You are a compliance extraction assistant. Convert guideline content into concise actionable checks.",
                    UserPromptFormat =
                        "Guide Text:\n{{guide_text}}\n\nAppendix Text:\n{{appendix_text}}\n\nReturn JSON array with code,title,reference,requirementText."
                },
                new PromptTemplate
                {
                    TemplateType = "prospectus-evaluation",
                    Name = "Prospectus Compliance Evaluation",
                    Description = "Evaluate prospectus text against selected compliance checks",
                    Version = 1,
                    IsActive = true,
                    SystemPrompt =
                        "You are a strict compliance evaluator. Determine if each rule is Compliant, NonCompliant, or NeedsReview based only on evidence in the prospectus text.",
                    UserPromptFormat =
                        "Prospectus Text:\n{{prospectus_text}}\n\nRules:\n{{rules_json}}\n\nReturn JSON array with ruleCode,status,reason,evidenceExcerpt,pageNumber,confidenceScore."
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
