//+:cnd:noEmit
//#if (notification == true)
using AdsPush.Abstraction.Settings;
//#endif
using System.Text;
using Boilerplate.Server.Api.Services;
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

        const int MinimumJwtIssuerSigningKeySecretByteLength = 64; // 512 bits = 64 bytes, minimum for HS512
        var jwtIssuerSigningKeySecretByteLength = Encoding.UTF8.GetBytes(Identity.JwtIssuerSigningKeySecret).Length;
        if (jwtIssuerSigningKeySecretByteLength <= MinimumJwtIssuerSigningKeySecretByteLength)
        {
            throw new ArgumentException(
                $"The JWT signing key must be greater than {MinimumJwtIssuerSigningKeySecretByteLength} bytes " +
                $"({MinimumJwtIssuerSigningKeySecretByteLength * 8} bits) for HS512. Current key is {jwtIssuerSigningKeySecretByteLength} bytes.");
        }

        if (AppEnvironment.IsDevelopment() is false)
        {
            if (Identity.JwtIssuerSigningKeySecret is "VeryLongJWTIssuerSiginingKeySecretThatIsMoreThan64BytesToEnsureCompatibilityWithHS512Algorithm")
            {
                throw new InvalidOperationException(@"Please replace JwtIssuerSigningKeySecret with a new one.");
            }

            //#if (captcha == "reCaptcha")
            if (GoogleRecaptchaSecretKey is "6LdMKr4pAAAAANvngWNam_nlHzEDJ2t6SfV6L_DS")
            {
                throw new InvalidOperationException("The GoogleRecaptchaSecretKey is not set. Please set it in the server's appsettings.json file.");
            }
            //#endif

            //#if (notification == true)
            if (AdsPushVapid?.PrivateKey is "dMIR1ICj-lDWYZ-ZYCwXKyC2ShYayYYkEL-oOPnpq9c" || AdsPushVapid?.Subject is "mailto:test@bitplatform.dev")
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
    [Required]
    public string JwtIssuerSigningKeySecret { get; set; } = default!;

    /// <summary>
    /// BearerTokenExpiration used as JWT's expiration claim, access token's `expires in` and cookie's `max age`.
    /// </summary>
    public TimeSpan BearerTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }

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
    /// <see cref="SignInManagerExtensions.OtpSignInAsync(SignInManager{Models.Identity.User}, Models.Identity.User, string)"/>
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
    public AzureOpenAIOptions? AzureOpenAI { get; set; }
}

public class OpenAIOptions
{
    public string? ChatModel { get; set; }
    public Uri? ChatEndpoint { get; set; }
    public string? ChatApiKey { get; set; }

    public string? EmbeddingModel { get; set; }
    public Uri? EmbeddingEndpoint { get; set; }
    public string? EmbeddingApiKey { get; set; }
}

public class AzureOpenAIOptions
{
    public string? ChatModel { get; set; }
    public Uri? ChatEndpoint { get; set; }
    public string? ChatApiKey { get; set; }

    public string? EmbeddingModel { get; set; }
    public Uri? EmbeddingEndpoint { get; set; }
    public string? EmbeddingApiKey { get; set; }
}

//#endif

public partial class EmailOptions
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

//#if (cloudflare == true)
public class CloudflareOptions
{
    public string? ApiToken { get; set; }

    public string? ZoneId { get; set; }

    /// <summary>
    /// The <see cref="ResponseCacheService"/> clears the cache for the current domain by default.
    /// If multiple Cloudflare-hosted domains point to your origin backend, you will need to
    /// purge the cache for each of them individually.
    /// </summary>
    public Uri[] AdditionalDomains { get; set; } = [];

    public bool Configured => string.IsNullOrEmpty(ApiToken) is false &&
        string.IsNullOrEmpty(ZoneId) is false;
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
            _ => throw new ArgumentOutOfRangeException(nameof(platformType), platformType, null)
        };
    }
}
