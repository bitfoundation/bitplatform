namespace BlazorDual.Api;

public class AppSettings
{
    public IdentitySettings IdentitySettings { get; set; } = default!;

    public JwtSettings JwtSettings { get; set; } = default!;

    public EmailSettings EmailSettings { get; set; } = default!;

    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    public string UserProfileImagePath { get; set; } = default!;

    public string WebServerAddress { get; set; } = default!;
}

public class HealthCheckSettings
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
    /// <summary>
    /// If true, the web app tries to store emails in the bin\sent-emails folder instead of sending them using smtp server (recommended for testing purposes only).
    /// </summary>
    public bool UseLocalFolderForEmails => Host is "LocalFolder";
    public int Port { get; set; }
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DefaulFromEmail { get; set; } = default!;
    public string DefaultFromName { get; set; } = default!;
    public bool HasCredential => (string.IsNullOrEmpty(UserName) is false) && (string.IsNullOrEmpty(Password) is false);
}
