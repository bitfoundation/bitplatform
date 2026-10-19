using Bit.BlazorUI.Build;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The lazy scripts (BitBlazorUILazyScripts): a publish carrying one module per script instead of the bundles, the boot
/// modules the packages' JavaScript initializers import as the app starts, and - trimmed - only the modules the app can
/// reach, with the boot modules written again for what it keeps. And the other way round: a bundle-mode publish carries
/// none of it, so no app ever has Blazor import the boot scripts next to the bundles.
/// </summary>
[TestClass]
public sealed class LazyScriptsTrimmingTests
{
    private const string Core = "Bit.BlazorUI";
    private const string Extras = "Bit.BlazorUI.Extras";
    private const string Legacy = "Bit.BlazorUI.Legacy";

    private static PublishResult LazyServer => Publisher.Publish(Publisher.Fixture, "BitBlazorUILazyScripts=true", "BitBlazorUITrimAssets=true", "BitBlazorUITrimIcons=true");

    private static PublishResult LazyWebAssembly => Publisher.Publish(Publisher.Consumer, "BitBlazorUILazyScripts=true");

    [TestMethod]
    public void ABundleModeAppShouldPublishNoneOfTheLazyScripts()
    {
        var publish = Publisher.Publish(Publisher.Fixture);
        publish.AssertSucceeded();

        foreach (var package in new[] { Core, Extras, Legacy })
        {
            Assert.AreEqual(0, publish.PublishedModules(package).Count, $"{package} published the modules of the lazy scripts in bundle mode.");
        }

        Assert.IsFalse(publish.HasAsset(Core, "scripts/bit.blazorui.boot.js"), "The core boot module was published in bundle mode.");
        Assert.IsFalse(publish.HasAsset(Core, "scripts/Bit.BlazorUI.lib.module.js"), "The core initializer was published in bundle mode.");

        // And Blazor is not told to import one, which would load the boot scripts beside the bundle.
        Assert.IsFalse(publish.ListedInitializers().Any(i => i.Contains("Bit.BlazorUI", StringComparison.Ordinal)),
                       $"A bundle-mode app lists the initializers of the lazy scripts: {string.Join(", ", publish.ListedInitializers())}");
    }

    [TestMethod]
    public void ALazyAppShouldPublishEveryModuleAndNoBundleWhereNothingIsTrimmed()
    {
        // A server app trims nothing unless it asks: every module, the full boot modules, no bundle.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUILazyScripts=true");
        publish.AssertSucceeded();

        foreach (var package in new[] { Core, Extras, Legacy })
        {
            var pieces = PublishResult.GetPieces(package);
            CollectionAssert.AreEquivalent(pieces.ScriptModules.Select(m => m.Name).ToList(), publish.PublishedModules(package), $"{package} did not publish every module.");
            Assert.IsFalse(publish.HasAsset(package, pieces.ScriptBundle!), $"{package} published its bundle in lazy mode.");

            if (pieces.ScriptBoot is not null)
            {
                Assert.AreEqual(AssetComposer.BootModuleText(pieces.ScriptBoot, pieces.ScriptModules.Where(m => m.Boot)), publish.ReadAsset(package, pieces.ScriptBoot));
            }
        }

        // The stylesheets are the same in both modes.
        Assert.IsTrue(publish.HasAsset(Core, "styles/bit.blazorui.css"));

        // Blazor imports the initializers of the packages that boot a script; Legacy boots none and has none.
        var initializers = publish.ListedInitializers();
        Assert.IsTrue(initializers.Any(i => i.StartsWith("_content/Bit.BlazorUI/scripts/Bit.BlazorUI.", StringComparison.Ordinal)), string.Join(", ", initializers));
        Assert.IsTrue(initializers.Any(i => i.StartsWith("_content/Bit.BlazorUI.Extras/scripts/Bit.BlazorUI.Extras.", StringComparison.Ordinal)), string.Join(", ", initializers));
        Assert.IsFalse(initializers.Any(i => i.Contains("Legacy", StringComparison.Ordinal)), string.Join(", ", initializers));
    }

