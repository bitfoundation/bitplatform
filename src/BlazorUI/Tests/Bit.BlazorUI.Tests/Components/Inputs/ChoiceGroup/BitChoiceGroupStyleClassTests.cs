using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers how the per-item Style/Class, the Styles/Classes of the component and the built-in state markers
/// are composed onto the container of each item.
/// </summary>
[TestClass]
public class BitChoiceGroupStyleClassTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
    ];

    [TestMethod]
    public void BitChoiceGroupShouldSeparateTheItemStylesWithASemicolon()
    {
        var items = GetItems();
        items[0].Style = "color:red";

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, "A");
            parameters.Add(p => p.Styles, new BitChoiceGroupClassStyles
            {
                ItemContainer = "padding:8px",
                ItemChecked = "border:1px solid red"
            });
        });

        var style = component.FindAll(".bit-chg-icn")[0].GetAttribute("style");

        Assert.AreEqual("color:red;padding:8px;border:1px solid red", style);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotApplyTheCheckedStyleToUncheckedItems()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, "A");
            parameters.Add(p => p.Styles, new BitChoiceGroupClassStyles { ItemChecked = "border:1px solid red" });
        });

        var containers = component.FindAll(".bit-chg-icn");

        Assert.AreEqual("border:1px solid red", containers[0].GetAttribute("style"));
        Assert.AreEqual(string.Empty, containers[1].GetAttribute("style"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldApplyTheCheckedClassAlongsideTheCheckedStyleWhenAnItemTemplateIsUsed()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, "A");
            parameters.Add(p => p.ItemTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, item.Text));
            parameters.Add(p => p.Classes, new BitChoiceGroupClassStyles { ItemChecked = "custom-checked" });
            parameters.Add(p => p.Styles, new BitChoiceGroupClassStyles { ItemChecked = "border:1px solid red" });
        });

        var containers = component.FindAll(".bit-chg-icn");

        Assert.IsTrue(containers[0].ClassList.Contains("custom-checked"));
        Assert.IsTrue(containers[0].ClassList.Contains("bit-chg-ich"));
        Assert.AreEqual("border:1px solid red", containers[0].GetAttribute("style"));

        Assert.IsFalse(containers[1].ClassList.Contains("custom-checked"));
        Assert.IsFalse(containers[1].ClassList.Contains("bit-chg-ich"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldMarkADisabledItemAsDisabledWhenAnItemTemplateIsUsed()
    {
        var items = GetItems();
        items[1].IsEnabled = false;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, item.Text));
        });

        var containers = component.FindAll(".bit-chg-icn");

        Assert.IsFalse(containers[0].ClassList.Contains("bit-chg-ids"));
        Assert.IsTrue(containers[1].ClassList.Contains("bit-chg-ids"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotMarkTheItemAsHavingAnImageWhenATemplateReplacesTheContent()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", ImageSrc = "a.png" },
        };

        var withoutTemplate = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsTrue(withoutTemplate.Find(".bit-chg-icn").ClassList.Contains("bit-chg-ihi"));

        var withTemplate = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, item.Text));
        });

        Assert.IsFalse(withTemplate.Find(".bit-chg-icn").ClassList.Contains("bit-chg-ihi"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldComposeTheItemClassWithTheItemContainerClass()
    {
        var items = GetItems();
        items[0].Class = "item-class";

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Classes, new BitChoiceGroupClassStyles { ItemContainer = "container-class" });
        });

        var container = component.FindAll(".bit-chg-icn")[0];

        Assert.IsTrue(container.ClassList.Contains("bit-chg-icn"));
        Assert.IsTrue(container.ClassList.Contains("item-class"));
        Assert.IsTrue(container.ClassList.Contains("container-class"));
    }
}
