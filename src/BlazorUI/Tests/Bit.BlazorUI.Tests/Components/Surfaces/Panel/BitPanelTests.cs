using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

[TestClass]
public class BitPanelTests : BunitTestContext
{
    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);



    [TestMethod]
    public void BitPanelShouldRenderRootAndParts()
    {
        var com = RenderComponent<BitPanel>();

        Assert.IsNotNull(com.Find(".bit-pnl"));
        // The overlay stays in the page while the panel is closed so that it can fade out with it.
        Assert.IsNotNull(com.Find(".bit-pnl-ovl"));
        Assert.IsNotNull(com.Find(".bit-pnl-cnt"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var container = com.Find(".bit-pnl-cnt");
        var overlay = com.Find(".bit-pnl-ovl");

        Assert.AreEqual(isOpen, container.ClassList.Contains("bit-pnl-opn"));
        Assert.AreEqual(isOpen, overlay.ClassList.Contains("bit-pnl-ovl-opn"));
    }

    // A closed panel keeps its content in the page so that closing it has something to slide out, so the
    // content has to be taken out of the tab order and out of the accessibility tree some other way.
    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelShouldMarkTheClosedPanelInert(bool isOpen)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var container = com.Find(".bit-pnl-cnt");

        Assert.AreEqual(isOpen is false, container.HasAttribute("inert"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelModelessTest(bool modeless)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, modeless);
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(modeless ? 0 : 1, com.FindAll(".bit-pnl-ovl").Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelBlockingTest(bool blocking)
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Blocking, blocking);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl-ovl").Click();

        Assert.AreEqual(blocking, isOpen);
        Assert.AreEqual(blocking, com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-opn"));
    }

    [TestMethod]
    public void BitPanelCloseWhenClickOutOfPanelTest()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-opn"));

        com.Find(".bit-pnl-ovl").Click();

        Assert.IsFalse(isOpen);
        Assert.IsFalse(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-opn"));
    }

    [TestMethod]
    public void BitPanelContentTest()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        Assert.AreEqual("Test Content", com.Find(".bit-pnl-cnt").TextContent);
    }

    [TestMethod]
    public void BitPanelKeepMountedShouldKeepTheContentInThePageOnceItHasBeenOpened()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"kept\">Kept Content</div>");
        });

        // Nothing of it is rendered until the first opening either way.
        Assert.AreEqual(0, com.FindAll(".kept").Count);

        com.Render(p => p.Add(x => x.IsOpen, true));
        Assert.AreEqual(1, com.FindAll(".kept").Count);

        // Once rendered it stays, so the state the content holds survives the panel closing.
        com.Render(p => p.Add(x => x.IsOpen, false));
        Assert.AreEqual(1, com.FindAll(".kept").Count);
    }

    // A panel builds its content when it opens, so one that has never been opened costs nothing at all.
    [TestMethod]
    public void BitPanelShouldNotRenderItsContentBeforeTheFirstOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"body\">Body</div>");
        });

        Assert.AreEqual(0, com.FindAll(".body").Count);
    }

    [TestMethod,
        DataRow(BitSide.End),
        DataRow(BitSide.Start),
        DataRow(BitSide.Top),
        DataRow(BitSide.Bottom),
        DataRow(null)
    ]
    public void BitPanelPositionTest(BitSide? position)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            if (position.HasValue)
            {
                parameters.Add(p => p.Position, position.Value);
            }
        });

        var positionClass = position switch
        {
            BitSide.End => "bit-pnl-end",
            BitSide.Start => "bit-pnl-start",
            BitSide.Top => "bit-pnl-top",
            BitSide.Bottom => "bit-pnl-bottom",
            _ => "bit-pnl-end",
        };

        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains(positionClass));
    }

    [TestMethod,
        DataRow(BitSide.Start, "width"),
        DataRow(BitSide.End, "width"),
        DataRow(BitSide.Top, "height"),
        DataRow(BitSide.Bottom, "height")
    ]
    public void BitPanelSizeShouldFollowTheAxisThePanelSlidesOn(BitSide position, string property)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
            parameters.Add(p => p.Size, 320);
        });

        StringAssert.Contains(com.Find(".bit-pnl-cnt").GetAttribute("style"), $"{property}:320px");
    }

    [TestMethod]
    public void BitPanelFullSizeShouldReplaceTheSize()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullSize, true);
            parameters.Add(p => p.Size, 320);
        });

        var container = com.Find(".bit-pnl-cnt");

        Assert.IsTrue(container.ClassList.Contains("bit-pnl-fsz"));
        Assert.IsFalse((container.GetAttribute("style") ?? string.Empty).Contains("320px"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelAbsolutePositionTest(bool absolute)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, absolute);
        });

        Assert.AreEqual(absolute, com.Find(".bit-pnl").ClassList.Contains("bit-pnl-abs"));
    }

    [TestMethod]
    public void BitPanelShouldRenderTheAccessibilityContract()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Settings");
        });

        var container = com.Find(".bit-pnl-cnt");

        Assert.AreEqual("dialog", container.GetAttribute("role"));
        Assert.AreEqual("Settings", container.GetAttribute("aria-label"));
        Assert.AreEqual("true", container.GetAttribute("aria-modal"));
        // A panel that holds nothing focusable still needs somewhere to put the keyboard.
        Assert.AreEqual("-1", container.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPanelShouldReportTheAlertDialogRole()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsAlert, true);
        });

        Assert.AreEqual("alertdialog", com.Find(".bit-pnl-cnt").GetAttribute("role"));
    }

    // A panel that leaves the page usable is not a modal one, and saying so would tell a screen reader
    // something the user can prove wrong by clicking. Neither is a panel that is not open at all.
    [TestMethod]
    public void BitPanelShouldNotReportAModelessOrClosedPanelAsModal()
    {
        var modeless = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });
        Assert.IsNull(modeless.Find(".bit-pnl-cnt").GetAttribute("aria-modal"));

        var closed = RenderComponent<BitPanel>();
        Assert.IsNull(closed.Find(".bit-pnl-cnt").GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitPanelShouldPointAtTheElementsThatNameAndDescribeIt()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TitleAriaId, "the-title");
            parameters.Add(p => p.SubtitleAriaId, "the-subtitle");
        });

        var container = com.Find(".bit-pnl-cnt");

        Assert.AreEqual("the-title", container.GetAttribute("aria-labelledby"));
        Assert.AreEqual("the-subtitle", container.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitPanelShouldDismissOnEscape()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldNotDismissOnEscapeWhenAskedNotTo()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldIgnoreOtherKeys()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldNotDismissWhenDisabled()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl-ovl").Click();
        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.IsTrue(com.Find(".bit-pnl").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitPanelOnDismissShouldWorkCorrect()
    {
        var isOpen = true;
        var currentCount = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, newValue => isOpen = newValue);
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        com.Find(".bit-pnl-ovl").Click();

        Assert.IsFalse(isOpen);
        com.WaitForAssertion(() => Assert.AreEqual(1, currentCount));
    }

    // Every way the panel can be closed reaches the same callback, so a consumer that cleans up in it never
    // has to also watch the flag it bound.
    [TestMethod]
    public async Task BitPanelOnDismissShouldBeCalledForEveryWayOfClosing()
    {
        var isOpen = true;
        var dismissed = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, () => dismissed++);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        com.WaitForAssertion(() => Assert.AreEqual(1, dismissed));

        com.Render(p => p.Add(x => x.IsOpen, true));
        await com.InvokeAsync(() => com.Instance.Close());
        com.WaitForAssertion(() => Assert.AreEqual(2, dismissed));

        com.Render(p => p.Add(x => x.IsOpen, true));
        com.Render(p => p.Add(x => x.IsOpen, false));
        com.WaitForAssertion(() => Assert.AreEqual(3, dismissed));
    }

    [TestMethod]
    public void BitPanelOnOverlayClickShouldBeCalledEvenWhenBlocking()
    {
        var isOpen = true;
        var clicked = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Blocking, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOverlayClick, () => clicked++);
        });

        com.Find(".bit-pnl-ovl").Click();

        com.WaitForAssertion(() => Assert.AreEqual(1, clicked));
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitPanelOnOpenAndOnToggleShouldWorkCorrect()
    {
        var isOpen = false;
        var opened = 0;
        var toggles = new System.Collections.Generic.List<bool>();

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnToggle, (bool v) => toggles.Add(v));
        });

        com.Render(p => p.Add(x => x.IsOpen, true));
        com.WaitForAssertion(() => Assert.AreEqual(1, opened));

        com.Render(p => p.Add(x => x.IsOpen, false));
        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { true, false }, toggles));
    }

    [TestMethod]
    public async Task BitPanelOpenCloseAndToggleShouldWorkCorrect()
    {
        var isOpen = false;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await com.InvokeAsync(() => com.Instance.Open());
        Assert.IsTrue(isOpen);

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.IsFalse(isOpen);

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.IsTrue(isOpen);

        await com.InvokeAsync(() => com.Instance.Close());
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public async Task BitPanelOpenShouldDoNothingWhenDisabled()
    {
        var isOpen = false;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await com.InvokeAsync(() => com.Instance.Open());

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldTakeTheScrollbarOffThePageWhileItIsOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            // A panel pointed at no scroller of its own names none: the script resolves that to the page.
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.IsNull(locked.Arguments[1]);
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
        });
    }

    [TestMethod]
    public void BitPanelShouldTakeTheScrollbarOffTheNamedScroller()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual(".scroller", locked.Arguments[1]);
        });
    }

    [TestMethod]
    public void BitPanelShouldNotTouchThePageScrollingWhenNotAskedTo()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoScrollLock, true);
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    // A modeless panel leaves the page usable on purpose, so it never holds it.
    [TestMethod]
    public void BitPanelModelessShouldNotHoldThePage()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));
    }

    // A panel out of view carries none of the behaviours that only make sense for one the user can see.
    [TestMethod]
    public void BitPanelShouldNotHoldThePageWhileItIsCollapsed()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));
    }

    // AutoToggleScroll is the panel holding the scroller itself, so the hold it would else take is stood
    // down for it - the two would otherwise both be holding the same page.
    [TestMethod]
    public void BitPanelAutoToggleScrollShouldTakeTheOverflowRatherThanTheHold()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var toggled = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
            Assert.AreEqual("body", toggled.Arguments[1]);
            Assert.AreEqual(true, toggled.Arguments[2]);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitPanelAutoToggleScrollShouldHandTheOverflowBackWhenItCloses()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            var toggled = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
            Assert.AreEqual(false, toggled.Arguments[2]);
        });
    }

    // A panel that covers the page without holding it hands the gestures that land on it to the scroller
    // behind it: the layer they land on is fixed to the viewport, so the browser would else chain them to a
    // document that does not scroll.
    [TestMethod]
    public void BitPanelShouldForwardTheGesturesOfAPanelThatHoldsNothing()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoScrollLock, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var forwarded = Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"][^1];
            Assert.AreEqual(".scroller", forwarded.Arguments[2]);
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.stopForwardScroll"].Count));
    }

    // One holding the page has nothing to forward, and one aimed at no scroller of its own cannot use it:
    // the page is what the browser already chains to.
    [TestMethod]
    public void BitPanelShouldNotForwardTheGesturesOfAPanelThatHoldsThePage()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"].Count);
    }

    // An absolutely positioned panel covers a container of the page rather than the page, so the page it
    // never covered is not its to take the scrollbar off.
    [TestMethod]
    public void BitPanelShouldNotTakeTheScrollbarOffThePageForAnAbsolutelyPositionedPanel()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));
    }

    // A scroller it was pointed at by hand is still held: that is how a contained panel holds the container
    // it is laid out in.
    [TestMethod]
    public void BitPanelShouldTakeTheScrollbarOffTheNamedScrollerOfAnAbsolutelyPositionedPanel()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual(".scroller", locked.Arguments[1]);
        });
    }

    // Its layer is laid out inside the container rather than fixed to the viewport, so its gestures already
    // reach what scrolls behind it and forwarding them would move that scroller twice over.
    [TestMethod]
    public void BitPanelShouldNotForwardTheGesturesOfAnAbsolutelyPositionedPanel()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"].Count));
    }

    [TestMethod]
    public void BitPanelShouldNotForwardTheGesturesOfAPanelAimedAtNoScrollerOfItsOwn()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoScrollLock, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"].Count));
    }

    // A panel taken off the page while it was open would otherwise leave the page without its scrollbar.
    [TestMethod]
    public void BitPanelShouldGiveTheScrollbarBackWhenItIsDisposedWhileOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        var released = Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"][^1];
        Assert.AreEqual(Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[0], released.Arguments[0]);
    }

    [TestMethod]
    public void BitPanelShouldTrapTheFocusOfAModalPanelAndMoveTheFocusIntoIt()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocusOrigin"].Count);
        });
    }

    [TestMethod]
    public void BitPanelShouldNotTrapTheFocusOfAModelessPanel()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, true);
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        // The page behind a modeless panel is meant to be reached, so the keyboard is left free to go there.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitPanelNoFocusTrapAndNoAutoFocusShouldLeaveTheKeyboardAlone()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoAutoFocus, true);
            parameters.Add(p => p.NoFocusTrap, true);
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
    }

    // The trap is registered against the open panel, so turning it off while the panel is open has to reach
    // the registration rather than wait for the next time the panel opens.
    [TestMethod]
    public void BitPanelShouldReleaseTheFocusTrapWhenItIsTurnedOffWhileOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        com.Render(p => p.Add(x => x.NoFocusTrap, true));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count));
    }

    [TestMethod]
    public void BitPanelShouldRegisterTheSwipeGestureWithTheGeometryOfThePanel()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Position, BitSide.Bottom);
            parameters.Add(p => p.SwipeTrigger, 0.5m);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments;

        Assert.AreEqual(0.5m, arguments[1]);
        Assert.AreEqual(BitSide.Bottom, arguments[2]);
        Assert.AreEqual(false, arguments[3]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, arguments[4]);
    }

    // Every input of the geometry the gesture was registered with is a parameter that can change at runtime.
    [TestMethod]
    public void BitPanelShouldRegisterTheSwipeGestureAgainWhenTheGeometryChanges()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Position, BitSide.End);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        com.Render(p => p.Add(x => x.Position, BitSide.Top));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.dispose"].Count);
            Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
        });

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments;

        Assert.AreEqual(BitSide.Top, arguments[2]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, arguments[4]);
    }

    // A fraction of the size of the panel that is zero or negative would dismiss the panel on the first pixel
    // of a drag, and one above the whole of it would never dismiss the panel at all.
    [TestMethod,
        DataRow(null, 0.25),
        DataRow(0.0, 0.25),
        DataRow(-0.5, 0.25),
        DataRow(1.5, 0.25),
        DataRow(0.4, 0.4)
    ]
    public void BitPanelShouldClampTheSwipeTrigger(double? trigger, double expected)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            if (trigger.HasValue)
            {
                parameters.Add(p => p.SwipeTrigger, (decimal)trigger.Value);
            }
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        Assert.AreEqual((decimal)expected, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitPanelNoSwipeShouldRegisterNoGesture()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoSwipe, true);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
    }

    [TestMethod]
    public void BitPanelShouldRenderTheClassesAndStylesOfItsParts()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitPanelClassStyles
            {
                Root = "custom-root",
                Overlay = "custom-overlay",
                Container = "custom-container"
            });
            parameters.Add(p => p.Styles, new BitPanelClassStyles
            {
                Root = "color:red",
                Overlay = "background-color:blue",
                Container = "padding:1rem"
            });
        });

        Assert.IsTrue(com.Find(".bit-pnl").ClassList.Contains("custom-root"));
        Assert.IsTrue(com.Find(".bit-pnl-ovl").ClassList.Contains("custom-overlay"));
        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains("custom-container"));

        StringAssert.Contains(com.Find(".bit-pnl").GetAttribute("style"), "color:red");
        StringAssert.Contains(com.Find(".bit-pnl-ovl").GetAttribute("style"), "background-color:blue");
        StringAssert.Contains(com.Find(".bit-pnl-cnt").GetAttribute("style"), "padding:1rem");
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelModeFullShouldGiveTheOverlayABackgroundOfItsOwn(bool modeFull)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ModeFull, modeFull);
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(modeFull, com.Find(".bit-pnl-ovl").ClassList.Contains("bit-pnl-ovl-mfl"));
    }

    // A panel that is not a dialog at all - one left beside the page rather than over it - is better
    // announced as something the user can walk past.
    [TestMethod]
    public void BitPanelRoleShouldTakeOverFromTheDialogItIsAnnouncedAs()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, true);
            parameters.Add(p => p.Role, "complementary");
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("complementary", com.Find(".bit-pnl-cnt").GetAttribute("role"));

        // It wins over the alert dialog of an IsAlert panel too.
        com.Render(p => p.Add(x => x.IsAlert, true));
        Assert.AreEqual("complementary", com.Find(".bit-pnl-cnt").GetAttribute("role"));
    }

    // aria-modal belongs to the two dialog roles and to nothing else, so a panel announced as something the
    // user can walk past never carries it.
    [TestMethod]
    public void BitPanelShouldNotReportAModalOnARoleThatIsNotADialog()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Role, "complementary");
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsFalse(com.Find(".bit-pnl-cnt").HasAttribute("aria-modal"));

        com.Render(p => p.Add(x => x.Role, "alertdialog"));
        Assert.AreEqual("true", com.Find(".bit-pnl-cnt").GetAttribute("aria-modal"));
    }

    // The panel and the overlay it comes with are lifted as a pair, and the panel stays above the overlay.
    [TestMethod]
    public void BitPanelZIndexShouldLiftThePanelAndItsOverlayTogether()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ZIndex, 1310);
        });

        var style = com.Find(".bit-pnl").GetAttribute("style");

        StringAssert.Contains(style, "--bit-pnl-zin-ovl:1310");
        StringAssert.Contains(style, "--bit-pnl-zin-cnt:1311");

        com.Render(p => p.Add(x => x.ZIndex, null));

        Assert.IsFalse((com.Find(".bit-pnl").GetAttribute("style") ?? string.Empty).Contains("--bit-pnl-zin"));
    }

    [TestMethod]
    public void BitPanelOnDismissingShouldTellTheOverlayFromTheEscapeKey()
    {
        var isOpen = true;
        var reasons = new System.Collections.Generic.List<BitPanelDismissReason>();

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitPanelDismissArgs args) => reasons.Add(args.Reason));
        });

        com.Find(".bit-pnl-ovl").Click();
        com.Render(p => p.Add(x => x.IsOpen, true));

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        CollectionAssert.AreEqual(
            new[] { BitPanelDismissReason.Overlay, BitPanelDismissReason.Escape },
            reasons);
    }

    [TestMethod]
    public async Task BitPanelOnDismissingShouldReportTheSwipeAndTheCloseMethod()
    {
        var isOpen = true;
        var reasons = new System.Collections.Generic.List<BitPanelDismissReason>();

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitPanelDismissArgs args) => reasons.Add(args.Reason));
        });

        await com.InvokeAsync(() => com.Instance._OnClose());
        com.Render(p => p.Add(x => x.IsOpen, true));

        await com.InvokeAsync(() => com.Instance.Close());

        CollectionAssert.AreEqual(
            new[] { BitPanelDismissReason.Swipe, BitPanelDismissReason.Programmatic },
            reasons);
    }

    // The click that dismissed the panel is handed to the callback, so a consumer never has to reach for the
    // pointer through the reason.
    [TestMethod]
    public void BitPanelOnDismissingShouldCarryTheClickOfAnOverlayDismissal()
    {
        var isOpen = true;
        BitPanelDismissArgs? received = null;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismissing, (BitPanelDismissArgs args) => received = args);
        });

        com.Find(".bit-pnl-ovl").Click(new MouseEventArgs { ClientX = 12, ClientY = 34 });

        Assert.IsNotNull(received);
        Assert.AreEqual(BitPanelDismissReason.Overlay, received.Reason);
        Assert.IsNotNull(received.Mouse);
        Assert.AreEqual(12, received.Mouse.ClientX);

        // Nothing but a pointer dismissal has a click to carry.
        com.Render(p => p.Add(x => x.IsOpen, true));
        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(BitPanelDismissReason.Escape, received.Reason);
        Assert.IsNull(received.Mouse);
    }

    [TestMethod]
    public async Task BitPanelOnDismissingShouldBeAbleToRefuseTheDismissal()
    {
        var isOpen = true;
        var dismissed = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, () => dismissed++);
            parameters.Add(p => p.OnDismissing, (BitPanelDismissArgs args) => args.Cancel = true);
        });

        com.Find(".bit-pnl-ovl").Click();
        Assert.IsTrue(isOpen);

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsTrue(isOpen);

        await com.InvokeAsync(() => com.Instance.Close());
        Assert.IsTrue(isOpen);

        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-opn"));
        // A dismissal that never happened is never reported.
        Assert.AreEqual(0, dismissed);
    }

    // The flag has already been set by the time the panel sees it, so there is nothing left to refuse.
    [TestMethod]
    public void BitPanelOnDismissingShouldNotSeeTheFlagBeingSetFromTheOutside()
    {
        var called = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnDismissing, (BitPanelDismissArgs args) => { called++; args.Cancel = true; });
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        Assert.AreEqual(0, called);
        Assert.IsFalse(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-opn"));
    }

    // A disabled panel takes nothing from the user, but the code that owns it can always close it.
    [TestMethod]
    public async Task BitPanelCloseShouldStillWorkOnADisabledPanel()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await com.InvokeAsync(() => com.Instance.Close());

        Assert.IsFalse(isOpen);
    }

    // The lock is named after the panel that holds it, so an element several panels are holding still at once
    // gets its scrolling back when the last of them lets go rather than when the first one does.
    [TestMethod]
    public void BitPanelShouldNameItselfAsTheHolderOfTheScrollLock()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual(2, locked.Arguments.Count);
            Assert.IsNotNull(locked.Arguments[0]);
            StringAssert.Contains(locked.Arguments[0]!.ToString(), "container");
        });
    }

    // The scrollbar was taken off the element the selector named when the panel opened, so a selector that has
    // changed since has to be followed.
    [TestMethod]
    public void BitPanelShouldFollowTheScrollerSelectorWhileItIsOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".first");
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(p => p.Add(x => x.ScrollerSelector, ".second"));

        com.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"];

            Assert.IsTrue(invocations.Any(i => Equals(i.Arguments[1], ".first")));
            Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);

            var locked = invocations[^1];
            Assert.AreEqual(".second", locked.Arguments[1]);
        });
    }

    // The end of the movement is only known to the page, so the panel is told about it from there.
    [TestMethod]
    public async Task BitPanelOnTransitionEndShouldReportTheStateThePanelSettledIn()
    {
        var settled = new System.Collections.Generic.List<bool>();

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.OnTransitionEnd, (bool v) => settled.Add(v));
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupTransitionEnd"].Count));

        com.Render(p => p.Add(x => x.IsOpen, true));
        await com.InvokeAsync(() => com.Instance._OnTransitionEnd());

        com.Render(p => p.Add(x => x.IsOpen, false));
        await com.InvokeAsync(() => com.Instance._OnTransitionEnd());

        CollectionAssert.AreEqual(new[] { true, false }, settled);
    }

    // The content is only taken out once the panel has finished sliding away with it, so the closing is still
    // seen with something in it.
    [TestMethod]
    public async Task BitPanelShouldKeepTheContentUntilThePanelHasSlidAway()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"body\">Body</div>");
        });

        // Nothing of it is rendered until the first opening.
        Assert.AreEqual(0, com.FindAll(".body").Count);

        com.Render(p => p.Add(x => x.IsOpen, true));
        Assert.AreEqual(1, com.FindAll(".body").Count);

        com.Render(p => p.Add(x => x.IsOpen, false));
        Assert.AreEqual(1, com.FindAll(".body").Count);

        await com.InvokeAsync(() => com.Instance._OnTransitionEnd());
        Assert.AreEqual(0, com.FindAll(".body").Count);

        // And the next opening builds it again.
        com.Render(p => p.Add(x => x.IsOpen, true));
        Assert.AreEqual(1, com.FindAll(".body").Count);
    }

    // A panel that keeps its content keeps it whatever the page reports about the movement.
    [TestMethod]
    public async Task BitPanelKeepMountedShouldKeepTheContentAfterTheTransition()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div class=\"body\">Body</div>");
        });

        com.Render(p => p.Add(x => x.IsOpen, false));
        await com.InvokeAsync(() => com.Instance._OnTransitionEnd());

        Assert.AreEqual(1, com.FindAll(".body").Count);
    }

    [TestMethod]
    public void BitPanelRtlShouldFlipTheTransformFactor()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.IsTrue(com.Find(".bit-pnl").ClassList.Contains("bit-rtl"));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        // The gesture has to know the direction too, since the edge the panel slides in from is a logical one.
        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments[3]);
    }

    // A panel given none of the header, the footer or the close button is the plain surface it has always
    // been: nothing is laid out around its content and the panel itself keeps the scrolling.
    [TestMethod]
    public void BitPanelShouldBuildNoChromeWhenItWasGivenNone()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        Assert.AreEqual(0, com.FindAll(".bit-pnl-hcn").Count);
        Assert.AreEqual(0, com.FindAll(".bit-pnl-bdy").Count);
        Assert.AreEqual(0, com.FindAll(".bit-pnl-ftr").Count);
        Assert.IsFalse(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-sec"));
    }

    [TestMethod,
        DataRow("HeaderText"),
        DataRow("FooterText"),
        DataRow("ShowCloseButton")
    ]
    public void BitPanelShouldBuildTheChromeAroundABodyOfItsOwn(string parameter)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);

            if (parameter is "HeaderText") parameters.Add(p => p.HeaderText, "Header Text");
            if (parameter is "FooterText") parameters.Add(p => p.FooterText, "Footer Text");
            if (parameter is "ShowCloseButton") parameters.Add(p => p.ShowCloseButton, true);

            parameters.AddChildContent("<div>Test Content</div>");
        });

        com.Find(".bit-pnl-bdy").MarkupMatches("<div class=\"bit-pnl-bdy\"><div>Test Content</div></div>");

        // The scrolling moves from the panel to the body, so the parts around it stay put while it scrolls.
        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-sec"));
    }

    [TestMethod]
    public void BitPanelBodyAliasTest()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.Body, Markup("<div>Aliased Body</div>"));
        });

        Assert.AreEqual("Aliased Body", com.Find(".bit-pnl-bdy").TextContent);
    }

    [TestMethod]
    public void BitPanelHeaderContentTest()
    {
        const string headerContent = "<div>Test Header Content</div>";

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, headerContent);
        });

        com.Find(".bit-pnl-hcn :first-child :first-child").MarkupMatches(headerContent);
    }

    [TestMethod]
    public void BitPanelFooterContentTest()
    {
        const string footerContent = "<div>Test Footer Content</div>";

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Footer, footerContent);
        });

        com.Find(".bit-pnl-ftr :first-child").MarkupMatches(footerContent);
    }

    [TestMethod]
    public void BitPanelShouldRenderHeaderTextAndCloseButton()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.IsNotNull(com.Find(".bit-pnl-cls"));
        Assert.AreEqual("Header Text", com.Find(".bit-pnl-hdr").TextContent);
    }

    // The template is the richer of the two, so it is what a panel given both renders.
    [TestMethod]
    public void BitPanelHeaderAndFooterTemplatesShouldWinOverTheirText()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, "<div>Header Template</div>");
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.Footer, "<div>Footer Template</div>");
            parameters.Add(p => p.FooterText, "Footer Text");
        });

        Assert.AreEqual("Header Template", com.Find(".bit-pnl-hdr").TextContent);
        Assert.AreEqual("Footer Template", com.Find(".bit-pnl-ftr").TextContent);
    }

    [TestMethod]
    public void BitPanelShouldRenderFooterTextWhenFooterTemplateMissing()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FooterText, "Footer Text");
        });

        Assert.AreEqual("Footer Text", com.Find(".bit-pnl-ftr").TextContent);
    }

    [TestMethod]
    public void BitPanelShouldDismissOnTheCloseButton()
    {
        var dismissed = 0;
        var isOpen = true;
        BitPanelDismissReason? reason = null;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.OnDismissing, EventCallback.Factory.Create<BitPanelDismissArgs>(this, args => reason = args.Reason));
        });

        com.Find(".bit-pnl-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            // The dismissal is reported once, by the render that closed the panel, however it was closed.
            Assert.AreEqual(1, dismissed);
            // The close button is a dismissal of its own, so a panel that refuses the gestures which could be
            // a slip can still let through the button the user aimed at.
            Assert.AreEqual(BitPanelDismissReason.CloseButton, reason);
        });
    }

    [TestMethod]
    public void BitPanelOnDismissingShouldBeAbleToRefuseTheCloseButton()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnDismissing, EventCallback.Factory.Create<BitPanelDismissArgs>(this, args => args.Cancel = true));
        });

        com.Find(".bit-pnl-cls").Click();

        com.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    [TestMethod]
    public void BitPanelCloseButtonShouldDoNothingWhenDisabled()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        var button = com.Find(".bit-pnl-cls");

        Assert.IsTrue(button.HasAttribute("disabled"));

        button.Click();

        com.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    // A dialog needs an accessible name, and a panel that renders a header of its own is already showing the
    // name it should be given.
    [TestMethod]
    public void BitPanelShouldNameTheDialogByItsHeader()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
        });

        Assert.AreEqual(com.Find(".bit-pnl-hdr").Id, com.Find(".bit-pnl-cnt").GetAttribute("aria-labelledby"));

        // A name set by hand wins over the header the panel is already showing.
        com.Render(p => p.Add(x => x.TitleAriaId, "named-by-hand"));
        Assert.AreEqual("named-by-hand", com.Find(".bit-pnl-cnt").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitPanelAriaLabelShouldWinOverTheHeader()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.AriaLabel, "Named by hand");
        });

        var container = com.Find(".bit-pnl-cnt");

        Assert.IsNull(container.GetAttribute("aria-labelledby"));
        Assert.AreEqual("Named by hand", container.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPanelShouldNameTheCloseButton()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.AreEqual("Close", com.Find(".bit-pnl-cls").GetAttribute("aria-label"));
        Assert.AreEqual("Close", com.Find(".bit-pnl-cls").GetAttribute("title"));

        com.Render(p => p.Add(x => x.CloseButtonTitle, "Dismiss"));

        Assert.AreEqual("Dismiss", com.Find(".bit-pnl-cls").GetAttribute("aria-label"));
        Assert.AreEqual("Dismiss", com.Find(".bit-pnl-cls").GetAttribute("title"));
    }

    [TestMethod]
    public void BitPanelCloseIconShouldTakeOverFromTheBuiltInOne()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.IsTrue(com.Find(".bit-pnl-cli").ClassList.Contains("bit-icon--Cancel"));

        com.Render(p => p.Add(x => x.CloseIconName, "ChromeClose"));
        Assert.IsTrue(com.Find(".bit-pnl-cli").ClassList.Contains("bit-icon--ChromeClose"));

        // The external icon takes precedence over the built-in one named beside it.
        com.Render(p => p.Add(x => x.CloseIcon, BitIconInfo.Fa("solid xmark")));
        Assert.IsFalse(com.Find(".bit-pnl-cli").ClassList.Contains("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public void BitPanelShouldRenderTheClassesAndStylesOfItsSections()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.FooterText, "Footer Text");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Classes, new BitPanelClassStyles
            {
                HeaderContainer = "custom-header-container",
                Header = "custom-header",
                CloseButton = "custom-close-button",
                CloseIcon = "custom-close-icon",
                Body = "custom-body",
                Footer = "custom-footer"
            });
            parameters.Add(p => p.Styles, new BitPanelClassStyles
            {
                HeaderContainer = "color:red",
                Header = "color:blue",
                CloseButton = "color:green",
                CloseIcon = "color:purple",
                Body = "padding:1rem",
                Footer = "margin:1rem"
            });
        });

        Assert.IsTrue(com.Find(".bit-pnl-hcn").ClassList.Contains("custom-header-container"));
        Assert.IsTrue(com.Find(".bit-pnl-hdr").ClassList.Contains("custom-header"));
        Assert.IsTrue(com.Find(".bit-pnl-cls").ClassList.Contains("custom-close-button"));
        Assert.IsTrue(com.Find(".bit-pnl-cli").ClassList.Contains("custom-close-icon"));
        Assert.IsTrue(com.Find(".bit-pnl-bdy").ClassList.Contains("custom-body"));
        Assert.IsTrue(com.Find(".bit-pnl-ftr").ClassList.Contains("custom-footer"));

        StringAssert.Contains(com.Find(".bit-pnl-hcn").GetAttribute("style"), "color:red");
        StringAssert.Contains(com.Find(".bit-pnl-hdr").GetAttribute("style"), "color:blue");
        StringAssert.Contains(com.Find(".bit-pnl-cls").GetAttribute("style"), "color:green");
        StringAssert.Contains(com.Find(".bit-pnl-cli").GetAttribute("style"), "color:purple");
        StringAssert.Contains(com.Find(".bit-pnl-bdy").GetAttribute("style"), "padding:1rem");
        StringAssert.Contains(com.Find(".bit-pnl-ftr").GetAttribute("style"), "margin:1rem");
    }

    // An application shell scrolls a region of its own, so the body of such an app never scrolls and a hold
    // taken on it would hold nothing. BitAppShell cascades its scroller under this name, and a panel that
    // has not been pointed at a scroller of its own holds that one instead of the page.
    [TestMethod]
    public void BitPanelShouldHoldTheScrollerOfTheApplicationShellItIsInside()
    {
        var shell = new ElementReference("shell-container");

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.AddCascadingValue("BitAppShell.Container", (ElementReference?)shell);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual(shell, locked.Arguments[1]);
        });
    }

    // The shell is the fallback, not the answer: a panel told which scroller to hold holds that one.
    [TestMethod]
    public void BitPanelShouldPreferTheScrollerItWasPointedAtOverTheOneOfTheShell()
    {
        var shell = new ElementReference("shell-container");

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.AddCascadingValue("BitAppShell.Container", (ElementReference?)shell);
            parameters.Add(p => p.ScrollerSelector, "#own-scroller");
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual("#own-scroller", locked.Arguments[1]);
        });
    }

    // An element the consumer hands over beats both the selector and the shell.
    [TestMethod]
    public void BitPanelShouldHoldTheScrollerElementItWasGivenOverEverythingElse()
    {
        var shell = new ElementReference("shell-container");
        var own = new ElementReference("own-container");

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.AddCascadingValue("BitAppShell.Container", (ElementReference?)shell);
            parameters.Add(p => p.ScrollerSelector, "#own-scroller");
            parameters.Add(p => p.ScrollerElement, (ElementReference?)own);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1];
            Assert.AreEqual(own, locked.Arguments[1]);
        });
    }

    // The hold is registered against the element the parameters resolved to, so a panel pointed somewhere
    // else while it is open lets the old one go before taking the new one.
    [TestMethod]
    public void BitPanelShouldFollowTheScrollerElementWhileItIsOpen()
    {
        var first = new ElementReference("first-container");
        var second = new ElementReference("second-container");

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.ScrollerElement, (ElementReference?)first);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(p => p.Add(x => x.ScrollerElement, (ElementReference?)second));

        com.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"];

            Assert.IsTrue(invocations.Any(i => Equals(i.Arguments[1], first)));
            Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
            Assert.AreEqual(second, invocations[^1].Arguments[1]);
        });
    }

    // Nothing is handed back by a panel that was told not to hand anything back, so nothing is recorded for
    // it either.
    [TestMethod]
    public void BitPanelShouldNotRememberTheFocusWhenItWillNotHandItBack()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoRestoreFocus, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
    }

    [TestMethod]
    public void BitPanelShouldNotHandTheFocusBackWhenAskedNotTo()
    {
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoRestoreFocus, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() => Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocusOrigin"].Count));
    }

    // The counterpart of OnOverlayClick for the keyboard: the place to react to a dismissal that was refused.
    [TestMethod]
    public void BitPanelShouldReportEveryEscapeEvenTheOnesItRefusesToBeDismissedBy()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldReportTheEscapeItIsDismissedBy()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitPanelShouldNotReportAnyKeyButEscapeAsAnEscape()
    {
        var escapes = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
            parameters.Add(p => p.IsOpen, true);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(0, escapes);
    }

    // A disabled panel takes nothing from the user, the key included.
    [TestMethod]
    public void BitPanelShouldNotReportAnEscapeItIsInNoStateToActOn()
    {
        var escapes = 0;

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.IsOpen, true);
        });

        com.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, escapes);
    }
}
