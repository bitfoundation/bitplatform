using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolResultView;

/// <summary>
/// Covers the BitParams cascade of the BitAiToolResultView: what a BitAiToolResultViewParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiToolResultViewParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiToolResultView.CascadingParameters),
        nameof(BitAiToolResultView.Part),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiToolResultViewParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiToolResultView>(0);
                builder.AddAttribute(1, nameof(BitAiToolResultView.Part), new BitAiToolResultPart("c1", BitAiToolOutcome.FromText("x")));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiToolResultViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiToolResultView", BitAiToolResultViewParams.ParamName);
        Assert.AreEqual(BitAiToolResultViewParams.ParamName, new BitAiToolResultViewParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiToolResultViewParams());
    }

    [TestMethod]
    public void BitAiToolResultViewParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiToolResultView).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiToolResultViewParams).GetProperty(name), $"BitAiToolResultViewParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiToolResultViewShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiToolResultViewParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-aitrv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiToolResultViewShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiToolResultViewParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiToolResultView.Class), "own");
            builder.AddAttribute(11, nameof(BitAiToolResultView.Classes), new BitAiToolResultViewClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-aitrv");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
