using System.Text.Json;
using System.Text.Json.Serialization;

namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Mirrors the JSON options Program.cs registers (camelCase + snake_case enum
/// converter) so response deserialization in tests uses the same contract real
/// clients see, rather than System.Text.Json's unrelated defaults.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };
}
