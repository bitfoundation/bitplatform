using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.SwipeTrap;

[TestClass]
public class BitSwipeTrapTests : BunitTestContext
{
    [TestMethod]
    public void BitSwipeTrapShouldRenderChildContent()
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.AddChildContent("<p class=\"trap-content\">Swipe me</p>");
        });

        component.MarkupMatches(@"<div class=""bit-stp"" id:ignore><p class=""trap-content"">Swipe me</p></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSwipeTrapShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-stp");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)]
    public void BitSwipeTrapShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var rtlClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-stp{rtlClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stp"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow("custom-id"),
        DataRow(null)]
    public void BitSwipeTrapShouldRespectId(string id)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-stp""></div>");
    }

    [TestMethod,
        DataRow("padding: 1rem;"),
        DataRow(null)]
    public void BitSwipeTrapShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stp"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stp"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow("custom-class"),
        DataRow(null)]
    public void BitSwipeTrapShouldRespectClass(string cssClass)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        var expectedClass = cssClass.HasValue() ? $"bit-stp {cssClass}" : "bit-stp";

        component.MarkupMatches(@$"<div class=""{expectedClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSwipeTrapShouldRespectHtmlAttributes()
    {
        // The splat only reaches the trap through the render tree: HtmlAttributes is a plain parameter on
        // BitComponentBase rather than a CaptureUnmatchedValues one, so bUnit's AddUnmatched cannot feed it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSwipeTrap>(0);
                builder.AddAttribute(1, "data-test", "swipe");
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("swipe", component.Find(".bit-stp").GetAttribute("data-test"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Hidden),
        DataRow(BitVisibility.Collapsed)]
    public void BitSwipeTrapShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-stp"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-stp"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stp"" id:ignore></div>");
                break;
        }
    }

    [TestMethod]
    public void BitSwipeTrapShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "swipe area");
        });

        component.MarkupMatches(@"<div aria-label=""swipe area"" class=""bit-stp"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitSwipeOrientation.None),
        DataRow(BitSwipeOrientation.Horizontal),
        DataRow(BitSwipeOrientation.Vertical),
        DataRow(BitSwipeOrientation.Auto),
        DataRow(null)]
    public void BitSwipeTrapShouldRespectOrientationLockClass(BitSwipeOrientation? orientationLock)
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OrientationLock, orientationLock);
        });

        var root = component.Find(".bit-stp");

        if (orientationLock is BitSwipeOrientation.Horizontal)
        {
            Assert.IsTrue(root.ClassList.Contains("bit-stp-hrz"));
        }
        else if (orientationLock is BitSwipeOrientation.Vertical)
        {
            Assert.IsTrue(root.ClassList.Contains("bit-stp-vrt"));
        }
        else if (orientationLock is BitSwipeOrientation.Auto)
        {
            Assert.IsTrue(root.ClassList.Contains("bit-stp-lck"));
        }
        else
        {
            Assert.IsFalse(root.ClassList.Contains("bit-stp-hrz"));
            Assert.IsFalse(root.ClassList.Contains("bit-stp-vrt"));
            Assert.IsFalse(root.ClassList.Contains("bit-stp-lck"));
        }
    }

    [TestMethod]
    public void BitSwipeTrapShouldUpdateOrientationLockClassOnParameterChange()
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OrientationLock, BitSwipeOrientation.Horizontal);
        });

        Assert.IsTrue(component.Find(".bit-stp").ClassList.Contains("bit-stp-hrz"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.OrientationLock, BitSwipeOrientation.Vertical);
        });

        var root = component.Find(".bit-stp");
        Assert.IsFalse(root.ClassList.Contains("bit-stp-hrz"));
        Assert.IsTrue(root.ClassList.Contains("bit-stp-vrt"));
    }

    [TestMethod]
    public void BitSwipeTrapShouldCallJsSetupOnFirstRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        RenderComponent<BitSwipeTrap>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.setup");
    }

    [TestMethod]
    public void BitSwipeTrapShouldPassDefaultValuesToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        RenderComponent<BitSwipeTrap>();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.setup");

        Assert.AreEqual(0.25m, invocation.Arguments[2]);       // trigger
        Assert.AreEqual(0m, invocation.Arguments[3]);          // triggerVelocity
        Assert.AreEqual(0m, invocation.Arguments[4]);          // threshold
        Assert.AreEqual(0, invocation.Arguments[5]);           // throttle
        Assert.AreEqual(BitSwipeOrientation.None, invocation.Arguments[6]); // orientationLock
        Assert.AreEqual(false, invocation.Arguments[7]);       // touchOnly
        Assert.IsNull(invocation.Arguments[8]);                // skipSelector
    }

    [TestMethod]
    public void BitSwipeTrapShouldPassCustomValuesToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Trigger, 60m);
            parameters.Add(p => p.TriggerVelocity, 0.5m);
            parameters.Add(p => p.Threshold, 10m);
            parameters.Add(p => p.Throttle, 20);
            parameters.Add(p => p.OrientationLock, BitSwipeOrientation.Horizontal);
            parameters.Add(p => p.TouchOnly, true);
            parameters.Add(p => p.SkipSelector, ".no-swipe");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.setup");

        Assert.AreEqual(60m, invocation.Arguments[2]);
        Assert.AreEqual(0.5m, invocation.Arguments[3]);
        Assert.AreEqual(10m, invocation.Arguments[4]);
        Assert.AreEqual(20, invocation.Arguments[5]);
        Assert.AreEqual(BitSwipeOrientation.Horizontal, invocation.Arguments[6]);
        Assert.AreEqual(true, invocation.Arguments[7]);
        Assert.AreEqual(".no-swipe", invocation.Arguments[8]);
    }

    [TestMethod]
    public void BitSwipeTrapShouldReSetupJsOnParameterChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Trigger, 0.3m);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Trigger, 0.7m);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.dispose");

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual(0.3m, setups[0].Arguments[2]);
        Assert.AreEqual(0.7m, setups[1].Arguments[2]);
    }

    [TestMethod]
    public void BitSwipeTrapShouldNotReSetupJsWhenParametersAreUnchanged()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.Trigger, 0.3m);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Trigger, 0.3m);
        });

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        Assert.AreEqual(1, setups.Count);
    }

    [TestMethod]
    public void BitSwipeTrapShouldReSetupJsOnFilteringParameterChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.TouchOnly, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.TouchOnly, true);
            parameters.Add(p => p.SkipSelector, ".no-swipe");
        });

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        Assert.AreEqual(3, setups.Count);
        Assert.AreEqual(false, setups[0].Arguments[7]);
        Assert.AreEqual(true, setups[1].Arguments[7]);
        Assert.AreEqual(".no-swipe", setups[2].Arguments[8]);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldDisposeJsInteropOnDispose()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.dispose");
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnStart()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnStart, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnStart(10, 20, "touch");

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(10, eventArgs!.StartX);
        Assert.AreEqual(20, eventArgs.StartY);
        Assert.AreEqual(0, eventArgs.DiffX);
        Assert.AreEqual(0, eventArgs.DiffY);
        Assert.AreEqual(0, eventArgs.VelocityX);
        Assert.AreEqual(0, eventArgs.VelocityY);
        Assert.AreEqual("touch", eventArgs.PointerType);
        Assert.IsFalse(eventArgs.IsCanceled);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnMove()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnMove, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnMove(5, 6, 7, 8, 0.9m, 1.1m);

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(5, eventArgs!.StartX);
        Assert.AreEqual(6, eventArgs.StartY);
        Assert.AreEqual(7, eventArgs.DiffX);
        Assert.AreEqual(8, eventArgs.DiffY);
        Assert.AreEqual(0.9m, eventArgs.VelocityX);
        Assert.AreEqual(1.1m, eventArgs.VelocityY);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnEnd()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnEnd, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnEnd(1, 2, 3, 4, 0.5m, 0.6m, "mouse");

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(1, eventArgs!.StartX);
        Assert.AreEqual(2, eventArgs.StartY);
        Assert.AreEqual(3, eventArgs.DiffX);
        Assert.AreEqual(4, eventArgs.DiffY);
        Assert.AreEqual(0.5m, eventArgs.VelocityX);
        Assert.AreEqual(0.6m, eventArgs.VelocityY);
        Assert.AreEqual("mouse", eventArgs.PointerType);
        Assert.IsFalse(eventArgs.IsCanceled);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnEndWithIsCanceled()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnEnd, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnEnd(1, 2, 3, 4, 0, 0, "touch", true);

        Assert.IsNotNull(eventArgs);
        Assert.IsTrue(eventArgs!.IsCanceled);
        Assert.AreEqual("touch", eventArgs.PointerType);
    }

    [TestMethod,
        DataRow(10, 2, BitSwipeDirection.Right),
        DataRow(-5, 1, BitSwipeDirection.Left),
        DataRow(2, 9, BitSwipeDirection.Bottom),
        DataRow(3, -7, BitSwipeDirection.Top),
        DataRow(5, 5, BitSwipeDirection.Right),
        DataRow(-5, -5, BitSwipeDirection.Left),
        DataRow(5, -5, BitSwipeDirection.Right),
        DataRow(-5, 5, BitSwipeDirection.Left)]
    public async Task BitSwipeTrapShouldInvokeOnTrigger(int diffX, int diffY, BitSwipeDirection expectedDirection)
    {
        BitSwipeTrapTriggerArgs? triggerArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnTrigger, (BitSwipeTrapTriggerArgs args) => triggerArgs = args);
        });

        await component.Instance._OnTrigger(diffX, diffY, 0.4m, 0.3m, "pen", 240);

        Assert.IsNotNull(triggerArgs);
        Assert.AreEqual(expectedDirection, triggerArgs!.Direction);
        Assert.AreEqual(diffX, triggerArgs.DiffX);
        Assert.AreEqual(diffY, triggerArgs.DiffY);
        Assert.AreEqual(0.4m, triggerArgs.VelocityX);
        Assert.AreEqual(0.3m, triggerArgs.VelocityY);
        Assert.AreEqual("pen", triggerArgs.PointerType);
        Assert.AreEqual(240, triggerArgs.Duration);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldReportDurationOnMoveAndEnd()
    {
        BitSwipeTrapEventArgs? moveArgs = null;
        BitSwipeTrapEventArgs? endArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnMove, (BitSwipeTrapEventArgs args) => moveArgs = args);
            parameters.Add(p => p.OnEnd, (BitSwipeTrapEventArgs args) => endArgs = args);
        });

        await component.Instance._OnMove(1, 2, 3, 4, 0, 0, "touch", 120);
        await component.Instance._OnEnd(1, 2, 3, 4, 0, 0, "touch", false, 350);

        Assert.IsNotNull(moveArgs);
        Assert.AreEqual(120, moveArgs!.Duration);
        Assert.IsFalse(moveArgs.IsCanceled);

        Assert.IsNotNull(endArgs);
        Assert.AreEqual(350, endArgs!.Duration);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldReportZeroDurationOnStart()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnStart, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnStart(10, 20, "mouse");

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(0, eventArgs!.Duration);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldNotThrowWhenNoEventHandlerIsAttached()
    {
        // Every callback is optional: a trap used only for its OnTrigger must not fault the circuit
        // when the JS side reports the moves and the release that led to it.
        var component = RenderComponent<BitSwipeTrap>();

        await component.Instance._OnStart(1, 2, "touch");
        await component.Instance._OnMove(1, 2, 3, 4, 0, 0, "touch", 10);
        await component.Instance._OnEnd(1, 2, 3, 4, 0, 0, "touch", false, 20);
        await component.Instance._OnTrigger(3, 4, 0, 0, "touch", 20);
    }

    [TestMethod]
    public void BitSwipeTrapShouldPassDotNetObjectReferenceToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        var component = RenderComponent<BitSwipeTrap>();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.setup");

        Assert.AreEqual(10, invocation.Arguments.Count);
        Assert.AreEqual(component.Instance.UniqueId.ToString(), invocation.Arguments[0]);
        Assert.IsInstanceOfType<DotNetObjectReference<BitSwipeTrap>>(invocation.Arguments[9]);
    }

    [TestMethod]
    public void BitSwipeTrapShouldDisposeTheOldTrapWhenItIsSetUpAgain()
    {
        // A re-setup tears the previous one down first, and it names the trap it is tearing down: two
        // live setups over one element would double every event the trap reports.
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Threshold, 10m);
        });

        var id = component.Instance.UniqueId.ToString();
        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        var disposes = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.dispose").ToList();

        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual(1, disposes.Count);
        Assert.AreEqual(id, disposes[0].Arguments[0]);
        Assert.IsTrue(setups.All(s => Equals(s.Arguments[0], id)));
    }

    [TestMethod]
    public void BitSwipeTrapShouldReSetupJsOnThresholdAndThrottleChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Threshold, 12m);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Threshold, 12m);
            parameters.Add(p => p.Throttle, 50);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Threshold, 12m);
            parameters.Add(p => p.Throttle, 50);
            parameters.Add(p => p.TriggerVelocity, 0.4m);
        });

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        Assert.AreEqual(4, setups.Count);
        Assert.AreEqual(12m, setups[1].Arguments[4]);
        Assert.AreEqual(50, setups[2].Arguments[5]);
        Assert.AreEqual(0.4m, setups[3].Arguments[3]);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldDisposeJsInteropOnlyOnce()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.dispose");

        var component = RenderComponent<BitSwipeTrap>();

        await component.Instance.DisposeAsync();
        await component.Instance.DisposeAsync();

        var disposes = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.dispose").ToList();
        Assert.AreEqual(1, disposes.Count);
    }
}