    [TestMethod]
    public void ATrimmedLazyAppShouldPublishTheModulesItCanReach()
    {
        LazyServer.AssertSucceeded();

        // The very scripts the bundle would have kept, each as its module.
        var modules = LazyServer.PublishedModules(Core);
        foreach (var expected in new[] { "general", "Utils.Keys", "BitTextField", "Utils.Escape", "Callouts", "Callouts.Registry", "Utils.Tooltips", "Utils.Focus", "PageVisibility" })
        {
            CollectionAssert.Contains(modules, expected, $"The trimmed lazy app has no module {expected}.");
        }
        foreach (var unexpected in new[] { "BitScrollablePane", "BitSwiper", "BitSplitter", "BitFileUpload", "SnackBars", "BitTheme", "BitPivot", "Stickies", "Events" })
        {
            CollectionAssert.DoesNotContain(modules, unexpected, $"The trimmed lazy app publishes {unexpected}, which nothing it runs imports.");
        }
        Assert.AreEqual(0, LazyServer.PublishedModules(Legacy).Count);

        // The boot module imports the scripts the app starts with that it keeps, in the bundle's order - not the theme's,
        // which nothing of the app needs.
        var pieces = PublishResult.GetPieces(Core);
        var booted = pieces.ScriptModules.Where(m => m.Boot && modules.Contains(m.Name)).ToList();
        CollectionAssert.AreEquivalent(new[] { "general", "Utils.Keys", "Utils.Escape" }, booted.Select(m => m.Name).ToList());
        Assert.AreEqual(AssetComposer.BootModuleText(pieces.ScriptBoot!, booted), LazyServer.ReadAsset(Core, pieces.ScriptBoot!));

        // Every module that is published imports only modules that are.
        foreach (var name in modules)
        {
            foreach (var dependency in pieces.ScriptModules.Single(m => m.Name == name).Dependencies)
            {
                CollectionAssert.Contains(modules, dependency, $"{name} imports {dependency}, which the publish left out.");
            }
        }

        Assert.IsFalse(LazyServer.HasAsset(Core, "scripts/bit.blazorui.js"));
        StringAssert.Contains(LazyServer.Output, "Bit.BlazorUI: Bit.BlazorUI lazy script modules -> ");
    }

    [TestMethod]
    public void TheModulesShouldAllBePublishedWhereTheCsprojAsksForThem()
    {
        // BitBlazorUIIncludeScriptModules written out: both shapes, every module, the bundle trimmed as usual.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUIIncludeScriptModules=true");
        publish.AssertSucceeded();

        CollectionAssert.AreEquivalent(PublishResult.GetPieces(Core).ScriptModules.Select(m => m.Name).ToList(), publish.PublishedModules(Core));
        CollectionAssert.DoesNotContain(publish.KeptScripts(Core), "BitSwiper");
    }

    [TestMethod]
    public void ATrimmedLazyWebAssemblyAppShouldPublishTheModulesILLinkLeftItAndNoBundle()
    {
        LazyWebAssembly.AssertSucceeded();

        var modules = LazyWebAssembly.PublishedModules(Core);
        foreach (var expected in new[] { "general", "Utils.Keys", "Callouts", "BitTextField", "BitNav", "BitTheme" }) CollectionAssert.Contains(modules, expected);
        foreach (var unexpected in new[] { "BitScrollablePane", "BitSplitter", "BitPullToRefresh" }) CollectionAssert.DoesNotContain(modules, unexpected);
        CollectionAssert.Contains(LazyWebAssembly.PublishedModules(Extras), "BitDataGrid");
        CollectionAssert.DoesNotContain(LazyWebAssembly.PublishedModules(Extras), "BitPdfViewer");

        Assert.IsFalse(LazyWebAssembly.HasAsset(Core, "scripts/bit.blazorui.js"));
        Assert.IsTrue(LazyWebAssembly.HasAsset(Core, "scripts/bit.blazorui.boot.js"));
    }

