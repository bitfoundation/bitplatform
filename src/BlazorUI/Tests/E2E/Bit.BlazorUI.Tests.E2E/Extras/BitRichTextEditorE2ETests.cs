using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitRichTextEditor on <c>/e2e/rich-text-editor</c>: typing into the contenteditable surface and
/// the two-way HTML Value (debounced, OnChange, set and cleared by the page, sanitized on the way in), the placeholder,
/// Height/MaxHeight/Resizable/SpellCheck, every BitRichTextEditorToolbar group, every formatting command applied to a
/// selection from the toolbar and from the keyboard (inline marks, scripts, block formats, lists, task lists, indent,
/// quote and code blocks, alignment, direction, rules, clear formatting, undo/redo, fonts and colors with and without a
/// palette), the link, image, table, media, find, emoji, color and help panels, the source view, full screen,
/// custom toolbar items and order, custom keyboard shortcuts, the selection toolbar, mentions, the slash menu, the
/// markdown input rules and SmartTypography, the sanitization policy and plain-text paste, image upload, the form
/// contract (Label, Required, Description, ShowCount/MaxLength, EditForm validation, ErrorMessage, Invalid), the
/// events, the Localizer, the imperative API, ReadOnly and Disabled (with the page's handlers dropped), the BitParams
/// cascade, Style/Class/Styles/Classes and the public CSS variables, RTL, Escape inside a dialog, prerendering and
/// unmounting.
/// </summary>
[TestClass]
public class BitRichTextEditorE2ETests : E2ETestBase
{
    private const string Route = "/e2e/rich-text-editor";

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private ILocator Surface(string id) => Page.Locator($"#{id} .bit-rte-edt");

    private ILocator Btn(string id, string label) => Page.Locator($"#{id} .bit-rte-tlb button[aria-label='{label}']");

    private Task<bool> SelectTextAsync(string id, string text) => Page.Locator($"#{id}").EvaluateAsync<bool>(@"(root, text) => {
        const ed = root.querySelector('.bit-rte-edt');
        ed.focus();
        const walker = document.createTreeWalker(ed, NodeFilter.SHOW_TEXT);
        let n;
        while ((n = walker.nextNode())) {
            const i = n.nodeValue.indexOf(text);
            if (i >= 0) {
                const r = document.createRange();
                r.setStart(n, i);
                r.setEnd(n, i + text.length);
                const s = getSelection();
                s.removeAllRanges();
                s.addRange(r);
                return true;
            }
        }
        return false;
    }", text);

    private Task SelectElementAsync(string id, string selector) => Page.Locator($"#{id}").EvaluateAsync(@"(root, selector) => {
        const ed = root.querySelector('.bit-rte-edt');
        ed.focus();
        const r = document.createRange();
        r.selectNodeContents(ed.querySelector(selector));
        const s = getSelection();
        s.removeAllRanges();
        s.addRange(r);
    }", selector);

    private Task CaretAtEndAsync(string id) => Page.Locator($"#{id}").EvaluateAsync(@"root => {
        const ed = root.querySelector('.bit-rte-edt');
        ed.focus();
        const walker = document.createTreeWalker(ed, NodeFilter.SHOW_TEXT);
        let last = null, n;
        while ((n = walker.nextNode())) last = n;
        const r = document.createRange();
        if (last) { r.setStart(last, last.nodeValue.length); } else { r.selectNodeContents(ed.lastElementChild || ed); r.collapse(false); }
        r.collapse(true);
        const s = getSelection();
        s.removeAllRanges();
        s.addRange(r);
    }");

    private Task<string> HtmlAsync(string id) => Surface(id).EvaluateAsync<string>("e => e.innerHTML");

    private async Task EmptyAsync(string id)
    {
        await Surface(id).ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");
        await Page.Keyboard.PressAsync("Delete");
    }

