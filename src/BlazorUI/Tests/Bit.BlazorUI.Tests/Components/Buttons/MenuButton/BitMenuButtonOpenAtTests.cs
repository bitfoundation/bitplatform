using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

[TestClass]
public class BitMenuButtonOpenAtTests : BunitTestContext
{
    private const string Toggle = "BitBlazorUI.Callouts.toggle";
    private const string Reposition = "BitBlazorUI.Callouts.reposition";
    private const string FocusItem = "BitBlazorUI.MenuButtons.focusItem";
    private const string CaptureOrigin = "BitBlazorUI.Utils.captureFocusOrigin";
    private const string RestoreOrigin = "BitBlazorUI.Utils.restoreFocusOrigin";
    private const string FocusElement = "Blazor._internal.domWrapper.focus";

    private static List<BitMenuButtonItem> Items() =>
    [
        new() { Text = "Cut", Key = "cut" },
        new()
        {
            Text = "Share",
            Key = "share",
            ChildItems = [new() { Text = "Email", Key = "email" }]
        },
        new() { Text = "Delete", Key = "delete" }
    ];

    private IRenderedComponent<BitMenuButton<BitMenuButtonItem>> Render(bool disabled = false, bool split = false)
    {
        return RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Actions");
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.Split, split);
        });
    }

    private static string GetComponentId(JSRuntimeInvocation toggle) => (string)toggle.Arguments[1]!;

    [TestMethod]
    public async Task BitMenuButtonShouldOpenItsMenuAtAPointOnTheScreen()
    {
        var com = Render();

        // The point is only rendered for a menu that was opened at one.
        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);

        await com.InvokeAsync(() => com.Instance.OpenAt(120, 240));

        Assert.IsTrue(com.Instance.IsOpen);
        Assert.AreEqual("true", com.Find(".bit-mnb-opb").GetAttribute("aria-expanded"));

        var point = com.Find(".bit-mnb-pnt");
        var style = point.GetAttribute("style");

        Assert.IsTrue(style!.Contains("left:120px"));
        Assert.IsTrue(style.Contains("top:240px"));
        Assert.AreEqual("true", point.GetAttribute("aria-hidden"));

        // The menu is placed against that point rather than against the button, and once.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[Toggle].Count));

        var toggle = Context.JSInterop.Invocations[Toggle][0];

        Assert.AreEqual(point.Id, GetComponentId(toggle));
        Assert.AreEqual(true, toggle.Arguments[6]);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldMoveTheFocusIntoAMenuOpenedAtAPoint()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        // The trigger is not where the user is, so the keyboard is taken straight to the first item.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[FocusItem].Count));

        var focus = Context.JSInterop.Invocations[FocusItem][0];

        Assert.AreEqual(com.Find(".bit-mnb-cal").Id, focus.Arguments[0]);
        Assert.AreEqual("first", focus.Arguments[1]);

        // The origin is remembered before the focus leaves it.
        var capture = Context.JSInterop.Invocations[CaptureOrigin].Single();

        Assert.AreEqual(com.Find(".bit-mnb-cal").Id, capture.Arguments[0]);
        Assert.IsTrue(Context.JSInterop.Invocations.ToList().IndexOf(capture) < Context.JSInterop.Invocations.ToList().IndexOf(focus));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldOpenAtThePointOfAMouseEvent()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(new MouseEventArgs { ClientX = 33, ClientY = 44 }));

        var style = com.Find(".bit-mnb-pnt").GetAttribute("style");

        Assert.IsTrue(style!.Contains("left:33px"));
        Assert.IsTrue(style.Contains("top:44px"));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldWriteThePointWithTheInvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            // A comma decimal separator would leave the browser with a length it cannot read.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var com = Render();

            await com.InvokeAsync(() => com.Instance.OpenAt(10.5, 20.25));

            var style = com.Find(".bit-mnb-pnt").GetAttribute("style");

            Assert.IsTrue(style!.Contains("left:10.5px"));
            Assert.IsTrue(style.Contains("top:20.25px"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public async Task BitMenuButtonShouldMoveAMenuOpenAtAPointWithoutReopeningIt()
    {
        var changes = new List<bool>();

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.IsOpenChanged, v => changes.Add(v));
        });

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));
        await com.InvokeAsync(() => com.Instance.OpenAt(30, 40));

        var style = com.Find(".bit-mnb-pnt").GetAttribute("style");

        Assert.IsTrue(style!.Contains("left:30px"));
        Assert.IsTrue(style.Contains("top:40px"));

        // Nothing about the open state changed for the second call, so nothing is reported for it.
        Assert.IsTrue(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new[] { true }, changes);

        // The menu never went anywhere, so it is laid out again rather than toggled again, and the keyboard is
        // taken to the top of the menu at its new place.
        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[Reposition].Count));

        Assert.AreEqual(1, Context.JSInterop.Invocations[Toggle].Count);
        Assert.AreEqual(2, Context.JSInterop.Invocations[FocusItem].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldCloseTheSubmenusOfAMenuMovedToANewPoint()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].Click();

        Assert.AreEqual("true", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));

        // A right-click somewhere else is a fresh menu there.
        await com.InvokeAsync(() => com.Instance.OpenAt(30, 40));

        Assert.AreEqual("false", com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[1].GetAttribute("aria-expanded"));
        Assert.IsTrue(com.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldMoveAMenuOpenedFromTheButtonToAPoint()
    {
        var com = Render();

        com.Find(".bit-mnb-opb").Click();

        Assert.AreEqual(com.Find(".bit-mnb").Id, GetComponentId(Context.JSInterop.Invocations[Toggle][0]));

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        // The menu was placed against the button, which a reposition would keep it on: it is placed again,
        // against the point.
        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations[Toggle].Count));

        var toggle = Context.JSInterop.Invocations[Toggle][1];

        Assert.AreEqual(com.Find(".bit-mnb-pnt").Id, GetComponentId(toggle));
        Assert.AreEqual(true, toggle.Arguments[6]);
        Assert.IsTrue(com.Instance.IsOpen);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldGiveThePlacementBackToTheButtonOnceClosed()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.Find(".bit-mnb-ovl").Click();

        Assert.IsFalse(com.Instance.IsOpen);

        com.Find(".bit-mnb-opb").Click();

        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);
        Assert.AreEqual(com.Find(".bit-mnb").Id, GetComponentId(Context.JSInterop.Invocations[Toggle][^1]));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldGiveThePlacementBackToTheButtonWhenDismissedFromOutside()
    {
        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        // Another callout taking over, or a click on the page.
        await com.InvokeAsync(() => com.Instance.CloseCalloutBeforeAnotherCalloutIsOpened());

        com.Find(".bit-mnb-opb").Click();

        Assert.AreEqual(com.Find(".bit-mnb").Id, GetComponentId(Context.JSInterop.Invocations[Toggle][^1]));
    }

    [TestMethod,
        DataRow("Escape"),
        DataRow("Tab")]
    public async Task BitMenuButtonShouldHandTheFocusBackToWhereItWasWhenAMenuOpenedAtAPointCloses(string key)
    {
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(RestoreOrigin, _ => true).SetResult(true);

        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.Find(".bit-mnb-cal").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.IsFalse(com.Instance.IsOpen);

        // The origin took the focus back, so the trigger the user never went near is left alone.
        var restore = Context.JSInterop.Invocations[RestoreOrigin].Single();

        Assert.AreEqual(com.Find(".bit-mnb-cal").Id, restore.Arguments[0]);
        Assert.AreEqual(0, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldHandTheFocusBackToWhereItWasWhenAnItemOfAMenuOpenedAtAPointIsClicked()
    {
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(RestoreOrigin, _ => true).SetResult(true);

        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm")[0].Click();

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(1, Context.JSInterop.Invocations[RestoreOrigin].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldHandTheFocusBackToWhereItWasWhenTheOverlayOfAMenuOpenedAtAPointIsClicked()
    {
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(RestoreOrigin, _ => true).SetResult(true);

        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.Find(".bit-mnb-ovl").Click();

        Assert.AreEqual(1, Context.JSInterop.Invocations[RestoreOrigin].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)]
    public async Task BitMenuButtonShouldFocusTheTriggerWhenTheOriginCannotTakeTheFocusBack(bool captured)
    {
        // Not captured: the focus was on the body when the menu opened. Captured but not restored: the element
        // that had it has left the page since.
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(captured);
        Context.JSInterop.Setup<bool>(RestoreOrigin, _ => true).SetResult(false);

        var com = Render(split: true);

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        com.Find(".bit-mnb-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(captured ? 1 : 0, Context.JSInterop.Invocations[RestoreOrigin].Count);

        // The focus is not left in the menu that has just been hidden: the trigger takes it.
        Assert.AreEqual(1, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotHandTheFocusToAnOriginLeftOverFromAMenuDismissedFromOutside()
    {
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(RestoreOrigin, _ => true).SetResult(true);

        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        // Dismissed by a click on the page: nothing hands the focus anywhere, so the origin is never taken back.
        await com.InvokeAsync(() => com.Instance.CloseCalloutBeforeAnotherCalloutIsOpened());

        // A menu opened from the button closes back to the button.
        com.Find(".bit-mnb-opb").Click();
        com.Find(".bit-mnb-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, Context.JSInterop.Invocations[RestoreOrigin].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotOpenAtAPointWhenDisabled()
    {
        var com = Render(disabled: true);

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[Toggle].Count);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldNotOpenAtAPointWhenItsOpenStateIsBoundOneWay()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.IsOpen, false);
        });

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        // The page owns the open state and was not asked, so the point is not left behind for a later open.
        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[Toggle].Count);

        // When the page does open it, the menu is placed against the button.
        com.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(0, com.FindAll(".bit-mnb-pnt").Count);
        Assert.AreEqual(com.Find(".bit-mnb").Id, GetComponentId(Context.JSInterop.Invocations[Toggle].Single()));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public async Task BitMenuButtonShouldLetGoOfAnOriginNeverHandedBackWhenItIsDisposed(bool captured)
    {
        Context.JSInterop.Setup<bool>(CaptureOrigin, _ => true).SetResult(captured);

        var com = Render();

        await com.InvokeAsync(() => com.Instance.OpenAt(10, 20));

        var calloutId = com.Find(".bit-mnb-cal").Id;

        await Context.DisposeComponentsAsync();

        // An origin left in the script would keep its element alive for as long as the page lives.
        var disposals = Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusOrigin"];

        Assert.AreEqual(captured ? 1 : 0, disposals.Count);

        if (captured)
        {
            Assert.AreEqual(calloutId, disposals[0].Arguments[0]);
        }
    }
}
