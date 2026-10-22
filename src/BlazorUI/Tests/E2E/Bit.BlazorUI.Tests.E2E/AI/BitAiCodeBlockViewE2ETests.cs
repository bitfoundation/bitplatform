namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiCodeBlockView at /e2e/aicodeblockview in a real browser: the header naming the language or the file, the
/// tokens of each language as spans of their kind painted with the role colors of the bit theme in the light and the
/// dark scheme, the copy button (pointer and keyboard, what reaches the clipboard, the confirmation, the AI glyph),
/// the line numbers kept out of the copied and selected text, a block in a right-to-left page reading left to right
/// and isolated, highlighting held back while a block streams, a long line scrolling inside the block, every
/// --bit-AiCodeBlockView-* variable, Classes/Styles and print.
/// </summary>
[TestClass]
public class BitAiCodeBlockViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aicodeblockview";

    private const string CSharpCode = "// Adds two numbers\npublic static int Add(int a, int b)\n{\n    var name = \"total\";\n    return a + b + 42;\n}";

    // Each token class, the theme token its color comes from, and the block of the page that has one.
    private static readonly (string Class, string Token, string Block)[] TokenColors =
    [
        ("bit-aicbv-kwd", "--bit-clr-pri-fg", "cbv-cs"),
        ("bit-aicbv-typ", "--bit-clr-suc-fg", "cbv-cs"),
        ("bit-aicbv-str", "--bit-clr-swr-fg", "cbv-cs"),
        ("bit-aicbv-num", "--bit-clr-sec-fg", "cbv-cs"),
        ("bit-aicbv-cmt", "--bit-clr-fg-ter", "cbv-cs"),
        ("bit-aicbv-fnc", "--bit-clr-wrn-fg", "cbv-cs"),
        ("bit-aicbv-pnc", "--bit-clr-fg-sec", "cbv-cs"),
        ("bit-aicbv-prp", "--bit-clr-pri-fg", "cbv-yaml"),
        ("bit-aicbv-cst", "--bit-clr-sec-fg", "cbv-file"),
        ("bit-aicbv-ins", "--bit-clr-suc-fg", "cbv-diff"),
        ("bit-aicbv-del", "--bit-clr-err-fg", "cbv-diff"),
        ("bit-aicbv-met", "--bit-clr-fg-ter", "cbv-diff"),
        ("bit-aicbv-tag", "--bit-clr-err-fg", "cbv-html"),
        ("bit-aicbv-atr", "--bit-clr-sec-fg", "cbv-html"),
        ("bit-aicbv-lab", "--bit-clr-suc-fg", "cbv-rust"),
    ];

    // Stands in for the clipboard: every text written to it is kept, the latest last.
    private const string ClipboardRecorder =
        """
        (() => {
            window.__e2eClipboard = [];
            const record = text => { window.__e2eClipboard.push(String(text)); return Promise.resolve(); };
            if (navigator.clipboard) {
                try { Object.defineProperty(navigator.clipboard, 'writeText', { value: record, configurable: true }); } catch { }
            }
        })();
        """;

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_ACodeBlock_ShowsItsLanguageItsCodeAndACopyButton()
    {
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-cs");
        await Expect(block).ToHaveClassAsync(HasClass("bit-aicbv"));
        await Expect(block.Locator(".bit-aicbv-hdr .bit-aicbv-lng")).ToHaveTextAsync("csharp");
        await Expect(block.Locator(".bit-aicbv-lng")).ToHaveCSSAsync("font-family", await Page.ResolveFontFamilyAsync("var(--bit-tpg-font-family-mono)"));
        await Expect(block.Locator("pre.bit-aicbv-pre > code.bit-aicbv-cod")).ToHaveClassAsync(HasClass("language-csharp"));
        Assert.AreEqual(CSharpCode, await block.Locator("pre.bit-aicbv-pre").TextContentAsync());
        await Expect(block.Locator(".bit-aicbv-hdr .bit-aicpb button")).ToHaveAccessibleNameAsync("Copy");

        // A file name is shown in place of the language, and a block with neither is "text".
        await Expect(Page.Locator("#cbv-file .bit-aicbv-lng")).ToHaveTextAsync("appsettings.json");
        await Expect(Page.Locator("#cbv-plain .bit-aicbv-lng")).ToHaveTextAsync("text");
        await Expect(Page.Locator("#cbv-plain code")).ToHaveClassAsync(HasClass("language-text"));

        // Code of no language nobody knows is left plain, with no spans at all.
        await Expect(Page.Locator("#cbv-unknown .bit-aicbv-lng")).ToHaveTextAsync("klingon");
        await Expect(Page.Locator("#cbv-unknown code span")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cbv-plain code span")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#cbv-noheader .bit-aicbv-hdr")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cbv-noheader .bit-aicbv-pre")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Highlighting_TheTokensOfEachLanguage_AreSpansOfTheirKind()
    {
        await OpenAsync(Route);

        var cs = Page.Locator("#cbv-cs");
        await Expect(cs.Locator(".bit-aicbv-cmt")).ToHaveTextAsync("// Adds two numbers");
        await Expect(cs.Locator(".bit-aicbv-kwd")).ToHaveTextAsync(["public", "static", "return"]);
        await Expect(cs.Locator(".bit-aicbv-typ")).ToContainTextAsync(["int", "var"]);
        await Expect(cs.Locator(".bit-aicbv-fnc")).ToHaveTextAsync("Add");
        await Expect(cs.Locator(".bit-aicbv-str")).ToHaveTextAsync("\"total\"");
        await Expect(cs.Locator(".bit-aicbv-num")).ToHaveTextAsync("42");

        var json = Page.Locator("#cbv-file");
        await Expect(json.Locator(".bit-aicbv-cst")).ToHaveTextAsync("true");
        await Expect(json.Locator(".bit-aicbv-num")).ToHaveTextAsync("3");

        await Expect(Page.Locator("#cbv-yaml .bit-aicbv-prp")).ToHaveTextAsync(["name", "count"]);

        var diff = Page.Locator("#cbv-diff");
        await Expect(diff.Locator(".bit-aicbv-ins")).ToContainTextAsync(["var x = 2;"]);
        await Expect(diff.Locator(".bit-aicbv-del")).ToContainTextAsync(["var x = 1;"]);

        var html = Page.Locator("#cbv-html");
        await Expect(html.Locator(".bit-aicbv-tag").First).ToContainTextAsync("a");
        await Expect(html.Locator(".bit-aicbv-atr")).ToContainTextAsync(["href", "class"]);

        await Expect(Page.Locator("#cbv-rust .bit-aicbv-lab").First).ToHaveTextAsync("'a");

        // The spans only color the text: the code reads exactly as it was written.
        Assert.AreEqual("<a href=\"/home\" class=\"link\">Home</a>", await html.Locator("pre").TextContentAsync());
    }

    [TestMethod]
    public async Task Highlighting_TheKeysOfAJsonObject_ArePaintedAsPropertiesAndItsValuesAsStrings()
    {
        await OpenAsync(Route);

        // The stylesheet documents the property color for "keys and properties (JSON, YAML, CSS)", and the JSON grammar
        // asks for colon properties: a quoted key is a property, a quoted value a string.
        var json = Page.Locator("#cbv-file");
        await Expect(json.Locator(".bit-aicbv-prp")).ToHaveTextAsync(["\"name\"", "\"count\"", "\"on\""]);
        await Expect(json.Locator(".bit-aicbv-str")).ToHaveTextAsync(["\"bit\""]);
    }

    [TestMethod]
    public async Task Colors_EveryTokenKind_IsPaintedWithItsThemeColorInLightAndDark()
    {
        await OpenAsync(Route);

        await ExpectTokenColorsAsync();
        var lightKeyword = await Page.Locator("#cbv-cs .bit-aicbv-kwd").First.EvaluateAsync<string>("e => getComputedStyle(e).color");

        await Page.EvaluateAsync("() => document.documentElement.setAttribute('bit-theme', 'dark')");
        await Expect(Page.Locator("#cbv-cs .bit-aicbv-kwd").First).Not.ToHaveCSSAsync("color", lightKeyword);
        await ExpectTokenColorsAsync();

        // Comments and labels are set in italics as well.
        await Expect(Page.Locator("#cbv-cs .bit-aicbv-cmt")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#cbv-rust .bit-aicbv-lab").First).ToHaveCSSAsync("font-style", "italic");

        async Task ExpectTokenColorsAsync()
        {
            foreach (var (cls, token, block) in TokenColors)
            {
                await Expect(Page.Locator($"#{block} .{cls}").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync($"var({token})"));
            }

            await Expect(Page.Locator("#cbv-cs .bit-aicbv-hdr")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
            await Expect(Page.Locator("#cbv-cs")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        }
    }

    // ---------- copy ----------

    [TestMethod]
    public async Task Copy_TheCopyButton_PutsExactlyTheCodeOnTheClipboardAndConfirmsIt()
    {
        await PrepareClipboardAsync();
        await OpenAsync(Route);

        var copy = Page.Locator("#cbv-copy .bit-aicpb");
        var button = copy.Locator("button");

        await copy.Locator(".bit-icon-ai--Copy").AssertAiGlyphAsync("Copy");
        await Expect(button).ToHaveAccessibleNameAsync("Copy");

        await button.ClickAsync();

        await ExpectClipboardAsync(CSharpCode);

        // The glyph turns into a check mark, the name of the button says so, and the confirmation is announced.
        await Expect(button).ToHaveAccessibleNameAsync("Copied");
        await Expect(copy.Locator("[role='status']")).ToHaveTextAsync("Copied");
        await Expect(copy.Locator(".bit-icon--Accept")).ToHaveCountAsync(1);

        // Then it is ready for the next copy.
        await Expect(button).ToHaveAccessibleNameAsync("Copy");
        await Expect(copy.Locator("[role='status']")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Copy_TheKeyboard_ReachesTheCopyButtonAndCopiesWithEnter()
    {
        await PrepareClipboardAsync();
        await OpenAsync(Route);

        await Page.Locator("#cbv-before-copy").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var button = Page.Locator("#cbv-copy .bit-aicpb button");
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");

        await ExpectClipboardAsync(CSharpCode);
        await Expect(button).ToHaveAccessibleNameAsync("Copied");
    }

    [TestMethod]
    public async Task Copy_ABlockWithLineNumbers_CopiesAndSelectsTheCodeWithoutTheNumbers()
    {
        await PrepareClipboardAsync();
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-copy-numbered");
        await Expect(block.Locator(".bit-aicbv-lin")).ToHaveCountAsync(6);

        await block.Locator(".bit-aicpb button").ClickAsync();
        await ExpectClipboardAsync(CSharpCode);

        // The numbers are drawn, not written: selecting the code selects the code alone.
        var selected = await block.Locator("code").EvaluateAsync<string>(
            """
            e => {
                const selection = getSelection();
                selection.removeAllRanges();
                selection.selectAllChildren(e);
                const text = selection.toString();
                selection.removeAllRanges();
                return text;
            }
            """);
        Assert.AreEqual(CSharpCode, selected.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    [TestMethod]
    public async Task Copy_ShowCodeCopyOff_DrawsTheHeaderWithoutAButton()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cbv-nocopy .bit-aicbv-lng")).ToHaveTextAsync("csharp");
        await Expect(Page.Locator("#cbv-nocopy .bit-aicpb")).ToHaveCountAsync(0);
    }

    // ---------- line numbers ----------

    [TestMethod]
    public async Task LineNumbers_EveryLine_IsNumberedInAGutterAtItsLeftInEitherDirection()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "cbv-lines", "cbv-lines-rtl" })
        {
            var lines = Page.Locator($"#{id} .bit-aicbv-lin");
            await Expect(lines).ToHaveCountAsync(6);

            var before = await lines.First.EvaluateAsync<string[]>(
                "e => { const s = getComputedStyle(e, '::before'); return [s.content, s.counterIncrement, s.userSelect || s.webkitUserSelect, s.width, s.display, s.color]; }");
            StringAssert.Contains(before[0], "counter(bit-aicbv-line", $"{id}: the number is the line's counter.");
            StringAssert.Contains(before[1], "bit-aicbv-line", $"{id}: each line counts one more.");
            Assert.AreEqual("none", before[2], $"{id}: a number cannot be selected.");
            Assert.AreEqual("inline-block", before[4]);
            Assert.AreEqual(await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"), before[5], $"{id}: the numbers are the tertiary text color.");
            StringAssert.Contains(await Page.Locator($"#{id} .bit-aicbv-pre").EvaluateAsync<string>("e => getComputedStyle(e).counterReset"), "bit-aicbv-line");

            // The gutter is at the left of the code - its start in the left-to-right lines - even in a right-to-left page.
            var line = await lines.First.RectAsync();
            var firstCharacter = await lines.First.CharacterRectAsync(0);
            var gutter = AiE2EHelpers.ParsePx(before[3]);
            Assert.IsTrue(firstCharacter.Left - line.Left >= gutter, $"{id}: the code starts after the gutter ({firstCharacter.Left - line.Left} >= {gutter}).");
        }
    }

    // ---------- rtl ----------

    [TestMethod]
    public async Task Rtl_ACodeBlockInARightToLeftPage_ReadsLeftToRightAndIsIsolated()
    {
        await OpenAsync(Route);

        foreach (var block in new[] { Page.Locator("#cbv-rtl"), Page.Locator("#cbv-rtl-explicit"), Page.Locator("#cbv-rtl-markdown .bit-aicbv") })
        {
            var pre = block.Locator(".bit-aicbv-pre");
            await Expect(pre).ToHaveCSSAsync("direction", "ltr");
            await Expect(pre).ToHaveCSSAsync("unicode-bidi", "isolate");

            // "return a + b;" starts at the left edge of the block and ends with its semicolon at the right of the b.
            var code = pre.Locator("code");
            var first = await code.CharacterRectAsync(0);
            var b = await code.CharacterRectAsync("return a + ".Length);
            var semicolon = await code.CharacterRectAsync("return a + b".Length);
            var box = await pre.RectAsync();
            var padding = AiE2EHelpers.ParsePx(await pre.EvaluateAsync<string>("e => getComputedStyle(e).paddingLeft"));
            Assert.AreEqual(box.Left + padding, first.Left, 2, "The code starts at the left edge of the block.");
            Assert.IsTrue(b.Left < semicolon.Left, "The semicolon ends the line at its right.");

            // The header keeps the direction of the page: the language at the right, the copy button at the left.
            await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveCSSAsync("direction", "rtl");
            var label = await block.Locator(".bit-aicbv-lng").RectAsync();
            var copy = await block.Locator(".bit-aicpb").RectAsync();
            Assert.IsTrue(label.Left > copy.Left, "The language is at the start of a right-to-left header.");
        }

        var ltrLabel = await Page.Locator("#cbv-ltr .bit-aicbv-lng").RectAsync();
        var ltrCopy = await Page.Locator("#cbv-ltr .bit-aicpb").RectAsync();
        Assert.IsTrue(ltrLabel.Left < ltrCopy.Left);
    }

    // ---------- streaming ----------

    [TestMethod]
    public async Task Streaming_ABlockStillStreaming_IsHighlightedOnlyOnceItIsComplete()
    {
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-stream");
        await Expect(block.Locator("pre")).ToHaveTextAsync("public static int Add(");
        await Expect(block.Locator("code span")).ToHaveCountAsync(0);

        await Page.Locator("#cbv-stream-append").ClickAsync();
        await Expect(block.Locator("pre")).ToHaveTextAsync("public static int Add(int a, int b) => a + b;");
        await Expect(block.Locator("code span")).ToHaveCountAsync(0);

        await Page.Locator("#cbv-stream-finish").ClickAsync();
        await Expect(Page.Locator("#cbv-stream-state")).ToHaveTextAsync("done");
        await Expect(block.Locator(".bit-aicbv-kwd").First).ToHaveTextAsync("public");
        await Expect(block.Locator("pre")).ToHaveTextAsync("public static int Add(int a, int b) => a + b;");
    }

    // ---------- overflow ----------

    [TestMethod]
    public async Task Overflow_ALongLine_ScrollsInsideTheBlockRatherThanWideningIt()
    {
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-long");
        var body = block.Locator(".bit-aicbv-bdy");

        Assert.IsTrue((await block.BoundingBoxAsync())!.Width <= 300.5, "The block is no wider than its container.");
        await Expect(body).ToHaveCSSAsync("overflow-x", "auto");
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveCSSAsync("white-space", "pre");

        var sizes = await body.EvaluateAsync<double[]>("e => [e.scrollWidth, e.clientWidth]");
        Assert.IsTrue(sizes[0] > sizes[1], "The long line overflows the body, which scrolls it.");

        await body.EvaluateAsync("e => e.scrollLeft = e.scrollWidth");
        Assert.IsTrue(await body.EvaluateAsync<double>("e => e.scrollLeft") > 0);
        Assert.IsTrue((await block.BoundingBoxAsync())!.Width <= 300.5);
    }

    // ---------- public variables, Classes and Styles ----------

    [TestMethod]
    public async Task Variables_ThePublicVariables_RestyleTheBlockAndEveryTokenKind()
    {
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-vars");
        await Expect(block).ToHaveCSSAsync("background-color", "rgb(250, 250, 240)");
        await Expect(block).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(block).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(block).ToHaveCSSAsync("border-top-left-radius", "12px");
        await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveCSSAsync("border-bottom-color", "rgb(4, 5, 6)");

        var pre = block.Locator(".bit-aicbv-pre");
        await Expect(pre).ToHaveCSSAsync("padding-top", "24px");
        await Expect(pre).ToHaveCSSAsync("font-family", "serif");
        await Expect(pre).ToHaveCSSAsync("font-size", "20px");
        await Expect(block.Locator(".bit-aicbv-lng")).ToHaveCSSAsync("font-family", "serif");

        await Expect(block.Locator(".bit-aicbv-kwd").First).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(block.Locator(".bit-aicbv-str")).ToHaveCSSAsync("color", "rgb(0, 200, 0)");
        await Expect(block.Locator(".bit-aicbv-num")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(block.Locator(".bit-aicbv-cmt")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(Page.Locator("#cbv-vars-yaml .bit-aicbv-prp").First).ToHaveCSSAsync("color", "rgb(19, 20, 21)");

        // A label has no color set of its own, so it follows the type color.
        await Expect(Page.Locator("#cbv-vars-rust .bit-aicbv-lab").First).ToHaveCSSAsync("color", "rgb(16, 17, 18)");

        Assert.AreEqual("rgb(22, 23, 24)", await Page.Locator("#cbv-vars-lines .bit-aicbv-lin").First.EvaluateAsync<string>("e => getComputedStyle(e, '::before').color"));

        // A variable on the Style of one block restyles that block.
        await Expect(Page.Locator("#cbv-vars-style .bit-aicbv-kwd").First).ToHaveCSSAsync("color", "rgb(0, 120, 0)");
        await Expect(Page.Locator("#cbv-cs .bit-aicbv-kwd").First).Not.ToHaveCSSAsync("color", "rgb(0, 120, 0)");
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheirParts()
    {
        await OpenAsync(Route);

        var block = Page.Locator("#cbv-parts");
        await Expect(block).ToHaveClassAsync(HasClass("e2e-cbv-class"));
        await Expect(block).ToHaveClassAsync(HasClass("e2e-cbv-root"));
        await Expect(block).ToHaveCSSAsync("outline-color", "rgb(30, 31, 32)");
        await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveClassAsync(HasClass("e2e-cbv-header"));
        await Expect(block.Locator(".bit-aicbv-hdr")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(block.Locator(".bit-aicbv-lng")).ToHaveClassAsync(HasClass("e2e-cbv-label"));
        await Expect(block.Locator(".bit-aicbv-bdy")).ToHaveClassAsync(HasClass("e2e-cbv-body"));
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveClassAsync(HasClass("e2e-cbv-pre"));
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveCSSAsync("padding-top", "30px");
        await Expect(block.Locator(".bit-aicbv-cod")).ToHaveClassAsync(HasClass("e2e-cbv-code"));
        await Expect(block.Locator(".bit-aicbv-lin").First).ToHaveClassAsync(HasClass("e2e-cbv-line"));
        await Expect(block.Locator(".bit-aicbv-lin").First).ToHaveCSSAsync("font-style", "italic");
    }

    [TestMethod]
    public async Task Print_APrintedBlock_WrapsItsLongLinesRatherThanCuttingThemOff()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        var block = Page.Locator("#cbv-long");
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveCSSAsync("white-space", "pre-wrap");
        await Expect(block.Locator(".bit-aicbv-bdy")).ToHaveCSSAsync("overflow-x", "visible");
        await Expect(block).ToHaveCSSAsync("break-inside", "avoid");

        var sizes = await block.Locator(".bit-aicbv-bdy").EvaluateAsync<double[]>("e => [e.scrollWidth, e.clientWidth]");
        Assert.IsTrue(sizes[0] <= sizes[1] + 1, "Nothing of the line is left beyond the edge.");
    }

    // ---------- helpers ----------

    // The clipboard of the browser is not one a test can read alike in every engine - nor one of its own, with test
    // classes running beside each other in one browser - so what the button hands the Clipboard API is recorded instead.
    private Task PrepareClipboardAsync() => Page.AddInitScriptAsync(ClipboardRecorder);

    private async Task ExpectClipboardAsync(string expected)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string last;

        // The copy is a round trip to the script, so the text lands a moment after the press.
        while ((last = await Page.EvaluateAsync<string>("() => (window.__e2eClipboard ?? []).at(-1) ?? ''")) != expected && watch.ElapsedMilliseconds < 15000)
        {
            await Task.Delay(100);
        }

        Assert.AreEqual(expected, last, "The clipboard holds exactly the code.");
    }
}
