using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitPdfViewer on <c>/e2e/pdf-viewer</c>, against three-page samples the page serves from
/// <c>wwwroot/samples</c> (text, a nested outline, an internal link, metadata, layers, an attachment; an RC4 encrypted
/// one; a file that is not a PDF): loading from a URL and from bytes, prerendering, page navigation (buttons, page box,
/// thumbnails, bookmarks, links, keyboard, API), zoom (buttons, presets, fit modes, Min/Max/Step, keyboard), rotation,
/// scroll and spread layouts, the pan tool, find with its options and highlights, the text-selection layer, the HTML
/// and Canvas render modes, the sidebars (thumbnails, bookmarks, layers, attachments), document properties,
/// download and print with their cancelable events, fullscreen and presentation mode, opening a file through the
/// picker and by drop (with the size limit), the empty, loading and error states and their templates, the password
/// prompt (dialog, rejected password, cancel, known password, callback, no prompt), the two-way CurrentPage/Zoom/
/// Rotation, every event, the public API, Texts, Disabled (with the page's handlers dropped), the BitParams cascade,
/// Style/Class/Styles/Classes and the public CSS variables, RTL and unmounting.
/// </summary>
[TestClass]
public class BitPdfViewerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/pdf-viewer";

    private ILocator Viewer(string id) => Page.Locator($"#{id}");

    private ILocator Btn(string id, string label) => Page.Locator($"#{id} .bit-pdv-toolbar button[aria-label='{label}']");

    private ILocator PageOf(string id, int number) => Page.Locator($"#{id} .bit-pdv-pages [data-page='{number}']");

    private async Task OpenLoadedAsync(string id = "pdv-main", string? path = null)
    {
        await OpenAsync(path ?? Route);
        await Expect(PageOf(id, 1).Locator(".bit-pdv-text-layer, canvas").First).ToBeAttachedAsync();
    }

    private async Task<byte[]> SampleBytesAsync(string name = "pdf-sample.pdf")
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/samples/{name}");
        return await response.BodyAsync();
    }

    [TestMethod]
    public async Task Prerender_TheServerHtmlHoldsTheFirstPageOfABytesSource()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.AreEqual(200, response.Status);
        StringAssert.Contains(html, "id=\"pdv-dims\"");
        var dims = html[html.IndexOf("id=\"pdv-dims\"", StringComparison.Ordinal)..];
        StringAssert.Contains(dims[..Math.Min(dims.Length, 40000)], "Page One Heading");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Load_AUrlSource_RendersItsPagesAndRaisesTheLoadEvents()
    {
        await OpenLoadedAsync();

        await Expect(Page.Locator("#pdv-main .bit-pdv-pages > .bit-pdv-page")).ToHaveCountAsync(3);
        await Expect(PageOf("pdv-main", 1)).ToHaveAttributeAsync("role", "group");
        await Expect(PageOf("pdv-main", 1)).ToHaveAttributeAsync("aria-label", "Page 1");
        await Expect(PageOf("pdv-main", 1)).ToContainTextAsync("Page One Heading");
        await Expect(Page.Locator("#pdv-main .bit-pdv-title")).ToHaveTextAsync("pdf-sample.pdf");
        await Expect(Page.Locator("#pdv-main .bit-pdv-page-total")).ToHaveTextAsync("/ 3");
        await Expect(Page.Locator("#pdv-main .bit-pdv-surface")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.Locator("#pdv-main .bit-pdv-surface")).ToHaveAttributeAsync("tabindex", "0");

        await Expect(Page.Locator("#loaded-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#rendered-pages")).ToContainTextAsync("1");
        await Expect(Page.Locator("#progress")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#error")).ToBeEmptyAsync();
    }

    [TestMethod]
    public async Task Navigation_TheButtonsAndThePageBoxMoveThroughThePages()
    {
        await OpenLoadedAsync();

        var first = Btn("pdv-main", "First page");
        var prev = Btn("pdv-main", "Previous page");
        var next = Btn("pdv-main", "Next page");
        var last = Btn("pdv-main", "Last page");
        var box = Page.Locator("#pdv-main .bit-pdv-page-input");

        await Expect(first).ToBeDisabledAsync();
        await Expect(prev).ToBeDisabledAsync();
        await Expect(prev).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(box).ToHaveValueAsync("1");

        await next.ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Expect(box).ToHaveValueAsync("2");
        await Expect(PageOf("pdv-main", 2)).ToHaveClassAsync(HasClass("bit-pdv-cur"));
        await Expect(PageOf("pdv-main", 2)).ToBeInViewportAsync();

        await last.ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Expect(next).ToBeDisabledAsync();
        await Expect(last).ToBeDisabledAsync();
        await Expect(PageOf("pdv-main", 3)).ToContainTextAsync("Page Three Heading");

        await prev.ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await first.ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("1");

        await box.FillAsync("3");
        await box.PressAsync("Enter");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#page-changed")).ToHaveTextAsync("2,3,2,1,3");
    }

    [TestMethod]
    public async Task Navigation_AnInternalLinkGoesToItsPage()
    {
        await OpenLoadedAsync();

        var link = PageOf("pdv-main", 1).Locator("a[aria-label='Go to page 3']");
        await Expect(link).ToHaveAttributeAsync("href", "#page=3");
        await link.ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Expect(PageOf("pdv-main", 3)).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task Navigation_ScrollingTheSurfaceUpdatesTheCurrentPage()
    {
        await OpenLoadedAsync();

        await Page.Locator("#pdv-main .bit-pdv-surface").EvaluateAsync("s => s.scrollTop = s.scrollHeight");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#rendered-pages")).ToHaveTextAsync("1,2,3");
        await Expect(PageOf("pdv-main", 3).Locator(".bit-pdv-page-placeholder")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Binding_CurrentPageZoomAndRotationSetByThePage_ReachTheViewer()
    {
        await OpenLoadedAsync();

        await Page.ClickAsync("#btn-set-page");
        await Expect(PageOf("pdv-main", 3)).ToHaveClassAsync(HasClass("bit-pdv-cur"));
        await Expect(Page.Locator("#pdv-main .bit-pdv-page-input")).ToHaveValueAsync("3");
        await Expect(PageOf("pdv-main", 3)).ToBeInViewportAsync();

        await Page.ClickAsync("#btn-set-zoom");
        await Expect(Page.Locator("#pdv-main .bit-pdv-zoom")).ToHaveTextAsync("150%");
        await Expect(PageOf("pdv-main", 1)).ToHaveAttributeAsync("style", new Regex("width:918px"));

        await Page.ClickAsync("#btn-set-rotation");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("90");
        // A quarter turn lays the page on its side once the viewer has re-rendered it.
        await Page.WaitForFunctionAsync("() => { const r = document.querySelector('#pdv-main .bit-pdv-pages [data-page=\"1\"]').getBoundingClientRect(); return r.width > r.height; }");
    }

    [TestMethod]
    public async Task Zoom_TheButtonsTheLevelSelectAndTheKeyboard()
    {
        await OpenLoadedAsync();

        var label = Page.Locator("#pdv-main .bit-pdv-zoom");
        var select = Page.Locator("#pdv-main select[aria-label='Zoom level']");
        await Expect(select).ToHaveValueAsync("FitWidth");
        var options = await select.Locator("option").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Automatic zoom", "Fit width", "Fit page", "Fit height", "Actual size", "50%", "75%", "100%", "125%", "150%", "200%", "300%", "400%" }, options.Select(o => o.Trim()).ToArray());

        await select.SelectOptionAsync("z1");
        await Expect(label).ToHaveTextAsync("100%");
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("1");

        await Btn("pdv-main", "Zoom in").ClickAsync();
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("1.2");
        await Expect(label).ToHaveTextAsync("120%");
        await Btn("pdv-main", "Zoom out").ClickAsync();
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("1");

        await select.SelectOptionAsync("ActualSize");
        await Expect(label).ToHaveTextAsync("100%");
        await select.SelectOptionAsync("FitPage");
        var surface = await Page.Locator("#pdv-main .bit-pdv-surface").EvaluateAsync<double>("s => s.clientHeight");
        var page = (await PageOf("pdv-main", 1).BoundingBoxAsync())!;
        Assert.IsTrue(page.Height <= surface, "Fit page left the page taller than the surface");

        // The keyboard, from the surface.
        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        await Page.Keyboard.PressAsync("Control+0");
        await Expect(label).ToHaveTextAsync("100%");
        await Page.Keyboard.PressAsync("Control+Equal");
        await Expect(label).ToHaveTextAsync("120%");
        await Page.Keyboard.PressAsync("Control+Minus");
        await Expect(label).ToHaveTextAsync("100%");
        Assert.IsTrue(int.Parse((await Page.Locator("#zoom-changed").TextContentAsync())!) >= 6);
    }

    [TestMethod]
    public async Task Zoom_InitialModePresetsAndLimits()
    {
        await OpenLoadedAsync("pdv-zoom");

        var select = Page.Locator("#pdv-zoom select[aria-label='Zoom level']");
        await Expect(select).ToHaveValueAsync("FitPage");
        var options = await select.Locator("option").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Automatic zoom", "Fit width", "Fit page", "Fit height", "Actual size", "50%", "100%", "200%" }, options.Select(o => o.Trim()).ToArray());

        var label = Page.Locator("#pdv-zoom .bit-pdv-zoom");
        await select.SelectOptionAsync("z1");
        await Btn("pdv-zoom", "Zoom in").ClickAsync();
        await Expect(label).ToHaveTextAsync("150%");
        await Btn("pdv-zoom", "Zoom in").ClickAsync();
        await Expect(label).ToHaveTextAsync("200%");
        await Btn("pdv-zoom", "Zoom in").ClickAsync();
        await Expect(label).ToHaveTextAsync("200%");

        await select.SelectOptionAsync("z0.5");
        await Btn("pdv-zoom", "Zoom out").ClickAsync();
        await Expect(label).ToHaveTextAsync("50%");

        await Expect(Page.Locator("#pdv-zoom-actual .bit-pdv-zoom")).ToHaveTextAsync("100%");
        await Expect(PageOf("pdv-zoom-actual", 1)).ToHaveAttributeAsync("style", new Regex("width:612px"));
    }

    [TestMethod]
    public async Task Rotation_TheButtonsAndTheKeyboardTurnThePages()
    {
        await OpenLoadedAsync();

        await Page.Locator("#pdv-main select[aria-label='Zoom level']").SelectOptionAsync("z1");
        await Btn("pdv-main", "Rotate clockwise").ClickAsync();
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("90");
        await Expect(PageOf("pdv-main", 1)).ToHaveAttributeAsync("style", new Regex("width:792px;height:612px"));

        await Btn("pdv-main", "Rotate counter clockwise").ClickAsync();
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("0");
        await Expect(PageOf("pdv-main", 1)).ToHaveAttributeAsync("style", new Regex("width:612px;height:792px"));

        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        await Page.Keyboard.PressAsync("r");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("90");
        await Page.Keyboard.PressAsync("Shift+R");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("0");
        await Page.Keyboard.PressAsync("Shift+R");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("270");
        await Expect(Page.Locator("#rotation-changed")).ToHaveTextAsync("90,0,90,0,270");
    }

    [TestMethod]
    public async Task Keyboard_ShortcutsMoveFindAndToggle()
    {
        await OpenLoadedAsync();

        var surface = Page.Locator("#pdv-main .bit-pdv-surface");
        await surface.FocusAsync();
        await Page.Keyboard.PressAsync("n");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("j");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Page.Keyboard.PressAsync("k");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("p");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("End");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("F4");
        await Expect(Page.Locator("#pdv-main .bit-pdv-thumbs")).ToBeVisibleAsync();
        await Expect(Page.Locator("#sidebar-changed")).ToHaveTextAsync("Thumbnails");
        await surface.FocusAsync();
        await Page.Keyboard.PressAsync("F4");
        await Expect(Page.Locator("#pdv-main .bit-pdv-thumbs")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Control+f");
        var search = Page.Locator("#pdv-main .bit-pdv-search-input");
        await Expect(search).ToBeFocusedAsync();
        await search.FillAsync("keyword");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("1/3");
        await surface.FocusAsync();
        await Page.Keyboard.PressAsync("Control+g");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("2/3");
        await Page.Keyboard.PressAsync("Control+Shift+G");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("1/3");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-box")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Find_CountsWalksAndHighlightsMatches_WithItsOptions()
    {
        await OpenLoadedAsync();

        var toggle = Btn("pdv-main", "Find in document");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        var box = Page.Locator("#pdv-main .bit-pdv-search-box");
        await Expect(box).ToHaveAttributeAsync("role", "search");
        var input = box.Locator(".bit-pdv-search-input");
        await Expect(input).ToBeFocusedAsync();
        var count = box.Locator(".bit-pdv-search-count");

        await input.FillAsync("alpha");
        await Expect(count).ToHaveTextAsync("1/2");
        // The matches are painted with the CSS Custom Highlight API, so the page markup is never touched.
        await Page.WaitForFunctionAsync("() => CSS.highlights.get('bit-pdv-search-current')?.size === 1");
        Assert.AreEqual("alpha", await Page.EvaluateAsync<string>("() => [...CSS.highlights.get('bit-pdv-search-current')][0].toString().toLowerCase()"));

        await input.PressAsync("Enter");
        await Expect(count).ToHaveTextAsync("2/2");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await input.PressAsync("Shift+Enter");
        await Expect(count).ToHaveTextAsync("1/2");
        await box.Locator("button[aria-label='Next match']").ClickAsync();
        await Expect(count).ToHaveTextAsync("2/2");
        await box.Locator("button[aria-label='Previous match']").ClickAsync();
        await Expect(count).ToHaveTextAsync("1/2");

        // Highlight all marks every match, not just the current one.
        var highlightAll = box.Locator("button[aria-label='Highlight all']");
        var allBefore = await Page.EvaluateAsync<int>("() => CSS.highlights.get('bit-pdv-search')?.size ?? 0");
        await highlightAll.ClickAsync();
        await Page.WaitForFunctionAsync($"() => (CSS.highlights.get('bit-pdv-search')?.size ?? 0) !== {allBefore}");
        var pressed = await highlightAll.GetAttributeAsync("aria-pressed");
        var allAfter = await Page.EvaluateAsync<int>("() => CSS.highlights.get('bit-pdv-search')?.size ?? 0");
        Assert.AreEqual(pressed == "true", allAfter >= 2, $"highlight all is {pressed} with {allAfter} highlighted");

        // Match case and whole words narrow it down.
        await input.FillAsync("Bravo");
        await Expect(count).ToHaveTextAsync("1/1");
        var matchCase = box.Locator("button[aria-label='Match case']");
        await input.FillAsync("bravo");
        await matchCase.ClickAsync();
        await Expect(matchCase).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(count).ToHaveTextAsync("Phrase not found");
        await matchCase.ClickAsync();
        await Expect(count).ToHaveTextAsync("1/1");

        await input.FillAsync("key");
        await Expect(count).ToHaveTextAsync("1/3");
        var wholeWord = box.Locator("button[aria-label='Whole words']");
        await wholeWord.ClickAsync();
        await Expect(count).ToHaveTextAsync("Phrase not found");
        await Expect(box.Locator("button[aria-label='Next match']")).ToBeDisabledAsync();
        await wholeWord.ClickAsync();
        await box.Locator("button[aria-label='Match diacritics']").ClickAsync();
        await Expect(box.Locator("button[aria-label='Match diacritics']")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(count).ToHaveTextAsync("1/3");

        // Escape closes the box and takes the highlights with it.
        await input.PressAsync("Escape");
        await Expect(box).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => (CSS.highlights.get('bit-pdv-search')?.size ?? 0) === 0 && (CSS.highlights.get('bit-pdv-search-current')?.size ?? 0) === 0");
    }

    [TestMethod]
    public async Task TextLayer_TheDocumentTextIsSelectable_AndGetSelectedTextReadsIt()
    {
        await OpenLoadedAsync();

        var layer = PageOf("pdv-main", 1).Locator(".bit-pdv-text-layer");
        var span = layer.Locator("span", new() { HasTextString = "Searchable alpha keyword" });
        await Expect(span).ToHaveCountAsync(1);
        Assert.AreEqual("text", await span.EvaluateAsync<string>("s => getComputedStyle(s).userSelect"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await span.EvaluateAsync<string>("s => getComputedStyle(s).color"));
        await Expect(Viewer("pdv-main").Locator(".bit-pdv-surface")).Not.ToHaveClassAsync(HasClass("bit-pdv-nocopy"));

        // The text layer lies over the painted text, glyph run for glyph run.
        var painted = PageOf("pdv-main", 1).Locator("div[aria-hidden='true'] span", new() { HasTextString = "Searchable alpha keyword" });
        var a = (await span.BoundingBoxAsync())!;
        var b = (await painted.BoundingBoxAsync())!;
        Assert.AreEqual(b.X, a.X, 2);
        Assert.AreEqual(b.Y, a.Y, 2);

        await span.EvaluateAsync("s => { const r = document.createRange(); r.selectNodeContents(s); const sel = getSelection(); sel.removeAllRanges(); sel.addRange(r); }");
        await Page.ClickAsync("#btn-api-selected");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("selected:Searchable alpha keyword on page one.");

        await Page.ClickAsync("#btn-api-clear-selection");
        await Page.WaitForFunctionAsync("() => getSelection().toString() === ''");
    }

    [TestMethod]
    public async Task RenderMode_CanvasPaintsTheTextAndKeepsASelectableLayer()
    {
        await OpenLoadedAsync("pdv-canvas");

        var canvas = PageOf("pdv-canvas", 1).Locator("canvas[data-bit-pdv-canvas]");
        await Expect(canvas).ToHaveCountAsync(1);
        await Expect(canvas).ToHaveAttributeAsync("width", "612");
        await Page.WaitForFunctionAsync(@"() => {
            const c = document.querySelector('#pdv-canvas [data-page=""1""] canvas');
            const d = c.getContext('2d').getImageData(60, 40, 300, 100).data;
            for (let i = 0; i < d.length; i += 4) if (d[i + 3] > 0 && d[i] < 128) return true;
            return false;
        }");

        // No painted HTML text in this mode, only the transparent selection layer.
        await Expect(PageOf("pdv-canvas", 1).Locator("div[aria-hidden='true'] span")).ToHaveCountAsync(0);
        await Expect(PageOf("pdv-canvas", 1).Locator(".bit-pdv-text-layer span", new() { HasTextString = "Page One Heading" })).ToHaveCountAsync(1);

        // The HTML mode paints the text as real elements.
        await Expect(PageOf("pdv-main", 1).Locator("canvas")).ToHaveCountAsync(0);
        await Expect(PageOf("pdv-main", 1).Locator("div[aria-hidden='true'] span", new() { HasTextString = "Page One Heading" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Sidebar_ThumbnailsAreAListboxThatNavigates()
    {
        await OpenLoadedAsync("pdv-sidebar");

        var thumbs = Page.Locator("#pdv-sidebar .bit-pdv-thumbs");
        await Expect(thumbs).ToHaveAttributeAsync("role", "listbox");
        var options = thumbs.Locator("[role='option']");
        await Expect(options).ToHaveCountAsync(3);
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(options.Nth(0)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-label", "Page 3");
        await Expect(Btn("pdv-sidebar", "Page thumbnails")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(options.Nth(0).Locator(".bit-pdv-thumb-svg")).ToContainTextAsync("Page One Heading");

        await options.Nth(2).ClickAsync();
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(PageOf("pdv-sidebar", 3)).ToHaveClassAsync(HasClass("bit-pdv-cur"));

        await options.Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Home");
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("End");
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-selected", "true");

        await Btn("pdv-sidebar", "Page thumbnails").ClickAsync();
        await Expect(thumbs).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Sidebar_BookmarksAreATreeThatNavigatesAndFolds()
    {
        await OpenLoadedAsync();

        await Btn("pdv-main", "Bookmarks").ClickAsync();
        await Expect(Btn("pdv-main", "Bookmarks")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#sidebar-changed")).ToHaveTextAsync("Bookmarks");
        var tree = Page.Locator("#pdv-main .bit-pdv-outline");
        await Expect(tree).ToHaveAttributeAsync("role", "tree");
        var top = tree.Locator(":scope > ul > li[role='treeitem']");
        await Expect(top).ToHaveCountAsync(3);
        await Expect(top.Locator(":scope > .bit-pdv-outline-row .bit-pdv-outline-item")).ToHaveTextAsync(["Chapter One", "Chapter Two", "Chapter Three"]);

        var chapterTwo = top.Nth(1);
        var expanded = await chapterTwo.GetAttributeAsync("aria-expanded");
        Assert.IsNotNull(expanded);
        if (expanded == "false") await chapterTwo.Locator(".bit-pdv-outline-toggle").ClickAsync();
        await Expect(chapterTwo).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(chapterTwo.Locator("li[role='treeitem']")).ToHaveTextAsync("Section Two A");

        await top.Nth(2).ClickAsync();
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Expect(top.Nth(2)).ToHaveAttributeAsync("aria-selected", "true");

        // The arrow keys fold the tree.
        await chapterTwo.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(chapterTwo).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(chapterTwo.Locator("li[role='treeitem']")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(chapterTwo).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Sidebar_LayersShowAndHideTheirContent()
    {
        await OpenLoadedAsync();

        await Btn("pdv-main", "Layers").ClickAsync();
        var layers = Page.Locator("#pdv-main .bit-pdv-layers .bit-pdv-layer");
        await Expect(layers).ToHaveCountAsync(2);
        await Expect(layers.Nth(0)).ToContainTextAsync("Base drawing");
        await Expect(layers.Nth(0).Locator("input")).ToBeCheckedAsync();
        await Expect(layers.Nth(1)).ToContainTextAsync("Notes");
        await Expect(layers.Nth(1).Locator("input")).Not.ToBeCheckedAsync();

        await Expect(PageOf("pdv-main", 1)).ToContainTextAsync("Base layer text");
        await Expect(PageOf("pdv-main", 1)).Not.ToContainTextAsync("Notes layer text");

        await layers.Nth(1).Locator("input").CheckAsync();
        await Expect(PageOf("pdv-main", 1)).ToContainTextAsync("Notes layer text");
        await layers.Nth(0).Locator("input").UncheckAsync();
        await Expect(PageOf("pdv-main", 1)).Not.ToContainTextAsync("Base layer text");
    }

    [TestMethod]
    public async Task Sidebar_AttachmentsListAndDownloadTheEmbeddedFiles()
    {
        await OpenLoadedAsync();

        await Btn("pdv-main", "Attachments").ClickAsync();
        var list = Page.Locator("#pdv-main .bit-pdv-attachments");
        await Expect(list).ToHaveAttributeAsync("role", "list");
        var item = list.Locator("button.bit-pdv-attachment");
        await Expect(item.Locator(".bit-pdv-attachment-name")).ToHaveTextAsync("notes.txt");
        await Expect(item).ToHaveAttributeAsync("title", "Some notes");

        var download = await Page.RunAndWaitForDownloadAsync(() => item.ClickAsync());
        Assert.AreEqual("notes.txt", download.SuggestedFilename);
        var path = await download.PathAsync();
        Assert.AreEqual("hello attachment", await File.ReadAllTextAsync(path!));
    }

    [TestMethod]
    public async Task Properties_TheDialogDescribesTheDocument_AndEscapeClosesIt()
    {
        await OpenLoadedAsync();

        await Btn("pdv-main", "Document properties").ClickAsync();
        var dialog = Page.Locator("#pdv-main [role='dialog']");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Page.Locator($"#{await dialog.GetAttributeAsync("aria-labelledby")}")).ToHaveTextAsync("Document properties");

        async Task ExpectRow(string label, string value) =>
            await Expect(dialog.Locator("dl > div", new() { Has = Page.Locator("dt", new() { HasTextString = label }) }).Locator("dd").First).ToHaveTextAsync(value);

        await ExpectRow("File name", "pdf-sample.pdf");
        await ExpectRow("Title", "E2E Sample Document");
        await ExpectRow("Author", "Agent Twenty");
        await ExpectRow("Subject", "Viewer testing");
        await ExpectRow("Keywords", "pdf e2e sample");
        await ExpectRow("Creator", "e2e generator");
        await ExpectRow("Producer", "python stdlib");
        await ExpectRow("PDF version", "1.7");
        await ExpectRow("Page count", "3");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);

        await Btn("pdv-main", "Document properties").ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.Locator("button[aria-label='Close']").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Download_SavesTheDocument_UnderTheNameOnDownloadingGives_OrNotAtAllWhenCancelled()
    {
        await OpenLoadedAsync();

        var download = await Page.RunAndWaitForDownloadAsync(() => Btn("pdv-main", "Download document").ClickAsync());
        Assert.AreEqual("renamed-sample.pdf", download.SuggestedFilename);
        await Expect(Page.Locator("#downloading")).ToHaveTextAsync("pdf-sample.pdf");
        var bytes = await File.ReadAllBytesAsync((await download.PathAsync())!);
        CollectionAssert.AreEqual(await SampleBytesAsync(), bytes);

        // Ctrl+S from the surface downloads too.
        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        download = await Page.RunAndWaitForDownloadAsync(() => Page.Keyboard.PressAsync("Control+s"));
        Assert.AreEqual("renamed-sample.pdf", download.SuggestedFilename);

        // A cancelled download saves nothing: the next download the page sees is the one made after it.
        await Page.ClickAsync("#btn-cancel-download");
        var downloads = new List<string>();
        Page.Download += (_, d) => downloads.Add(d.SuggestedFilename);
        await Btn("pdv-main", "Download document").ClickAsync();
        await Expect(Page.Locator("#btn-cancel-download")).ToHaveTextAsync("Cancel download: True");
        await Page.ClickAsync("#btn-cancel-download");
        await Expect(Page.Locator("#btn-cancel-download")).ToHaveTextAsync("Cancel download: False");
        await Page.RunAndWaitForDownloadAsync(() => Btn("pdv-main", "Download document").ClickAsync());
        Assert.AreEqual(1, downloads.Count, "a cancelled download was saved");
    }

    [TestMethod]
    public async Task Print_PrintsTheRangeOnPrintingIsTold_OrNothingWhenCancelled()
    {
        await Page.AddInitScriptAsync(@"
            if (window.top !== window) return;
            window.__printCalls = [];
            new MutationObserver(records => {
                for (const record of records) for (const node of record.addedNodes) {
                    if (node.tagName !== 'IFRAME') continue;
                    const hook = () => { try { node.contentWindow.print = () => window.__printCalls.push(node.contentDocument.body.innerText); } catch { } };
                    hook(); setTimeout(hook, 0); setTimeout(hook, 50);
                }
            }).observe(document, { childList: true, subtree: true });");
        await OpenLoadedAsync();

        // Every page has to be drawn before it can be printed.
        await Btn("pdv-main", "Print document").ClickAsync();
        await Expect(Page.Locator("#printing")).ToHaveTextAsync("1-3");
        await Page.WaitForFunctionAsync("() => window.__printCalls.length === 1");
        var printed = await Page.EvaluateAsync<string>("() => window.__printCalls[0]");
        StringAssert.Contains(printed, "Page One Heading");
        StringAssert.Contains(printed, "Page Three Heading");

        await Page.ClickAsync("#btn-api-next");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-api-print-current");
        await Expect(Page.Locator("#printing")).ToHaveTextAsync("2-2");
        await Page.WaitForFunctionAsync("() => window.__printCalls.length === 2");
        printed = await Page.EvaluateAsync<string>("() => window.__printCalls[1]");
        StringAssert.Contains(printed, "Page Two Heading");
        Assert.IsFalse(printed.Contains("Page One Heading"), "printing the current page printed another one");

        // A cancelled print prints nothing: the next print the page sees is the one made after it.
        await Page.ClickAsync("#btn-cancel-print");
        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        await Page.Keyboard.PressAsync("Control+p");
        await Expect(Page.Locator("#printing")).ToHaveTextAsync("1-3");
        await Page.ClickAsync("#btn-cancel-print");
        await Expect(Page.Locator("#btn-cancel-print")).ToHaveTextAsync("Cancel print: False");
        await Page.ClickAsync("#btn-api-print-current");
        await Page.WaitForFunctionAsync("() => window.__printCalls.length >= 3");
        Assert.AreEqual(3, await Page.EvaluateAsync<int>("() => window.__printCalls.length"), "a cancelled print was printed");
        StringAssert.Contains(await Page.EvaluateAsync<string>("() => window.__printCalls[2]"), "Page Two Heading");
    }

    [TestMethod]
    public async Task Fullscreen_TheButtonEntersAndLeavesTheBrowsersFullscreen()
    {
        await OpenLoadedAsync();

        var button = Btn("pdv-main", "Enter fullscreen");
        await button.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.fullscreenElement && document.fullscreenElement.id === 'pdv-main'");
        var exit = Btn("pdv-main", "Exit fullscreen");
        await Expect(exit).ToHaveAttributeAsync("aria-pressed", "true");

        await exit.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.fullscreenElement === null");
        await Expect(Btn("pdv-main", "Enter fullscreen")).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    public async Task Presentation_ShowsOnePageAtATime_AndTheArrowsTurnIt()
    {
        await OpenLoadedAsync();

        await Btn("pdv-main", "Presentation mode").ClickAsync();
        await Expect(Viewer("pdv-main")).ToHaveClassAsync(HasClass("bit-pdv-presenting"));
        await Expect(Page.Locator("#pdv-main .bit-pdv-pages")).ToHaveClassAsync(HasClass("bit-pdv-single"));

        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => document.fullscreenElement?.id === 'pdv-main'"), "presenting is not fullscreen");
        await Page.EvaluateAsync("() => document.exitFullscreen()");
        await Expect(Viewer("pdv-main")).Not.ToHaveClassAsync(HasClass("bit-pdv-presenting"));
        await Expect(Page.Locator("#pdv-main .bit-pdv-pages")).Not.ToHaveClassAsync(HasClass("bit-pdv-single"));

        await Page.Locator("#pdv-main .bit-pdv-surface").FocusAsync();
        await Page.Keyboard.PressAsync("Control+Alt+p");
        await Expect(Viewer("pdv-main")).ToHaveClassAsync(HasClass("bit-pdv-presenting"));
        await Page.ClickAsync("#btn-api-presentation", new() { Force = true });
        await Expect(Viewer("pdv-main")).Not.ToHaveClassAsync(HasClass("bit-pdv-presenting"));
    }

    [TestMethod]
    public async Task Layout_ScrollAndSpreadModesLayThePagesOut()
    {
        await OpenLoadedAsync();

        await Page.Locator("#pdv-main select[aria-label='Zoom level']").SelectOptionAsync("z0.5");
        var scroll = Page.Locator("#pdv-main select[aria-label='Scroll mode']");
        var spread = Page.Locator("#pdv-main select[aria-label='Spread mode']");

        async Task<(PageBoxes One, PageBoxes Two)> BoxesAsync()
        {
            var one = (await PageOf("pdv-main", 1).BoundingBoxAsync())!;
            var two = (await PageOf("pdv-main", 2).BoundingBoxAsync())!;
            return (new(one.X, one.Y, one.Width), new(two.X, two.Y, two.Width));
        }

        var (v1, v2) = await BoxesAsync();
        Assert.IsTrue(v2.Y > v1.Y + 100, "vertical scrolling does not stack the pages");

        await scroll.SelectOptionAsync("Horizontal");
        await Expect(scroll).ToHaveValueAsync("Horizontal");
        await Page.WaitForFunctionAsync("() => { const a = document.querySelector('#pdv-main [data-page=\"1\"]').getBoundingClientRect(); const b = document.querySelector('#pdv-main [data-page=\"2\"]').getBoundingClientRect(); return b.left > a.right - 1 && Math.abs(b.top - a.top) < 2; }");

        await scroll.SelectOptionAsync("Page");
        await Expect(Page.Locator("#pdv-main .bit-pdv-pages")).ToHaveClassAsync(HasClass("bit-pdv-single"));

        await scroll.SelectOptionAsync("Vertical");
        await spread.SelectOptionAsync("Odd");
        var rows = Page.Locator("#pdv-main .bit-pdv-row");
        await Expect(rows).ToHaveCountAsync(2);
        await Expect(rows.First.Locator("[data-page]")).ToHaveCountAsync(2);
        var (s1, s2) = await BoxesAsync();
        Assert.IsTrue(s2.X > s1.X + s1.Width - 1 && Math.Abs(s2.Y - s1.Y) < 2, "an odd spread does not put pages 1 and 2 side by side");

        await spread.SelectOptionAsync("Even");
        await Expect(rows.First.Locator("[data-page]")).ToHaveCountAsync(1);
        await spread.SelectOptionAsync("None");
        await Expect(rows).ToHaveCountAsync(0);

        // The initial modes are parameters too.
        await Expect(Page.Locator("#pdv-layout select[aria-label='Scroll mode']")).ToHaveValueAsync("Wrapped");
        await Expect(Page.Locator("#pdv-layout select[aria-label='Spread mode']")).ToHaveValueAsync("Odd");
        await Expect(Page.Locator("#pdv-layout .bit-pdv-row")).ToHaveCountAsync(2);
    }

    private sealed record PageBoxes(float X, float Y, float Width);

    [TestMethod]
    public async Task PanTool_DraggingScrollsTheDocument()
    {
        await OpenLoadedAsync();

        // The initial cursor tool is a parameter.
        await Expect(Page.Locator("#pdv-layout .bit-pdv-surface")).ToHaveClassAsync(HasClass("bit-pdv-pan"));
        await Expect(Btn("pdv-layout", "Pan tool")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator("#pdv-main select[aria-label='Zoom level']").SelectOptionAsync("z1");
        var surface = Page.Locator("#pdv-main .bit-pdv-surface");
        await Btn("pdv-main", "Pan tool").ClickAsync();
        await Expect(surface).ToHaveClassAsync(HasClass("bit-pdv-pan"));
        await Expect(Btn("pdv-main", "Pan tool")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(surface).ToHaveCSSAsync("cursor", new Regex("grab"));

        await surface.ScrollIntoViewIfNeededAsync();
        var before = await surface.EvaluateAsync<double>("s => s.scrollTop");
        var box = (await surface.BoundingBoxAsync())!;
        var bottom = Math.Min(box.Y + box.Height, Page.ViewportSize!.Height) - 20;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, bottom);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + 20, new() { Steps = 10 });
        await Page.Mouse.UpAsync();
        // The surface scrolls smoothly, so the drag's distance is reached once the scroll settles.
        await Page.WaitForFunctionAsync($"() => document.querySelector('#pdv-main .bit-pdv-surface').scrollTop > {before + 50}");

        await Btn("pdv-main", "Pan tool").ClickAsync();
        await Expect(surface).Not.ToHaveClassAsync(HasClass("bit-pdv-pan"));
    }

    [TestMethod]
    public async Task Api_TheNavigationZoomLayoutAndSearchMethods()
    {
        await OpenLoadedAsync();

        await Page.ClickAsync("#btn-api-goto");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-api-next");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Page.ClickAsync("#btn-api-first");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-api-last");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("3");
        await Page.ClickAsync("#btn-api-prev");
        await Expect(Page.Locator("#page-value")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-api-set-zoom");
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-api-zoom-in");
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("2.4");
        await Page.ClickAsync("#btn-api-zoom-out");
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-api-actual");
        await Expect(Page.Locator("#zoom-value")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-api-rotate-cw");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("90");
        await Page.ClickAsync("#btn-api-rotate-ccw");
        await Expect(Page.Locator("#rotation-value")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-api-horizontal");
        await Page.ClickAsync("#btn-api-spread-odd");
        await Page.ClickAsync("#btn-api-pan");
        await Page.ClickAsync("#btn-api-bookmarks");
        await Page.ClickAsync("#btn-api-search");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("1/3");
        await Page.ClickAsync("#btn-api-find-next");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("2/3");
        await Page.ClickAsync("#btn-api-find-prev");
        await Expect(Page.Locator("#pdv-main .bit-pdv-search-count")).ToHaveTextAsync("1/3");

        await Page.ClickAsync("#btn-api-info");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync(new Regex("^info:"));
        var info = (await Page.Locator("#api-result").TextContentAsync())!;
        var sample = await SampleBytesAsync();
        StringAssert.StartsWith(info, $"info:3|E2E Sample Document|Agent Twenty|False|1.7|{sample.Length}|True:3|True:1|True:2|{sample.Length}|ActualSize|Horizontal|Odd|Pan|Bookmarks|True:keyword:1/3|False");

        // ClearSearch empties the query and its matches; the box stays where the reader left it.
        await Page.ClickAsync("#btn-api-clear-search");
        await Page.ClickAsync("#btn-api-info");
        await Expect(Page.Locator("#api-result")).ToContainTextAsync("|True::0/0|");
        await Page.WaitForFunctionAsync("() => (CSS.highlights.get('bit-pdv-search-current')?.size ?? 0) === 0");
    }

    [TestMethod]
    public async Task Api_ExtractTextAndOpenAsync()
    {
        await OpenLoadedAsync();

        await Page.ClickAsync("#btn-api-text");
        // The extraction is a round trip: the result is waited for before it is read.
        await Expect(Page.Locator("#api-result")).ToContainTextAsync("text:");
        var text = (await Page.Locator("#api-result").TextContentAsync())!;
        StringAssert.Contains(text, "text:");
        var parts = text["text:".Length..].Split("##");
        StringAssert.Contains(parts[0], "Page Two Heading");
        Assert.IsFalse(parts[0].Contains("Page One Heading"), "ExtractPageText(2) read page 1");
        StringAssert.Contains(parts[1], "Page One Heading");
        StringAssert.Contains(parts[1], " | ");
        StringAssert.Contains(parts[1], "Page Three Heading");

        await Page.ClickAsync("#btn-api-open-bytes");
        await Expect(Page.Locator("#pdv-main .bit-pdv-title")).ToHaveTextAsync("opened-by-api.pdf");
        await Expect(Page.Locator("#loaded-count")).ToHaveTextAsync("2");
        await Expect(PageOf("pdv-main", 1)).ToContainTextAsync("Page One Heading");
    }

    [TestMethod]
    public async Task Encrypted_ThePasswordDialogRejectsAWrongPasswordAndOpensWithTheRightOne()
    {
        await OpenAsync(Route);

        var dialog = Page.Locator("#pdv-encrypted [role='dialog']");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog).ToContainTextAsync("This document is protected. Enter its password to open it.");
        var input = dialog.Locator("input[type='password']");
        await Expect(input).ToBeFocusedAsync();

        await input.FillAsync("wrong");
        await input.PressAsync("Enter");
        await Expect(dialog).ToContainTextAsync("That password was not accepted. Try again.");
        await Expect(dialog.Locator("input[type='password']")).ToHaveAttributeAsync("aria-invalid", "true");

        await dialog.Locator("input[type='password']").FillAsync("hunter2");
        await dialog.Locator("button", new() { HasTextString = "Open" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(PageOf("pdv-encrypted", 1)).ToContainTextAsync("Unlocked secret content.");
    }

    [TestMethod]
    public async Task Encrypted_CancellingThePrompt_ReportsTheFailure()
    {
        await OpenAsync(Route);

        var dialog = Page.Locator("#pdv-encrypted [role='dialog']");
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.Locator("button", new() { HasTextString = "Cancel" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.Locator("#encrypted-error")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#pdv-encrypted .bit-pdv-error")).ToHaveAttributeAsync("role", "alert");
    }

    [TestMethod]
    public async Task Encrypted_AKnownPasswordACallbackAndNoPrompt()
    {
        await OpenAsync(Route);

        await Expect(PageOf("pdv-encrypted-known", 1)).ToContainTextAsync("Unlocked secret content.");
        await Expect(PageOf("pdv-encrypted-callback", 1)).ToContainTextAsync("Unlocked secret content.");
        await Expect(Page.Locator("#password-asked")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#pdv-encrypted-callback [role='dialog']")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#noprompt-error")).ToContainTextAsync("password");
        await Expect(Page.Locator("#pdv-encrypted-noprompt [role='dialog']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pdv-encrypted-noprompt .bit-pdv-error")).ToContainTextAsync("password");
    }

    [TestMethod]
    public async Task States_EmptyErrorAndMissing_WithAndWithoutTemplates()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#pdv-empty .bit-pdv-empty")).ToHaveTextAsync("No document loaded.");
        await Expect(Page.Locator("#pdv-empty-tpl .tpl-empty")).ToHaveTextAsync("Nothing yet");
        await Expect(Btn("pdv-empty", "Next page")).ToBeDisabledAsync();

        var broken = Page.Locator("#pdv-broken .bit-pdv-error");
        await Expect(broken).ToHaveAttributeAsync("role", "alert");
        await Expect(broken).ToContainTextAsync("Error:");
        await Expect(Page.Locator("#broken-error")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#pdv-broken-tpl .tpl-error")).ToContainTextAsync("Failed: ");

        await Expect(Page.Locator("#pdv-missing .bit-pdv-error")).ToContainTextAsync("Failed to fetch document");
        await Expect(Page.Locator("#missing-error")).ToContainTextAsync("404");
    }

    [TestMethod]
    public async Task States_TheLoadingTemplateShowsWhileTheDocumentIsFetched()
    {
        var sample = await SampleBytesAsync();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var release = new TaskCompletionSource();
        var serve = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            var stream = client.GetStream();
            var buffer = new byte[8192];
            await stream.ReadAsync(buffer);
            await release.Task;
            var head = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/pdf\r\nContent-Length: {sample.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(head);
            await stream.WriteAsync(sample);
            await stream.FlushAsync();
        });

        try
        {
            await OpenAsync($"{Route}?slowUrl={Uri.EscapeDataString($"http://127.0.0.1:{port}/slow.pdf")}");
            await Page.ClickAsync("#btn-load-slow");
            await Expect(Page.Locator("#pdv-slow .tpl-loading")).ToHaveTextAsync("Loading please wait");
            await Expect(Page.Locator("#pdv-slow .bit-pdv-surface")).ToHaveAttributeAsync("aria-busy", "true");

            release.SetResult();
            await Expect(PageOf("pdv-slow", 1)).ToContainTextAsync("Page One Heading");
            await Expect(Page.Locator("#pdv-slow .tpl-loading")).ToHaveCountAsync(0);
            await serve;
        }
        finally
        {
            release.TrySetResult();
            listener.Stop();
        }
    }

    [TestMethod]
    public async Task OpenFile_ThePickerAndADropOpenADocument_AndTheSizeLimitAndANonPdfAreRefused()
    {
        await OpenAsync(Route);

        var sample = await SampleBytesAsync();
        var input = Page.Locator("#pdv-open input.bit-pdv-file");
        await Expect(input).ToHaveAttributeAsync("accept", "application/pdf,.pdf");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Open file");

        await input.SetInputFilesAsync(new FilePayload { Name = "picked.pdf", MimeType = "application/pdf", Buffer = sample });
        await Expect(PageOf("pdv-open", 1)).ToContainTextAsync("Page One Heading");
        await Expect(Page.Locator("#opened-name")).ToHaveTextAsync("picked.pdf");
        await Expect(Page.Locator("#pdv-open .bit-pdv-title")).ToHaveTextAsync("picked.pdf");

        await input.SetInputFilesAsync(new FilePayload { Name = "text.pdf", MimeType = "application/pdf", Buffer = Encoding.ASCII.GetBytes("not a pdf at all") });
        await Expect(Page.Locator("#pdv-open .bit-pdv-error")).ToBeVisibleAsync();
        await Expect(Page.Locator("#open-error")).Not.ToBeEmptyAsync();

        var big = new byte[200_000];
        sample.CopyTo(big, 0);
        await input.SetInputFilesAsync(new FilePayload { Name = "big.pdf", MimeType = "application/pdf", Buffer = big });
        await Expect(Page.Locator("#open-error")).ToContainTextAsync(new Regex("big\\.pdf|size|large|MB|KB", RegexOptions.IgnoreCase));

        // With no OnFileOpened, the viewer opens a picked file itself.
        await Page.Locator("#pdv-open-self input.bit-pdv-file").SetInputFilesAsync(new FilePayload { Name = "self.pdf", MimeType = "application/pdf", Buffer = sample });
        await Expect(PageOf("pdv-open-self", 1)).ToContainTextAsync("Page One Heading");
        await Expect(Page.Locator("#pdv-open-self .bit-pdv-title")).ToHaveTextAsync("self.pdf");

        // A file dropped on the viewer is opened the same way.
        await Page.Locator("#pdv-open").EvaluateAsync(@"(root, bytes) => {
            const dt = new DataTransfer();
            dt.items.add(new File([new Uint8Array(bytes)], 'dropped.pdf', { type: 'application/pdf' }));
            const target = root.querySelector('.bit-pdv-surface');
            target.dispatchEvent(new DragEvent('dragenter', { dataTransfer: dt, bubbles: true, cancelable: true }));
            target.dispatchEvent(new DragEvent('dragover', { dataTransfer: dt, bubbles: true, cancelable: true }));
            window.__pdvDropping = root.classList.contains('bit-pdv-dropping') || !!root.querySelector('.bit-pdv-dropping');
            target.dispatchEvent(new DragEvent('drop', { dataTransfer: dt, bubbles: true, cancelable: true }));
        }", sample.Select(b => (int)b).ToArray());
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__pdvDropping"), "a file over the viewer does not mark it as a drop target");
        await Expect(Page.Locator("#opened-name")).ToHaveTextAsync("dropped.pdf");
        await Expect(PageOf("pdv-open", 1)).ToContainTextAsync("Page One Heading");
    }

    [TestMethod]
    [DataRow("None", new string[0])]
    [DataRow("Thumbnails", new[] { "Page thumbnails" })]
    [DataRow("Bookmarks", new[] { "Bookmarks" })]
    [DataRow("Navigation", new[] { "Previous page", "Next page" })]
    [DataRow("FirstLastPage", new[] { "First page", "Last page" })]
    [DataRow("Zoom", new[] { "Zoom out", "Zoom in" })]
    [DataRow("Search", new[] { "Find in document" })]
    [DataRow("Rotate", new[] { "Rotate counter clockwise", "Rotate clockwise" })]
    [DataRow("Download", new[] { "Download document" })]
    [DataRow("Print", new[] { "Print document" })]
    [DataRow("Fullscreen", new[] { "Enter fullscreen" })]
    [DataRow("Properties", new[] { "Document properties" })]
    [DataRow("CursorTool", new[] { "Pan tool" })]
    [DataRow("Attachments", new[] { "Attachments" })]
    [DataRow("Presentation", new[] { "Presentation mode" })]
    [DataRow("Layers", new[] { "Layers" })]
    public async Task Toolbar_EachItem_RendersItsControls(string item, string[] labels)
    {
        await OpenLoadedAsync("pdv-items");

        await Page.Locator("#toolbar-select").SelectOptionAsync(item);
        await Expect(Page.Locator("#toolbar-name")).ToHaveTextAsync(item);

        var names = await Page.Locator("#pdv-items .bit-pdv-toolbar button[aria-label]").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('aria-label'))");
        CollectionAssert.AreEquivalent(labels, names, $"{item}: {string.Join(", ", names)}");

        // The templates stay at both ends whatever the items.
        await Expect(Page.Locator("#pdv-items .tpl-start")).ToHaveTextAsync("Start 3");
        await Expect(Page.Locator("#pdv-items .tpl-end")).ToHaveTextAsync("End 1");
    }

    [TestMethod]
    public async Task Toolbar_TheOtherItemsAndTheTemplates()
    {
        await OpenLoadedAsync("pdv-items");

        var select = Page.Locator("#toolbar-select");
        await select.SelectOptionAsync("Title");
        await Expect(Page.Locator("#pdv-items .bit-pdv-title")).ToHaveTextAsync("pdf-sample.pdf");
        await select.SelectOptionAsync("Layout");
        await Expect(Page.Locator("#pdv-items select[aria-label='Scroll mode']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#pdv-items select[aria-label='Spread mode']")).ToHaveCountAsync(1);
        await select.SelectOptionAsync("OpenFile");
        await Expect(Page.Locator("#pdv-items input.bit-pdv-file")).ToHaveCountAsync(1);
        await select.SelectOptionAsync("Navigation");
        await Expect(Page.Locator("#pdv-items .bit-pdv-page-input")).ToHaveCountAsync(1);

        // A template gets the viewer.
        await Page.Locator("#pdv-items .tpl-start").ClickAsync();
        await Expect(Page.Locator("#pdv-items .tpl-end")).ToHaveTextAsync("End 3");

        // No toolbar at all.
        await Expect(Page.Locator("#pdv-notoolbar .bit-pdv-toolbar")).ToHaveCountAsync(0);
        await Expect(PageOf("pdv-notoolbar", 1)).ToContainTextAsync("Page One Heading");
    }

    [TestMethod]
    public async Task Dimensions_HeightAndWidthSizeTheViewer()
    {
        await OpenAsync(Route);

        var box = (await Viewer("pdv-dims").BoundingBoxAsync())!;
        Assert.AreEqual(400, box.Height, 2);
        Assert.AreEqual(500, box.Width, 2);
        StringAssert.Contains(await Viewer("pdv-dims").GetAttributeAsync("style"), "400px");
    }

    [TestMethod]
    public async Task Localization_TextsReplaceTheStrings()
    {
        await OpenLoadedAsync("pdv-localized");

        await Expect(Page.Locator("#pdv-localized .bit-pdv-toolbar")).ToHaveAttributeAsync("aria-label", "PDF-Werkzeugleiste");
        await Expect(Btn("pdv-localized", "Nächste Seite")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-localized", "Vorherige Seite")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-localized", "Vergrößern")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-localized", "Verkleinern")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-localized", "Herunterladen")).ToHaveCountAsync(1);
        await Expect(PageOf("pdv-localized", 1)).ToHaveAttributeAsync("aria-label", "Seite 1");
        // A string it leaves alone stays English.
        await Expect(Btn("pdv-localized", "Print document")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Disabled_TurnsTheViewerOff_AndDropsThePagesActivationHandlers()
    {
        await OpenLoadedAsync("pdv-disabled");

        var root = Viewer("pdv-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#pdv-disabled .bit-pdv-toolbar")).ToHaveAttributeAsync("inert", "");
        await Expect(Page.Locator("#pdv-disabled .bit-pdv-outline")).ToHaveAttributeAsync("inert", "");
        await Btn("pdv-disabled", "Next page").ClickAsync(new() { Force = true });

        var surface = Page.Locator("#pdv-disabled .bit-pdv-surface");
        await surface.ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");
        await surface.FocusAsync();
        await Page.Keyboard.PressAsync("n");
        await surface.HoverAsync(new() { Force = true });
        await Expect(Page.Locator("#disabled-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-keys")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#pdv-disabled .bit-pdv-page-input")).ToHaveValueAsync("1");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#pdv-disabled .bit-pdv-toolbar")).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Btn("pdv-disabled", "Next page").ClickAsync();
        await Expect(Page.Locator("#pdv-disabled .bit-pdv-page-input")).ToHaveValueAsync("2");
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Cascading_BitParams_SetDefaultsAndAViewersOwnValueWins()
    {
        await OpenLoadedAsync("pdv-cascaded");

        Assert.AreEqual(350, (await Viewer("pdv-cascaded").BoundingBoxAsync())!.Height, 2);
        await Expect(Page.Locator("#pdv-cascaded .bit-pdv-toolbar")).ToHaveClassAsync(HasClass("cascaded-toolbar"));
        await Expect(Btn("pdv-cascaded", "Next page")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-cascaded", "Zoom in")).ToHaveCountAsync(1);
        await Expect(Btn("pdv-cascaded", "Download document")).ToHaveCountAsync(0);

        await Expect(Btn("pdv-cascaded-own", "Download document")).ToHaveCountAsync(1);
        Assert.AreEqual(350, (await Viewer("pdv-cascaded-own").BoundingBoxAsync())!.Height, 2);
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheirParts()
    {
        await OpenLoadedAsync("pdv-styles");

        await Expect(Viewer("pdv-style")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Viewer("pdv-style")).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(Viewer("pdv-style")).ToHaveCSSAsync("margin-top", "7px");

        var root = Viewer("pdv-styles");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(1, 1, 1)");
        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            (".bit-pdv-toolbar", "c-toolbar", "background-color", "rgb(2, 2, 2)"),
            (".bit-pdv-toolbar button.bit-pdv-btn", "c-button", "color", "rgb(3, 3, 3)"),
            (".bit-pdv-title", "c-title", "color", "rgb(4, 4, 4)"),
            (".bit-pdv-body", "c-body", "padding-top", "3px"),
            (".bit-pdv-thumbs", "c-thumbs", "background-color", "rgb(5, 5, 5)"),
            (".bit-pdv-thumb", "c-thumb", "outline-color", "rgb(6, 6, 6)"),
            (".bit-pdv-surface", "c-surface", "background-color", "rgb(7, 7, 7)"),
            (".bit-pdv-pages > .bit-pdv-page", "c-page", "outline-color", "rgb(8, 8, 8)"),
        };
        foreach (var (selector, cls, property, value) in parts)
        {
            var part = root.Locator(selector).First;
            await Expect(part).ToHaveClassAsync(HasClass(cls));
            await Expect(part).ToHaveCSSAsync(property, value);
        }
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheViewer()
    {
        await OpenLoadedAsync("pdv-vars");

        var checks = new (string Selector, string Property, string Expected)[]
        {
            ("", "color", "rgb(11, 12, 13)"),
            ("", "border-top-color", "rgb(1, 2, 3)"),
            ("", "border-top-width", "4px"),
            ("", "border-top-left-radius", "13px"),
            ("", "height", "333px"),
            ("", "width", "444px"),
            (" .bit-pdv-toolbar", "background-color", "rgb(31, 32, 33)"),
            (" .bit-pdv-toolbar", "border-bottom-color", "rgb(4, 5, 6)"),
            (" .bit-pdv-toolbar button.bit-pdv-btn", "width", "41px"),
            (" .bit-pdv-toolbar button.bit-pdv-btn", "border-top-left-radius", "9px"),
            (" .bit-pdv-page-input", "background-color", "rgb(41, 42, 43)"),
            (" .bit-pdv-page-input", "border-top-color", "rgb(51, 52, 53)"),
            ("", "background-color", "rgb(21, 22, 23)"),
            (" .bit-pdv-pages > .bit-pdv-page", "background-color", "rgb(61, 62, 63)"),
            (" .bit-pdv-pages > .bit-pdv-page", "filter", "invert(1)"),
        };

        foreach (var (selector, property, expected) in checks)
        {
            Assert.AreEqual(expected, await ComputedStyleAsync($"#pdv-vars{selector}", property), $"{selector} {property}");
            Assert.AreNotEqual(expected, await ComputedStyleAsync($"#pdv-vars-default{selector}", property), $"default {selector} {property}");
        }

        // The padding around the pages and the gap between them.
        Assert.AreEqual("27px", await ComputedStyleAsync("#pdv-vars .bit-pdv-surface", "padding-top"));
        Assert.AreEqual("31px", await ComputedStyleAsync("#pdv-vars .bit-pdv-pages", "row-gap"));
        var one = (await PageOf("pdv-vars", 1).BoundingBoxAsync())!;
        var two = (await PageOf("pdv-vars", 2).BoundingBoxAsync())!;
        Assert.AreEqual(31, two.Y - (one.Y + one.Height), 2, "page gap");
    }

    [TestMethod]
    public async Task Rtl_FlipsTheChrome_ButKeepsTheNumbersLeftToRight()
    {
        await OpenLoadedAsync("pdv-rtl");

        var root = Viewer("pdv-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "pdv-rtl", "pdv-rtl-inherited" })
        {
            var toolbar = (await Page.Locator($"#{id} .bit-pdv-toolbar").BoundingBoxAsync())!;
            var first = (await Btn(id, "Page thumbnails").BoundingBoxAsync())!;
            Assert.IsTrue(first.X > toolbar.X + toolbar.Width / 2, $"{id}: the first group is not on the right");

            // The page box and its arrows read left to right, as numbers do.
            var prev = (await Btn(id, "Previous page").BoundingBoxAsync())!;
            var next = (await Btn(id, "Next page").BoundingBoxAsync())!;
            Assert.IsTrue(prev.X < next.X, $"{id}: the page arrows were flipped");
        }
    }

    [TestMethod]
    public async Task Base_VisibilityHtmlAttributesAndAriaLabel_ReachTheRoot()
    {
        await OpenLoadedAsync("pdv-visibility");

        var root = Viewer("pdv-visibility");
        await Expect(root).ToHaveAttributeAsync("data-test", "pdv-data");
        await Expect(root).ToHaveAttributeAsync("title", "Viewer title");
        await Expect(Page.Locator("#pdv-visibility .bit-pdv-surface")).ToHaveAttributeAsync("aria-label", new Regex("Quarterly report|pdf-sample\\.pdf"));

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Page.ClickAsync("#btn-visibility");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_StopsTheSmoothScroll_UnlessForceAnimationIsSet()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenLoadedAsync();

        await Expect(Page.Locator("#pdv-main .bit-pdv-surface")).ToHaveCSSAsync("scroll-behavior", "auto");
        await Expect(Page.Locator("#pdv-zoom-actual")).ToHaveClassAsync(HasClass("bit-fam"));
        await Expect(Page.Locator("#pdv-zoom-actual .bit-pdv-surface")).ToHaveCSSAsync("scroll-behavior", "smooth");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Expect(Page.Locator("#pdv-main .bit-pdv-surface")).ToHaveCSSAsync("scroll-behavior", "smooth");
    }

    [TestMethod]
    public async Task Narrow_TheToolbarStaysOneRowThatScrollsSideways()
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenLoadedAsync();

        var toolbar = Page.Locator("#pdv-main .bit-pdv-toolbar");
        await Expect(toolbar).ToHaveCSSAsync("flex-wrap", "nowrap");
        await Expect(toolbar).ToHaveCSSAsync("overflow-x", "auto");
        Assert.IsTrue(await toolbar.EvaluateAsync<bool>("t => t.scrollWidth > t.clientWidth"), "the toolbar does not scroll on a phone");

        // Its controls keep their real size, and a quarter of the row stays on one line.
        var next = (await Btn("pdv-main", "Next page").BoundingBoxAsync())!;
        var download = (await Btn("pdv-main", "Download document").BoundingBoxAsync())!;
        Assert.AreEqual(next.Height, download.Height, 1);
        Assert.AreEqual(next.Y, download.Y, 2, "the toolbar wrapped");
    }

    [TestMethod]
    public async Task Lifecycle_Unmount_DisposesWithoutAnError()
    {
        await OpenLoadedAsync("pdv-unmount");

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Viewer("pdv-unmount")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(PageOf("pdv-unmount", 1)).ToContainTextAsync("Page One Heading");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
