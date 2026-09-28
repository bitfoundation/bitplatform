using System;
using System.Collections.Generic;
using System.Threading;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Collapse;

/// <summary>
/// Covers the BitParams cascade of the Collapse: what a BitCollapseParams fills in, what it leaves alone because
/// the collapse wrote it for itself, and that what the collapse derives from its parameters (its classes, its
/// styles, its rendering and its timing) follows the cascaded values.
/// </summary>
[TestClass]
public class BitCollapseParamsTests : BunitTestContext
{
    private IRenderedComponent<BitParams> RenderWithParams(BitCollapseParams collapseParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { collapseParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCollapse>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitCollapse.ChildContent), (RenderFragment)(b => b.AddContent(0, "Hello")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCollapseParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitCollapse)}", BitCollapseParams.ParamName);
        Assert.AreEqual("BitParams.BitCollapse", BitCollapseParams.ParamName);
    }

    [TestMethod]
    public void BitCollapseParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCollapseParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCollapseParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedClasses()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            Background = BitColorKind.Secondary,
            Horizontal = true,
            NoAnimation = true,
            NoFade = true,
            NoPadding = true,
            ExpandOnPrint = true,
            Classes = new() { Root = "cascaded-root", Content = "cascaded-content", Wrapper = "cascaded-wrapper" },
        });

        var root = component.Find(".bit-col");

        Assert.IsTrue(root.ClassList.Contains("bit-col-sbg"));
        Assert.IsTrue(root.ClassList.Contains("bit-col-hor"));
        Assert.IsTrue(root.ClassList.Contains("bit-col-nan"));
        Assert.IsTrue(root.ClassList.Contains("bit-col-nfd"));
        Assert.IsTrue(root.ClassList.Contains("bit-col-npd"));
        Assert.IsTrue(root.ClassList.Contains("bit-col-eop"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-col-con").ClassList.Contains("cascaded-content"));
        Assert.IsTrue(component.Find(".bit-col-wrp").ClassList.Contains("cascaded-wrapper"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedStyles()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            Duration = 450,
            Delay = 50,
            Easing = "linear",
            CollapsedSize = "3rem",
            Styles = new() { Root = "outline: 1px solid red;", Content = "color: blue;" },
        });

        var root = component.Find(".bit-col");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-col-dur-full:450ms"));
        Assert.IsTrue(style.Contains("--bit-col-del-full:50ms"));
        Assert.IsTrue(style.Contains("--bit-col-eas:linear"));
        Assert.IsTrue(style.Contains("--bit-col-csz:3rem"));
        Assert.IsTrue(style.Contains("outline: 1px solid red;"));
        Assert.AreEqual("color: blue;", component.Find(".bit-col-con").GetAttribute("style"));

        // A cascaded CollapsedSize is a peek like one set on the collapse itself.
        Assert.IsTrue(root.ClassList.Contains("bit-col-pek"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedDirectionalDurations()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            ExpandDuration = 900,
            CollapseDuration = 200,
        }, builder => builder.AddAttribute(1, nameof(BitCollapse.Expanded), true));

        Assert.IsTrue((component.Find(".bit-col").GetAttribute("style") ?? string.Empty).Contains("--bit-col-dur-full:900ms"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedRendering()
    {
        var component = RenderWithParams(new BitCollapseParams { LazyRender = true });

        // A cascaded LazyRender keeps the content of a collapse that was never opened out of the DOM.
        Assert.IsFalse(component.Find(".bit-col-wrp").InnerHtml.Contains("Hello"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedHiddenUntilFound()
    {
        var component = RenderWithParams(new BitCollapseParams { HiddenUntilFound = true });

        Assert.AreEqual("until-found", component.Find(".bit-col-con").GetAttribute("hidden"));
        Assert.IsTrue(component.Find(".bit-col").ClassList.Contains("bit-col-huf"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedRole()
    {
        var component = RenderWithParams(new BitCollapseParams { Role = "group" });

        Assert.AreEqual("group", component.Find(".bit-col-con").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCollapseShouldApplyAnEmptyCascadedRole()
    {
        var component = RenderWithParams(new BitCollapseParams { Role = string.Empty });

        Assert.IsNull(component.Find(".bit-col-con").GetAttribute("role"));
    }

    [TestMethod]
    public void BitCollapseShouldUnmountThroughTheCascade()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            UnmountOnCollapse = true,
            NoAnimation = true,
        }, builder => builder.AddAttribute(1, nameof(BitCollapse.Expanded), true));

        Assert.IsTrue(component.Find(".bit-col-wrp").InnerHtml.Contains("Hello"));

        var collapse = component.FindComponent<BitCollapse>();

        collapse.Render(parameters => parameters.Add(p => p.Expanded, false));

        Thread.Sleep(100);

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-col-wrp").InnerHtml.Contains("Hello")),
                                   TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void BitCollapseShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            Class = "cascaded-class",
            Style = "margin: 4px;",
            Dir = BitDir.Rtl,
        });

        var root = component.Find(".bit-col");

        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue((root.GetAttribute("style") ?? string.Empty).Contains("margin: 4px;"));
    }

    [TestMethod]
    public void BitCollapseDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitCollapseParams
        {
            Background = BitColorKind.Secondary,
            Duration = 450,
            NoPadding = true,
            Horizontal = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCollapse.Background), BitColorKind.Tertiary);
            builder.AddAttribute(2, nameof(BitCollapse.Duration), 900);
            builder.AddAttribute(3, nameof(BitCollapse.NoPadding), false);
        });

        var root = component.Find(".bit-col");
        var style = root.GetAttribute("style") ?? string.Empty;

        // Direct parameters win over the cascaded ones, false included.
        Assert.IsTrue(root.ClassList.Contains("bit-col-tbg"));
        Assert.IsFalse(root.ClassList.Contains("bit-col-sbg"));
        Assert.IsTrue(style.Contains("--bit-col-dur-full:900ms"));
        Assert.IsFalse(style.Contains("--bit-col-dur-full:450ms"));
        Assert.IsFalse(root.ClassList.Contains("bit-col-npd"));

        // What the collapse left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-col-hor"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepItsDefaultsWithoutCascadingParameters()
    {
        var component = RenderComponent<BitCollapse>(parameters => parameters.AddChildContent("Hello"));

        var instance = component.Instance;

        Assert.IsNull(instance.CascadingParameters);
        Assert.IsNull(instance.Background);
        Assert.IsNull(instance.Duration);
        Assert.IsFalse(instance.Horizontal);
        Assert.IsFalse(instance.ExpandOnPrint);
    }

    [TestMethod]
    public void BitCollapseParamsShouldLeaveUnsetValuesAlone()
    {
        var component = RenderWithParams(new BitCollapseParams());

        var instance = component.FindComponent<BitCollapse>().Instance;
        var root = component.Find(".bit-col");

        Assert.IsNull(instance.Background);
        Assert.IsNull(instance.Duration);
        Assert.IsNull(instance.CollapsedSize);
        Assert.IsNull(instance.Role);
        Assert.IsFalse(instance.Horizontal);
        Assert.IsFalse(instance.LazyRender);
        Assert.IsFalse(instance.NoPadding);
        Assert.AreEqual("region", component.Find(".bit-col-con").GetAttribute("role"));
        Assert.IsFalse(root.ClassList.Contains("bit-col-hor"));
        Assert.IsFalse(root.ClassList.Contains("bit-col-npd"));
    }

    [TestMethod]
    public void BitCollapseShouldNotCascadeTheState()
    {
        // The state is what each section holds for itself, so BitCollapseParams offers nothing to cascade it with.
        Assert.IsNull(typeof(BitCollapseParams).GetProperty(nameof(BitCollapse.Expanded)));
        Assert.IsNull(typeof(BitCollapseParams).GetProperty(nameof(BitCollapse.DefaultExpanded)));
        Assert.IsNull(typeof(BitCollapseParams).GetProperty(nameof(BitCollapse.LabelledBy)));
    }
}
