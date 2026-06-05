namespace Finbridge.Application.Configuration;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "finbridge";
    public string Audience { get; set; } = "finbridge";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
}
