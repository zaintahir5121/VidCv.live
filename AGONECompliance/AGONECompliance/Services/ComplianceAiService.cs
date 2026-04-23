using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AGONECompliance.Data;
using AGONECompliance.Domain;
using AGONECompliance.Options;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AGONECompliance.Services;

public sealed class ComplianceAiService(
    IHttpClientFactory httpClientFactory,
    IOptions<AzureOptions> azureOptions,
    ComplianceDbContext dbContext,
    ILogger<ComplianceAiService> logger) : IComplianceAiService
{
    private readonly AzureOptions _options = azureOptions.Value;

    public async Task<List<ComplianceRule>> GenerateRulesAsync(
        string guideText,
        string appendixText,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.PromptTemplates
            .Where(x => x.TemplateType == "rule-extraction" && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null || !IsOpenAiConfigured())
        {
            return FallbackRules();
        }

        var userPrompt = template.UserPromptFormat
            .Replace("{{guide_text}}", TrimForModel(guideText))
            .Replace("{{appendix_text}}", TrimForModel(appendixText));

        var json = await CallAzureOpenAiAsync(template.SystemPrompt, userPrompt, cancellationToken);
        var parsed = TryDeserialize<List<RuleGenerationPayload>>(json);
        if (parsed is null || parsed.Count == 0)
        {
            logger.LogWarning("AI rule generation returned no structured rules. Using fallback rules.");
            return FallbackRules();
        }

        return parsed
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.RequirementText))
            .Select(x => new ComplianceRule
            {
                Code = SanitizeCode(x.Code ?? "GENERATED-RULE"),
                Title = x.Title?.Trim() ?? "Generated Rule",
                Reference = x.Reference?.Trim() ?? "Generated",
                RequirementText = x.RequirementText?.Trim() ?? string.Empty,
                IsActive = true
            })
            .ToList();
    }

    public async Task<List<RuleAssessment>> EvaluateProspectusAsync(
        string prospectusText,
        IReadOnlyCollection<ComplianceRule> selectedRules,
        CancellationToken cancellationToken)
    {
        if (selectedRules.Count == 0)
        {
            return [];
        }

        var template = await dbContext.PromptTemplates
            .Where(x => x.TemplateType == "prospectus-evaluation" && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null || !IsOpenAiConfigured())
        {
            return EvaluateWithHeuristics(prospectusText, selectedRules);
        }

        var rulesPayload = selectedRules.Select(rule => new
        {
            ruleId = rule.Id,
            ruleCode = rule.Code,
            title = rule.Title,
            reference = rule.Reference,
            requirementText = rule.RequirementText
        });

        var userPrompt = template.UserPromptFormat
            .Replace("{{prospectus_text}}", TrimForModel(prospectusText, 160_000))
            .Replace("{{rules_json}}", JsonSerializer.Serialize(rulesPayload));

        var responseContent = await CallAzureOpenAiAsync(template.SystemPrompt, userPrompt, cancellationToken);
        var parsed = TryDeserialize<List<EvaluationPayload>>(responseContent);
        if (parsed is null || parsed.Count == 0)
        {
            logger.LogWarning("AI evaluation returned no structured data. Using heuristics.");
            return EvaluateWithHeuristics(prospectusText, selectedRules);
        }

        var byRuleCode = selectedRules.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var items = new List<RuleAssessment>();
        foreach (var item in parsed)
        {
            if (string.IsNullOrWhiteSpace(item.RuleCode) || !byRuleCode.TryGetValue(item.RuleCode, out var rule))
            {
                continue;
            }

            items.Add(new RuleAssessment
            {
                RuleId = rule.Id,
                RuleCode = rule.Code,
                GuideReference = rule.Reference,
                Status = ParseStatus(item.Status),
                Reason = item.Reason?.Trim() ?? "No reason provided.",
                EvidenceExcerpt = item.EvidenceExcerpt?.Trim() ?? string.Empty,
                PageNumber = item.PageNumber,
                ConfidenceScore = ClampConfidence(item.ConfidenceScore)
            });
        }

        if (items.Count == 0)
        {
            return EvaluateWithHeuristics(prospectusText, selectedRules);
        }

        return EnsureEveryRuleHasAssessment(selectedRules, items);
    }

    private bool IsOpenAiConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.OpenAi.Endpoint)
               && !string.IsNullOrWhiteSpace(_options.OpenAi.ApiKey)
               && !string.IsNullOrWhiteSpace(_options.OpenAi.DeploymentName);
    }

    private async Task<string> CallAzureOpenAiAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var endpoint = _options.OpenAi.Endpoint.TrimEnd('/');
        var uri = $"{endpoint}/openai/deployments/{_options.OpenAi.DeploymentName}/chat/completions?api-version=2024-10-21";

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("api-key", _options.OpenAi.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient(nameof(ComplianceAiService));
        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI call failed ({(int)response.StatusCode}): {body}");
        }

        var document = JsonDocument.Parse(body);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "[]";
    }

    private static List<ComplianceRule> FallbackRules()
    {
        return
        [
            new ComplianceRule
            {
                Code = "PG-1.09-1.18-REDACTION",
                Title = "Prospectus Exposure Redaction",
                Reference = "Part B 1.09, Part E 1.18",
                RequirementText =
                    "The prospectus exposure copy may redact pricing, indicative timetable, and salient underwriting/cornerstone terms."
            },
            new ComplianceRule
            {
                Code = "PG-4.11-DISCLOSURES",
                Title = "Management Legal and Regulatory Disclosures",
                Reference = "PG 4.11",
                RequirementText =
                    "The prospectus must disclose bankruptcy, criminal, civil, regulatory and unsatisfied judgment matters for key persons in the last 10 years."
            },
            new ComplianceRule
            {
                Code = "PG-1.06-DIRECTORY",
                Title = "Directory and Adviser Details Completeness",
                Reference = "PG 1.06",
                RequirementText =
                    "The prospectus directory should list directors, secretary qualifications, office contacts, advisers, experts and stock exchange details."
            }
        ];
    }

    private static List<RuleAssessment> EvaluateWithHeuristics(
        string prospectusText,
        IReadOnlyCollection<ComplianceRule> selectedRules)
    {
        var normalized = prospectusText.ToLowerInvariant();
        var items = new List<RuleAssessment>();

        foreach (var rule in selectedRules)
        {
            var requiredTerms = GetRequiredTerms(rule);
            var hits = requiredTerms.Count(term => normalized.Contains(term, StringComparison.Ordinal));
            var confidence = requiredTerms.Count == 0 ? 0.4m : Math.Round((decimal)hits / requiredTerms.Count, 4);
            var status = confidence switch
            {
                >= 0.75m => ComplianceStatus.Compliant,
                >= 0.40m => ComplianceStatus.NeedsReview,
                _ => ComplianceStatus.NonCompliant
            };

            items.Add(new RuleAssessment
            {
                RuleId = rule.Id,
                RuleCode = rule.Code,
                GuideReference = rule.Reference,
                Status = status,
                Reason = $"Heuristic match score based on {hits}/{requiredTerms.Count} expected indicators.",
                EvidenceExcerpt = ExtractEvidence(prospectusText, requiredTerms),
                PageNumber = ExtractFirstPageNumber(prospectusText),
                ConfidenceScore = confidence
            });
        }

        return items;
    }

    private static IReadOnlyList<string> GetRequiredTerms(ComplianceRule rule)
    {
        var source = $"{rule.Title} {rule.RequirementText}".ToLowerInvariant();
        var terms = new List<string>();

        if (source.Contains("redact", StringComparison.Ordinal))
        {
            terms.AddRange(["pricing", "timetable", "underwriting"]);
        }

        if (source.Contains("disclose", StringComparison.Ordinal) || source.Contains("bankruptcy", StringComparison.Ordinal))
        {
            terms.AddRange(["bankruptcy", "criminal", "judgment", "regulatory"]);
        }

        if (source.Contains("directory", StringComparison.Ordinal) || source.Contains("adviser", StringComparison.Ordinal))
        {
            terms.AddRange(["director", "secretary", "registered office", "adviser"]);
        }

        if (terms.Count == 0)
        {
            var split = Regex.Split(rule.RequirementText.ToLowerInvariant(), @"\W+")
                .Where(x => x.Length > 5)
                .Distinct()
                .Take(8)
                .ToList();
            terms.AddRange(split);
        }

        return terms.Distinct().ToList();
    }

    private static string ExtractEvidence(string text, IReadOnlyList<string> terms)
    {
        foreach (var term in terms)
        {
            var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = Math.Max(0, index - 120);
                var length = Math.Min(text.Length - start, 320);
                return text.Substring(start, length).ReplaceLineEndings(" ");
            }
        }

        return "No direct excerpt found in heuristic mode.";
    }

    private static int? ExtractFirstPageNumber(string text)
    {
        var match = Regex.Match(text, @"\[Page\s+(?<page>\d+)\]", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups["page"].Value, out var page))
        {
            return page;
        }

        return 1;
    }

    private static List<RuleAssessment> EnsureEveryRuleHasAssessment(
        IReadOnlyCollection<ComplianceRule> selectedRules,
        IReadOnlyCollection<RuleAssessment> produced)
    {
        var byId = produced.ToDictionary(x => x.RuleId);
        var final = new List<RuleAssessment>();
        foreach (var rule in selectedRules)
        {
            if (byId.TryGetValue(rule.Id, out var existing))
            {
                final.Add(existing);
                continue;
            }

            final.Add(new RuleAssessment
            {
                RuleId = rule.Id,
                RuleCode = rule.Code,
                GuideReference = rule.Reference,
                Status = ComplianceStatus.NeedsReview,
                Reason = "No explicit model result returned for this rule.",
                EvidenceExcerpt = string.Empty,
                PageNumber = null,
                ConfidenceScore = 0.35m
            });
        }

        return final;
    }

    private static T? TryDeserialize<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            var extracted = ExtractJsonBlock(json);
            if (string.IsNullOrWhiteSpace(extracted))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(extracted, options);
        }
    }

    private static string ExtractJsonBlock(string source)
    {
        var fencedMatch = Regex.Match(source, "```json\\s*(?<json>[\\s\\S]*?)```", RegexOptions.IgnoreCase);
        if (fencedMatch.Success)
        {
            return fencedMatch.Groups["json"].Value;
        }

        var firstArray = source.IndexOf('[');
        var lastArray = source.LastIndexOf(']');
        if (firstArray >= 0 && lastArray > firstArray)
        {
            return source[firstArray..(lastArray + 1)];
        }

        var firstObj = source.IndexOf('{');
        var lastObj = source.LastIndexOf('}');
        if (firstObj >= 0 && lastObj > firstObj)
        {
            return source[firstObj..(lastObj + 1)];
        }

        return source;
    }

    private static ComplianceStatus ParseStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "compliant" => ComplianceStatus.Compliant,
            "noncompliant" => ComplianceStatus.NonCompliant,
            "non-compliant" => ComplianceStatus.NonCompliant,
            "needsreview" => ComplianceStatus.NeedsReview,
            "needs-review" => ComplianceStatus.NeedsReview,
            "needs review" => ComplianceStatus.NeedsReview,
            _ => ComplianceStatus.NeedsReview
        };
    }

    private static decimal ClampConfidence(decimal value)
    {
        if (value < 0m)
        {
            return 0m;
        }

        if (value > 1m)
        {
            return 1m;
        }

        return Math.Round(value, 4);
    }

    private static string TrimForModel(string source, int maxLength = 100_000)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        return source.Length <= maxLength ? source : source[..maxLength];
    }

    private static string SanitizeCode(string source)
    {
        var normalized = source.Trim().ToUpperInvariant();
        normalized = Regex.Replace(normalized, @"[^A-Z0-9\-]", "-");
        normalized = Regex.Replace(normalized, @"\-{2,}", "-");
        return normalized.Trim('-');
    }

    private sealed class RuleGenerationPayload
    {
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Reference { get; set; }
        public string? RequirementText { get; set; }
    }

    private sealed class EvaluationPayload
    {
        public string? RuleCode { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? EvidenceExcerpt { get; set; }
        public int? PageNumber { get; set; }
        public decimal ConfidenceScore { get; set; }
    }
}
