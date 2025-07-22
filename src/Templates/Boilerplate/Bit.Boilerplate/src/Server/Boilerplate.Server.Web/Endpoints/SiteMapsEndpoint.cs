//+:cnd:noEmit
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Boilerplate.Shared;
using Boilerplate.Shared.Attributes;
using Boilerplate.Client.Core.Services;
//#if(module == "Sales")
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;
//#endif

public static partial class SiteMapEndpoint
{
    public static WebApplication UseSiteMap(this WebApplication app)
    {
        const string siteMapHeader = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">";

        app.MapGet("/sitemap_index.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
        {
            const string SITEMAP_INDEX_FORMAT = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
   <sitemap>
      <loc>{0}sitemap.xml</loc>
   </sitemap>
//#if(module == 'Sales')
   <sitemap>
      <loc>{0}products.xml</loc>
   </sitemap>
//#endif
</sitemapindex>";

            var baseUrl = context.Request.GetBaseUrl();

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(string.Format(SITEMAP_INDEX_FORMAT, baseUrl), context.RequestAborted);
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

        //#if(module == "Sales")
        app.MapGet("/products.xml", [AppResponseCache(SharedMaxAge = 60 * 5)] async (IProductViewController controller, HttpContext context) =>
        {
            var baseUrl = context.Request.GetBaseUrl();
            var products = await controller.WithQuery(new ODataQuery() { Select = $"{nameof(ProductDto.ShortId)},{nameof(ProductDto.Name)}" }).Get(context.RequestAborted);
            var productsUrls = products.Select(p => p.PageUrl).ToArray();

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

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex RouteRegex();
}
