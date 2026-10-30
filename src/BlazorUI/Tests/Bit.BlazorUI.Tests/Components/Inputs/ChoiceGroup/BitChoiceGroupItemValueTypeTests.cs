using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers a ChoiceGroup whose built-in items carry a value type other than its TValue: the BitChoiceGroupItem&lt;Guid&gt;
/// of a group bound to a Guid? works as a BitChoiceGroupItem&lt;Guid?&gt; would, the values of other items are read the
/// way the NameSelectors read them, the NameSelectors still decide how such items are read when they are given, and a
/// group of options of another TValue (which never reach it) fails before it does anything instead of rendering empty.
/// </summary>
[TestClass]
public class BitChoiceGroupItemValueTypeTests : BunitTestContext
{
    private static readonly Guid _oneId = Guid.NewGuid();
    private static readonly Guid _twoId = Guid.NewGuid();

    private static List<BitChoiceGroupItem<Guid>> GetItems() =>
    [
        new() { Text = "One", Value = _oneId },
        new() { Text = "Two", Value = _twoId },
    ];

    [TestMethod]
    public void BitChoiceGroupShouldCheckTheItemOfANullableValueFromItemsOfItsUnderlyingType()
    {
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Value, _twoId);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.IsFalse(inputs[0].HasAttribute("checked"));
        Assert.IsTrue(inputs[1].HasAttribute("checked"));
        Assert.AreEqual(_oneId.ToString(), inputs[0].GetAttribute("value"));
        Assert.AreEqual(_twoId.ToString(), inputs[1].GetAttribute("value"));
        Assert.AreEqual("Two", component.FindAll(".bit-chg-icn")[1].TextContent.Trim());
        Assert.IsTrue(items[1].IsSelected);
        Assert.AreEqual(1, items[1].Index);
    }

    [TestMethod]
    public void BitChoiceGroupShouldRaiseValueChangedWithTheValueOfAnItemOfTheUnderlyingType()
    {
        Guid? bound = null;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        component.FindAll(".bit-chg-icn input")[1].Change(true);

        Assert.AreEqual(_twoId, bound);
        Assert.IsTrue(component.FindAll(".bit-chg-icn input")[1].HasAttribute("checked"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderTheTemplateOfAnItemOfTheUnderlyingType()
    {
        var items = GetItems();
        items[1].Template = item => builder => builder.AddContent(0, $"Custom {item.Text}");

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        StringAssert.Contains(component.Markup, "Custom Two");
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderTheTemplateOfAnItemOfADerivedType()
    {
        var items = new List<DerivedItem> { new() { Text = "One", Value = _oneId }, new() { Text = "Two", Value = _twoId } };
        items[1].Template = item => builder => builder.AddContent(0, $"Custom {item.Text}");

        var component = RenderComponent<BitChoiceGroup<DerivedItem, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        StringAssert.Contains(component.Markup, "Custom Two");
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderTheTemplateOfAnItemOfADerivedTypeOfItsOwnTValue()
    {
        var items = new List<DerivedItem> { new() { Text = "One", Value = _oneId }, new() { Text = "Two", Value = _twoId } };
        items[1].Template = item => builder => builder.AddContent(0, $"Custom {item.Text}");

        var component = RenderComponent<BitChoiceGroup<DerivedItem, Guid>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        StringAssert.Contains(component.Markup, "Custom Two");
    }

    [TestMethod]
    public void BitChoiceGroupShouldCheckTheItemOfAValueFromItemsOfATypeThatConvertsToIt()
    {
        long? bound = 2;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<int>, long?>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        Assert.IsTrue(component.FindAll(".bit-chg-icn input")[1].HasAttribute("checked"));

        component.FindAll(".bit-chg-icn input")[0].Change(true);

        Assert.AreEqual(1L, bound);
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderItemsWhoseValuesAreNoTValueWithoutCheckingAnyOfThem()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<Guid>, int>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, 1);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.HasCount(2, inputs);
        Assert.IsTrue(inputs.All(input => input.HasAttribute("checked") is false));
        Assert.AreEqual("Two", component.FindAll(".bit-chg-icn")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitChoiceGroupShouldReadMismatchedItemsThroughTheGivenNameSelectors()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<int>, long>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Add(p => p.NameSelectors, new() { Value = { Selector = item => item.Value * 10L } });
            parameters.Add(p => p.Value, 20L);
        });

        Assert.IsTrue(component.FindAll(".bit-chg-icn input")[1].HasAttribute("checked"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldReadTheItemsOfTheUnderlyingTypeThroughTheGivenNameSelectors()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.NameSelectors, new() { Text = { Selector = item => $"{item.Text}!" } });
        });

        Assert.AreEqual("Two!", component.FindAll(".bit-chg-icn")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitChoiceGroupShouldThrowForOptionsOfAnotherTValue()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitChoiceGroup<BitChoiceGroupOption<Guid>, Guid?>>());

        StringAssert.Contains(exception.Message, "BitChoiceGroupOption<Guid?>");
    }

    [TestMethod]
    public void BitChoiceGroupShouldThrowForOptionsOfAnotherTValueWhateverTheNameSelectors()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitChoiceGroup<BitChoiceGroupOption<Guid>, Guid?>>(parameters =>
            {
                parameters.Add(p => p.NameSelectors, new());
            }));
    }

    [TestMethod]
    public void IBitChoiceGroupItemShouldHaveAMemberForEveryPublicPropertyOfTheBuiltInItem()
    {
        var itemInterface = typeof(BitChoiceGroupItem<>).Assembly.GetType("Bit.BlazorUI.IBitChoiceGroupItem", throwOnError: true)!;
        var interfaceMembers = itemInterface.GetProperties().Select(p => p.Name).ToHashSet();

        // The template and the value are read through methods typed by the TItem and TValue of the group.
        Assert.IsNotNull(itemInterface.GetMethod("GetTemplate"));
        Assert.IsNotNull(itemInterface.GetMethod("GetValue"));
        interfaceMembers.Add(nameof(BitChoiceGroupItem<int>.Template));
        interfaceMembers.Add(nameof(BitChoiceGroupItem<int>.Value));

        var missing = typeof(BitChoiceGroupItem<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                     .Select(p => p.Name)
                                                     .Where(name => interfaceMembers.Contains(name) is false)
                                                     .ToArray();

        Assert.IsEmpty(missing, "A ChoiceGroup reads the built-in items of another T through IBitChoiceGroupItem, which has no member for: " +
                                string.Join(", ", missing));
    }

    public class DerivedItem : BitChoiceGroupItem<Guid>;
}
