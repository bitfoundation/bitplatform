using System.Collections.Concurrent;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The AI package lazy-loaded (BlazorWebAssemblyLazyLoad), as the Trimming page's "Lazy-loading the AI package" section
/// sets it up: Bit.BlazorUI.Tests.Trimming.LazyAi, a trimmed WebAssembly app with the lazy scripts whose chat page, the
/// AI assemblies and the library holding that page are downloaded when the chat opens - nothing of the AI packages
/// registered at startup. What it costs is recorded by <see cref="TrimmedSizeReportTests"/> (<see cref="MeasureAiPackage"/>).
/// </summary>
[TestClass]
public sealed class LazyAiTrimmingTests
{
    private const string Core = "Bit.BlazorUI";
    private const string AI = "Bit.BlazorUI.AI";
    private const string Font = "fonts/FabMDL2.4.66.bit.BlazorUI.AI.woff2";

    private static readonly string[] LazyAssemblies = ["Bit.BlazorUI.AI", "Bit.BlazorUI.AI.Core", "Bit.BlazorUI.Tests.Trimming.LazyAi.Chat"];

    public static PublishResult Published => Publisher.Publish(Publisher.LazyAi);

    [TestMethod]
    public void TheAiAssembliesShouldBeLoadedOnlyWhenTheChatOpens()
    {
        Published.AssertSucceeded();

        // Listed as the assemblies LazyAssemblyLoader loads, and not as the ones the app starts with.
        var lazy = Published.BootResources("lazyAssembly");
        var eager = Published.BootResources("assembly");
        foreach (var assembly in LazyAssemblies)
        {
            CollectionAssert.Contains(lazy, assembly + ".wasm", $"{assembly} is not a lazy assembly of the publish: {string.Join(", ", lazy)}");
            CollectionAssert.DoesNotContain(eager, assembly + ".wasm", $"{assembly} is loaded as the app starts.");
            Assert.IsNotNull(Published.FindFrameworkAssembly(assembly), $"The publish has no {assembly}.");
        }

        // The core is what the app starts with.
        CollectionAssert.Contains(eager, "Bit.BlazorUI.wasm");
    }

    [TestMethod]
    public void ALazyAiAssemblyShouldStillBringItsStylesScriptsAndFont()
    {
        Published.AssertSucceeded();

        // The trimming reads the lazy assemblies as it reads the others: the chat's stylesheets and scripts are kept.
        var styles = Published.KeptStyles(AI);
        foreach (var expected in new[] { "BitAiChat", "BitAiMessageList", "BitAiChatMessage", "BitAiPromptInput", "BitAiCopyButton" })
        {
            CollectionAssert.Contains(styles, expected, $"The AI stylesheet left out {expected}: {string.Join(", ", styles)}");
        }

        var modules = Published.PublishedModules(AI);
        foreach (var expected in new[] { "BitAiPromptInput", "BitAiMessageList", "BitAiCopyButton", "BitAiScrollStops" })
        {
            CollectionAssert.Contains(modules, expected, $"The lazy scripts left out {expected}: {string.Join(", ", modules)}");
        }

        // No bundle: the boot module and the package's JavaScript initializer, which Blazor imports as the app starts.
        Assert.IsFalse(Published.HasAsset(AI, "scripts/bit.blazorui.ai.js"), "The AI bundle was published in lazy mode.");
        Assert.IsTrue(Published.HasAsset(AI, "scripts/bit.blazorui.ai.boot.js"), "The AI boot module was not published.");
        Assert.IsTrue(Published.BootInitializers().Any(i => i.StartsWith("_content/Bit.BlazorUI.AI/scripts/Bit.BlazorUI.AI.", StringComparison.Ordinal)),
                      $"Blazor is not told to import the AI initializer: {string.Join(", ", Published.BootInitializers())}");

        // The glyph font, where the stylesheet points.
        Assert.IsTrue(Published.HasAsset(AI, Font), "The AI glyph font was not published.");
        StringAssert.Contains(Published.ReadAsset(AI, "styles/bit.blazorui.ai.css"), "../" + Font);
    }

