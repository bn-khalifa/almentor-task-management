namespace Almentor.TaskApi.Infrastructure.Auth;

/// <summary>
/// Strongly-typed JWT config, bound from the "Jwt" configuration section. The
/// signing <see cref="Key"/> is a secret (user-secrets / env); the rest live in
/// appsettings.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
