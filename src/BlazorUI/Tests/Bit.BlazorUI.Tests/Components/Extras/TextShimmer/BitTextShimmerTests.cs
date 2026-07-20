using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextShimmer;

[TestClass]
public class BitTextShimmerTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-tsh");

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
    public void BitTextShimmerShouldRenderText()
    {
        const string text = "Thinking...";

        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, text);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(text, root.TextContent);
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderParagraphElementByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("P", root.TagName);
    }

    [TestMethod,
        DataRow("h1"),
        DataRow("span"),
        DataRow("div")]
    public void BitTextShimmerShouldRespectElement(string element)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(element.ToUpperInvariant(), root.TagName);
    }

    [TestMethod]
    public void BitTextShimmerShouldScaleSpreadByTextLength()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "12345");
            parameters.Add(p => p.Spread, 2);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:10px"));
    }

    [TestMethod]
    public void BitTextShimmerShouldScaleSpreadByContentLengthWithoutText()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.ContentLength, 20);
            parameters.Add(p => p.Spread, 1.5);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:30px"));
    }

    [TestMethod]
    public void BitTextShimmerShouldNotRenderOptionalStyleVariablesByDefault()
    {
        var component = RenderComponent<BitTextShimmer>();

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-spread:20px"));
        Assert.IsFalse(style.Contains("--bit-tsh-duration"));
        Assert.IsFalse(style.Contains("--bit-tsh-base-clr"));
        Assert.IsFalse(style.Contains("--bit-tsh-gradient-clr"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectDuration()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Duration, 1500);
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-duration:1500ms"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRespectColors()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.BaseColor, "#3f3f46");
            parameters.Add(p => p.GradientColor, "#22d3ee");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-tsh-base-clr:#3f3f46"));
        Assert.IsTrue(style.Contains("--bit-tsh-gradient-clr:#22d3ee"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTextShimmerShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual(forceAnimation, root.ClassList.Contains("bit-tsh-fam"));
    }

    [TestMethod]
    public void BitTextShimmerShouldRenderChildContentOverText()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "the text");
            parameters.AddChildContent("<strong>the content</strong>");
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("the content", root.TextContent);
        Assert.IsNotNull(component.Find(".bit-tsh strong"));
    }
}
