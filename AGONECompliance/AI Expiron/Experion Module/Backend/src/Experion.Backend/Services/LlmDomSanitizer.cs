using System.Text.RegularExpressions;
using Experion.Backend.Models;

namespace Experion.Backend.Services;

public sealed class LlmDomSanitizer : IDomSanitizer
{
    public Task<DomSanitizeResult> SanitizeAsync(
        string rawDom,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var withoutScripts = Regex.Replace(
            rawDom ?? string.Empty,
            "<script[\\s\\S]*?</script>",
            string.Empty,
            RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(
            withoutScripts,
            "<style[\\s\\S]*?</style>",
            string.Empty,
            RegexOptions.IgnoreCase);
        var plainText = Regex.Replace(withoutStyles, "<[^>]+>", " ");
        plainText = Regex.Replace(plainText, "\\s+", " ").Trim();
        if (plainText.Length > 4000)
        {
            plainText = plainText[..4000];
        }

        var compactDom = Regex.Replace(withoutStyles, "\\s+", " ").Trim();
        if (compactDom.Length > 8000)
        {
            compactDom = compactDom[..8000];
        }

        var summary = string.IsNullOrWhiteSpace(userPrompt)
            ? plainText
            : $"{userPrompt.Trim()} | {plainText}";
        if (summary.Length > 240)
        {
            summary = $"{summary[..240]}...";
        }

        return Task.FromResult(new DomSanitizeResult
        {
            CleanedDom = compactDom,
            NormalizedText = plainText,
            DomSummary = summary
        });
    }
}
