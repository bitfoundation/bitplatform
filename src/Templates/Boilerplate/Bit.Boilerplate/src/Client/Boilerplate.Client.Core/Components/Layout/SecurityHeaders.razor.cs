//+:cnd:noEmit
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
        var ownOrigins = new HashSet<string> { "'self'", apiUrl, apiUrl.Replace("http:", "ws:").Replace("https:", "wss:") };
        if (string.IsNullOrWhiteSpace(webAppUrl) is false)
            ownOrigins.Add(webAppUrl);
        var ownOriginsString = string.Join(" ", ownOrigins);

        // 2. Service Specific Origins
        var connectSrc = new HashSet<string>(ownOrigins);
        var imgSrc = new HashSet<string> { ownOriginsString, "data:" };
        var scriptSrc = new HashSet<string> { "'self'", "'unsafe-inline'" };
        var styleSrc = new HashSet<string> { "'self'", "'unsafe-inline'" };
        var fontSrc = new HashSet<string> { "'self'" };
        var frameSrc = new HashSet<string> { "'self'" };
        var mediaSrc = new HashSet<string> { "'self'" };

        //#if (appInsights == true)
        // --- Add Azure App Insights ---
        connectSrc.Add("https://dc.services.visualstudio.com https://*.in.applicationinsights.azure.com");
        //#endif

        //#if (appInsights == true)
        // --- Add Sentry ---
        connectSrc.Add("https://*.sentry.io");
        //#endif

        //#if (captcha == "reCaptcha")
        // --- Add Google reCAPTCHA ---
        connectSrc.Add("https://www.google.com/");
        scriptSrc.Add("https://www.google.com/recaptcha/ https://www.gstatic.com/");
        frameSrc.Add("https://www.google.com/recaptcha/");
        //#endif

        //#if (ads == true)
        // --- Add Google Ads ---
        scriptSrc.Add("https://www.googleadservices.com https://googleads.g.doubleclick.net https://securepubads.g.doubleclick.net https://*.adtrafficquality.google https://*.googlesyndication.com");
        connectSrc.Add("https://securepubads.g.doubleclick.net https://*.adtrafficquality.google https://*.googlesyndication.com https://csi.gstatic.com");
        imgSrc.Add("https://www.google.com https://googleads.g.doubleclick.net https://*.googlesyndication.com https://www.gstatic.com https://imasdk.googleapis.com https://*.adtrafficquality.google");
        frameSrc.Add("https://googleads.g.doubleclick.net https://*.googlesyndication.com https://*.adtrafficquality.google");
        mediaSrc.Add("https://*.gvt1.com");
        //#endif

        // --- Add Google Fonts ---
        fontSrc.Add("https://fonts.gstatic.com");
        styleSrc.Add("https://fonts.googleapis.com");

        // --- Add for home page's github stats ---
        imgSrc.Add("https://img.shields.io");
        connectSrc.Add("https://api.github.com");

        if (AppEnvironment.IsDevelopment())
        {
            connectSrc.Add("ws://localhost:* wss://localhost:*"); // Allow localhost WebSocket connections during development (hot reload / debugging)
            connectSrc.Add("https://raw.githubusercontent.com/"); // Source maps
        }

        // Construct the final CSP string
        CspContent = $"default-src {string.Join(" ", ownOrigins)}; " + // Fallback for all directives.
                     $"script-src {string.Join(" ", scriptSrc)}; " +   // Defines valid sources for executable JavaScript.
                     $"style-src {string.Join(" ", styleSrc)}; " +     // Specifies trusted sources for CSS stylesheets.
                     $"font-src {string.Join(" ", fontSrc)}; " +       // Controls where web fonts (e.g., Google Fonts) can be loaded from.
                     $"img-src {string.Join(" ", imgSrc)}; " +         // Defines allowed origins for images.
                     $"connect-src {string.Join(" ", connectSrc)}; " + // Limits targets for API calls (fetch, XHR, WebSockets).
                     $"frame-src {string.Join(" ", frameSrc)}; " +     // Restricts sources for iframes and nested browsing contexts.
                     $"media-src {string.Join(" ", mediaSrc)}; " +     // Controls where video/audio media can be loaded from.
                     $"worker-src 'self'; " +                          // Restricts the sources from which Web Workers can be loaded to the same origin.
                     $"base-uri {string.Join(" ", ownOrigins)}; " +    // Restricts the URLs that can be used in a document's <base> element.
                     $"form-action {string.Join(" ", ownOrigins)}; " + // Ensures form data is only submitted to your own server.
                     $"object-src 'none';";                            // Disallows legacy plugins like Flash.

        if (AppEnvironment.IsDevelopment() is false)
        {
            CspContent += "upgrade-insecure-requests; ";               // Tells browsers to treat all of this site's insecure URLs (those over HTTP) as though they have been replaced with secure URLs.
        }
    }
}
