using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Labels;

/// <summary>
/// Covers the BitParams cascade of the Label: what a BitLabelParams fills in, what it leaves alone because the label
/// wrote it for itself, and that what the label derives from its parameters (its element, its classes, its styles and
/// its indicators) follows the cascaded values.
/// </summary>
[TestClass]
public class BitLabelParamsTests : BunitTestContext
{
    // What a label is about rather than how it looks - its content, the control it names and the markup put in its
    // indicators - is written per label, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitLabel.CascadingParameters),
        nameof(BitLabel.ChildContent),
        nameof(BitLabel.For),
        nameof(BitLabel.OptionalTemplate),
        nameof(BitLabel.RequiredTemplate),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitLabelParams labelParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { labelParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitLabel>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Caption")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitLabelParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitLabel)}", BitLabelParams.ParamName);
        Assert.AreEqual("BitParams.BitLabel", BitLabelParams.ParamName);
    }

    [TestMethod]
    public void BitLabelParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitLabelParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitLabelParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitLabelParamsShouldCarryEveryParameterThatIsNotPerLabel()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitLabel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitLabelParams).GetProperty(name), $"BitLabelParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedClasses()
    {
        var component = RenderWithParams(new BitLabelParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            NoWrap = true,
            NoSelect = true,
            VisuallyHidden = true,
            Classes = new() { Root = "cascaded-root" },
        });

        var root = component.Find(".bit-lbl");

        Assert.IsTrue(root.ClassList.Contains("bit-lbl-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-nwr"));
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-nsl"));
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-vhd"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedStyles()
    {
        var component = RenderWithParams(new BitLabelParams
        {
            Required = true,
            Styles = new() { Root = "font-style: italic;", RequiredIndicator = "color: blueviolet;" },
        });

        Assert.AreEqual("font-style: italic;", component.Find(".bit-lbl").GetAttribute("style"));
        Assert.AreEqual("color: blueviolet;", component.Find(".bit-lbl-rqi").GetAttribute("style"));
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedElement()
    {
        var component = RenderWithParams(new BitLabelParams { Element = "legend" });

        Assert.AreEqual("LEGEND", component.Find(".bit-lbl").TagName);
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedRequiredIndicator()
    {
        var component = RenderWithParams(new BitLabelParams { Required = true, RequiredText = "(required)" });

        var root = component.Find(".bit-lbl");
        var indicator = component.Find(".bit-lbl-rqi");

        Assert.IsTrue(root.ClassList.Contains("bit-lbl-req"));
        Assert.AreEqual("(required)", indicator.TextContent);
        // A word put in place of the asterisk is meant to be read, whether the label or the cascade wrote it.
        Assert.IsFalse(indicator.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedOptionalIndicator()
    {
        var component = RenderWithParams(new BitLabelParams { Optional = true, OptionalText = "(if any)" });

        Assert.IsTrue(component.Find(".bit-lbl").ClassList.Contains("bit-lbl-opt"));
        Assert.AreEqual("(if any)", component.Find(".bit-lbl-opi").TextContent);
    }

    [TestMethod]
    public void BitLabelShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitLabelParams { Dir = BitDir.Rtl, IsEnabled = false, Class = "cascaded" });

        var root = component.Find(".bit-lbl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
    }

    [TestMethod]
    public void BitLabelShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitLabelParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            Element = "div",
            Required = true,
            OptionalText = "(cascaded)",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitLabel.Color), (BitColor?)BitColor.Success);
            builder.AddAttribute(2, nameof(BitLabel.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitLabel.Element), "label");
            builder.AddAttribute(4, nameof(BitLabel.Required), false);
            builder.AddAttribute(5, nameof(BitLabel.Optional), true);
            builder.AddAttribute(6, nameof(BitLabel.OptionalText), "(own)");
        });

        var root = component.Find(".bit-lbl");

        Assert.AreEqual("LABEL", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-lbl-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-lbl-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-lbl-lg"));

        // Required written as false by the label opts it out of the cascaded Required, so its Optional shows.
        Assert.AreEqual(0, component.FindAll(".bit-lbl-rqi").Count);
        Assert.AreEqual("(own)", component.Find(".bit-lbl-opi").TextContent);
    }

    [TestMethod]
    public void BitLabelShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitLabelParams());

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore>Caption</label>");
    }
}
