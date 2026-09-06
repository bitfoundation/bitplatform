using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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
            component.MarkupMatches(@$"<div class=""bit-ovl"" id:ignore><div class=""bit-ovl-cnt"">{childContent}</div></div>");
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

        component.MarkupMatches(@$"<div data-val-test=""bit"" class=""bit-ovl"" id:ignore><div class=""bit-ovl-cnt"">I'm an overlay</div></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldRespectBlocking(bool blocking)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, currentValue => isOpen = currentValue);
            parameters.Add(p => p.Blocking, blocking);
        });

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");

        Assert.IsTrue(isOpen);

        var element = component.Find(".bit-ovl");
        element.Click();

        var cssClass = blocking ? " bit-ovl-opn" : null;

        component.MarkupMatches(@$"<div aria-hidden=""true"" class=""bit-ovl{cssClass}"" id:ignore></div>");

        if (blocking)
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

    // The element takes precedence over the selector, and it is the element the toggle is handed.
    [TestMethod]
    public void BitOverlayShouldPreferTheScrollerElementOverTheSelector()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.ScrollerElement, new ElementReference("test-scroller"));
        });

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
            Assert.IsInstanceOfType<ElementReference>(invocation.Arguments[1]);
            Assert.AreEqual("test-scroller", ((ElementReference)invocation.Arguments[1]!).Id);
        }, TimeSpan.FromSeconds(5));
    }

    // Taking the scrollbar away narrows the scroller, which would shift everything behind the Overlay
    // sideways in the frame it appears in; the room it took is asked to be given back as padding.
    [TestMethod]
    public void BitOverlayShouldAskForTheScrollbarRoomToBeCompensated()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
            Assert.AreEqual(true, invocation.Arguments[3]);
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

    // A fixed layer chains the wheel and the touch drag it catches to the document, which in a layout that
    // scrolls a region of its own is not the thing that scrolls; the gestures are handed to that region
    // for as long as an Overlay that leaves it scrolling is open.
    [TestMethod]
    public void BitOverlayShouldForwardTheGesturesToTheScrollerItLeavesScrolling()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"][0];
            Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[0]);
            Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[1]);
            Assert.AreEqual(".scroller", invocation.Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // Only the Overlay holding nothing wants the forwarding, only the one anchored to the screen needs it,
    // and only the one aimed at a scroller of its own can use it - the page is what the browser already
    // chains to. A closed Overlay catches no gesture in the first place.
    [TestMethod,
        DataRow(false, false, false, false),
        DataRow(true, false, false, true),
        DataRow(false, true, false, true),
        DataRow(false, false, true, true)
    ]
    public void BitOverlayShouldNotForwardTheGesturesItHasNoBusinessForwarding(bool autoToggleScroll,
                                                                              bool absolutePosition,
                                                                              bool noScroller,
                                                                              bool isOpen)
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.AutoToggleScroll, autoToggleScroll);
            parameters.Add(p => p.AbsolutePosition, absolutePosition);
            parameters.Add(p => p.ScrollerSelector, noScroller ? null : ".scroller");
        });

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.forwardScroll");
    }

    // The forwarding is registered against the scroller it was aimed at, so an Overlay pointed somewhere
    // else while it is open takes it back and makes it again.
    [TestMethod]
    public void BitOverlayShouldRemakeTheForwardingWhenItIsAimedAtAnotherScroller()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"]), TimeSpan.FromSeconds(5));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".another-scroller");
        });

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"];
            Assert.HasCount(2, invocations);
            Assert.AreEqual(".another-scroller", invocations[1].Arguments[2]);
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.stopForwardScroll");
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitOverlayShouldStopForwardingTheGesturesWhenItCloses()
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"]), TimeSpan.FromSeconds(5));

        isOpen = false;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(
            () => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.stopForwardScroll"]),
            TimeSpan.FromSeconds(5));
    }

    // The listeners live on a registry that lasts as long as the page does, so an Overlay taken off the
    // page while it was forwarding would leave them behind for good.
    [TestMethod]
    public async Task BitOverlayShouldStopForwardingTheGesturesOnDispose()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"]), TimeSpan.FromSeconds(5));

        await Context.DisposeComponentsAsync();

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.stopForwardScroll"]);
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

    // The hold is what AutoToggleScroll says it is for as long as the Overlay is open, rather than only what
    // it said at the opening: one told to hold its scroller while it is open takes the hold there and then.
    [TestMethod]
    public void BitOverlayShouldTakeTheOverflowWhenItIsToldToWhileItIsOpen()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.toggleOverflow");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(1, invocations);
            Assert.AreEqual(true, invocations[0].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // And one told to let go hands the overflow back without waiting to be closed.
    [TestMethod]
    public void BitOverlayShouldHandTheOverflowBackWhenItIsToldToWhileItIsOpen()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]), TimeSpan.FromSeconds(5));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, false);
        });

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(2, invocations);
            Assert.AreEqual(false, invocations[1].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // The hold is registered against the scroller it was taken on, so an Overlay pointed somewhere else while
    // it is open lets go of the one it holds before it takes the one it is pointed at now.
    [TestMethod]
    public void BitOverlayShouldRetakeTheOverflowWhenItIsAimedAtAnotherScroller()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]), TimeSpan.FromSeconds(5));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".another-scroller");
        });

        component.WaitForAssertion(() =>
        {
            var invocations = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"];
            Assert.HasCount(3, invocations);
            Assert.AreEqual(".scroller", invocations[1].Arguments[1]);
            Assert.AreEqual(false, invocations[1].Arguments[2]);
            Assert.AreEqual(".another-scroller", invocations[2].Arguments[1]);
            Assert.AreEqual(true, invocations[2].Arguments[2]);
        }, TimeSpan.FromSeconds(5));
    }

    // Handing the gestures on and taking the overflow away are two ways of doing the one job, so an Overlay
    // that takes up one of them stands the other down rather than being left doing both at once.
    [TestMethod]
    public void BitOverlayShouldStopForwardingOnceItTakesTheOverflowWhileItIsOpen()
    {
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(() => Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"]), TimeSpan.FromSeconds(5));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.stopForwardScroll"]);
            Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"]);
            Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forwardScroll"]);
        }, TimeSpan.FromSeconds(5));
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

    // OnClick is reported for the clicks a Blocking Overlay refuses to be closed by, which is what
    // makes it the place to react to a click that was turned down.
    [TestMethod]
    public void BitOverlayShouldFireOnClickOnABlockingOverlay()
    {
        int clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var rootDiv = component.Find(".bit-ovl");
        rootDiv.Click();

        Assert.AreEqual(1, clickedValue);

        component.MarkupMatches(@"<div aria-hidden=""true"" class=""bit-ovl bit-ovl-opn"" id:ignore></div>");
    }

    // The layer is what a dismissal is aimed at: the content it hosts is the thing the user is reaching
    // past the layer for, so a click on it must leave the Overlay up.
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldNotCloseOnAClickOnItsContent(bool blocking)
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.Blocking, blocking);
            parameters.AddChildContent("<button>inside</button>");
        });

        component.Find("button").Click();

        Assert.IsTrue(isOpen);
        component.MarkupMatches(@"<div class=""bit-ovl bit-ovl-opn"" id:ignore><div class=""bit-ovl-cnt""><button>inside</button></div></div>");
    }

    // The click is still reported, which is what makes OnClick the one place a consumer closing the Overlay
    // on terms of its own has to look.
    [TestMethod]
    public void BitOverlayShouldFireOnClickForAClickOnItsContent()
    {
        var clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnClick, () => clickedValue++);
            parameters.AddChildContent("<button>inside</button>");
        });

        component.Find("button").Click();

        Assert.AreEqual(1, clickedValue);
    }

    // A closed Overlay reports nothing, its content included: the click that reaches it in a test is one
    // no user could have made in the browser.
    [TestMethod]
    public void BitOverlayShouldNotFireOnClickForAContentClickWhileClosed()
    {
        var clickedValue = 0;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => clickedValue++);
            parameters.AddChildContent("<button>inside</button>");
        });

        component.Find("button").Click();

        Assert.AreEqual(0, clickedValue);
    }

    // The last stretch of a selection dragged out of a box: the press began on the content and only reached
    // the layer on the way back up, so the click the browser reports on the Overlay is not a dismissal.
    [TestMethod]
    public void BitOverlayShouldNotCloseWhenThePressBeganOnItsContent()
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.AddChildContent("<span>inside</span>");
        });

        component.Find("span").MouseDown();
        component.Find(".bit-ovl").Click();

        Assert.IsTrue(isOpen);

        // The refusal lasts for that one click only: the next press on the layer dismisses as it always did.
        component.Find(".bit-ovl").MouseDown();
        component.Find(".bit-ovl").Click();

        Assert.IsFalse(isOpen);
    }

    // A press that began on the layer is a dismissal wherever it ends.
    [TestMethod]
    public void BitOverlayShouldCloseWhenThePressBeganOnTheLayer()
    {
        var isOpen = true;
        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.AddChildContent("<span>inside</span>");
        });

        var overlay = component.Find(".bit-ovl");
        overlay.MouseDown();
        overlay.Click();

        Assert.IsFalse(isOpen);
    }

    // The state change is reported however it was brought about, so a binding, the methods and a click on
    // the layer all come through the same pair of callbacks.
    [TestMethod]
    public async Task BitOverlayShouldRespectOnOpenAndOnClose()
    {
        var opened = 0;
        var closed = 0;
        var isOpen = false;

        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        // A closed Overlay that was never opened has nothing to report.
        Assert.AreEqual(0, opened);
        Assert.AreEqual(0, closed);

        isOpen = true;
        component.Render(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, opened), TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, closed);

        component.Find(".bit-ovl").Click();

        component.WaitForAssertion(() => Assert.AreEqual(1, closed), TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, opened);

        await component.InvokeAsync(() => component.Instance.Toggle());

        component.WaitForAssertion(() => Assert.AreEqual(2, opened), TimeSpan.FromSeconds(5));

        await component.InvokeAsync(() => component.Instance.Close());

        component.WaitForAssertion(() => Assert.AreEqual(2, closed), TimeSpan.FromSeconds(5));
    }

    // One that starts open reports that opening, since the callback is about the state rather than about
    // the gesture that would otherwise have caused it.
    [TestMethod]
    public void BitOverlayShouldReportTheOpeningOfAnOverlayThatStartsOpen()
    {
        var opened = 0;
        var closed = 0;

        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, opened), TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, closed);
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

    // The offset belongs to the scroller the hold is on, so an Overlay re-aimed while it is open carries the
    // scrollTop of the scroller it is pointed at now rather than the one it opened over.
    [TestMethod]
    public void BitOverlayShouldCarryTheScrollTopOffsetOfTheScrollerItWasReAimedAt()
    {
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", i => ".scroller".Equals(i.Arguments[1])).SetResult(120);
        Context.JSInterop.Setup<float>("BitBlazorUI.Utils.toggleOverflow", i => ".another-scroller".Equals(i.Arguments[1])).SetResult(300);

        var component = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        component.WaitForAssertion(
            () => StringAssert.Contains(component.Find(".bit-ovl").GetAttribute("style"), "top:120px"),
            TimeSpan.FromSeconds(5));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AbsolutePosition, true);
            parameters.Add(p => p.AutoToggleScroll, true);
            parameters.Add(p => p.ScrollerSelector, ".another-scroller");
        });

        component.WaitForAssertion(
            () => StringAssert.Contains(component.Find(".bit-ovl").GetAttribute("style"), "top:300px"),
            TimeSpan.FromSeconds(5));
    }
}
