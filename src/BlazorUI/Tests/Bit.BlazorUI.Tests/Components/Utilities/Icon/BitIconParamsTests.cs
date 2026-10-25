using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

/// <summary>
/// Covers the BitParams cascade of the Icon: what a BitIconParams fills in - the look, the transform, the animation,
/// the resolver of the icon set and every parameter of BitComponentBase - what it leaves alone because the icon wrote
/// it for itself, and what it deliberately never carries (the glyph itself).
/// </summary>
[TestClass]
public class BitIconParamsTests : BunitTestContext
{
    // The glyph an icon draws, its content, its name and its handler are per icon: a default glyph shared by every
    // icon of a subtree is a subtree of identical icons, which is never what was meant.
    private static readonly string[] _notCascaded =
    [
        nameof(BitIcon.CascadingParameters),
        nameof(BitIcon.ChildContent),
        nameof(BitIcon.Icon),
        nameof(BitIcon.IconName),
        nameof(BitIcon.OnClick),
        nameof(BitIcon.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitIconParams iconParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { iconParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitIcon>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitIconParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitIcon)}", BitIconParams.ParamName);
        Assert.AreEqual("BitParams.BitIcon", BitIconParams.ParamName);
    }

    [TestMethod]
    public void BitIconParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitIconParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitIconParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitIconParamsShouldCarryEveryParameterThatIsNotPerIcon()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitIcon).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                        .Select(p => p.Name)
                                        .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitIconParams).GetProperty(name), $"BitIconParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitIconParamsShouldNotCarryTheGlyph()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitIcon.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitIconParams).GetProperty(name), $"BitIconParams carries {name}, which is per icon.");
        }
    }

    [TestMethod]
    public void BitIconShouldApplyCascadedLookClasses()
    {
        var component = RenderWithParams(new BitIconParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            Variant = BitVariant.Fill,
            FixedWidth = true,
            Circular = true,
            Inline = true,
        });

        var root = component.Find(".bit-ico");

        Assert.IsTrue(root.ClassList.Contains("bit-ico-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-txt"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-fxw"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-cir"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-inl"));
    }

    [TestMethod]
    public void BitIconShouldApplyCascadedTransform()
    {
        var component = RenderWithParams(new BitIconParams
        {
            Rotate = BitIconRotate.Rotate90,
            Flip = BitIconFlip.Horizontal,
            FlipRtl = true,
        });

        var root = component.Find(".bit-ico");

        Assert.IsTrue(root.ClassList.Contains("bit-ico-rt90"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-flh"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-frt"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-trn"));
    }

    [TestMethod]
    public void BitIconShouldApplyCascadedRotateAngleAndFontSize()
    {
        var component = RenderWithParams(new BitIconParams { RotateAngle = 45, FontSize = "2rem" });

        var root = component.Find(".bit-ico");
        var style = root.GetAttribute("style");

        Assert.IsTrue(root.ClassList.Contains("bit-ico-trn"));
        StringAssert.Contains(style, "--bit-ico-rotate:45deg");
        StringAssert.Contains(style, "font-size:2rem");
    }

    [TestMethod]
    public void BitIconShouldApplyCascadedAnimation()
    {
        var component = RenderWithParams(new BitIconParams
        {
            Animation = BitIconAnimation.Fade,
            AnimationDuration = "2s",
            AnimationDelay = "500ms",
            AnimationIterationCount = 3,
        });

        var root = component.Find(".bit-ico");
        var style = root.GetAttribute("style");

        Assert.IsTrue(root.ClassList.Contains("bit-ico-fad"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-anm"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-dly"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-itr"));
        StringAssert.Contains(style, "--bit-ico-anm-dur:2s");
        StringAssert.Contains(style, "--bit-ico-anm-dly:500ms");
        StringAssert.Contains(style, "--bit-ico-anm-itr:3");
    }

    [TestMethod]
    public void BitIconShouldResolveItsNameThroughACascadedResolver()
    {
        var component = RenderWithParams(new BitIconParams { IconResolver = name => BitIconInfo.Fa($"solid {name}") }, builder =>
        {
            builder.AddAttribute(1, nameof(BitIcon.IconName), "house");
        });

        // One resolver on the params is every icon of a subtree drawn from the other set.
        var root = component.Find(".bit-ico");

        Assert.IsTrue(root.ClassList.Contains("fa-solid"));
        Assert.IsTrue(root.ClassList.Contains("fa-house"));
        Assert.IsFalse(root.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitIconShouldLetItsOwnIconWinOverACascadedResolver()
    {
        var component = RenderWithParams(new BitIconParams { IconResolver = name => BitIconInfo.Fa($"solid {name}") }, builder =>
        {
            builder.AddAttribute(1, nameof(BitIcon.IconName), "house");
            // Boxed, since BitIconInfo converts to a string and would otherwise pick that overload.
            builder.AddAttribute(2, nameof(BitIcon.Icon), (object)BitIconInfo.Bi("github"));
        });

        var root = component.Find(".bit-ico");

        Assert.IsTrue(root.ClassList.Contains("bi-github"));
        Assert.IsFalse(root.ClassList.Contains("fa-house"));
    }

    [TestMethod]
    public void BitIconShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitIconParams
        {
            AriaLabel = "Saved",
            Class = "cascaded",
            Dir = BitDir.Rtl,
            Disabled = true,
            Id = "cascaded-id",
            Style = "color: red;",
            Visibility = BitVisibility.Hidden,
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var root = component.Find(".bit-ico");

        // A cascaded label names the icon the way the parameter does: it is announced rather than hidden.
        Assert.AreEqual("Saved", root.GetAttribute("aria-label"));
        Assert.AreEqual("img", root.GetAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("cascaded-id", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void BitIconShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitIconParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            Variant = BitVariant.Fill,
            Rotate = BitIconRotate.Rotate90,
            FontSize = "2rem",
            Animation = BitIconAnimation.Spin,
            FixedWidth = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitIcon.Color), (BitColor?)BitColor.Success);
            builder.AddAttribute(2, nameof(BitIcon.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitIcon.Variant), (BitVariant?)BitVariant.Outline);
            builder.AddAttribute(4, nameof(BitIcon.Rotate), (BitIconRotate?)BitIconRotate.Rotate180);
            builder.AddAttribute(5, nameof(BitIcon.FontSize), "1rem");
            builder.AddAttribute(6, nameof(BitIcon.Animation), (BitIconAnimation?)BitIconAnimation.Beat);
            builder.AddAttribute(7, nameof(BitIcon.FixedWidth), false);
        });

        var root = component.Find(".bit-ico");

        Assert.IsTrue(root.ClassList.Contains("bit-ico-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-out"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-fil"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-rt180"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-rt90"));
        Assert.IsTrue(root.ClassList.Contains("bit-ico-bet"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-spn"));
        // A false written by the icon is a decision, so the cascaded true does not turn the box on.
        Assert.IsFalse(root.ClassList.Contains("bit-ico-fxw"));
        StringAssert.Contains(root.GetAttribute("style"), "font-size:1rem");
    }

    [TestMethod]
    public void BitIconShouldGiveBackCascadedValuesWhenTheCascadeStopsCarryingThem()
    {
        var component = RenderWithParams(new BitIconParams { Color = BitColor.Error, Animation = BitIconAnimation.Spin, FontSize = "2rem" });

        var instance = component.FindComponent<BitIcon>().Instance;

        Assert.IsTrue(component.Find(".bit-ico").ClassList.Contains("bit-ico-err"));

        // The same number of params objects, so BitParams keeps its child instead of building a new one.
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitIconParams() }));

        Assert.AreSame(instance, component.FindComponent<BitIcon>().Instance);

        var root = component.Find(".bit-ico");

        Assert.IsFalse(root.ClassList.Contains("bit-ico-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-ico-spn"));
        Assert.IsFalse(root.HasAttribute("style"));
        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Animation);
        Assert.IsNull(instance.FontSize);
    }

    [TestMethod]
    public void BitIconShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitIconParams());

        component.MarkupMatches(@"<i class=""bit-ico bit-ico-txt"" aria-hidden=""true"" id:ignore />");
    }
}
