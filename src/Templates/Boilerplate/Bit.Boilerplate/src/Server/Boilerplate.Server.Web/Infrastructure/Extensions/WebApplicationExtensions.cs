//+:cnd:noEmit
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
//#if(module == "Sales")
using Boilerplate.Shared.Features.Products;
//#endif

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public WebApplication UseSiteMap()
        {
            const string siteMapHeader = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">";

            app.MapGet("/sitemap_index.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
            {
                // The conditional may not live inside the xml literal below. A template directive is a line based text
                // directive, so the engine strips it from generated projects wherever it sits - but in this repo's own
                // tree, where no engine runs, those two lines are ordinary string content and get served as character
                // data inside <sitemapindex>, which is not valid against the sitemaps.org schema.
                List<string> sitemaps = ["sitemap.xml"];
                //#if(module == "Sales")
                sitemaps.Add("products.xml");
                //#endif

                var baseUrl = context.Request.GetBaseUrl();

                var sitemapIndex = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
    {string.Join(Environment.NewLine, sitemaps.Select(sitemap => $"<sitemap><loc>{new Uri(baseUrl, sitemap)}</loc></sitemap>"))}
</sitemapindex>";

                context.Response.Headers.ContentType = "application/xml";

                await context.Response.WriteAsync(sitemapIndex, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            app.MapGet("/sitemap.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
            {
                var urls = AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Boilerplate.Client") is true)
                     .SelectMany(asm => asm.ExportedTypes)
                     .Where(att => att.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() is false)
                     .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
                     .Where(att => RouteRegex().IsMatch(att.Template) is false)
                     .Select(att => att.Template)
                     .Except([PageUrls.NotFound, PageUrls.NotAuthorized])
                     .ToArray();

                urls = CultureInfoManager.InvariantGlobalization is false
                        ? urls.Union(CultureInfoManager.SupportedCultures.SelectMany(sc => urls.Select(url => $"{sc.Culture.Name}{url}"))).ToArray()
                        : urls;

                var baseUrl = context.Request.GetBaseUrl();

                var siteMap = @$"{siteMapHeader}
    {string.Join(Environment.NewLine, urls.Select(u => $"<url><loc>{new Uri(baseUrl, u)}</loc></url>"))}
</urlset>";

                context.Response.Headers.ContentType = "application/xml";

                await context.Response.WriteAsync(siteMap, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            // https://llmstxt.org - a markdown file that helps LLMs discover the app's pages.
            // Unlike sitemap.xml (which has no description element), llms.txt is where the page
            // descriptions declared in PageUrls actually belong.
            app.MapGet("/llms.txt", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
            {
                var baseUrl = context.Request.GetBaseUrl();

                var pages = string.Join(Environment.NewLine, PageUrls.GetPages()
                    .Select(page => $"- [{page.Url}]({new Uri(baseUrl, page.Url)}): {page.Description}"));

                var llms = @$"# Boilerplate

> Boilerplate is a cross-platform application available on Android, iOS, Windows, macOS and as a Web (PWA) app.

## Pages

{pages}
";

                context.Response.Headers.ContentType = "text/plain; charset=utf-8";

                await context.Response.WriteAsync(llms, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            //#if(module == "Sales")
            app.MapGet("/products.xml", [AppResponseCache(SharedMaxAge = 60 * 5)] async (IProductViewController controller, HttpContext context) =>
            {
                var baseUrl = context.Request.GetBaseUrl();

                // A sitemap file may carry at most 50,000 urls, and every product url is emitted once per supported
                // culture below, so this is the most products one document can hold. It is also what bounds the read:
                // ProductViewController.Get sets no PageSize, and EnableQueryFeatures(maxTopValue) only rejects an
                // oversized explicit $top - it cannot cap a request that sends none - so without a $top of our own an
                // anonymous request reads the whole Products table and materializes a url per row per culture.
                // A catalogue larger than this needs the document split into /products-1.xml, /products-2.xml, ...
                // listed in sitemap_index.xml; until then the tail is not advertised, and the warning below says so.
                var maxProducts = 50_000 / (CultureInfoManager.InvariantGlobalization ? 1 : CultureInfoManager.SupportedCultures.Length + 1);
                const int pageSize = 100; // The largest $top Program.Services.cs's EnableQueryFeatures(maxTopValue) accepts.

                List<string> pagedProductsUrls = [];

                while (pagedProductsUrls.Count <= maxProducts)
                {
                    var top = Math.Min(pageSize, maxProducts + 1 - pagedProductsUrls.Count);

                    var page = await controller.WithQuery(new ODataQuery()
                    {
                        Select = nameof(ProductDto.ShortId), // All ProductDto.PageUrl needs.
                        OrderBy = nameof(ProductDto.ShortId), // $skip without an order returns an arbitrary subset.
                        Top = top,
                        Skip = pagedProductsUrls.Count
                    }).Get(context.RequestAborted);

                    pagedProductsUrls.AddRange(page.Select(p => p.PageUrl));

                    if (page.Count < top)
                        break; // Fewer rows than were asked for, so that was the end of the catalogue.
                }

                if (pagedProductsUrls.Count > maxProducts)
                {
                    app.Logger.LogWarning("products.xml holds the first {MaxProducts} products only. A sitemap may carry 50,000 urls and each product is listed once per culture, so the rest of the catalogue is not advertised until this document is split into several.", maxProducts);
                }

                var productsUrls = pagedProductsUrls.Take(maxProducts).ToArray();

                productsUrls = CultureInfoManager.InvariantGlobalization is false
                    ? productsUrls.Union(CultureInfoManager.SupportedCultures.SelectMany(sc => productsUrls.Select(url => $"{sc.Culture.Name}{url}"))).ToArray()
                    : productsUrls;

                var productsMap = @$"{siteMapHeader}
    {string.Join(Environment.NewLine, productsUrls.Select(productUrl => $"<url><loc>{new Uri(baseUrl, productUrl)}</loc></url>"))}
</urlset>";

                context.Response.Headers.ContentType = "application/xml";

                await context.Response.WriteAsync(productsMap, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");
            //#endif

            return app;
        }
    }

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex RouteRegex();
}
