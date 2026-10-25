using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

/// <summary>
/// Covers the BitParams cascade of the Overlay: the name it is cascaded under, that the params object carries every
/// parameter an overlay does not write per instance, what a cascaded value fills in and what the overlay keeps
/// because it wrote it for itself.
/// </summary>
[TestClass]
public class BitOverlayParamsTests : BunitTestContext
{
    // What an overlay shows, whether it is open and what it reports back are written per overlay, so they have no
    // place on the params object; the scroller element is a reference to one element of one page.
    private static readonly string[] _notCascaded =
    [
        nameof(BitOverlay.CascadingParameters),
        nameof(BitOverlay.ChildContent),
        nameof(BitOverlay.DefaultIsOpen),
        nameof(BitOverlay.IsOpen),
        nameof(BitOverlay.IsOpenChanged),
        nameof(BitOverlay.OnClick),
        nameof(BitOverlay.OnClose),
        nameof(BitOverlay.OnOpen),
        nameof(BitOverlay.ScrollerElement),
    ];

    [TestMethod]
    public void BitOverlayParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitOverlay)}", BitOverlayParams.ParamName);
        Assert.AreEqual("BitParams.BitOverlay", BitOverlayParams.ParamName);
    }

    [TestMethod]
    public void BitOverlayParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitOverlayParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitOverlayParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitOverlayParamsShouldCarryEveryParameterThatIsNotPerOverlay()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitOverlay).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                           .Select(p => p.Name)
                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitOverlayParams).GetProperty(name), $"BitOverlayParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitOverlayShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitOverlayParams() });
            parameters.AddChildContent<BitOverlay>();
        });

        component.MarkupMatches(@"<div tabindex=""-1"" inert class=""bit-ovl"" id:ignore></div>");
    }

    [TestMethod]
    public void BitOverlayShouldApplyCascadedBaseParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitOverlayParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded", AriaLabel = "Loading" } });
            parameters.AddChildContent<BitOverlay>();
        });

        var root = component.Find(".bit-ovl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("Loading", root.GetAttribute("aria-label"));
    }

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