    [TestMethod]
    [DynamicData(nameof(BrowserSession.Engines), typeof(BrowserSession))]
    public async Task TheTrimmedLazyServerAppShouldRunInEveryEngine(string engine)
    {
        await using var session = await BrowserSession.OpenAsync(engine);
        LazyServer.AssertSucceeded();

        var requested = new List<string>();
        session.Page.Request += (_, request) => requested.Add(request.Url);

        await using var server = await AppServer.StartPublishedAsync(LazyServer.Directory, "Bit.BlazorUI.Tests.Trimming.Fixture");
        var page = session.Page;

        await page.GotoAsync(server.Url);
        await session.WaitForAsync("() => document.getElementById('fixture-ready')?.hidden === false", "The circuit never rendered the components");

        // No bundle ran; the initializer booted what acts without being called, and nothing of a component the page does
        // not render came with it.
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.$bundles === 'undefined'"), "A bundle ran in a lazy app.");
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Utils.claimEscape === 'function'"), "The claim of the Escape key was not booted.");
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Swiper === 'undefined' && typeof BitBlazorUI.SnackBars === 'undefined'"));
        Assert.IsFalse(requested.Any(r => r.Contains("/bit.blazorui.js", StringComparison.Ordinal) || r.Contains("/bit.blazorui.extras.js", StringComparison.Ordinal)));

        await session.AssertEveryComponentIsStyledAsync(LazyServer, Core, Extras);

        // The dropdown's callout opens and closes on Escape - the callouts' module imported for it.
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-drp").First, "BitDropdown");
        Assert.IsTrue(requested.Any(r => r.Contains("/_content/Bit.BlazorUI/scripts/modules/Callouts.js", StringComparison.Ordinal)), "The callouts' module was not imported.");

        // The tooltip shows on the pointer and goes on Escape.
        await page.Locator(".bit-ttp .bit-ico").First.HoverAsync();
        await session.WaitForAsync("() => { const tip = document.querySelector('.bit-ttp [role=tooltip]'); return !!tip && getComputedStyle(tip).visibility !== 'hidden' && tip.getBoundingClientRect().width > 0; }", "BitTooltip did not show on the pointer");
        await page.Keyboard.PressAsync("Escape");
        await session.WaitForAsync("() => { const tip = document.querySelector('.bit-ttp [role=tooltip]'); return !tip || getComputedStyle(tip).visibility === 'hidden' || tip.getBoundingClientRect().width === 0; }", "Escape did not dismiss the BitTooltip");

        await page.Locator(".bit-tfl input").First.FillAsync("Ada");
        Assert.AreEqual("Ada", await page.Locator(".bit-tfl input").First.InputValueAsync());

        session.AssertNoErrors($"The trimmed lazy server app in {engine}");
    }

    [TestMethod]
    [DynamicData(nameof(BrowserSession.Engines), typeof(BrowserSession))]
    public async Task TheTrimmedLazyWebAssemblyAppShouldRunInEveryEngine(string engine)
    {
        await using var session = await BrowserSession.OpenAsync(engine);
        LazyWebAssembly.AssertSucceeded();

        await using var server = await AppServer.StartStaticAsync(LazyWebAssembly.WebRoot, "index.lazy.html");
        var page = session.Page;

        await page.GotoAsync(server.Url);
        await page.WaitForSelectorAsync(".bit-btn", new() { Timeout = 90_000 });

        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.$bundles === 'undefined'"), "A bundle ran in a lazy app.");
        await session.AssertEveryComponentIsStyledAsync(LazyWebAssembly, Core, Extras);

        // The scripts with the most moving parts, each imported the first time it is called - the synchronous calls of
        // WebAssembly included (FastInvoke), which import their module before they run.
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-drp").First, "BitDropdown");
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-dtp input").First, "BitDatePicker");

        await page.Locator(".bit-tgi input").First.FillAsync("alpha");
        await page.Locator(".bit-tgi input").First.PressAsync("Enter");
        await session.WaitForAsync("() => document.querySelectorAll('.bit-tgi-tag').length === 1", "BitTagsInput did not add the tag typed into it");

        // The data grid's script of the Extras, which uses the core's focus scripts: the .NET side imports those first.
        await session.WaitForAsync("() => document.querySelector('.bit-dtg')?.textContent.includes('Ada')", "BitDataGrid rendered no rows");
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.DataGrid === 'function' || typeof BitBlazorUI.DataGrid === 'object'"), "The data grid's module was not imported.");

        // The theme switcher (BitTheme.ts, booted for its markup and called from .NET) flips the theme.
        await PressAsync(page.Locator(".bit-ths-lgt").First);
        await session.WaitForAsync("() => document.documentElement.getAttribute('bit-theme') === 'dark'", "BitThemeSwitcher did not turn the page dark");

        session.AssertNoErrors($"The trimmed lazy WebAssembly app in {engine}");
    }

    private static async Task PressAsync(ILocator button)
    {
        await button.FocusAsync();
        await button.PressAsync("Enter");
    }
}
