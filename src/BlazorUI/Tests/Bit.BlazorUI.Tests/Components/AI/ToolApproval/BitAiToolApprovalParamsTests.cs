using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolApproval;

/// <summary>
/// Covers the BitParams cascade of the BitAiToolApproval: what a BitAiToolApprovalParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiToolApprovalParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiToolApproval.CascadingParameters),
        nameof(BitAiToolApproval.Call),
        nameof(BitAiToolApproval.Session),
        nameof(BitAiToolApproval.OnDecision),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiToolApprovalParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiToolApproval>(0);
                builder.AddAttribute(1, nameof(BitAiToolApproval.Call), new BitAiToolCallPart("c1", "search"));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiToolApprovalParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiToolApproval", BitAiToolApprovalParams.ParamName);
        Assert.AreEqual(BitAiToolApprovalParams.ParamName, new BitAiToolApprovalParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiToolApprovalParams());
    }

    [TestMethod]
    public void BitAiToolApprovalParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiToolApproval).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiToolApprovalParams).GetProperty(name), $"BitAiToolApprovalParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiToolApprovalShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiToolApprovalParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-aitap");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiToolApprovalShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiToolApprovalParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiToolApproval.Class), "own");
            builder.AddAttribute(11, nameof(BitAiToolApproval.Classes), new BitAiToolApprovalClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-aitap");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }
}
