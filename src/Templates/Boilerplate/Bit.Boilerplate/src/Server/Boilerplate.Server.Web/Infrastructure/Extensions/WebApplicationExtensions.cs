//+:cnd:noEmit
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Boilerplate.Server.Web;
//#if(module == "Sales")
using Boilerplate.Shared.Features.Products;
//#endif

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Makes <c>/{culture}/...</c> the one url a Blazor page is served under: 302s <c>/about</c>,
        /// <c>/about?culture=fa-IR</c> and <c>/FA-ir/about</c> onto the canonical form. With the culture in the path
        /// the url alone identifies the language variant, which is what lets pre-rendered pages be cached on the CDN
        /// edge and in the browser (See <c>AppResponseCachePolicy</c>) while one purge by the bare path still clears
        /// every culture of a page. The redirect target is whatever <c>UseLocalization</c>'s provider chain resolved
        /// (query, route, cookie, Accept-Language) - the user's preference decides where a culture-less url lands,
        /// while a url that names a culture is served in that culture for everyone. The redirect itself is
        /// per-caller: 302 and never cacheable.
        /// </summary>
        public WebApplication UseCultureUrlRedirection()
        {
            if (CultureInfoManager.InvariantGlobalization)
                return app;

            app.Use(async (context, next) =>
            {
                var request = context.Request;

                if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) is false
                    || context.IsBlazorPageContext() is false
                    // The service worker's app-shell request must be served as requested: a service worker may not
                    // answer a navigation with a `redirected` response - and with `no-prerender` nothing is rendered.
                    || request.Query.ContainsKey("no-prerender"))
                {
                    await next(context);
                    return;
                }

                // What UseLocalization resolved for this request; always one of the supported cultures.
                var culture = CultureInfo.CurrentUICulture.Name;

                var path = request.Path.Value ?? "/";
                var segments = path.Split('/'); // The path always starts with '/', so segments[0] is empty.
                var firstSegment = segments.Length > 1 ? segments[1] : string.Empty;

                var hasCultureQueryParameter = request.Query.ContainsKey("culture");

                if (hasCultureQueryParameter is false && string.Equals(firstSegment, culture, StringComparison.Ordinal))
                {
                    await next(context); // Already canonical.
                    return;
                }

                // A miscased or query-overruled culture segment is replaced rather than prefixed twice.
                var barePath = CultureInfoManager.GetCultureInfo(firstSegment) is not null
                    ? $"/{string.Join('/', segments.Skip(2))}"
                    : path;

                var query = request.QueryString;
                if (hasCultureQueryParameter)
                {
                    var queryParameters = AppQueryStringCollection.Parse(query.Value ?? string.Empty);
                    queryParameters.Remove("culture");
                    query = queryParameters is { Count: > 0 } ? new QueryString($"?{queryParameters}") : QueryString.Empty;
                }

                // 302 and never cacheable: the target depends on the caller's cookie and Accept-Language, and a
                // remembered redirect would pin one user's language onto a url forever.
                context.Response.GetTypedHeaders().CacheControl = new() { NoStore = true, Private = true };
                context.Response.Redirect(UriHelper.BuildRelative(request.PathBase, $"/{culture}{barePath}", query));
            });

            return app;
        }

        // [mirror] precompressed static files - keep in sync with:
        // - src/LandingPage/Boilerplate.LandingPage.Server/Infrastructure/Extensions/WebApplicationExtensions.cs (UsePrecompressedStaticFiles)
        /// <summary>
        /// Serves the .br file the publish wrote next to a static file to a browser that accepts Brotli, _framework's
        /// included, instead of leaving UseResponseCompression to compress it again.
        /// </summary>
        public WebApplication UsePrecompressedStaticFiles()
        {
            var contentTypes = new FileExtensionContentTypeProvider();
            contentTypes.Mappings[".br"] = "application/octet-stream";

            app.UseWhen(context => (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                && context.GetEndpoint()?.RequestDelegate is null
                && contentTypes.TryGetContentType(context.Request.Path.Value!, out _)
                && app.Environment.WebRootFileProvider.GetFileInfo($"{context.Request.Path}.br").Exists, precompressedApp =>
            {
                precompressedApp.Use(async (context, next) =>
                {
                    context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);

                    if (context.Request.GetTypedHeaders().AcceptEncoding.Any(encoding => encoding.Value.Equals("br", StringComparison.OrdinalIgnoreCase) && encoding.Quality is not 0))
                    {
                        context.Request.Path = $"{context.Request.Path}.br";
                        context.Response.Headers.ContentEncoding = "br";
                        context.Response.GetTypedHeaders().CacheControl = new() { NoTransform = true };
                    }

                    await next.Invoke();
                });

                precompressedApp.UseStaticFiles(new StaticFileOptions
                {
                    ContentTypeProvider = contentTypes,
                    OnPrepareResponse = file =>
                    {
                        if (file.Context.Response.Headers.ContentEncoding == "br"
                            && contentTypes.TryGetContentType(Path.GetFileNameWithoutExtension(file.File.Name), out var contentType))
                        {
                            file.Context.Response.ContentType = contentType;
                        }
                    }
                });
            });

            return app;
        }

        /// <summary>
        /// Sends <c>X-Robots-Tag: noindex</c> with every page whose <see cref="PageUrls"/> url carries
        /// <see cref="NoIndexAttribute"/>. <c>AppPageData</c> renders the same as a meta tag, but with pre-rendering off
        /// that tag only exists once the page's scripts have run, and a header is read from the first response.
        /// </summary>
        public WebApplication UseNoIndexHeader()
        {
            app.Use(async (context, next) =>
            {
                if (context.IsBlazorPageContext() && PageUrls.IsNoIndex(context.Request.GetUri()))
                {
                    context.Response.Headers["X-Robots-Tag"] = "noindex";
                }

                await next(context);
            });

            return app;
        }

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
                var urls = SitemapPages;

                // Culture-prefixed urls only: the bare url always 302s (See UseCultureUrlRedirection), and a sitemap
                // entry that always redirects is one crawlers flag rather than index.
                urls = CultureInfoManager.InvariantGlobalization is false
                        ? [.. CultureInfoManager.SupportedCultures.SelectMany(sc => urls.Select(url => $"{sc.Culture.Name}{url}"))]
                        : urls;

                var baseUrl = context.Request.GetBaseUrl();

                var siteMap = @$"{siteMapHeader}
    {string.Join(Environment.NewLine, urls.Select(u => $"<url><loc>{new Uri(baseUrl, u)}</loc></url>"))}
