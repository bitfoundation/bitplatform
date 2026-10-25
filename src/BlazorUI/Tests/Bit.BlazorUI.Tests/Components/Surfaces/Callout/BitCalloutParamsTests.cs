using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

/// <summary>
/// Covers the BitParams cascade of the Callout: that every parameter which is not about one callout has a
/// counterpart on BitCalloutParams, that what the callout decides from the cascaded values - its dismissals, its
/// Escape claim, the classes and styles of its parts, the direction carried over to the relocated popup - follows
/// them, that a value the callout wrote for itself is kept, that a cascaded value goes back to the default once the
/// cascade stops carrying it, and that an empty params object leaves the callout as it was.
/// BitCalloutCascadingParamsTest renders the look and the positioning properties; what is here is what that
/// markup cannot show.
/// </summary>
[TestClass]
public class BitCalloutParamsTests : BunitTestContext
{
    // What a callout is about rather than how every callout behaves - its anchor, its content and the parts of it,
    // its open state, the role and the ids it is named by, and what it answers - is written per callout, so it has
    // no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCallout.CascadingParameters),
        nameof(BitCallout.Anchor),
        nameof(BitCallout.AnchorEl),
        nameof(BitCallout.AnchorId),
        nameof(BitCallout.AriaDescribedBy),
        nameof(BitCallout.AriaLabelledBy),
        nameof(BitCallout.ChildContent),
        nameof(BitCallout.Content),
        nameof(BitCallout.DefaultIsOpen),
        nameof(BitCallout.Footer),
        nameof(BitCallout.FooterId),
        nameof(BitCallout.Header),
        nameof(BitCallout.HeaderId),
        nameof(BitCallout.IsOpen),
        nameof(BitCallout.OnDismiss),
        nameof(BitCallout.OnOpen),
        nameof(BitCallout.OnToggle),
        nameof(BitCallout.Role),
        nameof(BitCallout.ScrollContainerId),
    ];

    private static readonly RenderFragment _anchor = builder => builder.AddMarkupContent(0, "<button>Anchor</button>");

    private IRenderedComponent<BitParams> RenderWithParams(BitCalloutParams calloutParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { calloutParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCallout>(0);
                builder.AddAttribute(1, nameof(BitCallout.Anchor), _anchor);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitCallout.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<button class=\"item\">Item</button>")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCalloutParamsShouldHaveCorrectParamName()
    {
        var @params = new BitCalloutParams();

        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitCallout)}", BitCalloutParams.ParamName);
        Assert.AreEqual("BitParams.BitCallout", BitCalloutParams.ParamName);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCalloutParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCalloutParamsShouldCarryEveryParameterThatIsNotPerCallout()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCallout).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                           .Select(p => p.Name)
                                           .Where(n => baseParameters.Contains(n) is false
                                                    && _notCascaded.Contains(n) is false
                                                    && n.EndsWith("Changed", StringComparison.Ordinal) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCalloutParams).GetProperty(name), $"BitCalloutParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitCalloutParamsShouldNotCascadeWhatBelongsToOneCallout()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitCallout.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitCalloutParams).GetProperty(name), $"BitCalloutParams cascades {name}, which belongs to one callout.");
        }
    }

    [TestMethod]
    public void BitCalloutShouldApplyCascadedClassesAndStylesToEveryPart()
    {
        var component = RenderWithParams(new BitCalloutParams
        {
            ShowArrow = true,
            Classes = new()
            {
                Root = "cascaded-root",
                Opened = "cascaded-opened",
                AnchorContainer = "cascaded-anchor",
                Arrow = "cascaded-arrow",
                Content = "cascaded-content",
                Overlay = "cascaded-overlay",
            },
            Styles = new()
            {
                Root = "color:red;",
                Opened = "background:blue;",
                AnchorContainer = "padding:4px;",
                Arrow = "opacity:0.5;",
                Content = "margin:2px;",
                Overlay = "cursor:pointer;",
            },
        }, builder => builder.AddAttribute(2, nameof(BitCallout.IsOpen), true));

        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("cascaded-opened"));
        Assert.IsTrue(component.Find(".bit-clo-acn").ClassList.Contains("cascaded-anchor"));
        Assert.IsTrue(component.Find(".bit-clo-arw").ClassList.Contains("cascaded-arrow"));
        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("cascaded-content"));
        Assert.IsTrue(component.Find(".bit-clo-ovl").ClassList.Contains("cascaded-overlay"));

        StringAssert.Contains(component.Find(".bit-clo").GetAttribute("style"), "color:red");
        StringAssert.Contains(component.Find(".bit-clo").GetAttribute("style"), "background:blue");
        StringAssert.Contains(component.Find(".bit-clo-acn").GetAttribute("style"), "padding:4px");
        StringAssert.Contains(component.Find(".bit-clo-arw").GetAttribute("style"), "opacity:0.5");
        StringAssert.Contains(component.Find(".bit-clo-cal").GetAttribute("style"), "margin:2px");
        StringAssert.Contains(component.Find(".bit-clo-ovl").GetAttribute("style"), "cursor:pointer");
    }

    [TestMethod]
    public void BitCalloutShouldCloseOnAContentClickThroughTheCascade()
    {
        var component = RenderWithParams(new BitCalloutParams { AutoClose = true });

        component.Find(".bit-clo-acn").Click();
        Assert.IsTrue(component.FindComponent<BitCallout>().Instance.IsOpen);

        component.Find(".bit-clo-cal").Click();
        Assert.IsFalse(component.FindComponent<BitCallout>().Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldKeepTheOverlayClickFromDismissingItThroughTheCascade()
    {
        var component = RenderWithParams(new BitCalloutParams { NoDismissOnOutsideClick = true });

        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-ovl").Click();

        Assert.IsTrue(component.FindComponent<BitCallout>().Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldNeitherTakeNorClaimTheEscapeWhenTheCascadeTurnsItOff()
    {
        var component = RenderWithParams(new BitCalloutParams { NoDismissOnEscape = true });

        component.Find(".bit-clo-acn").Click();

        // The claim is read off the same state the handler decides on, so a cascaded NoDismissOnEscape leaves the
        // key to whatever the anchor sits in.
        Assert.IsNull(component.Find(".bit-clo").GetAttribute("data-bit-esc"));

        component.Find(".bit-clo").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        component.Find(".bit-clo-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(component.FindComponent<BitCallout>().Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldHandCascadedPositioningOptionsToThePlacement()
    {
        var component = RenderWithParams(new BitCalloutParams
        {
            Gap = 12,
            CollisionPadding = 24,
            AlignmentOffset = 6,
            ArrowPadding = 20,
            NoFlip = true,
            NoDismissOnScroll = true,
            Placement = BitPlacement.Top,
            Alignment = BitPlacement.Center,
            ShowArrow = true,
        });

        component.Find(".bit-clo-acn").Click();

        var arguments = Context.JSInterop.Invocations["BitBlazorUI.Callouts.toggle"][^1].Arguments;

        Assert.AreEqual(component.Find(".bit-clo-arw").Id, arguments[18]);
        Assert.AreEqual(12, arguments[19]);
        Assert.AreEqual("top", arguments[21]);
        Assert.AreEqual("center", arguments[22]);
        Assert.AreEqual(true, arguments[23]);
        Assert.AreEqual(24, arguments[24]);
        Assert.AreEqual(6, arguments[25]);
        Assert.AreEqual(20, arguments[26]);
        Assert.AreEqual(true, arguments[27]);
    }

    [TestMethod]
    public void BitCalloutShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitCalloutParams
        {
            Dir = BitDir.Rtl,
            Class = "cascaded",
            Style = "margin:1px;",
            AriaLabel = "Cascaded",
        });

        var root = component.Find(".bit-clo");
        var callout = component.Find(".bit-clo-cal");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        // The callout is relocated to the body while it is open, so the direction and the name are carried over to it.
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("Cascaded", callout.GetAttribute("aria-label"));
        Assert.AreEqual("group", callout.GetAttribute("role"));
    }

    [TestMethod]
    public void BitCalloutShouldNotOpenWhenTheCascadeDisablesIt()
    {
        var component = RenderWithParams(new BitCalloutParams { Disabled = true });

        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("bit-dis"));

        component.Find(".bit-clo-acn").Click();

        Assert.IsFalse(component.FindComponent<BitCallout>().Instance.IsOpen);
    }

    [TestMethod]
    public void BitCalloutShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCalloutParams
        {
            AutoClose = true,
            NoDismissOnOutsideClick = true,
            MaxHeight = "20rem",
            Background = BitColorKind.Secondary,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitCallout.AutoClose), false);
            builder.AddAttribute(3, nameof(BitCallout.NoDismissOnOutsideClick), false);
            builder.AddAttribute(4, nameof(BitCallout.MaxHeight), "8rem");
        });

        var callout = component.FindComponent<BitCallout>();

        // Written as false by the callout, the two dismissal opt-outs stay off whatever the cascade says; the
        // background it did not write is still filled in.
        component.Find(".bit-clo-acn").Click();
        component.Find(".bit-clo-cal").Click();
        Assert.IsTrue(callout.Instance.IsOpen);

        component.Find(".bit-clo-ovl").Click();
        Assert.IsFalse(callout.Instance.IsOpen);

        StringAssert.Contains(component.Find(".bit-clo-cal").GetAttribute("style"), "--bit-clo-mxh:8rem");
        Assert.IsFalse(component.Find(".bit-clo-cal").GetAttribute("style")!.Contains("20rem"));
        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-bsg"));
    }

    [TestMethod]
    public void BitCalloutShouldGoBackToItsDefaultsOnceTheCascadeStopsCarryingAValue()
    {
        var component = RenderWithParams(new BitCalloutParams { NoShadow = true, Width = "18rem", ShowArrow = true });

        Assert.IsTrue(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-nsh"));
        Assert.AreEqual(1, component.FindAll(".bit-clo-arw").Count);

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitCalloutParams() }));

        var callout = component.FindComponent<BitCallout>().Instance;

        Assert.IsFalse(callout.NoShadow);
        Assert.IsNull(callout.Width);
        Assert.IsFalse(callout.ShowArrow);
        Assert.IsFalse(component.Find(".bit-clo-cal").ClassList.Contains("bit-clo-nsh"));
        Assert.IsNull(component.Find(".bit-clo-cal").GetAttribute("style"));
        Assert.AreEqual(0, component.FindAll(".bit-clo-arw").Count);
    }

    [TestMethod]
    public void BitCalloutShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitCalloutParams());

        var callout = component.FindComponent<BitCallout>().Instance;

        Assert.IsNull(callout.Alignment);
        Assert.IsNull(callout.Placement);
        Assert.IsNull(callout.Direction);
        Assert.IsNull(callout.ResponsiveMode);
        Assert.IsNull(callout.Background);
        Assert.IsNull(callout.Border);
        Assert.IsNull(callout.Width);
        Assert.IsNull(callout.MaxHeight);
        Assert.AreEqual(0, callout.Gap);
        Assert.AreEqual(0, callout.CollisionPadding);
        Assert.AreEqual(150, callout.HoverCloseDelay);
        Assert.AreEqual(0, callout.HoverOpenDelay);
        Assert.IsFalse(callout.AutoClose);
        Assert.IsFalse(callout.Modal);
        Assert.IsFalse(callout.TrapFocus);
        Assert.IsFalse(callout.ShowArrow);
        Assert.IsFalse(callout.NoShadow);

        Assert.AreEqual(1, component.Find(".bit-clo").ClassList.Length);
        Assert.IsTrue(component.Find(".bit-clo").ClassList.Contains("bit-clo"));
        Assert.IsNull(component.Find(".bit-clo-cal").GetAttribute("style"));
        Assert.IsNull(component.Find(".bit-clo-cal").GetAttribute("role"));
    }
}
