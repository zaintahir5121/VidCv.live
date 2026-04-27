using System.Text.RegularExpressions;

namespace Experion.Backend.Services;

public sealed class SimpleTokenEstimator : ITokenEstimator
{
    private static readonly Regex TokenRegex = new("[A-Za-z0-9_]+|[^\\s]", RegexOptions.Compiled);

    public int Estimate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return TokenRegex.Matches(text).Count;
    }
}