    [TestMethod]
    public async Task Prerender_TheServerHtmlHoldsTheToolbarAndTheSanitizedValue()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<div id=\"rte-basic\" class=\"bit-rte\"><div role=\"toolbar\" aria-label=\"Formatting\"");
        StringAssert.Contains(html, "contenteditable=\"true\" data-placeholder=\"Write something...\"><p>Initial <strong>bound</strong> value</p></div>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);
        await Expect(Surface("rte-basic").Locator("strong")).ToHaveTextAsync("bound");
    }

    [TestMethod]
    public async Task Value_TypingUpdatesTheBoundHtml_AfterTheDebounce_AndRaisesOnChange()
    {
        await OpenAsync(Route);

        var surface = Surface("rte-basic");
        await Expect(surface).ToHaveAttributeAsync("role", "textbox");
        await Expect(surface).ToHaveAttributeAsync("aria-multiline", "true");
        await Expect(surface).ToHaveAttributeAsync("contenteditable", "true");

        await CaretAtEndAsync("rte-basic");
        await Page.Keyboard.TypeAsync(" typed");

        await Expect(Page.Locator("#value")).ToHaveTextAsync("<p>Initial <strong>bound</strong> value typed</p>");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync("<p>Initial <strong>bound</strong> value typed</p>");
        Assert.IsTrue(int.Parse((await Page.Locator("#change-count").TextContentAsync())!) >= 1);
    }

    [TestMethod]
    public async Task Value_SetByThePage_ReachesTheSurface_AndIsSanitized()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-set-value");
        await Expect(Surface("rte-basic").Locator("h2")).ToHaveTextAsync("Set from the page");
        await Expect(Surface("rte-basic").Locator("em")).ToHaveTextAsync("content");

        await Page.ClickAsync("#btn-set-unsafe");
        await Expect(Surface("rte-basic")).ToContainTextAsync("safe");
        var html = await HtmlAsync("rte-basic");
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase), html);
        Assert.IsFalse(html.Contains("onclick", StringComparison.OrdinalIgnoreCase), html);
        Assert.IsFalse(html.Contains("onerror", StringComparison.OrdinalIgnoreCase), html);
        Assert.IsFalse(html.Contains("javascript:", StringComparison.OrdinalIgnoreCase), html);
        await Surface("rte-basic").ClickAsync();
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__rteInjected === undefined"));

        // Clearing the value empties the surface, which shows the placeholder again.
        await Page.ClickAsync("#btn-clear-value");
        await Expect(Surface("rte-basic")).ToHaveTextAsync("");
        await Expect(Surface("rte-basic")).ToHaveAttributeAsync("aria-placeholder", "Write something...");
        var placeholder = await Surface("rte-basic").EvaluateAsync<string>("e => getComputedStyle(e.querySelector('p') || e, '::before').content");
        StringAssert.Contains(placeholder, "Write something...");
    }

    [TestMethod]
    public async Task Size_HeightMaxHeightResizableAndSpellCheck()
    {
        await OpenAsync(Route);

        var surface = Surface("rte-size");
        StringAssert.Contains(await Page.Locator("#rte-size").GetAttributeAsync("style"), "--bit-RichTextEditor-height:8rem");
        await Expect(surface).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(surface).ToHaveClassAsync(HasClass("bit-rte-edt-rsz"));
        await Expect(surface).ToHaveCSSAsync("resize", "vertical");
        var initial = (await surface.BoundingBoxAsync())!.Height;
        Assert.AreEqual(128, initial, 2, "the surface starts at Height");

        // It grows with the content up to MaxHeight, then scrolls.
        await surface.ClickAsync();
        for (var i = 0; i < 20; i++) await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForFunctionAsync("() => Math.abs(document.querySelector('#rte-size .bit-rte-edt').getBoundingClientRect().height - 224) < 2");
        Assert.IsTrue(await surface.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"));
    }

    [TestMethod]
    [DataRow("None", new string[0])]
    [DataRow("History", new[] { "Undo", "Redo" })]
    [DataRow("BlockFormat", new[] { "Paragraph format" })]
    [DataRow("Inline", new[] { "Bold", "Italic", "Underline", "Strikethrough", "Inline code" })]
    [DataRow("Lists", new[] { "Bullet list", "Numbered list", "Task list" })]
    [DataRow("Blocks", new[] { "Block quote", "Code block" })]
    [DataRow("Link", new[] { "Insert or edit link", "Remove link" })]
    [DataRow("Alignment", new[] { "Align left", "Align center", "Align right", "Justify" })]
    [DataRow("Clear", new[] { "Clear formatting" })]
    [DataRow("Image", new[] { "Insert image", "Float image left", "Center image", "Float image right" })]
    [DataRow("Color", new[] { "Text color", "Remove text color", "Highlight color", "Remove highlight" })]
    [DataRow("Font", new[] { "Font family", "Font size" })]
    [DataRow("Indent", new[] { "Decrease indent", "Increase indent" })]
    [DataRow("Script", new[] { "Subscript", "Superscript" })]
    [DataRow("Source", new[] { "HTML source view" })]
    [DataRow("Table", new[] { "Insert table", "Insert row above", "Insert row below", "Insert column before", "Insert column after", "Delete row", "Delete column", "Toggle header row", "Merge cells", "Split cell", "Delete table" })]
    [DataRow("Media", new[] { "Embed media" })]
    [DataRow("Rule", new[] { "Horizontal rule" })]
    [DataRow("Emoji", new[] { "Insert emoji" })]
    [DataRow("Find", new[] { "Find and replace" })]
    [DataRow("FullScreen", new[] { "Toggle full screen" })]
    [DataRow("Direction", new[] { "Left to right", "Right to left" })]
    [DataRow("Help", new[] { "Keyboard shortcuts" })]
    public async Task Toolbar_EachGroup_RendersItsControls(string group, string[] labels)
    {
        await OpenAsync(Route);

        await Page.Locator("#toolbar-select").SelectOptionAsync(group);
        await Expect(Page.Locator("#toolbar-name")).ToHaveTextAsync(group);

        var controls = Page.Locator("#rte-groups .bit-rte-tlb").Locator("button, select, input");
        var names = await controls.EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('aria-label'))");
        CollectionAssert.AreEquivalent(labels, names, $"{group}: {string.Join(", ", names)}");
        await Expect(Page.Locator("#rte-groups .bit-rte-tlb .bit-rte-grp")).ToHaveCountAsync(labels.Length == 0 ? 0 : 1);
    }

    [TestMethod]
    public async Task Toolbar_AllAndAllExtended_RenderTheirGroups()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#rte-groups .bit-rte-grp")).ToHaveCountAsync(8);
        await Page.Locator("#toolbar-select").SelectOptionAsync("AllExtended");
        await Expect(Page.Locator("#rte-groups .bit-rte-grp")).ToHaveCountAsync(22);
    }

    [TestMethod]
    [DataRow("Bold", "b")]
    [DataRow("Italic", "i")]
    [DataRow("Underline", "u")]
    [DataRow("Strikethrough", "strike")]
    [DataRow("Inline code", "code")]
    [DataRow("Subscript", "sub")]
    [DataRow("Superscript", "sup")]
    public async Task Inline_EachMark_IsAppliedToTheSelection_AndReportedOnItsButton(string label, string tag)
    {
        await OpenAsync(Route);

        Assert.IsTrue(await SelectTextAsync("rte-ext", "beta"));
        var button = Btn("rte-ext", label);
        await button.ClickAsync();

        await Expect(Surface("rte-ext").Locator(tag)).ToHaveTextAsync("beta");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync($"<{tag}>beta</{tag}>");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-rte-act"));

        // Applied again, it comes off.
        await button.ClickAsync();
        await Expect(Surface("rte-ext").Locator(tag)).ToHaveCountAsync(0);
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    [DataRow("h1")]
    [DataRow("h2")]
    [DataRow("h3")]
    [DataRow("h4")]
    [DataRow("h5")]
    [DataRow("h6")]
    [DataRow("blockquote")]
    [DataRow("pre")]
    [DataRow("p")]
    public async Task BlockFormat_EachFormat_ReplacesTheBlockAtTheCaret(string format)
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        var select = Page.Locator("#rte-ext select[aria-label='Paragraph format']");
        if (format == "p")
        {
            await select.SelectOptionAsync("h2");
            await Expect(Surface("rte-ext").Locator("h2")).ToHaveCountAsync(1);
            await SelectTextAsync("rte-ext", "beta");
        }

        await select.SelectOptionAsync(format);
        await Expect(Surface("rte-ext").Locator($"{format}")).ToContainTextAsync("alpha beta gamma");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync($"<{format}");
        await Expect(select).ToHaveValueAsync(format);
    }

    [TestMethod]
    public async Task Lists_BulletNumberedAndTaskLists()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Bullet list").ClickAsync();
        await Expect(Surface("rte-ext").Locator("ul > li")).ToHaveTextAsync("alpha beta gamma");
        await Expect(Btn("rte-ext", "Bullet list")).ToHaveAttributeAsync("aria-pressed", "true");

        await Btn("rte-ext", "Numbered list").ClickAsync();
        await Expect(Surface("rte-ext").Locator("ol > li")).ToHaveTextAsync("alpha beta gamma");
        await Expect(Surface("rte-ext").Locator("ul")).ToHaveCountAsync(0);
        await Expect(Btn("rte-ext", "Numbered list")).ToHaveAttributeAsync("aria-pressed", "true");

        await Btn("rte-ext", "Task list").ClickAsync();
        var tasks = Surface("rte-ext").Locator("ul.bit-rte-tasks > li");
        await Expect(tasks).ToHaveCountAsync(1);
        await Expect(tasks).ToHaveAttributeAsync("data-checked", "false");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync("bit-rte-tasks");
        await Expect(Btn("rte-ext", "Task list")).ToHaveAttributeAsync("aria-pressed", "true");

        // Ticking the box of an item checks it.
        var box = await tasks.BoundingBoxAsync();
        var rtl = await tasks.EvaluateAsync<bool>("li => getComputedStyle(li).direction === 'rtl'");
        await Page.Mouse.ClickAsync(rtl ? box!.X + box.Width - 6 : box!.X + 6, box.Y + box.Height / 2);
        await Expect(tasks).ToHaveAttributeAsync("data-checked", "true");
    }

    [TestMethod]
    public async Task Indent_NestsAListItem_AndOutdentBringsItBack()
    {
        await OpenAsync(Route);

        await Surface("rte-ext").ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");
        await Page.Keyboard.TypeAsync("one");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("two");
        await Expect(Surface("rte-ext").Locator("p")).ToHaveCountAsync(2);
        await Page.Keyboard.PressAsync("Control+a");
        await Btn("rte-ext", "Bullet list").ClickAsync();
        await Expect(Surface("rte-ext").Locator("ul > li")).ToHaveCountAsync(2);

        await SelectTextAsync("rte-ext", "two");
        await Btn("rte-ext", "Increase indent").ClickAsync();
        await Expect(Surface("rte-ext").Locator("ul ul > li, ul > li > ul > li")).ToHaveTextAsync("two");

        await Btn("rte-ext", "Decrease indent").ClickAsync();
        await Expect(Surface("rte-ext").Locator("ul ul")).ToHaveCountAsync(0);
        await Expect(Surface("rte-ext").Locator("ul > li")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Blocks_QuoteAndCodeBlockToggle()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Block quote").ClickAsync();
        await Expect(Surface("rte-ext").Locator("blockquote")).ToContainTextAsync("alpha beta gamma");
        await Expect(Btn("rte-ext", "Block quote")).ToHaveAttributeAsync("aria-pressed", "true");
        await Btn("rte-ext", "Block quote").ClickAsync();
        await Expect(Surface("rte-ext").Locator("blockquote")).ToHaveCountAsync(0);

        await Btn("rte-ext", "Code block").ClickAsync();
        await Expect(Surface("rte-ext").Locator("pre")).ToContainTextAsync("alpha beta gamma");
        await Expect(Btn("rte-ext", "Code block")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    [DataRow("Align left", "left")]
    [DataRow("Align center", "center")]
    [DataRow("Align right", "right")]
    [DataRow("Justify", "justify")]
    public async Task Alignment_EachButton_AlignsTheBlock(string label, string align)
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", label).ClickAsync();

        var block = Surface("rte-ext").Locator("p, div").First;
        await Expect(block).ToHaveCSSAsync("text-align", align == "left" ? new Regex("^(left|start)$") : new Regex($"^{align}$"));
        await Expect(Btn("rte-ext", label)).ToHaveAttributeAsync("aria-pressed", "true");
        if (align != "left") await Expect(Page.Locator("#ext-value")).ToContainTextAsync($"text-align: {align}");
    }

    [TestMethod]
    public async Task Direction_SetsTheDirectionOfTheBlock()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Right to left").ClickAsync();
        await Expect(Surface("rte-ext").Locator("[dir='rtl']")).ToContainTextAsync("alpha beta gamma");
        await Expect(Btn("rte-ext", "Right to left")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync("dir=\"rtl\"");

        await Btn("rte-ext", "Left to right").ClickAsync();
        await Expect(Btn("rte-ext", "Left to right")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Surface("rte-ext").Locator("[dir='rtl']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task RuleClearFormattingAndHistory()
    {
        await OpenAsync(Route);

        await CaretAtEndAsync("rte-ext");
        await Btn("rte-ext", "Horizontal rule").ClickAsync();
        await Expect(Surface("rte-ext").Locator("hr")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync("<hr>");

        await Btn("rte-ext", "Undo").ClickAsync();
        await Expect(Surface("rte-ext").Locator("hr")).ToHaveCountAsync(0);
        await Btn("rte-ext", "Redo").ClickAsync();
        await Expect(Surface("rte-ext").Locator("hr")).ToHaveCountAsync(1);

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Bold").ClickAsync();
        await Btn("rte-ext", "Italic").ClickAsync();
        await Expect(Surface("rte-ext").Locator("strong, b")).ToHaveCountAsync(1);
        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Clear formatting").ClickAsync();
        await Expect(Surface("rte-ext").Locator("strong, b, em, i")).ToHaveCountAsync(0);
        await Expect(Surface("rte-ext")).ToContainTextAsync("alpha beta gamma");
    }

    [TestMethod]
    [DataRow("Control+b", "b")]
    [DataRow("Control+i", "i")]
    [DataRow("Control+u", "u")]
    [DataRow("Control+Shift+KeyX", "strike")]
    [DataRow("Control+e", "code")]
    public async Task Shortcuts_InlineMarks(string keys, string tag)
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Page.Keyboard.PressAsync(keys);
        await Expect(Surface("rte-ext").Locator(tag)).ToHaveTextAsync("beta");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync($"<{tag}>beta</{tag}>");
    }

    [TestMethod]
    public async Task Shortcuts_ListsRemoveFormatUndoAndRedo()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Page.Keyboard.PressAsync("Control+Shift+Digit8");
        await Expect(Surface("rte-ext").Locator("ul > li")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Control+Shift+Digit7");
        await Expect(Surface("rte-ext").Locator("ol > li")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Control+Shift+Digit9");
        await Expect(Surface("rte-ext").Locator("ul.bit-rte-tasks > li")).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Control+z");
        await Expect(Surface("rte-ext").Locator("ul.bit-rte-tasks")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Control+y");
        await Expect(Surface("rte-ext").Locator("ul.bit-rte-tasks")).ToHaveCountAsync(1);

        await SelectTextAsync("rte-ext", "beta");
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(Surface("rte-ext").Locator("b")).ToHaveCountAsync(1);
        await SelectTextAsync("rte-ext", "beta");
        await Page.Keyboard.PressAsync("Control+Backslash");
        await Expect(Surface("rte-ext").Locator("b")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Fonts_FamilyAndSize_StyleTheSelection()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Page.Locator("#rte-ext select[aria-label='Font family']").SelectOptionAsync("Georgia");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync("Georgia");
        var family = await Surface("rte-ext").EvaluateAsync<string>(@"e => { const w = document.createTreeWalker(e, NodeFilter.SHOW_TEXT); let n; while ((n = w.nextNode())) if (n.nodeValue.includes('beta')) return getComputedStyle(n.parentElement).fontFamily; return ''; }");
        StringAssert.Contains(family, "Georgia");

        await SelectTextAsync("rte-ext", "beta");
        await Page.Locator("#rte-ext select[aria-label='Font size']").SelectOptionAsync("24px");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync("24px");
        var size = await Surface("rte-ext").EvaluateAsync<string>(@"e => { const w = document.createTreeWalker(e, NodeFilter.SHOW_TEXT); let n; while ((n = w.nextNode())) if (n.nodeValue.includes('beta')) return getComputedStyle(n.parentElement).fontSize; return ''; }");
        Assert.AreEqual("24px", size);
    }

    [TestMethod]
    public async Task Colors_NativePickers_PaintTheTextAndTheHighlight_AndRemoveThem()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-ext", "beta");
        await Page.Locator("#rte-ext input[aria-label='Text color']").FillAsync("#ff0000");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync(new Regex("color: (rgb\\(255, 0, 0\\)|#ff0000)"));

        await SelectTextAsync("rte-ext", "gamma");
        await Page.Locator("#rte-ext input[aria-label='Highlight color']").FillAsync("#00ff00");
        await Expect(Page.Locator("#ext-value")).ToContainTextAsync(new Regex("background-color: (rgb\\(0, 255, 0\\)|#00ff00)"));

        await SelectTextAsync("rte-ext", "beta");
        await Btn("rte-ext", "Remove text color").ClickAsync();
        await Expect(Page.Locator("#ext-value")).Not.ToContainTextAsync(new Regex("[^-]color: (rgb\\(255, 0, 0\\)|#ff0000)"));
        await SelectTextAsync("rte-ext", "gamma");
        await Btn("rte-ext", "Remove highlight").ClickAsync();
        await Expect(Page.Locator("#ext-value")).Not.ToContainTextAsync("background-color");
    }

    [TestMethod]
    public async Task Colors_APalette_OpensAPanelOfSwatches()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-colors", "paint");
        var textColor = Btn("rte-colors", "Text color");
        await textColor.ClickAsync();
        await Expect(textColor).ToHaveAttributeAsync("aria-expanded", "true");
        var panel = Page.Locator("#" + await textColor.GetAttributeAsync("aria-controls"));
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel.Locator(".bit-rte-swatches button")).ToHaveCountAsync(3);
        await Expect(panel.Locator(".bit-rte-swatches button").First).ToBeFocusedAsync();

        // The arrow keys walk the swatches; Enter picks one.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(panel.Locator(".bit-rte-swatches button").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#color-value")).ToContainTextAsync(new Regex("color: (rgb\\(0, 170, 0\\)|#00aa00)"));
        await Expect(panel).ToHaveCountAsync(0);

        // The highlight panel picks with the pointer; the fonts come from the parameters.
        await SelectTextAsync("rte-colors", "me");
        await Btn("rte-colors", "Highlight color").ClickAsync();
        await Page.Locator("#rte-colors .bit-rte-swatches button").Nth(2).ClickAsync();
        await Expect(Page.Locator("#color-value")).ToContainTextAsync(new Regex("background-color: (rgb\\(0, 0, 255\\)|#0000ff)"));

        var families = await Page.Locator("#rte-colors select[aria-label='Font family'] option").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Font", "Georgia", "Courier New" }, families.ToArray());
        var sizes = await Page.Locator("#rte-colors select[aria-label='Font size'] option").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Size", "12px", "20px" }, sizes.ToArray());
    }

    [TestMethod]
    public async Task Links_ThePanelAddsEditsOpensAndRemovesALink()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-links", "site");
        await Page.Keyboard.PressAsync("Control+k");
        var panel = Page.Locator("#rte-links .bit-rte-bar[role='group']");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Insert or edit link");
        var url = panel.Locator("input[type='url']");
        await Expect(url).ToBeFocusedAsync();
        await Expect(Btn("rte-links", "Insert or edit link")).ToHaveAttributeAsync("aria-expanded", "true");

        await url.FillAsync("https://bitplatform.dev");
        await panel.Locator("input[type='checkbox']").CheckAsync();
        await url.PressAsync("Enter");

        var link = Surface("rte-links").Locator("a");
        await Expect(link).ToHaveTextAsync("site");
        await Expect(link).ToHaveAttributeAsync("href", "https://bitplatform.dev");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", new Regex("noopener"));
        await Expect(Page.Locator("#link-value")).ToContainTextAsync("href=\"https://bitplatform.dev\"");
        await Expect(panel).ToHaveCountAsync(0);

        // With the caret in the link, the panel edits it, offers to open it and to remove it.
        await SelectElementAsync("rte-links", "a");
        await Expect(Btn("rte-links", "Insert or edit link")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Btn("rte-links", "Remove link")).ToBeEnabledAsync();
        await Btn("rte-links", "Insert or edit link").ClickAsync();
        await Expect(panel.Locator("a", new() { HasTextString = "Open" })).ToHaveAttributeAsync("href", "https://bitplatform.dev");
        await panel.Locator("button", new() { HasTextString = "Remove" }).ClickAsync();
        await Expect(Surface("rte-links").Locator("a")).ToHaveCountAsync(0);
        await Expect(Surface("rte-links")).ToContainTextAsync("visit the site now");
    }

    [TestMethod]
    public async Task Links_AnUnsafeUrlIsRefused_AndTheUnlinkShortcutRemovesALink()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-links", "site");
        await Btn("rte-links", "Insert or edit link").ClickAsync();
        var panel = Page.Locator("#rte-links .bit-rte-bar[role='group']");
        await panel.Locator("input[type='url']").FillAsync("javascript:alert(1)");
        await panel.Locator("button", new() { HasTextString = "Apply" }).ClickAsync();
        await Expect(Surface("rte-links").Locator("a")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#link-errors")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#link-error")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#rte-links .bit-rte-err")).ToHaveAttributeAsync("role", "alert");

        await panel.Locator("input[type='url']").FillAsync("https://example.com");
        await panel.Locator("input[aria-label='Link text']").FillAsync("ignored");
        await panel.Locator("button", new() { HasTextString = "Apply" }).ClickAsync();
        await Expect(Surface("rte-links").Locator("a")).ToHaveAttributeAsync("href", "https://example.com");

        await SelectElementAsync("rte-links", "a");
        await Page.Keyboard.PressAsync("Control+Shift+KeyK");
        await Expect(Surface("rte-links").Locator("a")).ToHaveCountAsync(0);

        // Cancel closes the panel and puts the focus back in the text.
        await SelectTextAsync("rte-links", "visit");
        await Page.Keyboard.PressAsync("Control+k");
        await panel.Locator("button", new() { HasTextString = "Cancel" }).ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Links_ATypedUrlBecomesALink_UnlessAutoLinkIsOff()
    {
        await OpenAsync(Route);

        await CaretAtEndAsync("rte-links");
        await Page.Keyboard.TypeAsync(" https://example.org ");
        await Expect(Surface("rte-links").Locator("a[href='https://example.org']")).ToHaveCountAsync(1);

        await Surface("rte-autolink-off").ClickAsync();
        await Page.Keyboard.TypeAsync("see https://example.org ");
        await Expect(Page.Locator("#autolink-value")).ToContainTextAsync("https://example.org");
        await Expect(Surface("rte-autolink-off").Locator("a")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Images_ThePanelInsertsAnImage_AndTheAlignButtonsFloatIt()
    {
        await OpenAsync(Route);

        await CaretAtEndAsync("rte-images");
        var imageButton = Btn("rte-images", "Insert image");
        await Expect(Btn("rte-images", "Float image left")).ToBeDisabledAsync();
        await imageButton.ClickAsync();
        var panel = Page.Locator("#rte-images .bit-rte-bar[role='group']");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Insert image");
        await Expect(panel.Locator("input[type='url']")).ToBeFocusedAsync();
        await panel.Locator("input[type='url']").FillAsync("not a url");
        await panel.Locator("button", new() { HasTextString = "Insert" }).ClickAsync();
        await Expect(Page.Locator("#rte-images .bit-rte-err")).ToHaveTextAsync("That image URL is not valid.");
        await Expect(Page.Locator("#image-error")).ToHaveTextAsync("invalid-url");

        await panel.Locator("input[type='url']").FillAsync($"{BaseUrl}/samples/mdv-sample.png");
        await panel.Locator("input[aria-label='Alternative text']").FillAsync("logo");
        await panel.Locator("input[type='number']").FillAsync("30");
        await panel.Locator("button", new() { HasTextString = "Insert" }).ClickAsync();

        var image = Surface("rte-images").Locator("img");
        await Expect(image).ToHaveAttributeAsync("src", $"{BaseUrl}/samples/mdv-sample.png");
        await Expect(Page.Locator("#rte-images .bit-rte-err")).ToHaveCountAsync(0);
        await Expect(image).ToHaveAttributeAsync("alt", "logo");
        await Expect(image).ToHaveJSPropertyAsync("width", 30);
        await Expect(Page.Locator("#image-value")).ToContainTextAsync("alt=\"logo\"");

        // A selected image enables the float buttons and turns the panel into an editor of it.
        await image.ClickAsync();
        await Expect(Btn("rte-images", "Edit image")).ToHaveCountAsync(1);
        await Expect(Btn("rte-images", "Float image left")).ToBeEnabledAsync();
        await Btn("rte-images", "Float image left").ClickAsync();
        await Expect(image).ToHaveCSSAsync("float", "left");
        await Expect(Btn("rte-images", "Float image left")).ToHaveAttributeAsync("aria-pressed", "true");
        await Btn("rte-images", "Float image right").ClickAsync();
        await Expect(image).ToHaveCSSAsync("float", "right");
        await Btn("rte-images", "Center image").ClickAsync();
        await Expect(image).ToHaveCSSAsync("float", "none");
        await Expect(Btn("rte-images", "Center image")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Images_ADroppedImage_IsUploaded_AndOneTooLargeIsRefused()
    {
        await OpenAsync(Route);

        async Task DropAsync(string id, string name, int size)
        {
            await Page.Locator($"#{id}").EvaluateAsync(@"(root, d) => {
                const bytes = new Uint8Array(d.size);
                bytes.set(d.png.slice(0, Math.min(d.png.length, d.size)));
                const dt = new DataTransfer();
                dt.items.add(new File([bytes], d.name, { type: 'image/png' }));
                const ed = root.querySelector('.bit-rte-edt');
                const r = ed.getBoundingClientRect();
                const opts = { dataTransfer: dt, bubbles: true, cancelable: true, clientX: r.left + 10, clientY: r.top + 10 };
                ed.dispatchEvent(new DragEvent('dragover', opts));
                ed.dispatchEvent(new DragEvent('drop', opts));
            }", new { name, size, png = TinyPng.Select(b => (int)b).ToArray() });
        }

        await CaretAtEndAsync("rte-images");
        await DropAsync("rte-images", "dropped.png", TinyPng.Length);
        await Expect(Surface("rte-images").Locator("img[src='/samples/mdv-sample.png']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#last-upload")).ToHaveTextAsync($"dropped.png|image/png|{TinyPng.Length}");

        await DropAsync("rte-images", "huge.png", 5000);
        await Expect(Page.Locator("#image-error")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#uploads")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#rte-images .bit-rte-err")).ToHaveAttributeAsync("role", "alert");

        // With no upload handler, an image is embedded as a data URL.
        await CaretAtEndAsync("rte-images-inline");
        await DropAsync("rte-images-inline", "inline.png", TinyPng.Length);
        await Expect(Surface("rte-images-inline").Locator("img[src^='data:image/png;base64,']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Tables_ThePanelInsertsATable_AndTheOpsEditIt()
    {
        await OpenAsync(Route);

        await CaretAtEndAsync("rte-tables");
        await Expect(Btn("rte-tables", "Insert row below")).ToBeDisabledAsync();
        await Btn("rte-tables", "Insert table").ClickAsync();
        var panel = Page.Locator("#rte-tables .bit-rte-bar[role='group']");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Insert table");
        await panel.Locator("input[type='number']").Nth(0).FillAsync("2");
        await panel.Locator("input[type='number']").Nth(1).FillAsync("3");
        await panel.Locator("input[type='checkbox']").CheckAsync();
        await panel.Locator("button", new() { HasTextString = "Insert" }).ClickAsync();

        var table = Surface("rte-tables").Locator("table");
        await Expect(table).ToHaveCountAsync(1);
        await Expect(table.Locator("th")).ToHaveCountAsync(3);
        await Expect(table.Locator("tr")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#table-value")).ToContainTextAsync("<table");

        // The caret is in the table: the ops are enabled.
        async Task OpAsync(string label)
        {
            await table.Locator("td").Last.ClickAsync();
            await Expect(Btn("rte-tables", label)).ToBeEnabledAsync();
            await Btn("rte-tables", label).ClickAsync();
        }

        await OpAsync("Insert row below");
        await Expect(table.Locator("tr")).ToHaveCountAsync(3);
        await OpAsync("Insert row above");
        await Expect(table.Locator("tr")).ToHaveCountAsync(4);
        await OpAsync("Delete row");
        await Expect(table.Locator("tr")).ToHaveCountAsync(3);
        await OpAsync("Insert column after");
        await Expect(table.Locator("tr").First.Locator("th, td")).ToHaveCountAsync(4);
        await OpAsync("Insert column before");
        await Expect(table.Locator("tr").First.Locator("th, td")).ToHaveCountAsync(5);
        await OpAsync("Delete column");
        await Expect(table.Locator("tr").First.Locator("th, td")).ToHaveCountAsync(4);

        await OpAsync("Toggle header row");
        await Expect(table.Locator("th")).ToHaveCountAsync(0);
        await OpAsync("Toggle header row");
        await Expect(table.Locator("th")).ToHaveCountAsync(4);

        await OpAsync("Delete table");
        await Expect(Surface("rte-tables").Locator("table")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Media_EmbedsAnAllowedVideo_AndRefusesAnyOtherUrl()
    {
        await Page.RouteAsync("https://www.youtube-nocookie.com/**", route => route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = "<html></html>" }));
        await OpenAsync(Route);

        await CaretAtEndAsync("rte-media");
        await Btn("rte-media", "Embed media").ClickAsync();
        var panel = Page.Locator("#rte-media .bit-rte-bar[role='group']");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Embed media");
        await panel.Locator("input[type='url']").FillAsync("https://evil.example.com/video");
        await panel.Locator("button", new() { HasTextString = "Embed" }).ClickAsync();
        await Expect(Page.Locator("#media-error")).Not.ToBeEmptyAsync();
        await Expect(Surface("rte-media").Locator("iframe")).ToHaveCountAsync(0);

        await panel.Locator("input[type='url']").FillAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await panel.Locator("button", new() { HasTextString = "Embed" }).ClickAsync();
        await Expect(Surface("rte-media").Locator("iframe")).ToHaveAttributeAsync("src", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ");
        await Expect(Page.Locator("#media-value")).ToContainTextAsync("youtube-nocookie.com/embed/dQw4w9WgXcQ");

        await CaretAtEndAsync("rte-media");
        await Btn("rte-media", "Horizontal rule").ClickAsync();
        await Expect(Surface("rte-media").Locator("hr")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Find_CountsWalksAndReplacesMatches_WithoutLeakingMarksIntoTheValue()
    {
        await OpenAsync(Route);

        await Btn("rte-find", "Find and replace").ClickAsync();
        var panel = Page.Locator("#rte-find .bit-rte-bar[role='search']");
        await Expect(panel).ToBeVisibleAsync();
        var find = panel.Locator("input[aria-label='Find']");
        await Expect(find).ToBeFocusedAsync();
        var count = panel.Locator(".bit-rte-find-count");

        await find.FillAsync("cat");
        await Expect(count).ToHaveTextAsync("1 of 3");
        await Expect(Surface("rte-find").Locator("mark.bit-rte-find")).ToHaveCountAsync(3);
        var markBackground = await Surface("rte-find").Locator("mark.bit-rte-find").First.EvaluateAsync<string>("m => getComputedStyle(m).backgroundColor");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", markBackground);

        await panel.Locator("input[aria-label='Whole word']").CheckAsync();
        await Expect(count).ToHaveTextAsync(new Regex("^(1 of 2|2 matches)$"));
        await panel.Locator("input[aria-label='Match case']").CheckAsync();
        await Expect(count).ToHaveTextAsync("1 match");
        await panel.Locator("input[aria-label='Match case']").UncheckAsync();
        await Expect(count).ToHaveTextAsync(new Regex("^(1 of 2|2 matches)$"));

        await panel.Locator("button[aria-label='Next match']").ClickAsync();
        await Expect(count).ToHaveTextAsync(new Regex("of 2$"));
        var current = await count.TextContentAsync();
        await panel.Locator("button[aria-label='Next match']").ClickAsync();
        await Expect(count).Not.ToHaveTextAsync(current!);
        await panel.Locator("button[aria-label='Previous match']").ClickAsync();
        await Expect(count).ToHaveTextAsync(current!);
        await Expect(Surface("rte-find").Locator("mark.bit-rte-find-cur")).ToHaveCountAsync(1);
        await panel.Locator("button[aria-label='Previous match']").ClickAsync();
        await panel.Locator("button[aria-label='Next match']").ClickAsync();
        await Expect(count).ToHaveTextAsync(current!);

        await panel.Locator("input[aria-label='Replace with']").FillAsync("dog");
        await panel.Locator("button", new() { HasTextString = "Replace" }).First.ClickAsync();
        await Expect(Surface("rte-find")).ToContainTextAsync("dog");
        await panel.Locator("button[aria-label='Replace all']").ClickAsync();
        await Expect(count).ToHaveTextAsync(new Regex("replaced$"));
        await Expect(Page.Locator("#find-value")).ToHaveTextAsync("<p>The dog sat. The dog ran. A category.</p>");
        await Expect(Page.Locator("#find-value")).Not.ToContainTextAsync("bit-rte-find");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(Surface("rte-find").Locator("mark")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Sanitization_APolicyKeepsOnlyItsTags_AndThePastesFollowIt()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not let a page construct a ClipboardEvent with data.");

        await OpenAsync(Route);

        async Task PasteAsync(string id, string text, string html)
        {
            await Page.Locator($"#{id}").EvaluateAsync(@"(root, d) => {
                const ed = root.querySelector('.bit-rte-edt');
                ed.focus();
                const dt = new DataTransfer();
                dt.setData('text/plain', d.text);
                dt.setData('text/html', d.html);
                ed.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
            }", new { text, html });
        }

        const string rich = "<h1>Title</h1><p><strong>bold</strong> <span style=\"color:red\">red</span> <a href=\"http://insecure.example.com\">http</a></p><script>window.__rteInjected = 1</script>";

        await CaretAtEndAsync("rte-policy");
        await PasteAsync("rte-policy", "Title bold red http", rich);
        await Expect(Surface("rte-policy").Locator("strong")).ToHaveTextAsync("bold");
        await Expect(Page.Locator("#policy-value")).ToContainTextAsync("<strong>bold</strong>");
        Assert.IsFalse((await HtmlAsync("rte-policy")).Contains("<script", StringComparison.OrdinalIgnoreCase));
        var policyHtml = (await Page.Locator("#policy-value").TextContentAsync())!;
        Assert.IsFalse(policyHtml.Contains("<h1", StringComparison.OrdinalIgnoreCase), policyHtml);
        Assert.IsFalse(policyHtml.Contains("http://insecure", StringComparison.OrdinalIgnoreCase), policyHtml);
        Assert.IsFalse(policyHtml.Contains("<script", StringComparison.OrdinalIgnoreCase), policyHtml);
        Assert.IsFalse(policyHtml.Contains("style=", StringComparison.OrdinalIgnoreCase), policyHtml);

        await Surface("rte-plain").ClickAsync();
        await PasteAsync("rte-plain", "Plain only", "<h1>Plain</h1><p><strong>only</strong></p>");
        await Expect(Surface("rte-plain")).ToContainTextAsync("Plain only");
        await Expect(Surface("rte-plain").Locator("h1, strong")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#plain-value")).ToContainTextAsync("Plain only");
        await Expect(Page.Locator("#plain-value")).Not.ToContainTextAsync("<strong>");

        await Surface("rte-rich-paste").ClickAsync();
        await PasteAsync("rte-rich-paste", "Title bold red http", rich);
        await Expect(Surface("rte-rich-paste").Locator("strong")).ToHaveTextAsync("bold");
        await Expect(Surface("rte-rich-paste").Locator("script")).ToHaveCountAsync(0);
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__rteInjected === undefined"));
    }

    [TestMethod]
    public async Task Source_TheSourceViewShowsAndEditsTheHtml()
    {
        await OpenAsync(Route);

        var toggle = Btn("rte-policy", "HTML source view");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        var source = Page.Locator("#rte-policy textarea.bit-rte-src");
        await Expect(source).ToBeVisibleAsync();
        await Expect(source).ToHaveAttributeAsync("aria-label", "HTML source");
        await Expect(source).ToHaveValueAsync(new Regex("<p>policy</p>"));
        await Expect(Surface("rte-policy")).ToBeHiddenAsync();
        // Editing commands are off while the source is shown.
        await Expect(Btn("rte-policy", "Bold")).ToBeDisabledAsync();

        await source.FillAsync("<p><strong>edited</strong> <u>gone</u></p><script>x()</script>");
        await toggle.ClickAsync();
        await Expect(source).ToHaveCountAsync(0);
        await Expect(Surface("rte-policy").Locator("strong")).ToHaveTextAsync("edited");
        await Expect(Surface("rte-policy").Locator("u, script")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#policy-value")).ToContainTextAsync("<strong>edited</strong>");
    }

    [TestMethod]
    public async Task Typing_MarkdownInputRules_TurnMarkersIntoBlocksAndMarks()
    {
        await OpenAsync(Route);

        var surface = Surface("rte-typing");
        await surface.ClickAsync();

        await Page.Keyboard.TypeAsync("# Heading");
        await Expect(surface.Locator("h1")).ToHaveTextAsync("Heading");
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.TypeAsync("- item");
        await Expect(surface.Locator("ul > li")).ToHaveTextAsync("item");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.TypeAsync("1. first");
        await Expect(surface.Locator("ol > li")).ToHaveTextAsync("first");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.TypeAsync("> quoted");
        await Expect(surface.Locator("blockquote")).ToContainTextAsync("quoted");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.TypeAsync("[] todo");
        await Expect(surface.Locator("ul.bit-rte-tasks > li")).ToHaveTextAsync("todo");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.TypeAsync("some **strong** and `code` ");
        await Expect(surface.Locator("strong")).ToHaveTextAsync("strong");
        await Expect(surface.Locator("code")).ToHaveTextAsync("code");
        await Expect(Page.Locator("#typing-value")).ToContainTextAsync("<strong>strong</strong>");
    }

    [TestMethod]
    public async Task Typing_SmartTypography_ReplacesShorthandsAsTyped()
    {
        await OpenAsync(Route);

        await Surface("rte-typing").ClickAsync();
        await Page.Keyboard.TypeAsync("\"Quote\" a -- b (c) wait... x -> y ");
        await Expect(Surface("rte-typing")).ToContainTextAsync("“Quote” a — b © wait… x → y");
    }

    [TestMethod]
    public async Task Typing_TheSlashMenu_FiltersAndAppliesACommand()
    {
        await OpenAsync(Route);

        await Surface("rte-typing").ClickAsync();
        await Page.Keyboard.TypeAsync("/");
        var filter = Page.Locator("#rte-typing .bit-rte-slash input[role='combobox']");
        await Expect(filter).ToBeFocusedAsync();
        var options = Page.Locator("#rte-typing .bit-rte-slash [role='option']");
        await Expect(options).ToHaveCountAsync(10);
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.TypeAsync("numb");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(options).ToHaveTextAsync("Numbered list");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#rte-typing .bit-rte-slash")).ToHaveCountAsync(0);
        await Expect(Surface("rte-typing").Locator("ol")).ToHaveCountAsync(1);
        await Expect(Surface("rte-typing")).Not.ToContainTextAsync("/");

        // Arrow keys walk the options; Escape dismisses the menu.
        await Page.Keyboard.TypeAsync("x");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("/");
        await Expect(filter).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(filter).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(1).GetAttributeAsync("id"))!);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#rte-typing .bit-rte-slash")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Shortcuts_ACustomMapBindsBlocksAndOverridesBuiltIns()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-shortcuts", "text");
        await Page.Keyboard.PressAsync("Control+Shift+Digit1");
        await Expect(Surface("rte-shortcuts").Locator("h1")).ToHaveTextAsync("shortcut text");
        await Page.Keyboard.PressAsync("Control+Shift+Digit1");
        await Expect(Surface("rte-shortcuts").Locator("h1")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Control+Shift+KeyQ");
        await Expect(Surface("rte-shortcuts").Locator("blockquote")).ToHaveCountAsync(1);

        // Ctrl+B was rebound to italic.
        await SelectTextAsync("rte-shortcuts", "text");
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(Surface("rte-shortcuts").Locator("i")).ToHaveTextAsync("text");
        await Expect(Surface("rte-shortcuts").Locator("b, strong")).ToHaveCountAsync(0);
        await Expect(Btn("rte-shortcuts", "Italic")).ToHaveAttributeAsync("aria-keyshortcuts", new Regex("Control\\+B"));
    }

    [TestMethod]
    public async Task Help_AltZeroOpensTheShortcutsPanel_AndEscapeClosesIt()
    {
        await OpenAsync(Route);

        await Surface("rte-shortcuts").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+Digit0");
        var panel = Page.Locator("#rte-shortcuts .bit-rte-help");
        await Expect(panel).ToBeVisibleAsync();
        var list = panel.Locator(".bit-rte-help-scroll");
        await Expect(list).ToBeFocusedAsync();
        await Expect(list).ToHaveAttributeAsync("role", "region");
        await Expect(Btn("rte-shortcuts", "Keyboard shortcuts")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel.Locator("tbody tr").First).ToBeVisibleAsync();
        Assert.IsTrue(await panel.Locator("tbody tr").CountAsync() > 5);
        await Expect(panel.Locator("kbd", new() { HasTextString = "Ctrl+Shift+1" })).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(Surface("rte-shortcuts")).ToBeFocusedAsync();

        // The surface is described by the hint that says where the help is.
        var hintId = (await Surface("rte-shortcuts").GetAttributeAsync("aria-describedby"))!.Split(' ').Last();
        await Expect(Page.Locator($"#{hintId}")).ToHaveTextAsync("Press Alt+0 for keyboard shortcuts.");
    }

    [TestMethod]
    public async Task QuickToolbar_FloatsOverASelection_AndFormatsIt()
    {
        await OpenAsync(Route);

        var quick = Page.Locator("#rte-quick .bit-rte-quick");
        await Expect(quick).ToHaveCountAsync(0);
        await Expect(Page.Locator("#rte-quick .bit-rte-tlb")).ToHaveCountAsync(0);

        await SelectTextAsync("rte-quick", "toolbar");
        await Expect(quick).ToBeVisibleAsync();
        await Expect(quick).ToHaveAttributeAsync("role", "toolbar");
        await Expect(quick).ToHaveAttributeAsync("aria-label", "Selection formatting");

        // It sits near the selection.
        var selection = await Page.EvaluateAsync<double[]>("() => { const r = getSelection().getRangeAt(0).getBoundingClientRect(); return [r.left, r.top, r.bottom]; }");
        var box = (await quick.BoundingBoxAsync())!;
        Assert.IsTrue(Math.Abs(box.Y + box.Height - selection[1]) < 80 || Math.Abs(box.Y - selection[2]) < 80, "the selection toolbar is far from the selection");

        await quick.Locator("button[aria-label='Bold']").ClickAsync();
        await Expect(Surface("rte-quick").Locator("b")).ToHaveTextAsync("toolbar");
        await Expect(quick.Locator("button[aria-label='Bold']")).ToHaveAttributeAsync("aria-pressed", "true");

        // A collapsed caret hides it.
        await CaretAtEndAsync("rte-quick");
        await Expect(quick).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Mentions_AtOpensSuggestions_AndEnterInsertsAChip()
    {
        await OpenAsync(Route);

        await Surface("rte-mentions").ClickAsync();
        await Page.Keyboard.TypeAsync("hi @");
        var search = Page.Locator("#rte-mentions .bit-rte-mention-menu input[role='combobox']");
        await Expect(search).ToBeFocusedAsync();
        var options = Page.Locator("#rte-mentions .bit-rte-mention-menu [role='option']");
        await Expect(options).ToHaveCountAsync(3);

        await Page.Keyboard.TypeAsync("al");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(options.Locator(".bit-rte-mention-name")).ToHaveTextAsync("Alan Turing");
        await Expect(options.Locator(".bit-rte-mention-desc")).ToHaveTextAsync("Computer scientist");
        await Page.Keyboard.PressAsync("Enter");

        var chip = Surface("rte-mentions").Locator("span.bit-rte-mention");
        await Expect(chip).ToHaveTextAsync("@Alan Turing");
        await Expect(chip).ToHaveAttributeAsync("data-mention-id", "u2");
        await Expect(Page.Locator("#last-mention")).ToHaveTextAsync("u2:Alan Turing");
        await Expect(Page.Locator("#mention-value")).ToContainTextAsync("data-mention-id=\"u2\"");

        // A search with no match says so; Escape dismisses it.
        await Page.Keyboard.TypeAsync(" @");
        await Expect(search).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("zz");
        await Expect(Page.Locator("#rte-mentions .bit-rte-mention-menu .bit-rte-emoji-empty")).ToHaveTextAsync("No matches");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#rte-mentions .bit-rte-mention-menu")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Emoji_ThePanelSearchesAndInsertsAnEmoji()
    {
        await OpenAsync(Route);

        await Page.Locator("#toolbar-select").SelectOptionAsync("Emoji");
        await Surface("rte-groups").ClickAsync();
        await Btn("rte-groups", "Insert emoji").ClickAsync();
        var panel = Page.Locator("#rte-groups .bit-rte-emoji-panel");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel.Locator("input[aria-label='Search emoji']")).ToBeFocusedAsync();
        Assert.IsTrue(await panel.Locator(".bit-rte-emoji").CountAsync() > 20);

        await panel.Locator("input[aria-label='Search emoji']").FillAsync("zzzz-none");
        await Expect(panel.Locator(".bit-rte-emoji-empty")).ToHaveTextAsync("No matches");
        await panel.Locator("input[aria-label='Search emoji']").FillAsync("");

        var first = panel.Locator(".bit-rte-emoji").First;
        var glyph = (await first.TextContentAsync())!;
        await first.ClickAsync();
        await Expect(Surface("rte-groups")).ToContainTextAsync(glyph);
    }

    [TestMethod]
    public async Task FullScreen_CoversTheViewport_AndEscapeLeavesIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#toolbar-select").SelectOptionAsync("FullScreen");
        var root = Page.Locator("#rte-groups");
        var toggle = Btn("rte-groups", "Toggle full screen");
        await toggle.ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-rte-fsc"));
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        var box = (await root.BoundingBoxAsync())!;
        Assert.AreEqual(Page.ViewportSize!.Width, box.Width, 2);
        Assert.AreEqual(Page.ViewportSize!.Height, box.Height, 2);

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => document.fullscreenElement !== null"), "the browser is not in full screen");

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => document.fullscreenElement === null"));

        // Leaving full screen through the browser (Escape, its own UI) is followed.
        await toggle.ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-rte-fsc"));
        await Page.EvaluateAsync("() => document.exitFullscreen()");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rte-fsc"));
    }

    [TestMethod]
    public async Task CustomToolbar_OrderAndCustomItems()
    {
        await OpenAsync(Route);

        var groups = Page.Locator("#rte-custom .bit-rte-tlb > *");
        var stamp = Page.Locator("#rte-custom .bit-rte-tlb button[aria-label='Insert stamp']");
        await Expect(stamp).ToHaveCountAsync(1);
        await Expect(stamp).ToContainTextAsync("Stamp");
        // The custom item comes first, then the inline group, then the history group.
        var firstLabel = await groups.First.EvaluateAsync<string>("e => e.matches('button') ? e.getAttribute('aria-label') : e.querySelector('button').getAttribute('aria-label')");
        Assert.AreEqual("Insert stamp", firstLabel);
        var lastLabel = await groups.Last.EvaluateAsync<string>("e => e.querySelector('button')?.getAttribute('aria-label') ?? e.getAttribute('aria-label')");
        Assert.AreEqual("Undo", lastLabel);

        await CaretAtEndAsync("rte-custom");
        await stamp.ClickAsync();
        await Expect(Surface("rte-custom")).ToContainTextAsync("custom[STAMP]");
        await Expect(Page.Locator("#custom-value")).ToContainTextAsync("[STAMP]");

        // IsActive follows the selection state.
        await SelectTextAsync("rte-custom", "custom");
        await Btn("rte-custom", "Bold").ClickAsync();
        await SelectTextAsync("rte-custom", "cust");
        await Expect(stamp).ToHaveClassAsync(HasClass("bit-rte-act"));
    }

    [TestMethod]
    public async Task ToolbarKeyboard_IsOneTabStop_WithArrowKeys()
    {
        await OpenAsync(Route);

        var buttons = Page.Locator("#rte-basic .bit-rte-tlb").Locator("button, select");
        var stops = await buttons.EvaluateAllAsync<string[]>("els => els.map(e => String(e.getAttribute('tabindex')))");
        Assert.AreEqual(1, stops.Count(t => t != "-1"), "the toolbar is not one tab stop");
        Assert.AreNotEqual("-1", stops[0]);

        await buttons.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(buttons.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(buttons.Last).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(buttons.First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(buttons.Last).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Form_LabelRequiredDescriptionAndCount()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-form-editor");
        var label = root.Locator(".bit-rte-lbl");
        await Expect(label).ToHaveTextAsync("Description");
        await Expect(label).ToHaveClassAsync(HasClass("bit-rte-req"));
        var surface = Surface("rte-form-editor");
        await Expect(surface).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await Expect(surface).ToHaveAttributeAsync("aria-required", "true");
        Assert.IsNull(await surface.GetAttributeAsync("aria-label"));
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Description" })).ToHaveCountAsync(1);

        var describedBy = (await surface.GetAttributeAsync("aria-describedby"))!;
        var description = root.Locator(".bit-rte-dsc");
        await Expect(description).ToHaveTextAsync("Plain words work best.");
        StringAssert.Contains(describedBy, (await description.GetAttributeAsync("id"))!);

        var count = root.Locator(".bit-rte-cnt");
        await Expect(count).ToContainTextAsync("0 words · 0/40 chars");

        await label.ClickAsync();
        await Expect(surface).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("one two");
        await Expect(count).ToContainTextAsync("2 words · 7/40 chars");
    }

    [TestMethod]
    public async Task Form_ValidationRunsAsTyped_MaxLengthIsEnforced_AndAValidFormSubmits()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-form-editor");
        var surface = Surface("rte-form-editor");
        await surface.ClickAsync();
        await Page.Keyboard.TypeAsync("short");
        await Expect(Page.Locator("#form .validation-message")).ToHaveTextAsync("Write at least 20 characters.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(surface).ToHaveAttributeAsync("aria-invalid", "true");

        await Page.Keyboard.TypeAsync(" but now it is long enough to pass the limit check");
        await Expect(root.Locator(".bit-rte-cnt")).ToContainTextAsync("/40 chars");
        await Expect(root.Locator(".bit-rte-cnt-over")).ToHaveTextAsync("limit reached");
        var text = await surface.InnerTextAsync();
        Assert.IsTrue(text.Trim().Length <= 40, $"MaxLength let {text.Trim().Length} characters in");
        await Expect(Page.Locator("#form .validation-message")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.ClickAsync("#btn-submit");
        await Expect(Page.Locator("#valid-submits")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Form_ErrorMessageAndInvalid_MarkTheEditor()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-error");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        var borderBefore = await root.EvaluateAsync<string>("r => getComputedStyle(r).borderTopColor");

        await Page.ClickAsync("#btn-set-error");
        var message = root.Locator(".bit-rte-erm");
        await Expect(message).ToHaveTextAsync("The server rejected it.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Surface("rte-error")).ToHaveAttributeAsync("aria-invalid", "true");
        StringAssert.Contains(await Surface("rte-error").GetAttributeAsync("aria-describedby"), (await message.GetAttributeAsync("id"))!);
        Assert.AreNotEqual(borderBefore, await root.EvaluateAsync<string>("r => getComputedStyle(r).borderTopColor"));

        // Editing the value clears the error the page set (@bind-Value:after is the page's), Invalid alone marks it too.
        await Page.ClickAsync("#btn-toggle-invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Events_FocusBlurAndSelectionChange()
    {
        await OpenAsync(Route);

        await SelectTextAsync("rte-events", "bold");
        await Expect(Page.Locator("#focus-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#selection-state")).ToHaveTextAsync(new Regex("^bold:True\\|italic:False\\|block:p\\|"));

        await SelectTextAsync("rte-events", "events");
        await Expect(Page.Locator("#selection-state")).ToHaveTextAsync(new Regex("^bold:False\\|italic:False\\|block:p\\|"));

        await Page.Locator("#after-events").FocusAsync();
        await Expect(Page.Locator("#blur-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Localization_TheLocalizerReplacesTheStringsItKnows()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#rte-localized .bit-rte-tlb")).ToHaveAttributeAsync("aria-label", "Formatierung");
        await Expect(Surface("rte-localized")).ToHaveAttributeAsync("aria-label", "Texteditor");
        await Expect(Btn("rte-localized", "Fett")).ToHaveAttributeAsync("title", "Fett (Strg+B)");
        await Expect(Btn("rte-localized", "Kursiv")).ToHaveCountAsync(1);
        await Expect(Btn("rte-localized", "Suchen und ersetzen")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#rte-localized select[aria-label='Paragraph format'] option[value='p']")).ToHaveTextAsync("Standard");
        await Expect(Page.Locator("#rte-localized .bit-rte-hint")).ToHaveTextAsync("Alt+0 zeigt die Tastenkürzel.");
        // A key it does not know keeps the English default.
        await Expect(Btn("rte-localized", "Underline")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Api_EveryPublicMethod()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-api-focus");
        await Expect(Surface("rte-api")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-api-get-html");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("html:<p>hello world</p>");
        await Page.ClickAsync("#btn-api-get-text");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("text:hello world");
        await Page.ClickAsync("#btn-api-counts");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("counts:11:2:False");

        await SelectTextAsync("rte-api", "world");
        await Page.ClickAsync("#btn-api-get-selected");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("selected:world");
        await SelectTextAsync("rte-api", "world");
        await Page.ClickAsync("#btn-api-bold");
        await Expect(Surface("rte-api").Locator("b")).ToHaveTextAsync("world");

        await CaretAtEndAsync("rte-api");
        await Page.ClickAsync("#btn-api-insert-text");
        await Expect(Surface("rte-api")).ToContainTextAsync("world <text>");
        await Page.ClickAsync("#btn-api-insert-html");
        await Expect(Surface("rte-api").Locator("em")).ToHaveTextAsync("emph");
        await Expect(Surface("rte-api").Locator("script")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-api-undo");
        await Expect(Surface("rte-api").Locator("em")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-api-redo");
        await Expect(Surface("rte-api").Locator("em")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-api-set-html");
        await Expect(Surface("rte-api").Locator("h2")).ToHaveTextAsync("Set by API");
        await Expect(Page.Locator("#api-value")).ToHaveTextAsync("<h2>Set by API</h2><p>two words</p>");

        await Page.ClickAsync("#btn-api-select-all");
        await Page.WaitForFunctionAsync("() => getSelection().toString().includes('Set by API') && getSelection().toString().includes('two words')");
        await Expect(Surface("rte-api")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-api-clear");
        await Expect(Surface("rte-api")).ToHaveTextAsync("");
        await Page.ClickAsync("#btn-api-counts");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("counts:0:0:True");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__rteInjected === undefined"));
    }

    [TestMethod]
    public async Task ReadOnly_TheSurfaceIsAReadableTabStop()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rte-ro"));
        var surface = Surface("rte-readonly");
        await Expect(surface).ToHaveAttributeAsync("contenteditable", "false");
        await Expect(surface).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(surface).ToHaveAttributeAsync("tabindex", "0");

        await surface.FocusAsync();
        await Expect(surface).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("typed");
        await Expect(surface).ToHaveTextAsync("Read-only content with find me twice: find me.");

        // Every control of the toolbar is off, the find panel (whose replace edits) included; the text stays selectable.
        await Expect(Btn("rte-readonly-toolbar", "Bold")).ToBeDisabledAsync();
        await Expect(Btn("rte-readonly-toolbar", "Bold")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Btn("rte-readonly-toolbar", "Find and replace")).ToBeDisabledAsync();
        await SelectTextAsync("rte-readonly", "content");
        Assert.AreEqual("content", await Page.EvaluateAsync<string>("() => getSelection().toString()"));
    }

    [TestMethod]
    public async Task Disabled_MarksEveryPart_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        var surface = Surface("rte-disabled");
        await Expect(surface).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(surface).ToHaveAttributeAsync("contenteditable", "false");
        Assert.IsNull(await surface.GetAttributeAsync("tabindex"));
        await Expect(Btn("rte-disabled", "Bold")).ToBeDisabledAsync();
        await Expect(Btn("rte-disabled", "Bold")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Btn("rte-disabled", "Keyboard shortcuts")).ToBeDisabledAsync();

        await surface.ClickAsync(new() { Force = true });
        await root.Locator(".bit-rte-lbl").ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");
        await surface.HoverAsync(new() { Force = true });
        await Expect(Page.Locator("#disabled-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-keys")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(surface).ToHaveAttributeAsync("contenteditable", "true");
        Assert.IsNull(await surface.GetAttributeAsync("aria-disabled"));
        await Expect(Btn("rte-disabled", "Bold")).ToBeEnabledAsync();
        await surface.ClickAsync();
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");
        await Page.Keyboard.PressAsync("a");
        await Expect(Page.Locator("#disabled-keys")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Cascading_BitParams_SetDefaultsAndAnEditorsOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Surface("rte-cascaded")).ToHaveAttributeAsync("aria-placeholder", "Cascaded placeholder");
        await Expect(Surface("rte-cascaded")).ToHaveClassAsync(HasClass("cascaded-editor"));
        await Expect(Page.Locator("#rte-cascaded .bit-rte-tlb")).ToHaveCountAsync(0);
        StringAssert.Contains(await Page.Locator("#rte-cascaded").GetAttributeAsync("style"), "--bit-RichTextEditor-height:9rem");

        await Expect(Surface("rte-cascaded-own")).ToHaveAttributeAsync("aria-placeholder", "My own placeholder");
        await Expect(Page.Locator("#rte-cascaded-own .bit-rte-tlb")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#rte-style")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Page.Locator("#rte-style")).ToHaveCSSAsync("border-top-color", "rgb(165, 42, 42)");
        await Expect(Page.Locator("#rte-style")).ToHaveCSSAsync("margin-top", "7px");

        var root = Page.Locator("#rte-styles");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(1, 1, 1)");
        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            (".bit-rte-lbl", "c-label", "color", "rgb(2, 2, 2)"),
            (".bit-rte-tlb", "c-toolbar", "background-color", "rgb(3, 3, 3)"),
            (".bit-rte-grp", "c-group", "padding-top", "3px"),
            (".bit-rte-tlb button", "c-button", "color", "rgb(4, 4, 4)"),
            (".bit-rte-edt", "c-editor", "color", "rgb(5, 5, 5)"),
            (".bit-rte-ftr", "c-footer", "background-color", "rgb(6, 6, 6)"),
            (".bit-rte-dsc", "c-description", "color", "rgb(7, 7, 7)"),
            (".bit-rte-cnt", "c-count", "color", "rgb(8, 8, 8)"),
        };
        foreach (var (selector, cls, property, value) in parts)
        {
            var part = root.Locator(selector).First;
            await Expect(part).ToHaveClassAsync(HasClass(cls));
            await Expect(part).ToHaveCSSAsync(property, value);
        }
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheEditor()
    {
        await OpenAsync(Route);

        var checks = new (string Selector, string Property, string Expected)[]
        {
            ("", "color", "rgb(11, 12, 13)"),
            ("", "background-color", "rgb(250, 240, 230)"),
            ("", "border-top-color", "rgb(1, 2, 3)"),
            ("", "border-top-width", "4px"),
            ("", "border-top-left-radius", "13px"),
            (" .bit-rte-tlb", "border-bottom-color", "rgb(4, 5, 6)"),
            (" .bit-rte-tlb", "background-color", "rgb(140, 150, 160)"),
            (" .bit-rte-edt", "padding-top", "17px"),
            (" .bit-rte-edt", "min-height", "150px"),
            (" .bit-rte-edt", "font-size", "19px"),
            (" .bit-rte-edt", "line-height", "38px"),
            (" .bit-rte-edt a", "color", "rgb(20, 30, 40)"),
            (" .bit-rte-edt code", "background-color", "rgb(50, 60, 70)"),
            (" .bit-rte-edt blockquote", "border-left-color", "rgb(80, 90, 100)"),
            (" .bit-rte-edt td", "border-top-color", "rgb(110, 120, 130)"),
            (" .bit-rte-tlb button", "width", "41px"),
            (" .bit-rte-tlb button", "color", "rgb(170, 180, 190)"),
            (" .bit-rte-tlb button", "border-top-left-radius", "9px"),
            (" .bit-rte-dsc", "color", "rgb(21, 22, 23)"),
            (" .bit-rte-cnt", "color", "rgb(24, 25, 26)"),
            (" .bit-rte-ftr", "background-color", "rgb(27, 28, 29)"),
        };

        foreach (var (selector, property, expected) in checks)
        {
            Assert.AreEqual(expected, await ComputedStyleAsync($"#rte-vars{selector}", property), $"{selector} {property}");
            Assert.AreNotEqual(expected, await ComputedStyleAsync($"#rte-vars-default{selector}", property), $"default {selector} {property}");
        }

        StringAssert.Contains(await ComputedStyleAsync("#rte-vars .bit-rte-edt", "font-family"), "Georgia");
        Assert.AreEqual("150px", await ComputedStyleAsync("#rte-vars .bit-rte-edt", "min-height"));
    }

    [TestMethod]
    public async Task Rtl_FlipsTheEditor_AndTheDirectionButtonsSetTheBlock()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#rte-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "rte-rtl", "rte-rtl-inherited" })
        {
            var toolbar = (await Page.Locator($"#{id} .bit-rte-tlb").BoundingBoxAsync())!;
            var first = (await Page.Locator($"#{id} .bit-rte-tlb button").First.BoundingBoxAsync())!;
            Assert.IsTrue(first.X > toolbar.X + toolbar.Width / 2, $"{id}: the first button is not on the right");
        }

        await SelectTextAsync("rte-rtl", "سلام");
        await Btn("rte-rtl", "Left to right").ClickAsync();
        await Expect(Surface("rte-rtl").Locator("[dir='ltr']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#rtl-value")).ToContainTextAsync("dir=\"ltr\"");

        // The toolbar's arrows follow what is on screen.
        var buttons = Page.Locator("#rte-rtl .bit-rte-tlb").Locator("button, select");
        await buttons.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(buttons.Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Escape_InADialog_ClosesTheLinkPanelFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dialog");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Surface("rte-dialog")).ToBeVisibleAsync();

        await SelectTextAsync("rte-dialog", "dialog");
        await Page.Keyboard.PressAsync("Control+k");
        var panel = Page.Locator("#rte-dialog .bit-rte-bar[role='group']");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Surface("rte-dialog").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoFocus_MovesTheFocusIntoTheEditorOnLoad()
    {
        await OpenAsync("/e2e/rich-text-editor/autofocus");

        await Expect(Page.Locator("#rte-autofocus .bit-rte-edt")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes_ReachTheRoot()
    {
        await OpenAsync("/e2e/rich-text-editor/autofocus");

        var root = Page.Locator("#rte-visibility");
        await Expect(root).ToHaveAttributeAsync("data-test", "rte-data");
        await Expect(root).ToHaveAttributeAsync("title", "Editor title");
        await Expect(root).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await root.BoundingBoxAsync())!.Height > 0, "a hidden editor keeps its box");

        await Page.ClickAsync("#btn-visibility");
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("Collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-visibility");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Lifecycle_Unmount_DisposesWithoutAnError()
    {
        await OpenAsync(Route);

        await Btn("rte-unmount", "Toggle full screen").ClickAsync();
        await Expect(Page.Locator("#rte-unmount")).ToHaveClassAsync(HasClass("bit-rte-fsc"));

        await Page.Locator("#btn-toggle-mount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#rte-unmount")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Surface("rte-unmount")).ToHaveTextAsync("unmount me");
        await Expect(Page.Locator("#rte-unmount")).Not.ToHaveClassAsync(HasClass("bit-rte-fsc"));

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
