using Azure.Identity;
using Microsoft.SemanticKernel;

namespace ZavaSemanticKernelProvider;

/// <summary>
/// Simple provider that creates a Semantic Kernel instance configured for Azure OpenAI chat completion.
/// </summary>
public class SemanticKernelProvider
{
    private readonly Kernel _kernel;

    public SemanticKernelProvider(
        string openAIConnection = "ConnectionStrings:microsoftfoundrycnnstring",
        string chatDeploymentName = "gpt-5-mini")
    {
        // Parse the connection string into endpoint + apiKey.
        var (endpoint, apiKey) = ParseAzureOpenAIConnection(openAIConnection);

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        // Add Azure OpenAI Chat Completion service using parsed connection data.
        // Removed hard-coded placeholder values and now using provided configuration. (Change)

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: chatDeploymentName,
                endpoint: endpoint,
                credentials: new DefaultAzureCredential()
            );
        }
        else
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: chatDeploymentName,
                endpoint: endpoint,
                apiKey: apiKey
            );
        }

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

        connection = connection.Trim();

        // Support endpoint-only connection string: "Endpoint=https://resource.openai.azure.com/;"
        if (connection.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
            !connection.Contains("Key=", StringComparison.OrdinalIgnoreCase) &&
            !connection.Contains("ApiKey=", StringComparison.OrdinalIgnoreCase) &&
            !connection.Contains("Api-Key=", StringComparison.OrdinalIgnoreCase))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in connection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int eq = segment.IndexOf('=');
                if (eq < 1) continue;
                string key = segment[..eq].Trim();
                string value = segment[(eq + 1)..].Trim();
                if (key.Length == 0) continue;
                dict[key] = value;
            }

            if (!dict.TryGetValue("Endpoint", out var endpointValue) || string.IsNullOrWhiteSpace(endpointValue))
            {
                dict.TryGetValue("Url", out endpointValue);
            }

            if (string.IsNullOrWhiteSpace(endpointValue))
                throw new InvalidOperationException("Endpoint not found in Azure OpenAI connection string. Expected 'Endpoint=...'.");

            return (NormalizeEndpoint(endpointValue), string.Empty);
        }

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
        }

        // General key=value parsing (preferred path).
        var dictGeneral = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in connection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = segment.IndexOf('=');
            if (eq < 1) continue;
            string key = segment[..eq].Trim();
            string value = segment[(eq + 1)..].Trim();
            if (key.Length == 0) continue;
            dictGeneral[key] = value;
        }

        if (!dictGeneral.TryGetValue("Endpoint", out var endpointGeneralValue) || string.IsNullOrWhiteSpace(endpointGeneralValue))
        {
            dictGeneral.TryGetValue("Url", out endpointGeneralValue);
        }

        if (string.IsNullOrWhiteSpace(endpointGeneralValue))
            throw new InvalidOperationException("Endpoint not found in Azure OpenAI connection string. Expected 'Endpoint=...'.");

        string? apiKey = null;
        dictGeneral.TryGetValue("Key", out apiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
            dictGeneral.TryGetValue("ApiKey", out apiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
            dictGeneral.TryGetValue("Api-Key", out apiKey);

        // If no key is present, return empty string for key.
        if (string.IsNullOrWhiteSpace(apiKey))
            return (NormalizeEndpoint(endpointGeneralValue), string.Empty);

        return (NormalizeEndpoint(endpointGeneralValue), apiKey.Trim());
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