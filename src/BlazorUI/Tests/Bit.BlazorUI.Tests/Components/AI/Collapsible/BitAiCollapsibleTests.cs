using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Collapsible;

[TestClass]
public class BitAiCollapsibleTests : BitAiTestBase
{
    [TestMethod]
    public void TheHeaderIsAButtonThatControlsTheBody()
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Reasoning")
            .Add(c => c.Open, true)
            .AddChildContent("<p class='body'>steps</p>"));

        var header = panel.Find("button");

        Assert.AreEqual("button", header.GetAttribute("type"));
        Assert.AreEqual("true", header.GetAttribute("aria-expanded"));
        StringAssert.Contains(header.TextContent, "Reasoning");
        Assert.AreEqual("steps", panel.Find($"[id='{header.GetAttribute("aria-controls")}'] .body").TextContent);
    }

    [TestMethod]
    public void TheBodyIsRenderedOnlyWhileOpen()
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Details")
            .AddChildContent("<p class='body'>steps</p>"));

        Assert.AreEqual("false", panel.Find("button").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, panel.FindAll(".body").Count);

        panel.Find("button").Click();

        Assert.AreEqual("true", panel.Find("button").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, panel.FindAll(".body").Count);
    }

    [TestMethod]
    public void TogglingReportsTheNewStateThroughOpenChanged()
    {
        var reported = new List<bool>();
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Details")
            .Add(c => c.OpenChanged, v => reported.Add(v)));

        panel.Find("button").Click();
        panel.Find("button").Click();

        CollectionAssert.AreEqual(new[] { true, false }, reported);
        Assert.IsFalse(panel.Instance.Open);
    }

    [TestMethod]
    public void TheHeaderTemplateReplacesTheTitleAndTheSuffixTemplateSitsBesideTheToggle()
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Ignored")
            .Add(c => c.HeaderTemplate, b => b.AddMarkupContent(0, "<span class='custom-header'>Thought for 3s</span>"))
            .Add(c => c.SuffixTemplate, b => b.AddMarkupContent(0, "<span class='chip'>done</span>")));

        var header = panel.Find("button");

        Assert.IsNotNull(header.QuerySelector(".custom-header"));
        Assert.IsFalse(header.TextContent.Contains("Ignored"));
        // Outside the button, so it is not read as part of the name of the toggle.
        Assert.IsNull(header.QuerySelector(".chip"));
        Assert.AreEqual(1, panel.FindAll(".chip").Count);
    }

    [TestMethod]
    public void ADisabledPanelDoesNotToggle()
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Details")
            .Add(c => c.Disabled, true));

        panel.Find("button").Click();

        Assert.AreEqual("false", panel.Find("button").GetAttribute("aria-expanded"));
        Assert.IsTrue(panel.Find(".bit-aiclp").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitAiCollapsibleShouldRenderTheRootClassAndPartClassStyles()
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p
            .Add(c => c.Title, "Details")
            .Add(c => c.Id, "panel")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Classes, new BitAiCollapsibleClassStyles { Root = "root-class", Accordion = new() { Root = "accordion-class" } })
            .Add(c => c.Styles, new BitAiCollapsibleClassStyles { Root = "padding: 2px" }));

        var root = panel.Find(".bit-aiclp");

        Assert.AreEqual("panel", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding: 2px");
        Assert.IsTrue(panel.Find(".bit-acd").ClassList.Contains("accordion-class"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiCollapsibleShouldRespectDir(BitDir? dir, string? expected)
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p.Add(c => c.Dir, dir));

        var root = panel.Find(".bit-aiclp");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiCollapsibleShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var panel = RenderComponent<BitAiCollapsible>(p => p.Add(c => c.Visibility, visibility));

        var style = panel.Find(".bit-aiclp").GetAttribute("style") ?? string.Empty;

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
    public void BitAiCollapsibleParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiCollapsible", BitAiCollapsibleParams.ParamName);
        Assert.AreEqual(BitAiCollapsibleParams.ParamName, new BitAiCollapsibleParams().Name);
    }

    [TestMethod]
    public void BitAiCollapsibleParamsShouldCarryEveryParameterThatBelongsToAGroupOfPanels()
    {
        string[] notCascaded =
        [
            nameof(BitAiCollapsible.CascadingParameters),
            nameof(BitAiCollapsible.CascadedOptions),
            nameof(BitAiCollapsible.ChildContent),
            nameof(BitAiCollapsible.HeaderTemplate),
            nameof(BitAiCollapsible.Open),
            nameof(BitAiCollapsible.OpenChanged),
            nameof(BitAiCollapsible.SuffixTemplate),
            nameof(BitAiCollapsible.Title),
        ];
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiCollapsible).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && notCascaded.Contains(n) is false)
                                                 .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiCollapsibleParams).GetProperty(name), $"BitAiCollapsibleParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiCollapsibleShouldTakeTheCascadedValues()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiCollapsibleParams { Class = "cascaded", Classes = new() { Accordion = new() { Root = "cascaded-accordion" } } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiCollapsible>(0);
                builder.AddAttribute(1, nameof(BitAiCollapsible.Title), "Details");
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-aiclp").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-acd").ClassList.Contains("cascaded-accordion"));
    }
}
