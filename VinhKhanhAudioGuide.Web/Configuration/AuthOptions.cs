namespace VinhKhanhAudioGuide.Web.Configuration;

public class AuthOptions
{
    public List<AuthUserOption> Users { get; set; } = new();
    
    public string JwtSecret { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public int JwtExpiryDays { get; set; } = 7;
}

public class AuthUserOption
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> LocationIds { get; set; } = new();
}
