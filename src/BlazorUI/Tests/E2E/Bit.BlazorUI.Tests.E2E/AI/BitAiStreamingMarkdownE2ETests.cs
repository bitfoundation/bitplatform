using System.Globalization;
using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiStreamingMarkdown at /e2e/aistreamingmarkdown in a real browser: Markdown that grows one cut at a time
/// - a bold run, an emphasis and a code span whose closing markers have not arrived, a fence still open, a table half
/// written - drawn settled at every step and never with a literal marker; the end of the stream dropping the caret and
/// showing a marker that never closed as written; a whole answer played by the scripted transport, watched at every
/// state it goes through; the caret (its blink, reduced motion, right to left, print, forced colors), the Markdown
/// options in force, the --bit-AiMarkdownView-* variables and Classes/Styles reaching the view it draws with.
/// </summary>
[TestClass]
public class BitAiStreamingMarkdownE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aistreamingmarkdown";

    // ---------- progressive rendering ----------

    [TestMethod]
    public async Task Steps_EmphasisAndCodeSpansStreamingIn_NeverShowTheirMarkers()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#sm-steps .bit-aimdv");
        var first = view.Locator("p.bit-aimdv-par").Nth(0);
        var second = view.Locator("p.bit-aimdv-par").Nth(1);

        // "The **short": the bold run has not closed, and its marker is not drawn while it waits.
        await StepAsync(1);
        await Expect(Page.Locator("#sm-raw")).ToHaveTextAsync("The **short");
        await Expect(first).ToHaveTextAsync("The short");
        await Expect(first.Locator("strong")).ToHaveCountAsync(0);
        await Expect(first.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        // The closing marker arrives: the run turns bold, and the next emphasis waits in its turn.
        await StepAsync(2);
        await Expect(first.Locator("strong")).ToHaveTextAsync("short answer");
        await Expect(first).ToHaveTextAsync("The short answer is ma");
        await Expect(first.Locator("em")).ToHaveCountAsync(0);

        // A code span whose closing backtick has not arrived is plain text without the backtick.
        await StepAsync(3);
        await Expect(first.Locator("em")).ToHaveTextAsync("matters");
        await Expect(first).ToHaveTextAsync("The short answer is matters.");
        await Expect(second).ToHaveTextAsync("Call State");
        await Expect(second.Locator("code")).ToHaveCountAsync(0);

        // The caret moved on with the text to the paragraph being written.
        await Expect(first.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(second.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        await StepAsync(4);
        await Expect(second.Locator("code.bit-aimdv-cod")).ToHaveTextAsync("StateHasChanged");
        await Expect(second).ToHaveTextAsync("Call StateHasChanged sparingly.");

        await Expect(view).Not.ToContainTextAsync("*");
        await Expect(view).Not.ToContainTextAsync("`");
    }

    [TestMethod]
    public async Task Steps_AnUnclosedFence_IsAPlainCodeBlockUntilItClosesAndIsHighlightedThen()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#sm-steps .bit-aimdv");
        for (var step = 1; step <= 4; step++) await StepAsync(step);

        // The fence is open: a code block already, its language in the header, its text as it is so far, uncolored.
        var block = view.Locator(".bit-aicbv");
        await Expect(block).ToHaveCountAsync(1);
        await Expect(block.Locator(".bit-aicbv-lng")).ToHaveTextAsync("csharp");
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveTextAsync("var total = items.Sum(i => i.Price");
        await Expect(block.Locator(".bit-aicbv-cod span")).ToHaveCountAsync(0);
        await Expect(view).Not.ToContainTextAsync("```");

        // The closing fence arrives: the block is complete, and highlighted once.
        await StepAsync(5);
        await StepAsync(6);
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveTextAsync("var total = items.Sum(i => i.Price);");
        await Expect(block.Locator(".bit-aicbv-typ").First).ToHaveTextAsync("var");
        await Expect(view.Locator(".bit-aicbv")).ToHaveCountAsync(1);
        await Expect(view).Not.ToContainTextAsync("```");
    }

    [TestMethod]
    public async Task Steps_AClosingFenceHalfWritten_IsNeverDrawnAsCode()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#sm-steps .bit-aimdv");
        for (var step = 1; step <= 5; step++) await StepAsync(step);

        // Two of the three backticks of the closing fence have arrived: they are the start of a fence, not code, and the
        // block waits for the third rather than drawing them.
        await Expect(Page.Locator("#sm-raw")).ToContainTextAsync("i.Price);\n``");
        var block = view.Locator(".bit-aicbv");
        await Expect(block.Locator(".bit-aicbv-pre")).ToHaveTextAsync("var total = items.Sum(i => i.Price);");
        await Expect(block.Locator(".bit-aicbv-pre")).Not.ToContainTextAsync("`");
        await Expect(block.Locator(".bit-aicbv-cod span")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Steps_AHalfWrittenTable_IsATableThatFillsInRowByRow()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#sm-steps .bit-aimdv");
        for (var step = 1; step <= 6; step++) await StepAsync(step);

        var table = view.GetByRole(AriaRole.Table);
        var rows = table.Locator("tbody > tr");

        // The header, its delimiter row and half of the first row: already a table, the cell as far as it got.
        await Expect(table.GetByRole(AriaRole.Columnheader)).ToHaveTextAsync(["Mode", "Runs on"]);
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(rows.Nth(0).Locator("td")).ToHaveTextAsync(["Server", "The ser"]);
        await Expect(view).Not.ToContainTextAsync("|");

        await StepAsync(7);
        await Expect(rows).ToHaveCountAsync(2);
        await Expect(rows.Nth(0).Locator("td")).ToHaveTextAsync(["Server", "The server"]);
        await Expect(rows.Nth(1).Locator("td")).ToHaveTextAsync(["WebAssembly", "The browser"]);
        await Expect(view).Not.ToContainTextAsync("|");
    }

    [TestMethod]
    public async Task Finish_TheStreamEnding_DropsTheCaretAndShowsAMarkerThatNeverClosedAsWritten()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#sm-steps .bit-aimdv");
        for (var step = 1; step <= 7; step++) await StepAsync(step);

        var last = view.Locator("p.bit-aimdv-par").Last;
        await Expect(last).ToHaveTextAsync("And a dangling");
        await Expect(last.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        await Page.Locator("#sm-finish").ClickAsync();
        await Expect(Page.Locator("#sm-streaming")).ToHaveTextAsync("no");

        // Nothing more is coming: the caret goes, and a marker whose partner never came is the text the model wrote.
        await Expect(view.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(last).ToHaveTextAsync("And a **dangling");

        // What was settled stays as it was.
        await Expect(view.GetByRole(AriaRole.Table).Locator("tbody > tr")).ToHaveCountAsync(2);
        await Expect(view.Locator(".bit-aicbv-typ").First).ToHaveTextAsync("var");
        await Expect(view.Locator("strong").First).ToHaveTextAsync("short answer");

        // A text that does not continue the previous one is parsed afresh.
        await Page.Locator("#sm-reset").ClickAsync();
        await StepAsync(1);
        await Expect(view.Locator("p.bit-aimdv-par")).ToHaveCountAsync(1);
        await Expect(view.Locator("p.bit-aimdv-par")).ToHaveTextAsync("The short");
        await Expect(view.Locator(".bit-aicbv, table")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Transport_AStreamedAnswer_ArrivesProgressivelyAndNeverShowsAMarker()
    {
        await OpenAsync(Route);

        // Every state the answer goes through, read the moment the page changes: how long its text is, whether a
        // marker of the Markdown shows in it, and whether the caret trails it.
        await Page.RecordTextLengthsAsync("sm-live", "#sm-live");
        await Page.EvaluateAsync(
            """
            () => {
                const state = window.__e2eStream = { samples: 0, caret: false, prose: [] };
                const read = () => {
                    const live = document.querySelector('#sm-live');
                    if (!live) return;
                    state.samples++;
                    if (live.querySelector('.bit-aimdv-crt')) state.caret = true;
                    const prose = live.cloneNode(true);
                    prose.querySelectorAll('.bit-aicbv').forEach(e => e.remove());
                    if (/[*`]/.test(prose.textContent)) state.prose.push(prose.textContent.slice(-60));
                };
                new MutationObserver(read).observe(document.body, { childList: true, subtree: true, characterData: true });
            }
            """);

        await Page.Locator("#sm-send").ClickAsync();
        await Expect(Page.Locator("#sm-live-state")).ToHaveTextAsync("Complete");

        var live = Page.Locator("#sm-live");
        await Expect(live.Locator("h2.bit-aimdv-hdg")).ToHaveTextAsync("Render modes");
        await Expect(live.Locator("strong")).ToHaveTextAsync("two places");
        await Expect(live.GetByRole(AriaRole.Table).Locator("tbody > tr")).ToHaveCountAsync(2);
        await Expect(live.Locator("a.bit-aimdv-lnk")).ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes");
        await Expect(live.Locator("code.bit-aimdv-cod")).ToHaveTextAsync("StateHasChanged");
        await Expect(live.Locator(".bit-aicbv-typ").First).ToHaveTextAsync("var");
        await Expect(live.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);

        // It arrived a little at a time, not in one piece at the end.
        var lengths = await Page.TextLengthsAsync("sm-live");
        Assert.IsTrue(lengths.Length >= 6, $"The answer went through several states: {string.Join(",", lengths)}.");
        Assert.IsTrue(lengths.Count(l => l > 0 && l < lengths[^1] / 2) >= 2, $"Parts of the answer were drawn long before the whole of it: {string.Join(",", lengths)}.");

        var stream = await Page.EvaluateAsync<JsonElement>("() => window.__e2eStream");
        Assert.IsTrue(stream.GetProperty("caret").GetBoolean(), "The caret trailed the text while it streamed.");
        var prose = stream.GetProperty("prose").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.AreEqual(0, prose.Length, $"A marker of the Markdown showed in the prose: {string.Join(" | ", prose)}");
    }

    // ---------- caret ----------

    [TestMethod]
    public async Task Caret_WhileStreaming_TrailsTheTextAndBlinks()
    {
        await OpenAsync(Route);

        var paragraph = Page.Locator("#sm-caret p.bit-aimdv-par");
        var caret = paragraph.Locator(".bit-aimdv-crt");

        await Expect(caret).ToHaveCountAsync(1);
        await Expect(caret).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(caret).ToHaveCSSAsync("animation-name", "bit-aimdv-blink");
        await Expect(caret).ToHaveCSSAsync("animation-iteration-count", "infinite");
        Assert.AreEqual(1100, await caret.AnimationDurationAsync("bit-aimdv-blink") ?? 0, 1);
        await Expect(caret).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));

        var last = await paragraph.CharacterRectAsync("The answer is still being".Length - 1);
        Assert.IsTrue((await caret.RectAsync()).Left >= last.Right - 0.5, "The caret is after the last character.");

        // Text that is complete has none, and neither has one under options that turn it off.
        await Expect(Page.Locator("#sm-caret-done .bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sm-caret-off .bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sm-caret-off p.bit-aimdv-par")).ToHaveTextAsync("No caret is drawn after this");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_HoldsTheCaretStillUnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var caret = Page.Locator("#sm-caret .bit-aimdv-crt");
        await Expect(caret).ToHaveCSSAsync("animation-name", "none");
        await Expect(caret).ToHaveCSSAsync("opacity", "0.8");
        await Expect(caret).ToBeVisibleAsync();

        await Expect(Page.Locator("#sm-caret-fam .bit-aimdv-crt")).ToHaveCSSAsync("animation-name", "bit-aimdv-blink");
    }

    // ---------- options ----------

    [TestMethod]
    public async Task Options_SoftBreaksAsHardBreaks_KeepsTheLineBreaksTheModelMeant()
    {
        await OpenAsync(Route);

        var hard = Page.Locator("#sm-hard p.bit-aimdv-par");
        await Expect(hard.Locator("br")).ToHaveCountAsync(1);
        var firstLine = await hard.CharacterRectAsync(0);
        var secondLine = await hard.CharacterRectAsync("Server rendering is fast to first paint.".Length);
        Assert.IsTrue(secondLine.Top >= firstLine.Bottom - 1, "The second sentence starts a line of its own.");

        // By default a single newline is a space, as CommonMark has it.
        var soft = Page.Locator("#sm-soft p.bit-aimdv-par");
        await Expect(soft.Locator("br")).ToHaveCountAsync(0);
        await Expect(soft).ToHaveTextAsync("Server rendering is fast to first paint. WebAssembly works offline.");
    }

    // ---------- rtl ----------

    [TestMethod]
    public async Task Rtl_ARightToLeftAnswer_IsTrailedByTheCaretAtItsLeftEnd()
    {
        await OpenAsync(Route);

        const string text = "پاسخ هنوز در حال نوشتن است";

        foreach (var id in new[] { "sm-rtl", "sm-rtl-inherited" })
        {
            var paragraph = Page.Locator($"#{id} p.bit-aimdv-par");
            var caret = paragraph.Locator(".bit-aimdv-crt");

            await Expect(paragraph).ToHaveCSSAsync("direction", "rtl");
            await Expect(caret).ToHaveCSSAsync("margin-left", "0px");
            await Expect(caret).ToHaveCSSAsync("margin-right", "1px");

            var lastCharacter = await paragraph.CharacterRectAsync(text.Length - 1);
            var box = await caret.RectAsync();
            Assert.IsTrue(box.Right <= lastCharacter.Left + 0.5, $"{id}: the caret ({box.Right}) is at the left end of the text ({lastCharacter.Left}).");
        }

        var ltr = Page.Locator("#sm-ltr .bit-aimdv-crt");
        await Expect(ltr).ToHaveCSSAsync("margin-left", "1px");
        await Expect(ltr).ToHaveCSSAsync("margin-right", "0px");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task Variables_TheMarkdownViewVariables_RestyleTheStreamingText()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#sm-vars .bit-aimdv-crt")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#sm-vars .bit-aimdv-hdg")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        // Its own root lets the text shrink inside a flex parent rather than widening it.
        await Expect(Page.Locator("#sm-vars")).ToHaveClassAsync(HasClass("bit-aismd"));
        await Expect(Page.Locator("#sm-vars")).ToHaveCSSAsync("min-width", "0px");
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheRootAndTheViewItDrawsWith()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#sm-parts");
        await Expect(root).ToHaveClassAsync(HasClass("e2e-sm-class"));
        await Expect(root).ToHaveClassAsync(HasClass("e2e-sm-root"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(7, 8, 9)");
        await Expect(root).ToHaveCSSAsync("padding-top", "6px");

        await Expect(root.Locator(".bit-aimdv")).ToHaveClassAsync(HasClass("e2e-sm-view"));
        await Expect(root.Locator(".bit-aimdv-hdg")).ToHaveClassAsync(HasClass("e2e-sm-heading"));
        await Expect(root.Locator(".bit-aimdv-hdg")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-aimdv-crt")).ToHaveClassAsync(HasClass("e2e-sm-caret"));
        await Expect(root.Locator(".bit-aimdv-crt")).ToHaveCSSAsync("width", "20px");
    }

    [TestMethod]
    public async Task Print_APrintedAnswerStillStreaming_HasNoCaret()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#sm-caret .bit-aimdv-crt")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task ForcedColors_Active_PaintsTheCaretInTheSystemHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        await Expect(Page.Locator("#sm-caret .bit-aimdv-crt")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("Highlight"));
    }

    // ---------- helpers ----------

    private async Task StepAsync(int step)
    {
        await Page.Locator("#sm-next").ClickAsync();
        await Expect(Page.Locator("#sm-step")).ToHaveTextAsync(step.ToString(CultureInfo.InvariantCulture));
    }
}
