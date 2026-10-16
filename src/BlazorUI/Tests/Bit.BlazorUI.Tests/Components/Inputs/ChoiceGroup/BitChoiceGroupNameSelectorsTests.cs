using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the custom item API of the ChoiceGroup: an item of the app's own type is read through the property names
/// and the selectors of its NameSelectors, and the index and the selection are written back through the same.
/// </summary>
[TestClass]
public class BitChoiceGroupNameSelectorsTests : BunitTestContext
{
    public class Fruit
    {
        public string? Name { get; set; }

        public string? Code { get; set; }

        public string? Hint { get; set; }

        public string? Glyph { get; set; }

        public bool Off { get; set; }

        public int Pos { get; set; }

        public bool Chosen { get; set; }
    }

    private static List<Fruit> GetFruits() =>
    [
        new() { Name = "Apple", Code = "apl", Hint = "Sweet" },
        new() { Name = "Lemon", Code = "lmn", Glyph = "Sunny" },
        new() { Name = "Durian", Code = "drn", Off = true },
    ];

    private static BitChoiceGroupNameSelectors<Fruit, string> GetNameSelectors() => new()
    {
        Text = new(nameof(Fruit.Name)),
        Value = new(nameof(Fruit.Code)),
        Description = new(nameof(Fruit.Hint)),
        IconName = new(nameof(Fruit.Glyph)),
        IsDisabled = new(nameof(Fruit.Off)),
        Index = nameof(Fruit.Pos),
        IsSelected = nameof(Fruit.Chosen),
    };

    [TestMethod]
    public void BitChoiceGroupShouldReadACustomItemThroughThePropertyNames()
    {
        var fruits = GetFruits();

        var component = RenderComponent<BitChoiceGroup<Fruit, string>>(parameters =>
        {
            parameters.Add(p => p.Items, fruits);
            parameters.Add(p => p.NameSelectors, GetNameSelectors());
        });

        var containers = component.FindAll(".bit-chg-icn");
        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.AreEqual(3, containers.Count);
        CollectionAssert.AreEqual(new[] { "Apple", "Lemon", "Durian" }, component.FindAll(".bit-chg-itx").Select(t => t.TextContent).ToArray());
        CollectionAssert.AreEqual(new[] { "apl", "lmn", "drn" }, inputs.Select(i => i.GetAttribute("value")).ToArray());
        Assert.AreEqual("Sweet", component.Find(".bit-chg-dsc").TextContent);
        Assert.IsTrue(containers[1].QuerySelector(".bit-chg-ico")!.ClassList.Contains("bit-icon--Sunny"));
        Assert.IsTrue(inputs[2].HasAttribute("disabled"));
        Assert.IsTrue(containers[2].ClassList.Contains("bit-chg-ids"));

        // The index is written back onto the items through the name given for it.
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, fruits.Select(f => f.Pos).ToArray());
    }

    [TestMethod]
    public void BitChoiceGroupShouldWriteTheSelectionBackThroughThePropertyName()
    {
        var fruits = GetFruits();
        var value = "apl";

        var component = RenderComponent<BitChoiceGroup<Fruit, string>>(parameters =>
        {
            parameters.Add(p => p.Items, fruits);
            parameters.Add(p => p.NameSelectors, GetNameSelectors());
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        Assert.IsTrue(fruits[0].Chosen);
        Assert.IsTrue(component.FindAll(".bit-chg-icn")[0].ClassList.Contains("bit-chg-ich"));

        component.FindAll(".bit-chg-icn input")[1].Change(true);

        Assert.AreEqual("lmn", value);
        Assert.IsFalse(fruits[0].Chosen);
        Assert.IsTrue(fruits[1].Chosen);
        Assert.IsTrue(component.FindAll(".bit-chg-icn")[1].ClassList.Contains("bit-chg-ich"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldPreferTheSelectorOverThePropertyName()
    {
        var fruits = GetFruits();
        var selectors = GetNameSelectors();

        selectors.Text = new("NoSuchProperty") { Selector = f => f.Name!.ToUpperInvariant() };
        selectors.IsDisabled = new("NoSuchProperty") { Selector = f => f.Code == "apl" };

        var component = RenderComponent<BitChoiceGroup<Fruit, string>>(parameters =>
        {
            parameters.Add(p => p.Items, fruits);
            parameters.Add(p => p.NameSelectors, selectors);
        });

        CollectionAssert.AreEqual(new[] { "APPLE", "LEMON", "DURIAN" }, component.FindAll(".bit-chg-itx").Select(t => t.TextContent).ToArray());

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.IsTrue(inputs[0].HasAttribute("disabled"));
        Assert.IsFalse(inputs[1].HasAttribute("disabled"));
        Assert.IsFalse(inputs[2].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldFallBackToTheDefaultPropertyNamesOfACustomItem()
    {
        // The default selectors read the property names of BitChoiceGroupItem, so a custom type that spells its
        // properties the same way needs no selectors at all.
        var items = new List<LookAlike>
        {
            new() { Text = "First", Value = "1" },
            new() { Text = "Second", Value = "2", IsDisabled = true },
        };

        var component = RenderComponent<BitChoiceGroup<LookAlike, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitChoiceGroupNameSelectors<LookAlike, string>());
            parameters.Add(p => p.Value, "2");
        });

        CollectionAssert.AreEqual(new[] { "First", "Second" }, component.FindAll(".bit-chg-itx").Select(t => t.TextContent).ToArray());
        Assert.IsTrue(component.FindAll(".bit-chg-icn")[1].ClassList.Contains("bit-chg-ich"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn")[1].ClassList.Contains("bit-chg-ids"));
        Assert.AreEqual(1, items[1].Index);
        Assert.IsTrue(items[1].IsSelected);
    }

    public class LookAlike
    {
        public string? Text { get; set; }

        public string? Value { get; set; }

        public bool IsDisabled { get; set; }

        public int Index { get; set; }

        public bool IsSelected { get; set; }
    }
}
