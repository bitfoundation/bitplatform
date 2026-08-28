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
    public void BitPanelContentAliasTest()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Content, Markup("<div>Aliased Content</div>"));
        });

        Assert.AreEqual("Aliased Content", com.Find(".bit-pnl-cnt").TextContent);
    }

    [TestMethod]
    public void BitPanelLazyRenderShouldKeepTheContentOutUntilTheFirstOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"lazy\">Lazy Content</div>");
        });

        Assert.AreEqual(0, com.FindAll(".lazy").Count);

        com.Render(p => p.Add(x => x.IsOpen, true));
        Assert.AreEqual(1, com.FindAll(".lazy").Count);

        // Once rendered it stays, so the state the content holds survives the panel closing.
        com.Render(p => p.Add(x => x.IsOpen, false));
        Assert.AreEqual(1, com.FindAll(".lazy").Count);
    }

    [TestMethod]
    public void BitPanelWithoutLazyRenderShouldRenderTheContentWhileClosed()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"eager\">Eager Content</div>");
        });

        Assert.AreEqual(1, com.FindAll(".eager").Count);
    }

    [TestMethod,
        DataRow(BitPanelPosition.End),
        DataRow(BitPanelPosition.Start),
        DataRow(BitPanelPosition.Top),
        DataRow(BitPanelPosition.Bottom),
        DataRow(null)
    ]
    public void BitPanelPositionTest(BitPanelPosition? position)
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
            BitPanelPosition.End => "bit-pnl-end",
            BitPanelPosition.Start => "bit-pnl-start",
            BitPanelPosition.Top => "bit-pnl-top",
            BitPanelPosition.Bottom => "bit-pnl-bottom",
            _ => "bit-pnl-end",
        };

        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains(positionClass));
    }

    [TestMethod,
        DataRow(BitPanelPosition.Start, "width"),
        DataRow(BitPanelPosition.End, "width"),
        DataRow(BitPanelPosition.Top, "height"),
        DataRow(BitPanelPosition.Bottom, "height")
    ]
    public void BitPanelSizeShouldFollowTheAxisThePanelSlidesOn(BitPanelPosition position, string property)
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
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
            Assert.AreEqual("body", locked.Arguments[0]);
            Assert.AreEqual(true, locked.Arguments[1]);
        });

        com.Render(p => p.Add(x => x.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            var released = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
            Assert.AreEqual("body", released.Arguments[0]);
            Assert.AreEqual(false, released.Arguments[1]);
        });
    }

    [TestMethod]
    public void BitPanelShouldTakeTheScrollbarOffTheNamedScroller()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            var locked = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
            Assert.AreEqual(".scroller", locked.Arguments[0]);
        });
    }

    [TestMethod]
    public void BitPanelShouldNotTouchThePageScrollingWhenNotAskedTo()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
        });

        com.Render(p => p.Add(x => x.IsOpen, true));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count);
    }

    // A panel taken off the page while it was open would otherwise leave the page without its scrollbar.
    [TestMethod]
    public void BitPanelShouldGiveTheScrollbarBackWhenItIsDisposedWhileOpen()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreNotEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"].Count));

        com.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        var released = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][^1];
        Assert.AreEqual(false, released.Arguments[1]);
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
            parameters.Add(p => p.Position, BitPanelPosition.Bottom);
            parameters.Add(p => p.SwipeTrigger, 0.5m);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments;

        Assert.AreEqual(0.5m, arguments[1]);
        Assert.AreEqual(BitPanelPosition.Bottom, arguments[2]);
        Assert.AreEqual(false, arguments[3]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, arguments[4]);
    }

    // Every input of the geometry the gesture was registered with is a parameter that can change at runtime.
    [TestMethod]
    public void BitPanelShouldRegisterTheSwipeGestureAgainWhenTheGeometryChanges()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Position, BitPanelPosition.End);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count));

        com.Render(p => p.Add(x => x.Position, BitPanelPosition.Top));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Swipes.dispose"].Count);
            Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"].Count);
        });

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Swipes.setup"][^1].Arguments;

        Assert.AreEqual(BitPanelPosition.Top, arguments[2]);
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
}
