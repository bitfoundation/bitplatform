using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the built-in content of an item: its prefix, its image (size, alt and the checked state image)
/// and how they interact with the templates of the component.
/// </summary>
[TestClass]
public class BitChoiceGroupItemContentTests : BunitTestContext
{
    [TestMethod]
    public void BitChoiceGroupShouldNotSizeTheImageWrapperWhenNoImageSizeIsGiven()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        // A 0x0 wrapper would hide the image, since the image itself is sized to 100% of the wrapper.
        Assert.AreEqual(string.Empty, component.Find(".bit-chg-iiw").GetAttribute("style"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldSizeTheImageWrapperFromTheImageSize()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png", ImageSize = new BitImageSize(32, 24) },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Styles, new BitChoiceGroupClassStyles { ItemImageWrapper = "opacity:0.5" });
        });

        Assert.AreEqual("width:32px;height:24px;opacity:0.5", component.Find(".bit-chg-iiw").GetAttribute("style"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepTheImageOfACheckedItemWithoutASelectedImage()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, "A");
        });

        Assert.AreEqual("a.png", component.Find(".bit-chg-iiw img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldSwapToTheSelectedImageOfACheckedItem()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png", SelectedImageSrc = "a-selected.png" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual("a.png", component.Find(".bit-chg-iiw img").GetAttribute("src"));

        component.Render(parameters => parameters.Add(p => p.Value, "A"));

        Assert.AreEqual("a-selected.png", component.Find(".bit-chg-iiw img").GetAttribute("src"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldAlwaysRenderAnAltAttributeOnTheImage()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png" },
            new() { Text = "B", Value = "B", ImageSrc = "b.png", ImageAlt = "the b image" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var images = component.FindAll(".bit-chg-iiw img");

        Assert.IsTrue(images[0].HasAttribute("alt"));
        Assert.AreEqual(string.Empty, images[0].GetAttribute("alt"));
        Assert.AreEqual("the b image", images[1].GetAttribute("alt"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldFallBackToTheItemPrefixTemplateForAnEmptyPrefix()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Prefix = string.Empty },
            new() { Text = "B", Value = "B", Prefix = "$10 — " },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemPrefixTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, $"[{item.Index}]"));
        });

        var labels = component.FindAll(".bit-chg-itl");

        StringAssert.Contains(labels[0].TextContent, "[0]");
        StringAssert.Contains(labels[1].TextContent, "$10 — ");
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderTheSuffixAfterTheItemContent()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "Standard", Value = "Standard", Suffix = "Free" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Classes, new BitChoiceGroupClassStyles { ItemSuffix = "suffix-class" });
        });

        var label = component.Find(".bit-chg-itl");

        Assert.AreEqual("Free", component.Find(".suffix-class").TextContent);
        StringAssert.EndsWith(label.TextContent.Trim(), "Free");
    }

    [TestMethod]
    public void BitChoiceGroupShouldFallBackToTheItemSuffixTemplateForAnEmptySuffix()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Suffix = string.Empty },
            new() { Text = "B", Value = "B", Suffix = "$10" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemSuffixTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, $"[{item.Index}]"));
        });

        var labels = component.FindAll(".bit-chg-itl");

        StringAssert.Contains(labels[0].TextContent, "[0]");
        StringAssert.Contains(labels[1].TextContent, "$10");
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotRenderTheSuffixWhenATemplateTakesOverTheItem()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Suffix = "Free" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, item.Text));
        });

        Assert.AreEqual("A", component.Find(".bit-chg-itl").TextContent.Trim());
    }
}
