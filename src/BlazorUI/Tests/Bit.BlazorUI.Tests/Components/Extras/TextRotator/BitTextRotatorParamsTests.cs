using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextRotator;

/// <summary>
/// Covers the BitParams cascade of the TextRotator: what a BitTextRotatorParams fills in, and what it leaves alone
/// because the rotator wrote it for itself.
/// </summary>
[TestClass]
public class BitTextRotatorParamsTests : BunitTestContext
{
    // What belongs to a single rotator rather than to a group of them: its items.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTextRotator.CascadingParameters),
        nameof(BitTextRotator.Items),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitTextRotatorParams rotatorParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { rotatorParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTextRotator>(0);
                builder.AddAttribute(1, nameof(BitTextRotator.Items), new[] { "web", "mobile", "desktop" });
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTextRotatorParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitTextRotator", BitTextRotatorParams.ParamName);
        Assert.AreEqual(BitTextRotatorParams.ParamName, new BitTextRotatorParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitTextRotatorParams());
    }

    [TestMethod]
    public void BitTextRotatorParamsShouldCarryEveryParameterThatBelongsToAGroupOfRotators()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitTextRotator).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTextRotatorParams).GetProperty(name), $"BitTextRotatorParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTextRotatorShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitTextRotatorParams
        {
            Class = "cascaded",
            Classes = new() { Item = "item-class" },
            Element = "h2",
            Interval = 1234,
            Styles = new() { CurrentItem = "font-weight:bold" },
        });

        var root = component.Find(".bit-trt");
        var rotator = component.FindComponent<BitTextRotator>().Instance;

        Assert.AreEqual("H2", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(component.FindAll(".bit-trt-itm").All(i => i.ClassList.Contains("item-class")));
        StringAssert.Contains(component.FindAll(".bit-trt-itm")[0].GetAttribute("style"), "font-weight:bold");
        Assert.AreEqual(1234d, rotator.Interval);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldBePausedByACascadedPaused()
    {
        var component = RenderWithParams(new BitTextRotatorParams { Interval = 50, Paused = true });

        await Task.Delay(300);

        Assert.IsTrue(component.FindAll(".bit-trt-itm")[0].ClassList.Contains("bit-trt-cur"));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
    }

    [TestMethod]
    public void BitTextRotatorShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTextRotatorParams { Element = "h2", Interval = 60_000, Paused = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitTextRotator.Element), "p");
            builder.AddAttribute(3, nameof(BitTextRotator.Interval), 50d);
            builder.AddAttribute(4, nameof(BitTextRotator.Paused), false);
        });

        Assert.AreEqual("P", component.Find(".bit-trt").TagName);

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count), TimeSpan.FromSeconds(5));
    }
}
