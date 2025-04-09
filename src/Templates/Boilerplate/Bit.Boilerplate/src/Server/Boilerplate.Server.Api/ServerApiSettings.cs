//+:cnd:noEmit
//#if (notification == true)
using AdsPush.Abstraction.Settings;
//#endif
using System.Text;
using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api;

public partial class ServerApiSettings : SharedSettings
{
    [Required]
    public AppIdentityOptions Identity { get; set; } = default!;

    [Required]
    public EmailOptions Email { get; set; } = default!;

    public AIOptions? AI { get; set; }

    public SmsOptions? Sms { get; set; }

    [Required]
    public string UserProfileImagesDir { get; set; } = default!;

    //#if (captcha == "reCaptcha")
    [Required]
    public string GoogleRecaptchaSecretKey { get; set; } = default!;
    //#endif

    //#if (notification == true)
    public AdsPushVapidSettings? AdsPushVapid { get; set; }

    public AdsPushFirebaseSettings? AdsPushFirebase { get; set; }

    public AdsPushAPNSSettings? AdsPushAPNS { get; set; }
    //#endif

    public ForwardedHeadersOptions? ForwardedHeaders { get; set; }

    //#if (cloudflare == true)
    public CloudflareOptions? Cloudflare { get; set; }
    //#endif

    public ResponseCachingOptions ResponseCaching { get; set; } = default!;

    /// <summary>
    /// Lists the permitted origins for CORS requests, return URLs following social sign-in and email confirmation, etc., along with allowed origins for Web Auth.
    /// </summary>
    public Uri[] TrustedOrigins { get; set; } = [];

    //#if (module == "Admin" || module == "Sales")
    [Required]
    public string ProductImagesDir { get; set; } = default!;
    //#endif

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (Identity is null)
            throw new InvalidOperationException("Identity configuration is required.");

        if (AI is not null)
        {
            Validator.TryValidateObject(AI, new ValidationContext(AI), validationResults, true);
        }

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
        if (ForwardedHeaders is not null)
        {
            Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);
        }
        Validator.TryValidateObject(ResponseCaching, new ValidationContext(ResponseCaching), validationResults, true);

        const int MinimumJwtIssuerSigningKeySecretByteLength = 64; // 512 bits = 64 bytes, minimum for HS512
        var jwtIssuerSigningKeySecretByteLength = Encoding.UTF8.GetBytes(Identity.JwtIssuerSigningKeySecret).Length;
        if (jwtIssuerSigningKeySecretByteLength <= MinimumJwtIssuerSigningKeySecretByteLength)
        {
            throw new ArgumentException(
                $"The JWT signing key must be greater than {MinimumJwtIssuerSigningKeySecretByteLength} bytes " +
                $"({MinimumJwtIssuerSigningKeySecretByteLength * 8} bits) for HS512. Current key is {jwtIssuerSigningKeySecretByteLength} bytes.");
        }

        if (AppEnvironment.IsDev() is false)
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

    internal bool IsAllowedOrigin(Uri origin)
    {
        return TrustedOrigins.Any(trustedOrigin => trustedOrigin == origin)
            || TrustedOriginsRegex().IsMatch(origin.ToString());
    }

    //-:cnd:noEmit
    /// <summary>
    /// Blazor Hybrid's webview, localhost, devtunnels, github codespaces.
    /// </summary>
#if Development
    [GeneratedRegex(@"^(http|https|app):\/\/(localhost|0\.0\.0\.0|0\.0\.0\.1|127\.0\.0\.1|.*?devtunnels\.ms|.*?github\.dev)(:\d+)?(\/.*)?$")]
#else
    [GeneratedRegex(@"^(http|https|app):\/\/(localhost|0\.0\.0\.0|0\.0\.0\.1|127\.0\.0\.1)(:\d+)?(\/.*)?$")]
#endif
    //+:cnd:noEmit
    private partial Regex TrustedOriginsRegex();
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
    public int MaxConcurrentPrivilegedSessions { get; set; }
}

public partial class AIOptions
{
    [Required]
    public string? Model { get; set; }

    [Required]
    public Uri? Endpoint { get; set; }

    [Required]
    public string? ApiKey { get; set; }
}

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

public class ResponseCachingOptions
{
    /// <summary>
    /// Enables ASP.NET Core's response output caching
    /// </summary>
    public bool EnableOutputCaching { get; set; }

    /// <summary>
    /// Enables CDN's edge servers caching
    /// </summary>
    public bool EnableCdnEdgeCaching { get; set; }
}
