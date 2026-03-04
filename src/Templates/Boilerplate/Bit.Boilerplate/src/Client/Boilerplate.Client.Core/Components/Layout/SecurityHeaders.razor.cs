namespace Boilerplate.Client.Core.Components.Layout;

public partial class SecurityHeaders
{
    [AutoInject] private ClientCoreSettings settings = default!;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddressProvider = default!;

    protected string? CspContent { get; set; }

    protected override void OnInitialized()
    {
        BuildCspString();
    }

    private void BuildCspString()
    {
        var apiUrl = absoluteServerAddressProvider.GetAddress().ToString();
        var webAppUrl = settings.WebAppUrl?.ToString();

        // 1. Common Trusted Origins (Your own servers)
        var ownOrigins = new List<string> { "'self'", apiUrl };
        if (string.IsNullOrWhiteSpace(webAppUrl) is false)
            ownOrigins.Add(webAppUrl);
        var ownOriginsString = string.Join(" ", ownOrigins.Distinct());

        // 2. Service Specific Origins
        var connectSrc = new List<string> { ownOriginsString };
        var imgSrc = new List<string> { ownOriginsString, "data:" };
        var scriptSrc = new List<string> { "'self'", "'unsafe-inline'", "'unsafe-eval'" };
        var frameSrc = new List<string> { "'self'" };

        // --- Add Azure App Insights & Sentry ---
        connectSrc.Add("https://dc.services.visualstudio.com https://*.in.applicationinsights.azure.com https://*.sentry.io");

        // --- Add Google reCAPTCHA ---
        scriptSrc.Add("https://www.google.com/recaptcha/ https://www.gstatic.com/");
        frameSrc.Add("https://www.google.com/recaptcha/");

        // --- Add Google Ads ---
        scriptSrc.Add("https://www.googleadservices.com https://googleads.g.doubleclick.net");
        imgSrc.Add("https://www.google.com https://googleads.g.doubleclick.net");
        frameSrc.Add("https://googleads.g.doubleclick.net");

        // Construct the final CSP string
        CspContent = $"default-src 'self' 'unsafe-inline' 'unsafe-eval' data:; " +
                     $"script-src {string.Join(" ", scriptSrc)}; " +
                     $"img-src {string.Join(" ", imgSrc)}; " +
                     $"connect-src {string.Join(" ", connectSrc)}; " +
                     $"frame-src {string.Join(" ", frameSrc)}; " +
                     $"object-src 'none';";
    }
}
