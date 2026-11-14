using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

[TestClass]
public class BitMenuButtonNoOverlayTests : BunitTestContext
{
    private const string Toggle = "BitBlazorUI.Callouts.toggle";

    private static List<BitMenuButtonItem> Items() =>
    [
        new() { Text = "Cut", Key = "cut" },
        new() { Text = "Copy", Key = "copy" }
    ];

    private IRenderedComponent<BitMenuButton<BitMenuButtonItem>> Render(bool noOverlay, bool split = false, List<string?>? clicks = null)
    {
        return RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Actions");
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.NoOverlay, noOverlay);
            parameters.Add(p => p.Split, split);
            if (clicks is not null)
            {
                parameters.Add(p => p.OnClick, (BitMenuButtonItem? item) => clicks.Add(item?.Text));
            }
        });
    }

    private static string GetOverlayId(JSRuntimeInvocation toggle) => (string)toggle.Arguments[5]!;

    [TestMethod]
    public void BitMenuButtonShouldRenderNoOverlayWhenThePageKeepsItsOwnClicks()
    {
        var com = Render(noOverlay: true);

        Assert.AreEqual(0, com.FindAll(".bit-mnb-ovl").Count);

        // Everything else about the menu is unchanged: only the element between it and the page is gone.
        Assert.IsNotNull(com.Find(".bit-mnb-cal"));
        Assert.IsNotNull(com.Find(".bit-mnb-opb"));
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderTheOverlayByDefault()
    {
        var com = Render(noOverlay: false);

        Assert.AreEqual(1, com.FindAll(".bit-mnb-ovl").Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitMenuButtonShouldTellThePositioningWhetherAnOverlayTakesTheOutsideClicks(bool noOverlay)
    {
        var com = Render(noOverlay);

        com.Find(".bit-mnb-opb").Click();

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[Toggle].Count));

        var toggle = Context.JSInterop.Invocations[Toggle][0];

        // An empty id is how the JS side is told that there is no overlay to take the outside clicks for this
        // menu, and that the page-level handler has to dismiss it instead.
        var expected = noOverlay ? string.Empty : com.Find(".bit-mnb-ovl").Id;

        Assert.AreEqual(expected, GetOverlayId(toggle));
    }

    [TestMethod]
    public async Task BitMenuButtonWithoutAnOverlayShouldCloseOnceTheJsSideDismissesIt()
    {
        var com = Render(noOverlay: true);

        com.Find(".bit-mnb-opb").Click();

        Assert.IsTrue(com.Instance.IsOpen);

        // What a click or a right-click on the page outside of the menu ends in.
        await com.InvokeAsync(() => com.Instance.CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual("false", com.Find(".bit-mnb-opb").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitMenuButtonWithoutAnOverlayShouldCloseItsMenuFromItsButton()
    {
        var com = Render(noOverlay: true);

        var button = com.Find(".bit-mnb-opb");

        button.Click();

        Assert.IsTrue(com.Instance.IsOpen);

        // Nothing covers the button any more, so the click that would have landed on the overlay reaches it.
        button.Click();

        Assert.IsFalse(com.Instance.IsOpen);
        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations[Toggle].Count));
        Assert.AreEqual(false, Context.JSInterop.Invocations[Toggle][1].Arguments[6]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitMenuButtonShouldCloseItsMenuWhenTheMainHalfOfTheSplitButtonRunsWithItOpen(bool noOverlay)
    {
        var clicks = new List<string?>();
        var com = Render(noOverlay, split: true, clicks: clicks);

        com.Find(".bit-mnb-chb").Click();

        Assert.IsTrue(com.Instance.IsOpen);

        // Without an overlay the main half can be clicked with the menu open; the command still runs, and running
        // it is leaving the menu.
        com.Find(".bit-mnb-opb").Click();

        Assert.IsFalse(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new string?[] { null }, clicks);
    }

    [TestMethod]
    public void BitMenuButtonShouldNotOpenItsMenuFromTheMainHalfOfTheSplitButton()
    {
        var clicks = new List<string?>();
        var com = Render(noOverlay: true, split: true, clicks: clicks);

        com.Find(".bit-mnb-opb").Click();

        // The main half runs its command and leaves the menu to the chevron, as it always has.
        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(1, clicks.Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[Toggle].Count);
    }
}
