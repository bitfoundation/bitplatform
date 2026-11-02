using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ThinkingIndicator;

[TestClass]
public class BitAiThinkingIndicatorTests : BitAiTestBase
{
    [TestMethod]
    public void TheIndicatorIsAPoliteStatusThatSaysTheModelIsThinking()
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>();

        var status = indicator.Find("[role=status]");

        Assert.AreEqual("polite", status.GetAttribute("aria-live"));
        Assert.AreEqual("Thinking", status.TextContent.Trim());
        // The dots are a drawing, hidden from assistive technologies.
        Assert.AreEqual("true", indicator.Find(".bit-ldn-bnd-ccn").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void TheLabelReplacesTheDefaultText()
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>(p => p.Add(c => c.Label, "Searching the web"));

        Assert.AreEqual("Searching the web", indicator.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void TheDotsAreAnInlineBouncingDotsLoader()
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>();

        var loader = indicator.Find(".bit-aithi > .bit-ldn");

        Assert.IsTrue(loader.ClassList.Contains("bit-ldn-bnd"));
        Assert.IsTrue(loader.ClassList.Contains("bit-ldn-inl"));
    }

    [TestMethod]
    public void BitAiThinkingIndicatorShouldRenderTheRootClassAndPartClassStyles()
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>(p => p
            .Add(c => c.Id, "thinking")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Classes, new BitAiThinkingIndicatorClassStyles { Root = "root-class", Loading = new() { Label = "label-class" } })
            .Add(c => c.Styles, new BitAiThinkingIndicatorClassStyles { Root = "padding: 2px" }));

        var root = indicator.Find(".bit-aithi");

        Assert.AreEqual("thinking", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding: 2px");
        Assert.IsTrue(indicator.Find(".bit-ldn-lbl").ClassList.Contains("label-class"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitAiThinkingIndicatorShouldRespectDisabled(bool isEnabled)
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>(p => p.Add(c => c.Disabled, isEnabled is false));

        Assert.AreEqual(isEnabled is false, indicator.Find(".bit-aithi").ClassList.Contains("bit-dis"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiThinkingIndicatorShouldRespectDir(BitDir? dir, string? expected)
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>(p => p.Add(c => c.Dir, dir));

        var root = indicator.Find(".bit-aithi");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiThinkingIndicatorShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var indicator = RenderComponent<BitAiThinkingIndicator>(p => p.Add(c => c.Visibility, visibility));

        var style = indicator.Find(".bit-aithi").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitAiThinkingIndicatorParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiThinkingIndicator", BitAiThinkingIndicatorParams.ParamName);
        Assert.AreEqual(BitAiThinkingIndicatorParams.ParamName, new BitAiThinkingIndicatorParams().Name);
    }

    [TestMethod]
    public void BitAiThinkingIndicatorParamsShouldCarryEveryParameterThatBelongsToAGroupOfIndicators()
    {
        string[] notCascaded =
        [
            nameof(BitAiThinkingIndicator.CascadingParameters),
            nameof(BitAiThinkingIndicator.CascadedOptions),
            nameof(BitAiThinkingIndicator.Label),
        ];
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiThinkingIndicator).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                       .Select(p => p.Name)
                                                       .Where(n => baseParameters.Contains(n) is false && notCascaded.Contains(n) is false)
                                                       .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiThinkingIndicatorParams).GetProperty(name), $"BitAiThinkingIndicatorParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiThinkingIndicatorShouldTakeTheCascadedValues()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiThinkingIndicatorParams { Class = "cascaded", Styles = new() { Root = "color: red" } }
            });
            parameters.AddChildContent<BitAiThinkingIndicator>();
        });

        var root = component.Find(".bit-aithi");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
    }
}
