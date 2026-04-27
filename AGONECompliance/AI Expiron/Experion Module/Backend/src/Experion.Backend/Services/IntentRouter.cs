using System.Text.RegularExpressions;
using Experion.Backend.Models;

namespace Experion.Backend.Services;

public sealed class IntentRouter : IIntentRouter
{
    private static readonly HashSet<string> ActionTokens =
    [
        "create",
        "update",
        "delete",
        "submit",
        "approve",
        "assign",
        "send",
        "execute",
        "publish",
        "post",
        "facebook",
        "api",
        "call",
        "book",
        "schedule"
    ];

    public ExperionContextMode DecideContext(ExperionContextInput input)
    {
        var source = $"{input.UserPrompt} {input.CleanedDom}";
        var tokens = Regex.Matches(source.ToLowerInvariant(), "[a-z0-9]+")
            .Select(m => m.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return tokens.Overlaps(ActionTokens) ? ExperionContextMode.Action : ExperionContextMode.Llm;
    }
}