    [TestMethod]
    [DynamicData(nameof(BrowserSession.Engines), typeof(BrowserSession))]
    public async Task TheChatShouldDownloadTheAiPackageWhenItOpensAndAnswer(string engine)
    {
        await using var session = await BrowserSession.OpenAsync(engine);
        Published.AssertSucceeded();

        var requested = new ConcurrentQueue<string>();
        session.Page.Request += (_, request) => requested.Enqueue(request.Url);

        await using var server = await AppServer.StartStaticAsync(Published.WebRoot);
        var page = session.Page;

        await page.GotoAsync(server.Url);
        await page.WaitForSelectorAsync("#home-ready .bit-tfl", new() { Timeout = 90_000 });

        // Started: none of the AI assemblies, no module of it but the one its boot module imports, no glyph font.
        Assert.IsFalse(requested.Any(r => r.Contains("/_framework/Bit.BlazorUI.AI", StringComparison.Ordinal)), "An AI assembly was downloaded before the chat opened.");
        Assert.IsFalse(requested.Any(r => r.Contains("/_content/Bit.BlazorUI.AI/scripts/modules/", StringComparison.Ordinal) && r.Contains("/BitAiScrollStops.js", StringComparison.Ordinal) is false),
                       $"An AI module was imported before the chat opened: {string.Join(", ", requested.Where(r => r.Contains("/scripts/modules/", StringComparison.Ordinal)))}");
        Assert.IsFalse(requested.Any(r => r.Contains(".bit.BlazorUI.AI.woff2", StringComparison.Ordinal)), "The AI glyph font was downloaded before anything drew a glyph of it.");

        await page.GetByRole(AriaRole.Link, new() { Name = "Open the chat" }).ClickAsync();
        await session.WaitForAsync("() => !!document.querySelector('#chat-ready .bit-aicht')", "The chat did not open");

        foreach (var assembly in new[] { "Bit.BlazorUI.AI", "Bit.BlazorUI.AI.Core" })
        {
            var file = "/_framework/" + Path.GetFileName(Published.FindFrameworkAssembly(assembly));
            Assert.IsTrue(requested.Any(r => r.Contains(file, StringComparison.Ordinal)), $"{assembly} was not downloaded when the chat opened.");
        }

        await session.AssertEveryComponentIsStyledAsync(Published, Core, AI);

        // A prompt sent through the composer's script is answered, and the answer's copy button, drawn with the AI
        // package's glyph font, reaches its script.
        await session.AssertAiChatAnswersAsync();
        Assert.IsTrue(requested.Any(r => r.Contains("/_content/Bit.BlazorUI.AI/scripts/modules/BitAiPromptInput.js", StringComparison.Ordinal)), "The composer's module was not imported.");
        await session.AssertAiCopyButtonWorksAsync();

        session.AssertNoErrors($"The lazy-loaded AI app in {engine}");
    }

    /// <summary>
    /// What the AI package costs the trimmed app, part by part: each file's bytes and those of the Brotli copy the publish
    /// wrote of it (none for the font, already compressed), and whether the app downloads it as it starts or once the chat
    /// opens - the scripts of that part on first use.
    /// </summary>
    public static List<AiPackagePart> MeasureAiPackage(PublishResult publish)
    {
        var web = publish.WebRoot;

        AiPackagePart Part(string name, string when, IEnumerable<string> files)
        {
            var paths = files.ToList();
            Assert.AreNotEqual(0, paths.Count, $"The publish has nothing of the AI package's {name}.");

            return new(name, when, [.. paths.Select(p => Path.GetRelativePath(web, p).Replace('\\', '/'))],
                       paths.Sum(p => new FileInfo(p).Length),
                       paths.Sum(p => File.Exists(p + ".br") ? new FileInfo(p + ".br").Length : new FileInfo(p).Length));
        }

        string Asset(string path) => publish.GetAssetPath(AI, path);

        var boot = new[] { Asset("scripts/bit.blazorui.ai.boot.js") }
            .Concat(Directory.EnumerateFiles(Asset("scripts"), "Bit.BlazorUI.AI.*lib.module.js"))
            .Concat(PublishResult.GetPieces(AI).ScriptModules.Where(m => m.Boot).Select(m => Asset($"scripts/modules/{m.Name}.js")).Where(File.Exists));
        var onDemand = publish.PublishedModules(AI)
            .Where(m => PublishResult.GetPieces(AI).ScriptModules.Single(s => s.Name == m).Boot is false)
            .Select(m => Asset($"scripts/modules/{m}.js"));

        return
        [
            Part("stylesheet", "startup", [Asset("styles/bit.blazorui.ai.css")]),
            Part("initializer and boot scripts", "startup", boot),
            Part("Bit.BlazorUI.AI assembly", "chat", [publish.FindFrameworkAssembly("Bit.BlazorUI.AI")!]),
            Part("Bit.BlazorUI.AI.Core assembly", "chat", [publish.FindFrameworkAssembly("Bit.BlazorUI.AI.Core")!]),
            Part("script modules", "chat", onDemand),
            Part("glyph font", "chat", [Asset(Font)]),
        ];
    }

    public sealed record AiPackagePart(string Part, string When, List<string> Files, long Bytes, long BrotliBytes);
}
