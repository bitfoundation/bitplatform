namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiMessagePartView at /e2e/aimessagepartview in a real browser: every part of the everything script's
/// streamed reply (reasoning, cited text, an artifact, structured data, generative UI) drawn by the view of its kind, a
/// part of every other kind (text, Markdown, reasoning, code, a tool call and a tool result, an image, a file, structured
/// data, sources, an error) dispatched the same way, a kind no view knows shown as its text, a part nothing is drawn for
/// (a silent kind, a citation) leaving no element at all, a registered
/// renderer drawing its kind inside a boundary and a change of one part rendering that part's view and no other, Styles
/// and Classes, a wide part scrolling inside its own view, and RTL.
/// </summary>
[TestClass]
public class BitAiMessagePartViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aimessagepartview";

    private ILocator View(string id) => Page.Locator($"#{id}");

    // The one element a part view holds: the view of the part's kind.
    private ILocator Inner(string id) => Page.Locator($"#{id} > *");

    [TestMethod]
    public async Task Everything_EachPartOfAStreamedReply_IsDrawnByTheViewOfItsKind()
    {
        await OpenAsync(Route);

        await Page.Locator("#ev-ask").ClickAsync();

        await Expect(Page.Locator("#ev-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ev-kinds")).ToHaveTextAsync("reasoning,text,artifact,structured-data,generative-ui");

        var views = Page.Locator("#everything .bit-aimpv");
        await Expect(views).ToHaveCountAsync(5);

        (string Kind, string ViewClass)[] expected =
        [
            ("reasoning", "bit-airpv"),
            ("text", "bit-aitpv"),
            ("artifact", "bit-aiapv"),
            ("structured-data", "bit-aisdv"),
            ("generative-ui", "bit-aiguv"),
        ];

        for (var i = 0; i < expected.Length; i++)
        {
            var id = $"ev-{i}";
            await Expect(View(id)).ToHaveAttributeAsync("data-kind", expected[i].Kind);
            await Expect(Inner(id)).ToHaveCountAsync(1);
            await Expect(Inner(id)).ToHaveClassAsync(HasClass(expected[i].ViewClass));
        }

        await Expect(Inner("ev-1")).ToContainTextAsync("Interactive Server keeps the state of a component on the server.");
        await Expect(Inner("ev-2")).ToContainTextAsync("Notes");
        await Expect(Inner("ev-3").Locator("table")).ToContainTextAsync("WebAssembly");
        await Expect(Inner("ev-4").Locator(".e2e-ai-card-title")).ToHaveTextAsync("Visitors");
        await Expect(Inner("ev-4").Locator(".e2e-ai-card-value")).ToHaveTextAsync("1200");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Kinds_EveryOtherKindOfPart_IsDispatchedToItsView()
    {
        await OpenAsync(Route);

        (string Id, string ViewClass, string Text)[] expected =
        [
            ("kind-text", "bit-aitpv", "Plain text of the answer."),
            ("kind-markdown", "bit-aitpv", "A Markdown part."),
            ("kind-reasoning", "bit-airpv", ""),
            ("kind-code", "bit-aicbv", "var total = 1 + 2;"),
            ("kind-tool-call", "bit-aitcv", "Used Weather"),
            ("kind-tool-result", "bit-aitrv", "2 results"),
            ("kind-image", "bit-aiipv", ""),
            ("kind-file", "bit-aiach", "report.pdf"),
            ("kind-structured", "bit-aisdv", "WebAssembly"),
            ("kind-sources", "bit-aisrl", "Render modes"),
            ("kind-error", "bit-aierv", "The tool loop stopped."),
        ];

        foreach (var (id, viewClass, text) in expected)
        {
            await Expect(View(id)).ToHaveClassAsync(HasClass("bit-aimpv"));
            await Expect(Inner(id)).ToHaveCountAsync(1);
            await Expect(Inner(id)).ToHaveClassAsync(HasClass(viewClass));
            if (text.Length > 0) await Expect(Inner(id)).ToContainTextAsync(text);
        }

        // What the views of those kinds make of them, in the page.
        await Expect(View("kind-text").Locator("strong")).ToHaveTextAsync("text");
        await Expect(View("kind-markdown").Locator("em")).ToHaveTextAsync("Markdown");
        await Expect(View("kind-image").Locator("img")).ToHaveAttributeAsync("alt", "A chart of the visitors");
        await Expect(View("kind-error").Locator("[role=alert]")).ToHaveCountAsync(1);
        await Expect(View("kind-tool-call").Locator(".bit-aitcv-hdr")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Unknown_AKindNoViewKnows_IsShownAsTheTextItCarries_OrNotAtAll()
    {
        await OpenAsync(Route);

        var fallback = Inner("kind-unknown-text");
        await Expect(fallback).ToHaveCountAsync(1);
        await Expect(fallback).ToHaveClassAsync(HasClass("bit-aimpv-fbk"));
        Assert.AreEqual("P", await fallback.EvaluateAsync<string>("e => e.tagName"));
        Assert.AreEqual("A kind no view knows,\nshown as the text it carries.", (await fallback.InnerTextAsync()).Replace("\r", ""));
        await Expect(fallback).ToHaveCSSAsync("white-space", "pre-wrap");
        await Expect(fallback).ToHaveCSSAsync("margin-top", "0px");

        // A part nothing is drawn for - a kind with no text to show, a citation (which its text and the source list
        // show) - leaves no element behind, rather than an empty one among the parts of the message.
        foreach (var id in new[] { "kind-unknown-silent", "kind-citation" })
        {
            await Expect(View(id)).ToHaveCountAsync(0);
        }

        await Expect(Page.Locator("#kinds > div > .bit-aimpv")).ToHaveCountAsync(13);
        var emptyViews = await Page.Locator("#kinds .bit-aimpv").EvaluateAllAsync<int>("views => views.filter(v => v.childElementCount === 0).length");
        Assert.AreEqual(0, emptyViews, "No part view is left empty.");
    }

    [TestMethod]
    public async Task Custom_ARegisteredRenderer_DrawsItsKindInsideABoundary_AndIsHandedTheMessage()
    {
        await OpenAsync(Route);

        var note = View("kind-custom").Locator(".e2e-mpv-note");
        await Expect(note).ToHaveTextAsync("A note drawn by the page's own renderer.");
        await Expect(note).ToHaveAttributeAsync("data-message", "msg-e2e-mpv");
        await Expect(View("kind-custom").Locator(".bit-airbd")).ToHaveCountAsync(0);
        await Expect(View("kind-custom").Locator(".bit-aimpv-fbk")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Custom_AChangeOfOnePart_RendersThatPartsViewAndNoOther()
    {
        await OpenAsync(Route);

        var first = View("cst-first").Locator(".e2e-mpv-note");
        var second = View("cst-second").Locator(".e2e-mpv-note");
        await Expect(first).ToHaveTextAsync("First note");
        var firstRenders = int.Parse(await first.GetAttributeAsync("data-renders") ?? "0");
        var secondRenders = int.Parse(await second.GetAttributeAsync("data-renders") ?? "0");

        // Changed off the renderer's thread, by a button that renders only itself.
        await Page.Locator("#cst-change-first").ClickAsync();

        await Expect(first).ToHaveTextAsync("First note, changed");
        await Expect(first).ToHaveAttributeAsync("data-renders", (firstRenders + 1).ToString());
        await NextFramesAsync();
        await Expect(second).ToHaveAttributeAsync("data-renders", secondRenders.ToString());

        // A render of the page around them renders both, which is what the counts would have shown above.
        await Page.Locator("#cst-rerender").ClickAsync();
        await Expect(Page.Locator("#cst-rerender")).ToHaveTextAsync("Re-render the page (1)");
        await Expect(second).Not.ToHaveAttributeAsync("data-renders", secondRenders.ToString());
    }

    [TestMethod]
    public async Task Style_StylesAndClasses_ReachTheRootAndTheFallback()
    {
        await OpenAsync(Route);

        var view = View("mpv-styles");
        await Expect(view).ToHaveCSSAsync("padding-top", "6px");
        await Expect(view).ToHaveClassAsync(HasClass("e2e-mpv-root"));
        await Expect(view).ToHaveCSSAsync("display", "block");
        await Expect(view.Locator(".bit-aimpv-fbk")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(view.Locator(".bit-aimpv-fbk")).ToHaveClassAsync(HasClass("e2e-mpv-fallback"));
    }

    [TestMethod]
    public async Task Narrow_AWidePart_ScrollsInsideItsOwnView_RatherThanWideningTheMessage()
    {
        await OpenAsync(Route);

        var view = View("mpv-narrow");
        var (_, _, right, _) = await view.RectAsync();
        var (_, _, boxRight, _) = await Page.Locator("#mpv-narrow-box").RectAsync();
        Assert.IsTrue(right <= boxRight + 0.5, "The part view keeps to the width of the message.");
        await Expect(view).ToHaveCSSAsync("min-width", "0px");
        Assert.IsTrue(await view.EvaluateAsync<bool>("e => [...e.querySelectorAll('*')].some(x => x.scrollWidth > x.clientWidth + 10 && ['auto', 'scroll'].includes(getComputedStyle(x).overflowX))"),
                      "The code scrolls inside its own view.");
    }

    [TestMethod]
    public async Task Rtl_TheViewAndWhatItShowsReadFromTheRight()
    {
        await OpenAsync(Route);

        var view = View("mpv-rtl");
        await Expect(view).ToHaveAttributeAsync("dir", "rtl");
        await Expect(view).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(view.Locator(".bit-aimpv-fbk")).ToHaveCSSAsync("direction", "rtl");
        await Expect(View("mpv-styles").Locator(".bit-aimpv-fbk")).ToHaveCSSAsync("direction", "ltr");
    }
}
