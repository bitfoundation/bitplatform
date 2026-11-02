using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ErrorView;

/// <summary>
/// Covers the BitParams cascade of the BitAiErrorView: what a BitAiErrorViewParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiErrorViewParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiErrorView.CascadingParameters),
        nameof(BitAiErrorView.Error),
        nameof(BitAiErrorView.Compact),
        nameof(BitAiErrorView.OnRetry),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiErrorViewParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiErrorView>(0);
                builder.AddAttribute(1, nameof(BitAiErrorView.Error), new BitAiError("boom"));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiErrorViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiErrorView", BitAiErrorViewParams.ParamName);
        Assert.AreEqual(BitAiErrorViewParams.ParamName, new BitAiErrorViewParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiErrorViewParams());
    }

    [TestMethod]
    public void BitAiErrorViewParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiErrorView).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiErrorViewParams).GetProperty(name), $"BitAiErrorViewParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiErrorViewShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiErrorViewParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-aierv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiErrorViewShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiErrorViewParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiErrorView.Class), "own");
            builder.AddAttribute(11, nameof(BitAiErrorView.Classes), new BitAiErrorViewClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-aierv");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
