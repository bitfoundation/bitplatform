using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// Covers a dropdown whose built-in items carry a value type other than its TValue: the BitDropdownItem&lt;Guid&gt; of a
/// dropdown bound to a Guid? (ids are rarely nullable while the property a dropdown is bound to often is) works as a
/// BitDropdownItem&lt;Guid?&gt; would, and a pair whose item values can never be a TValue fails as soon as it is rendered
/// instead of rendering a dropdown in which no item ever matches a value.
/// </summary>
[TestClass]
public class BitDropdownItemValueTypeTests : BunitTestContext
{
    private static readonly Guid _oneId = Guid.NewGuid();
    private static readonly Guid _twoId = Guid.NewGuid();

    private static List<BitDropdownItem<Guid>> GetItems() =>
    [
        new() { Text = "One", Value = _oneId },
        new() { Text = "Two", Value = _twoId },
    ];

    [TestMethod]
    public void BitDropdownShouldShowTheItemOfANullableValueFromItemsOfItsUnderlyingType()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Value, _twoId);
        });

        Assert.AreEqual("Two", component.Find(".bit-drp-tdp").InnerHtml);
        Assert.IsTrue(component.Instance.Items!.Last().IsSelected);
        Assert.IsFalse(component.Instance.Items!.First().IsSelected);
    }

    [TestMethod]
    public void BitDropdownShouldRenderTheItemsOfItsUnderlyingTypeWithTheirOwnProperties()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var items = GetItems();
        items[1].IsDisabled = true;

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsOpen, true);
        });

        var drpItems = component.FindAll(".bit-drp-itm");

        Assert.AreEqual(2, drpItems.Count);
        Assert.AreEqual("One", drpItems[0].TextContent.Trim());
        Assert.AreEqual("Two", drpItems[1].TextContent.Trim());
        Assert.IsTrue(drpItems[1].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDropdownShouldRaiseValueChangedWithTheValueOfAnItemOfTheUnderlyingType()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var changes = new List<Guid?>();

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ValueChanged, (Guid? value) => changes.Add(value));
        });

        component.FindAll(".bit-drp-itm")[1].Click();

        CollectionAssert.AreEqual(new Guid?[] { _twoId }, changes);
    }

    [TestMethod]
    public void BitDropdownShouldSelectTheValuesOfAMultiSelectFromItemsOfTheUnderlyingType()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Values, new Guid?[] { _oneId, _twoId });
        });

        Assert.AreEqual(2, component.Instance.SelectedItems.Count);
        Assert.AreEqual("One, Two", component.Find(".bit-drp-tdp").InnerHtml);
    }

    [TestMethod]
    public void BitDropdownShouldAddADynamicItemOfTheUnderlyingType()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("Three");

        var newId = Guid.NewGuid();
        var added = new List<BitDropdownItem<Guid>>();

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<Guid>? _) => newId);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<Guid> item) => added.Add(item));
        });

        var comboInput = component.Find(".bit-drp-inp");
        comboInput.Input("Three");
        comboInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual("Three", added[0].Text);
        Assert.AreEqual("Three", added[0].Title);
        Assert.AreEqual(newId, added[0].Value);
        Assert.AreEqual(newId, component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldThrowForItemsWhoseValuesCanNeverBeATValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitDropdown<BitDropdownItem<int>, long>>(parameters =>
            {
                parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }]);
            }));

        StringAssert.Contains(exception.Message, "BitDropdownItem<Int32>");
        StringAssert.Contains(exception.Message, "BitDropdownItem<Int64>");
    }

    [TestMethod]
    public void BitDropdownShouldThrowForNullableItemsOfANonNullableTValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitDropdown<BitDropdownItem<Guid?>, Guid>>(parameters =>
            {
                parameters.Add(p => p.Items, [new() { Text = "One", Value = _oneId }]);
            }));

        StringAssert.Contains(exception.Message, "BitDropdownItem<Guid?>");
    }

    [TestMethod]
    public void BitDropdownShouldThrowForOptionsOfAnotherTValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // An option reaches the dropdown through a cascade typed by its own TValue, so one of another TValue never
        // registers with it at all - not even an option of the underlying type of a nullable TValue.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitDropdown<BitDropdownOption<Guid>, Guid?>>());

        StringAssert.Contains(exception.Message, "BitDropdownOption<Guid?>");
    }

    [TestMethod]
    public void BitDropdownShouldReadMismatchedItemsThroughTheGivenNameSelectors()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<int>, long>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Add(p => p.NameSelectors, new() { Value = { Selector = item => item.Value } });
            parameters.Add(p => p.Value, 2L);
        });

        Assert.AreEqual("Two", component.Find(".bit-drp-tdp").InnerHtml);
    }
}
