using QRCoder;
using Microsoft.AspNetCore.Http.Extensions;

namespace Boilerplate.LandingPage.Server.Features.Download;

public static class DownloadEndpoints
{
    public static void MapLandingDownloads(this WebApplication app)
    {
        foreach (var site in LandingSites.All)
        {
            app.MapGet($"{site.Path}/download", (HttpContext context) => Redirect(context, site)).ExcludeFromDescription();

            if (LandingCultures.InvariantGlobalization is false)
            {
                app.MapGet($"/{{culture}}{site.Path}/download", (HttpContext context) => Redirect(context, site)).ExcludeFromDescription();
            }

            app.MapGet($"{site.Path}/download.svg", (HttpContext context) =>
            {
                var target = new Uri(context.GetSiteUrl(), $"{site.Path.TrimStart('/')}/download?utm_source=qr&utm_medium=landing");

                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(target.ToString(), QRCodeGenerator.ECCLevel.M);
                var svg = new SvgQRCode(data).GetGraphic(8, "#111111", "#FFFFFF", drawQuietZones: true, sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);

                context.Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromDays(1) };
                return Results.Text(svg, "image/svg+xml");
            }).ExcludeFromDescription();
        }
    }

    private static IResult Redirect(HttpContext context, LandingSite site)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var platform = userAgent switch
        {
            _ when userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase) => LandingPlatform.iOS,
            _ when userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Android,
            _ when userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Windows,
            _ when userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) => LandingPlatform.macOS,
            _ => LandingPlatform.Web
        };

        var query = context.Request.Query;
        var link = site.Find(platform);

        string target;

        if (link is { Status: not LandingPlatformStatus.ComingSoon, Url: { } url })
        {
            target = platform switch
            {
                LandingPlatform.Android => AppendQuery(url, "referrer", string.Join('&', query.Where(q => q.Key.StartsWith("utm_", StringComparison.Ordinal)).Select(q => $"{q.Key}={q.Value}"))),
                LandingPlatform.iOS or LandingPlatform.macOS => AppendQuery(url, "ct", query["utm_campaign"].FirstOrDefault() ?? query["utm_source"].FirstOrDefault()),
                _ => url
            };
        }
        else
        {
            var (culture, _) = LandingCultures.Split(context.Request.Path.Value ?? "/");
            target = $"{LandingCultures.Localize(site.Path, culture)}{context.Request.QueryString}#download";
        }

        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.Vary = "User-Agent";

        return Results.Redirect(target);
    }

    private static string AppendQuery(string url, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return url;

        return $"{url}{(url.Contains('?') ? '&' : '?')}{key}={Uri.EscapeDataString(value)}";
    }
}
