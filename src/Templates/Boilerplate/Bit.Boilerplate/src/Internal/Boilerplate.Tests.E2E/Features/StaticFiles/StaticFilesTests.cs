using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.E2E.Features.StaticFiles;

/// <summary>
/// The Brotli files the publish produced are what a browser gets. The publish compresses every static web asset at the
/// highest Brotli level (the <c>.br</c> next to each file in the IIS site's wwwroot); Server.Web serves those bytes as
/// they are, and Cloudflare in front of it must not decompress them and re-encode on its own - it may, unless the
/// response says <c>no-transform</c>, and for a browser that offers zstd it does.
/// <para>
/// Every url here carries a <c>v</c>, because that is what the app asks for: bswup fetches each asset with the
/// manifest hash in one (See bit-bswup.sw.js, <c>searchParams.set("v", ...)</c>), and Server.Web's static file rule
/// keys on a <c>v</c> being present to answer <c>no-transform</c> (See Program.Middlewares.cs). The bare url, which
/// nothing asks for, is left out of this on purpose.
/// </para>
/// <para>
/// The IIS sites are on this machine (See DeployedApps.DeploymentOfApi and .github/actions/deploy-to-server), so the
/// published <c>.br</c> is read straight from the site folder and compared byte for byte with what the internet gets.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class StaticFilesTests
{
    /// <summary>What Chrome and Edge send today: zstd is offered, so a CDN free to transform will choose it.</summary>
    private const string browserAcceptEncoding = "gzip, deflate, br, zstd";

    /// <summary>The largest few carry nearly all of the download, and where a re-encode costs the most.</summary>
    private const int filesPerSite = 3;

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Sales, @"C:\inetpub\SalesModule", DisplayName = "Sales (Server.Web + Cloudflare)")]
    [DataRow(App.AdminPanel, @"C:\inetpub\AdminPanel", DisplayName = "AdminPanel (Server.Web + Cloudflare)")]
    [DataRow(App.Todo, @"C:\inetpub\Todo", DisplayName = "Todo (Server.Web + Cloudflare)")]
    public async Task FrameworkFiles_Should_ReachTheBrowserAsThePublishedBrotli(App app, string sitePath)
    {
        var framework = Path.Combine(sitePath, "wwwroot", "_framework");
        if (Directory.Exists(framework) is false)
            Assert.Inconclusive($"{framework} is not on this machine, so there is no published .br to compare with.");

        var published = new DirectoryInfo(framework).GetFiles("*.br").OrderByDescending(f => f.Length).Take(filesPerSite).ToArray();
        Assert.IsNotEmpty(published, $"{framework} has no .br files.");

        foreach (var br in published)
        {
            var publishedBytes = await File.ReadAllBytesAsync(br.FullName, TestContext.CancellationToken);
            var publishedHash = Convert.ToHexString(SHA256.HashData(publishedBytes));

            // Any v does: the rule keys on the parameter, not on its value (See the class summary).
            var url = new Uri(new Uri(DeployedApps.AddressOf(app)), $"_framework/{Path.GetFileNameWithoutExtension(br.Name)}?v={publishedHash}");

            using var response = await Send(url, browserAcceptEncoding);
            var served = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");
            Assert.AreEqual("br", string.Join(',', response.Content.Headers.ContentEncoding),
                $"{url} reached a browser offering '{browserAcceptEncoding}' as '{string.Join(',', response.Content.Headers.ContentEncoding)}' ({served.Length:N0} bytes) instead of the published Brotli ({br.Length:N0} bytes). Cache-Control: {response.Headers.CacheControl}");

            Assert.AreEqual(publishedHash, Convert.ToHexString(SHA256.HashData(served)),
                $"{url} is Brotli ({served.Length:N0} bytes), but not the published {br.Name} ({publishedBytes.Length:N0} bytes) - it was compressed again on the way.");
        }
    }

    /// <summary>
    /// A versioned asset (<c>?v=sha256-...</c>, See Bit.BlazorUI.Assets' Link / Script) already carries
    /// <c>no-transform</c> (Server.Web's static file rule), so Cloudflare leaves its Brotli alone even when zstd is offered.
    /// </summary>
    [TestMethod]
    public async Task AVersionedAsset_Should_StayTheServersBrotli_ForABrowserOfferingZstd()
    {
        // The page itself decompressed: only the asset's own bytes are compared on the wire.
        using var pageClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { Timeout = TimeSpan.FromMinutes(2) };
        var html = await pageClient.GetStringAsync(new Uri(new Uri(DeployedApps.Sales), "en-US/"), TestContext.CancellationToken);
        var asset = VersionedScript().Match(html);
        Assert.IsTrue(asset.Success, "The Sales home page links no versioned script.");

        using var response = await Send(new Uri(new Uri(DeployedApps.Sales), WebUtility.HtmlDecode(asset.Value)), browserAcceptEncoding);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Headers.CacheControl, $"{asset.Value} has no Cache-Control.");
        Assert.IsTrue(response.Headers.CacheControl.NoTransform, $"{asset.Value} has no no-transform: {response.Headers.CacheControl}");
        Assert.AreEqual("br", string.Join(',', response.Content.Headers.ContentEncoding), $"{asset.Value} was re-encoded.");
    }

    /// <summary>Azure Static Web Apps are not behind Cloudflare: they answer with Brotli themselves.</summary>
    [TestMethod]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    [DataRow(App.TodoAot, DisplayName = "TodoAot (Static Web App)")]
    public async Task FrameworkFiles_Should_BeBrotli_OnTheStaticWebApps(App app)
    {
        var url = new Uri(new Uri(DeployedApps.AddressOf(app)), "_framework/dotnet.native.wasm");

        using var response = await Send(url, browserAcceptEncoding);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");
        Assert.AreEqual("br", string.Join(',', response.Content.Headers.ContentEncoding), $"{url} came as '{string.Join(',', response.Content.Headers.ContentEncoding)}'.");
    }

    /// <summary>No automatic decompression: the bytes compared are the ones on the wire.</summary>
    private async Task<HttpResponseMessage> Send(Uri url, string acceptEncoding)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None }) { Timeout = TimeSpan.FromMinutes(3) };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    [GeneratedRegex(@"_content/[^""]+\.js\?v=sha256-[^""]+")]
    private static partial Regex VersionedScript();
}
