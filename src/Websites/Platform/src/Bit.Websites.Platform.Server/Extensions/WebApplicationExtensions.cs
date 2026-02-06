namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        // NOTE: These headers represent a strong security baseline.
        // Depending on your application's requirements, you might need to relax or tighten these settings further.

        // 1. Strict-Transport-Security (HSTS)
        // Enforces HTTPS connections.
        // TIP: For "HSTS Preload", it's easier to configure it on Cloudflare CDN
        // or your web server, rather than hardcoding the preload directive here.
        app.UseHsts();

        // 2. X-Content-Type-Options
        // Prevents browsers from sniffing MIME types (stops executing text/plain as scripts).
        app.UseXContentTypeOptions();

        // 3. X-XSS-Protection
        // Legacy header. Enables the browser's built-in XSS filter in block mode.
        app.UseXXssProtection(options => options.EnabledWithBlockMode());

        // 4. X-Frame-Options (XFO)
        // Prevents Clickjacking by ensuring the site can only be framed by itself (SameOrigin).
        app.UseXfo(options => options.SameOrigin());

        // 5. Referrer-Policy
        // Protects user privacy by only sending the origin (domain) when navigating to external sites.
        app.UseReferrerPolicy(opts => opts.StrictOriginWhenCrossOrigin());

        app.Use(async (context, next) =>
        {
            // 6. Permissions-Policy
            // "Disables" sensitive hardware/API access to reduce the attack surface.
            // Example: If building an E-Commerce or Delivery app, remove 'payment' or 'geolocation' from this list.
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=(), payment=(), usb=(), display-capture=()");

            // 7. Cross-Origin-Resource-Policy (CORP)
            // Set to 'cross-origin' to explicitly allow resources (images, fonts, etc.) to be loaded by 
            // clients on different origins/domains and Blazor Hybrid (WebView).
            // NOTE: Using 'same-site' or 'same-origin' would block rendering in these multi-origin scenarios,
            // but they also help prevent hotlinking and bandwidth theft from untrusted third-party sites.
            // By choosing 'cross-origin', you allow *any* external site to embed your static assets, which can
            // increase bandwidth costs and enable unauthorized re-use of your images/assets.
            // Consider compensating controls such as CDN-level hotlink protection, WAF rules, rate limiting,
            // and/or caching policies to mitigate potential abuse while still supporting hybrid/multi-origin clients.
            context.Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");

            // 8. Content-Security-Policy (CSP) - Mini Version
            // 'object-src none': Blocks legacy plugins like Flash.
            // 'frame-ancestors self': Modern replacement for X-Frame-Options.
            // 'form-action self': Restricts forms to only submit to your own domain (prevents form hijacking).
            context.Response.Headers.Append("Content-Security-Policy", "object-src 'none'; frame-ancestors 'self'; form-action 'self';");

            await next();
        });

        return app;
    }
}
