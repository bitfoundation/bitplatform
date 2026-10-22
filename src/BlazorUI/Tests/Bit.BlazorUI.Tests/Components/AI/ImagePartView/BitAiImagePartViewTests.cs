using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ImagePartView;

[TestClass]
public class BitAiImagePartViewTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiImagePartViewShouldRenderTheImageInAFigure()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Width = 640, Height = 480 };

        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part));

        var root = component.Find(".bit-aiipv");
        Assert.AreEqual("FIGURE", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));

        var image = root.QuerySelector("img.bit-aiipv-img")!;
        Assert.AreEqual("https://img.x.dev/cat.png", image.GetAttribute("src"));
        Assert.AreEqual("A cat", image.GetAttribute("alt"));
        Assert.AreEqual("640", image.GetAttribute("width"));
        Assert.AreEqual("480", image.GetAttribute("height"));
        Assert.AreEqual("lazy", image.GetAttribute("loading"));
        Assert.AreEqual("async", image.GetAttribute("decoding"));

        Assert.IsEmpty(component.FindAll("figcaption"));
    }

    [TestMethod]
    public void HowTheImageLoadsIsWrittenAheadOfWhatItLoads()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Alt = "A cat", Width = 640, Height = 480 };

        var image = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part)).Find("img");
        var names = image.Attributes.Select(a => a.Name).ToList();

        // Blazor sets the attributes of a new element in the order they are written, and an engine handed the source
        // before the loading attribute starts the fetch right then: a lazy image would load at once, wherever it is.
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("loading"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("decoding"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("width"));
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("height"));
    }

    [TestMethod]
    public void AnImageWithoutAlternativeTextIsMarkedDecorativeAndHasNoSize()
    {
        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png")));

        var image = component.Find("img");

        Assert.AreEqual(string.Empty, image.GetAttribute("alt"));
        Assert.IsFalse(image.HasAttribute("width"));
        Assert.IsFalse(image.HasAttribute("height"));
    }

    [TestMethod]
    public void TheCaptionIsRenderedWhenThePartHasOne()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Caption = "A cat on a mat" };

        var component = RenderComponent<BitAiImagePartView>(p => p.Add(c => c.Part, part));

        Assert.AreEqual("A cat on a mat", component.Find(".bit-aiipv > figcaption.bit-aiipv-cap").TextContent);
    }

    [TestMethod]
    public void BitAiImagePartViewShouldApplyTheClassesAndStylesOfItsParts()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png") { Caption = "caption" };

        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: red")
            .Add(c => c.Classes, new() { Root = "c-root", Image = "c-img", Caption = "c-cap" })
            .Add(c => c.Styles, new() { Image = "margin: 1px", Caption = "margin: 2px" }));

        var root = component.Find(".bit-aiipv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aiipv-img").ClassList.Contains("c-img"));
        Assert.AreEqual("margin: 1px", component.Find(".bit-aiipv-img").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aiipv-cap").ClassList.Contains("c-cap"));
        Assert.AreEqual("margin: 2px", component.Find(".bit-aiipv-cap").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiImagePartViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aiipv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl")]
    public void BitAiImagePartViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiImagePartView>(p => p
            .Add(c => c.Part, new BitAiImagePart("https://img.x.dev/cat.png"))
            .Add(c => c.Dir, dir));

        Assert.AreEqual(expected, component.Find(".bit-aiipv").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitAiImagePartViewShouldTakeTheCascadedParams()
    {
        var part = new BitAiImagePart("https://img.x.dev/cat.png");

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiImagePartViewParams { Class = "cascaded", Classes = new() { Image = "cascaded-img" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiImagePartView>(0);
                builder.AddAttribute(1, nameof(BitAiImagePartView.Part), part);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-aiipv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiipv-img").ClassList.Contains("cascaded-img"));
        Assert.AreEqual("BitParams.BitAiImagePartView", new BitAiImagePartViewParams().Name);
    }
}
