using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.LandingPage.Server.Features.Seo;

public static class SeoEndpoints
{
    private static readonly XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace xhtmlNamespace = "http://www.w3.org/1999/xhtml";

    public static string[] Pages => field ??= [.. typeof(Client.Program).Assembly.ExportedTypes
        .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
        .Select(route => route.Template)
        .Where(template => template.Contains('{') is false && template is not LandingRoutes.NotFound)
        .Distinct()
        .Order(StringComparer.Ordinal)];

    public static void MapLandingSeo(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<LandingServerSettings>();

        if (settings.Site.AllowIndexing is false)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
                await next.Invoke();
            });
        }

        app.MapGet("/robots.txt", (HttpContext context) =>
        {
            var siteUrl = context.GetSiteUrl();

            var robots = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Allow: /")
                .AppendLine("Disallow: /api/")
                .AppendLine()
                .AppendLine($"Sitemap: {new Uri(siteUrl, "sitemap.xml")}")
                .ToString();

            return Results.Text(robots, "text/plain; charset=utf-8");
        }).ExcludeFromDescription();

        app.MapGet("/sitemap.xml", (HttpContext context) =>
        {
            var siteUrl = context.GetSiteUrl();

            var urls = Pages.SelectMany(page => LandingCultures.Supported.Select(s => (page, culture: s.Culture)))
                .Select(entry => new XElement(sitemapNamespace + "url",
                    new XElement(sitemapNamespace + "loc", Absolute(siteUrl, entry.page, entry.culture)),
                    LandingCultures.InvariantGlobalization
                        ? null
                        : LandingCultures.Supported.Select(s => new XElement(xhtmlNamespace + "link",
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", s.Culture.Name),
                            new XAttribute("href", Absolute(siteUrl, entry.page, s.Culture))))));

            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(sitemapNamespace + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtmlNamespace),
                urls));

            return Results.Text($"{document.Declaration}{document.ToString(SaveOptions.DisableFormatting)}", "application/xml; charset=utf-8");
        }).ExcludeFromDescription();

        app.MapGet("/llms.txt", (HttpContext context) =>
        {
            var siteUrl = context.GetSiteUrl();

            var llms = new StringBuilder()
                .AppendLine("# Boilerplate")
                .AppendLine()
                .AppendLine("> Sample landing pages, one per kind of product, built with bit Boilerplate.")
                .AppendLine();

            foreach (var site in LandingSites.All)
            {
                llms.AppendLine($"## {site.Name}").AppendLine();

                foreach (var page in Pages.Where(p => p == site.Path || p.StartsWith($"{site.Path}/", StringComparison.Ordinal)))
                {
                    llms.AppendLine($"- [{page}]({Absolute(siteUrl, page, LandingCultures.Default)})");
                }

                llms.AppendLine();
            }

            return Results.Text(llms.ToString(), "text/plain; charset=utf-8");
        }).ExcludeFromDescription();
    }

    public static string Absolute(Uri siteUrl, string page, CultureInfo culture)
    {
        return new Uri(siteUrl, LandingCultures.Localize(page, culture).TrimStart('/')).ToString();
    }
}
