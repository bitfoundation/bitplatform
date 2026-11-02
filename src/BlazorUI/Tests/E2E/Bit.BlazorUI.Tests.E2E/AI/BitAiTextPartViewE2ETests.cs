namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiTextPartView at /e2e/aitextpartview in a real browser: a text part rendered as Markdown, the anchored
/// citations of its message woven in as markers right where each cited span ends (none inside code), the markers
/// activated by the pointer and the keyboard, inline citations turned off, a part streaming in with its caret and a
/// citation held back until its text has arrived, the citations script played by the transport, right-to-left text,
/// the --bit-AiMarkdownView-* variables and Classes/Styles reaching the Markdown it draws with.
/// </summary>
[TestClass]
public class BitAiTextPartViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aitextpartview";

    private const string First = "Interactive Server keeps the state on the server.";
    private const string Second = " If the circuit drops, the state goes with it.";

    // The text of the paragraph ahead of an element of it, as a reader meets it.
    private const string TextBefore =
        """
        e => {
            const paragraph = e.closest('p');
            const range = document.createRange();
            range.setStart(paragraph, 0);
            range.setEndBefore(e);
            return range.toString();
        }
        """;

    [TestMethod]
    public async Task Basic_ATextPart_IsRenderedAsMarkdown()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#tpv-basic");
        await Expect(view).ToHaveClassAsync(HasClass("bit-aitpv"));
        await Expect(view.Locator(":scope > .bit-aismd > .bit-aimdv")).ToHaveCountAsync(1);

        var paragraph = view.Locator("p.bit-aimdv-par");
        await Expect(paragraph.Locator("strong")).ToHaveTextAsync("short answer");
        await Expect(paragraph.Locator("em")).ToHaveTextAsync("where");
        await Expect(paragraph.Locator("a.bit-aimdv-lnk")).ToHaveAttributeAsync("href", "https://bitplatform.dev/");
        await Expect(paragraph.Locator("code.bit-aimdv-cod")).ToHaveTextAsync("StateHasChanged");

        // A part that is complete has no caret.
        await Expect(view.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(view).ToHaveCSSAsync("min-width", "0px");
    }

    // ---------- citations ----------

    [TestMethod]
    public async Task Citations_AnchoredSpans_GetTheirMarkersRightWhereTheClaimEnds()
    {
        await OpenAsync(Route);

        var paragraph = Page.Locator("#tpv-cited p.bit-aimdv-par");
        var markers = paragraph.Locator("button.bit-aicmk");

        await Expect(markers).ToHaveTextAsync(["1", "2"]);
        await Expect(markers.Nth(0)).ToHaveAccessibleNameAsync(new System.Text.RegularExpressions.Regex("Blazor render modes"));
        await Expect(markers.Nth(1)).ToHaveAccessibleNameAsync(new System.Text.RegularExpressions.Regex("SignalR circuit lifetime"));

        // Mid-text, after the sentence each one cites - not appended at the end where no one connects it to anything.
        Assert.AreEqual(First, await markers.Nth(0).EvaluateAsync<string>(TextBefore));
        Assert.AreEqual(First + "1" + Second, await markers.Nth(1).EvaluateAsync<string>(TextBefore));

        // The third span ends inside the code span: a marker there would break the code, so it is left out.
        await Expect(paragraph.Locator("code.bit-aimdv-cod")).ToHaveTextAsync("PersistState()");
        await Expect(paragraph).Not.ToContainTextAsync("[3]");
        await Expect(paragraph).ToHaveTextAsync(First + "1" + Second + "2 Call PersistState() to keep it.");
    }

    [TestMethod]
    public async Task Citations_ActivatingAMarker_RaisesOnCitationClickWithItsCitation()
    {
        await OpenAsync(Route);

        var markers = Page.Locator("#tpv-cited button.bit-aicmk");

        await markers.Nth(1).ClickAsync();
        await Expect(Page.Locator("#tpv-log")).ToHaveTextAsync("SignalR circuit lifetime");

        await markers.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#tpv-log")).ToHaveTextAsync("SignalR circuit lifetime,Blazor render modes");
    }

    [TestMethod]
    public async Task Citations_InlineCitationsOff_LeavesTheTextAsTheModelWroteIt()
    {
        await OpenAsync(Route);

        var paragraph = Page.Locator("#tpv-cited-off p.bit-aimdv-par");
        await Expect(paragraph.Locator("button")).ToHaveCountAsync(0);
        await Expect(paragraph).ToHaveTextAsync(First + Second + " Call PersistState() to keep it.");
    }

    // ---------- streaming ----------

    [TestMethod]
    public async Task Streaming_APartStreamingIn_GrowsWithACaretAndHoldsACitationBackUntilItsTextHasArrived()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#tpv-stream");
        var paragraph = view.Locator("p.bit-aimdv-par");

        await Expect(paragraph).ToHaveCountAsync(0);

        await AppendAsync(1);
        await Expect(paragraph).ToHaveTextAsync("Here is bo");
        await Expect(paragraph.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        // The cited span reaches past what has arrived: no marker yet, rather than one in the wrong place.
        await AppendAsync(2);
        await Expect(paragraph.Locator("strong")).ToHaveTextAsync("bold");
        await Expect(paragraph).ToHaveTextAsync("Here is bold text, and a cited");
        await Expect(paragraph.Locator("button.bit-aicmk")).ToHaveCountAsync(0);

        await AppendAsync(3);
        var marker = paragraph.Locator("button.bit-aicmk");
        await Expect(marker).ToHaveTextAsync("1");
        Assert.AreEqual("Here is bold text, and a cited sentence.", await marker.EvaluateAsync<string>(TextBefore));
        await Expect(paragraph.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        await Page.Locator("#tpv-complete").ClickAsync();
        await Expect(paragraph.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);
        await Expect(marker).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Transport_TheCitationsScript_WeavesEachMarkerAfterTheSentenceItCites()
    {
        await OpenAsync(Route);

        await Page.Locator("#tpv-send").ClickAsync();
        await Expect(Page.Locator("#tpv-live-state")).ToHaveTextAsync("Complete");

        var live = Page.Locator("#tpv-live");
        var paragraphs = live.Locator("p.bit-aimdv-par");
        await Expect(paragraphs).ToHaveCountAsync(2);

        var markers = paragraphs.Nth(0).Locator("button.bit-aicmk");
        await Expect(markers).ToHaveTextAsync(["1", "2"]);
        Assert.AreEqual("Interactive Server keeps the state of a component on the server.", await markers.Nth(0).EvaluateAsync<string>(TextBefore));
        Assert.AreEqual("Interactive Server keeps the state of a component on the server.1 If the circuit drops, the state goes with it.",
            await markers.Nth(1).EvaluateAsync<string>(TextBefore));

        await Expect(paragraphs.Nth(1)).ToHaveTextAsync("That is the trade the render mode makes.");
        await Expect(live.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);

        await markers.Nth(0).ClickAsync();
        await Expect(Page.Locator("#tpv-log")).ToHaveTextAsync("Blazor render modes");
    }

    // ---------- rtl ----------

    [TestMethod]
    public async Task Rtl_ARightToLeftPart_ReadsFromTheRightWithItsCodeLeftToRight()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tpv-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "tpv-rtl", "tpv-rtl-inherited" })
        {
            var paragraph = Page.Locator($"#{id} p.bit-aimdv-par");
            await Expect(paragraph).ToHaveCSSAsync("direction", "rtl");

            var code = paragraph.Locator("code.bit-aimdv-cod");
            await Expect(code).ToHaveCSSAsync("direction", "ltr");
            await Expect(code).ToHaveCSSAsync("unicode-bidi", "isolate");
            Assert.IsTrue((await code.CharacterRectAsync(4)).Left < (await code.CharacterRectAsync(5)).Left, $"{id}: the ; of x = 1; stays at its right.");

            // The sentence starts at the right edge of the paragraph.
            var box = await paragraph.RectAsync();
            var first = await paragraph.CharacterRectAsync(0);
            Assert.AreEqual(box.Right, first.Right, 2, $"{id}: the first word is at the right.");
        }
    }

    // ---------- style ----------

    [TestMethod]
    public async Task Variables_TheMarkdownViewVariables_RestyleThePart()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tpv-vars .bit-aimdv-lnk")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#tpv-basic .bit-aimdv-lnk")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-fg)"));
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheRootAndTheMarkdownInsideIt()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#tpv-parts");
        await Expect(root).ToHaveClassAsync(HasClass("e2e-tpv-class"));
        await Expect(root).ToHaveClassAsync(HasClass("e2e-tpv-root"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(root).ToHaveCSSAsync("padding-top", "6px");

        await Expect(root.Locator(".bit-aismd")).ToHaveClassAsync(HasClass("e2e-tpv-markdown"));
        await Expect(root.Locator(".bit-aimdv")).ToHaveClassAsync(HasClass("e2e-tpv-view"));
        await Expect(root.Locator(".bit-aimdv-par")).ToHaveClassAsync(HasClass("e2e-tpv-paragraph"));
        await Expect(root.Locator(".bit-aimdv-par")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-aimdv-lnk")).ToHaveClassAsync(HasClass("e2e-tpv-link"));
    }

    private async Task AppendAsync(int chunks)
    {
        await Page.Locator("#tpv-append").ClickAsync();
        await Expect(Page.Locator("#tpv-chunks")).ToHaveTextAsync(chunks.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
