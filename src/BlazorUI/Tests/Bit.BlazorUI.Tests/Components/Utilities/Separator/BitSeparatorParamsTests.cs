using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

/// <summary>
/// Covers the BitParams cascade of the Separator: what a BitSeparatorParams fills in, what it leaves alone because the
/// separator wrote it for itself, and that what the separator derives from its parameters (its element, its role, its
/// classes and its styles) follows the cascaded values.
/// </summary>
[TestClass]
public class BitSeparatorParamsTests : BunitTestContext
{
    // The content is what a separator is about rather than how it looks, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitSeparator.CascadingParameters),
        nameof(BitSeparator.ChildContent),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitSeparatorParams separatorParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { separatorParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSeparator>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitSeparator.ChildContent), (RenderFragment)(b => b.AddContent(0, "Caption")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitSeparatorParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSeparator)}", BitSeparatorParams.ParamName);
        Assert.AreEqual("BitParams.BitSeparator", BitSeparatorParams.ParamName);
    }

    [TestMethod]
    public void BitSeparatorParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSeparatorParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSeparatorParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSeparatorParamsShouldCarryEveryParameterThatIsNotContent()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitSeparator).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitSeparatorParams).GetProperty(name), $"BitSeparatorParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldApplyCascadedClasses()
    {
        var component = RenderWithParams(new BitSeparatorParams
        {
            AlignContent = BitSeparatorAlignContent.Start,
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Tertiary,
            Color = BitColor.Error,
            LineStyle = BitSeparatorLineStyle.Dashed,
            Size = BitSize.Large,
            Vertical = true,
            Classes = new() { Root = "cascaded-root", Content = "cascaded-content" },
        });

        var root = component.Find(".bit-spr");

        Assert.IsTrue(root.ClassList.Contains("bit-spr-srt"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-bsg"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-btr"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-dsh"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-vrt"));
        Assert.IsFalse(root.ClassList.Contains("bit-spr-hrz"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-spr-cnt").ClassList.Contains("cascaded-content"));
        Assert.AreEqual("vertical", root.GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitSeparatorShouldApplyCascadedStyles()
    {
        var component = RenderWithParams(new BitSeparatorParams
        {
            AutoSize = true,
            Thickness = "3px",
            ContentOffset = "2rem",
            Inset = "1rem 0",
            Styles = new() { Root = "margin: 1rem;", Content = "color: red;" },
        });

        var style = component.Find(".bit-spr").GetAttribute("style");

        StringAssert.Contains(style, "margin: 1rem;");
        StringAssert.Contains(style, "width:auto");
        StringAssert.Contains(style, "--bit-spr-siz:3px");
        StringAssert.Contains(style, "--bit-spr-ofs:2rem");
        StringAssert.Contains(style, "--bit-spr-ins:1rem 0");
        Assert.AreEqual("color: red;", component.Find(".bit-spr-cnt").GetAttribute("style"));
    }

    [TestMethod]
    public void BitSeparatorShouldApplyCascadedElementAndDecorative()
    {
        var component = RenderWithParams(new BitSeparatorParams { Element = "li", Decorative = true });

        var root = component.Find(".bit-spr");

        Assert.AreEqual("LI", root.TagName);
        Assert.AreEqual("none", root.GetAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitSeparatorShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitSeparatorParams { Dir = BitDir.Rtl, IsEnabled = false, Class = "cascaded" });

        var root = component.Find(".bit-spr");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
    }

    [TestMethod]
    public void BitSeparatorShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitSeparatorParams
        {
            Color = BitColor.Error,
            LineStyle = BitSeparatorLineStyle.Dashed,
            Vertical = true,
            Decorative = true,
            Element = "li",
            Thickness = "3px",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSeparator.Color), (BitColor?)BitColor.Success);
            builder.AddAttribute(2, nameof(BitSeparator.LineStyle), (BitSeparatorLineStyle?)BitSeparatorLineStyle.Solid);
            builder.AddAttribute(3, nameof(BitSeparator.Vertical), false);
            builder.AddAttribute(4, nameof(BitSeparator.Decorative), false);
            builder.AddAttribute(5, nameof(BitSeparator.Element), "section");
            builder.AddAttribute(6, nameof(BitSeparator.Thickness), "1px");
        });

        var root = component.Find(".bit-spr");

        Assert.AreEqual("SECTION", root.TagName);
        Assert.AreEqual("separator", root.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-spr-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-sld"));
        Assert.IsFalse(root.ClassList.Contains("bit-spr-dsh"));
        Assert.IsTrue(root.ClassList.Contains("bit-spr-hrz"));
        Assert.AreEqual("--bit-spr-siz:1px", root.GetAttribute("style"));
    }

    [TestMethod]
    public void BitSeparatorShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitSeparatorParams());

        var contentId = $"{component.FindComponent<BitSeparator>().Instance.UniqueId}-cnt";

        component.MarkupMatches(@$"<div role=""separator"" aria-labelledby=""{contentId}"" class=""bit-spr bit-spr-hrz bit-spr-ctr"" id:ignore>
                                      <div id=""{contentId}"" class=""bit-spr-cnt"">Caption</div>
                                  </div>");
    }
}