</urlset>";

                context.Response.Headers.ContentType = "application/xml";

                await context.Response.WriteAsync(siteMap, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            app.MapGet("/robots.txt", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
            {
                var robots = "User-agent: *\nDisallow:\n";

                if (app.Environment.IsProduction())
                {
                    robots += $"\nSitemap: {new Uri(context.Request.GetBaseUrl(), "sitemap_index.xml")}\n";
                }

                context.Response.Headers.ContentType = "text/plain; charset=utf-8";

                await context.Response.WriteAsync(robots, context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            // https://llmstxt.org - a markdown file that helps LLMs discover the app's pages.
            // Unlike sitemap.xml (which has no description element), llms.txt is where the page
            // descriptions declared in PageUrls actually belong. It lists the pages sitemap.xml lists, the public ones a
            // reader can open, under the default culture's url, which answers without a redirect; its name and summary
            // come from the LlmsTxt section of appsettings.json.
            var llmsTxtSettings = app.Services.GetRequiredService<ServerWebSettings>().LlmsTxt;

            app.MapGet("/llms.txt", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
            {
                var baseUrl = context.Request.GetBaseUrl();

                var pages = string.Join('\n', PageUrls.GetPages()
                    .Where(page => SitemapPages.Contains(page.Url, StringComparer.OrdinalIgnoreCase))
                    .Select(page => $"- [{page.Url}]({new Uri(baseUrl, CultureInfoManager.InvariantGlobalization ? page.Url : $"{CultureInfoManager.DefaultCulture.Name}{page.Url}")}): {page.Description}"));

                var llms = new StringBuilder($"# {llmsTxtSettings.Name}\n\n");

                if (string.IsNullOrWhiteSpace(llmsTxtSettings.Summary) is false)
                {
                    llms.Append($"> {llmsTxtSettings.Summary}\n\n");
                }

                llms.Append($"## Pages\n\n{pages}\n");

                context.Response.Headers.ContentType = "text/plain; charset=utf-8";

                await context.Response.WriteAsync(llms.ToString(), context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            //#if(module == "Sales")
            app.MapGet("/products.xml", [AppResponseCache(SharedMaxAge = 60 * 5)] async (IProductViewController controller, HttpContext context) =>
            {
                var baseUrl = context.Request.GetBaseUrl();

                // A sitemap file may carry at most 50,000 urls, and every product url is emitted once per supported
                // culture below, so this is the most products one document can hold.
                // A catalogue larger than this needs the document split into /products-1.xml, /products-2.xml, ...
                // listed in sitemap_index.xml; until then the tail is not advertised, and the warning below says so.
                var maxProducts = 50_000 / (CultureInfoManager.InvariantGlobalization ? 1 : CultureInfoManager.SupportedCultures.Length);
                const int pageSize = 100; // ProductViewController.Get's EnableQuery(PageSize), and the largest $top EnableQueryFeatures(maxTopValue) accepts.

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

                // Culture-prefixed urls only, for the same reason as sitemap.xml above: the bare product url redirects.
                productsUrls = CultureInfoManager.InvariantGlobalization is false
                    ? [.. CultureInfoManager.SupportedCultures.SelectMany(sc => productsUrls.Select(url => $"{sc.Culture.Name}{url}"))]
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

        /// <summary>
        /// <c>/.well-known/security.txt</c> (https://www.rfc-editor.org/rfc/rfc9116): whom to tell about a vulnerability.
        /// Mapped only once the SecurityTxt section of appsettings.json names a contact.
        /// </summary>
        public WebApplication MapSecurityTxt()
        {
            var securityTxtSettings = app.Services.GetRequiredService<ServerWebSettings>().SecurityTxt;

            if (securityTxtSettings.Contact is not { Length: > 0 })
                return app;

            app.MapGet("/.well-known/security.txt", [AppResponseCache(SharedMaxAge = 3600 * 24)] async (context) =>
            {
                // RFC 9116 asks for an Expires less than a year ahead. Without a configured one, the file stays current as
                // long as the app runs; a configured one lapses unless someone checks the file again and moves it.
                var expires = securityTxtSettings.Expires ?? new DateTimeOffset(DateTime.UtcNow.Date.AddDays(180), TimeSpan.Zero);

                var securityTxt = new StringBuilder();

                foreach (var contact in securityTxtSettings.Contact)
                {
                    securityTxt.Append($"Contact: {contact}\n");
                }

                securityTxt.Append($"Expires: {expires.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}\n");

                if (string.IsNullOrWhiteSpace(securityTxtSettings.PreferredLanguages) is false)
                {
                    securityTxt.Append($"Preferred-Languages: {securityTxtSettings.PreferredLanguages}\n");
                }

                if (string.IsNullOrWhiteSpace(securityTxtSettings.Policy) is false)
                {
                    securityTxt.Append($"Policy: {securityTxtSettings.Policy}\n");
                }

                securityTxt.Append($"Canonical: {new Uri(context.Request.GetBaseUrl(), ".well-known/security.txt")}\n");

                context.Response.Headers.ContentType = "text/plain; charset=utf-8";

                await context.Response.WriteAsync(securityTxt.ToString(), context.RequestAborted);
            }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

            return app;
        }
    }

    /// <summary>
    /// The pages <c>sitemap.xml</c> and <c>llms.txt</c> list, by their culture-less url: every routable page without a
    /// route parameter whose component carries no <see cref="AuthorizeAttribute"/>, less the ones whose
    /// <see cref="PageUrls"/> constant keeps them out - a page search engines should not index (See
    /// <see cref="NoIndexAttribute"/>), and one that only exists to sign in or needs an account to show anything
    /// (See <see cref="NotInSitemapAttribute"/>).
    /// </summary>
    private static string[] SitemapPages => field ??= [.. AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Boilerplate.Client") is true)
        .SelectMany(asm => asm.ExportedTypes)
        .Where(att => att.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() is false)
        .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
        .Where(att => RouteRegex().IsMatch(att.Template) is false)
        .Select(att => att.Template)
        .Where(url => PageUrls.NotInSitemapUrls.Contains(url) is false)
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex RouteRegex();
}
