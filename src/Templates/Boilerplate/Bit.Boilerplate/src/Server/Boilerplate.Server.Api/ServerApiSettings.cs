//+:cnd:noEmit
//#if (notification == true)
using AdsPush.Abstraction.Settings;
//#endif
using Boilerplate.Server.Shared;

namespace Boilerplate.Server.Api;

public partial class ServerApiSettings : ServerSharedSettings
{
    [Required]
    public AppIdentityOptions Identity { get; set; } = default!;

    [Required]
    public EmailOptions Email { get; set; } = default!;

    //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
    public AIOptions? AI { get; set; }
    //#endif

    public SmsOptions? Sms { get; set; }

    [Required]
    public string UserProfileImagesDir { get; set; } = default!;

    //#if (signalR == true)
    [Required]
    public string AiChatImagesDir { get; set; } = default!;

    /// <summary>
    /// How long an image attached to an AI chat message is kept before <c>AiChatImagesRetentionJobRunner</c> deletes it
    /// and its blob.
    /// </summary>
    public TimeSpan AiChatImagesRetention { get; set; }
    //#endif

    //#if (captcha == "reCaptcha")
    /// <summary>
    /// Create one at https://console.cloud.google.com/security/recaptcha/create for Web Application Type and use site key in Client.Core
    /// </summary>
    [Required]
    public string GoogleRecaptchaSecretKey { get; set; } = default!;
    //#endif

    //#if (notification == true)
    public AdsPushVapidSettings? AdsPushVapid { get; set; }

    public AdsPushFirebaseSettings? AdsPushFirebase { get; set; }

    public AdsPushAPNSSettings? AdsPushAPNS { get; set; }
    //#endif

    //#if (cloudflare == true)
    public CloudflareOptions? Cloudflare { get; set; }
    //#endif

    //#if (module == "Admin" || module == "Sales")
    [Required]
    public string ProductImagesDir { get; set; } = default!;
    //#endif

    public HangfireOptions? Hangfire { get; set; }

    public SupportedAppVersionsOptions? SupportedAppVersions { get; set; }

    /// <summary>
    /// The root ConnectionStrings section. Bound so <see cref="Validate"/> can reject the shared development
    /// defaults shipped in appsettings.json outside of Development.
    /// </summary>
    public Dictionary<string, string?>? ConnectionStrings { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (Identity is null)
            throw new InvalidOperationException("Identity configuration is required.");

        if (Email is null)
            throw new InvalidOperationException("Email configuration is required.");

        Validator.TryValidateObject(Identity, new ValidationContext(Identity), validationResults, true);
        Validator.TryValidateObject(Email, new ValidationContext(Email), validationResults, true);
        if (Sms is not null)
        {
            Validator.TryValidateObject(Sms, new ValidationContext(Sms), validationResults, true);
        }
        //#if (notification == true)
        if (AdsPushVapid is not null)
        {
            Validator.TryValidateObject(AdsPushVapid, new ValidationContext(AdsPushVapid), validationResults, true);
        }
        //#endif
        if (SupportedAppVersions is not null)
        {
            Validator.TryValidateObject(SupportedAppVersions, new ValidationContext(SupportedAppVersions), validationResults, true);
        }

        //#if (signalR == true)
        if (AiChatImagesRetention <= TimeSpan.Zero)
        {
            validationResults.Add(new ValidationResult($"{nameof(AiChatImagesRetention)} must be greater than zero.", [nameof(AiChatImagesRetention)]));
        }
        //#endif

        if (Identity.UnconfirmedUsersRetention <= TimeSpan.Zero)
        {
            validationResults.Add(new ValidationResult($"{nameof(AppIdentityOptions.UnconfirmedUsersRetention)} must be greater than zero.", [nameof(Identity)]));
        }

        if (AppEnvironment.IsDevelopment() is false)
        {
            // Matched on the host, not on the shipped literal: editing the sample's user name or password still leaves
            // every outgoing mail - confirmation codes and magic links included - in a public shared test mailbox.
            if (ConnectionStrings?.GetValueOrDefault("smtp")?.Contains("ethereal.email", StringComparison.OrdinalIgnoreCase) is true)
            {
                throw new InvalidOperationException("The smtp connection string still points at the shared ethereal.email test mailbox. Please set it in the server's appsettings.json file.");
            }

            //#if (captcha == "reCaptcha")
            if (GoogleRecaptchaSecretKey is "6LdMKr4pAAAAANvngWNam_nlHzEDJ2t6SfV6L_DS")
            {
                throw new InvalidOperationException("The GoogleRecaptchaSecretKey is not set. Please set it in the server's appsettings.json file.");
            }
            //#endif

            //#if (notification == true)
            if (AdsPushVapid?.PrivateKey is "dMIR1ICj-lDWYZ-ZYCwXKyC2ShYayYYkEL-oOPnpq9c" || AdsPushVapid?.Subject is "mailto:you@example.com")
            {
                throw new InvalidOperationException("The AdsPushVapid's PrivateKey and Subject are not set. Please set them in the server's appsettings.json file.");
            }
            //#endif
        }

        return validationResults;
    }
}

