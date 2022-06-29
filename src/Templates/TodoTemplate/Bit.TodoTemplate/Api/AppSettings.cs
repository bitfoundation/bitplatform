namespace TodoTemplate.Api;

public class AppSettings
{
    public IdentitySettings IdentitySettings { get; set; } = default!;

    public JwtSettings JwtSettings { get; set; } = default!;

    public EmailSettings EmailSettings { get; set; } = default!;

    public HealCheckSettings HealCheckSettings { get; set; } = default!;

    public string UserProfileImagePath { get; set; } = default!;

    public string WebServerAddress { get; set; } = default!;
}

public class HealCheckSettings
{
    public bool EnableHealthChecks { get; set; }
}

public class IdentitySettings
{
    public bool PasswordRequireDigit { get; set; }
    public int PasswordRequiredLength { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool RequireUniqueEmail { get; set; }
    public TimeSpan ConfirmationEmailResendDelay { get; set; }
    public TimeSpan ResetPasswordEmailResendDelay { get; set; }
}

public class JwtSettings
{
    public string IdentityCertificatePassword { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int NotBeforeMinutes { get; set; }
    public int ExpirationMinutes { get; set; }
}

public class EmailSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DefaulFromEmail { get; set; } = default!;
    public string DefaultFromName { get; set; } = default!;
    public bool HasCredential => (string.IsNullOrEmpty(UserName) is false) && (string.IsNullOrEmpty(Password) is false);
}
