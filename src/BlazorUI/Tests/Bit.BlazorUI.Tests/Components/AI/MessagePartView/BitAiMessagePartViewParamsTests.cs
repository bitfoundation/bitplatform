using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessagePartView;

/// <summary>
/// Covers the BitParams cascade of the BitAiMessagePartView: what a BitAiMessagePartViewParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiMessagePartViewParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiMessagePartView.CascadingParameters),
        nameof(BitAiMessagePartView.Part),
        nameof(BitAiMessagePartView.Message),
        nameof(BitAiMessagePartView.Conversation),
        nameof(BitAiMessagePartView.Session),
        nameof(BitAiMessagePartView.OnCitationClick),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiMessagePartViewParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiMessagePartView>(0);
                var call = new BitAiToolCallPart("c1", "search");
                builder.AddAttribute(1, nameof(BitAiMessagePartView.Part), call);
                builder.AddAttribute(2, nameof(BitAiMessagePartView.Message), Assistant(call));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiMessagePartViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiMessagePartView", BitAiMessagePartViewParams.ParamName);
        Assert.AreEqual(BitAiMessagePartViewParams.ParamName, new BitAiMessagePartViewParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiMessagePartViewParams());
    }

    [TestMethod]
    public void BitAiMessagePartViewParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiMessagePartView).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiMessagePartViewParams).GetProperty(name), $"BitAiMessagePartViewParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiMessagePartViewShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiMessagePartViewParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-aimpv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiMessagePartViewShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiMessagePartViewParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiMessagePartView.Class), "own");
            builder.AddAttribute(11, nameof(BitAiMessagePartView.Classes), new BitAiMessagePartViewClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-aimpv");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
