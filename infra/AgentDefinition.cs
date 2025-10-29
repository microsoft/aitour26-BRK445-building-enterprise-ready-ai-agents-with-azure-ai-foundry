using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infra.AgentDeployment;

// Mirrors original accessibility and pattern so System.Text.Json source generator can emit implementation.
internal sealed record AgentDefinition(string Name, string Instructions, List<string>? Files);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(List<AgentDefinition>))]
internal partial class AgentDefinitionJsonContext : JsonSerializerContext
{
    // Intentionally left empty; source generator supplies implementation.
}
