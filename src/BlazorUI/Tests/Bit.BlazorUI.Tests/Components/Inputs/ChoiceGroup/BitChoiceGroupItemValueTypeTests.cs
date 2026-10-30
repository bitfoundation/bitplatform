using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers a ChoiceGroup whose built-in items carry a value type other than its TValue: the BitChoiceGroupItem&lt;Guid&gt;
/// of a group bound to a Guid? works as a BitChoiceGroupItem&lt;Guid?&gt; would, and a pair whose item values can never be
/// a TValue fails as soon as it is rendered instead of rendering a group in which no item ever matches a value.
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
    public void BitChoiceGroupShouldThrowForItemsWhoseValuesCanNeverBeATValue()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitChoiceGroup<BitChoiceGroupItem<int>, long>>(parameters =>
            {
                parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }]);
            }));

        StringAssert.Contains(exception.Message, "BitChoiceGroupItem<Int32>");
        StringAssert.Contains(exception.Message, "BitChoiceGroupItem<Int64>");
    }

    [TestMethod]
    public void BitChoiceGroupShouldThrowForOptionsOfAnotherTValue()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitChoiceGroup<BitChoiceGroupOption<Guid>, Guid?>>());

        StringAssert.Contains(exception.Message, "BitChoiceGroupOption<Guid?>");
    }

    [TestMethod]
    public void BitChoiceGroupShouldReadMismatchedItemsThroughTheGivenNameSelectors()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<int>, long>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Add(p => p.NameSelectors, new() { Value = { Selector = item => item.Value } });
            parameters.Add(p => p.Value, 2L);
        });

        Assert.IsTrue(component.FindAll(".bit-chg-icn input")[1].HasAttribute("checked"));
    }
}
