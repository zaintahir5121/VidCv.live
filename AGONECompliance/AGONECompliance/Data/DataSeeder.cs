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
                        "For prospectus exposure, the electronic copy may redact pricing of securities and related disclosures, indicative listing timetable, and salient terms of underwriting/cornerstone agreements.",
                    ClassificationCategory = "Requirement",
                    ActionParty = "Management"
                },
                new ComplianceRule
                {
                    EvaluationWorkspaceId = defaultWorkspace.Id,
                    Code = "PG-4.11-DISCLOSURES",
                    Title = "Promoter/Director/Key Personnel Disclosures",
                    Reference = "PG 4.11",
                    RequirementText =
                        "Disclose involvement of each promoter, director, key senior management, or key technical personnel in bankruptcy, disqualification, criminal cases, judgments, civil allegations, injunctions, reprimands, and unsatisfied judgments over the last 10 years.",
                    ClassificationCategory = "Requirement",
                    ActionParty = "Management"
                },
                new ComplianceRule
                {
                    EvaluationWorkspaceId = defaultWorkspace.Id,
                    Code = "PG-1.06-DIRECTORY",
                    Title = "Directory Completeness",
                    Reference = "PG 1.06",
                    RequirementText =
                        "Directory must include details for directors, company secretary qualifications, office contact channels, key advisers, reporting accountant, experts, and stock exchange listing details where applicable.",
                    ClassificationCategory = "Requirement",
                    ActionParty = "Management"
                });
        }

        if (!await dbContext.PromptTemplates.AnyAsync(cancellationToken))
        {
            dbContext.PromptTemplates.AddRange(
                new PromptTemplate
                {
                    TemplateType = "rule-extraction",
                    Name = "Rule Extraction from Guide and Appendix",
                    Description = "Derive structured checks from appendix requirement document",
                    Version = 1,
                    IsActive = true,
                    SystemPrompt =
                        "You are an intelligent assistant that extracts compliance checks from an appendix requirement document. " +
                        "Classify each extracted item using: category = Info or Requirement, and action_party = Onsite, Management, or None (None only when category is Info). " +
                        "If requirement references multiple sections and context exists, provide the full requirement text instead of partial quotes. " +
                        "Return only a valid JSON array.",
                    UserPromptFormat =
                        "Appendix Text:\n{{appendix_text}}\n\n" +
                        "Extract checks from Appendix Text only. Do not use Guide text for rule extraction.\n" +
                        "Use this exact classification rubric:\n" +
                        "Category = Info or Requirement.\n" +
                        "Action Party = Onsite or Management; if Category is Info set Action Party to None.\n" +
                        "Info includes headings/titles, definitions/concepts, non-compliance implication statements, and reporting structure descriptions.\n" +
                        "Requirement includes explicit obligations/actions that shall be complied.\n" +
                        "Onsite includes physical/skilled workplace execution actions.\n" +
                        "Management includes documentation/notification/filing/reporting obligations.\n" +
                        "If requirement references multiple sections and context is available, provide full requirement text.\n" +
                        "Return JSON array items with fields: code, title, reference, requirementText, classificationCategory, actionParty.\n" +
                        "Allowed values:\n" +
                        "- classificationCategory: Info | Requirement\n" +
                        "- actionParty: Onsite | Management | None"
                },
                new PromptTemplate
                {
                    TemplateType = "prospectus-evaluation",
                    Name = "Prospectus Compliance Evaluation",
                    Description = "Evaluate prospectus text against selected compliance checks",
                    Version = 1,
                    IsActive = true,
                    SystemPrompt =
                        "You are a strict compliance evaluator. Determine if each rule is Compliant, NonCompliant, or NeedsReview based only on evidence in the prospectus text. " +
                        "Always identify page-level location for matched evidence when available.",
                    UserPromptFormat =
                        "Prospectus Text:\n{{prospectus_text}}\n\nRules:\n{{rules_json}}\n\n" +
                        "For each rule, use guideContext as mandatory supporting context when available. Evaluate prospectus evidence against the combined meaning of requirementText + guideContext.\n" +
                        "Return JSON array with fields: ruleCode,status,reason,evidenceExcerpt,pageNumber,confidenceScore."
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
