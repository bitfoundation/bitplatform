using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiCitationMarker at /e2e/aicitationmarker in a real browser: the numbered button set as a superscript of
/// the text around it, its name and its native preview (the quote cut short), activation by the pointer and the keyboard
/// with its focus ring, Disabled, the [n] of Markdown turned into markers inline that reveal and focus their source, the
/// anchored citations of a message, the hover moving variables and never the paint, every --bit-AiCitationMarker-*
/// variable, Styles and Classes, forced colors (an edge, and a number that stays readable under the pointer) and RTL.
/// </summary>
[TestClass]
public class BitAiCitationMarkerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aicitationmarker";

    private ILocator Marker(string id) => Page.Locator($"#{id}");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_AMarker_IsANumberedButtonNamedAfterItsSource()
    {
        await OpenAsync(Route);

        var marker = Marker("cmk-basic");
        Assert.AreEqual("BUTTON", await marker.EvaluateAsync<string>("e => e.tagName"));
        await Expect(marker).ToHaveAttributeAsync("type", "button");
        await Expect(marker).ToHaveClassAsync(HasClass("bit-aicmk"));
        await Expect(marker).ToHaveTextAsync("1");
        await Expect(marker).ToHaveAttributeAsync("aria-label", "Source 1: Blazor render modes");
        await Expect(marker).ToHaveAccessibleNameAsync("Source 1: Blazor render modes");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Source 1: Blazor render modes", Exact = true }).First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Basic_TheMarker_IsASuperscriptSizedByTheTextItSitsIn()
    {
        await OpenAsync(Route);

        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("vertical-align", "super");
        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("font-size", "14px");
        await Expect(Marker("cmk-in-heading")).ToHaveCSSAsync("font-size", "28px");
        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("cursor", "pointer");

        // It sits on the line of the claim, right after its last word, and above its baseline.
        var claim = await Page.Locator("#cmk-claim").RectAsync();
        var marker = await Marker("cmk-basic").RectAsync();
        Assert.AreEqual(claim.Right, marker.Left, 6, "The marker follows the claim.");
        Assert.IsTrue(marker.Top < claim.Bottom && marker.Bottom > claim.Top, "The marker is on the line of the claim.");
        Assert.IsTrue((marker.Top + marker.Bottom) / 2 < (claim.Top + claim.Bottom) / 2, "The marker is raised as a superscript.");

        // The chip at rest is the tint of the primary role with its foreground.
        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri-tint)"));
        await Expect(Marker("cmk-basic")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-fg)"));
    }

    [TestMethod]
    public async Task Preview_TheNativeTitle_IsTheSourceAndTheQuoteCutShort()
    {
        await OpenAsync(Route);

        await Expect(Marker("cmk-basic")).ToHaveAttributeAsync("title", "Blazor render modes\n\nInteractive Server renders on the server.");
        await Expect(Marker("cmk-noquote")).ToHaveAttributeAsync("title", "SignalR circuit lifetime");

        var quote = string.Join(" ", Enumerable.Repeat("render", 50));
        await Expect(Marker("cmk-long")).ToHaveAttributeAsync("title", $"Blazor render modes\n\n{quote[..220].TrimEnd()}...");

        // A title the page wrote is kept, and the name still carries the source.
        await Expect(Marker("cmk-title")).ToHaveAttributeAsync("title", "A preview of the page's own");
        await Expect(Marker("cmk-title")).ToHaveAttributeAsync("aria-label", "Source 1: Blazor render modes");
    }

    [TestMethod]
    public async Task Name_AnUnnumberedOrARenamedMarker_IsNamedAccordingly()
    {
        await OpenAsync(Route);

        await Expect(Marker("cmk-unnumbered")).ToHaveTextAsync("");
        await Expect(Marker("cmk-unnumbered")).ToHaveAttributeAsync("aria-label", "Source: SignalR circuit lifetime");
        await Expect(Marker("cmk-aria")).ToHaveAttributeAsync("aria-label", "Source one, the render modes guide");
        await Expect(Marker("cmk-aria")).ToHaveAccessibleNameAsync("Source one, the render modes guide");
    }

    // ---------- activation ----------

    [TestMethod]
    public async Task Activate_AClick_HandsTheCitationOver()
    {
        await OpenAsync(Route);

        await Marker("cmk-act-2").ClickAsync();
        await Expect(Page.Locator("#cmk-activated")).ToHaveTextAsync("cit-2:2");
        await Expect(Page.Locator("#cmk-activations")).ToHaveTextAsync("1");

        await Marker("cmk-act-1").ClickAsync();
        await Expect(Page.Locator("#cmk-activated")).ToHaveTextAsync("cit-1:1");
        await Expect(Page.Locator("#cmk-activations")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Activate_TheKeyboard_ReachesTheMarkersInOrderAndActivatesThem()
    {
        await OpenAsync(Route);

        await Page.Locator("#cmk-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Marker("cmk-act-1")).ToBeFocusedAsync();

        // The focus ring is drawn for the keyboard.
        await Expect(Marker("cmk-act-1")).Not.ToHaveCSSAsync("box-shadow", "none");
        await Expect(Marker("cmk-act-1")).ToHaveCSSAsync("outline-style", "none");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#cmk-activated")).ToHaveTextAsync("cit-1:1");
        await Expect(Page.Locator("#cmk-activations")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Marker("cmk-act-2")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#cmk-activated")).ToHaveTextAsync("cit-2:2");
        await Expect(Page.Locator("#cmk-activations")).ToHaveTextAsync("2");

        // The disabled marker is no stop of the tab order.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#cmk-after")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_AMarker_IsDrawnDisabledAndHandsNothingOver()
    {
        await OpenAsync(Route);

        var marker = Marker("cmk-act-disabled");
        await Expect(marker).ToBeDisabledAsync();
        await Expect(marker).ToHaveAttributeAsync("disabled", "");
        await Expect(marker).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(marker).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(marker).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-dis-text)"));
        await Expect(marker).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri-dis)"));
        await Expect(marker).ToHaveCSSAsync("cursor", "default");

        await marker.ClickAsync(new() { Force = true });
        await marker.DispatchEventAsync("click");
        await Marker("cmk-act-1").ClickAsync();
        await Expect(Page.Locator("#cmk-activations")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cmk-activated")).ToHaveTextAsync("cit-1:1");
    }

    // ---------- in markdown ----------

    [TestMethod]
    public async Task Markdown_TheBracketedNumbers_BecomeMarkersInlineAndAnUnknownOneStaysText()
    {
        await OpenAsync(Route);

        var paragraph = Page.Locator("#cmk-md p").First;
        await Expect(paragraph).ToContainTextAsync("Interactive Server keeps the state on the server");
        var markers = paragraph.Locator(".bit-aicmk");
        await Expect(markers).ToHaveTextAsync(["1", "2"]);
        await Expect(markers.Nth(0)).ToHaveAttributeAsync("aria-label", "Source 1: Blazor render modes");
        await Expect(markers.Nth(1)).ToHaveAttributeAsync("aria-label", "Source 2: SignalR circuit lifetime");

        // [3] matches no citation, so it stays the text the model wrote.
        await Expect(paragraph).ToContainTextAsync("Nobody wrote this down [3].");
        await Expect(paragraph).Not.ToContainTextAsync("[1]");

        // The markers sit on the lines of the paragraph rather than breaking it.
        var p = await paragraph.RectAsync();
        var first = await markers.Nth(0).RectAsync();
        Assert.IsTrue(first.Top >= p.Top - 2 && first.Bottom <= p.Bottom + 2, "The marker is inside the paragraph.");
        await Expect(markers.Nth(0)).ToHaveCSSAsync("display", "inline-flex");
    }

    [TestMethod]
    public async Task Markdown_AMarkerActivatedByTheKeyboard_RevealsAndFocusesItsSource()
    {
        await OpenAsync(Route);

        var markers = Page.Locator("#cmk-md .bit-aicmk");
        await markers.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(markers.Nth(1)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#cmk-md-activated")).ToHaveTextAsync("2 SignalR circuit lifetime");
        await Expect(Page.Locator("#cmk-md-src-2")).ToBeFocusedAsync();
        await Expect(Page.Locator("#cmk-md-src-2 .bit-aisrc-ttl")).ToHaveTextAsync("SignalR circuit lifetime");

        await markers.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#cmk-md-activated")).ToHaveTextAsync("1 Blazor render modes");
        await Expect(Page.Locator("#cmk-md-src-1")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Message_AnchoredCitations_AreMarkedInTheTextAndListedUnderTheSameNumbers()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-message-ask").ClickAsync();

        var message = Page.Locator("#cmk-msg");
        await Expect(message).ToContainTextAsync("That is the trade the render mode makes.");
        var markers = message.Locator(".bit-aicmk");
        await Expect(markers).ToHaveTextAsync(["1", "2"]);
        await Expect(markers.Nth(0)).ToHaveAttributeAsync("title", "Blazor render modes\n\nInteractive Server renders on the server.");
        await Expect(markers.Nth(1)).ToHaveAttributeAsync("title", "SignalR circuit lifetime\n\nWhen a circuit is lost, its state is disposed.");

        // Each marker closes the sentence it supports.
        var before = await markers.Nth(0).EvaluateAsync<string>(
            "e => { let n = e.previousSibling; while (n && (n.nodeType === Node.COMMENT_NODE || !n.textContent.trim())) n = n.previousSibling; return n ? n.textContent : ''; }");
        StringAssert.EndsWith(before.TrimEnd(), "on the server.");

        var list = message.Locator(".bit-aisrl");
        await Expect(list.Locator(".bit-aisrc-idx")).ToHaveTextAsync(["1", "2"]);
        await Expect(list.Locator(".bit-aisrc-ttl")).ToHaveTextAsync(["Blazor render modes", "SignalR circuit lifetime"]);

        await markers.Nth(1).ClickAsync();
        await Expect(Page.Locator("#cmk-msg-activated")).ToHaveTextAsync("SignalR circuit lifetime");
        await AssertNoServerErrorsAsync();
    }

    // ---------- look ----------

    [TestMethod]
    public async Task Hover_ThePointer_MovesTheMarkerToItsHoverColors()
    {
        await OpenAsync(Route);

        var marker = Marker("cmk-basic");
        await marker.HoverAsync();
        await Expect(marker).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(marker).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-text)"));

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(marker).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri-tint)"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheMarkerAtRestAndUnderThePointer()
    {
        await OpenAsync(Route);

        var marker = Marker("cmk-vars");
        await Expect(marker).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(marker).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(marker).ToHaveCSSAsync("border-top-left-radius", "6px");

        await marker.HoverAsync();
        await Expect(marker).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(marker).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
    }

    [TestMethod]
    public async Task State_AClassOfThePage_KeepsItsPaintUnderThePointer()
    {
        await OpenAsync(Route);

        var marker = Marker("cmk-class");
        await Expect(marker).ToHaveCSSAsync("background-color", "rgb(9, 9, 9)");
        await marker.HoverAsync();
        await Expect(marker).ToHaveCSSAsync("background-color", "rgb(9, 9, 9)");
        await Expect(marker).ToHaveCSSAsync("color", "rgb(250, 250, 250)");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheButton()
    {
        await OpenAsync(Route);

        await Expect(Marker("cmk-styles")).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Marker("cmk-styles")).ToHaveClassAsync(HasClass("e2e-cmk-root"));
        await Expect(Marker("cmk-class")).ToHaveClassAsync(HasClass("e2e-cmk-paint"));
    }

    [TestMethod]
    public async Task ForcedColors_TheMarker_HasAnEdgeAndStaysReadableUnderThePointer()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var marker = Marker("cmk-basic");
        await Expect(marker).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(marker).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("ButtonText"));
        await AssertReadableAsync(marker, "at rest");

        // Under the pointer the number must still be readable against what it sits on.
        await marker.HoverAsync();
        await NextFramesAsync();
        await Page.WaitForTimeoutAsync(300);
        await AssertReadableAsync(marker, "under the pointer");
        await Expect(marker).ToHaveCSSAsync("border-top-style", "solid");

        // The paint stays the one at rest, and the state is carried by the system Highlight on the edge.
        await Expect(marker).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("Highlight"));

        // A disabled marker is GrayText, edge and number alike.
        var gray = await Page.ResolveColorAsync("GrayText");
        await Expect(Marker("cmk-act-disabled")).ToHaveCSSAsync("color", gray);
        await Expect(Marker("cmk-act-disabled")).ToHaveCSSAsync("border-top-color", gray);
    }

    [TestMethod]
    public async Task Rtl_TheMarker_EndsTheClaimInItsReadingDirection()
    {
        await OpenAsync(Route);

        await Expect(Marker("cmk-rtl-inherited")).ToHaveCSSAsync("direction", "rtl");
        var claim = await Page.Locator("#cmk-rtl-claim").RectAsync();
        var marker = await Marker("cmk-rtl-inherited").RectAsync();
        Assert.AreEqual(claim.Left, marker.Right, 6, "Right to left, the marker follows the claim on its left.");

        await Expect(Marker("cmk-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Marker("cmk-rtl")).ToHaveCSSAsync("direction", "rtl");
    }

    // ---------- helpers ----------

    // The number is readable when its color and the color under it are not one and the same.
    private async Task AssertReadableAsync(ILocator marker, string when)
    {
        var colors = await marker.EvaluateAsync<string[]>(
            """
            e => {
                const s = getComputedStyle(e);
                let bg = s.backgroundColor, n = e;
                while ((bg === 'rgba(0, 0, 0, 0)' || bg === 'transparent') && n.parentElement) { n = n.parentElement; bg = getComputedStyle(n).backgroundColor; }
                return [s.color, bg];
            }
            """);

        Assert.AreNotEqual(colors[0], colors[1], $"The number is readable {when}: {colors[0]} on {colors[1]}.");
    }
}
