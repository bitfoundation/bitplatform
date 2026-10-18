using Bit.BlazorUI.Tests.Trimming.Infrastructure;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The trimming of a publish ILLink does not touch - a Blazor Web App's server, rendering Fixture's Main.razor
/// interactively - which reads the app's assemblies for the library types they reference (BitBlazorUIScan) and the
/// untrimmed library assemblies for everything those reach.
/// </summary>
[TestClass]
public sealed class ServerAppTrimmingTests
{
    private const string Core = "Bit.BlazorUI";
    private const string Extras = "Bit.BlazorUI.Extras";
    private const string Legacy = "Bit.BlazorUI.Legacy";
    private const string Icons = "Bit.BlazorUI.Icons";

    private static PublishResult Trimmed => Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUITrimIcons=true");

    [TestMethod]
    public void AServerAppShouldPublishTheFullBundlesUnlessItAsksForTheTrimming()
    {
        var publish = Publisher.Publish(Publisher.Fixture);
        publish.AssertSucceeded();

        // The trimming only ever runs where its switch is on, which a server app turns on itself.
        foreach (var (package, bundle) in new[] { (Core, "scripts/bit.blazorui.js"), (Core, "styles/bit.blazorui.css"), (Extras, "styles/bit.blazorui.extras.css"), (Icons, "styles/bit.blazorui.icons.css") })
        {
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(Publisher.Root, package, "wwwroot", bundle.Replace('/', Path.DirectorySeparatorChar))),
                                      File.ReadAllBytes(publish.GetAssetPath(package, bundle)),
                                      $"{package}/{bundle} was changed by a publish that did not ask for the trimming.");
        }

        Assert.IsTrue(Directory.Exists(publish.GetAssetPath(Legacy, "pdf.js")), "A vendor folder was left out of a publish that did not ask for the trimming.");
    }

    [TestMethod]
    public void TheStylesheetsShouldBeThoseOfTheRenderedComponentsAlone()
    {
        Trimmed.AssertSucceeded();

        CollectionAssert.AreEqual(new[] { "BitButton", "BitDropdown", "BitTextField", "BitTooltip", "BitIcon", "BitText" }, Trimmed.KeptStyles(Core));
        CollectionAssert.AreEqual(new[] { "BitErrorBoundary" }, Trimmed.KeptStyles(Extras));
        CollectionAssert.AreEqual(Array.Empty<string>(), Trimmed.KeptStyles(Legacy));

        // Every variant of the core bundle keeps the same components: they differ in their theme, never in their parts.
        foreach (var bundle in new[] { "styles/bit.blazorui.fluent.css", "styles/bit.blazorui.fluent-dark.css", "styles/bit.blazorui.fluent-light.css" })
        {
            CollectionAssert.AreEqual(Trimmed.KeptStyles(Core), Trimmed.KeptStyles(Core, bundle), bundle);
        }
    }

    [TestMethod]
    public void TheScriptsShouldBeThoseTheRenderedComponentsCanReach()
    {
        Trimmed.AssertSucceeded();
        var scripts = Trimmed.KeptScripts(Core);

        // What the components call into, what those scripts use, what every bundle has - and what the app's own script
        // (wwwroot/scripts/fixture.js) calls through window.BitBlazorUI, which nothing of its .NET code names.
        foreach (var expected in new[] { "general", "Utils.Keys", "BitTextField", "Utils.Escape", "Callouts", "Callouts.Registry", "Utils.Tooltips", "Utils.Focus", "PageVisibility" })
        {
            CollectionAssert.Contains(scripts, expected, $"The trimmed core script is missing {expected}.");
        }

        // What no component of the app reaches - nor the services AddBitBlazorUIServices registers, which the app never
        // asks for: the modal's and the snack bars' scripts, the theme's (no bit-theme attribute is written anywhere).
        foreach (var unexpected in new[] { "BitScrollablePane", "BitSwiper", "BitSplitter", "BitFileUpload", "SnackBars", "BitTheme", "BitPivot", "Stickies" })
        {
            CollectionAssert.DoesNotContain(scripts, unexpected, $"The trimmed core script keeps {unexpected}, which nothing the app renders uses.");
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), Trimmed.KeptScripts(Legacy));
    }

    [TestMethod]
    public void EveryTrimmedBundleShouldBeItsPiecesInTheFullBundlesOrder()
    {
        Trimmed.AssertSucceeded();

        foreach (var package in new[] { Core, Extras, Legacy })
        {
            Trimmed.AssertScriptsAreThePiecesInOrder(package);
            Trimmed.AssertStylesAreThePiecesInOrder(package);
        }
    }

    [TestMethod]
    public void TheIconsShouldBeTheOnesTheCodeNames()
    {
        Trimmed.AssertSucceeded();
        var css = Trimmed.ReadAsset(Icons, "styles/bit.blazorui.icons.css");

        // BitIconName.Add is a const, so the name is a literal of the app's own code; nothing names Delete.
        StringAssert.Contains(css, ".bit-icon--Add::before");
        Assert.IsFalse(css.Contains(".bit-icon--Delete::before"), "A glyph nothing names was kept.");
        StringAssert.Contains(css, "@font-face");

        // A name the code does not carry - read out of data - is kept by naming it in the csproj.
        var named = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUITrimIcons=true", "FixtureIcons=Delete");
        named.AssertSucceeded();
        StringAssert.Contains(named.ReadAsset(Icons, "styles/bit.blazorui.icons.css"), ".bit-icon--Delete::before");
    }

    [TestMethod]
    public void TheVendorFoldersNothingLoadsShouldBeLeftOut()
    {
        Trimmed.AssertSucceeded();

        foreach (var (package, folder) in new[] { (Legacy, "pdf.js"), (Legacy, "quilljs"), (Legacy, "chart.js"), (Legacy, "marked"), (Extras, "flags"), (Extras, "leaflet"), (Extras, "openlayers") })
        {
            Assert.IsFalse(Directory.Exists(Trimmed.GetAssetPath(package, folder)), $"{package}/{folder} was published though nothing the app runs loads it.");
            Assert.IsFalse(Trimmed.ReadEndpoints().Any(e => e.GetProperty("Route").GetString()!.StartsWith($"_content/{package}/{folder}/", StringComparison.Ordinal)),
                           $"{package}/{folder} still has endpoints.");
        }

        // The packages' own fonts are what their stylesheets load, and stay.
        Assert.IsTrue(Directory.Exists(Trimmed.GetAssetPath(Icons, "fonts")));
    }

    [TestMethod]
    public void ATrimmedBundleShouldBeServedAtItsOwnUrlWithItsLabel()
    {
        Trimmed.AssertSucceeded();
        var endpoints = Trimmed.ReadEndpoints();
        var length = new FileInfo(Trimmed.GetAssetPath(Core, "styles/bit.blazorui.css")).Length.ToString();

        // The plain route serves the trimmed file - its length is the trimmed one's.
        var plain = endpoints.Single(e => e.GetProperty("Route").GetString() == "_content/Bit.BlazorUI/styles/bit.blazorui.css" && e.GetProperty("Selectors").GetArrayLength() == 0);
        Assert.AreEqual(length, plain.GetProperty("ResponseHeaders").EnumerateArray().Single(h => h.GetProperty("Name").GetString() == "Content-Length").GetProperty("Value").GetString());

        // The fingerprinted one is found through its label, which is what @Assets["_content/Bit.BlazorUI/..."] reads.
        var labelled = endpoints.Where(e => e.GetProperty("EndpointProperties").EnumerateArray()
                                             .Any(p => p.GetProperty("Name").GetString() == "label" && p.GetProperty("Value").GetString() == "_content/Bit.BlazorUI/styles/bit.blazorui.css"))
                                .ToList();
        Assert.IsTrue(labelled.Count > 0, "No endpoint carries the label of the trimmed core stylesheet.");
        Assert.IsTrue(labelled.All(e => e.GetProperty("Route").GetString()!.StartsWith("_content/Bit.BlazorUI/styles/bit.blazorui.", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WhatTheCsprojNamesShouldBeKeptWhateverTheSignalsSay()
    {
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "FixtureComponents=BitCard", "FixtureAssets=BitScrollablePane|BitSwiper");
        publish.AssertSucceeded();

        // A component is kept with everything it reaches; a script or a stylesheet by its own name.
        CollectionAssert.Contains(publish.KeptStyles(Core), "BitCard");
        CollectionAssert.Contains(publish.KeptStyles(Core), "BitSwiper");
        CollectionAssert.Contains(publish.KeptScripts(Core), "BitScrollablePane");
        CollectionAssert.Contains(publish.KeptScripts(Core), "BitSwiper");
    }

    [TestMethod]
    public void ANameThatIsNothingOfThePackagesShouldFailThePublish()
    {
        var component = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "FixtureComponents=BitNoSuchComponent");
        Assert.AreNotEqual(0, component.ExitCode, "A <BitBlazorUIComponent> naming no type was let through.");
        StringAssert.Contains(component.Output, "<BitBlazorUIComponent Include=\"BitNoSuchComponent\" /> names no type");

        var asset = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "FixtureAssets=NoSuchScript");
        Assert.AreNotEqual(0, asset.ExitCode, "A <BitBlazorUIAsset> naming no piece was let through.");
        StringAssert.Contains(asset.Output, "<BitBlazorUIAsset Include=\"NoSuchScript\" /> names no script or component stylesheet");
    }

    [TestMethod]
    public void WithNothingToGoOnTheFullBundlesShouldBePublished()
    {
        // The switch on, but the scan off and nothing named: no signal, so nothing is trimmed - never to nothing.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUIScan=None");
        publish.AssertSucceeded();

        CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(Publisher.Root, Core, "wwwroot", "styles", "bit.blazorui.css")),
                                  File.ReadAllBytes(publish.GetAssetPath(Core, "styles/bit.blazorui.css")));
    }

    [TestMethod]
    public void NamesOfScriptsOrIconsAloneShouldNotTrimAnything()
    {
        // A list of glyphs or scripts says nothing of the components an app renders: with the scan off and no component
        // named, the names add to nothing, and every bundle is published whole - never trimmed down to the names alone.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUITrimIcons=true", "BitBlazorUIScan=None", "FixtureIcons=Add", "FixtureAssets=BitButton");
        publish.AssertSucceeded();

        foreach (var (package, bundle) in new[] { (Core, "scripts/bit.blazorui.js"), (Core, "styles/bit.blazorui.css"), (Icons, "styles/bit.blazorui.icons.css") })
        {
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(Publisher.Root, package, "wwwroot", bundle.Replace('/', Path.DirectorySeparatorChar))),
                                      File.ReadAllBytes(publish.GetAssetPath(package, bundle)),
                                      $"{package}/{bundle} was trimmed off names that say nothing of the app's components.");
        }
    }

    [TestMethod]
    [DynamicData(nameof(BrowserSession.Engines), typeof(BrowserSession))]
    public async Task TheTrimmedAppShouldRunInEveryEngine(string engine)
    {
        await using var session = await BrowserSession.OpenAsync(engine);
        Trimmed.AssertSucceeded();

        await using var server = await AppServer.StartPublishedAsync(Trimmed.Directory, "Bit.BlazorUI.Tests.Trimming.Fixture");
        var page = session.Page;

        await page.GotoAsync(server.Url);
        await session.WaitForAsync("() => document.getElementById('fixture-ready')?.hidden === false", "The circuit never rendered the components");

        await session.AssertEveryComponentIsStyledAsync(Trimmed, Core, Extras);

        // Kept for what the app renders, and the rest left out of the very bundle the page loaded.
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Callouts.toggle === 'function' && typeof BitBlazorUI.Utils.setupTooltip === 'function'"));
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Swiper === 'undefined' && typeof BitBlazorUI.SnackBars === 'undefined'"));

        // The dropdown's callout opens and closes on Escape.
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-drp").First, "BitDropdown");

        // The tooltip shows on the pointer and goes on Escape (Utils.Tooltips, kept for BitTooltip's interop calls).
        await page.Locator(".bit-ttp .bit-ico").First.HoverAsync();
        await session.WaitForAsync("() => { const tip = document.querySelector('.bit-ttp [role=tooltip]'); return !!tip && getComputedStyle(tip).visibility !== 'hidden' && tip.getBoundingClientRect().width > 0; }", "BitTooltip did not show on the pointer");
        await page.Keyboard.PressAsync("Escape");
        await session.WaitForAsync("() => { const tip = document.querySelector('.bit-ttp [role=tooltip]'); return !tip || getComputedStyle(tip).visibility === 'hidden' || tip.getBoundingClientRect().width === 0; }", "Escape did not dismiss the BitTooltip");

        // Typed into the text field, which stays what was typed.
        await page.Locator(".bit-tfl input").First.FillAsync("Ada");
        Assert.AreEqual("Ada", await page.Locator(".bit-tfl input").First.InputValueAsync());

        // The app's own script reaches the script it calls through the global.
        await page.EvaluateAsync("() => window.fixtureDisposeVisibility('nothing-registered')");

        // The icon stylesheet the page loaded has the glyph the code names, and not the one it does not (read off its rules:
        // the core stylesheet draws a few common glyphs of its own, Delete among them).
        var glyphs = await page.EvaluateAsync<bool[]>(@"() => {
            const sheet = [...document.styleSheets].find(s => (s.href || '').includes('bit.blazorui.icons'));
            const selectors = [...sheet.cssRules].map(r => r.selectorText || '');
            return ['Add', 'Delete'].map(name => selectors.some(s => s.includes('.bit-icon--' + name + '::')));
        }");
        Assert.IsTrue(glyphs[0], "The icon stylesheet the page loaded has no rule for the Add glyph the code names.");
        Assert.IsFalse(glyphs[1], "The icon stylesheet the page loaded still has a rule for the Delete glyph nothing names.");

        // And the glyph is drawn with it.
        var add = await page.EvaluateAsync<string>(@"() => {
            const probe = document.createElement('i');
            probe.className = 'bit-icon bit-icon--Add';
            document.body.appendChild(probe);
            const content = getComputedStyle(probe, '::before').content;
            probe.remove();
            return content;
        }");
        Assert.IsFalse(add is "none" or "normal" or "" or "\"\"", $"The Add glyph the code names is not drawn: {add}");

        session.AssertNoErrors($"The trimmed server app in {engine}");
    }

    [TestMethod]
    public void TheTypeNamesScanShouldKeepAtLeastWhatTheTypeReferencesKeep()
    {
        // The coarser mode matches names rather than references: it may keep more, never less.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", "BitBlazorUIScan=TypeNames");
        publish.AssertSucceeded();
        Trimmed.AssertSucceeded();

        CollectionAssert.IsSubsetOf(Trimmed.KeptStyles(Core), publish.KeptStyles(Core));
        CollectionAssert.IsSubsetOf(Trimmed.KeptScripts(Core), publish.KeptScripts(Core));
        CollectionAssert.DoesNotContain(publish.KeptStyles(Core), "BitSwiper");
        publish.AssertScriptsAreThePiecesInOrder(Core);
        publish.AssertStylesAreThePiecesInOrder(Core);
    }

    [TestMethod]
    public void AScanReadingNothingOfTheAppShouldKeepEverythingAndSaySo()
    {
        // Assemblies none of which references the packages are not the app's: trimming on what they say would leave out
        // every script and stylesheet the app uses.
        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", $"FixtureScanAssemblies={typeof(Bit.BlazorUI.Build.PackageAssets).Assembly.Location}");
        publish.AssertSucceeded();

        StringAssert.Contains(publish.Output, "BITBLAZORUI002");
        CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(Publisher.Root, Core, "wwwroot", "styles", "bit.blazorui.css")),
                                  File.ReadAllBytes(publish.GetAssetPath(Core, "styles/bit.blazorui.css")));
    }

    [TestMethod]
    public void APublishTrimmedByDotNetShouldTrimTheAssetsWithoutBeingAsked()
    {
        // No BitBlazorUITrimAssets: the .NET trimmer running is what turns it on, as in a WebAssembly project. (ILLink also
        // trims away the pages Blazor finds by reflection here, which is the app's own business - only the switch is
        // checked.)
        var publish = Publisher.Publish(Publisher.Fixture, "FixturePublishTrimmed=true");
        publish.AssertSucceeded();

        StringAssert.Contains(publish.Output, "Bit.BlazorUI: Bit.BlazorUI/styles/bit.blazorui.css -> ");
        Assert.AreNotEqual(new FileInfo(Path.Combine(Publisher.Root, Core, "wwwroot", "styles", "bit.blazorui.css")).Length,
                           new FileInfo(publish.GetAssetPath(Core, "styles/bit.blazorui.css")).Length);
    }

    [TestMethod]
    public void TheMarkupOfTheAppShouldKeepTheScriptsItNeeds()
    {
        // The theme's script reads the bit-theme attributes off the page, which an app may write in a host page the build
        // does not compile; a file the csproj names is read for them like the app's own .html is.
        var page = Path.Combine(Path.GetTempPath(), "bit-blazorui-trimming", "host-page.html");
        Directory.CreateDirectory(Path.GetDirectoryName(page)!);
        File.WriteAllText(page, "<html bit-theme-system bit-theme-persist><body></body></html>");

        var publish = Publisher.Publish(Publisher.Fixture, "BitBlazorUITrimAssets=true", $"FixtureScanFiles={page}");
        publish.AssertSucceeded();

        CollectionAssert.Contains(publish.KeptScripts(Core), "BitTheme");
        CollectionAssert.DoesNotContain(Trimmed.KeptScripts(Core), "BitTheme");
    }
}
