namespace Alloca.Application.Common.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = "Alloca";
    public string Audience { get; set; } = "Alloca";
    public string Secret { get; set; } = default!;
    public int ExpirationMinutes { get; set; } = 480;
}
