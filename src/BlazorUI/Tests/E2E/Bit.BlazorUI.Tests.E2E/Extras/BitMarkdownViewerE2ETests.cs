using System.Threading;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitMarkdownViewer on <c>/e2e/markdown-viewer</c>: the CommonMark core and every flavor of the
/// pipelines (GitHub tables, strikethrough, task lists, autolinks, footnotes, alerts; emoji, emphasis extras,
/// SmartyPants, soft breaks, front matter, containers, definition lists, abbreviations, figures, math, auto ids and
/// permalinks, link options, base URL and URL rewriters, localized texts), the untrusted-input guards (image policy,
/// URL sanitizing, raw HTML as text, bidi stripping, length and nesting caps), the templates, the interactive task
/// lists, the accessibility attributes, the BitParams cascade, the public CSS variables, RTL and Auto direction, the
/// base parameters, prerendering, runtime re-parses and unmounting.
/// </summary>
[TestClass]
public class BitMarkdownViewerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/markdown-viewer";

    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    [TestMethod]
    public async Task Prerender_TheServerHtmlHoldsTheRenderedDocument_BeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.AreEqual(200, response.Status);
        StringAssert.Contains(html, "<div id=\"mdv-basic\" class=\"bit-mdv\"><h1>Heading one</h1>");
        StringAssert.Contains(html, "<del>strikethrough</del>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);
        await Expect(Page.Locator("#mdv-basic h1")).ToHaveTextAsync("Heading one");
    }

    [TestMethod]
    public async Task Basic_CoreBlocks_RenderAsRealElements()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-basic");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mdv"));

        for (var level = 1; level <= 6; level++)
        {
            await Expect(root.Locator($"h{level}")).ToHaveCountAsync(1);
        }
        await Expect(root.Locator("h3")).ToHaveTextAsync("Heading three");
        await Expect(root.Locator("strong").First).ToHaveTextAsync("strong");
        await Expect(root.Locator("em").First).ToHaveTextAsync("emphasis");
        await Expect(root.Locator("p > code").First).ToHaveTextAsync("inline code");

        var referenceLink = root.Locator("a[title='bit platform']");
        await Expect(referenceLink).ToHaveAttributeAsync("href", "https://bitplatform.dev");
        var inlineLink = root.Locator("a[title='Inline title']");
        await Expect(inlineLink).ToHaveAttributeAsync("href", "https://example.com/inline");

        await Expect(root.Locator("ul > li")).ToHaveCountAsync(2);
        await Expect(root.Locator("ul > li ol > li")).ToHaveTextAsync(["Nested one", "Nested two"]);
        await Expect(root.Locator("ol[start='3'] > li")).ToHaveTextAsync(["three", "four"]);

        await Expect(root.Locator("blockquote p")).ToContainTextAsync("© 2026 — but &copy; stays literal in code.");

        var fenced = root.Locator("pre.language-csharp");
        await Expect(fenced).ToHaveAttributeAsync("tabindex", "0");
        await Expect(fenced).ToHaveAttributeAsync("role", "region");
        await Expect(fenced).ToHaveAttributeAsync("aria-label", "Code block");
        await Expect(fenced.Locator("code.language-csharp")).ToHaveTextAsync("var html = \"no innerHTML\";");
        await Expect(root.Locator("pre:not([class]) code")).ToHaveTextAsync("indented code");

        await Expect(root.Locator("br")).ToHaveCountAsync(1);
        await Expect(root.Locator("hr")).ToHaveCountAsync(1);

        var image = root.Locator("img");
        await Expect(image).ToHaveAttributeAsync("alt", "Sample image");
        await Expect(image).ToHaveAttributeAsync("title", "Sample title");
        await Expect(image).ToHaveAttributeAsync("referrerpolicy", "no-referrer");
        await Expect(image).ToHaveAttributeAsync("loading", "lazy");
        await image.ScrollIntoViewIfNeededAsync();
        await Expect(image).ToHaveJSPropertyAsync("naturalWidth", 40);
    }

    [TestMethod]
    public async Task Basic_TwoTrailingSpaces_BreakTheLine()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-hard-break p br")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#mdv-hard-break p")).ToHaveTextAsync(new Regex("^Two spaces\\s*break here$"));
    }

    [TestMethod]
    public async Task Basic_HeadingRamp_ScalesWithTheDocumentFontSize()
    {
        await OpenAsync(Route);

        var expected = new Dictionary<string, double> { ["h1"] = 2, ["h2"] = 1.5, ["h3"] = 1.25, ["h4"] = 1, ["h5"] = 0.875, ["h6"] = 0.85 };
        var rootSize = ParsePx(await ComputedStyleAsync("#mdv-basic", "font-size"));

        foreach (var (tag, ratio) in expected)
        {
            var size = ParsePx(await ComputedStyleAsync($"#mdv-basic {tag}", "font-size"));
            Assert.AreEqual(rootSize * ratio, size, 0.5, $"{tag} font-size");
        }

        // h1 and h2 carry the rule under them, h3 does not.
        Assert.AreEqual("1px", await ComputedStyleAsync("#mdv-basic h1", "border-bottom-width"));
        Assert.AreEqual("1px", await ComputedStyleAsync("#mdv-basic h2", "border-bottom-width"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#mdv-basic h3", "border-bottom-width"));
    }

    [TestMethod]
    public async Task Untrusted_RawHtmlAndScripts_RenderAsTextAndNeverRun()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-basic")).ToContainTextAsync("Raw <b>HTML</b> and <script>window.__mdvInjected = true;</script> are text.");
        await Expect(Page.Locator("#mdv-basic b")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-basic script")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-untrusted script")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-untrusted img[src='x']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-untrusted")).ToContainTextAsync("<img src=x onerror=alert(1)>");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__mdvInjected === undefined"));

        // Unsafe destinations keep their text and lose the destination.
        foreach (var (viewer, text) in new[] { ("#mdv-basic", "a javascript link"), ("#mdv-basic", "a data link"), ("#mdv-untrusted", "a javascript link"), ("#mdv-untrusted", "vbscript") })
        {
            var link = Page.Locator($"{viewer} a", new() { HasTextString = text });
            await Expect(link).ToHaveCountAsync(1);
            Assert.IsNull(await link.GetAttributeAsync("href"), $"{text} kept its destination");
        }
    }

    [TestMethod]
    public async Task GitHub_Tables_AlignColumnsAndStripeRows()
    {
        await OpenAsync(Route);

        var wrapper = Page.Locator("#mdv-github .bit-mdv-table-wrapper");
        await Expect(wrapper).ToHaveAttributeAsync("role", "region");
        await Expect(wrapper).ToHaveAttributeAsync("aria-label", "Table");
        await Expect(wrapper).ToHaveAttributeAsync("tabindex", "0");

        await Expect(wrapper.Locator("th")).ToHaveTextAsync(["Left", "Center", "Right", "None"]);
        await Expect(wrapper.Locator("th").Nth(0)).ToHaveAttributeAsync("scope", "col");

        var expected = new[] { ("bit-mdv-align-left", "start"), ("bit-mdv-align-center", "center"), ("bit-mdv-align-right", "end") };
        for (var i = 0; i < expected.Length; i++)
        {
            var (cls, align) = expected[i];
            await Expect(wrapper.Locator("th").Nth(i)).ToHaveClassAsync(HasClass(cls));
            await Expect(wrapper.Locator("tbody tr").First.Locator("td").Nth(i)).ToHaveCSSAsync("text-align", align);
        }

        // The center cell's text really sits in the middle of its cell, the right cell's at its end.
        var centered = await wrapper.Locator("tbody tr").First.Locator("td").Nth(1).EvaluateAsync<double[]>(
            "td => { const r = document.createRange(); r.selectNodeContents(td); const t = r.getBoundingClientRect(); const c = td.getBoundingClientRect(); return [t.left - c.left, c.right - t.right]; }");
        Assert.AreEqual(centered[0], centered[1], 2, "the centered text has the same room on both sides");

        var odd = await ComputedStyleAsync("#mdv-github tbody tr:nth-child(1)", "background-color");
        var even = await ComputedStyleAsync("#mdv-github tbody tr:nth-child(2)", "background-color");
        Assert.AreNotEqual(odd, even, "every second body row is striped");
    }

    [TestMethod]
    public async Task GitHub_StrikethroughAndAutolinks_RenderAsElements()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-github");
        await Expect(root.Locator("del")).ToHaveTextAsync("strikethrough");
        await Expect(root.Locator("a", new() { HasTextString = "https://bitplatform.dev" })).ToHaveAttributeAsync("href", "https://bitplatform.dev");
        await Expect(root.Locator("a", new() { HasTextString = "www.example.com" })).ToHaveAttributeAsync("href", "http://www.example.com");
        await Expect(root.Locator("a", new() { HasTextString = "mail@example.com" })).ToHaveAttributeAsync("href", "mailto:mail@example.com");

        // The same source without the flavors keeps every marker as text.
        var basic = Page.Locator("#mdv-github-basic");
        await Expect(basic.Locator("del, table, a, .markdown-alert, .footnotes, input")).ToHaveCountAsync(0);
        await Expect(basic).ToContainTextAsync("~~strikethrough~~");
        await Expect(basic).ToContainTextAsync("[!NOTE]");
        await Expect(basic.Locator("blockquote")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task GitHub_TaskLists_WithNoHandler_AreReadOnlyBoxes()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#mdv-github ul.contains-task-list");
        await Expect(list.Locator("li.task-list-item")).ToHaveCountAsync(2);
        await Expect(list.Locator("li.task-list-item").First).ToHaveCSSAsync("list-style-type", "none");

        var boxes = list.Locator("input.task-list-item-checkbox");
        await Expect(boxes.Nth(0)).ToBeCheckedAsync();
        await Expect(boxes.Nth(1)).Not.ToBeCheckedAsync();
        for (var i = 0; i < 2; i++)
        {
            await Expect(boxes.Nth(i)).ToBeDisabledAsync();
            await Expect(boxes.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await boxes.Nth(1).ClickAsync(new() { Force = true });
        await Expect(boxes.Nth(1)).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task GitHub_Footnotes_LinkBothWaysAndAreScopedToTheViewer()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-github");
        var refs = root.Locator("sup.footnote-ref a");
        await Expect(refs).ToHaveTextAsync(["1", "1", "2"]);

        var firstHref = await refs.Nth(0).GetAttributeAsync("href");
        StringAssert.Matches(firstHref, new Regex("^/e2e/markdown-viewer#[a-z0-9]+-fn-1$"));
        var scope = Regex.Match(firstHref!, "#([a-z0-9]+)-fn-1").Groups[1].Value;

        var section = root.Locator("section.footnotes");
        await Expect(section).ToHaveAttributeAsync("aria-labelledby", $"{scope}-footnotes-label");
        await Expect(refs.Nth(0)).ToHaveAttributeAsync("aria-describedby", $"{scope}-footnotes-label");

        // The heading is there for a screen reader only.
        var heading = section.Locator($"#{scope}-footnotes-label");
        await Expect(heading).ToHaveTextAsync("Footnotes");
        var box = await heading.BoundingBoxAsync();
        Assert.IsNotNull(box);
        Assert.IsTrue(box.Width <= 1 && box.Height <= 1, "the footnotes heading is visually hidden");

        var backrefs = section.Locator("a.footnote-backref");
        await Expect(backrefs).ToHaveCountAsync(3);
        await Expect(backrefs.Nth(0)).ToHaveAttributeAsync("aria-label", "Back to reference 1-1");
        await Expect(backrefs.Nth(1)).ToHaveAttributeAsync("aria-label", "Back to reference 1-2");
        await Expect(backrefs.Nth(2)).ToHaveAttributeAsync("aria-label", "Back to reference 2");

        // Following a reference lands on its footnote, and the back-link lands on the citation again.
        await refs.Nth(2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"#{scope}-fn-2$"));
        await Expect(section.Locator($"#{scope}-fn-2")).ToBeInViewportAsync();

        await backrefs.Nth(2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"#{scope}-fnref-2$"));
        await Expect(refs.Nth(2)).ToBeInViewportAsync();

        // A second viewer with footnotes on the page uses another scope.
        var localizedHref = await Page.Locator("#mdv-localized sup.footnote-ref a").GetAttributeAsync("href");
        Assert.IsFalse(localizedHref!.Contains($"#{scope}-"), "two viewers share footnote ids");
    }

    [TestMethod]
    [DataRow("note", "Note")]
    [DataRow("tip", "Tip")]
    [DataRow("important", "Important")]
    [DataRow("warning", "Warning")]
    [DataRow("caution", "Caution")]
    public async Task GitHub_Alerts_EachKindIsTitledAndColored(string kind, string title)
    {
        await OpenAsync(Route);

        var alert = Page.Locator($"#mdv-github .markdown-alert.markdown-alert-{kind}");
        await Expect(alert).ToHaveCountAsync(1);
        await Expect(alert.Locator(".markdown-alert-title")).ToHaveTextAsync(title);
        await Expect(alert.Locator(".markdown-alert-title svg.bit-mdv-alert-icon")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(alert.Locator("p").Nth(1)).ToHaveTextAsync($"{title} body.");

        // The bar and the title take the kind's own color, unlike a plain block quote's bar.
        var bar = await alert.EvaluateAsync<string>("e => getComputedStyle(e).borderLeftColor");
        var titleColor = await alert.Locator(".markdown-alert-title").EvaluateAsync<string>("e => getComputedStyle(e).color");
        Assert.AreEqual(bar, titleColor);
        Assert.AreEqual("2px", await alert.EvaluateAsync<string>("e => getComputedStyle(e).borderLeftWidth"));

        var others = await Page.Locator("#mdv-github .markdown-alert").EvaluateAllAsync<string[]>("els => els.map(e => getComputedStyle(e).borderLeftColor)");
        Assert.AreEqual(5, others.Distinct().Count(), "the five kinds have five colors");
    }

    [TestMethod]
    public async Task CustomPipeline_OnlyTheChosenFlavorsApply()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-custom");
        await Expect(root.Locator("h1")).ToHaveTextAsync("Custom pipeline ✨");
        await Expect(root.Locator("table")).ToHaveCountAsync(1);
        await Expect(root.Locator("del")).ToHaveTextAsync("Old");
        await Expect(root.Locator("input.task-list-item-checkbox")).ToHaveCountAsync(2);
        await Expect(root.Locator("a")).ToHaveCountAsync(0);
        await Expect(root).ToContainTextAsync("so https://bitplatform.dev stays plain text");

        // UseEmojis(overrides) extends the map and replaces a built-in shortcode of the same name.
        await Expect(Page.Locator("#mdv-emoji-override p")).ToHaveTextAsync("[ROCKET] and [BIT]");
    }

    [TestMethod]
    public async Task Typography_EmphasisExtrasAndSmartyPants_RenderRealElementsAndCurlyText()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-typography");
        await Expect(root.Locator("sub")).ToHaveTextAsync("2");
        await Expect(root.Locator("sup")).ToHaveTextAsync("2");
        await Expect(root.Locator("ins")).ToHaveTextAsync("adds streaming");
        await Expect(root.Locator("del")).ToHaveTextAsync("drops");
        await Expect(root.Locator("mark")).ToHaveTextAsync("read this");

        await Expect(root.Locator("p").Nth(1)).ToHaveTextAsync("“Typography matters,” she said – and it’s hard… The 2024–2026 range; an aside — like this.");
        await Expect(root.Locator("code")).ToHaveTextAsync("--- \"not curled\" ...");

        var markBackground = await ComputedStyleAsync("#mdv-typography mark", "background-color");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", markBackground);
        await Expect(root.Locator("ins")).ToHaveCSSAsync("text-decoration-line", "underline");
    }

    [TestMethod]
    public async Task LineBreaks_SoftLineAsHardLine_BreaksEveryNewline()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-soft br")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-hard br")).ToHaveCountAsync(1);

        var softHeight = (await Page.Locator("#mdv-soft p").BoundingBoxAsync())!.Height;
        var hardHeight = (await Page.Locator("#mdv-hard p").BoundingBoxAsync())!.Height;
        Assert.IsTrue(hardHeight > softHeight * 1.5, "the hard-broken paragraph takes two lines");
    }

    [TestMethod]
    public async Task FrontMatter_IsParsedOutOfTheDocumentAndReadThroughOnParsed()
    {
        await OpenAsync(Route);

        // Without the flavor, the block is a rule and a setext heading.
        await Expect(Page.Locator("#mdv-front-default hr")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#mdv-front-default h2")).ToContainTextAsync("title: Release notes");

        await Expect(Page.Locator("#mdv-front hr")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-front > *").First).ToHaveTextAsync("Release notes");
        await Expect(Page.Locator("#mdv-front")).Not.ToContainTextAsync("title:");

        await Expect(Page.Locator("#front-matter-text")).ToHaveTextAsync("---|False|title: Release notes | date: 2026-09-09");
        await Expect(Page.Locator("#front-matter-toml")).ToHaveTextAsync("+++|True|title = \"Toml notes\"");
        await Expect(Page.Locator("#mdv-front-toml")).ToHaveTextAsync("Toml body.");
    }

    [TestMethod]
    public async Task Containers_RenderTitledNestedBlocksAndARealDetails()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-containers");
        var tip = root.Locator(":scope > .markdown-container-tip");
        await Expect(tip.Locator(".markdown-container-title")).ToHaveTextAsync("Start here");

        var warning = root.Locator(":scope > .markdown-container-warning");
        await Expect(warning.Locator(":scope > .markdown-container-title")).ToHaveTextAsync("Read this first");
        await Expect(warning.Locator("strong")).ToHaveTextAsync("bold");
        await Expect(warning.Locator(":scope > .markdown-container-note")).ToHaveTextAsync("Nested note.");

        await Expect(root.Locator(":scope > .markdown-container-glossary")).ToHaveTextAsync("Unstyled body.");

        // The tip and warning are pre-styled with their kind's bar color; the glossary keeps the plain one.
        var tipBar = await tip.EvaluateAsync<string>("e => getComputedStyle(e).borderLeftColor");
        var warningBar = await warning.EvaluateAsync<string>("e => getComputedStyle(e).borderLeftColor");
        var glossaryBar = await root.Locator(":scope > .markdown-container-glossary").EvaluateAsync<string>("e => getComputedStyle(e).borderLeftColor");
        Assert.AreNotEqual(tipBar, warningBar);
        Assert.AreNotEqual(tipBar, glossaryBar);

        var details = root.Locator("details.markdown-container-details");
        await Expect(details.Locator("summary")).ToHaveTextAsync("How the fence is read");
        await Expect(details.Locator("p")).Not.ToBeVisibleAsync();
        await details.Locator("summary").ClickAsync();
        await Expect(details).ToHaveAttributeAsync("open", "");
        await Expect(details.Locator("p")).ToBeVisibleAsync();

        // A keyboard user closes it again.
        await details.Locator("summary").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(details.Locator("p")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Documentation_DefinitionListsAbbreviationsAndFigures()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-docs");
        await Expect(root.Locator("dl > dt")).ToHaveTextAsync(["Pipeline", "AST"]);
        await Expect(root.Locator("dl > dd")).ToHaveTextAsync(["The immutable set of flavors.", "Build it once.", "The tree the parser produces from the AST source."]);

        var abbreviations = root.Locator("abbr");
        await Expect(abbreviations).ToHaveCountAsync(2);
        await Expect(abbreviations.First).ToHaveAttributeAsync("title", "Abstract Syntax Tree");
        await Expect(root).Not.ToContainTextAsync("*[AST]");

        var figure = root.Locator("figure");
        await Expect(figure.Locator("img")).ToHaveAttributeAsync("alt", "The sample logo");
        await Expect(figure.Locator("figcaption")).ToHaveTextAsync("A captioned figure");
    }

    [TestMethod]
    public async Task Math_TexIsKeptVerbatimAndPricesAreLeftAlone()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-math");
        await Expect(root.Locator("span.math.math-inline")).ToHaveTextAsync("$e^{i\\pi} + 1 = 0$");
        await Expect(root.Locator("div.math.math-display")).ToHaveTextAsync("$$\\int_0^1 x^2 \\, dx$$");
        await Expect(root.Locator("div.math-display")).ToHaveCSSAsync("text-align", "center");
        await Expect(root.Locator("p").Last).ToHaveTextAsync("Prices are left alone: $5 and $10.");
        await Expect(root.Locator(".math")).ToHaveCountAsync(2);
    }

    [TestMethod]
    [DataRow("same", true, false)]
    [DataRow("none", false, false)]
    [DataRow("all", true, true)]
    public async Task Untrusted_ImageRendering_LoadsOnlyWhatThePolicyAllows(string policy, bool sameOriginLoads, bool crossOriginLoads)
    {
        var crossOriginRequests = 0;
        await Page.RouteAsync("https://images.e2e-cross-origin.test/**", async route =>
        {
            Interlocked.Increment(ref crossOriginRequests);
            await route.FulfillAsync(new() { Status = 200, ContentType = "image/png", BodyBytes = OnePixelPng });
        });

        await OpenAsync(Route);

        await Page.ClickAsync($"#btn-img-{policy}");
        await Expect(Page.Locator("#image-rendering")).ToHaveTextAsync(policy switch { "same" => "SameOrigin", "none" => "None", _ => "All" });

        var same = Page.Locator("#mdv-untrusted img[alt='same origin']");
        var cross = Page.Locator("#mdv-untrusted img[alt='cross origin']");
        await cross.ScrollIntoViewIfNeededAsync();

        if (sameOriginLoads)
        {
            await same.ScrollIntoViewIfNeededAsync();
            await Expect(same).ToHaveAttributeAsync("src", "/samples/mdv-sample.png");
            await Expect(same).ToHaveJSPropertyAsync("naturalWidth", 40);
        }
        else
        {
            await Expect(same).Not.ToHaveAttributeAsync("src", new Regex(".*"));
        }

        if (crossOriginLoads)
        {
            await Expect(cross).ToHaveAttributeAsync("src", "https://images.e2e-cross-origin.test/remote.png");
            await Expect(cross).ToHaveJSPropertyAsync("naturalWidth", 1);
            Assert.IsTrue(crossOriginRequests > 0);
        }
        else
        {
            await Expect(cross).Not.ToHaveAttributeAsync("src", new Regex(".*"));
            await NextFramesAsync();
            Assert.AreEqual(0, crossOriginRequests, "a blocked image was still fetched");
        }

        // A blocked image keeps its alt text.
        await Expect(cross).ToHaveAttributeAsync("alt", "cross origin");
    }

    [TestMethod]
    public async Task Untrusted_StripBidiControlCharacters_RemovesTheOverrides()
    {
        await OpenAsync(Route);

        var kept = await Page.Locator("#mdv-bidi-kept p").TextContentAsync();
        var stripped = await Page.Locator("#mdv-bidi-stripped p").TextContentAsync();

        Assert.AreEqual("abc‮def‬ghi", kept);
        Assert.AreEqual("abcdefghi", stripped);
    }

    [TestMethod]
    public async Task Untrusted_MaxLength_TruncatesTheSourceBeforeParsing_NeverInsideASurrogatePair()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-max-length p")).ToHaveTextAsync("0123456789**");
        await Expect(Page.Locator("#mdv-max-length strong")).ToHaveCountAsync(0);

        var surrogate = await Page.Locator("#mdv-max-length-surrogate p").TextContentAsync();
        Assert.AreEqual("ab", surrogate, "the lone high surrogate was kept");
    }

    [TestMethod]
    public async Task Untrusted_MaxNestingDepth_RendersDeeperContentAsText()
    {
        await OpenAsync(Route);

        var limited = await Page.Locator("#mdv-depth blockquote").CountAsync();
        var unlimited = await Page.Locator("#mdv-depth-default blockquote").CountAsync();

        Assert.AreEqual(6, unlimited);
        Assert.IsTrue(limited < unlimited, $"MaxNestingDepth=3 still nested {limited} quotes");
        await Expect(Page.Locator("#mdv-depth p")).ToContainTextAsync("> deep quote");
        await Expect(Page.Locator("#mdv-depth-default p")).ToHaveTextAsync("deep quote");
    }

    [TestMethod]
    public async Task Toc_OnParsed_ReadsTheHeadingsAndThePlainText()
    {
        await OpenAsync(Route);

        var items = Page.Locator("#toc-nav a.toc-item");
        await Expect(items).ToHaveTextAsync(["Installation", ".NET CLI", "Release notes", "Added"]);
        await Expect(items.Nth(0)).ToHaveAttributeAsync("href", "#install");
        await Expect(items.Nth(1)).ToHaveAttributeAsync("data-level", "3");

        // ToPlainText keeps what the document says, without markup or destinations.
        var excerpt = await Page.Locator("#toc-excerpt").TextContentAsync();
        StringAssert.Contains(excerpt, "See Installation first and elsewhere.");
        Assert.IsFalse(excerpt!.Contains('#'), "the excerpt carries a destination or a permalink");
    }

    [TestMethod]
    public async Task Toc_AutoIdentifiers_GiveUniqueSlugsAndPinnedIds()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-toc h2").First).ToHaveAttributeAsync("id", "install");
        await Expect(Page.Locator("#mdv-toc h3").First).ToHaveAttributeAsync("id", "net-cli");
        await Expect(Page.Locator("#mdv-toc h2").First).Not.ToContainTextAsync("{#install}");

        await Expect(Page.Locator("#mdv-auto-ids h1")).ToHaveAttributeAsync("id", "hello-world");
        await Expect(Page.Locator("#mdv-auto-ids h2")).ToHaveAttributeAsync("id", "hello-world-1");
        // Without anchorLinks, no permalink is drawn.
        await Expect(Page.Locator("#mdv-auto-ids .bit-mdv-anchor")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Toc_Permalinks_AppearOnHoverAndOnKeyboardFocus()
    {
        await OpenAsync(Route);

        var heading = Page.Locator("#mdv-toc h2#install");
        var anchor = heading.Locator("a.bit-mdv-anchor");
        await Expect(anchor).ToHaveAttributeAsync("aria-label", "Permalink to Installation");
        await Expect(anchor).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#install");
        await Expect(anchor.Locator("span")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(anchor).ToHaveCSSAsync("opacity", "0");

        await heading.HoverAsync();
        await Expect(anchor).ToHaveCSSAsync("opacity", "1");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(anchor).ToHaveCSSAsync("opacity", "0");

        // Tabbing onto the permalink shows it as well.
        await Page.Locator("#toc-nav a.toc-item").Last.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(anchor).ToBeFocusedAsync();
        await Expect(anchor).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task Toc_InPageLink_ScrollsToTheHeading_KeepingTheScrollMargin()
    {
        await OpenAsync(Route);

        var heading = Page.Locator("#mdv-toc h2#install");
        await Expect(heading).ToHaveCSSAsync("scroll-margin-block-start", "40px");

        var link = Page.Locator("#mdv-toc p a", new() { HasTextString = "Installation" });
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#install");
        await link.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/markdown-viewer#install$"));
        await Page.WaitForFunctionAsync("() => Math.abs(document.getElementById('install').getBoundingClientRect().top - 40) < 3");

        // A link to an id the document does not declare is left as it is.
        await Expect(Page.Locator("#mdv-toc p a", new() { HasTextString = "elsewhere" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#not-a-heading");
    }

    [TestMethod]
    public async Task Links_Default_ExternalOpensANewTabAndSaysSo()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-links-default");
        var external = root.Locator("a[href='https://example.com/page']");
        await Expect(external).ToHaveAttributeAsync("target", "_blank");
        await Expect(external).ToHaveAttributeAsync("rel", "noopener noreferrer");
        await Expect(root.GetByRole(AriaRole.Link, new() { Name = "external link (opens in a new tab)", Exact = true })).ToHaveCountAsync(1);

        // The notice is read out, not shown.
        var notice = external.Locator(".bit-mdv-new-tab");
        var box = await notice.BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "the new-tab notice is visible");

        var internalLink = root.Locator("a", new() { HasTextString = "an internal one" });
        await Expect(internalLink).ToHaveAttributeAsync("href", "/e2e/other");
        Assert.IsNull(await internalLink.GetAttributeAsync("target"));

        // An in-page link is written against the page, so <base href="/"> cannot send it to the home page.
        await Expect(root.Locator("a", new() { HasTextString = "a fragment" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#frag");

        // The link color and its underline at rest.
        await Expect(internalLink).ToHaveCSSAsync("text-decoration-line", "underline");
    }

    [TestMethod]
    public async Task Links_UseLinkOptions_ReplacesTargetsAndRels()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-links-policy");
        var external = root.Locator("a[href='https://example.com/page']");
        Assert.IsNull(await external.GetAttributeAsync("target"), "a Self target writes no target");
        await Expect(external).ToHaveAttributeAsync("rel", "noopener noreferrer nofollow ugc");
        await Expect(external.Locator(".bit-mdv-new-tab")).ToHaveCountAsync(0);

        var internalLink = root.Locator("a[href='/e2e/other']");
        await Expect(internalLink).ToHaveAttributeAsync("target", "_top");
        await Expect(internalLink).ToHaveAttributeAsync("rel", "bookmark");
    }

    [TestMethod]
    public async Task Links_UseBaseUrl_ResolvesOnlyRelativeDestinations()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-base-url");
        var image = root.Locator("img");
        await Expect(image).ToHaveAttributeAsync("src", "/samples/mdv-sample.png");
        await image.ScrollIntoViewIfNeededAsync();
        await Expect(image).ToHaveJSPropertyAsync("naturalWidth", 40);

        await Expect(root.Locator("a", new() { HasTextString = "relative link" })).ToHaveAttributeAsync("href", "/samples/docs/readme.md");
        await Expect(root.Locator("a", new() { HasTextString = "absolute" })).ToHaveAttributeAsync("href", "https://example.com/x");
        await Expect(root.Locator("a", new() { HasTextString = "root" })).ToHaveAttributeAsync("href", "/root.md");
        await Expect(root.Locator("a", new() { HasTextString = "fragment" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#f");
    }

    [TestMethod]
    public async Task Links_UseUrlRewriter_RewritesDropsAndReSanitizes()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-rewriter");
        await Expect(root.Locator("a", new() { HasTextString = "to rewrite" })).ToHaveAttributeAsync("href", "https://new.example.com/a");
        Assert.IsNull(await root.Locator("a", new() { HasTextString = "to drop" }).GetAttributeAsync("href"), "a null rewrite kept the destination");
        Assert.IsNull(await root.Locator("a", new() { HasTextString = "unsafe" }).GetAttributeAsync("href"), "the rewriter reintroduced an unsafe destination");

        // The image was rewritten to another cross-origin host, which the SameOrigin policy then blocks.
        await Expect(root.Locator("img[alt='img']")).Not.ToHaveAttributeAsync("src", new Regex(".*"));
    }

    [TestMethod]
    public async Task Inline_RendersASpanWithoutParagraphs_AndADivForOtherBlocks()
    {
        await OpenAsync(Route);

        var inline = Page.Locator("#mdv-inline");
        Assert.AreEqual("SPAN", await inline.EvaluateAsync<string>("e => e.tagName"));
        await Expect(inline).ToHaveClassAsync(HasClass("bit-mdv-inline"));
        await Expect(inline.Locator("p")).ToHaveCountAsync(0);
        await Expect(inline).ToHaveTextAsync("the fastest path is Span<T>");
        await Expect(inline).ToHaveCSSAsync("display", "inline");
        await Expect(Page.Locator("#inline-host")).ToHaveTextAsync("Formatting a value in place: the fastest path is Span<T> - end.");

        // The viewer sits in the host's line rather than a line of its own.
        var hostBox = await Page.Locator("#inline-host").BoundingBoxAsync();
        var viewerBox = await inline.BoundingBoxAsync();
        Assert.IsTrue(hostBox!.Height < viewerBox!.Height * 1.8, "the inline viewer broke the host's line");
        Assert.IsTrue(viewerBox.X > hostBox.X + 50, "the inline viewer starts a line of its own");

        var withList = Page.Locator("#mdv-inline-list");
        Assert.AreEqual("DIV", await withList.EvaluateAsync<string>("e => e.tagName"));
        await Expect(withList).ToHaveClassAsync(HasClass("bit-mdv-inline"));
        await Expect(withList).ToHaveCSSAsync("display", "inline");
        await Expect(withList.Locator("li")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Templates_DrawCodeBlocksLinksAndImages_WithTheirOwnMarkup()
    {
        await Page.RouteAsync("https://images.e2e-cross-origin.test/**", route => route.FulfillAsync(new() { Status = 200, ContentType = "image/png", BodyBytes = OnePixelPng }));
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-templates");
        var wrappers = root.Locator(".bit-mdv-tpl");
        await Expect(wrappers).ToHaveCountAsync(4);
        await Expect(wrappers.First).ToHaveCSSAsync("display", "contents");

        var blocks = root.Locator(".tpl-code");
        await Expect(blocks).ToHaveCountAsync(2);
        await Expect(blocks.Nth(0)).ToHaveAttributeAsync("data-lang", "csharp");
        await Expect(blocks.Nth(1)).ToHaveAttributeAsync("data-lang", "text");
        await Expect(root.Locator("pre[role='region']")).ToHaveCountAsync(0);

        // The template's own handlers work inside the document.
        await blocks.Nth(0).Locator(".tpl-copy").ClickAsync();
        await Expect(Page.Locator("#copied-code")).ToHaveTextAsync("var x = 1;");

        var link = root.Locator("a.tpl-link");
        await Expect(link).ToHaveTextAsync("[templated link]");
        await Expect(link).ToHaveAttributeAsync("href", "https://bitplatform.dev");
        await Expect(link).ToHaveAttributeAsync("data-title", "Link title");

        await Expect(root.Locator("figure.tpl-image")).ToHaveAttributeAsync("data-url", "https://images.e2e-cross-origin.test/t.png");
        await Expect(root.Locator("figure.tpl-image")).ToHaveTextAsync("templated alt");

        // The image policy runs before the template: a blocked image reaches it with an empty Url.
        await Expect(Page.Locator("#mdv-templates-blocked figure.tpl-image")).ToHaveAttributeAsync("data-url", "");
        await Expect(Page.Locator("#mdv-templates-blocked figure.tpl-image")).ToHaveTextAsync("blocked alt");
    }

    [TestMethod]
    public async Task Tasks_TickingABox_RaisesOnTaskChangedWithTheRewrittenSource()
    {
        await OpenAsync(Route);

        var boxes = Page.Locator("#mdv-tasks input.task-list-item-checkbox");
        await Expect(boxes).ToHaveCountAsync(5);
        await Expect(boxes.Nth(1)).ToHaveAttributeAsync("aria-label", "Write the docs");
        await Expect(boxes.Nth(1)).ToBeEnabledAsync();
        Assert.IsNull(await boxes.Nth(1).GetAttributeAsync("aria-disabled"));

        await boxes.Nth(1).ClickAsync();
        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("1:True");
        await Expect(Page.Locator("#task-markdown")).ToContainTextAsync("- [x] Write the docs");
        await Expect(boxes.Nth(1)).ToBeCheckedAsync();

        await boxes.Nth(0).ClickAsync();
        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("0:False");
        await Expect(Page.Locator("#task-markdown")).ToContainTextAsync("- [ ] Write the parser");
        await Expect(boxes.Nth(0)).Not.ToBeCheckedAsync();

        // A nested task, and the task after a code block holding a task marker, are counted in reading order.
        await boxes.Nth(3).ClickAsync();
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("3:True");
        await Expect(Page.Locator("#task-markdown")).ToContainTextAsync("- [x] Icons");

        await boxes.Nth(4).ClickAsync();
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("4:True");
        await Expect(Page.Locator("#task-markdown")).ToContainTextAsync("- [x] After code");
        await Expect(Page.Locator("#task-markdown")).ToContainTextAsync("- [ ] not a task");
        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Tasks_Keyboard_SpaceTogglesTheFocusedBox()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#mdv-tasks input.task-list-item-checkbox").Nth(2);
        await box.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("2:True");
        await Expect(box).ToBeCheckedAsync();
        await Expect(box).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Tasks_Disabled_KeepsTheBoxesReadOnly_AndEnablingRestoresThem()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-tasks");
        var boxes = root.Locator("input.task-list-item-checkbox");

        await Page.ClickAsync("#btn-tasks-disable");
        await Expect(Page.Locator("#tasks-disabled")).ToHaveTextAsync("True");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 5; i++)
        {
            await Expect(boxes.Nth(i)).ToBeDisabledAsync();
            await Expect(boxes.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await boxes.Nth(1).ClickAsync(new() { Force = true });
        await Expect(boxes.Nth(1)).Not.ToBeCheckedAsync();
        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-tasks-disable");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(boxes.Nth(1)).ToBeEnabledAsync();
        await boxes.Nth(1).ClickAsync();
        await Expect(Page.Locator("#task-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Tasks_AnItemWithNoText_IsNamedByItsNumber()
    {
        await OpenAsync(Route);

        var boxes = Page.Locator("#mdv-tasks-empty input.task-list-item-checkbox");
        await Expect(boxes.Nth(0)).ToHaveAttributeAsync("aria-label", "Task 1");
        await Expect(boxes.Nth(1)).ToHaveAttributeAsync("aria-label", "named");
        await Expect(Page.Locator("#mdv-tasks-empty").GetByRole(AriaRole.Checkbox, new() { Name = "Task 1" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Localization_UseTexts_ReplacesEveryWordTheRenderersWrite()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-localized");
        await Expect(root.Locator(".markdown-alert-title")).ToHaveTextAsync(["Warnung", "Hinweis", "Tipp", "Wichtig", "Vorsicht"]);
        await Expect(root.Locator(".bit-mdv-table-wrapper")).ToHaveAttributeAsync("aria-label", "Tabelle");
        await Expect(root.Locator(".bit-mdv-new-tab")).ToHaveTextAsync(" (öffnet in neuem Tab)");
        await Expect(root.Locator("section.footnotes h2")).ToHaveTextAsync("Fußnoten");
        await Expect(root.Locator("a.footnote-backref")).ToHaveAttributeAsync("aria-label", "Zurück zur Referenz 1");
        await Expect(root.GetByRole(AriaRole.Link, new() { Name = "externe Link (öffnet in neuem Tab)", Exact = true })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelHeadingOffsetAndIdPrefix()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-a11y");
        await Expect(root).ToHaveAttributeAsync("role", "region");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Comment by Sam");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Comment by Sam" })).ToHaveCountAsync(1);

        // # renders as h4 under the page's h3; ## as h5.
        await Expect(root.Locator("h4")).ToHaveTextAsync(new Regex("^Looks good to me"));
        await Expect(root.Locator("h5")).ToHaveTextAsync(new Regex("^Naming"));
        await Expect(root.Locator("h1, h2, h3")).ToHaveCountAsync(0);

        await Expect(root.Locator("h4")).ToHaveAttributeAsync("id", "comment-42-looks-good-to-me");
        await Expect(root.Locator("h5")).ToHaveAttributeAsync("id", "comment-42-naming");
        await Expect(root.Locator("p a", new() { HasTextString = "Naming" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#comment-42-naming");

        // An unnamed viewer is no region.
        Assert.IsNull(await Page.Locator("#mdv-basic").GetAttributeAsync("role"));

        // The offset is clamped at h6.
        await Expect(Page.Locator("#mdv-offset-clamp h6")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Accessibility_AWideCodeBlock_IsAScrollableTabStop()
    {
        await OpenAsync(Route);

        var pre = Page.Locator("#mdv-wide-code pre");
        await Expect(pre).ToHaveAttributeAsync("tabindex", "0");
        Assert.IsTrue(await pre.EvaluateAsync<bool>("p => p.scrollWidth > p.clientWidth"), "the code block does not overflow");

        // Tab reaches it from the element before it, and the arrow keys scroll it.
        await Page.Locator("#mdv-offset-clamp").EvaluateAsync("e => { e.setAttribute('tabindex', '-1'); e.focus(); }");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(pre).ToBeFocusedAsync();

        for (var i = 0; i < 5; i++) await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForFunctionAsync("() => document.querySelector('#mdv-wide-code pre').scrollLeft > 0");
    }

    [TestMethod]
    public async Task Cascading_BitParams_SetDefaultsAndAViewersOwnValueWins()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#mdv-cascaded");
        await Expect(cascaded).ToHaveClassAsync(HasClass("cascaded-class"));
        await Expect(cascaded.Locator("h3")).ToHaveTextAsync("Release");
        await Expect(cascaded.Locator("del")).ToHaveTextAsync("Old");
        await Expect(cascaded.Locator("img")).Not.ToHaveAttributeAsync("src", new Regex(".*"));

        var own = Page.Locator("#mdv-cascaded-own");
        await Expect(own.Locator("h1")).ToHaveTextAsync("Release");
        await Expect(own.Locator("del")).ToHaveCountAsync(0);
        await Expect(own).ToContainTextAsync("~~Old~~");
        // The image policy was not set on this one, so the cascaded None still applies.
        await Expect(own.Locator("img")).Not.ToHaveAttributeAsync("src", new Regex(".*"));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-style")).ToHaveCSSAsync("border-left-color", "rgb(255, 0, 0)");
        await Expect(Page.Locator("#mdv-style")).ToHaveCSSAsync("padding-left", "16px");
        await Expect(Page.Locator("#mdv-class")).ToHaveClassAsync(HasClass("custom-mdv"));
        await Expect(Page.Locator("#mdv-class")).ToHaveClassAsync(HasClass("bit-mdv"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDocument()
    {
        await OpenAsync(Route);

        const string v = "#mdv-css-vars";
        var checks = new (string Selector, string Property, string Expected)[]
        {
            (v, "color", "rgb(1, 2, 3)"),
            (v, "font-size", "20px"),
            (v, "line-height", "40px"),
            ($"{v} > p", "margin-bottom", "30px"),
            ($"{v} h2", "color", "rgb(168, 85, 247)"),
            ($"{v} h2", "font-weight", "900"),
            ($"{v} h2", "border-bottom-color", "rgb(0, 128, 0)"),
            ($"{v} p a", "color", "rgb(10, 20, 30)"),
            ($"{v} p a", "text-decoration-line", "none"),
            ($"{v} p code", "color", "rgb(4, 5, 6)"),
            ($"{v} p code", "background-color", "rgb(240, 230, 220)"),
            ($"{v} p code", "border-top-left-radius", "7px"),
            ($"{v} pre", "color", "rgb(7, 8, 9)"),
            ($"{v} pre", "background-color", "rgb(220, 230, 240)"),
            ($"{v} pre", "padding-top", "21px"),
            ($"{v} pre", "border-top-left-radius", "9px"),
            ($"{v} > blockquote", "color", "rgb(11, 12, 13)"),
            ($"{v} > blockquote", "border-left-color", "rgb(14, 15, 16)"),
            ($"{v} td", "border-top-color", "rgb(17, 18, 19)"),
            ($"{v} th", "background-color", "rgb(20, 21, 22)"),
            ($"{v} tbody tr:nth-child(2)", "background-color", "rgb(23, 24, 25)"),
            ($"{v} td", "padding-top", "11px"),
            ($"{v} > hr", "border-top-color", "rgb(26, 27, 28)"),
            ($"{v} > hr", "border-top-width", "6px"),
            ($"{v} mark", "color", "rgb(29, 30, 31)"),
            ($"{v} mark", "background-color", "rgb(32, 33, 34)"),
            ($"{v} .markdown-container", "background-color", "rgb(35, 36, 37)"),
            ($"{v} .markdown-alert-note", "border-left-color", "rgb(40, 0, 0)"),
            ($"{v} .markdown-alert-tip", "border-left-color", "rgb(0, 40, 0)"),
            ($"{v} .markdown-alert-important", "border-left-color", "rgb(0, 0, 40)"),
            ($"{v} .markdown-alert-warning", "border-left-color", "rgb(40, 40, 0)"),
            ($"{v} .markdown-alert-caution", "border-left-color", "rgb(0, 40, 40)"),
            ($"{v} .markdown-alert-caution .markdown-alert-title", "color", "rgb(0, 40, 40)"),
        };

        foreach (var (selector, property, expected) in checks)
        {
            Assert.AreEqual(expected, await ComputedStyleAsync(selector, property), $"{selector} {property}");

            // The same element of a viewer outside the ancestor keeps the default.
            var defaultSelector = selector.Replace(v, "#mdv-css-defaults");
            Assert.AreNotEqual(expected, await ComputedStyleAsync(defaultSelector, property), $"{defaultSelector} {property} already had the value");
        }

        StringAssert.Contains(await ComputedStyleAsync(v, "font-family"), "Georgia");
        StringAssert.Contains(await ComputedStyleAsync($"{v} h2", "font-family"), "Courier New");
        StringAssert.Contains(await ComputedStyleAsync($"{v} p code", "font-family"), "Consolas");

        // The hover color of a link.
        await Page.Locator($"{v} p a").HoverAsync();
        await Expect(Page.Locator($"{v} p a")).ToHaveCSSAsync("color", "rgb(200, 10, 10)");
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInheritedDirections_FlipTheLogicalLayout()
    {
        await OpenAsync(Route);

        var rtl = Page.Locator("#mdv-rtl");
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "#mdv-rtl", "#mdv-rtl-inherited" })
        {
            Assert.AreEqual("2px", await ComputedStyleAsync($"{id} > blockquote", "border-right-width"), $"{id} quote bar");
            Assert.AreEqual("0px", await ComputedStyleAsync($"{id} > blockquote", "border-left-width"), $"{id} quote bar");
            Assert.AreNotEqual("0px", await ComputedStyleAsync($"{id} > ul", "padding-right"), $"{id} list indent");
            Assert.AreEqual("0px", await ComputedStyleAsync($"{id} > ul", "padding-left"), $"{id} list indent");

            // The start-aligned column hugs the right edge, the end-aligned one the left.
            var startCell = await Page.Locator($"{id} td.bit-mdv-align-left").EvaluateAsync<double[]>(
                "td => { const r = document.createRange(); r.selectNodeContents(td); const t = r.getBoundingClientRect(); const c = td.getBoundingClientRect(); return [t.left - c.left, c.right - t.right]; }");
            Assert.IsTrue(startCell[1] < startCell[0], $"{id}: the start-aligned cell is not right-aligned");
        }

        Assert.IsNull(await Page.Locator("#mdv-rtl-inherited").GetAttributeAsync("dir"));
        Assert.AreEqual("2px", await ComputedStyleAsync("#mdv-ltr > blockquote", "border-left-width"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#mdv-ltr > blockquote", "border-right-width"));
    }

    [TestMethod]
    public async Task Rtl_DirAuto_GivesEachBlockTheDirectionOfItsOwnText()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-auto");
        await Expect(root).ToHaveAttributeAsync("dir", "auto");
        await Expect(root.Locator("h2")).ToHaveAttributeAsync("dir", "auto");
        await Expect(root.Locator("p").First).ToHaveAttributeAsync("dir", "auto");
        await Expect(root.Locator("li").First).ToHaveAttributeAsync("dir", "auto");

        Assert.IsTrue(await root.Locator("p").Nth(1).EvaluateAsync<bool>("p => p.matches(':dir(rtl)')"), "the Persian paragraph is not right to left");
        Assert.IsTrue(await root.Locator("p").Nth(0).EvaluateAsync<bool>("p => p.matches(':dir(ltr)')"), "the English paragraph is not left to right");
        Assert.IsTrue(await root.Locator("li").Nth(1).EvaluateAsync<bool>("li => li.matches(':dir(rtl)')"), "the Persian item is not right to left");
        Assert.IsTrue(await root.Locator("li").Nth(0).EvaluateAsync<bool>("li => li.matches(':dir(ltr)')"), "the English item is not left to right");
    }

    [TestMethod]
    public async Task Base_Visibility_HidesAndCollapsesTheRoot()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-visibility");
        await Expect(root).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await root.BoundingBoxAsync())!.Height > 0, "a hidden viewer keeps its box");

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Base_IdHtmlAttributesAndTabIndex_ReachTheRoot()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-attrs");
        await Expect(root).ToHaveAttributeAsync("data-test", "mdv-data");
        await Expect(root).ToHaveAttributeAsync("title", "Viewer title");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await root.FocusAsync();
        await Expect(root).ToBeFocusedAsync();

        // A viewer given no id still renders one of its own.
        var generated = await Page.Locator(".mdv-no-id").GetAttributeAsync("id");
        Assert.IsFalse(string.IsNullOrWhiteSpace(generated));
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReParseOnlyWhenAnInputChanges()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#mdv-runtime");
        await Expect(root.Locator("h1")).ToHaveTextAsync("Original");
        await Expect(Page.Locator("#parse-count")).ToHaveTextAsync("1");

        // An unrelated re-render, and an equal string, parse nothing again.
        await Page.ClickAsync("#btn-rerender");
        await Expect(Page.Locator("#rerenders")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-same-markdown");
        await Page.ClickAsync("#btn-rerender");
        await Expect(Page.Locator("#rerenders")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#parse-count")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-set-markdown");
        await Expect(root.Locator("h2")).ToHaveTextAsync("Changed");
        await Expect(root.Locator("strong")).ToHaveTextAsync("content");
        await Expect(Page.Locator("#parse-count")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-set-pipeline");
        await Expect(Page.Locator("#parse-count")).ToHaveTextAsync("3");

        await Page.ClickAsync("#btn-set-offset");
        await Expect(root.Locator("h4")).ToHaveTextAsync("Changed");
        await Expect(Page.Locator("#parse-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task PublicMembers_Document_HoldsTheMostRecentParse()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-read-document");
        await Expect(Page.Locator("#document-info")).ToHaveTextAsync("1:h1");

        await Page.ClickAsync("#btn-set-markdown");
        await Expect(Page.Locator("#mdv-runtime h2")).ToHaveTextAsync("Changed");
        await Page.ClickAsync("#btn-read-document");
        await Expect(Page.Locator("#document-info")).ToHaveTextAsync("2:h2");
    }

    [TestMethod]
    public async Task Lifecycle_Unmount_DisposesWithoutAnError_AndMountsAgain()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-unmount a").First).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdv-unmount")).ToHaveCountAsync(0);

        // The page's address changes after the viewer is gone: a disposed viewer must not follow it.
        await Page.EvaluateAsync("() => Blazor.navigateTo('/e2e/markdown-viewer?after=unmount', { replaceHistoryEntry: true })");
        await Expect(Page).ToHaveURLAsync(new Regex("after=unmount"));

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Page.Locator("#mdv-unmount a").First).ToHaveAttributeAsync("href", "/e2e/markdown-viewer?after=unmount#install");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Lifecycle_AddressChange_RewritesInPageLinksAgainstTheNewPage()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-links-default a", new() { HasTextString = "a fragment" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer#frag");

        await Page.EvaluateAsync("() => Blazor.navigateTo('/e2e/markdown-viewer?q=1')");
        await Expect(Page.Locator("#mdv-links-default a", new() { HasTextString = "a fragment" })).ToHaveAttributeAsync("href", "/e2e/markdown-viewer?q=1#frag");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_ThePermalinkFadeCollapses()
    {
        await OpenAsync(Route);
        var anchor = Page.Locator("#mdv-toc h2#install a.bit-mdv-anchor");
        var normal = await anchor.EvaluateAsync<string>("a => getComputedStyle(a).transitionDuration");
        Assert.AreNotEqual("0s", normal, "the permalink does not fade");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.WaitForFunctionAsync("() => parseFloat(getComputedStyle(document.querySelector('#mdv-toc h2#install a.bit-mdv-anchor')).transitionDuration) < 0.001");
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// BitMarkdownViewer on a phone: a touch screen has no hover, so the heading permalinks show at rest, and a table wider
/// than the screen scrolls inside its own region rather than widening the page.
/// </summary>
[TestClass]
public class BitMarkdownViewerTouchE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        IsMobile = true,
        HasTouch = true,
    };

    [TestMethod]
    public async Task Touch_PermalinksShowWithoutHover_AndAWideTableScrollsInItsRegion()
    {
        if (IsFirefox) Assert.Inconclusive("Firefox has no mobile emulation (IsMobile).");

        await OpenAsync("/e2e/markdown-viewer");

        await Expect(Page.Locator("#mdv-toc h2#install a.bit-mdv-anchor")).ToHaveCSSAsync("opacity", "1");

        var wrapper = Page.Locator("#mdv-css-vars .bit-mdv-table-wrapper");
        await Expect(wrapper).ToHaveCSSAsync("overflow-x", "auto");
        var pageWidth = await Page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");
        Assert.IsTrue(pageWidth <= 390 + 40, $"the document widened the page to {pageWidth}px");

        // A tap on a task box of an interactive list ticks it.
        await Page.Locator("#mdv-tasks input.task-list-item-checkbox").Nth(1).TapAsync();
        await Expect(Page.Locator("#task-last")).ToHaveTextAsync("1:True");
    }
}
