using System.Text;
using System.Text.Json;
using Experion.Backend.Models;
using Experion.Backend.Options;
using Microsoft.Extensions.Options;

namespace Experion.Backend.Services;

public sealed class OpenAiSuggestionClient(
    IHttpClientFactory httpClientFactory,
    ITokenEstimator tokenEstimator,
    IOptions<ExperionModuleOptions> options,
    ILogger<OpenAiSuggestionClient> logger) : IOpenAiSuggestionClient
{
    private readonly ExperionModuleOptions _options = options.Value;

    public async Task<OpenAiSuggestionResult> GenerateSuggestionAsync(
        string userPrompt,
        string cleanedDom,
        CancellationToken cancellationToken)
    {
        var fallback = BuildFallback(userPrompt, cleanedDom, tokenEstimator);
        if (!_options.OpenAi.Enabled
            || string.IsNullOrWhiteSpace(_options.OpenAi.Endpoint)
            || string.IsNullOrWhiteSpace(_options.OpenAi.ApiKey)
            || string.IsNullOrWhiteSpace(_options.OpenAi.DeploymentName))
        {
            return fallback;
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(OpenAiSuggestionClient));
            var endpoint = _options.OpenAi.Endpoint.TrimEnd('/');
            var url =
                $"{endpoint}/openai/deployments/{_options.OpenAi.DeploymentName}/chat/completions?api-version=2024-12-01-preview";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.OpenAi.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    messages = new object[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are Experion. Provide concise enterprise suggestions based on user prompt and UI DOM context."
                        },
                        new
                        {
                            role = "user",
                            content = $"Prompt={userPrompt}\n\nDOM:\n{cleanedDom}"
                        }
                    },
                    temperature = 0.2,
                    max_tokens = 320
                }),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI request failed with status {StatusCode}.", response.StatusCode);
                return fallback;
            }

            using var parsed = JsonDocument.Parse(raw);
            var suggestion = parsed.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? fallback.Suggestion;

            var promptTokens = fallback.PromptTokens;
            var completionTokens = tokenEstimator.Estimate(suggestion);
            if (parsed.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var p) && p.TryGetInt32(out var pValue))
                {
                    promptTokens = pValue;
                }

                if (usage.TryGetProperty("completion_tokens", out var c) && c.TryGetInt32(out var cValue))
                {
                    completionTokens = cValue;
                }
            }

            return new OpenAiSuggestionResult
            {
                Suggestion = suggestion,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI suggestion failed. Falling back.");
            return fallback;
        }
    }

    private static OpenAiSuggestionResult BuildFallback(string prompt, string cleanedDom, ITokenEstimator tokenEstimator)
    {
        var preview = cleanedDom.Length > 180 ? $"{cleanedDom[..180]}..." : cleanedDom;
        var suggestion =
            $"[LLM fallback] For '{prompt}', use this context: {preview}. Next step: confirm key inputs and proceed.";
        return new OpenAiSuggestionResult
        {
            Suggestion = suggestion,
            PromptTokens = tokenEstimator.Estimate(prompt) + tokenEstimator.Estimate(cleanedDom),
            CompletionTokens = tokenEstimator.Estimate(suggestion)
        };
    }
}
