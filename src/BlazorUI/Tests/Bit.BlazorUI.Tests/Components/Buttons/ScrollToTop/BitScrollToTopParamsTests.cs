using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ScrollToTop;

/// <summary>
/// The BitParams cascade of the scroll to top button: what a BitScrollToTopParams carries (what the buttons of an app agree
/// on), that the button takes it, and that what is set on the button itself wins over it.
/// </summary>
[TestClass]
public class BitScrollToTopParamsTests : BunitTestContext
{
    // What one button says: its words.
    private static readonly string[] _notCascaded =
    [
        nameof(BitScrollToTop.CascadingParameters),
        nameof(BitScrollToTop.Text),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitScrollToTopParams scrollToTopParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { scrollToTopParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitScrollToTop>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitScrollToTopParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitScrollToTop", BitScrollToTopParams.ParamName);
        Assert.AreEqual(BitScrollToTopParams.ParamName, new BitScrollToTopParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitScrollToTopParams());
    }

    [TestMethod]
    public void BitScrollToTopParamsShouldCarryEveryParameterButTheWordsOfOneButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitScrollToTop).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitScrollToTopParams).GetProperty(name), $"BitScrollToTopParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitScrollToTopParams).GetProperty(name), $"BitScrollToTopParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitScrollToTopShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitScrollToTopParams
        {
            Class = "cascaded-root",
            Float = true,
            FloatOffset = "2rem",
            FloatPosition = BitPosition.BottomLeft,
            IconName = "DoubleChevronUp",
            RevealOffset = 400,
            ScrollTarget = "#pane",
            Smooth = false,
            Classes = new() { Icon = "cascaded-icon" },
            Styles = new() { Icon = "color:red" },
        });

        var root = component.Find(".bit-stt");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-stt-ffx"));
        Assert.IsTrue(root.ClassList.Contains("bit-stt-blf"));
        Assert.IsTrue(root.ClassList.Contains("bit-stt-rvl"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-stt-flo:2rem");
        Assert.AreEqual("400", root.GetAttribute("data-bit-stt-offset"));
        Assert.AreEqual("#pane", root.GetAttribute("data-bit-stt-target"));
        Assert.AreEqual("instant", root.GetAttribute("data-bit-stt"));

        var icon = component.Find(".bit-stt-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--DoubleChevronUp"));
        Assert.IsTrue(icon.ClassList.Contains("cascaded-icon"));
        Assert.AreEqual("color:red", icon.GetAttribute("style"));
    }

    [TestMethod]
    public void BitScrollToTopShouldTakeACascadedFloatAbsoluteAndIcon()
    {
        var component = RenderWithParams(new BitScrollToTopParams { FloatAbsolute = true, Icon = BitIconInfo.Fa("solid arrow-up") });

        Assert.IsTrue(component.Find(".bit-stt").ClassList.Contains("bit-stt-fab"));
        Assert.IsTrue(component.Find(".bit-stt-ico").ClassList.Contains("fa-arrow-up"));
    }

    [TestMethod]
    public void BitScrollToTopShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitScrollToTopParams { RevealOffset = 400, Smooth = false, IconName = "DoubleChevronUp" }, builder =>
        {
            builder.AddComponentParameter(1, nameof(BitScrollToTop.RevealOffset), (int?)100);
            builder.AddComponentParameter(2, nameof(BitScrollToTop.Smooth), true);
        });

        var root = component.Find(".bit-stt");

        Assert.AreEqual("100", root.GetAttribute("data-bit-stt-offset"));
        Assert.AreEqual("smooth", root.GetAttribute("data-bit-stt"));
        Assert.IsTrue(component.Find(".bit-stt-ico").ClassList.Contains("bit-icon--DoubleChevronUp"), "the cascaded value is taken where the button sets none");
    }
}
