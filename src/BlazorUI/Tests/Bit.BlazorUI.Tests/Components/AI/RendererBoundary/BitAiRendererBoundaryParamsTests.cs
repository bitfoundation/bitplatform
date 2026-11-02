using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.RendererBoundary;

/// <summary>
/// Covers the BitParams cascade of the BitAiRendererBoundary: what a BitAiRendererBoundaryParams fills in, and what it leaves alone because the
/// component's own markup wrote it.
/// </summary>
[TestClass]
public class BitAiRendererBoundaryParamsTests : BitAiTestBase
{
    // What belongs to a single instance rather than to a group of them.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAiRendererBoundary.CascadingParameters),
        nameof(BitAiRendererBoundary.ChildContent),
        nameof(BitAiRendererBoundary.Label),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAiRendererBoundaryParams componentParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { componentParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiRendererBoundary>(0);
                builder.AddAttribute(1, nameof(BitAiRendererBoundary.ChildContent), (RenderFragment)(b => { b.OpenComponent<Thrower>(0); b.CloseComponent(); }));
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }



    [TestMethod]
    public void BitAiRendererBoundaryParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiRendererBoundary", BitAiRendererBoundaryParams.ParamName);
        Assert.AreEqual(BitAiRendererBoundaryParams.ParamName, new BitAiRendererBoundaryParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAiRendererBoundaryParams());
    }

    [TestMethod]
    public void BitAiRendererBoundaryParamsShouldCarryEveryParameterThatBelongsToAGroup()
    {
        var baseParameters = typeof(BitAiComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiRendererBoundary).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                       .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiRendererBoundaryParams).GetProperty(name), $"BitAiRendererBoundaryParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiRendererBoundaryShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAiRendererBoundaryParams
        {
            Class = "cascaded",
            Style = "margin:1px",
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Root = "padding:2px" },
        });

        var root = component.Find(".bit-airbd");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitAiRendererBoundaryShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderWithParams(new BitAiRendererBoundaryParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root" },
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitAiRendererBoundary.Class), "own");
            builder.AddAttribute(11, nameof(BitAiRendererBoundary.Classes), new BitAiRendererBoundaryClassStyles { Root = "own-root" });
        });

        var root = component.Find(".bit-airbd");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsTrue(root.ClassList.Contains("own-root"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-root"));
    }



    // The boundary renders a root element only in place of content that failed.
    private sealed class Thrower : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => throw new InvalidOperationException("boom");
    }
}
