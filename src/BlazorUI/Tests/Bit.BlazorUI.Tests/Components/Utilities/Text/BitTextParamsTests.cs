using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

/// <summary>
/// Covers the BitParams cascade of the Text: what a BitTextParams fills in, what it leaves alone because the text
/// wrote it for itself, and that what the text derives from its parameters (its element, its classes, its inline
/// style and its heading semantics) follows the cascaded values. The razor helper covers the typography, the color
/// and the alignment; this file covers every switch and every enum the helper leaves out.
/// </summary>
[TestClass]
public class BitTextParamsTests : BunitTestContext
{
    // What a text says is written per text, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitText.CascadingParameters),
        nameof(BitText.ChildContent),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitTextParams textParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { textParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitText>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitText.ChildContent), (RenderFragment)(b => b.AddContent(0, "Body")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTextParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitText)}", BitTextParams.ParamName);
        Assert.AreEqual("BitParams.BitText", BitTextParams.ParamName);
    }

    [TestMethod]
    public void BitTextParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitTextParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTextParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTextParamsShouldCarryEveryParameterThatIsNotContent()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitText).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                        .Select(p => p.Name)
                                        .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTextParams).GetProperty(name), $"BitTextParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTextShouldApplyEveryCascadedSwitch()
    {
        var component = RenderWithParams(new BitTextParams
        {
            Block = true,
            BreakWord = true,
            ForceBreak = true,
            Gutter = true,
            Hyphenate = true,
            Italic = true,
            Monospace = true,
            NoSelect = true,
            NoWrap = true,
            Numeric = true,
            PreserveWhitespace = true,
            Strikethrough = true,
            Underline = true,
            VisuallyHidden = true,
        });

        var classList = component.Find(".bit-txt").ClassList;

        foreach (var expected in new[]
        {
            "bit-txt-blk", "bit-txt-brw", "bit-txt-fbr", "bit-txt-gutter", "bit-txt-hyp", "bit-txt-itl", "bit-txt-mno",
            "bit-txt-nsl", "bit-txt-nowrap", "bit-txt-num", "bit-txt-pws", "bit-txt-stk", "bit-txt-und", "bit-txt-vhd",
        })
        {
            Assert.IsTrue(classList.Contains(expected), $"the {expected} class is missing");
        }
    }

    [TestMethod]
    public void BitTextShouldApplyTheCascadedTransformWrapAndWeight()
    {
        var component = RenderWithParams(new BitTextParams
        {
            Transform = BitTextTransform.Uppercase,
            Wrap = BitTextWrap.Balance,
            Weight = BitFontWeight.Semibold,
            Trim = BitTextTrim.End,
        });

        var classList = component.Find(".bit-txt").ClassList;

        Assert.IsTrue(classList.Contains("bit-txt-tru"));
        Assert.IsTrue(classList.Contains("bit-txt-wbl"));
        Assert.IsTrue(classList.Contains("bit-txt-fws"));
        Assert.IsTrue(classList.Contains("bit-txt-tme"));
    }

    [TestMethod]
    public void BitTextShouldApplyTheCascadedTypographyAndElement()
    {
        var component = RenderWithParams(new BitTextParams { Typography = BitTypography.H2 });

        // The element follows the cascaded typography the way it follows the text's own.
        Assert.AreEqual("H2", component.Find(".bit-txt").TagName);
        Assert.IsTrue(component.Find(".bit-txt").ClassList.Contains("bit-txt-h2"));

        component = RenderWithParams(new BitTextParams { Typography = BitTypography.H2, Element = "span" });

        Assert.AreEqual("SPAN", component.Find(".bit-txt").TagName);
    }

    [TestMethod]
    public void BitTextShouldApplyTheCascadedInlineStyles()
    {
        var component = RenderWithParams(new BitTextParams
        {
            Align = BitTextAlign.Justify,
            Gradient = "linear-gradient(red, blue)",
            LineClamp = 3,
        });

        var root = component.Find(".bit-txt");

        Assert.IsTrue(root.ClassList.Contains("bit-txt-grd"));
        Assert.IsTrue(root.ClassList.Contains("bit-txt-clp"));
        Assert.AreEqual("text-align:justify;background-image:linear-gradient(red, blue);-webkit-line-clamp:3;line-clamp:3", root.GetAttribute("style"));
    }

    [TestMethod]
    public void BitTextShouldApplyTheCascadedHeadingSemanticsAndLanguage()
    {
        var component = RenderWithParams(new BitTextParams { AriaLevel = 3, Lang = "fa" });

        var root = component.Find(".bit-txt");

        Assert.AreEqual("heading", root.GetAttribute("role"));
        Assert.AreEqual("3", root.GetAttribute("aria-level"));
        Assert.AreEqual("fa", root.GetAttribute("lang"));
    }

    [TestMethod]
    public void BitTextShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitTextParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded", Style = "margin:0", TabIndex = "-1" });

        var root = component.Find(".bit-txt");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("margin:0", root.GetAttribute("style"));
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitTextShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTextParams
        {
            NoWrap = true,
            Italic = true,
            Transform = BitTextTransform.Uppercase,
            Wrap = BitTextWrap.Balance,
            LineClamp = 3,
            AriaLevel = 3,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitText.NoWrap), false);
            builder.AddAttribute(2, nameof(BitText.Transform), (BitTextTransform?)BitTextTransform.Lowercase);
            builder.AddAttribute(3, nameof(BitText.Wrap), (BitTextWrap?)BitTextWrap.Pretty);
            builder.AddAttribute(4, nameof(BitText.LineClamp), (int?)1);
            builder.AddAttribute(5, nameof(BitText.AriaLevel), (int?)5);
        });

        var root = component.Find(".bit-txt");
        var classList = root.ClassList;

        // A false written by the text is a value the cascade must not overwrite...
        Assert.IsFalse(classList.Contains("bit-txt-nowrap"));
        Assert.IsTrue(classList.Contains("bit-txt-trl"));
        Assert.IsFalse(classList.Contains("bit-txt-tru"));
        Assert.IsTrue(classList.Contains("bit-txt-wpr"));
        Assert.IsFalse(classList.Contains("bit-txt-wbl"));
        Assert.AreEqual("-webkit-line-clamp:1;line-clamp:1", root.GetAttribute("style"));
        Assert.AreEqual("5", root.GetAttribute("aria-level"));

        // ...while what it did not set is still filled in from them.
        Assert.IsTrue(classList.Contains("bit-txt-itl"));
    }

    [TestMethod]
    public void BitTextShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitTextParams());

        component.MarkupMatches(@"<div class=""bit-txt bit-txt-subtitle1"" id:ignore>Body</div>");
    }
}
