//+:cnd:noEmit
namespace Boilerplate.Server.Api;

public class AppSettings : IValidatableObject
{
    public IdentitySettings Identity { get; set; } = default!;

    public EmailSettings Email { get; set; } = default!;

    public SmsSettings Sms { get; set; } = default!;

    [Required]
    public string UserProfileImagesDir { get; set; } = default!;

    //#if (captcha == "reCaptcha")
    [Required]
    public string GoogleRecaptchaSecretKey { get; set; } = default!;
    //#endif

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(Identity, new ValidationContext(Identity), validationResults, true);
        Validator.TryValidateObject(Email, new ValidationContext(Email), validationResults, true);
        Validator.TryValidateObject(Sms, new ValidationContext(Sms), validationResults, true);

        return validationResults;
    }
}

public class IdentitySettings : IdentityOptions
{
    public TimeSpan BearerTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

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
    /// If true, the web app tries to store emails as .eml file in the App_Data/sent-emails folder instead of sending them using smtp server (recommended for testing purposes only).
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
    public string? TwilioAccountSid { get; set; }
    public string? TwilioAutoToken { get; set; }

    public bool Configured => string.IsNullOrEmpty(FromPhoneNumber) is false &&
                              string.IsNullOrEmpty(TwilioAccountSid) is false &&
                              string.IsNullOrEmpty(TwilioAutoToken) is false;
}
