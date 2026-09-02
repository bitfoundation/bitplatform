using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

[TestClass]
public class BitOverlayTests : BunitTestContext
{
    // An Overlay that hosts no content and carries no accessible name is a purely decorative layer, so it
    // reports itself aria-hidden; one given content is the very thing a screen reader is meant to reach.
    [TestMethod]
    public void BitOverlayShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-dis"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitOverlayShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl {cssClass}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectId(string id)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" aria-hidden=""true"" class=""bit-ovl""></div>");
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitOverlayShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitOverlayShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var styleAttribute = visibility switch
        {
            BitVisibility.Hidden => @"style=""visibility: hidden;""",
            BitVisibility.Collapsed => @"style=""display: none;""",
            _ => null
        };

        component.MarkupMatches(@$"<div {styleAttribute} aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<div style=""display: none;"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    // An Overlay with content is no longer decorative, so the content case must not carry aria-hidden.
    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        if (childContent is not null)
        {
            component.MarkupMatches(@$"<div class=""bit-ovl"" id:ignore>{childContent}</div>");
        }
        else
        {
            component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
    }

    // A content-less Overlay that was given an accessible name is not decorative either, so the name is
    // what it renders rather than aria-hidden.
    [TestMethod]
    public void BitOverlayShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "loading");
        });

        component.MarkupMatches(@"<div aria-label=""loading"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitOverlayHtmlAttributesTest>();

        component.MarkupMatches(@$"<div data-val-test=""bit"" class=""bit-ovl"" id:ignore>I'm an overlay</div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectNoAutoClose(bool noAutoClose)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, currentValue => isOpen = currentValue);
            parameters.Add(p => p.NoAutoClose, noAutoClose);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        Assert.IsTrue(isOpen);

        var element = component.Find(".bit-ovl");
        element.Click();

        var cssClass = noAutoClose ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");

        if (noAutoClose)
        {
            Assert.IsTrue(isOpen);
        }
        else
        {
            Assert.IsFalse(isOpen);
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectAutoToggleScroll(bool autoToggleScroll)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AutoToggleScroll, autoToggleScroll);
        });

        var element = component.Find(".bit-ovl");
        element.Click();

        if (autoToggleScroll)
        {
            //AutoToggleScroll is false by default so it should invoke "BitBlazorUI.Utils.toggleOverflow" once and then once again on closing component
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.toggleOverflow", 2);
        }
        else
        {
            Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.toggleOverflow");
        }
    }

    // The toggle acts on the scroller the consumer named, and falls back to the page when none was named.
    [TestMethod,
        DataRow(".scroller"),
        DataRow(null)
    ]
    public void BitOverlayShouldToggleTheScrollerTheSelectorNames(string scrollerSelector)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, scrollerSelector);
        });

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
            Assert.AreEqual(scrollerSelector ?? "body", invocation.Arguments[1]);
            Assert.AreEqual(true, invocation.Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // The decision and the scroller are snapshotted at open time, so the close hands the overflow back even
    // when AutoToggleScroll was turned off - and hands it back to the scroller it was taken from even when
    // the selector was changed - while the Overlay was open.
    [TestMethod]
    public void BitOverlayShouldHandTheOverflowBackWithTheSnapshotTakenAtOpen()
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]), TimeSpan.FromSeconds(5));

        isOpen = false;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AutoToggleScroll, false);
            parameters.Add(p => p.ScrollerSelector, ".another-scroller");
        });

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(2, invocations);
            Assert.AreEqual(".scroller", invocations[1].Arguments[1]);
            Assert.AreEqual(false, invocations[1].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // An Overlay that never took the overflow away has nothing to hand back, whatever AutoToggleScroll
    // says by the time it closes.
    [TestMethod]
    public void BitOverlayShouldNotHandBackWhatItNeverTook()
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        isOpen = false;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.toggleOverflow");
    }

    // An Overlay taken off the page while it was open would otherwise leave the scroller it held without
    // its scrollbar for good.
    [TestMethod]
    public async Task BitOverlayShouldHandTheOverflowBackOnDispose()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]), TimeSpan.FromSeconds(5));

        await Context.DisposeComponentsAsync();

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
        Assert.HasCount(2, invocations);
        Assert.AreEqual(false, invocations[1].Arguments[2]);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectAbsolutePosition(bool absolutePosition)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, absolutePosition);
        });

        var cssClass = absolutePosition ? " bit-ovl-abs" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectAbsolutePositionChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
        });

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-abs"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectModeFull(bool modeFull)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.ModeFull, modeFull);
        });

        var cssClass = modeFull ? " bit-ovl-mfl" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectModeFullChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ModeFull, true);
        });

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-mfl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectCenter(bool center)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.Center, center);
        });

        var cssClass = center ? " bit-ovl-ctr" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldRespectCenterChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Center, true);
        });

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-ctr"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(1300),
        DataRow(null)
    ]
    public void BitOverlayShouldRespectZIndex(int? zIndex)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.ZIndex, zIndex);
        });

        if (zIndex.HasValue)
        {
            component.MarkupMatches(@$"<div style=""z-index:{zIndex}"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitOverlayShouldRespectZIndexChangingAfterRender()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ZIndex, 1300);
        });

        component.MarkupMatches(@"<div style=""z-index:1300"" aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectIsOpen(bool IsOpen)
    {
        var isOpenBind = IsOpen;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpenBind, value => isOpenBind = value);
        });

        var cssClass = IsOpen ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");

        Assert.AreEqual(IsOpen, isOpenBind);

        var element = component.Find(".bit-ovl");
        element.Click();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        Assert.IsFalse(isOpenBind);
    }

    [TestMethod]
    public void BitOverlayShouldRespectIsOpenChangingAfterRender()
    {
        var isOpen = false;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        isOpen = true;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");
    }

    // The uncontrolled starting state, which only applies while the consumer is not driving IsOpen itself.
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectDefaultIsOpen(bool defaultIsOpen)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, defaultIsOpen);
        });

        var cssClass = defaultIsOpen ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldPreferIsOpenOverDefaultIsOpen()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.DefaultIsOpen, true);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    // The click only means anything on an Overlay the user can actually see: a closed one is invisible in
    // the browser, so a click that reaches it in a test must not fire the callback either.
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectOnClick(bool isEnabled)
    {
        int clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var rootDiv = component.Find(".bit-ovl");
        rootDiv.Click();

        var expected = isEnabled ? 1 : 0;
        Assert.AreEqual(expected, clickedValue);
    }

    [TestMethod]
    public void BitOverlayShouldNotFireOnClickWhileClosed()
    {
        int clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var rootDiv = component.Find(".bit-ovl");
        rootDiv.Click();

        Assert.AreEqual(0, clickedValue);
    }

    // OnClick is reported for the clicks a NoAutoClose Overlay refuses to be closed by, which is what
    // makes it the place to react to a click that was turned down.
    [TestMethod]
    public void BitOverlayShouldFireOnClickOnANoAutoCloseOverlay()
    {
        int clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoAutoClose, true);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var rootDiv = component.Find(".bit-ovl");
        rootDiv.Click();

        Assert.AreEqual(1, clickedValue);

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitOverlayShouldRespectOpenCloseToggleMethods()
    {
        var component = RenderComponent<BitOverlay>();

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance.Open());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance.Close());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance.Toggle());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance.Toggle());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl"" id:ignore></div>");
    }

    // A disabled Overlay takes nothing from the user and is not opened by code either, but the code that
    // owns it can always close it: one disabled while it was open would otherwise be stuck on the screen.
    [TestMethod]
    public async Task BitOverlayMethodsShouldRespectIsEnabled()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Open());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-dis"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
        });

        await component.InvokeAsync(() => component.Instance.Open());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Close());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-dis"" id:ignore></div>");
    }

    // Toggle goes through Open and Close, so it inherits their stance on being disabled: it must not open a
    // disabled Overlay, but it must still close one that was disabled while it was open.
    [TestMethod]
    public async Task BitOverlayToggleShouldRespectIsEnabled()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Toggle());
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-dis"" id:ignore></div>");

        var isOpen = true;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn bit-dis"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance.Toggle());

        Assert.IsFalse(isOpen);
        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-dis"" id:ignore></div>");
    }

    // toggleOverflow reports the scroller's scrollTop, which only an absolutely positioned Overlay uses to
    // re-align itself over the container it is laid out in.
    [TestMethod]
    public void BitOverlayAbsolutePositionShouldCarryTheScrollTopOffsetOfTheToggledScroller()
    {
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", _ => true).SetResult(120);

        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(
            () => StringAssert.Contains(component.Find(".bit-ovl").GetAttribute("style"), "top:120px"),
            TimeSpan.FromSeconds(5));
    }

    // On a fixed Overlay the same declaration would push it off the bottom of the screen.
    [TestMethod]
    public void BitOverlayFixedOverlayShouldNeverCarryTheScrollTopOffset()
    {
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", _ => true).SetResult(120);

        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.Style, "color:red");
        });

        // Force the style builder to recompute so a stale offset would have a chance to land.
        component.Render(parameters => parameters.Add(p => p.Style, "color:blue"));

        var style = component.Find(".bit-ovl").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("top:"), $"A fixed Overlay must not carry a top offset, got '{style}'.");
    }

    // The offset is taken back with the overflow, so a closed Overlay carries nothing of the opening.
    [TestMethod]
    public void BitOverlayShouldDropTheScrollTopOffsetWhenItCloses()
    {
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", _ => true).SetResult(120);

        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(
            () => StringAssert.Contains(component.Find(".bit-ovl").GetAttribute("style"), "top:120px"),
            TimeSpan.FromSeconds(5));

        isOpen = false;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() =>
        {
            var style = component.Find(".bit-ovl").GetAttribute("style") ?? string.Empty;
            Assert.IsFalse(style.Contains("top:"), $"A closed Overlay must not carry a top offset, got '{style}'.");
        }, TimeSpan.FromSeconds(5));
    }
}
