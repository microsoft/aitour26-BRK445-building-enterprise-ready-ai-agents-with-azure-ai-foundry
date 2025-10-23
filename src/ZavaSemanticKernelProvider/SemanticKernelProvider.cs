using Microsoft.SemanticKernel;

namespace ZavaSemanticKernelProvider;

/// <summary>
/// Simple provider that creates a Semantic Kernel instance configured for Azure OpenAI chat completion.
/// </summary>
public class SemanticKernelProvider
{
    private readonly Kernel _kernel;

    public SemanticKernelProvider(
        string openAIConnection = "ConnectionStrings:aifoundry", 
        string chatDeploymentName = "gpt-5-mini")
    {
        // Parse the connection string into endpoint + apiKey.
        var (endpoint, apiKey) = ParseAzureOpenAIConnection(openAIConnection);

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        // Add Azure OpenAI Chat Completion service using parsed connection data.
        // Removed hard-coded placeholder values and now using provided configuration. (Change)
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: chatDeploymentName,
            endpoint: endpoint,
            apiKey: apiKey
        );

        _kernel = kernelBuilder.Build();
    }

    public Kernel? GetKernel() => _kernel;

    /// <summary>
    /// Parses an Azure OpenAI connection string into (endpoint, key).
    /// Expected canonical format (case-insensitive keys):
    ///   Endpoint=https://resource.openai.azure.com/;Key=YOUR_KEY;
    /// Optional API key synonyms supported: Key, ApiKey, Api-Key.
    ///
    /// Backward compatibility: a deprecated minimal format "https://...azure.com/;Key=...;" is still accepted
    /// but callers should migrate to the explicit key/value form shown above.
    /// </summary>
    private static (string endpoint, string key) ParseAzureOpenAIConnection(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
            throw new ArgumentException("Azure OpenAI connection string is null or empty.", nameof(connection));

        // Normalize any accidental surrounding whitespace/newlines.
        connection = connection.Trim();

        // Back-compat: support legacy minimal form: "https://...;Key=...;" (will be removed later).
        if (connection.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            connection.Contains(";Key=", StringComparison.OrdinalIgnoreCase) &&
            !connection.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = connection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? endpointPart = parts.FirstOrDefault(p => p.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            string? keyPart = parts.FirstOrDefault(p => p.StartsWith("Key=", StringComparison.OrdinalIgnoreCase));
            if (endpointPart is not null && keyPart is not null)
            {
                string ep = endpointPart.Trim();
                string legacyKey = keyPart[4..].Trim(); // after "Key="
                return (NormalizeEndpoint(ep), legacyKey);
            }
            // If the minimal parsing failed we continue to general parsing below for better error messages.
        }

        // General key=value parsing (preferred path).
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in connection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = segment.IndexOf('=');
            if (eq < 1) // need at least one char key
                continue; // ignore malformed pieces silently
            string key = segment[..eq].Trim();
            string value = segment[(eq + 1)..].Trim();
            if (key.Length == 0) continue;
            dict[key] = value; // last one wins
        }

        if (!dict.TryGetValue("Endpoint", out var endpointValue) || string.IsNullOrWhiteSpace(endpointValue))
        {
            // Fallback legacy alias "Url"
            dict.TryGetValue("Url", out endpointValue);
        }

        if (string.IsNullOrWhiteSpace(endpointValue))
            throw new InvalidOperationException("Endpoint not found in Azure OpenAI connection string. Expected 'Endpoint=...'.");

        if (!dict.TryGetValue("Key", out var apiKey) &&
            !dict.TryGetValue("ApiKey", out apiKey) &&
            !dict.TryGetValue("Api-Key", out apiKey))
        {
            throw new InvalidOperationException("API Key not found in Azure OpenAI connection string. Expected 'Key=' (or ApiKey/Api-Key). Example: Endpoint=https://resource.openai.azure.com/;Key=YOUR_KEY;");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API Key value is empty in Azure OpenAI connection string.");

        return (NormalizeEndpoint(endpointValue), apiKey.Trim());
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        endpoint = endpoint.Trim();
        // Ensure no trailing spaces or accidental query.
        if (!endpoint.EndsWith("/", StringComparison.Ordinal))
            endpoint += "/";
        return endpoint;
    }
}