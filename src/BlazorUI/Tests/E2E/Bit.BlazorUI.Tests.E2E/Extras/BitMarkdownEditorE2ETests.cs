using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitMarkdownEditor on <c>/e2e/markdown-editor</c>: typing and the two-way Value, DefaultValue and
/// OnChange, the Edit/Split/Preview modes (bound, one-way, F9, the toolbar), sync scroll, the stacked narrow layout,
/// Height/MinHeight/MaxHeight with the resize handle, AutoHeight, StickyToolbar, FullScreen (button, F11, Escape, scroll
/// lock, Tab kept inside), every command of the default toolbar and every keyboard shortcut, the toolbar's roving focus
/// and menus, custom toolbars (custom items, shortcuts, AlwaysEnabled, Text, IconName), the public methods (Run,
/// Insert, Undo/Redo, CanUndo/CanRedo, GetValue, Get/SetSelection, Replace, FindNext/FindPrevious, Focus/Blur,
/// ClearDraft), the preview (pipeline, template, debounce, empty text), the editing options (placeholder, spellcheck,
/// auto pairs, auto close, Tab indenting and the Escape-then-Tab way out, indent unit, list continuation, line moves),
/// the markdown spellings, ReadOnly, Disabled (with the page's handlers dropped), find and replace, the table tools,
/// image upload through the picker, paste and drop (with rejections), pasted tables and links, the status bar,
/// localized texts, EditForm validation, labels and descriptions, submit, the BitParams cascade, Style/Class/Styles/
/// Classes and the public CSS variables, RTL, the help dialog, Escape inside a dialog, autosaved drafts, prerendering
/// and unmounting.
/// </summary>
[TestClass]
public class BitMarkdownEditorE2ETests : E2ETestBase
{
    private const string Route = "/e2e/markdown-editor";

    private static ILocator Root(IPage page, string id) => page.Locator($"#{id}");

    private ILocator TextArea(string id) => Page.Locator($"#{id}-txt");

    private ILocator Button(string id, string cmd) => Page.Locator($"#{id} .bit-mde-tlb [data-cmd='{cmd}']");

    private async Task SetTextAsync(string id, string text, int? selectionStart = null, int? selectionEnd = null)
    {
        var textArea = TextArea(id);
        await textArea.FillAsync(text);
        var start = selectionStart ?? text.Length;
        var end = selectionEnd ?? start;
        await textArea.EvaluateAsync("(t, s) => { t.focus(); t.setSelectionRange(s[0], s[1]); t.dispatchEvent(new Event('select')); }", new[] { start, end });
    }

    private Task<int[]> SelectionAsync(string id)
    {
        return TextArea(id).EvaluateAsync<int[]>("t => [t.selectionStart, t.selectionEnd]");
    }

