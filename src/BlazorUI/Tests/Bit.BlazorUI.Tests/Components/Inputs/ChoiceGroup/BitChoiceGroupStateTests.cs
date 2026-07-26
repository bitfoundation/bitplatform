using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers how the ChoiceGroup keeps its own state (the rendered items, the IsSelected of each item and the
/// applied DefaultValue) in sync with the parameters it is given.
/// </summary>
[TestClass]
public class BitChoiceGroupStateTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
        new() { Text = "Item C", Value = "C" },
    ];

    [TestMethod]
    public void BitChoiceGroupShouldClearItemsWhenItemsBecomeEmpty()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        Assert.AreEqual(3, component.FindAll(".bit-chg-icn").Count);

        component.Render(parameters => parameters.Add(p => p.Items, new List<BitChoiceGroupItem<string>>()));

        Assert.AreEqual(0, component.FindAll(".bit-chg-icn").Count);
    }

    [TestMethod]
    public void BitChoiceGroupShouldDetectInPlaceMutationOfTheSameItemsCollection()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(3, component.FindAll(".bit-chg-icn").Count);

        items.Add(new() { Text = "Item D", Value = "D" });

        component.Render(parameters => parameters.Add(p => p.Items, items));

        Assert.AreEqual(4, component.FindAll(".bit-chg-icn").Count);
        Assert.AreEqual(3, items[3].Index);
    }

    [TestMethod]
    public void BitChoiceGroupShouldUpdateIsSelectedWhenTheValueChangesFromOutside()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, "A");
        });

        Assert.IsTrue(items[0].IsSelected);
        Assert.IsFalse(items[2].IsSelected);

        component.Render(parameters => parameters.Add(p => p.Value, "C"));

        Assert.IsFalse(items[0].IsSelected);
        Assert.IsTrue(items[2].IsSelected);
    }

    [TestMethod]
    public void BitChoiceGroupShouldClearIsSelectedWhenTheValueIsCleared()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, "B");
        });

        Assert.IsTrue(items[1].IsSelected);

        component.Render(parameters => parameters.Add(p => p.Value, (string?)null));

        Assert.IsFalse(items.Any(i => i.IsSelected));
    }

    [TestMethod]
    public void BitChoiceGroupShouldSetIsSelectedOnTheClickedItem()
    {
        var items = GetItems();
        var value = "A";

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-chg-icn input")[2].Change(true);

        Assert.AreEqual("C", value);
        Assert.IsFalse(items[0].IsSelected);
        Assert.IsTrue(items[2].IsSelected);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotReapplyDefaultValueWhenTheItemsChange()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, "A");
        });

        Assert.AreEqual("A", component.Instance.Value);

        // The user picks another item while in uncontrolled mode.
        component.FindAll(".bit-chg-icn input")[1].Change(true);
        Assert.AreEqual("B", component.Instance.Value);

        // A later change of the items must not revert that choice back to the DefaultValue.
        items.Add(new() { Text = "Item D", Value = "D" });
        component.Render(parameters => parameters.Add(p => p.Items, items));

        Assert.AreEqual("B", component.Instance.Value);
        Assert.IsTrue(items[1].IsSelected);
    }

    [TestMethod]
    public void BitChoiceGroupShouldApplyANewlyAssignedDefaultValue()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultValue, "A");
        });

        Assert.AreEqual("A", component.Instance.Value);

        component.Render(parameters => parameters.Add(p => p.DefaultValue, "C"));

        Assert.AreEqual("C", component.Instance.Value);
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepTheIndexOfTheItemsInSyncWithTheCollection()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, items.Select(i => i.Index).ToArray());

        var reversed = Enumerable.Reverse(items).ToList();
        component.Render(parameters => parameters.Add(p => p.Items, reversed));

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, reversed.Select(i => i.Index).ToArray());
        Assert.AreEqual(2, items[0].Index);
    }
}
