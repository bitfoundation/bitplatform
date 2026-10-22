using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiMarkdownView at /e2e/aimarkdownview in a real browser: every block under a class of its own, the prose
/// painted from the bit theme in the light and the dark scheme, heading anchors named after their view and reached
/// through a link, footnote references and back-links followed by the pointer and the keyboard, untrusted links and
/// markup that never reach the page as script, the image policies (nothing fetched until the reader asks, nothing at
/// all when blocked, the listed hosts only), the streaming caret and reduced motion, right-to-left text with its code
/// kept left to right and isolated, every --bit-AiMarkdownView-* variable, Classes/Styles, print and forced colors.
/// </summary>
[TestClass]
public class BitAiMarkdownViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aimarkdownview";

    // What every image of the page is answered with: the hosts of their addresses do not exist.
    private const string Svg = """<svg xmlns="http://www.w3.org/2000/svg" width="64" height="32"><rect width="64" height="32" fill="#2563eb"/></svg>""";

    private readonly List<string> _imageRequests = [];

    [TestInitialize]
    public async Task ServeTheImagesAsync()
    {
        // Answered here rather than by the network, which is also how a test tells which view fetched which image.
        await Page.RouteAsync("https://*.e2e.test/**", async route =>
        {
            lock (_imageRequests) _imageRequests.Add(route.Request.Url);
            await route.FulfillAsync(new() { Status = 200, ContentType = "image/svg+xml", Body = Svg });
        });
    }

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_ADocument_RendersEveryBlockUnderAClassOfItsOwn()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-basic");
        await Expect(view).ToHaveClassAsync(HasClass("bit-aimdv"));

        // A "#" is an h2 and a "##" an h3, so an answer never competes with the h1 of the page.
        await Expect(view.Locator("h2.bit-aimdv-hdg")).ToHaveTextAsync("Render modes");
        await Expect(view.Locator("h3.bit-aimdv-hdg")).ToHaveTextAsync("Where the code runs");
        await Expect(view.Locator("h1")).ToHaveCountAsync(0);

        var paragraph = view.Locator("p.bit-aimdv-par").First;
        await Expect(paragraph.Locator("strong")).ToHaveTextAsync("two places");
        await Expect(paragraph.Locator("em")).ToHaveTextAsync("matters");
        await Expect(paragraph.Locator("del")).ToHaveTextAsync("Never");

        await Expect(view.Locator("ul.bit-aimdv-lst:not(.bit-aimdv-tsk) > li.bit-aimdv-itm")).ToHaveCountAsync(2);
        await Expect(view.Locator("ol.bit-aimdv-lst")).ToHaveAttributeAsync("start", "3");

        // The boxes of a task list show what the model wrote; nothing of the page can change them.
        var boxes = view.Locator("ul.bit-aimdv-tsk input.bit-aimdv-chk");
        await Expect(boxes).ToHaveCountAsync(2);
        await Expect(boxes.Nth(0)).ToBeCheckedAsync();
        await Expect(boxes.Nth(1)).Not.ToBeCheckedAsync();
        await Expect(boxes.Nth(1)).ToBeDisabledAsync();
        await Expect(boxes.Nth(1)).ToHaveAttributeAsync("aria-disabled", "true");
        await boxes.Nth(1).ClickAsync(new() { Force = true });
        await Expect(boxes.Nth(1)).Not.ToBeCheckedAsync();

        // A table keeps its row and column semantics, and the alignments of its columns.
        var table = view.GetByRole(AriaRole.Table);
        await Expect(table).ToHaveCountAsync(1);
        await Expect(table.GetByRole(AriaRole.Columnheader)).ToHaveTextAsync(["Mode", "Runs on", "First paint"]);
        await Expect(table.Locator("tbody > tr")).ToHaveCountAsync(2);
        await Expect(table.Locator("th").Nth(0)).ToHaveCSSAsync("text-align", "start");
        await Expect(table.Locator("th").Nth(1)).ToHaveCSSAsync("text-align", "center");
        await Expect(table.Locator("tbody td").Nth(2)).ToHaveCSSAsync("text-align", "end");

        var link = view.Locator("a.bit-aimdv-lnk");
        await Expect(link).ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener noreferrer nofollow");

        await Expect(view.Locator("code.bit-aimdv-cod")).ToHaveTextAsync("StateHasChanged");
        await Expect(view.Locator("blockquote.bit-aimdv-quo")).ToHaveTextAsync("Rendering more often than the eye can follow is not smoother.");
        await Expect(view.Locator("hr.bit-aimdv-sep")).ToHaveCountAsync(1);

        // A fenced block is a code block of its own.
        await Expect(view.Locator(".bit-aicbv")).ToHaveCountAsync(1);
        await Expect(view.Locator(".bit-aicbv-lng")).ToHaveTextAsync("csharp");
        await Expect(view.Locator(".bit-aicbv-pre")).ToHaveTextAsync("var total = items.Sum(item => item.Price);");

        // With no document nothing is rendered at all.
        await Expect(Page.Locator("#mdv-null")).ToHaveCountAsync(0);
        Assert.AreEqual(0, await Page.Locator("#mdv-null-box").EvaluateAsync<int>("e => e.childElementCount"));
    }

    [TestMethod]
    public async Task Theme_TheProse_PaintsFromTheTokensOfTheBitThemeInLightAndDark()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-basic");
        var link = view.Locator("a.bit-aimdv-lnk");
        var code = view.Locator("code.bit-aimdv-cod");
        var quote = view.Locator("blockquote.bit-aimdv-quo");
        var heading = view.Locator("h2.bit-aimdv-hdg");
        var rule = view.Locator("hr.bit-aimdv-sep");

        // Links are told apart by more than their color: an underline at rest.
        await Expect(link).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(code).ToHaveCSSAsync("font-family", await Page.ResolveFontFamilyAsync("var(--bit-tpg-font-family-mono)"));
        await Expect(heading).ToHaveCSSAsync("font-weight", await ResolveAsync("fontWeight", "var(--bit-tpg-fw-semibold)"));
        await Expect(heading).ToHaveCSSAsync("font-size", await view.ResolveLengthInAsync("var(--bit-tpg-fs-2xl)"));

        await ExpectThemeColorsAsync();
        var lightLink = await link.EvaluateAsync<string>("e => getComputedStyle(e).color");

        // The dark scheme re-skins the prose through the same tokens.
        await Page.EvaluateAsync("() => document.documentElement.setAttribute('bit-theme', 'dark')");
        await Expect(link).Not.ToHaveCSSAsync("color", lightLink);
        await ExpectThemeColorsAsync();

        async Task ExpectThemeColorsAsync()
        {
            await Expect(link).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-fg)"));
            await Expect(quote).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
            await Expect(quote).ToHaveCSSAsync("border-left-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
            await Expect(rule).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
            await Expect(view.Locator(".bit-aimdv-tbw")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        }
    }

    // ---------- heading anchors ----------

    [TestMethod]
    public async Task Headings_TheSameDocumentInTwoViews_NamesEveryHeadingAfterItsView()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mdv-head-a h2.bit-aimdv-hdg")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#mdv-head-a h2.bit-aimdv-hdg").Nth(0)).ToHaveAttributeAsync("id", "mdv-head-a-bit-ai-h-render-modes");
        await Expect(Page.Locator("#mdv-head-a h3.bit-aimdv-hdg")).ToHaveAttributeAsync("id", "mdv-head-a-bit-ai-h-server");
        await Expect(Page.Locator("#mdv-head-a h2.bit-aimdv-hdg").Nth(1)).ToHaveAttributeAsync("id", "mdv-head-a-bit-ai-h-render-modes-2");
        await Expect(Page.Locator("#mdv-head-b h2.bit-aimdv-hdg").Nth(0)).ToHaveAttributeAsync("id", "mdv-head-b-bit-ai-h-render-modes");

        // Not one id of the page is given twice, however many views render the same headings and footnotes.
        var duplicates = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const seen = new Map();
                for (const e of document.querySelectorAll('[id]')) seen.set(e.id, (seen.get(e.id) || 0) + 1);
                return [...seen].filter(([, n]) => n > 1).map(([id]) => id);
            }
            """);
        Assert.AreEqual(0, duplicates.Length, $"Duplicate ids: {string.Join(", ", duplicates)}");
    }

    [TestMethod]
    public async Task Headings_ALinkToTheAnchorOfAHeading_ScrollsItToTheTopWithItsScrollMargin()
    {
        await OpenAsync(Route);

        var target = Page.Locator("#mdv-head-b-bit-ai-h-render-modes");
        await Expect(target).Not.ToBeInViewportAsync();

        await Page.Locator("#mdv-head-link").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/aimarkdownview#mdv-head-b-bit-ai-h-render-modes$"));
        await Expect(target).ToBeInViewportAsync();
        await Expect(Page.Locator("#mdv-head-a-bit-ai-h-render-modes")).Not.ToBeInViewportAsync();

        // The heading stops a little below the top of the viewport rather than against it.
        var margin = AiE2EHelpers.ParsePx(await target.EvaluateAsync<string>("e => getComputedStyle(e).scrollMarginBlockStart"));
        Assert.IsTrue(margin > 0, $"The heading has a scroll margin, was {margin}px.");
        await AiE2EHelpers.PollAsync(async () => Math.Abs((await target.RectAsync()).Top - margin) <= 2,
            "The heading is scrolled to its scroll margin below the top of the viewport.");
    }

    // ---------- footnotes ----------

    [TestMethod]
    public async Task Footnotes_References_AreListedAsNotesWithABackLinkPerReference()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-fn");
        var references = view.Locator("sup.bit-aimdv-fnr > a[role='doc-noteref']");
        await Expect(references).ToHaveTextAsync(["1", "2", "1"]);
        await Expect(references.Nth(0)).ToHaveAttributeAsync("href", "#mdv-fn-fn-1");
        await Expect(view.Locator("sup.bit-aimdv-fnr").Nth(2)).ToHaveAttributeAsync("id", "mdv-fn-fnref-1-2");

        var notes = view.Locator("section[role='doc-endnotes'] li.bit-aimdv-fni");
        await Expect(notes).ToHaveCountAsync(2);
        await Expect(notes.Nth(0)).ToHaveAttributeAsync("id", "mdv-fn-fn-1");
        await Expect(notes.Nth(0)).ToContainTextAsync("The first note.");

        // A note cited twice links back to both places.
        var backLinks = notes.Nth(0).Locator("a[role='doc-backlink']");
        await Expect(backLinks).ToHaveCountAsync(2);
        await Expect(backLinks.Nth(0)).ToHaveAttributeAsync("href", "#mdv-fn-fnref-1-1");
        await Expect(backLinks.Nth(1)).ToHaveAttributeAsync("href", "#mdv-fn-fnref-1-2");
        await Expect(backLinks.Nth(0)).ToHaveAccessibleNameAsync("Back to reference 1");

        // The notes are quieter than the text they annotate.
        await Expect(view.Locator(".bit-aimdv-fns")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));

        // Another view's footnote 1 is a note of its own.
        await Expect(Page.Locator("#mdv-fn2 li.bit-aimdv-fni")).ToHaveAttributeAsync("id", "mdv-fn2-fn-1");
        await Expect(Page.Locator("#mdv-fn2 a[role='doc-noteref']")).ToHaveAttributeAsync("href", "#mdv-fn2-fn-1");
    }

    [TestMethod]
    public async Task Footnotes_ClickingAReferenceAndItsBackLink_MovesBetweenThemOnThePage()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-fn");
        var reference = view.Locator("a[role='doc-noteref']").Nth(1);
        var note = view.Locator("#mdv-fn-fn-2");

        await reference.ClickAsync();

        // Following the reference brings its note into view, on this page: the app does not navigate anywhere else.
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/aimarkdownview#mdv-fn-fn-2$"));
        await Expect(note).ToBeInViewportAsync();
        await Expect(reference).Not.ToBeInViewportAsync();
        await Expect(Page.Locator("#mdv-fn")).ToHaveCountAsync(1);

        await note.Locator("a[role='doc-backlink']").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/aimarkdownview#mdv-fn-fnref-2-1$"));
        await Expect(reference).ToBeInViewportAsync();
        await Expect(note).Not.ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task Footnotes_TheKeyboard_FollowsAReferenceToItsNoteAndABackLinkHome()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-fn");
        var secondReferenceToNote1 = view.Locator("a[role='doc-noteref']").Nth(2);
        var note = view.Locator("#mdv-fn-fn-1");

        await secondReferenceToNote1.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/aimarkdownview#mdv-fn-fn-1$"));
        await Expect(note).ToBeInViewportAsync();
        // The keyboard follows the reader there: the focus is in the note, or the next Tab puts it there.
        await ExpectFocusMovedIntoAsync(note);

        var secondBackLink = note.Locator("a[role='doc-backlink']").Nth(1);
        await secondBackLink.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // The second back-link returns to the second place the note was cited from.
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/aimarkdownview#mdv-fn-fnref-1-2$"));
        await Expect(secondReferenceToNote1).ToBeInViewportAsync();
        await ExpectFocusMovedIntoAsync(view.Locator("#mdv-fn-fnref-1-2"));
    }

    // ---------- untrusted content ----------

    [TestMethod]
    public async Task Sanitize_UntrustedLinksAndMarkup_NeverReachThePageAsScript()
    {
        var dialogs = 0;
        Page.Dialog += async (_, dialog) =>
        {
            dialogs++;
            await dialog.DismissAsync();
        };

        await OpenAsync(Route);

        var view = Page.Locator("#mdv-unsafe");

        // Every scheme that can run script, however it is cased, is neutralised to "#". (The entity-encoded ones are
        // kept as written, which the browser reads as a harmless relative address: the protocol check below covers them.)
        foreach (var name in new[] { "a script link", "an upper-case one", "a vbscript one", "a data one" })
        {
            await Expect(view.GetByRole(AriaRole.Link, new() { Name = name, Exact = true })).ToHaveAttributeAsync("href", "#");
        }

        await Expect(view.GetByRole(AriaRole.Link, new() { Name = "a safe one" })).ToHaveAttributeAsync("href", "https://bitplatform.dev/");
        await Expect(view.GetByRole(AriaRole.Link, new() { Name = "a mail one" })).ToHaveAttributeAsync("href", "mailto:info@bitplatform.dev");
        await Expect(view.GetByRole(AriaRole.Link, new() { Name = "a relative one" })).ToHaveAttributeAsync("href", "/e2e/aimarkdownview");

        // What the browser itself parsed out of every href of the view: never a scheme that runs anything.
        var protocols = await view.Locator("a").EvaluateAllAsync<string[]>("links => links.map(a => a.protocol)");
        Assert.AreEqual(9, protocols.Length);
        foreach (var protocol in protocols)
        {
            Assert.IsTrue(protocol is "http:" or "https:" or "mailto:", $"A link of the view has the protocol {protocol}.");
        }

        // A script image becomes a blank pixel that keeps its alternative text.
        var image = view.Locator("img.bit-aimdv-img");
        await Expect(image).ToHaveAttributeAsync("alt", "a script image");
        await Expect(image).ToHaveAttributeAsync("src", new Regex("^data:image/gif;base64,"));

        // Raw HTML stays the text it is: no element of it reaches the page.
        await Expect(view.Locator("script, [onerror], [onmouseover]")).ToHaveCountAsync(0);
        await Expect(view).ToContainTextAsync("<img src=\"x\" onerror=\"alert('g')\">");
        await Expect(view).ToContainTextAsync("<script>alert('h')</script>");
        await Expect(view).ToContainTextAsync("<div onmouseover=\"alert('i')\">An HTML block</div>");

        await view.GetByText("An HTML block").HoverAsync();
        await NextFramesAsync();
        Assert.AreEqual(0, dialogs, "No script of the Markdown ran.");
    }

    // ---------- images ----------

    [TestMethod]
    public async Task Images_ClickToLoad_FetchesNothingUntilTheReaderAsks()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-img-ask");
        var chart = view.GetByRole(AriaRole.Button, new() { Name = "Load image: A chart of the visitors" });
        var photo = view.GetByRole(AriaRole.Button, new() { Name = "Load image: other.e2e.test" });

        // A placeholder per remote image, named after what it shows or, with no alternative text, its host.
        await Expect(chart).ToBeVisibleAsync();
        await Expect(photo).ToBeVisibleAsync();
        await Expect(chart).ToHaveAttributeAsync("title", "https://images.e2e.test/ask/chart.svg");
        await chart.Locator(".bit-icon-ai--Photo2").AssertAiGlyphAsync("Photo2");

        // An inline image is fetched from nowhere, so it is shown under this policy too.
        await Expect(view.Locator("img")).ToHaveCountAsync(1);
        await Expect(view.Locator("img")).ToHaveAttributeAsync("alt", "A pixel");

        await NextFramesAsync();
        Assert.IsFalse(Requested("/ask/"), "No image of the view was fetched before the reader asked.");

        await chart.ClickAsync();

        var image = view.Locator("img[alt='A chart of the visitors']");
        await Expect(image).ToHaveAttributeAsync("src", "https://images.e2e.test/ask/chart.svg");
        await ExpectLoadedAsync(image);
        Assert.IsTrue(Requested("/ask/chart.svg"));
        Assert.IsFalse(Requested("/ask/photo.svg"), "Loading one image loads that one alone.");
        await Expect(photo).ToBeVisibleAsync();

        // The keyboard asks the same way.
        await photo.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(view.Locator("img[src='https://other.e2e.test/ask/photo.svg']")).ToHaveAttributeAsync("alt", "");
        await Expect(photo).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Images_Block_ShowsTheAlternativeTextAndFetchesNothing()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-img-block");

        // Inline images are blocked too, and an image with no alternative text leaves nothing behind.
        await Expect(view.Locator("span.bit-aimdv-imb")).ToHaveTextAsync(["A chart of the visitors", "A pixel"]);
        await Expect(view.Locator("span.bit-aimdv-imb").First).ToHaveCSSAsync("font-style", "italic");
        await Expect(view.Locator("span.bit-aimdv-imb").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(view.Locator("img, button")).ToHaveCountAsync(0);

        await NextFramesAsync();
        Assert.IsFalse(Requested("/block/"), "A blocked image is never fetched.");
    }

    [TestMethod]
    public async Task Images_AllowListedHosts_ShowsTheListedHostAndAsksForTheRest()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-img-listed");
        var listed = view.Locator("img[alt='A chart of the visitors']");

        await Expect(listed).ToHaveAttributeAsync("loading", "lazy");
        await listed.ScrollIntoViewIfNeededAsync();
        await ExpectLoadedAsync(listed);
        await Expect(listed).ToHaveCSSAsync("max-width", "100%");

        await Expect(view.GetByRole(AriaRole.Button, new() { Name = "Load image: other.e2e.test" })).ToBeVisibleAsync();
        await Expect(view.Locator("img[alt='A pixel']")).ToHaveCountAsync(1);
        Assert.IsTrue(Requested("images.e2e.test/listed/chart.svg"));
        Assert.IsFalse(Requested("other.e2e.test/listed/"), "An image of a host that is not listed waits for the reader.");
    }

    // ---------- caret ----------

    [TestMethod]
    public async Task Caret_AnOpenParagraph_IsTrailedByABlinkingCaret()
    {
        await OpenAsync(Route);

        var paragraph = Page.Locator("#mdv-caret p.bit-aimdv-par");
        var caret = paragraph.Locator(".bit-aimdv-crt");

        await Expect(caret).ToHaveCountAsync(1);
        await Expect(caret).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(caret).ToHaveCSSAsync("display", "inline-block");
        await Expect(caret).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(caret).ToHaveCSSAsync("animation-name", "bit-aimdv-blink");
        Assert.AreEqual(1100, await caret.AnimationDurationAsync("bit-aimdv-blink") ?? 0, 1);

        // It trails the last character of the text, which a screen reader reads without it.
        var last = await paragraph.CharacterRectAsync("The answer is still being".Length - 1);
        var box = await caret.RectAsync();
        Assert.IsTrue(box.Left >= last.Right - 0.5, $"The caret ({box.Left}) is after the text ({last.Right}).");
        await Expect(paragraph).ToHaveTextAsync("The answer is still being");

        // A closed paragraph, a view not asked for one and the options turning it off draw none.
        await Expect(Page.Locator("#mdv-caret-closed .bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-caret-off .bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mdv-caret-option .bit-aimdv-crt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_StopsTheCaretUnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var caret = Page.Locator("#mdv-caret .bit-aimdv-crt");
        await Expect(caret).ToHaveCSSAsync("animation-name", "none");
        await Expect(caret).ToHaveCSSAsync("opacity", "0.8");
        await Expect(caret).ToBeVisibleAsync();

        // ForceAnimation on an ancestor keeps it blinking.
        var forced = Page.Locator("#mdv-caret-fam .bit-aimdv-crt");
        await Expect(forced).ToHaveCSSAsync("animation-name", "bit-aimdv-blink");
        Assert.IsNotNull(await forced.AnimationDurationAsync("bit-aimdv-blink"));

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(caret).ToHaveCSSAsync("animation-name", "bit-aimdv-blink");
    }

    // ---------- rtl ----------

    [TestMethod]
    public async Task Rtl_ARightToLeftAnswer_KeepsItsInlineCodeLeftToRightAndIsolated()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "mdv-rtl", "mdv-rtl-explicit" })
        {
            var paragraph = Page.Locator($"#{id} > p.bit-aimdv-par");
            await Expect(paragraph).ToHaveCSSAsync("direction", "rtl");

            var code = paragraph.Locator("code.bit-aimdv-cod");
            await Expect(code).ToHaveCSSAsync("direction", "ltr");
            await Expect(code).ToHaveCSSAsync("unicode-bidi", "isolate");

            // "x = 1;" reads left to right, its semicolon at its right end rather than moved to the other.
            var x = await code.CharacterRectAsync(0);
            var one = await code.CharacterRectAsync(4);
            var semicolon = await code.CharacterRectAsync(5);
            Assert.IsTrue(x.Left < one.Left && one.Left < semicolon.Left, $"{id}: the code is laid out left to right ({x.Left}, {one.Left}, {semicolon.Left}).");

            // The rest mirrors: the bar of the quote and the indent of the list are at the right.
            var quote = Page.Locator($"#{id} blockquote.bit-aimdv-quo");
            await Expect(quote).ToHaveCSSAsync("border-left-width", "0px");
            Assert.IsTrue(AiE2EHelpers.ParsePx(await quote.EvaluateAsync<string>("e => getComputedStyle(e).borderRightWidth")) > 0, $"{id}: the bar of the quote is at the right.");

            var list = Page.Locator($"#{id} ul.bit-aimdv-lst");
            await Expect(list).ToHaveCSSAsync("padding-left", "0px");
            Assert.IsTrue(AiE2EHelpers.ParsePx(await list.EvaluateAsync<string>("e => getComputedStyle(e).paddingRight")) > 0, $"{id}: the list is indented from the right.");
        }

        var ltrQuote = Page.Locator("#mdv-ltr blockquote.bit-aimdv-quo");
        await Expect(ltrQuote).ToHaveCSSAsync("border-right-width", "0px");
        Assert.IsTrue(AiE2EHelpers.ParsePx(await ltrQuote.EvaluateAsync<string>("e => getComputedStyle(e).borderLeftWidth")) > 0);
    }

    // ---------- public variables, Classes and Styles ----------

    [TestMethod]
    public async Task Variables_ThePublicVariables_RestyleEveryPartOfTheProse()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-vars");

        await Expect(view).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(view).ToHaveCSSAsync("font-size", "18px");
        await Expect(view).ToHaveCSSAsync("line-height", "36px");
        await Expect(view.Locator(":scope > :nth-child(2)")).ToHaveCSSAsync("margin-top", "40px");

        var heading = view.Locator(".bit-aimdv-hdg");
        await Expect(heading).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(heading).ToHaveCSSAsync("font-weight", "300");

        await Expect(view.Locator(".bit-aimdv-lnk")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");

        var code = view.Locator(".bit-aimdv-cod");
        await Expect(code).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(code).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(code).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(code).ToHaveCSSAsync("font-family", "serif");

        var quote = view.Locator(".bit-aimdv-quo");
        await Expect(quote).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(quote).ToHaveCSSAsync("border-left-color", "rgb(19, 20, 21)");

        await Expect(view.Locator(".bit-aimdv-sep")).ToHaveCSSAsync("border-top-color", "rgb(22, 23, 24)");

        var tableBox = view.Locator(".bit-aimdv-tbw");
        await Expect(tableBox).ToHaveCSSAsync("border-top-color", "rgb(25, 26, 27)");
        await Expect(tableBox).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(view.Locator(".bit-aimdv-thc").First).ToHaveCSSAsync("background-color", "rgb(28, 29, 30)");
        await Expect(view.Locator(".bit-aimdv-tbl")).ToHaveCSSAsync("font-size", "11px");

        await Expect(view.Locator(".bit-aimdv-crt")).ToHaveCSSAsync("background-color", "rgb(31, 32, 33)");

        // A variable on the Style of one view restyles that view.
        await Expect(Page.Locator("#mdv-vars-style .bit-aimdv-hdg")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(Page.Locator("#mdv-basic h2.bit-aimdv-hdg")).Not.ToHaveCSSAsync("color", "rgb(40, 41, 42)");
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheirParts()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#mdv-parts");
        await Expect(view).ToHaveClassAsync(HasClass("e2e-mdv-class"));
        await Expect(view).ToHaveClassAsync(HasClass("e2e-mdv-root"));
        await Expect(view).ToHaveCSSAsync("outline-color", "rgb(50, 51, 52)");

        await Expect(view.Locator(".bit-aimdv-hdg")).ToHaveClassAsync(HasClass("e2e-mdv-heading"));
        await Expect(view.Locator(".bit-aimdv-hdg")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(view.Locator(".bit-aimdv-par").First).ToHaveClassAsync(HasClass("e2e-mdv-paragraph"));
        await Expect(view.Locator(".bit-aimdv-lnk")).ToHaveClassAsync(HasClass("e2e-mdv-link"));
        await Expect(view.Locator(".bit-aimdv-cod")).ToHaveClassAsync(HasClass("e2e-mdv-code"));
        await Expect(view.Locator(".bit-aimdv-cod")).ToHaveCSSAsync("font-weight", "700");
        await Expect(view.Locator(".bit-aimdv-quo")).ToHaveClassAsync(HasClass("e2e-mdv-quote"));
        await Expect(view.Locator(".bit-aimdv-quo")).ToHaveCSSAsync("padding-top", "5px");
        await Expect(view.Locator(".bit-aimdv-tbl")).ToHaveClassAsync(HasClass("e2e-mdv-table"));
        await Expect(view.Locator(".bit-aimdv-crt")).ToHaveClassAsync(HasClass("e2e-mdv-caret"));
        await Expect(view.Locator(".bit-aimdv-crt")).ToHaveCSSAsync("width", "20px");
    }

    // ---------- print, forced colors ----------

    [TestMethod]
    public async Task Print_APrintedAnswer_HidesTheCaretAndKeepsTablesAndQuotesWhole()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#mdv-caret .bit-aimdv-crt")).ToHaveCSSAsync("display", "none");

        var tableBox = Page.Locator("#mdv-basic .bit-aimdv-tbw");
        await Expect(tableBox).ToHaveCSSAsync("break-inside", "avoid");
        await Expect(tableBox).ToHaveCSSAsync("overflow-x", "visible");
        await Expect(Page.Locator("#mdv-basic .bit-aimdv-quo")).ToHaveCSSAsync("break-inside", "avoid");
        await Expect(Page.Locator("#mdv-basic h2.bit-aimdv-hdg")).ToHaveCSSAsync("break-after", "avoid");

        await Page.EmulateMediaAsync(new() { Media = Media.Screen });
        await Expect(Page.Locator("#mdv-caret .bit-aimdv-crt")).ToHaveCSSAsync("display", "inline-block");
    }

    [TestMethod]
    public async Task ForcedColors_Active_OutlinesTheCodeAndPaintsTheCaretInHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        // Backgrounds are painted over in forced colors, so what one told apart gets an outline instead.
        await Expect(Page.Locator("#mdv-basic .bit-aimdv-cod")).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Page.Locator("#mdv-caret .bit-aimdv-crt")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("Highlight"));

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        await Expect(Page.Locator("#mdv-basic .bit-aimdv-cod")).ToHaveCSSAsync("outline-style", "none");
    }

    // ---------- helpers ----------

    private bool Requested(string fragment)
    {
        lock (_imageRequests) return _imageRequests.Exists(url => url.Contains(fragment, StringComparison.Ordinal));
    }

    private static async Task ExpectLoadedAsync(ILocator image)
    {
        await Expect(image).ToHaveJSPropertyAsync("complete", true);
        await AiE2EHelpers.PollAsync(async () => await image.EvaluateAsync<int>("e => e.naturalWidth") > 0, "The image was loaded and decoded.");
    }

    // The focus moved into the element, or the next Tab moves it there: following a fragment link moves the focus or the
    // point the sequential focus navigation starts from, and either way the keyboard goes on from the target.
    private async Task ExpectFocusMovedIntoAsync(ILocator target)
    {
        const string isInside = "e => e.contains(document.activeElement)";

        if (await target.EvaluateAsync<bool>(isInside)) return;

        await Page.Keyboard.PressAsync("Tab");
        Assert.IsTrue(await target.EvaluateAsync<bool>(isInside),
            $"The focus moved into the target of the link (it is on {await Page.EvaluateAsync<string>("() => document.activeElement?.outerHTML.slice(0, 120)")}).");
    }

    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            """
            ([property, value]) => {
                const probe = document.createElement('div');
                probe.style[property] = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe)[property];
                probe.remove();
                return resolved;
            }
            """, new[] { property, value });
    }
}