    [TestMethod]
    public async Task Prerender_TheServerHtmlHoldsTheToolbarAndTheSeededTextArea()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<div id=\"mde-bind\" class=\"bit-mde\"><div role=\"toolbar\" aria-label=\"Markdown formatting\"");
        StringAssert.Matches(html, new Regex("<textarea[^>]*id=\"mde-default-txt\"[^>]*>"));
        StringAssert.Contains(html, "# Default heading</textarea>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);
        await Expect(TextArea("mde-default")).ToHaveValueAsync("# Default heading");
    }

    [TestMethod]
    public async Task Binding_TypingUpdatesTheBoundValueAndRaisesOnChange()
    {
        await OpenAsync(Route);

        var textArea = TextArea("mde-bind");
        await Expect(textArea).ToHaveValueAsync("Initial **bound** value");

        await textArea.FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Page.Keyboard.TypeAsync(" typed");

        await Expect(Page.Locator("#bind-value")).ToHaveTextAsync("Initial **bound** value typed");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync("Initial **bound** value typed");
        Assert.IsTrue(int.Parse((await Page.Locator("#change-count").TextContentAsync())!) >= 1);

        // The preview follows.
        await Expect(Page.Locator("#mde-bind .bit-mde-ppn")).ToContainTextAsync("Initial bound value typed");
        await Expect(Page.Locator("#mde-bind .bit-mde-ppn strong")).ToHaveTextAsync("bound");
    }

    [TestMethod]
    public async Task Binding_AValueSetByThePage_ReachesTheTextAreaAndThePreview()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-set-value");
        await Expect(TextArea("mde-bind")).ToHaveValueAsync("# Set from the page");
        await Expect(Page.Locator("#mde-bind .bit-mde-ppn h1")).ToHaveTextAsync("Set from the page");

        // Editing the bound value from another control of the page.
        await Page.Locator("#page-input").FillAsync("from the page textarea");
        await Expect(TextArea("mde-bind")).ToHaveValueAsync("from the page textarea");

        await Page.ClickAsync("#btn-clear-value");
        await Expect(TextArea("mde-bind")).ToHaveValueAsync("");
        await Expect(Page.Locator("#mde-bind .bit-mde-emp")).ToHaveTextAsync("Nothing to preview yet.");

        // A value the page sets is not reported back as a change.
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task DefaultValue_SeedsTheEditorOnce_AndOnChangeReportsEdits()
    {
        await OpenAsync(Route);

        await Expect(TextArea("mde-default")).ToHaveValueAsync("# Default heading");
        await Expect(Page.Locator("#mde-default .bit-mde-ppn h1")).ToHaveTextAsync("Default heading");

        await TextArea("mde-default").FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Page.Keyboard.TypeAsync("!");
        await Expect(Page.Locator("#default-change")).ToHaveTextAsync("# Default heading!");
    }

    [TestMethod]
    public async Task Mode_EachModeShowsItsPanes_AndTheBindingFollowsBothWays()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-mode");
        var body = root.Locator(".bit-mde-bdy");
        var edit = root.Locator(".bit-mde-epn");
        var preview = root.Locator(".bit-mde-ppn");

        await Expect(body).ToHaveClassAsync(HasClass("bit-mde-split"));
        await Expect(edit).ToBeVisibleAsync();
        await Expect(preview).ToBeVisibleAsync();
        await Expect(root.Locator(".bit-mde-mod")).ToHaveTextAsync("Split");

        await Page.ClickAsync("#btn-mode-edit");
        await Expect(body).ToHaveClassAsync(HasClass("bit-mde-edit"));
        await Expect(preview).ToBeHiddenAsync();
        await Expect(edit).ToBeVisibleAsync();
        await Expect(root.Locator(".bit-mde-mod")).ToHaveTextAsync("Edit");
        // Nothing is rendered into the hidden preview.
        await Expect(preview.Locator(".bit-mdv")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mode-preview");
        await Expect(body).ToHaveClassAsync(HasClass("bit-mde-preview"));
        await Expect(edit).ToBeHiddenAsync();
        await Expect(preview.Locator("h1")).ToHaveTextAsync("Mode heading");
        await Expect(root.Locator(".bit-mde-mod")).ToHaveTextAsync("Preview");
        // The preview mode keeps aria-required only, never required, on the hidden textarea.

        // The toolbar's eye cycles Preview -> Edit -> Split and writes the mode back.
        await Button("mde-mode", "preview").ClickAsync();
        await Expect(Page.Locator("#mode-value")).ToHaveTextAsync("Edit");
        await Button("mde-mode", "preview").ClickAsync();
        await Expect(Page.Locator("#mode-value")).ToHaveTextAsync("Split");
        await Expect(root.Locator(".bit-mde-ann")).ToHaveTextAsync("Split mode");
    }

    [TestMethod]
    public async Task Mode_F9_CyclesTheModes_AndMovesTheFocusToTheVisiblePane()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-mode");
        await TextArea("mde-mode").FocusAsync();

        await Page.Keyboard.PressAsync("F9");
        await Expect(Page.Locator("#mode-value")).ToHaveTextAsync("Preview");
        await Expect(root.Locator(".bit-mde-ppn")).ToBeFocusedAsync();
        await Expect(root.Locator(".bit-mde-ann")).ToHaveTextAsync("Preview mode");

        // F9 in the preview pane goes on cycling.
        await Page.Keyboard.PressAsync("F9");
        await Expect(Page.Locator("#mode-value")).ToHaveTextAsync("Edit");
        await Expect(TextArea("mde-mode")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("F9");
        await Expect(Page.Locator("#mode-value")).ToHaveTextAsync("Split");
    }

    [TestMethod]
    public async Task Mode_BoundOneWay_TheToolbarCannotChangeIt()
    {
        await OpenAsync(Route);

        var body = Page.Locator("#mde-mode-oneway .bit-mde-bdy");
        await Expect(body).ToHaveClassAsync(HasClass("bit-mde-preview"));

        await Button("mde-mode-oneway", "preview").ClickAsync();
        await NextFramesAsync();
        await Expect(body).ToHaveClassAsync(HasClass("bit-mde-preview"));
    }

    [TestMethod]
    public async Task Mode_ANarrowSplitEditor_StacksItsPanes()
    {
        await OpenAsync(Route);

        var edit = await Page.Locator("#mde-narrow .bit-mde-epn").BoundingBoxAsync();
        var preview = await Page.Locator("#mde-narrow .bit-mde-ppn").BoundingBoxAsync();
        Assert.IsTrue(preview!.Y >= edit!.Y + edit.Height - 2, "the preview pane does not sit under the edit pane");
        Assert.AreEqual(edit.Width, preview.Width, 2);

        var wide = await Page.Locator("#mde-mode .bit-mde-epn").BoundingBoxAsync();
        var widePreview = await Page.Locator("#mde-mode .bit-mde-ppn").BoundingBoxAsync();
        Assert.AreEqual(wide!.Y, widePreview!.Y, 2, "a wide split editor stacked its panes");
    }

    [TestMethod]
    public async Task SyncScroll_ScrollingTheTextArea_ScrollsThePreview_UnlessTurnedOff()
    {
        await OpenAsync(Route);

        await TextArea("mde-sync").EvaluateAsync("t => { t.scrollTop = t.scrollHeight; t.dispatchEvent(new Event('scroll')); }");
        await Page.WaitForFunctionAsync("() => document.querySelector('#mde-sync .bit-mde-ppn').scrollTop > 0");

        await TextArea("mde-nosync").EvaluateAsync("t => { t.scrollTop = t.scrollHeight; t.dispatchEvent(new Event('scroll')); }");
        await NextFramesAsync();
        await NextFramesAsync();
        Assert.AreEqual(0, await Page.Locator("#mde-nosync .bit-mde-ppn").EvaluateAsync<double>("p => p.scrollTop"));
    }

    [TestMethod]
    public async Task Height_SizesTheBody_AndTheResizeHandleStaysBetweenMinAndMax()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-fullscreen");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mde-rsz"));
        StringAssert.Contains(await root.GetAttributeAsync("style"), "--bit-MarkdownEditor-height:10rem");
        var body = root.Locator(".bit-mde-bdy");
        await Expect(body).ToHaveCSSAsync("height", "160px");
        await Expect(body).ToHaveCSSAsync("resize", "vertical");
        await Expect(body).ToHaveCSSAsync("min-height", "96px");
        await Expect(body).ToHaveCSSAsync("max-height", "320px");

        // Drag the handle well past the maximum, then well past the minimum.
        await body.ScrollIntoViewIfNeededAsync();
        var box = (await body.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y + box.Height - 3);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y + box.Height + 400, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await Expect(body).ToHaveCSSAsync("height", "320px");

        box = (await body.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y + box.Height - 3);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y - 300, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await Expect(body).ToHaveCSSAsync("height", "96px");
    }

    [TestMethod]
    public async Task FullScreen_CoversTheViewport_LocksTheScroll_AndEscapeLeavesIt()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-fullscreen");
        await Page.ClickAsync("#btn-fullscreen");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mde-fsc"));
        await Expect(root).ToHaveCSSAsync("position", "fixed");
        await Expect(Button("mde-fullscreen", "fullscreen")).ToHaveAttributeAsync("aria-pressed", "true");

        var viewport = Page.ViewportSize!;
        var box = (await root.BoundingBoxAsync())!;
        Assert.AreEqual(0, box.X, 1);
        Assert.AreEqual(0, box.Y, 1);
        Assert.AreEqual(viewport.Width, box.Width, 1);
        Assert.AreEqual(viewport.Height, box.Height, 1);

        // The page behind it does not scroll.
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.body).overflow === 'hidden' || getComputedStyle(document.documentElement).overflow === 'hidden'");

        await TextArea("mde-fullscreen").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#fullscreen-value")).ToHaveTextAsync("False");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-mde-fsc"));
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.body).overflow !== 'hidden' && getComputedStyle(document.documentElement).overflow !== 'hidden'");

        // F11 and the toolbar button enter it again and write the binding back.
        await TextArea("mde-fullscreen").FocusAsync();
        await Page.Keyboard.PressAsync("F11");
        await Expect(Page.Locator("#fullscreen-value")).ToHaveTextAsync("True");
        await Button("mde-fullscreen", "fullscreen").ClickAsync();
        await Expect(Page.Locator("#fullscreen-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task FullScreen_KeepsTabInsideTheEditor()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-fullscreen");
        var root = Root(Page, "mde-fullscreen");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mde-fsc"));

        // The last tab stop is the preview pane; Tab from it comes back to the toolbar.
        await root.Locator(".bit-mde-ppn").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        var inside = await root.EvaluateAsync<bool>("r => r.contains(document.activeElement)");
        Assert.IsTrue(inside, "Tab left the full-screen editor");
        await Expect(root.Locator(".bit-mde-tlb .bit-mde-btn[tabindex='0']")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(root.Locator(".bit-mde-ppn")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoHeight_GrowsWithTheContentUpToMaxHeight()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-autoheight");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mde-ahg"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-mde-stk"));
        var body = root.Locator(".bit-mde-bdy");
        var initial = (await body.BoundingBoxAsync())!.Height;
        Assert.AreEqual(80, initial, 2, "AutoHeight starts at MinHeight");

        await SetTextAsync("mde-autoheight", string.Join("\n", Enumerable.Range(1, 6).Select(i => $"line {i}")));
        await Page.WaitForFunctionAsync($"() => document.querySelector('#mde-autoheight .bit-mde-bdy').getBoundingClientRect().height > {initial + 20}");

        await SetTextAsync("mde-autoheight", string.Join("\n", Enumerable.Range(1, 60).Select(i => $"line {i}")));
        await Page.WaitForFunctionAsync("() => Math.abs(document.querySelector('#mde-autoheight .bit-mde-bdy').getBoundingClientRect().height - 240) < 2");
    }

    [TestMethod]
    public async Task StickyToolbar_StaysInViewWhileThePageScrollsPastTheEditor()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-autoheight", string.Join("\n", Enumerable.Range(1, 60).Select(i => $"line {i}")));
        var toolbar = Page.Locator("#mde-autoheight .bit-mde-tlb");
        await Expect(toolbar).ToHaveCSSAsync("position", "sticky");

        // Scroll so the editor's top is above the viewport while its body is still on screen.
        await Page.Locator("#mde-autoheight").EvaluateAsync("r => window.scrollTo(0, window.scrollY + r.getBoundingClientRect().top + 120)");
        await Page.WaitForFunctionAsync("() => Math.abs(document.querySelector('#mde-autoheight .bit-mde-tlb').getBoundingClientRect().top) < 2");
        Assert.IsTrue(await Page.Locator("#mde-autoheight").EvaluateAsync<bool>("r => r.getBoundingClientRect().top < -50"));
    }

    [TestMethod]
    [DataRow("bold", "**word**")]
    [DataRow("italic", "*word*")]
    [DataRow("strikethrough", "~~word~~")]
    [DataRow("quote", "> word")]
    [DataRow("ul", "- word")]
    [DataRow("ol", "1. word")]
    [DataRow("task", "- [ ] word")]
    [DataRow("link", "[word](url)")]
    [DataRow("image", "![word](url)")]
    [DataRow("code", "`word`")]
    [DataRow("codeblock", "```\nword\n```")]
    public async Task Toolbar_EachCommand_TransformsTheSelection(string cmd, string expected)
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 0, 4);
        await Button("mde-cmd", cmd).ClickAsync();

        await Expect(TextArea("mde-cmd")).ToHaveValueAsync(expected);
        await Expect(Page.Locator("#cmd-value")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    [DataRow("bold", "**bold text**", "bold text")]
    [DataRow("italic", "*italic text*", "italic text")]
    [DataRow("strikethrough", "~~strikethrough~~", "strikethrough")]
    [DataRow("code", "`code`", "code")]
    [DataRow("link", "[text](url)", "text")]
    [DataRow("image", "![alt](url)", "alt")]
    public async Task Toolbar_ACommandOnAnEmptySelection_InsertsAPlaceholderAndSelectsIt(string cmd, string expected, string selected)
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "");
        await Button("mde-cmd", cmd).ClickAsync();

        await Expect(TextArea("mde-cmd")).ToHaveValueAsync(expected);
        var selection = await SelectionAsync("mde-cmd");
        Assert.AreEqual(selected, expected[selection[0]..selection[1]]);
    }

    [TestMethod]
    public async Task Toolbar_InsertCommands_TableHorizontalRuleAndClearFormatting()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "");
        await Button("mde-cmd", "table").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("| Column 1 | Column 2 |\n| -------- | -------- |\n| Cell     | Cell     |\n");

        await SetTextAsync("mde-cmd", "");
        await Button("mde-cmd", "hr").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("---\n");

        await SetTextAsync("mde-cmd", "**bold** and *italic*", 0, 21);
        await Button("mde-cmd", "clear").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("bold and italic");
    }

    [TestMethod]
    public async Task Toolbar_AToggleCommandAppliedTwice_UnwrapsAgain()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 0, 4);
        await Button("mde-cmd", "bold").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");
        await Button("mde-cmd", "bold").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("word");

        await Button("mde-cmd", "quote").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("> word");
        await Button("mde-cmd", "quote").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("word");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    public async Task Toolbar_HeadingMenu_EachLevelPrefixesTheLine(int level)
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 2, 2);
        var trigger = Button("mde-cmd", "heading");
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        var item = Page.Locator($"#mde-cmd .bit-mde-ddm [data-cmd='h{level}']");
        await Expect(item).ToBeVisibleAsync();
        await Expect(item).ToHaveAttributeAsync("role", "menuitemradio");
        await item.ClickAsync();

        await Expect(TextArea("mde-cmd")).ToHaveValueAsync($"{new string('#', level)} word");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        // The caret's heading level is reported on the closed trigger and on its menu item.
        await Expect(trigger).ToHaveClassAsync(HasClass("bit-mde-act"));
        await Expect(item).ToHaveAttributeAsync("aria-checked", "true");
    }

    [TestMethod]
    public async Task Toolbar_ActiveFormats_FollowTheCaret()
    {
        await OpenAsync(Route);

        var bold = Button("mde-cmd", "bold");
        var italic = Button("mde-cmd", "italic");
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");

        await SetTextAsync("mde-cmd", "**strong** and *em*", 4, 4);
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(bold).ToHaveClassAsync(HasClass("bit-mde-act"));
        await Expect(italic).ToHaveAttributeAsync("aria-pressed", "false");

        // The pressed look reads the active background.
        var activeBackground = await bold.EvaluateAsync<string>("b => getComputedStyle(b).backgroundColor");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", activeBackground);

        await TextArea("mde-cmd").EvaluateAsync("t => { t.setSelectionRange(17, 17); t.dispatchEvent(new Event('select')); document.dispatchEvent(new Event('selectionchange')); }");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(italic).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");

        // An insert-only command reports no pressed state at all.
        Assert.IsNull(await Button("mde-cmd", "link").GetAttributeAsync("aria-pressed"));
    }

    [TestMethod]
    public async Task Toolbar_UndoAndRedoButtons_FollowTheHistory()
    {
        await OpenAsync(Route);

        var undo = Button("mde-cmd", "undo");
        var redo = Button("mde-cmd", "redo");
        await Expect(undo).ToBeDisabledAsync();
        await Expect(undo).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(redo).ToBeDisabledAsync();

        await SetTextAsync("mde-cmd", "word", 0, 4);
        await Button("mde-cmd", "bold").ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");
        await Expect(undo).ToBeEnabledAsync();

        await undo.ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("word");
        await Expect(redo).ToBeEnabledAsync();

        await redo.ClickAsync();
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");
        await Expect(Page.Locator("#cmd-value")).ToHaveTextAsync("**word**");
    }

    [TestMethod]
    [DataRow("Control+b", "**word**")]
    [DataRow("Control+i", "*word*")]
    [DataRow("Control+Shift+KeyS", "~~word~~")]
    [DataRow("Control+e", "`word`")]
    [DataRow("Control+Alt+KeyC", "```\nword\n```")]
    [DataRow("Control+k", "[word](url)")]
    [DataRow("Control+Alt+Digit1", "# word")]
    [DataRow("Control+Alt+Digit2", "## word")]
    [DataRow("Control+Alt+Digit3", "### word")]
    [DataRow("Control+Alt+Digit4", "#### word")]
    [DataRow("Control+Alt+Digit5", "##### word")]
    [DataRow("Control+Alt+Digit6", "###### word")]
    [DataRow("Control+Shift+Period", "> word")]
    [DataRow("Control+Shift+Digit7", "1. word")]
    [DataRow("Control+Shift+Digit8", "- word")]
    [DataRow("Control+Shift+Digit9", "- [ ] word")]
    [DataRow("Control+d", "word\nword")]
    [DataRow("Control+Shift+KeyD", "")]
    public async Task Shortcuts_EachKeyboardShortcut_RunsItsCommand(string keys, string expected)
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 0, 4);
        await Page.Keyboard.PressAsync(keys);

        await Expect(TextArea("mde-cmd")).ToHaveValueAsync(expected);
        await Expect(Page.Locator("#cmd-value")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task Shortcuts_UndoRedo_WithControlZYAndShiftZ()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 0, 4);
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");

        await Page.Keyboard.PressAsync("Control+z");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("word");
        await Page.Keyboard.PressAsync("Control+y");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");
        await Page.Keyboard.PressAsync("Control+z");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("word");
        await Page.Keyboard.PressAsync("Control+Shift+KeyZ");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("**word**");
    }

    [TestMethod]
    public async Task Shortcuts_AltArrows_MoveTheLine()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "one\ntwo\nthree", 5, 5);
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("two\none\nthree");

        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("one\ntwo\nthree");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("one\nthree\ntwo");
    }

    [TestMethod]
    public async Task Shortcuts_PressedFasterThanTheRoundTrip_AreAllApplied()
    {
        await OpenAsync(Route);

        // A second command pressed while the first is still on its way to .NET and back runs after it, never dropped.
        await SetTextAsync("mde-cmd", "one\ntwo\nthree", 1, 1);
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("two\nthree\none");

        await SetTextAsync("mde-cmd", "- item");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync(new Regex("^- item\\n\\s*$"));
    }

    [TestMethod]
    public async Task Shortcuts_EnterContinuesAListAndEndsItOnAnEmptyItem()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "- first");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("- first\n- ");
        await Page.Keyboard.TypeAsync("second");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("- first\n- second\n- ");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync(new Regex("^- first\\n- second\\n\\s*$"));

        await SetTextAsync("mde-cmd", "1. one");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("1. one\n2. ");

        await SetTextAsync("mde-cmd", "- [x] done");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("- [x] done\n- [ ] ");

        await SetTextAsync("mde-cmd", "> quoted");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("> quoted\n> ");

        // A plain line takes a plain newline.
        await SetTextAsync("mde-cmd", "plain");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("plain\n");
    }

    [TestMethod]
    public async Task Shortcuts_TabIndentsAListItem_ShiftTabOutdents_EscapeThenTabLeaves()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-indent", "- item", 3, 3);
        await Page.Keyboard.PressAsync("Tab");
        await Expect(TextArea("mde-indent")).ToHaveValueAsync("    - item");
        await Expect(TextArea("mde-indent")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(TextArea("mde-indent")).ToHaveValueAsync("- item");

        // Escape, then Tab, always leaves: the editor never traps the keyboard.
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(TextArea("mde-indent")).Not.ToBeFocusedAsync();
        await Expect(TextArea("mde-indent")).ToHaveValueAsync("- item");
    }

    [TestMethod]
    public async Task Toolbar_IsOneTabStop_TheArrowsHomeAndEndMoveAlongIt()
    {
        await OpenAsync(Route);

        // Undo and redo are disabled, so the first stop is Bold.
        var stop = Page.Locator("#mde-cmd .bit-mde-tlb .bit-mde-btn[tabindex='0']");
        await Expect(stop).ToHaveCountAsync(1);
        await Expect(stop).ToHaveAttributeAsync("data-cmd", "bold");

        await stop.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button("mde-cmd", "italic")).ToBeFocusedAsync();
        await Expect(Button("mde-cmd", "italic")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Button("mde-cmd", "bold")).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Keyboard.PressAsync("End");
        await Expect(Button("mde-cmd", "help")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button("mde-cmd", "bold")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Button("mde-cmd", "help")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Button("mde-cmd", "bold")).ToBeFocusedAsync();

        // Tab leaves the toolbar for the textarea.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(TextArea("mde-cmd")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Toolbar_Menus_OpenFromTheKeyboardAndThePointer()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-cmd", "word", 1, 1);
        var trigger = Button("mde-cmd", "heading");
        await Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "menu");
        var menu = Page.Locator("#" + await trigger.GetAttributeAsync("aria-controls"));
        await Expect(menu).ToHaveAttributeAsync("role", "menu");
        await Expect(menu).Not.ToBeVisibleAsync();

        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu.Locator("[data-cmd='h1']")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu.Locator("[data-cmd='h2']")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(menu.Locator("[data-cmd='h6']")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(menu.Locator("[data-cmd='h1']")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(menu.Locator("[data-cmd='h6']")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(menu.Locator("[data-cmd='h1']")).ToBeFocusedAsync();

        // Escape closes the menu and hands the focus back to its trigger, and the editor is left alone.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).Not.ToBeVisibleAsync();
        await Expect(trigger).ToBeFocusedAsync();

        // Enter opens it too, and Enter on an item runs it and closes the menu.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(menu.Locator("[data-cmd='h1']")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TextArea("mde-cmd")).ToHaveValueAsync("## word");
        await Expect(menu).Not.ToBeVisibleAsync();
        // The command hands the keyboard back to the text it changed.
        await Expect(TextArea("mde-cmd")).ToBeFocusedAsync();

        // The pointer opens it on hover, and a press outside closes it.
        await Button("mde-cmd", "tabletools").HoverAsync();
        await Expect(Button("mde-cmd", "tabletools")).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Mouse.MoveAsync(5, 5);
        await Expect(Button("mde-cmd", "tabletools")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task CustomToolbar_RendersItsItems_AndRunsCustomItemsAndShortcuts()
    {
        await OpenAsync(Route);

        var toolbar = Page.Locator("#mde-custom .bit-mde-tlb");
        await Expect(toolbar.Locator(":scope > .bit-mde-btn, :scope > .bit-mde-dd")).ToHaveCountAsync(6);
        await Expect(toolbar.Locator(":scope > .bit-mde-sep")).ToHaveCountAsync(1);

        await Expect(Button("mde-custom", "bold")).ToHaveAttributeAsync("title", "Bold (Ctrl+B)");
        await Expect(Button("mde-custom", "bold")).ToHaveAttributeAsync("aria-keyshortcuts", "Control+B");
        await Expect(Button("mde-custom", "sup").Locator("i.bit-mde-ico")).ToHaveCountAsync(1);
        // An item with no title is named by its text, which it spells out.
        await Expect(Button("mde-custom", "lowered")).ToHaveAttributeAsync("aria-label", "Sub");
        await Expect(Button("mde-custom", "lowered")).ToHaveClassAsync(HasClass("bit-mde-btx"));
        await Expect(Button("mde-custom", "lowered").Locator(".bit-mde-mit")).ToHaveTextAsync("Sub");
        await Expect(Button("mde-custom", "count")).ToHaveAttributeAsync("title", "Count words (F2)");

        await SetTextAsync("mde-custom", "x2", 1, 2);
        await Button("mde-custom", "sup").ClickAsync();
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("x^2^");
        await SetTextAsync("mde-custom", "H2O", 1, 2);
        await Button("mde-custom", "lowered").ClickAsync();
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("H~2~O");

        // A custom item gets the editor.
        await SetTextAsync("mde-custom", "a b");
        await Button("mde-custom", "stamp").ClickAsync();
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("a b[STAMP]");
        await Expect(Page.Locator("#toolbar-status")).ToHaveTextAsync("stamped");
        await Expect(Page.Locator("#custom-value")).ToHaveTextAsync("a b[STAMP]");

        // Its shortcuts are bound.
        await SetTextAsync("mde-custom", "one two three");
        await Page.Keyboard.PressAsync("F2");
        await Expect(Page.Locator("#toolbar-status")).ToHaveTextAsync("words:3");
        await Page.Keyboard.PressAsync("Control+Shift+KeyK");
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("one two three[STAMP]");

        // Commands in a custom dropdown.
        await SetTextAsync("mde-custom", "one\ntwo", 5, 5);
        await Button("mde-custom", "lines").ClickAsync();
        await Page.Locator("#mde-custom .bit-mde-ddm [data-cmd='moveup']").ClickAsync();
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("two\none");
        await Expect(Page.Locator("#mde-custom .bit-mde-ddm .bit-mde-msep")).ToHaveAttributeAsync("role", "separator");
    }

    [TestMethod]
    public async Task CustomToolbar_ReadOnly_DisablesEditingItemsButKeepsAlwaysEnabledOnes()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-custom-readonly");
        await Expect(Page.Locator("#custom-readonly")).ToHaveTextAsync("True");

        await Expect(Button("mde-custom", "bold")).ToBeDisabledAsync();
        await Expect(Button("mde-custom", "stamp")).ToBeDisabledAsync();
        await Expect(Button("mde-custom", "lines")).ToBeDisabledAsync();
        await Expect(Button("mde-custom", "count")).ToBeEnabledAsync();

        await Button("mde-custom", "count").ClickAsync();
        await Expect(Page.Locator("#toolbar-status")).ToHaveTextAsync("words:1");
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("custom");

        // The shortcut of a disabled custom item does nothing.
        await TextArea("mde-custom").FocusAsync();
        await Page.Keyboard.PressAsync("Control+Shift+KeyK");
        await NextFramesAsync();
        await Expect(TextArea("mde-custom")).ToHaveValueAsync("custom");
    }

    [TestMethod]
    public async Task ShowToolbarAndShowStatusBar_False_HideTheChrome()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mde-nochrome .bit-mde-tlb")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mde-nochrome .bit-mde-sbr")).ToHaveCountAsync(0);
        await Expect(TextArea("mde-nochrome")).ToBeVisibleAsync();

        // The shortcuts still work without a toolbar.
        await SetTextAsync("mde-nochrome", "word", 0, 4);
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(TextArea("mde-nochrome")).ToHaveValueAsync("**word**");
    }

    [TestMethod]
    public async Task Api_RunInsertAndGetValue()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-run-h1");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("# hello world");

        await TextArea("mde-api").EvaluateAsync("t => t.setSelectionRange(t.value.length, t.value.length)");
        await Page.ClickAsync("#btn-insert");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("# hello world<INS>");

        await Page.ClickAsync("#btn-run-duplicate");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("# hello world<INS>\n# hello world<INS>");

        await Page.ClickAsync("#btn-get-value");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("value:# hello world<INS>\n# hello world<INS>");
    }

    [TestMethod]
    public async Task Api_UndoRedo_FollowCanUndoAndCanRedo()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#btn-undo")).ToBeDisabledAsync();
        await Page.ClickAsync("#btn-run-h1");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("# hello world");
        await Expect(Page.Locator("#can-undo")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#btn-undo")).ToBeEnabledAsync();

        await Page.ClickAsync("#btn-undo");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("hello world");
        await Expect(Page.Locator("#can-redo")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-redo");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("# hello world");
        await Expect(Page.Locator("#can-redo")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Api_GetSelectionAndSetSelection()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-set-selection");
        // The selection is set through a round trip, so it is waited for rather than read once.
        await Page.WaitForFunctionAsync("t => t.selectionStart === 0 && t.selectionEnd === 5", await TextArea("mde-api").ElementHandleAsync());
        Assert.AreEqual("0,5", string.Join(",", await SelectionAsync("mde-api")));

        await Page.ClickAsync("#btn-get-selection");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("selection:0-5:hello:False");

        await Page.ClickAsync("#btn-run-bold");
        await Expect(TextArea("mde-api")).ToHaveValueAsync("**hello** world");
    }

    [TestMethod]
    public async Task Api_FindAndReplace()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-find-next");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("find:1/2:True");
        var selection = await SelectionAsync("mde-api");
        Assert.AreEqual("o", "hello world"[selection[0]..selection[1]]);

        await Page.ClickAsync("#btn-find-next");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("find:2/2:True");

        await Page.ClickAsync("#btn-find-previous");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("find:1/2:True");

        await Page.ClickAsync("#btn-replace-first");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("replaced:1");
        await Expect(TextArea("mde-api")).ToHaveValueAsync(new Regex("^hell[Q]? w[oQ]rld$"));

        await Page.ClickAsync("#btn-replace");
        await Expect(Page.Locator("#api-result")).ToHaveTextAsync("replaced:1");
        await Expect(TextArea("mde-api")).Not.ToHaveValueAsync(new Regex("o"));
    }

    [TestMethod]
    public async Task Api_FocusAndBlur_RaiseOnFocusAndOnBlur()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-focus");
        await Expect(TextArea("mde-api")).ToBeFocusedAsync();
        await Expect(Page.Locator("#api-focus")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-blur");
        await Expect(Page.Locator("#api-blur")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-focus");
        await Expect(TextArea("mde-api")).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-blur");
        await Expect(TextArea("mde-api")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Preview_PipelineTemplateDebounceAndEmptyText()
    {
        await OpenAsync(Route);

        // The default preview is the GitHub flavor; PreviewPipeline swaps it.
        await Expect(Page.Locator("#mde-preview-github .bit-mde-ppn del")).ToHaveTextAsync("strike");
        await Expect(Page.Locator("#mde-preview-github .bit-mde-ppn table")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#mde-preview-github .bit-mde-ppn a[href='https://bitplatform.dev']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#mde-preview-basic .bit-mde-ppn del, #mde-preview-basic .bit-mde-ppn table, #mde-preview-basic .bit-mde-ppn a")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mde-preview-basic .bit-mde-ppn")).ToContainTextAsync("~~strike~~");

        var raw = Page.Locator("#mde-preview-template .raw-preview");
        await Expect(raw).ToHaveTextAsync("~~strike~~ and https://bitplatform.dev\n\n| a | b |\n| - | - |\n| 1 | 2 |");

        await Expect(Page.Locator("#mde-preview-empty .bit-mde-emp")).ToHaveTextAsync("Nothing to preview yet.");

        // The preview settles after the debounce rather than on every key.
        await SetTextAsync("mde-preview-template", "changed");
        await Expect(raw).Not.ToHaveTextAsync("changed", new() { Timeout = 300 });
        await Expect(raw).ToHaveTextAsync("changed");

        // The preview pane is a named, focusable group.
        var pane = Page.Locator("#mde-preview-github .bit-mde-ppn");
        await Expect(pane).ToHaveAttributeAsync("role", "group");
        await Expect(pane).ToHaveAttributeAsync("aria-label", "Markdown preview");
        await Expect(pane).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task Editing_PlaceholderSpellCheckAndAutoClosePairs()
    {
        await OpenAsync(Route);

        var textArea = TextArea("mde-editing");
        await Expect(textArea).ToHaveAttributeAsync("placeholder", "Type a ( here");
        await Expect(textArea).ToHaveAttributeAsync("spellcheck", "false");

        await SetTextAsync("mde-editing", "");
        await Page.Keyboard.TypeAsync("(");
        await Expect(textArea).ToHaveValueAsync("()");
        Assert.AreEqual("1,1", string.Join(",", await SelectionAsync("mde-editing")));

        await Page.Keyboard.TypeAsync("x");
        await Page.Keyboard.TypeAsync(")");
        await Expect(textArea).ToHaveValueAsync("(x)");

        await SetTextAsync("mde-editing", "");
        await Page.Keyboard.TypeAsync("[");
        await Expect(textArea).ToHaveValueAsync("[]");
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(textArea).ToHaveValueAsync("");

        // AutoPair wraps a selection in a typed pairing character.
        await SetTextAsync("mde-editing", "word", 0, 4);
        await Page.Keyboard.TypeAsync("*");
        await Expect(textArea).ToHaveValueAsync("*word*");

        // ChangeDebounceTime holds the value back, but it does reach .NET.
        await Expect(Page.Locator("#editing-value")).ToHaveTextAsync("*word*");
    }

    [TestMethod]
    public async Task Editing_TabIndentsFalse_LetsTabLeave_AndAutoPairFalseTypesOverTheSelection()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-editing", "text");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-editing")).ToBeFocusedAsync();
        await Expect(TextArea("mde-editing")).ToHaveValueAsync("text");

        await SetTextAsync("mde-indent", "word", 0, 4);
        await Page.Keyboard.TypeAsync("*");
        await Expect(TextArea("mde-indent")).ToHaveValueAsync("*");
    }

    [TestMethod]
    public async Task MarkdownSpelling_BoldItalicBulletAndTableSize()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-spelling", "word", 0, 4);
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(TextArea("mde-spelling")).ToHaveValueAsync("__word__");

        await SetTextAsync("mde-spelling", "word", 0, 4);
        await Page.Keyboard.PressAsync("Control+i");
        await Expect(TextArea("mde-spelling")).ToHaveValueAsync("_word_");

        await SetTextAsync("mde-spelling", "word", 0, 4);
        await Button("mde-spelling", "ul").ClickAsync();
        await Expect(TextArea("mde-spelling")).ToHaveValueAsync("* word");

        await SetTextAsync("mde-spelling", "word", 0, 4);
        await Button("mde-spelling", "task").ClickAsync();
        await Expect(TextArea("mde-spelling")).ToHaveValueAsync("* [ ] word");

        await SetTextAsync("mde-spelling", "");
        await Button("mde-spelling", "table").ClickAsync();
        await Expect(TextArea("mde-spelling")).ToHaveValueAsync(new Regex("Column 4"));
        var lines = (await TextArea("mde-spelling").InputValueAsync()).TrimEnd('\n').Split('\n');
        Assert.AreEqual(5, lines.Length, "a header, a separator and three rows");
        StringAssert.Contains(lines[0], "Column 4");
        Assert.AreEqual(5, lines[0].Count(c => c == '|'));

        await SetTextAsync("mde-plus", "word", 0, 4);
        await Button("mde-plus", "ul").ClickAsync();
        await Expect(TextArea("mde-plus")).ToHaveValueAsync("+ word");
        await Expect(Page.Locator("#plus-value")).ToHaveTextAsync("+ word");
    }

    [TestMethod]
    public async Task ReadOnly_KeepsTheTextSelectableButUnchangeable()
    {
        await OpenAsync(Route);

        var textArea = TextArea("mde-readonly");
        await Expect(textArea).ToHaveAttributeAsync("readonly", "");
        Assert.IsNull(await textArea.GetAttributeAsync("disabled"));

        await textArea.FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Page.Keyboard.TypeAsync("typed");
        await Page.Keyboard.PressAsync("Control+b");
        await Expect(textArea).ToHaveValueAsync("# Read-only\n\nselect me");

        await Expect(Button("mde-readonly", "bold")).ToBeDisabledAsync();
        await Expect(Button("mde-readonly", "undo")).ToBeDisabledAsync();
        // A menu whose every item edits is closed off too, separators or not.
        await Expect(Button("mde-readonly", "heading")).ToBeDisabledAsync();
        await Expect(Button("mde-readonly", "tabletools")).ToBeDisabledAsync();
        await Expect(Button("mde-readonly", "find")).ToBeEnabledAsync();
        await Expect(Button("mde-readonly", "preview")).ToBeEnabledAsync();

        // Selecting and searching still work.
        await Page.Keyboard.PressAsync("Control+a");
        var selection = await SelectionAsync("mde-readonly");
        Assert.AreEqual(selection[1] - selection[0], "# Read-only\n\nselect me".Length);
        await Expect(Page.Locator("#mde-readonly .bit-mde-ppn h1")).ToHaveTextAsync("Read-only");
        await Expect(Page.Locator("#readonly-value")).ToHaveTextAsync("# Read-only\n\nselect me");
    }

    [TestMethod]
    public async Task Disabled_MarksEveryPart_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        var textArea = TextArea("mde-disabled");
        await Expect(textArea).ToBeDisabledAsync();
        await Expect(textArea).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Button("mde-disabled", "bold")).ToBeDisabledAsync();
        await Expect(Button("mde-disabled", "bold")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Button("mde-disabled", "find")).ToBeDisabledAsync();
        await Expect(Button("mde-disabled", "heading")).ToBeDisabledAsync();

        // A disabled editor runs none of the page's activation handlers, but keeps the hover one.
        await root.Locator(".bit-mde-sbr").ClickAsync();
        await root.Locator(".bit-mde-lbl").ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");
        await root.Locator(".bit-mde-sbr").HoverAsync();
        await Expect(Page.Locator("#disabled-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-keys")).ToHaveTextAsync("0");

        // The disabled colors.
        var disabledColor = await textArea.EvaluateAsync<string>("t => getComputedStyle(t).color");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Page.Locator("#disabled-value")).ToHaveTextAsync("False");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(textArea).ToBeEnabledAsync();
        Assert.IsNull(await textArea.GetAttributeAsync("aria-disabled"));
        Assert.AreNotEqual(disabledColor, await textArea.EvaluateAsync<string>("t => getComputedStyle(t).color"));

        await root.Locator(".bit-mde-sbr").ClickAsync();
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("1");
        await textArea.FocusAsync();
        await Page.Keyboard.PressAsync("a");
        await Expect(Page.Locator("#disabled-keys")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Find_PanelWalksMatchesReplacesAndCloses()
    {
        await OpenAsync(Route);

        // Ctrl+F opens the panel seeded with the selection and focuses the find box.
        await SetTextAsync("mde-find", (await TextArea("mde-find").InputValueAsync()), 4, 12);
        await Page.Keyboard.PressAsync("Control+f");
        var panel = Page.Locator("#mde-find .bit-mde-fnd");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("role", "region");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Find and replace");
        var find = panel.Locator("input[type='search']");
        await Expect(find).ToBeFocusedAsync();
        await Expect(find).ToHaveValueAsync("markdown");
        await Expect(Button("mde-find", "find")).ToHaveAttributeAsync("aria-pressed", "true");

        var status = panel.Locator(".bit-mde-fns");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(status).ToHaveTextAsync(new Regex("^\\d of 3$"));
        var first = await status.TextContentAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(status).Not.ToHaveTextAsync(first!);
        await Page.Keyboard.PressAsync("Shift+Enter");
        await Expect(status).ToHaveTextAsync(first!);
        await Expect(find).ToBeFocusedAsync();

        // Match case narrows it down.
        var matchCase = panel.Locator(".bit-mde-fnc");
        await matchCase.ClickAsync();
        await Expect(matchCase).ToHaveAttributeAsync("aria-pressed", "true");
        await find.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(status).ToHaveTextAsync(new Regex("of 2$"));

        // The arrow buttons walk too.
        await panel.Locator("button[aria-label='Next match']").ClickAsync();
        await Expect(status).ToHaveTextAsync(new Regex("of 2$"));
        await panel.Locator("button[aria-label='Previous match']").ClickAsync();
        await Expect(status).ToHaveTextAsync(new Regex("of 2$"));

        // Enter in the replace box replaces one, Ctrl+Enter replaces all.
        var replace = panel.Locator("input[aria-label='Replace with']");
        await replace.FillAsync("MD");
        await replace.PressAsync("Enter");
        await Expect(TextArea("mde-find")).ToHaveValueAsync(new Regex("MD"));
        await replace.PressAsync("Control+Enter");
        await Expect(TextArea("mde-find")).ToHaveValueAsync("The MD editor finds every Markdown word in this MD document.");
        await Expect(Page.Locator("#find-value")).ToHaveTextAsync("The MD editor finds every Markdown word in this MD document.");

        // A term with no match says so.
        await find.FillAsync("zzz");
        await find.PressAsync("Enter");
        await Expect(status).ToHaveTextAsync("No results");

        // Escape closes the panel and gives the focus back to the textarea.
        await find.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(TextArea("mde-find")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Find_ToolbarButtonTogglesThePanel_AndAllReplacesEveryMatch()
    {
        await OpenAsync(Route);

        await Button("mde-find", "find").ClickAsync();
        var panel = Page.Locator("#mde-find .bit-mde-fnd");
        await Expect(panel).ToBeVisibleAsync();
        await panel.Locator("input[type='search']").FillAsync("markdown");
        await panel.Locator("input[aria-label='Replace with']").FillAsync("X");
        await panel.Locator("button[aria-label='Replace all']").ClickAsync();
        await Expect(TextArea("mde-find")).ToHaveValueAsync("The X editor finds every X word in this X document.");

        await panel.Locator(".bit-mde-hcl").ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);

        await Button("mde-find", "find").ClickAsync();
        await Expect(panel).ToBeVisibleAsync();
        await Button("mde-find", "find").ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);

        // A read-only editor searches but does not replace.
        await TextArea("mde-find-readonly").FocusAsync();
        await Page.Keyboard.PressAsync("Control+f");
        var readOnlyPanel = Page.Locator("#mde-find-readonly .bit-mde-fnd");
        await Expect(readOnlyPanel.Locator("button[aria-label='Replace all']")).ToBeDisabledAsync();
        await Expect(readOnlyPanel.Locator("button[aria-label='Replace all']")).ToHaveAttributeAsync("aria-disabled", "true");
        await readOnlyPanel.Locator("input[type='search']").FillAsync("find");
        await readOnlyPanel.Locator("input[type='search']").PressAsync("Enter");
        await Expect(readOnlyPanel.Locator(".bit-mde-fns")).ToHaveTextAsync("1 of 2");
    }

    [TestMethod]
    public async Task TableTools_EditTheTableAtTheCaret()
    {
        await OpenAsync(Route);

        var original = await TextArea("mde-table").InputValueAsync();
        var caret = original.IndexOf("a    |", StringComparison.Ordinal) + 1;

        async Task RunAsync(string cmd)
        {
            await Expect(TextArea("mde-table")).ToHaveValueAsync(original);
            await TextArea("mde-table").EvaluateAsync("(t, c) => { t.focus(); t.setSelectionRange(c, c); }", caret);
            await Button("mde-table", "tabletools").ClickAsync();
            await Page.Locator($"#mde-table .bit-mde-ddm [data-cmd='{cmd}']").ClickAsync();
            await Expect(TextArea("mde-table")).Not.ToHaveValueAsync(original);
        }

        await RunAsync("trowbelow");
        await Expect(TextArea("mde-table")).ToHaveValueAsync(new Regex("\\| a\\s+\\| 1\\s+\\|\\n\\|\\s+\\|\\s+\\|\\n\\| b"));

        await TextArea("mde-table").FillAsync(original);
        await RunAsync("trowabove");
        await Expect(TextArea("mde-table")).ToHaveValueAsync(new Regex("-\\s+\\|\\n\\|\\s+\\|\\s+\\|\\n\\| a"));

        await TextArea("mde-table").FillAsync(original);
        await RunAsync("trowdelete");
        await Expect(TextArea("mde-table")).Not.ToHaveValueAsync(new Regex("\\| a "));

        await TextArea("mde-table").FillAsync(original);
        await RunAsync("tcolafter");
        Assert.AreEqual(4, (await TextArea("mde-table").InputValueAsync()).Split('\n')[0].Count(c => c == '|'));

        await TextArea("mde-table").FillAsync(original);
        await RunAsync("tcolbefore");
        var header = (await TextArea("mde-table").InputValueAsync()).Split('\n')[0];
        Assert.AreEqual(4, header.Count(c => c == '|'));
        Assert.IsTrue(header.IndexOf("Name", StringComparison.Ordinal) > 4, "the new column is not before Name");

        await TextArea("mde-table").FillAsync(original);
        await RunAsync("tcoldelete");
        await Expect(TextArea("mde-table")).Not.ToHaveValueAsync(new Regex("Name"));

        foreach (var (cmd, pattern) in new[] { ("talignleft", "\\|\\s*:-+\\s*\\|"), ("taligncenter", "\\|\\s*:-+:\\s*\\|"), ("talignright", "\\|\\s*-+:\\s*\\|") })
        {
            await TextArea("mde-table").FillAsync(original);
            await RunAsync(cmd);
            var separator = (await TextArea("mde-table").InputValueAsync()).Split('\n')[1];
            StringAssert.Matches(separator, new Regex("^" + pattern), cmd);
        }
    }

    [TestMethod]
    public async Task TableTools_TabWalksTheCells_AndAddsARowPastTheLast()
    {
        await OpenAsync(Route);

        var value = await TextArea("mde-table").InputValueAsync();
        await TextArea("mde-table").EvaluateAsync("(t, c) => { t.focus(); t.setSelectionRange(c, c); }", value.IndexOf("Name", StringComparison.Ordinal));

        await Page.Keyboard.PressAsync("Tab");
        await Page.WaitForFunctionAsync("() => { const t = document.getElementById('mde-table-txt'); return t.value.slice(t.selectionStart, t.selectionEnd).trim() === 'Value'; }");

        // The walk selected the cell; the next Tab walks on from there, onto the first cell of the next row.
        await Page.Keyboard.PressAsync("Tab");
        await Page.WaitForFunctionAsync("() => { const t = document.getElementById('mde-table-txt'); return t.value.slice(t.selectionStart, t.selectionEnd).trim() === 'a'; }");

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.WaitForFunctionAsync("() => { const t = document.getElementById('mde-table-txt'); return t.value.slice(t.selectionStart, t.selectionEnd).trim() === 'Value'; }");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.WaitForFunctionAsync("() => { const t = document.getElementById('mde-table-txt'); return t.value.slice(t.selectionStart, t.selectionEnd).trim() === 'Name'; }");
        Assert.AreEqual(value, await TextArea("mde-table").InputValueAsync(), "walking the cells changed the text");

        // From the last cell, Tab adds a row.
        await TextArea("mde-table").EvaluateAsync("(t, c) => t.setSelectionRange(c, c)", value.LastIndexOf('2'));
        await Page.Keyboard.PressAsync("Tab");
        await Page.WaitForFunctionAsync("() => document.getElementById('mde-table-txt').value.trimEnd().split('\\n').length === 5");
    }

    [TestMethod]
    public async Task ImageUpload_TheToolbarButtonPicksAFile_AndTheReturnedUrlReplacesThePlaceholder()
    {
        await OpenAsync(Route);

        // The upload button exists only with an upload handler.
        await Expect(Button("mde-cmd", "upload")).ToHaveCountAsync(0);
        var upload = Button("mde-image", "upload");
        await Expect(upload).ToHaveAttributeAsync("aria-label", "Upload image");

        await SetTextAsync("mde-image", "");
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => upload.ClickAsync());
        Assert.IsTrue(chooser.IsMultiple);
        await chooser.SetFilesAsync(new FilePayload { Name = "e2e.png", MimeType = "image/png", Buffer = TinyPng });

        await Expect(TextArea("mde-image")).ToHaveValueAsync("![e2e.png](/samples/mdv-sample.png)");
        await Expect(Page.Locator("#last-upload")).ToHaveTextAsync($"e2e.png|image/png|{TinyPng.Length}");
        await Expect(Page.Locator("#image-value")).ToHaveTextAsync("![e2e.png](/samples/mdv-sample.png)");

        // A null URL cancels the insertion.
        await SetTextAsync("mde-image", "");
        chooser = await Page.RunAndWaitForFileChooserAsync(() => upload.ClickAsync());
        await chooser.SetFilesAsync(new FilePayload { Name = "cancel.png", MimeType = "image/png", Buffer = TinyPng });
        await Expect(Page.Locator("#uploads")).ToHaveTextAsync("2");
        await Expect(TextArea("mde-image")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task ImageUpload_ShowsAPlaceholderWhileUploading()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-image", "");
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Button("mde-image", "upload").ClickAsync());
        await chooser.SetFilesAsync(new FilePayload { Name = "slow.png", MimeType = "image/png", Buffer = TinyPng });

        await Expect(TextArea("mde-image")).ToHaveValueAsync("![…uploading-1…]()");
        await Page.ClickAsync("#btn-release-upload");
        await Expect(TextArea("mde-image")).ToHaveValueAsync("![slow.png](/samples/mdv-sample.png)");
    }

    [TestMethod]
    public async Task ImageUpload_RefusesAWrongTypeATooLargeFileAndOneThatDoesNotFit()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-image", "");
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Button("mde-image", "upload").ClickAsync());
        await chooser.SetFilesAsync(new FilePayload { Name = "anim.gif", MimeType = "image/gif", Buffer = TinyPng });
        await Expect(Page.Locator("#last-rejection")).ToHaveTextAsync($"anim.gif|image/gif|{TinyPng.Length}|Type");

        chooser = await Page.RunAndWaitForFileChooserAsync(() => Button("mde-image", "upload").ClickAsync());
        var big = new byte[5000];
        TinyPng.CopyTo(big, 0);
        await chooser.SetFilesAsync(new FilePayload { Name = "big.png", MimeType = "image/png", Buffer = big });
        await Expect(Page.Locator("#last-rejection")).ToHaveTextAsync("big.png|image/png|5000|Size");
        await Expect(Page.Locator("#rejections")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#uploads")).ToHaveTextAsync("0");
        await Expect(TextArea("mde-image")).ToHaveValueAsync("");

        // MaxLength leaves no room for the placeholder.
        await TextArea("mde-image-max").FocusAsync();
        chooser = await Page.RunAndWaitForFileChooserAsync(() => Button("mde-image-max", "upload").ClickAsync());
        await chooser.SetFilesAsync(new FilePayload { Name = "e2e.png", MimeType = "image/png", Buffer = TinyPng });
        await Expect(Page.Locator("#last-max-rejection")).ToHaveTextAsync("Length");
        await Expect(TextArea("mde-image-max")).ToHaveValueAsync("0123456789");
    }

    [TestMethod]
    public async Task ImageUpload_APastedAndADroppedImage_AreUploaded()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not let a page construct a ClipboardEvent carrying files.");

        await OpenAsync(Route);

        await SetTextAsync("mde-image", "");
        await TextArea("mde-image").EvaluateAsync(@"(t, bytes) => {
            const dt = new DataTransfer();
            dt.items.add(new File([new Uint8Array(bytes)], 'pasted.png', { type: 'image/png' }));
            t.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
        }", TinyPng.Select(b => (int)b).ToArray());
        await Expect(TextArea("mde-image")).ToHaveValueAsync("![pasted.png](/samples/mdv-sample.png)");

        // Dragging a file over the editor marks it as the drop target; dropping it uploads it.
        await SetTextAsync("mde-image", "");
        await Page.Locator("#mde-image").EvaluateAsync(@"(root, bytes) => {
            const dt = new DataTransfer();
            dt.items.add(new File([new Uint8Array(bytes)], 'dropped.png', { type: 'image/png' }));
            const t = root.querySelector('textarea');
            t.dispatchEvent(new DragEvent('dragover', { dataTransfer: dt, bubbles: true, cancelable: true }));
            window.__mdeDragMarked = root.hasAttribute('data-bit-mde-drag');
            t.dispatchEvent(new DragEvent('drop', { dataTransfer: dt, bubbles: true, cancelable: true }));
        }", TinyPng.Select(b => (int)b).ToArray());
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__mdeDragMarked"));
        await Expect(TextArea("mde-image")).ToHaveValueAsync("![dropped.png](/samples/mdv-sample.png)");
        await Expect(Page.Locator("#mde-image")).Not.ToHaveAttributeAsync("data-bit-mde-drag", new Regex(".*"));
    }

    [TestMethod]
    public async Task Paste_HtmlTablesSpreadsheetRowsAndUrlsBecomeMarkdown()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not let a page construct a ClipboardEvent with data.");

        await OpenAsync(Route);

        async Task PasteAsync(string text, string? html)
        {
            await TextArea("mde-paste").EvaluateAsync(@"(t, d) => {
                const dt = new DataTransfer();
                dt.setData('text/plain', d[0]);
                if (d[1]) dt.setData('text/html', d[1]);
                t.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
            }", new[] { text, html });
        }

        await SetTextAsync("mde-paste", "");
        await PasteAsync("A\tB\n1\t2", "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>");
        await Expect(TextArea("mde-paste")).ToHaveValueAsync(new Regex("^\\s*\\| A \\| B \\|\\n\\| --- \\| --- \\|\\n\\| 1 \\| 2 \\|\\s*$"));

        await SetTextAsync("mde-paste", "");
        await PasteAsync("X\tY\n3\t4", null);
        await Expect(TextArea("mde-paste")).ToHaveValueAsync(new Regex("^\\s*\\| X \\| Y \\|\\n\\| --- \\| --- \\|\\n\\| 3 \\| 4 \\|\\s*$"));

        await SetTextAsync("mde-paste", "see docs", 4, 8);
        await PasteAsync("https://bitplatform.dev", null);
        await Expect(TextArea("mde-paste")).ToHaveValueAsync("see [docs](https://bitplatform.dev)");
        await Expect(Page.Locator("#paste-value")).ToHaveTextAsync("see [docs](https://bitplatform.dev)");
    }

    [TestMethod]
    public async Task StatusBar_CountsWordsCharsReadingTimeAndTheCaret()
    {
        await OpenAsync(Route);

        var bar = Page.Locator("#mde-status .bit-mde-sbr");
        await Expect(bar).ToContainTextAsync("5 words");
        await Expect(bar).ToContainTextAsync("23 / 30 chars");
        await Expect(bar).ToContainTextAsync("3 min read");
        await Expect(bar.Locator(".bit-mde-mod")).ToHaveTextAsync("Edit");

        await SetTextAsync("mde-status", "one two three four five", 4, 7);
        await Expect(bar).ToContainTextAsync("Ln 1, Col 5");
        await Expect(bar).ToContainTextAsync("3 selected");

        await TextArea("mde-status").EvaluateAsync("t => { t.setSelectionRange(0, 0); }");
        await Page.Keyboard.PressAsync("Control+End");
        await Expect(bar).ToContainTextAsync("Ln 1, Col 24");
        await Expect(bar.Locator(".bit-mde-pos")).ToHaveCountAsync(1);

        // MaxLength caps the text and marks the counter at the limit.
        var counter = Page.Locator("#mde-status-cnt");
        await Expect(TextArea("mde-status")).ToHaveAttributeAsync("maxlength", "30");
        StringAssert.Contains(await TextArea("mde-status").GetAttributeAsync("aria-describedby"), "mde-status-cnt");
        await Page.Keyboard.TypeAsync(" six seven eight nine");
        await Expect(counter).ToHaveTextAsync("30 / 30 chars");
        await Expect(counter).ToHaveClassAsync(HasClass("bit-mde-lim"));
        await Expect(TextArea("mde-status")).ToHaveValueAsync("one two three four five six se");

        // CJK characters count as words of their own, an emoji as one character.
        var count = Page.Locator("#mde-count .bit-mde-sbr");
        await Expect(count).ToContainTextAsync("6 words");
        await Expect(count).ToContainTextAsync("15 chars");
    }

    [TestMethod]
    public async Task Localization_TextsReplaceEveryStringTheEditorRenders()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-french");
        await Expect(root.Locator(".bit-mde-tlb")).ToHaveAttributeAsync("aria-label", "Mise en forme Markdown");
        await Expect(TextArea("mde-french")).ToHaveAttributeAsync("aria-label", "Éditeur Markdown");
        await Expect(root.Locator(".bit-mde-ppn")).ToHaveAttributeAsync("aria-label", "Aperçu Markdown");
        await Expect(Button("mde-french", "bold")).ToHaveAttributeAsync("aria-label", "Gras");
        await Expect(Button("mde-french", "bold")).ToHaveAttributeAsync("title", "Gras (Ctrl+B)");
        await Expect(Button("mde-french", "italic")).ToHaveAttributeAsync("aria-label", "Italique");
        await Expect(root.Locator(".bit-mde-sbr")).ToContainTextAsync("3 mots");
        await Expect(root.Locator(".bit-mde-sbr")).ToContainTextAsync("13 caractères");
        await Expect(root.Locator(".bit-mde-sbr")).ToContainTextAsync("1 min de lecture");
        await Expect(root.Locator(".bit-mde-mod")).ToHaveTextAsync("Partagé");

        await SetTextAsync("mde-french", "");
        await Expect(root.Locator(".bit-mde-emp")).ToHaveTextAsync("Rien à afficher.");
    }

    [TestMethod]
    public async Task Form_LabelDescriptionRequiredAndName()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-form");
        var label = root.Locator("label.bit-mde-lbl");
        await Expect(label).ToHaveTextAsync("Release notes");
        await Expect(label).ToHaveAttributeAsync("for", "mde-form-txt");
        await Expect(label).ToHaveClassAsync(HasClass("bit-mde-req"));
        await Expect(Page.GetByLabel("Release notes", new() { Exact = true })).ToHaveAttributeAsync("id", "mde-form-txt");

        var textArea = TextArea("mde-form");
        await Expect(textArea).ToHaveAttributeAsync("required", "");
        await Expect(textArea).ToHaveAttributeAsync("aria-required", "true");
        await Expect(textArea).ToHaveAttributeAsync("name", "releaseNotes");
        Assert.IsNull(await textArea.GetAttributeAsync("aria-label"), "a labelled textarea is named by its label");

        var description = root.Locator(".bit-mde-des");
        await Expect(description).ToHaveTextAsync("Markdown is supported.");
        await Expect(description).ToHaveAttributeAsync("id", "mde-form-des");
        StringAssert.Contains(await textArea.GetAttributeAsync("aria-describedby"), "mde-form-des");

        // Clicking the label focuses the textarea.
        await label.ClickAsync();
        await Expect(textArea).ToBeFocusedAsync();
        await Expect(Page.Locator("#form-focus")).ToHaveTextAsync("1");

        // Templates and AriaLabel.
        await Expect(Page.Locator("#mde-label-template label .lbl-tpl")).ToHaveTextAsync("Templated label");
        await Expect(Page.Locator("#mde-label-template .bit-mde-des .des-tpl")).ToHaveTextAsync("Templated description");
        await Expect(TextArea("mde-label-template")).ToHaveAttributeAsync("aria-label", "Custom aria");
        await Expect(TextArea("mde-autofocus-host")).ToHaveAttributeAsync("aria-label", "Markdown editor");
    }

    [TestMethod]
    public async Task Form_ValidationRunsAsTyped_AndCtrlEnterSubmits()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-form");
        var textArea = TextArea("mde-form");

        await textArea.FocusAsync();
        await Page.Keyboard.TypeAsync("too short");
        await Expect(Page.Locator("#form .validation-message")).ToHaveTextAsync("Write at least 20 characters.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(textArea).ToHaveAttributeAsync("aria-invalid", "true");
        var invalidBorder = await root.EvaluateAsync<string>("r => getComputedStyle(r).borderTopColor");

        await Page.Keyboard.TypeAsync(" but now long enough");
        await Expect(Page.Locator("#form .validation-message")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        Assert.AreNotEqual(invalidBorder, await root.EvaluateAsync<string>("r => getComputedStyle(r).borderTopColor"));

        await Page.Keyboard.PressAsync("Control+Enter");
        await Expect(Page.Locator("#form-submits")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#form-submitted")).ToHaveTextAsync("too short but now long enough");
        await Expect(textArea).ToHaveValueAsync("too short but now long enough");

        await Page.ClickAsync("#btn-submit");
        await Expect(Page.Locator("#form-valid-submits")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#form-blur")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Form_SubmittingAnEmptyRequiredEditor_ShowsTheError()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-submit");
        await Expect(Page.Locator("#form .validation-message")).ToHaveCountAsync(1);
        await Expect(Root(Page, "mde-form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#form-valid-submits")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Cascading_BitParams_SetDefaultsAndAnEditorsOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(TextArea("mde-cascaded")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Page.Locator("#mde-cascaded .bit-mde-bdy")).ToHaveCSSAsync("height", "144px");
        await Expect(Page.Locator("#mde-cascaded .bit-mde-sbr")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#mde-cascaded .bit-mde-tlb")).ToHaveClassAsync(HasClass("cascaded-toolbar"));

        await Expect(TextArea("mde-cascaded-own")).ToHaveAttributeAsync("placeholder", "My own placeholder");
        await Expect(Page.Locator("#mde-cascaded-own .bit-mde-sbr")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#mde-cascaded-own .bit-mde-bdy")).ToHaveCSSAsync("height", "144px");
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Route);

        var styled = Root(Page, "mde-style");
        await Expect(styled).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(styled).ToHaveCSSAsync("border-top-color", "rgb(165, 42, 42)");
        await Expect(styled).ToHaveCSSAsync("margin-top", "7px");

        var root = Root(Page, "mde-styles");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(1, 1, 1)");
        var parts = new (string Selector, string Class, string Color, string Property)[]
        {
            (".bit-mde-lbl", "c-label", "rgb(2, 2, 2)", "color"),
            (".bit-mde-tlb", "c-toolbar", "rgb(3, 3, 3)", "background-color"),
            (".bit-mde-tlb [data-cmd='bold']", "c-button", "rgb(4, 4, 4)", "color"),
            (".bit-mde-txa", "c-textarea", "rgb(5, 5, 5)", "color"),
            (".bit-mde-ppn", "c-preview", "rgb(6, 6, 6)", "color"),
            (".bit-mde-des", "c-description", "rgb(7, 7, 7)", "color"),
            (".bit-mde-sbr", "c-status", "rgb(8, 8, 8)", "color"),
        };
        foreach (var (selector, cls, color, property) in parts)
        {
            var part = root.Locator(selector).First;
            await Expect(part).ToHaveClassAsync(HasClass(cls));
            await Expect(part).ToHaveCSSAsync(property, color);
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
            (" .bit-mde-tlb", "border-bottom-color", "rgb(4, 5, 6)"),
            (" .bit-mde-tlb", "background-color", "rgb(40, 50, 60)"),
            (" .bit-mde-bdy", "height", "150px"),
            (" .bit-mde-txa", "font-size", "19px"),
            (" .bit-mde-txa", "line-height", "38px"),
            (" .bit-mde-txa", "padding-top", "17px"),
            (" .bit-mde-lbl", "color", "rgb(7, 8, 9)"),
            (" .bit-mde-lbl", "font-size", "21px"),
            (" .bit-mde-lbl", "font-weight", "800"),
            (" .bit-mde-lbl", "letter-spacing", "2px"),
            (" .bit-mde-ppn", "padding-top", "23px"),
            (" .bit-mde-ppn", "background-color", "rgb(10, 20, 30)"),
            (" .bit-mde-tlb [data-cmd='bold']", "width", "41px"),
            (" .bit-mde-tlb [data-cmd='bold']", "color", "rgb(70, 80, 90)"),
            (" .bit-mde-tlb [data-cmd='bold']", "border-top-left-radius", "9px"),
            (" .bit-mde-sbr", "color", "rgb(100, 110, 120)"),
            (" .bit-mde-sbr", "background-color", "rgb(130, 140, 150)"),
        };

        foreach (var (selector, property, expected) in checks)
        {
            Assert.AreEqual(expected, await ComputedStyleAsync($"#mde-vars{selector}", property), $"{selector} {property}");
            Assert.AreNotEqual(expected, await ComputedStyleAsync($"#mde-vars-default{selector}", property), $"default {selector} {property}");
        }

        StringAssert.Contains(await ComputedStyleAsync("#mde-vars .bit-mde-txa", "font-family"), "Georgia");
    }

    [TestMethod]
    public async Task Rtl_FlipsTheToolbarAndItsArrowKeys()
    {
        await OpenAsync(Route);

        var root = Root(Page, "mde-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "mde-rtl", "mde-rtl-inherited" })
        {
            var toolbar = (await Page.Locator($"#{id} .bit-mde-tlb").BoundingBoxAsync())!;
            var first = (await Button(id, "undo").BoundingBoxAsync())!;
            Assert.IsTrue(first.X > toolbar.X + toolbar.Width / 2, $"{id}: the first button is not on the right");

            var edit = (await Page.Locator($"#{id} .bit-mde-epn").BoundingBoxAsync())!;
            var preview = (await Page.Locator($"#{id} .bit-mde-ppn").BoundingBoxAsync())!;
            Assert.IsTrue(edit.X > preview.X, $"{id}: the edit pane is not on the right");
        }

        // The arrows follow what is on screen: ArrowLeft goes to the next button.
        await Button("mde-rtl", "bold").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Button("mde-rtl", "italic")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button("mde-rtl", "bold")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Help_ShortcutsDialogOpensTrapsTheFocusAndClosesOnEscape()
    {
        await OpenAsync(Route);

        await SetTextAsync("mde-custom", "x");
        await Page.Keyboard.PressAsync("Control+Slash");
        var dialog = Page.Locator("#mde-custom [role='dialog']");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog).ToBeFocusedAsync();
        await Expect(Button("mde-custom", "help")).ToHaveCountAsync(0);

        var titleId = await dialog.GetAttributeAsync("aria-labelledby");
        await Expect(Page.Locator($"#{titleId}")).ToHaveTextAsync("Keyboard shortcuts");
        await Expect(dialog.Locator("dt", new() { HasTextString = "Bold" }).First).ToBeVisibleAsync();
        // The custom items' shortcuts are listed too.
        await Expect(dialog.Locator("dl > div", new() { HasTextString = "Insert stamp" })).ToContainTextAsync("Ctrl+Shift+K");
        await Expect(dialog.Locator("dl > div", new() { HasTextString = "Count words" })).ToContainTextAsync("F2");

        // Tab stays inside the dialog, and never leaves it even for a moment: the trap wraps the focus in the script as
        // the key goes down, so it is read straight after each press rather than waited for. The close button is the
        // dialog's one control, so every Tab and Shift+Tab lands on it.
        var close = dialog.Locator(".bit-mde-hcl");
        foreach (var key in new[] { "Tab", "Tab", "Tab", "Shift+Tab", "Shift+Tab" })
        {
            await Page.Keyboard.PressAsync(key);
            Assert.IsTrue(await close.EvaluateAsync<bool>("b => document.activeElement === b"), $"{key} took the focus out of the help dialog");
        }

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(TextArea("mde-custom")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Help_TheToolbarButtonTogglesIt_AndAClickOnTheBackdropClosesIt()
    {
        await OpenAsync(Route);

        var help = Button("mde-cmd", "help");
        await Expect(help).ToHaveAttributeAsync("aria-keyshortcuts", "Control+/");
        await help.ClickAsync();
        var dialog = Page.Locator("#mde-cmd [role='dialog']");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(help).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(dialog.Locator("dl > div")).ToHaveCountAsync(24);

        await dialog.Locator(".bit-mde-hcl").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await help.ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await Page.Locator("#mde-cmd .bit-mde-hlp").ClickAsync(new() { Position = new() { X = 3, Y = 3 } });
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Escape_InADialog_ClosesTheFindPanelFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dialog");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        var textArea = TextArea("mde-dialog");
        await Expect(textArea).ToBeVisibleAsync();

        await textArea.FocusAsync();
        await Page.Keyboard.PressAsync("Control+f");
        var panel = Page.Locator("#mde-dialog .bit-mde-fnd");
        await Expect(panel).ToBeVisibleAsync();

        // The find box claims the Escape that closes the panel.
        await Expect(panel.Locator("input[type='search']")).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(textArea).ToBeFocusedAsync();

        // With nothing left to close, the textarea claims nothing, and the dialog closes.
        Assert.IsNull(await textArea.GetAttributeAsync("data-bit-esc"));
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoSave_KeepsADraftThatSurvivesAReload_UntilClearDraft()
    {
        await OpenAsync(Route);

        await TextArea("mde-autosave").FocusAsync();
        await Page.Keyboard.TypeAsync("my draft");
        await TextArea("mde-autosave").BlurAsync();
        await Page.WaitForFunctionAsync("() => localStorage.getItem('e2e-agent20-mde-draft') === 'my draft'");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(TextArea("mde-autosave")).ToHaveValueAsync("my draft");
        await Expect(Page.Locator("#draft-restored")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#draft-value")).ToHaveTextAsync("my draft");

        await Page.ClickAsync("#btn-clear-draft");
        await Page.WaitForFunctionAsync("() => localStorage.getItem('e2e-agent20-mde-draft') === null");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(TextArea("mde-autosave")).ToHaveValueAsync("");
        await Expect(Page.Locator("#draft-restored")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Lifecycle_Unmount_DisposesWithoutAnError_AndReleasesTheScrollLock()
    {
        await OpenAsync(Route);

        await Button("mde-unmount", "fullscreen").ClickAsync();
        await Expect(Page.Locator("#mde-unmount")).ToHaveClassAsync(HasClass("bit-mde-fsc"));
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.body).overflow === 'hidden' || getComputedStyle(document.documentElement).overflow === 'hidden'");

        // The full-screen editor covers the page's button; the page removes it by script.
        await Page.Locator("#btn-toggle-mount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mde-unmount")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.body).overflow !== 'hidden' && getComputedStyle(document.documentElement).overflow !== 'hidden'");

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(TextArea("mde-unmount")).ToHaveValueAsync("unmount me");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task AutoFocus_MovesTheFocusIntoTheEditorOnLoad()
    {
        await OpenAsync("/e2e/markdown-editor/autofocus");

        await Expect(Page.Locator("#mde-autofocus-txt")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes_ReachTheRoot()
    {
        await OpenAsync("/e2e/markdown-editor/autofocus");

        var root = Page.Locator("#mde-visibility");
        await Expect(root).ToHaveAttributeAsync("data-test", "mde-data");
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

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
}
