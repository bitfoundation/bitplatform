using System.Text.RegularExpressions;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Playwright;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The trimming a Blazor WebAssembly app gets by default: Bit.BlazorUI.Tests.Consumer, published trimmed, its bundles put
/// back together off what ILLink left in the trimmed library assemblies - and, run in a browser, working.
/// </summary>
[TestClass]
public sealed partial class WebAssemblyTrimmingTests
{
    private const string Core = "Bit.BlazorUI";
    private const string Extras = "Bit.BlazorUI.Extras";

    private static PublishResult Published => Publisher.Publish(Publisher.Consumer);

    [TestMethod]
    public void AWebAssemblyAppShouldBeTrimmedByDefault()
    {
        Published.AssertSucceeded();

        // The app renders some two dozen components; the full bundles hold every one of them.
        var styles = Published.KeptStyles(Core);
        Assert.IsTrue(styles.Count is > 10 and < 60, $"The trimmed core stylesheet keeps {styles.Count} components: {string.Join(", ", styles)}");
        foreach (var expected in new[] { "BitButton", "BitTextField", "BitDropdown", "BitDatePicker", "BitNav", "BitModal" }) CollectionAssert.Contains(styles, expected);
        foreach (var unexpected in new[] { "BitCard", "BitCarousel", "BitSplitter", "BitScrollablePane" }) CollectionAssert.DoesNotContain(styles, unexpected);

        var scripts = Published.KeptScripts(Core);
        foreach (var expected in new[] { "general", "Utils.Keys", "Callouts", "BitTextField", "BitNav" }) CollectionAssert.Contains(scripts, expected);
        foreach (var unexpected in new[] { "BitScrollablePane", "BitSplitter", "BitPullToRefresh" }) CollectionAssert.DoesNotContain(scripts, unexpected);

        CollectionAssert.Contains(Published.KeptStyles(Extras), "BitDataGrid");
        CollectionAssert.DoesNotContain(Published.KeptStyles(Extras), "BitPdfViewer");
        CollectionAssert.DoesNotContain(Published.KeptScripts(Extras), "BitPdfViewer");

        foreach (var package in new[] { Core, Extras })
        {
            Published.AssertScriptsAreThePiecesInOrder(package);
            Published.AssertStylesAreThePiecesInOrder(package);
        }
    }

    [TestMethod]
    public async Task ATrimmedAppShouldRunWithEveryComponentStyledAndNoScriptMissing()
    {
        if (Environment.GetEnvironmentVariable("RUN_BROWSER_TESTS") != "1") Assert.Inconclusive("Set RUN_BROWSER_TESTS=1 to run the trimmed app in a browser.");

        Published.AssertSucceeded();

        await using var server = await StaticServer.StartAsync(Published.WebRoot);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        var errors = new List<string>();
        page.Console += (_, message) => { if (message.Type == "error") errors.Add($"console: {message.Text}"); };
        page.PageError += (_, error) => errors.Add($"page: {error}");
        page.Response += (_, response) => { if (response.Status >= 400) errors.Add($"{response.Status}: {response.Url}"); };

        await page.GotoAsync(server.Url);
        await page.WaitForSelectorAsync(".bit-btn", new() { Timeout = 60_000 });

        // What was trimmed away is gone from the page, and what the app uses is there.
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Callouts.toggle === 'function' && typeof BitBlazorUI.Utils.claimEscape === 'function'"));
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.ScrollablePane === 'undefined' && typeof BitBlazorUI.Splitter === 'undefined'"));

        // Every component class on the page has the stylesheet that styles it: the first segment of each bit- class
        // names a component's stylesheet (bit-btn, BitButton's), and that stylesheet is in the published bundle.
        var classes = await page.EvaluateAsync<string[]>("() => [...new Set([...document.querySelectorAll('[class]')].flatMap(e => [...e.classList]).filter(c => c.startsWith('bit-')))]");
        var prefixes = ChunkPrefixes();
        var kept = Published.KeptStyles(Core).Concat(Published.KeptStyles(Extras)).ToHashSet();
        var unstyled = classes.Select(c => ClassPrefix().Match(c))
                              .Where(m => m.Success && prefixes.ContainsKey(m.Groups["prefix"].Value))
                              .Select(m => (Class: m.Value, Chunk: prefixes[m.Groups["prefix"].Value]))
                              .Where(x => kept.Contains(x.Chunk) is false)
                              .Select(x => $"{x.Class} ({x.Chunk})")
                              .Distinct()
                              .ToList();
        Assert.AreEqual(0, unstyled.Count, $"The page renders classes whose stylesheet the trimmed bundle left out: {string.Join(", ", unstyled)}");

        // A callout opens - the script with the most moving parts of all - and closes on Escape.
        await page.Locator(".bit-drp").First.ClickAsync();
        await page.WaitForFunctionAsync("() => BitBlazorUI.Callouts.current.calloutId !== ''", null, new() { Timeout = 10_000 });
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForFunctionAsync("() => BitBlazorUI.Callouts.current.calloutId === ''", null, new() { Timeout = 10_000 });

        Assert.AreEqual(0, errors.Count, $"The trimmed app reported errors: {string.Join(" | ", errors)}");
    }

    // The class prefix each component stylesheet of the core and the Extras names its classes with (btn for BitButton):
    // the one most of its class selectors start with.
    private static Dictionary<string, string> ChunkPrefixes()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pieces in new[] { PublishResult.GetPieces(Core), PublishResult.GetPieces(Extras) })
        {
            foreach (var chunk in pieces.StyleChunks)
            {
                var prefix = ClassPrefix().Matches(File.ReadAllText(pieces.Resolve(chunk.File)))
                                          .GroupBy(m => m.Groups["prefix"].Value)
                                          .OrderByDescending(g => g.Count())
                                          .Select(g => g.Key)
                                          .FirstOrDefault();
                if (prefix is not null) result.TryAdd(prefix, chunk.Name);
            }
        }
        return result;
    }

    [GeneratedRegex(@"(?<![\w-])bit-(?<prefix>[a-z0-9]+)(?=[-\s.:,{)\]>]|$)")]
    private static partial Regex ClassPrefix();

    /// <summary>Serves a published WebAssembly app's wwwroot the way a static host would.</summary>
    private sealed class StaticServer(WebApplication app, string url) : IAsyncDisposable
    {
        public string Url { get; } = url;

        public static async Task<StaticServer> StartAsync(string webRoot)
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = webRoot, ContentRootPath = webRoot });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();

            var types = new FileExtensionContentTypeProvider();
            types.Mappings[".wasm"] = "application/wasm";
            types.Mappings[".dat"] = "application/octet-stream";
            types.Mappings[".blat"] = "application/octet-stream";
            types.Mappings[".webcil"] = "application/octet-stream";

            var files = new PhysicalFileProvider(webRoot);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = types, ServeUnknownFileTypes = true });
            app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });

            await app.StartAsync();
            return new StaticServer(app, app.Urls.First());
        }

        public async ValueTask DisposeAsync() => await app.DisposeAsync();
    }
}
