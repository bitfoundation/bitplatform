using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

[TestClass]
public class BitOverlayParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitOverlayShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitOverlayCascadingParamsTest>();

        var overlays = component.FindAll(".bit-ovl");

        Assert.AreEqual(2, overlays.Count);

        // The first Overlay takes everything from the cascading parameters.
        var first = overlays[0];
        foreach (var cls in new[] { "bit-ovl-mfl", "bit-ovl-abs", "bit-ovl-ctr", "cascaded" })
        {
            Assert.IsTrue(first.ClassList.Contains(cls), cls);
        }
        StringAssert.Contains(first.GetAttribute("style"), "z-index:7");

        // The second one sets its own position, z-index and class, which the cascade must not overwrite.
        var second = overlays[1];
        Assert.IsTrue(second.ClassList.Contains("bit-ovl-ben"));
        Assert.IsFalse(second.ClassList.Contains("bit-ovl-ctr"));
        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsTrue(second.ClassList.Contains("bit-ovl-mfl"));
        StringAssert.Contains(second.GetAttribute("style"), "z-index:9");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOverlayShouldTakeACascadedBlocking(bool takesTheCascade)
    {
        var isOpen = true;

        var component = RenderComponent<CascadingValue<BitOverlayParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitOverlayParams.ParamName);
            parameters.Add(p => p.Value, new BitOverlayParams { Blocking = true });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitOverlay>(0);
                builder.AddComponentParameter(1, nameof(BitOverlay.IsOpen), isOpen);
                builder.AddComponentParameter(2, nameof(BitOverlay.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
                if (takesTheCascade is false)
                {
                    // An Overlay that turns Blocking off itself keeps its light dismissal.
                    builder.AddComponentParameter(3, nameof(BitOverlay.Blocking), false);
                }
                builder.CloseComponent();
            }));
        });

        component.Find(".bit-ovl").Click();

        Assert.AreEqual(takesTheCascade, isOpen);
    }

    [TestMethod]
    public void BitOverlayShouldTakeACascadedScrollerSelector()
    {
        var component = RenderComponent<CascadingValue<BitOverlayParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitOverlayParams.ParamName);
            parameters.Add(p => p.Value, new BitOverlayParams { AutoToggleScroll = true, ScrollerSelector = ".app-scroller" });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitOverlay>(0);
                builder.AddComponentParameter(1, nameof(BitOverlay.IsOpen), true);
                builder.CloseComponent();
            }));
        });

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.toggleOverflow"][0];
            Assert.AreEqual(".app-scroller", invocation.Arguments[1]);
            Assert.AreEqual(true, invocation.Arguments[2]);
        });
    }

    [TestMethod]
    public async Task BitOverlayShouldTakeACascadedNoDismissOnEscape()
    {
        var isOpen = true;

        var component = RenderComponent<CascadingValue<BitOverlayParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitOverlayParams.ParamName);
            parameters.Add(p => p.Value, new BitOverlayParams { NoDismissOnEscape = true });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitOverlay>(0);
                builder.AddComponentParameter(1, nameof(BitOverlay.IsOpen), isOpen);
                builder.AddComponentParameter(2, nameof(BitOverlay.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
                builder.CloseComponent();
            }));
        });

        var overlay = component.FindComponent<BitOverlay>();

        await overlay.InvokeAsync(() => overlay.Instance._OnEscape());

        Assert.IsTrue(isOpen);
    }
}
