//+:cnd:noEmit
namespace Boilerplate.Server;

public class AppSettings : IValidatableObject
{
    public IdentitySettings IdentitySettings { get; set; } = default!;

    public EmailSettings EmailSettings { get; set; } = default!;

    public SmsSettings SmsSettings { get; set; } = default!;

    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    [Required]
    public string UserProfileImagesDir { get; set; } = default!;

    //#if (captcha == "reCaptcha")
    [Required]
    public string GoogleRecaptchaSecretKey { get; set; } = default!;
    //#endif

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(IdentitySettings, new ValidationContext(IdentitySettings), validationResults, true);
        Validator.TryValidateObject(EmailSettings, new ValidationContext(EmailSettings), validationResults, true);
        Validator.TryValidateObject(SmsSettings, new ValidationContext(SmsSettings), validationResults, true);
        Validator.TryValidateObject(HealthCheckSettings, new ValidationContext(HealthCheckSettings), validationResults, true);

        return validationResults;
    }
}

public class HealthCheckSettings
{
    public bool EnableHealthChecks { get; set; }
}

public class IdentitySettings
{
    public TimeSpan BearerTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Required]
    public string IdentityCertificatePassword { get; set; } = default!;
    public bool PasswordRequireDigit { get; set; }
    public int PasswordRequiredLength { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool RequireUniqueEmail { get; set; }
    /// <summary>
    /// To either confirm and/or change email
    /// </summary>
    public TimeSpan EmailTokenRequestResendDelay { get; set; }
    /// <summary>
    /// To either confirm and/or change phone number
    /// </summary>
    public TimeSpan PhoneNumberTokenRequestResendDelay { get; set; }
    public TimeSpan ResetPasswordTokenRequestResendDelay { get; set; }
    public TimeSpan TwoFactorTokenRequestResendDelay { get; set; }

    /// <summary>
    /// To sign in with either Otp or magic link.
    /// </summary>
    public TimeSpan OtpRequestResendDelay { get; set; }
}

public class EmailSettings
{
    [Required]
    public string Host { get; set; } = default!;
    /// <summary>
    /// If true, the web app tries to store emails as .eml file in the bin/Debug/net8.0/sent-emails folder instead of sending them using smtp server (recommended for testing purposes only).
    /// </summary>
    public bool UseLocalFolderForEmails => Host is "LocalFolder";

    [Range(1, 65535)]
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }

    [Required]
    public string DefaultFromEmail { get; set; } = default!;
    public bool HasCredential => (string.IsNullOrEmpty(UserName) is false) && (string.IsNullOrEmpty(Password) is false);
}

public class SmsSettings
{
    public string? FromPhoneNumber { get; set; }
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }

    public bool Configured => string.IsNullOrEmpty(FromPhoneNumber) is false &&
                              string.IsNullOrEmpty(AccountSid) is false &&
                              string.IsNullOrEmpty(AuthToken) is false;
}
