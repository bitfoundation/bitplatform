//+:cnd:noEmit
//#if (notification == true)
using AdsPush.Abstraction.Settings;
//#endif
using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api;

public partial class ServerApiSettings : SharedSettings
{
    /// <summary>
    /// It can also be configured using: dotnet user-secrets set 'DataProtectionCertificatePassword' '@nyPassw0rd'
    /// </summary>
    [Required]
    public string DataProtectionCertificatePassword { get; set; } = default!;

    [Required]
    public AppIdentityOptions Identity { get; set; } = default!;

    [Required]
    public EmailOptions Email { get; set; } = default!;

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

    /// <summary>
    /// Defines the list of origins permitted for CORS access to the API. These origins are also valid for use as return URLs after social sign-ins and for generating URLs in emails.
    /// </summary>
    public Uri[] AllowedOrigins { get; set; } = [];

    //#if (module == "Admin" || module == "Sales")
    [Required]
    public string ProductImagesDir { get; set; } = default!;
    //#endif

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
        if (ForwardedHeaders is not null)
        {
            Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);
        }

        if (AppEnvironment.IsDev() is false)
        {
            if (DataProtectionCertificatePassword is "P@ssw0rdP@ssw0rd")
            {
                throw new InvalidOperationException(@"The default test certificate is still in use. Please replace it with a new one by running the 'dotnet dev-certs https --export-path DataProtectionCertificate.pfx --password @nyPassw0rd'
command in the Server.Api's project's folder and replace P@ssw0rdP@ssw0rd with the new password.");
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
        return AllowedOrigins.Any(allowedOrigin => allowedOrigin == origin)
            || AllowedOriginsRegex().IsMatch(origin.ToString());
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
    private partial Regex AllowedOriginsRegex();
}

public partial class AppIdentityOptions : IdentityOptions
{
    /// <summary>
    /// BearerTokenExpiration used as JWT's expiration claim, access token's expires in and cookie's max age.
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
    /// To sign in with either Otp or magic link.
    /// </summary>
    public TimeSpan OtpTokenLifetime { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public int MaxConcurrentPrivilegedSessions { get; set; }
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
    /// If multiple Cloudflare-hosted domains point to your backend, you will need to
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
