using System.IO;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiArtifactView at /e2e/aiartifactview in a real browser: the body of every kind (the code, a document, an
/// HTML page running in a sandboxed frame that cannot reach the page around it, a JSON tree, a CSV, TSV and JSON table, an
/// image, an SVG shown as an image whose script never runs), the glyph of the kind from the AI icon font, the theme
/// tokens, the version dropdown, copy and download of the version on screen (the file name and the text saved), an
/// artifact streamed and revised through a session of the scripted transport, the full-screen panel (the overlay, the
/// small screen, Escape claimed only while expanded and a modified Escape left alone, the focus moved into the panel, a
/// modal around it, the running frame kept across the toggle), Disabled, RTL and the public variables.
/// </summary>
[TestClass]
public class BitAiArtifactViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiartifactview";

    private const string ClipboardStub = """
        Object.defineProperty(navigator, 'clipboard', {
            configurable: true,
            value: {
                writeText: text => { (window.__atvCopied ||= []).push(text); return Promise.resolve(); },
                readText: () => Promise.resolve((window.__atvCopied || []).at(-1) ?? '')
            }
        });
        """;

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Panel(string id) => Page.Locator($"#{id} > .bit-aiatv-pnl");

    private ILocator Body(string id) => Panel(id).Locator(":scope > .bit-aiatv-bdy");

    private ILocator ExpandButton(string id) => Panel(id).Locator(".bit-aiatv-exb");

    private ILocator DownloadButton(string id) => Panel(id).Locator(".bit-aiatv-dlb");

    private ILocator VersionDropdown(string id) => Panel(id).Locator(".bit-aiatv-ver");

    private static Regex Expanded => new(@"(^|\s)bit-aiatv-exp(\s|$)");

    // ---------- kinds ----------

    [TestMethod]
    public async Task Kinds_Code_ShowsTheCodeInACodeBlockWithoutAHeaderOfItsOwn()
    {
        await OpenAsync(Route);

        await Expect(Root("atv-code")).ToHaveClassAsync(HasClass("bit-aiatv"));
        await Expect(Panel("atv-code")).ToHaveAttributeAsync("aria-label", "Greeter.cs");
        await Expect(Panel("atv-code")).ToHaveAttributeAsync("data-artifact-id", new Regex(".+"));
        await Expect(Panel("atv-code").Locator(".bit-aiatv-ttl")).ToHaveTextAsync("Greeter.cs");
        await Expect(Panel("atv-code").Locator(".bit-aiatv-ttl")).ToHaveAttributeAsync("title", "Greeter.cs");

        var code = Body("atv-code").Locator(".bit-aicbv code");
        await Expect(code).ToContainTextAsync("public sealed class Greeter");
        await Expect(code).ToHaveClassAsync(HasClass("language-csharp"));

        // The panel's header is the only one: the code block's own is left out.
        await Expect(Body("atv-code").Locator(".bit-aicbv-hdr")).ToHaveCountAsync(0);

        // Nothing streams, so the header shows no dots and the body is not busy.
        await Expect(Panel("atv-code").Locator(".bit-aiatv-ldg")).ToHaveCountAsync(0);
        await Expect(Body("atv-code")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
    }

    [TestMethod]
    public async Task Kinds_Document_RendersItsMarkdown()
    {
        await OpenAsync(Route);

        var document = Body("atv-doc").Locator(".bit-aiatv-doc");
        await Expect(document.GetByRole(AriaRole.Heading, new() { Name = "Release notes" })).ToBeVisibleAsync();
        await Expect(document.Locator("li")).ToHaveCountAsync(2);
        await Expect(document.Locator("strong")).ToHaveTextAsync("Artifacts");
    }

    [TestMethod]
    public async Task Kinds_Html_RunsItsScriptInASandboxedFrameThatCannotReachTheHostPage()
    {
        await OpenAsync(Route);

        var frame = Body("atv-html").Locator("iframe.bit-aiatv-frm");
        await Expect(frame).ToHaveAttributeAsync("title", "Counter page");

        // Scripts, forms and popups, but never the same origin: that is what keeps the page around it out of reach.
        var sandbox = await frame.GetAttributeAsync("sandbox");
        CollectionAssert.AreEquivalent(new[] { "allow-scripts", "allow-forms", "allow-popups" }, sandbox!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        Assert.IsTrue(await frame.EvaluateAsync<bool>("f => f.sandbox.contains('allow-scripts') && !f.sandbox.contains('allow-same-origin')"));

        var page = Page.FrameLocator("#atv-html iframe.bit-aiatv-frm");
        await Expect(page.Locator("#ran")).ToHaveTextAsync("ran");
        await Expect(page.Locator("#origin")).ToHaveTextAsync("null");
        await Expect(page.Locator("#parent")).ToHaveTextAsync("blocked:SecurityError");
        await Expect(page.Locator("#cookie")).ToHaveTextAsync("none");
        await Expect(Page.Locator("body")).Not.ToHaveAttributeAsync("data-frame-reached", new Regex(".*"));

        // The page inside is live: its own script answers its own button.
        await page.Locator("#counter").ClickAsync();
        await Expect(page.Locator("#counter")).ToHaveTextAsync("Clicked 1 times");

        // A generated page is drawn on the white page it was written for, across the width of the panel.
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");
        var frameBox = await frame.BoundingBoxAsync();
        var bodyBox = await Body("atv-html").BoundingBoxAsync();
        Assert.AreEqual(bodyBox!.Width, frameBox!.Width, 1.5);
        Assert.IsTrue(frameBox.Height > 100, $"The frame is tall enough to read, was {frameBox.Height}.");
    }

    [TestMethod]
    public async Task Kinds_Json_ShowsTheValueAsATree()
    {
        await OpenAsync(Route);

        var tree = Body("atv-json").Locator(".bit-aiatv-jsn > .bit-aijsv");
        await Expect(tree).ToBeVisibleAsync();
        await Expect(tree.Locator(".bit-aijsv-key").First).ToHaveTextAsync("\"theme\"");
        await Expect(tree.Locator(".bit-aijsv-str").First).ToHaveTextAsync("\"dark\"");
        await Expect(tree.Locator(".bit-aijsv-num")).ToHaveTextAsync("14");
        await Expect(tree.Locator(".bit-aijsv-bln")).ToHaveTextAsync("true");
        await Expect(tree.Locator(".bit-aijsv-nul")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Kinds_Csv_ShowsATableThatKeepsItsQuotedFields()
    {
        await OpenAsync(Route);

        var table = Body("atv-csv").Locator(".bit-aiatv-tbw > table.bit-aiatv-tbl");
        await Expect(table.Locator("thead th")).ToHaveTextAsync(["product", "price", "stock"]);
        await Expect(table.Locator("thead th").First).ToHaveAttributeAsync("scope", "col");
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(3);
        await Expect(table.Locator("tbody tr").Nth(0).Locator("td")).ToHaveTextAsync(["Keyboard, wireless", "89.50", "12"]);
        await Expect(table.Locator("tbody tr").Nth(2).Locator("td")).ToHaveTextAsync(["Monitor 27\"", "329", "4"]);
    }

    [TestMethod]
    public async Task Kinds_Tsv_ShowsATableSplitOnTabs()
    {
        await OpenAsync(Route);

        var table = Body("atv-tsv").Locator("table.bit-aiatv-tbl");
        await Expect(table.Locator("thead th")).ToHaveTextAsync(["region", "sales"]);
        await Expect(table.Locator("tbody tr").Nth(1).Locator("td")).ToHaveTextAsync(["Asia", "1.9M"]);
    }

    [TestMethod]
    public async Task Kinds_JsonTable_ShowsADataTable()
    {
        await OpenAsync(Route);

        var table = Body("atv-jtable").Locator(".bit-aidtb");
        await Expect(table.Locator("th")).ToHaveTextAsync(["Name", "Role"]);
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(2);
        await Expect(table.Locator("tbody tr").Nth(1).Locator("td")).ToHaveTextAsync(["Grace", "Architect"]);
    }

    [TestMethod]
    public async Task Kinds_Image_ShowsTheImageNamedByItsTitle()
    {
        await OpenAsync(Route);

        var image = Body("atv-image").Locator("img.bit-aiatv-img");
        await Expect(image).ToHaveAttributeAsync("alt", "A pixel");
        await Expect(image).ToHaveAttributeAsync("src", new Regex("^data:image/png;base64,"));
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#atv-image img'); return i && i.complete && i.naturalWidth === 1; }");

        // An image shown from its address has no text to copy or download: neither button saves an empty file.
        await Expect(DownloadButton("atv-image")).ToBeDisabledAsync();
        await Expect(Panel("atv-image").Locator(".bit-aicpb button")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Kinds_Svg_IsShownAsAnImageWhoseScriptNeverRuns()
    {
        await OpenAsync(Route);

        var image = Body("atv-svg").Locator("img.bit-aiatv-img");
        await Expect(image).ToHaveAttributeAsync("alt", "Badge");
        await Expect(image).ToHaveAttributeAsync("src", new Regex("^data:image/svg\\+xml;base64,"));
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#atv-svg img'); return i && i.complete && i.naturalWidth === 120; }");

        // The markup is never put into the page, where its script would run.
        await Expect(Body("atv-svg").Locator("svg")).ToHaveCountAsync(0);
        await NextFramesAsync();
        await Expect(Page.Locator("body")).Not.ToHaveAttributeAsync("data-svg-ran", new Regex(".*"));
    }

    [TestMethod]
    [DataRow("atv-code", "Code")]
    [DataRow("atv-html", "Code")]
    [DataRow("atv-json", "Code")]
    [DataRow("atv-doc", "Page")]
    [DataRow("atv-image", "Photo2")]
    [DataRow("atv-svg", "Photo2")]
    [DataRow("atv-csv", "MapLayers")]
    public async Task Header_TheGlyphOfTheKind_IsDrawnFromTheAiIconFont(string id, string glyph)
    {
        await OpenAsync(Route);

        var icon = Panel(id).Locator(".bit-aiatv-ico");
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await icon.AssertAiGlyphAsync(glyph);

        // The buttons of the header draw their glyphs from the same font, none of which is in the core's.
        await DownloadButton(id).Locator("i").First.AssertAiGlyphAsync("Download");
        await ExpandButton(id).Locator("i").First.AssertAiGlyphAsync("FullScreen");

        // And the font is really loaded, so the glyphs are not drawn as boxes.
        var status = await Page.EvaluateAsync<string>(
            """
            async family => {
                await document.fonts.ready;
                const face = [...document.fonts].find(f => f.family.replace(/["']/g, '') === family);
                if (!face) return 'missing';
                if (face.status !== 'loaded') await face.load().catch(() => null);
                return face.status;
            }
            """, AiE2EHelpers.AiGlyphFontFamily);
        Assert.AreEqual("loaded", status);
    }

    [TestMethod]
    public async Task Theme_ThePanel_ReadsTheBitThemeTokens()
    {
        await OpenAsync(Route);

        var panel = Panel("atv-code");
        await Expect(panel).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(panel).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(panel).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(panel).ToHaveCSSAsync("border-top-left-radius", await panel.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));

        var header = panel.Locator(".bit-aiatv-hdr");
        await Expect(header).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(header).ToHaveCSSAsync("border-bottom-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        await Expect(header).ToHaveCSSAsync("display", "flex");

        await Expect(panel.Locator(".bit-aiatv-ico")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));

        var title = panel.Locator(".bit-aiatv-ttl");
        await Expect(title).ToHaveCSSAsync("font-size", await title.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));
        var weight = await title.EvaluateAsync<string>(
            "e => { const p = document.createElement('span'); p.style.fontWeight = 'var(--bit-tpg-fw-semibold)'; e.appendChild(p); const w = getComputedStyle(p).fontWeight; p.remove(); return w; }");
        await Expect(title).ToHaveCSSAsync("font-weight", weight);
        await Expect(title).ToHaveCSSAsync("text-overflow", "ellipsis");
    }

    // ---------- versions, copy, download ----------

    [TestMethod]
    public async Task Versions_TheDropdown_SwitchesTheVersionOnScreen()
    {
        await OpenAsync(Route);

        var dropdown = VersionDropdown("atv-versions");
        var field = dropdown.Locator(":scope > .bit-drp-wrp");
        await Expect(field).ToHaveAttributeAsync("aria-label", "Version");
        await Expect(dropdown.Locator(".bit-drp-tcn")).ToHaveTextAsync("Version 3");
        await Expect(Body("atv-versions").Locator("code")).ToHaveTextAsync("export const price = (quantity: number) => quantity * (quantity >= 10 ? 9 : 10);");

        var callout = await OpenVersionsAsync("atv-versions");
        await Expect(callout.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Version 1", "Version 2", "Version 3"]);
        await callout.GetByRole(AriaRole.Option, new() { Name = "Version 1", Exact = true }).ClickAsync();

        await Expect(callout).ToBeHiddenAsync();
        await Expect(dropdown.Locator(".bit-drp-tcn")).ToHaveTextAsync("Version 1");
        await Expect(Body("atv-versions").Locator("code")).ToHaveTextAsync("export const price = 10;");

        // And back to the latest one.
        callout = await OpenVersionsAsync("atv-versions");
        await callout.GetByRole(AriaRole.Option, new() { Name = "Version 3", Exact = true }).ClickAsync();
        await Expect(Body("atv-versions").Locator("code")).ToContainTextAsync("quantity >= 10");
    }

    [TestMethod]
    public async Task Versions_InitialVersion_OpensAtThatVersion()
    {
        await OpenAsync(Route);

        await Expect(VersionDropdown("atv-initial").Locator(".bit-drp-tcn")).ToHaveTextAsync("Version 2");
        await Expect(Body("atv-initial").Locator("code")).ToHaveTextAsync("export const price = (quantity: number) => quantity * 10;");
    }

    [TestMethod]
    public async Task Versions_ASingleVersion_ShowsNoDropdown()
    {
        await OpenAsync(Route);

        await Expect(VersionDropdown("atv-code")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Copy_TheCopyButton_CopiesTheVersionOnScreen()
    {
        await Page.AddInitScriptAsync(ClipboardStub);
        await OpenAsync(Route);

        var copy = Panel("atv-versions").Locator(".bit-aicpb button");
        await copy.ClickAsync();
        await Expect(copy).ToHaveAttributeAsync("aria-label", "Copied");
        Assert.AreEqual("export const price = (quantity: number) => quantity * (quantity >= 10 ? 9 : 10);", (await CopiedAsync()).Last());

        var callout = await OpenVersionsAsync("atv-versions");
        await callout.GetByRole(AriaRole.Option, new() { Name = "Version 1", Exact = true }).ClickAsync();
        await Expect(Body("atv-versions").Locator("code")).ToHaveTextAsync("export const price = 10;");

        await copy.ClickAsync();
        await Page.WaitForFunctionAsync("() => (window.__atvCopied || []).length === 2");
        Assert.AreEqual("export const price = 10;", (await CopiedAsync()).Last());
    }

    [TestMethod]
    [DataRow("atv-code", "greeter-cs.cs", "public sealed class Greeter\n{\n    public string Greet(string name) => $\"Hello, {name}!\";\n}\n")]
    [DataRow("atv-doc", "release-notes.md", "# Release notes\n\n- **Artifacts** keep a version history.\n- Structured output fills in while it streams.\n")]
    [DataRow("atv-json", "settings.json", """{"theme":"dark","fontSize":14,"autosave":true,"recent":["README.md","Program.cs"],"proxy":null}""")]
    [DataRow("atv-csv", "prices.csv", "product,price,stock\n\"Keyboard, wireless\",89.50,12\nMouse,20,140\n\"Monitor 27\"\"\",329,4\n")]
    [DataRow("atv-tsv", "regions.tsv", "region\tsales\nEurope\t1.2M\nAsia\t1.9M\n")]
    [DataRow("atv-jtable", "team.json", """[{"name":"Ada","role":"Engineer"},{"name":"Grace","role":"Architect"}]""")]
    [DataRow("atv-svg", "badge.svg", null)]
    [DataRow("atv-html", "counter-page.html", null)]
    public async Task Download_EachKind_SavesItsTextUnderTheNameOfItsKind(string id, string fileName, string? text)
    {
        await OpenAsync(Route);

        var button = DownloadButton(id);
        await Expect(button).ToHaveAttributeAsync("aria-label", "Download");
        await Expect(button).ToHaveAttributeAsync("title", "Download");

        var download = await Page.RunAndWaitForDownloadAsync(() => button.ClickAsync());
        Assert.AreEqual(fileName, download.SuggestedFilename);

        var saved = await File.ReadAllTextAsync((await download.PathAsync())!);
        if (text is not null)
        {
            Assert.AreEqual(text, saved);
        }
        else if (id == "atv-svg")
        {
            StringAssert.StartsWith(saved, "<svg xmlns=\"http://www.w3.org/2000/svg\"");
        }
        else
        {
            StringAssert.StartsWith(saved, "<!doctype html>");
            StringAssert.Contains(saved, "document.getElementById('ran').textContent = 'ran';");
        }
    }

    [TestMethod]
    public async Task Download_AnEarlierVersion_SavesThatVersionUnderItsNumber()
    {
        await OpenAsync(Route);

        var latest = await Page.RunAndWaitForDownloadAsync(() => DownloadButton("atv-versions").ClickAsync());
        Assert.AreEqual("pricing-ts.ts", latest.SuggestedFilename);
        Assert.AreEqual("export const price = (quantity: number) => quantity * (quantity >= 10 ? 9 : 10);", await File.ReadAllTextAsync((await latest.PathAsync())!));

        var callout = await OpenVersionsAsync("atv-versions");
        await callout.GetByRole(AriaRole.Option, new() { Name = "Version 2", Exact = true }).ClickAsync();
        await Expect(Body("atv-versions").Locator("code")).ToHaveTextAsync("export const price = (quantity: number) => quantity * 10;");

        var earlier = await Page.RunAndWaitForDownloadAsync(() => DownloadButton("atv-versions").ClickAsync());
        Assert.AreEqual("pricing-ts-v2.ts", earlier.SuggestedFilename);
        Assert.AreEqual("export const price = (quantity: number) => quantity * 10;", await File.ReadAllTextAsync((await earlier.PathAsync())!));
    }

    // ---------- streamed through a session ----------

    [TestMethod]
    public async Task Streaming_TheArtifact_FillsInWhileTheBodySaysItIsBusy()
    {
        await OpenAsync(Route);

        await Expect(Root("atv-stream")).ToHaveCountAsync(0);
        await Page.RecordTextLengthsAsync("atv-stream", "#atv-stream .bit-aiatv-doc");

        await Page.Locator("#btn-stream-write").ClickAsync();

        // While it streams, the dots are in the header (out of the accessibility tree) and the body is busy.
        await Expect(Panel("atv-stream").Locator(".bit-aiatv-ldg")).ToBeVisibleAsync();
        await Expect(Panel("atv-stream").Locator(".bit-aiatv-ldg")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Body("atv-stream")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Panel("atv-stream").Locator(".bit-aiatv-ttl")).ToHaveTextAsync("Project README");

        await Expect(Page.Locator("#atv-stream-answers")).ToHaveTextAsync("1", new() { Timeout = 20000 });

        await Expect(Panel("atv-stream").Locator(".bit-aiatv-ldg")).ToHaveCountAsync(0);
        await Expect(Body("atv-stream")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Body("atv-stream").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Bit.BlazorUI.AI" })).ToBeVisibleAsync();
        await Expect(Body("atv-stream").Locator(".bit-aiatv-doc")).ToContainTextAsync("The AI components of bit BlazorUI.");

        // The text arrived a slice at a time, not in one piece at the end.
        var lengths = (await Page.TextLengthsAsync("atv-stream")).Where(l => l > 0).ToArray();
        Assert.IsTrue(lengths.Length >= 3, $"The document grew in steps: {string.Join(",", lengths)}.");
        CollectionAssert.AreEqual(lengths.OrderBy(l => l).ToArray(), lengths, $"The document only grew: {string.Join(",", lengths)}.");

        // A finished first draft has a single version, so there is nothing to pick between.
        await Expect(VersionDropdown("atv-stream")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Streaming_ARevision_AddsAVersionTheDropdownGoesBackTo()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-stream-write").ClickAsync();
        await Expect(Page.Locator("#atv-stream-answers")).ToHaveTextAsync("1", new() { Timeout = 20000 });
        await Expect(Page.Locator("#atv-stream-state")).ToHaveTextAsync("idle");

        await Page.Locator("#btn-stream-revise").ClickAsync();

        // The revision streams into the same panel, as a second version.
        await Expect(Body("atv-stream")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.Locator("#atv-stream-answers")).ToHaveTextAsync("2", new() { Timeout = 20000 });
        await Expect(Root("atv-stream")).ToHaveCountAsync(1);

        await Expect(VersionDropdown("atv-stream").Locator(".bit-drp-tcn")).ToHaveTextAsync("Version 2");
        await Expect(Body("atv-stream").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Install" })).ToBeVisibleAsync();

        var callout = await OpenVersionsAsync("atv-stream");
        await Expect(callout.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Version 1", "Version 2"]);
        await callout.GetByRole(AriaRole.Option, new() { Name = "Version 1", Exact = true }).ClickAsync();

        await Expect(Body("atv-stream").Locator(".bit-aiatv-doc").GetByRole(AriaRole.Heading, new() { Name = "Install" })).ToHaveCountAsync(0);
        await Expect(Body("atv-stream").Locator(".bit-aiatv-doc")).ToContainTextAsync("The AI components of bit BlazorUI.");

        var download = await Page.RunAndWaitForDownloadAsync(() => DownloadButton("atv-stream").ClickAsync());
        Assert.AreEqual("project-readme-v1.md", download.SuggestedFilename);
        Assert.AreEqual("# Bit.BlazorUI.AI\n\nThe AI components of bit BlazorUI.\n", await File.ReadAllTextAsync((await download.PathAsync())!));
    }

    // ---------- full screen ----------

    [TestMethod]
    public async Task FullScreen_TheToggle_LaysThePanelOverThePageAndTheOverlayTakesItBack()
    {
        await OpenAsync(Route);

        var expand = ExpandButton("atv-full");
        await Expect(expand).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(expand).ToHaveAttributeAsync("aria-label", "Expand");
        await Expect(expand).ToHaveAttributeAsync("title", "Expand");
        await Expect(Root("atv-full").Locator(".bit-aiatv-ovl")).ToHaveCountAsync(0);
        await Expect(Panel("atv-full")).ToHaveCSSAsync("position", "static");

        await expand.ClickAsync();

        await Expect(Root("atv-full")).ToHaveClassAsync(Expanded);
        await Expect(expand).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(expand).ToHaveAttributeAsync("aria-label", "Collapse");
        await expand.Locator("i").First.AssertAiGlyphAsync("BackToWindow");

        var overlay = Root("atv-full").Locator(":scope > .bit-aiatv-ovl");
        await Expect(overlay).ToBeVisibleAsync();
        await Expect(overlay).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(overlay).ToHaveCSSAsync("position", "fixed");
        await Expect(overlay).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-overlay)"));

        // The panel is laid over the page with the inset around it, above the overlay.
        await Expect(Panel("atv-full")).ToHaveCSSAsync("position", "fixed");
        var viewport = await ViewportAsync();
        var (left, top, right, bottom) = await Panel("atv-full").RectAsync();
        Assert.AreEqual(left, top, 1, "The inset is the same on every side.");
        Assert.IsTrue(left > 0, $"There is room around the panel, was {left}.");
        Assert.AreEqual(viewport[0] - left, right, 1.5);
        Assert.AreEqual(viewport[1] - top, bottom, 1.5);
        var onTop = await Page.EvaluateAsync<bool>(
            "() => { const p = document.querySelector('#atv-full > .bit-aiatv-pnl'); const r = p.getBoundingClientRect(); return p.contains(document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2)); }");
        Assert.IsTrue(onTop, "The panel is drawn above the overlay.");

        // A click on the overlay, around the panel, takes it back into the page.
        await overlay.ClickAsync(new() { Position = new() { X = 3, Y = 3 } });

        await Expect(Root("atv-full")).Not.ToHaveClassAsync(Expanded);
        await Expect(overlay).ToHaveCountAsync(0);
        await Expect(Panel("atv-full")).ToHaveCSSAsync("position", "static");
        await Expect(expand).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    public async Task FullScreen_OnASmallScreen_FillsTheWholeViewport()
    {
        await Page.SetViewportSizeAsync(400, 700);
        await OpenAsync(Route);

        await ExpandButton("atv-full").ClickAsync();
        await Expect(Root("atv-full")).ToHaveClassAsync(Expanded);

        var viewport = await ViewportAsync();
        var (left, top, right, bottom) = await Panel("atv-full").RectAsync();
        Assert.AreEqual(0, left, 0.5);
        Assert.AreEqual(0, top, 0.5);
        Assert.AreEqual(viewport[0], right, 0.5);
        Assert.AreEqual(viewport[1], bottom, 0.5);
        await Expect(Panel("atv-full")).ToHaveCSSAsync("border-top-left-radius", "0px");
    }

    [TestMethod]
    public async Task FullScreen_Escape_IsClaimedAndCollapsesOnlyWhileExpanded()
    {
        await OpenAsync(Route);
        await RecordEscapesAsync();

        var root = Root("atv-full");
        var expand = ExpandButton("atv-full");
        await expand.FocusAsync();

        // Inline, the panel does nothing on Escape and leaves it to whatever is around it.
        await Expect(Panel("atv-full")).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await Page.Keyboard.PressAsync("Escape");
        await WaitForEscapesAsync(1);
        Assert.IsFalse((await EscapesAsync())[0], "An inline panel leaves the Escape unclaimed.");
        await Expect(root).Not.ToHaveClassAsync(Expanded);

        // The keyboard expands it...
        await Page.Keyboard.PressAsync("Enter");
        await Expect(root).ToHaveClassAsync(Expanded);
        await Expect(Panel("atv-full")).ToHaveAttributeAsync("data-bit-esc", "claim");

        // ...and the next Escape is claimed and collapses it.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(root).Not.ToHaveClassAsync(Expanded);
        await WaitForEscapesAsync(2);
        Assert.IsTrue((await EscapesAsync())[1], "The expanded panel claims its Escape.");
        await Expect(Panel("atv-full")).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await Expect(expand).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow("Shift+Escape")]
    [DataRow("Control+Escape")]
    [DataRow("Alt+Escape")]
    public async Task FullScreen_AModifiedEscape_IsLeftAlone(string keys)
    {
        await OpenAsync(Route);
        await RecordEscapesAsync();

        var root = Root("atv-full");
        await ExpandButton("atv-full").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(root).ToHaveClassAsync(Expanded);

        await Page.Keyboard.PressAsync(keys);
        await WaitForEscapesAsync(1);
        Assert.IsFalse((await EscapesAsync())[0], $"{keys} is not claimed.");
        await NextFramesAsync();
        await Expect(root).ToHaveClassAsync(Expanded);

        // The plain key still collapses it afterwards.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(root).Not.ToHaveClassAsync(Expanded);
    }

    [TestMethod]
    public async Task FullScreen_Expanding_MovesTheFocusIntoThePanel()
    {
        await OpenAsync(Route);

        // A screen reader activates the button by dispatching a click, which moves no focus of its own: the panel takes
        // it, so the Escape that collapses it reaches it.
        await ExpandButton("atv-full").EvaluateAsync("b => b.click()");
        await Expect(Root("atv-full")).ToHaveClassAsync(Expanded);

        await Page.WaitForFunctionAsync(
            "() => document.querySelector('#atv-full > .bit-aiatv-pnl').contains(document.activeElement)",
            null, new() { Timeout = 5000 });

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("atv-full")).Not.ToHaveClassAsync(Expanded);
    }

    [TestMethod]
    public async Task FullScreen_InAModal_TheFirstEscapeCollapsesThePanelAndTheNextClosesTheModal()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await Expect(Page.Locator("#atv-modal-state")).ToHaveTextAsync("open");
        await Expect(Root("atv-in-modal")).ToBeVisibleAsync();

        var expand = ExpandButton("atv-in-modal");
        await expand.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Root("atv-in-modal")).ToHaveClassAsync(Expanded);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("atv-in-modal")).Not.ToHaveClassAsync(Expanded);
        await NextFramesAsync();
        await Expect(Page.Locator("#atv-modal-state")).ToHaveTextAsync("open");
        await Expect(Root("atv-in-modal")).ToBeVisibleAsync();

        await expand.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#atv-modal-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task FullScreen_TheRunningPage_IsKeptAcrossTheToggle()
    {
        await OpenAsync(Route);

        var frame = Body("atv-full-html").Locator("iframe");
        var page = Page.FrameLocator("#atv-full-html iframe");
        await Expect(page.Locator("#ran")).ToHaveTextAsync("ran");
        var stamp = await page.Locator("#stamp").TextContentAsync();
        await page.Locator("#counter").ClickAsync();
        await page.Locator("#counter").ClickAsync();
        await Expect(page.Locator("#counter")).ToHaveTextAsync("Clicked 2 times");
        await frame.EvaluateAsync("f => f.__e2eKept = true");
        var inlineHeight = (await frame.BoundingBoxAsync())!.Height;

        await ExpandButton("atv-full-html").ClickAsync();
        await Expect(Root("atv-full-html")).ToHaveClassAsync(Expanded);

        // The same frame, still running the same page, now as tall as the body of the full-screen panel.
        Assert.IsTrue(await frame.EvaluateAsync<bool>("f => f.__e2eKept === true"), "The frame was not created again.");
        await Expect(page.Locator("#stamp")).ToHaveTextAsync(stamp!);
        await Expect(page.Locator("#counter")).ToHaveTextAsync("Clicked 2 times");
        var expandedHeight = (await frame.BoundingBoxAsync())!.Height;
        var bodyHeight = (await Body("atv-full-html").BoundingBoxAsync())!.Height;
        Assert.AreEqual(bodyHeight, expandedHeight, 1.5);
        Assert.IsTrue(expandedHeight > inlineHeight, $"The frame fills the full-screen panel: {inlineHeight} -> {expandedHeight}.");

        await ExpandButton("atv-full-html").ClickAsync();
        await Expect(Root("atv-full-html")).Not.ToHaveClassAsync(Expanded);
        Assert.IsTrue(await frame.EvaluateAsync<bool>("f => f.__e2eKept === true"));
        await Expect(page.Locator("#counter")).ToHaveTextAsync("Clicked 2 times");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheActions_AreUnavailable()
    {
        await OpenAsync(Route);

        await Expect(Root("atv-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(DownloadButton("atv-disabled")).ToBeDisabledAsync();
        await Expect(DownloadButton("atv-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(ExpandButton("atv-disabled")).ToBeDisabledAsync();
        await Expect(VersionDropdown("atv-disabled")).ToHaveClassAsync(HasClass("bit-dis"));

        // A click that still reaches the toggle (a screen reader's) does not expand it.
        await ExpandButton("atv-disabled").EvaluateAsync("b => { b.disabled = false; b.click(); }");
        await NextFramesAsync();
        await Expect(Root("atv-disabled")).Not.ToHaveClassAsync(Expanded);

        // The content is still there to read.
        await Expect(Body("atv-disabled").Locator("code")).ToContainTextAsync("quantity >= 10");
    }

    // ---------- RTL ----------

    [TestMethod]
    [DataRow("atv-rtl", true)]
    [DataRow("atv-rtl-inherited", false)]
    public async Task Rtl_TheHeader_ReadsRightToLeft(string id, bool explicitDir)
    {
        await OpenAsync(Route);

        if (explicitDir)
        {
            await Expect(Root(id)).ToHaveAttributeAsync("dir", "rtl");
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-rtl"));
        }
        else
        {
            await Expect(Root(id)).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        }

        await Expect(Panel(id)).ToHaveCSSAsync("direction", "rtl");

        var icon = await Panel(id).Locator(".bit-aiatv-ico").RectAsync();
        var title = await Panel(id).Locator(".bit-aiatv-ttl").RectAsync();
        var expand = await ExpandButton(id).RectAsync();
        var panel = await Panel(id).RectAsync();

        // The glyph of the kind starts the row on the right, the full-screen toggle ends it on the left.
        Assert.IsTrue(icon.Left >= title.Right - 1, "The glyph is to the right of the title.");
        Assert.IsTrue(expand.Right <= title.Left + 1, "The toggle is to the left of the title.");
        Assert.IsTrue(panel.Right - icon.Right < icon.Left - panel.Left, "The glyph sits on the right edge.");
    }

    // ---------- public variables ----------

    [TestMethod]
    public async Task Variables_OnTheInstance_RestyleThePanel()
    {
        await OpenAsync(Route);

        var panel = Panel("atv-vars");
        await Expect(panel).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(panel).ToHaveCSSAsync("color", "rgb(250, 251, 252)");
        await Expect(panel).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        await Expect(panel).ToHaveCSSAsync("border-top-left-radius", "3px");

        var header = panel.Locator(".bit-aiatv-hdr");
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(header).ToHaveCSSAsync("padding", "5px 7px");
        await Expect(header).ToHaveCSSAsync("column-gap", "11px");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(13, 14, 15)");
        await Expect(panel.Locator(".bit-aiatv-ico")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(panel.Locator(".bit-aiatv-ttl")).ToHaveCSSAsync("font-size", "21px");
        await Expect(panel.Locator(".bit-aiatv-ttl")).ToHaveCSSAsync("font-weight", "300");

        // The body scrolls past the height the variable gives it.
        var body = Body("atv-vars");
        await Expect(body).ToHaveCSSAsync("max-height", "60px");
        var scroll = await body.ScrollStateAsync();
        Assert.AreEqual(60, scroll.ClientHeight, 1);
        Assert.IsTrue(scroll.Height > scroll.ClientHeight, "The body scrolls.");

        // Expanded: the inset around the panel and the overlay behind it.
        await ExpandButton("atv-vars").ClickAsync();
        await Expect(Root("atv-vars")).ToHaveClassAsync(Expanded);
        var (left, top, _, _) = await panel.RectAsync();
        Assert.AreEqual(40, left, 0.5);
        Assert.AreEqual(40, top, 0.5);
        await Expect(Root("atv-vars").Locator(":scope > .bit-aiatv-ovl")).ToHaveCSSAsync("background-color", "rgba(255, 0, 0, 0.5)");
        await Expect(body).ToHaveCSSAsync("max-height", "none");
    }

    [TestMethod]
    public async Task Variables_OnAnAncestor_ReachThePanelsInside()
    {
        await OpenAsync(Route);

        var frame = Body("atv-vars-html").Locator("iframe");
        await Expect(frame).ToHaveCSSAsync("height", "77px");
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(200, 201, 202)");

        await Expect(Body("atv-vars-doc").Locator(".bit-aiatv-doc")).ToHaveCSSAsync("padding", "9px");

        var header = Body("atv-vars-csv").Locator("th").First;
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(30, 31, 32)");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(40, 41, 42)");

        // The variables were set for the panels inside: the one outside keeps its defaults.
        await Expect(Body("atv-html").Locator("iframe")).Not.ToHaveCSSAsync("height", "77px");
    }

    // ---------- helpers ----------

    private async Task<ILocator> OpenVersionsAsync(string id)
    {
        var field = VersionDropdown(id).Locator(":scope > .bit-drp-wrp");
        var fieldId = await field.GetAttributeAsync("id");
        Assert.IsFalse(string.IsNullOrEmpty(fieldId), "The field of the version dropdown carries an id.");

        await field.ClickAsync();

        var callout = Page.Locator($"[id='{fieldId}-callout']");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");

        return callout;
    }

    // The width and height of the viewport a fixed element is laid out in, less any scrollbar.
    private Task<double[]> ViewportAsync() =>
        Page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, document.documentElement.clientHeight]");

    private Task<string[]> CopiedAsync() => Page.EvaluateAsync<string[]>("() => window.__atvCopied ?? []");

    // Every Escape that reaches the window, and whether it was claimed (its default prevented) by then.
    private Task RecordEscapesAsync() =>
        Page.EvaluateAsync("() => { window.__atvEscapes = []; window.addEventListener('keydown', e => { if (e.key === 'Escape') window.__atvEscapes.push(e.defaultPrevented); }); }");

    private Task WaitForEscapesAsync(int count) =>
        Page.WaitForFunctionAsync("n => window.__atvEscapes.length >= n", count);

    private Task<bool[]> EscapesAsync() => Page.EvaluateAsync<bool[]>("() => window.__atvEscapes");
}