public partial class AppIdentityOptions : IdentityOptions
{
    /// <summary>
    /// BearerTokenExpiration used as JWT's expiration claim, access token's `expires in` and cookie's `max age`.
    /// </summary>
    public TimeSpan BearerTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }

    /// <summary>
    /// How long an unconfirmed, never-signed-in account is kept (See <see cref="Features.Identity.UnconfirmedUsersRetentionJobRunner"/>).
    /// </summary>
    public TimeSpan UnconfirmedUsersRetention { get; set; }

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    /// <summary>
    /// To either confirm and/or change email
    /// </summary>
    public TimeSpan EmailTokenLifetime { get; set; }
    /// <summary>
    /// To either confirm and/or change phone number
    /// </summary>
    public TimeSpan PhoneNumberTokenLifetime { get; set; }
    public TimeSpan ResetPasswordTokenLifetime { get; set; }
    public TimeSpan TwoFactorTokenLifetime { get; set; }

    /// <summary>
    /// <see cref="SignInManagerExtensions.OtpSignIn"/>
    /// </summary>
    public TimeSpan OtpTokenLifetime { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public int MaxPrivilegedSessionsCount { get; set; }
}

//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
public partial class AIOptions
{
    public OpenAIOptions? OpenAI { get; set; }
    public HuggingFaceOptions? HuggingFace { get; set; }
}

public class OpenAIOptions
{
    public string? ChatModel { get; set; }
    public Uri? ChatEndpoint { get; set; }
    public string? ChatApiKey { get; set; }

    public string? EmbeddingModel { get; set; }
    public Uri? EmbeddingEndpoint { get; set; }
    public string? EmbeddingApiKey { get; set; }

    //#if (signalR == true)
    public string? SpeechToTextModel { get; set; }
    public Uri? SpeechToTextEndpoint { get; set; }
    public string? SpeechToTextApiKey { get; set; }

    public string? TextToSpeechModel { get; set; }
    public Uri? TextToSpeechEndpoint { get; set; }
    public string? TextToSpeechApiKey { get; set; }
    public string? TextToSpeechVoice { get; set; }
    //#endif
}

public class HuggingFaceOptions
{
    public string? EmbeddingApiKey { get; set; }

    public string? EmbeddingEndpoint { get; set; }
}

//#endif

public partial class EmailOptions
{
    [Required]
    public string DefaultFromEmail { get; set; } = default!;
}

//#if (cloudflare == true)
public class CloudflareOptions
{
    public string? ApiToken { get; set; }

    /// <summary>
    /// The zones whose edge cache <see cref="ResponseCacheService"/> purges.
    /// A purge by cache-tag covers every hostname of a zone, so a single entry is enough unless the app is served
    /// from domains that belong to different Cloudflare zones (e.g. myapp.com and myapp.uk).
    /// </summary>
    public string[] ZoneIds { get; set; } = [];

    public bool Configured => string.IsNullOrEmpty(ApiToken) is false &&
        ZoneIds.Length > 0;
}
//#endif

public partial class SmsOptions
{
    public string? FromPhoneNumber { get; set; }
    public string? TwilioAccountSid { get; set; }
    public string? TwilioAutoToken { get; set; }

    public bool Configured => string.IsNullOrEmpty(FromPhoneNumber) is false &&
                              string.IsNullOrEmpty(TwilioAccountSid) is false &&
                              string.IsNullOrEmpty(TwilioAutoToken) is false;
}

public class HangfireOptions
{
    /// <summary>
    /// Useful for testing or in production when managing multiple codebases with a single database.
    /// </summary>
    public bool UseIsolatedStorage { get; set; }
}

public class SupportedAppVersionsOptions
{
    public Version? MinimumSupportedAndroidAppVersion { get; set; }

    public Version? MinimumSupportedIosAppVersion { get; set; }

    public Version? MinimumSupportedMacOSAppVersion { get; set; }

    public Version? MinimumSupportedWindowsAppVersion { get; set; }

    public Version? MinimumSupportedWebAppVersion { get; set; }

    public Version? GetMinimumSupportedAppVersion(AppPlatformType platformType)
    {
        return platformType switch
        {
            AppPlatformType.Android => MinimumSupportedAndroidAppVersion,
            AppPlatformType.Ios => MinimumSupportedIosAppVersion,
            AppPlatformType.MacOS => MinimumSupportedMacOSAppVersion,
            AppPlatformType.Windows => MinimumSupportedWindowsAppVersion,
            AppPlatformType.Web => MinimumSupportedWebAppVersion,
            AppPlatformType.Linux => null, // No Linux client ships a minimum version, so there is nothing to enforce.
            _ => throw new ArgumentOutOfRangeException(nameof(platformType), platformType, null)
        };
    }
}
