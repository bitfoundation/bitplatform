using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the callbacks of the ChoiceGroup and which item the base class exposes as its InputElement.
/// </summary>
[TestClass]
public class BitChoiceGroupEventsTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
        new() { Text = "Item C", Value = "C" },
    ];

    [TestMethod]
    public void BitChoiceGroupShouldRaiseOnFocusAndOnBlurWithTheItem()
    {
        BitChoiceGroupItem<string>? focused = null;
        BitChoiceGroupItem<string>? blurred = null;

        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnFocus, (BitChoiceGroupItem<string> item) => focused = item);
            parameters.Add(p => p.OnBlur, (BitChoiceGroupItem<string> item) => blurred = item);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        inputs[1].Focus();
        Assert.AreSame(items[1], focused);

        inputs[1].Blur();
        Assert.AreSame(items[1], blurred);
    }

    [TestMethod]
    public void BitChoiceGroupShouldRaiseOnClickForTheAlreadySelectedItem()
    {
        var clicks = new List<string?>();
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultValue, "A");
            parameters.Add(p => p.OnClick, (BitChoiceGroupItem<string> item) => clicks.Add(item.Value));
        });

        component.FindAll(".bit-chg-icn input")[0].Click();
        component.FindAll(".bit-chg-icn input")[0].Click();

        CollectionAssert.AreEqual(new[] { "A", "A" }, clicks.ToArray());
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotRaiseOnClickForADisabledItem()
    {
        var clicks = new List<string?>();
        var items = GetItems();
        items[1].IsEnabled = false;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnClick, (BitChoiceGroupItem<string> item) => clicks.Add(item.Value));
        });

        component.FindAll(".bit-chg-icn input")[1].Click();

        Assert.AreEqual(0, clicks.Count);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotChangeTheValueWhenReadOnly()
    {
        var value = "A";
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-chg-icn input")[2].Change(true);

        Assert.AreEqual("A", value);
        Assert.IsTrue(items[0].IsSelected);
        Assert.IsFalse(items[2].IsSelected);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotChangeTheValueWhenTheGroupIsDisabled()
    {
        var value = "A";
        var items = GetItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-chg-icn input")[2].Change(true);

        Assert.AreEqual("A", value);
    }

    [TestMethod]
    public void BitChoiceGroupShouldAutoFocusTheCheckedItem()
    {
        RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DefaultValue, "B");
            parameters.Add(p => p.AutoFocus, true);
        });

        Assert.AreEqual(1, FocusInvocationCount());
    }

    [TestMethod]
    public void BitChoiceGroupShouldAutoFocusTheFirstEnabledItemWhenNothingIsChecked()
    {
        var items = GetItems();
        items[0].IsEnabled = false;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.AutoFocus, true);
        });

        // Tabbing into the group skips the disabled first radio, so the focus target has to skip it too,
        // otherwise the auto focus lands on a disabled input and silently does nothing.
        Assert.AreEqual(1, FocusInvocationCount());
        Assert.AreEqual(component.FindAll(".bit-chg-icn input")[1].GetAttribute("blazor:elementreference"), FocusedElementId());
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotAutoFocusWhenReadOnly()
    {
        RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.AreEqual(0, FocusInvocationCount());
    }

    private int FocusInvocationCount()
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", System.StringComparison.OrdinalIgnoreCase));
    }

    // The id of the element the focus interop was called with, so a test can assert which input was focused
    // and not only that something was. It matches the blazor:elementreference attribute bUnit renders on the
    // elements captured with @ref.
    private string FocusedElementId()
    {
        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier.Contains("focus", System.StringComparison.OrdinalIgnoreCase));

        return ((ElementReference)invocation.Arguments[0]!).Id;
    }
}
