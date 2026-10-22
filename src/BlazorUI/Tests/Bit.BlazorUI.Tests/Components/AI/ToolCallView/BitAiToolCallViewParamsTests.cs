using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolCallView;

/// <summary>
/// Covers the BitParams cascade of the BitAiToolCallView: what a BitAiToolCallViewParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiToolCallViewParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiToolCallView.CascadingParameters),
        nameof(BitAiToolCallView.Call),
        nameof(BitAiToolCallView.Message),
        nameof(BitAiToolCallView.Session),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiToolCallViewParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiToolCallView>(0);
                builder.AddAttribute(1, nameof(BitAiToolCallView.Call), new BitAiToolCallPart("c1", "search"));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiToolCallViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiToolCallView", BitAiToolCallViewParams.ParamName);
        Assert.AreEqual(BitAiToolCallViewParams.ParamName, new BitAiToolCallViewParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiToolCallViewParams());
    }

    [TestMethod]
    public void BitAiToolCallViewParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiToolCallView).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiToolCallViewParams).GetProperty(name), $"BitAiToolCallViewParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiToolCallViewShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiToolCallViewParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-aitcv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiToolCallViewShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiToolCallViewParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiToolCallView.Class), "own");
            builder.AddAttribute(11, nameof(BitAiToolCallView.Classes), new BitAiToolCallViewClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-aitcv");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
