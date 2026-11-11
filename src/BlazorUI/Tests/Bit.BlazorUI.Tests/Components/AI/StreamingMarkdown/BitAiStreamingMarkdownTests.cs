using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.StreamingMarkdown;

[TestClass]
public class BitAiStreamingMarkdownTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiStreamingMarkdownShouldRenderItsRootAroundTheMarkdownView()
    {
        var component = RenderComponent<BitAiStreamingMarkdown>(p => p.Add(c => c.Text, "Hello **world**"));

        var root = component.Find(".bit-aismd");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
        Assert.AreEqual("Hello world", root.QuerySelector(".bit-aimdv > .bit-aimdv-par")!.TextContent);
    }

    [TestMethod]
    public void BitAiStreamingMarkdownShouldRespectClassStyleAndDir()
    {
        var component = RenderComponent<BitAiStreamingMarkdown>(p => p
            .Add(c => c.Text, "text")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: red")
            .Add(c => c.Dir, BitDir.Rtl));

        var root = component.Find(".bit-aismd");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.Contains("color: red", root.GetAttribute("style")!);
        // The class of the component is its own, not the Markdown view's.
        Assert.IsFalse(component.Find(".bit-aimdv").ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitAiStreamingMarkdownShouldHandItsMarkdownClassesToTheView()
    {
        var component = RenderComponent<BitAiStreamingMarkdown>(p => p
            .Add(c => c.Text, "text")
            .Add(c => c.Classes, new() { Root = "c-root", Markdown = new() { Root = "c-view", Paragraph = "c-par" } })
            .Add(c => c.Styles, new() { Root = "margin: 1px", Markdown = new() { Root = "margin: 2px" } }));

        Assert.IsTrue(component.Find(".bit-aismd").ClassList.Contains("c-root"));
        Assert.Contains("margin: 1px", component.Find(".bit-aismd").GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-aimdv").ClassList.Contains("c-view"));
        Assert.Contains("margin: 2px", component.Find(".bit-aimdv").GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-aimdv-par").ClassList.Contains("c-par"));
    }

    [TestMethod]
    public void BitAiStreamingMarkdownShouldTakeTheCascadedParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiStreamingMarkdownParams { Class = "cascaded", Classes = new() { Root = "cascaded-root" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiStreamingMarkdown>(0);
                builder.AddAttribute(1, nameof(BitAiStreamingMarkdown.Text), "text");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-aismd");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.AreEqual("BitParams.BitAiStreamingMarkdown", new BitAiStreamingMarkdownParams().Name);
    }

    [TestMethod]
    public void StreamingMarkdownParsesWithTheCascadedMarkdownOptions()
    {
        var withMath = RenderComponent<BitAiStreamingMarkdown>(p => p.Add(c => c.Text, "value $x^2$ here"));
        Assert.IsNotEmpty(withMath.FindAll(".bit-aimdv-mth"));

        var ui = BitAiUiOptions.Default with { Markdown = new BitAiMarkdownOptions { Math = false } };
        var withoutMath = RenderComponent<BitAiStreamingMarkdown>(p => p
            .Add(c => c.Text, "value $x^2$ here")
            .Add(c => c.CascadedOptions, ui));

        Assert.IsEmpty(withoutMath.FindAll(".bit-aimdv-mth"));
        Assert.Contains("$x^2$", withoutMath.Find(".bit-aimdv-par").TextContent);
    }

    [TestMethod]
    public void StreamingMarkdownReparsesWhenTheOptionsInstanceChanges()
    {
        var component = RenderComponent<OptionsHost>(p => p.Add(c => c.Options, BitAiUiOptions.Default));
        Assert.IsEmpty(component.FindAll("br"));

        var hardBreaks = BitAiUiOptions.Default with { Markdown = new BitAiMarkdownOptions { SoftBreaksAsHardBreaks = true } };
        component.Render(p => p.Add(c => c.Options, hardBreaks));

        Assert.IsNotEmpty(component.FindAll("br"));
    }

    [TestMethod]
    public void StreamingMarkdownKeepsEarlierBlocksAsTextArrivesAndSettlesWhenItEnds()
    {
        var component = RenderComponent<BitAiStreamingMarkdown>(p => p
            .Add(c => c.Text, "First.\n\nSecond **bo")
            .Add(c => c.IsStreaming, true));

        Assert.IsNotEmpty(component.FindAll(".bit-aimdv-crt"));
        Assert.DoesNotContain("**", component.Markup);

        component.Render(p => p
            .Add(c => c.Text, "First.\n\nSecond **bold**.")
            .Add(c => c.IsStreaming, false));

        Assert.Contains("<strong>bold</strong>", component.Markup);
        Assert.IsEmpty(component.FindAll(".bit-aimdv-crt"));
    }

    [TestMethod]
    public async Task StreamingMarkdownDisposesThroughTheAsyncPath()
    {
        var component = RenderComponent<BitAiStreamingMarkdown>(p => p.Add(c => c.Text, "text"));

        // The component is IAsyncDisposable; it must not rely on IDisposable, which Blazor never calls on a component
        // that implements both.
        Assert.IsFalse(component.Instance is IDisposable);
        await ((IAsyncDisposable)component.Instance).DisposeAsync();
    }



    /// <summary>Cascades a changeable options instance to one streaming component, as BitAiOptionsProvider would.</summary>
    private sealed class OptionsHost : ComponentBase
    {
        [Parameter]
        public BitAiUiOptions Options { get; set; } = BitAiUiOptions.Default;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<BitAiUiOptions>>(0);
            builder.AddComponentParameter(1, "Value", Options);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<BitAiStreamingMarkdown>(0);
                child.AddComponentParameter(1, "Text", "a\nb");
                child.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
