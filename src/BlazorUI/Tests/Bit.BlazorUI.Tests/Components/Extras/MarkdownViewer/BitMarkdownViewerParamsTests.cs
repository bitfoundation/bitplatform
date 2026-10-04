using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

/// <summary>
/// Covers the BitParams cascade of the MarkdownViewer: what a BitMarkdownViewerParams fills in, what it leaves alone
/// because the viewer wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitMarkdownViewerParamsTests : BunitTestContext
{
    // What belongs to a single document rather than to a group of them: its source, its layout and its events.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMarkdownViewer.CascadingParameters),
        nameof(BitMarkdownViewer.Inline),
        nameof(BitMarkdownViewer.Markdown),
        nameof(BitMarkdownViewer.OnParsed),
        nameof(BitMarkdownViewer.OnTaskChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitMarkdownViewerParams viewerParams,
                                                           string markdown,
                                                           Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { viewerParams });
            parameters.AddChildContent(builder => BuildViewer(builder, markdown, extraAttributes));
        });
    }

    private static void BuildViewer(RenderTreeBuilder builder, string markdown, Action<RenderTreeBuilder>? extraAttributes)
    {
        builder.OpenComponent<BitMarkdownViewer>(0);
        builder.AddAttribute(1, nameof(BitMarkdownViewer.Markdown), markdown);
        extraAttributes?.Invoke(builder);
        builder.CloseComponent();
    }

    [TestMethod]
    public void BitMarkdownViewerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitMarkdownViewer", BitMarkdownViewerParams.ParamName);
        Assert.AreEqual(BitMarkdownViewerParams.ParamName, new BitMarkdownViewerParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitMarkdownViewerParams());
    }

    [TestMethod]
    public void BitMarkdownViewerParamsShouldCarryEveryParameterThatBelongsToAGroupOfDocuments()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMarkdownViewer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                  .Select(p => p.Name)
                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                  .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMarkdownViewerParams).GetProperty(name), $"BitMarkdownViewerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitMarkdownViewerParams
        {
            Class = "cascaded",
            Pipeline = BitMarkdownPipelines.GitHub,
            HeadingLevelOffset = 1,
            ImageRendering = BitMarkdownViewerImageRendering.None,
            CodeBlockTemplate = code => builder => builder.AddContent(0, $"CODE:{code.Content.Trim()}"),
        }, "# Title\n\n~~gone~~\n\n![a](/a.png)\n\n```\nx\n```");

        var root = component.Find(".bit-mdv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual(1, component.FindAll(".bit-mdv h2").Count);
        Assert.AreEqual(0, component.FindAll(".bit-mdv h1").Count);
        Assert.AreEqual(1, component.FindAll(".bit-mdv del").Count);
        Assert.IsFalse(component.Find(".bit-mdv img").HasAttribute("src"));
        StringAssert.Contains(root.TextContent, "CODE:x");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTakeTheCascadedInputLimits()
    {
        var component = RenderWithParams(new BitMarkdownViewerParams
        {
            MaxLength = 5,
            StripBidiControlCharacters = true,
        }, "ab‮cdefghij");

        var viewer = component.FindComponent<BitMarkdownViewer>().Instance;

        Assert.AreEqual(5, viewer.MaxLength);
        Assert.IsTrue(viewer.StripBidiControlCharacters);
        Assert.AreEqual("abcde", component.Find(".bit-mdv p").TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitMarkdownViewerParams
        {
            Class = "cascaded",
            HeadingLevelOffset = 2,
            Pipeline = BitMarkdownPipelines.GitHub,
        }, "# Title\n\n~~kept~~", builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitMarkdownViewer.HeadingLevelOffset), 0);
            builder.AddAttribute(4, nameof(BitMarkdownViewer.Pipeline), BitMarkdownPipelines.Basic);
        });

        var root = component.Find(".bit-mdv");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.AreEqual(1, component.FindAll(".bit-mdv h1").Count);
        Assert.AreEqual(0, component.FindAll(".bit-mdv del").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldDropTheCascadedValuesWithTheCascade()
    {
        var component = RenderWithParams(new BitMarkdownViewerParams
        {
            HeadingLevelOffset = 1,
            Pipeline = BitMarkdownPipelines.GitHub,
        }, "# Title");

        Assert.AreEqual(1, component.FindAll(".bit-mdv h2").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.AddChildContent(builder => BuildViewer(builder, "# Title", null));
        });

        var viewer = component.FindComponent<BitMarkdownViewer>().Instance;

        Assert.AreEqual(0, viewer.HeadingLevelOffset);
        Assert.IsNull(viewer.Pipeline);
        Assert.AreEqual(1, component.FindAll(".bit-mdv h1").Count);
    }
}
