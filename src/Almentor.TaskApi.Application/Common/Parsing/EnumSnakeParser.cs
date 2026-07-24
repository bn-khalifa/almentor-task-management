using System.Text.Json;

namespace Almentor.TaskApi.Application.Common.Parsing;

/// <summary>
/// Parses snake_case wire values ("in_progress", "due_date") into enum
/// members, reusing the very same <see cref="JsonNamingPolicy.SnakeCaseLower"/>
/// the JSON body layer uses — so a value looks identical whether it arrives in a
/// request body or a query-string filter. Case-insensitive.
/// </summary>
public static class EnumSnakeParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            var wire = JsonNamingPolicy.SnakeCaseLower.ConvertName(candidate.ToString());
            if (string.Equals(wire, value, StringComparison.OrdinalIgnoreCase))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }

    // Null in → null out; otherwise the parsed value, or null if unrecognized
    public static TEnum? ParseOrNull<TEnum>(string? value) where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result) ? result : null;
}
