using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

[TestClass]
public class BitMarkdownViewerTests : BunitTestContext
{
    [TestMethod]
    public void BitMarkdownViewerShouldRenderExpectedClass()
    {
        var component = RenderComponent<BitMarkdownViewer>();

        var root = component.Find(".bit-mdv");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderHeading()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# hello");
        });

        var root = component.Find(".bit-mdv");

        Assert.Contains("<h1>hello</h1>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderInlineFormatting()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "a **bold** and *italic* word");
        });

        var root = component.Find(".bit-mdv");

        Assert.Contains("<strong>bold</strong>", root.InnerHtml);
        Assert.Contains("<em>italic</em>", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTreatRawHtmlAsText()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "<script>alert(1)</script>");
        });

        var root = component.Find(".bit-mdv");

        // Raw HTML must be escaped (rendered as text), never as live markup.
        Assert.DoesNotContain("<script>", root.InnerHtml);
        Assert.Contains("&lt;script&gt;", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldSanitizeUnsafeLinks()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[click](javascript:alert(1))");
        });

        var root = component.Find(".bit-mdv");

        Assert.DoesNotContain("javascript:", root.InnerHtml);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenMarkdownChanges()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# one");
        });

        Assert.Contains("<h1>one</h1>", component.Markup);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Markdown, "# two");
        });

        Assert.Contains("<h1>two</h1>", component.Markup);
        Assert.DoesNotContain("<h1>one</h1>", component.Markup);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownViewerShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Markdown, "enable");
        });

        var root = component.Find(".bit-mdv");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMarkdownViewerBasicPipelineShouldNotRenderTables()
    {
        var markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.DoesNotContain("<table", component.Markup);
    }

    [TestMethod]
    public void BitMarkdownViewerGitHubPipelineShouldRenderGfmFeatures()
    {
        var markdown = "~~gone~~\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] done\n- [ ] todo";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var markup = component.Markup;

        Assert.Contains("<del>gone</del>", markup);
        Assert.Contains("<table", markup);
        Assert.Contains("type=\"checkbox\"", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerAdvancedPipelineShouldRenderEmojiAndHeadingIds()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# Hello World :rocket:");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.Advanced);
        });

        var markup = component.Markup;

        Assert.Contains("🚀", markup);
        Assert.Contains("id=\"hello-world\"", markup);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenPipelineChanges()
    {
        var markdown = "~~gone~~";

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
        });

        Assert.DoesNotContain("<del>gone</del>", component.Markup);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.Contains("<del>gone</del>", component.Markup);
    }
}
