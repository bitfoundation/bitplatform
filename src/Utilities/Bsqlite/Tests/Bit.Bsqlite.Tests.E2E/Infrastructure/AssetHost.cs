using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// Serves the libraries' static assets the way a Blazor application does - each package's wwwroot under
/// <c>_content/&lt;package&gt;/</c> - next to the test pages, so the tests exercise the same URLs and MIME
/// types production uses. Kestrel in this process, on a free loopback port, for the whole run.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately sends no COOP/COEP headers: the default tier must work without cross-origin isolation, and
/// a test server that granted it would hide the day the library accidentally depended on it.
/// </para>
/// <para>
/// The same layout is repeated under <c>/nested/</c>, which is how an application published under a
/// sub-path (<c>&lt;base href="/nested/"&gt;</c>) serves its assets. TestPages/nested/index.html loads the
/// library from there, so the tests can prove that no module is reached by a root-relative URL.
/// </para>
/// </remarks>
public sealed class AssetHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private AssetHost(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>The URL the assets are served from, without a trailing slash.</summary>
    public string BaseUrl { get; }

    public static async Task<AssetHost> StartAsync()
    {
        foreach (var package in RepoLayout.Packages)
        {
            var root = RepoLayout.WebRoot(package);
            if (Directory.Exists(root) is false || Directory.EnumerateFiles(root, "*.js").Any() is false)
                throw new DirectoryNotFoundException(
                    $"{root} holds no compiled modules. Build the libraries first (building this test project does), " +
                    "after a restore that vendored the wa-sqlite engine.");
        }

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(baseUrl);
        builder.Logging.ClearProviders();

        var app = builder.Build();

        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".mjs"] = "text/javascript";
        contentTypes.Mappings[".wasm"] = "application/wasm";

        foreach (var basePath in new[] { "", "/nested" })
        {
            foreach (var package in RepoLayout.Packages)
            {
                Serve(app, $"{basePath}/_content/{package}", RepoLayout.WebRoot(package), contentTypes);
            }
        }

        Serve(app, "", RepoLayout.TestPages(), contentTypes);

        await app.StartAsync();
        return new AssetHost(app, baseUrl);
    }

    private static void Serve(WebApplication app, string requestPath, string directory, IContentTypeProvider contentTypes)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = requestPath,
            FileProvider = new PhysicalFileProvider(directory),
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = true,
            // Every test reloads modules a rebuild may just have replaced; a cached copy would pair new
            // JavaScript with old.
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-store",
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
