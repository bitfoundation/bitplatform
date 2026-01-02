using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
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

    [TestMethod]
    public void BitSwipeTrapShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "swipe area");
        });

        component.MarkupMatches(@"<div aria-label=""swipe area"" class=""bit-stp"" id:ignore></div>");
    }

    [TestMethod]
    public void BitSwipeTrapShouldCallJsSetupOnFirstRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        RenderComponent<BitSwipeTrap>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.SwipeTrap.setup");
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

        await component.Instance._OnStart(10, 20);

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(10, eventArgs!.StartX);
        Assert.AreEqual(20, eventArgs.StartY);
        Assert.AreEqual(0, eventArgs.DiffX);
        Assert.AreEqual(0, eventArgs.DiffY);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnMove()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnMove, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnMove(5, 6, 7, 8);

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(5, eventArgs!.StartX);
        Assert.AreEqual(6, eventArgs.StartY);
        Assert.AreEqual(7, eventArgs.DiffX);
        Assert.AreEqual(8, eventArgs.DiffY);
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldInvokeOnEnd()
    {
        BitSwipeTrapEventArgs? eventArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnEnd, (BitSwipeTrapEventArgs args) => eventArgs = args);
        });

        await component.Instance._OnEnd(1, 2, 3, 4);

        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(1, eventArgs!.StartX);
        Assert.AreEqual(2, eventArgs.StartY);
        Assert.AreEqual(3, eventArgs.DiffX);
        Assert.AreEqual(4, eventArgs.DiffY);
    }

    [TestMethod,
        DataRow(10, 2, BitSwipeDirection.Right),
        DataRow(-5, 1, BitSwipeDirection.Left),
        DataRow(2, 9, BitSwipeDirection.Bottom),
        DataRow(3, -7, BitSwipeDirection.Top)]
    public async Task BitSwipeTrapShouldInvokeOnTrigger(int diffX, int diffY, BitSwipeDirection expectedDirection)
    {
        BitSwipeTrapTriggerArgs? triggerArgs = null;

        var component = RenderComponent<BitSwipeTrap>(parameters =>
        {
            parameters.Add(p => p.OnTrigger, (BitSwipeTrapTriggerArgs args) => triggerArgs = args);
        });

        await component.Instance._OnTrigger(diffX, diffY);

        Assert.IsNotNull(triggerArgs);
        Assert.AreEqual(expectedDirection, triggerArgs!.Direction);
        Assert.AreEqual(diffX, triggerArgs.DiffX);
        Assert.AreEqual(diffY, triggerArgs.DiffY);
    }
}
