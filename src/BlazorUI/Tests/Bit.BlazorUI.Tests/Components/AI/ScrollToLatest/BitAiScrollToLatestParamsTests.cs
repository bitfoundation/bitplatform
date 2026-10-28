using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ScrollToLatest;

/// <summary>
/// Covers the BitParams cascade of the BitAiScrollToLatest: what a BitAiScrollToLatestParams fills in, and what it
/// leaves alone because the button set it for itself.
/// </summary>
[TestClass]
public class BitAiScrollToLatestParamsTests : BitAiTestBase
{
    // What makes one button the one it is: whether it is shown, and what pressing it does.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiScrollToLatest.CascadingParameters),
        nameof(BitAiScrollToLatest.OnClick),
        nameof(BitAiScrollToLatest.Visible),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiScrollToLatestParams buttonParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { buttonParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiScrollToLatest>(0);
                builder.AddAttribute(1, nameof(BitAiScrollToLatest.Visible), true);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAiScrollToLatestParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiScrollToLatest", BitAiScrollToLatestParams.ParamName);
        Assert.AreEqual(BitAiScrollToLatestParams.ParamName, new BitAiScrollToLatestParams().Name);
    }

    [TestMethod]
    public void BitAiScrollToLatestParamsShouldCarryEveryParameterThatBelongsToAGroupOfButtons()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiScrollToLatest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                    .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                    .Select(p => p.Name)
                                                    .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                    .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiScrollToLatestParams).GetProperty(name), $"BitAiScrollToLatestParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiScrollToLatestParams
        {
            Class = "cascaded",
            Label = "Newest",
        });

        Assert.IsTrue(component.Find(".bit-aistl").ClassList.Contains("cascaded"));
        Assert.AreEqual("Newest", component.Find("button").TextContent.Trim());
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAiScrollToLatestParams
        {
            Class = "cascaded",
            Label = "Newest",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitAiScrollToLatest.Label), "Latest");
        });

        Assert.IsTrue(component.Find(".bit-aistl").ClassList.Contains("own"));
        Assert.IsFalse(component.Find(".bit-aistl").ClassList.Contains("cascaded"));
        Assert.AreEqual("Latest", component.Find("button").TextContent.Trim());
    }
}
