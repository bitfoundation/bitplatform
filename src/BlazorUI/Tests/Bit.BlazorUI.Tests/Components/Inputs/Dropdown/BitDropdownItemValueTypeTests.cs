using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// Covers a dropdown whose built-in items carry a value type other than its TValue: the BitDropdownItem&lt;Guid&gt; of a
/// dropdown bound to a Guid? (ids are rarely nullable while the property a dropdown is bound to often is) works as a
/// BitDropdownItem&lt;Guid?&gt; would, the values of other items are read the way the NameSelectors read them, the
/// NameSelectors still decide how such items are read when they are given, and a dropdown of options of another TValue
/// (which never reach it) fails before it does anything instead of rendering without a single option.
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
    public void BitDropdownShouldShowTheItemOfAValueFromNullableItemsOfItsType()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid?>, Guid>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = _oneId }, new() { Text = "Two", Value = _twoId }]);
            parameters.Add(p => p.Value, _twoId);
        });

        Assert.AreEqual("Two", component.Find(".bit-drp-tdp").InnerHtml);
    }

    [TestMethod]
    public void BitDropdownShouldShowTheItemOfAValueFromItemsOfATypeThatConvertsToIt()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var changes = new List<long>();

        var component = RenderComponent<BitDropdown<BitDropdownItem<int>, long>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Add(p => p.Value, 2L);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ValueChanged, (long value) => changes.Add(value));
        });

        Assert.AreEqual("Two", component.Find(".bit-drp-tdp").InnerHtml);

        component.FindAll(".bit-drp-itm")[0].Click();

        CollectionAssert.AreEqual(new long[] { 1 }, changes);
    }

    [TestMethod]
    public void BitDropdownShouldRenderItemsWhoseValuesAreNoTValueWithoutMatchingAnyOfThem()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var bound = 7;
        var selected = new List<BitDropdownItem<Guid>>();

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, int>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.Value, bound, v => bound = v);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnSelectItem, (BitDropdownItem<Guid> item) => selected.Add(item));
        });

        var drpItems = component.FindAll(".bit-drp-itm");

        Assert.AreEqual(2, drpItems.Count);
        Assert.AreEqual("One", drpItems[0].TextContent.Trim());
        Assert.IsTrue(component.Instance.Items!.All(item => item.IsSelected is false));

        drpItems[1].Click();

        Assert.AreEqual(1, selected.Count);
        Assert.AreEqual("Two", selected[0].Text);
    }

    [TestMethod]
    public void BitDropdownShouldReadMismatchedItemsThroughTheGivenNameSelectors()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<int>, long>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }]);
            parameters.Add(p => p.NameSelectors, new() { Value = { Selector = item => item.Value * 10L } });
            parameters.Add(p => p.Value, 20L);
        });

        Assert.AreEqual("Two", component.Find(".bit-drp-tdp").InnerHtml);
    }

    [TestMethod]
    public void BitDropdownShouldReadTheItemsOfTheUnderlyingTypeThroughTheGivenNameSelectors()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDropdown<BitDropdownItem<Guid>, Guid?>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.NameSelectors, new() { Text = { Selector = item => $"{item.Text}!" } });
            parameters.Add(p => p.Value, _twoId);
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("Two!", component.Find(".bit-drp-tdp").InnerHtml);
        Assert.AreEqual("One!", component.FindAll(".bit-drp-itm")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDropdownShouldAddADynamicItemOfTheUnderlyingType()
    {
        var newId = Guid.NewGuid();
        var added = new List<BitDropdownItem<Guid>>();

        var component = RenderDynamicDropdown<BitDropdownItem<Guid>, Guid?>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<Guid>? _) => newId);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<Guid> item) => added.Add(item));
        });

        component.Find(".bit-drp-inp").Input("Three");

        Assert.HasCount(1, component.FindAll(".bit-drp-dyn"));

        AddDynamicItem(component, "Three");

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual("Three", added[0].Text);
        Assert.AreEqual("Three", added[0].Title);
        Assert.AreEqual(newId, added[0].Value);
        Assert.AreEqual(newId, component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldAddDynamicItemsOfTheUnderlyingTypeToAMultiSelect()
    {
        var ids = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var expected = ids.ToArray();
        var added = new List<BitDropdownItem<Guid>>();

        var component = RenderDynamicDropdown<BitDropdownItem<Guid>, Guid?>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<Guid>? _) => ids.Dequeue());
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<Guid> item) => added.Add(item));
        });

        AddDynamicItem(component, "Three");
        AddDynamicItem(component, "Four");

        CollectionAssert.AreEqual(expected, added.Select(item => item.Value).ToArray());
        CollectionAssert.AreEquivalent(expected.Select(id => (Guid?)id).ToArray(), component.Instance.Values!.ToArray());
    }

    [TestMethod]
    public void BitDropdownShouldNotOfferOrAddADynamicItemOfTheUnderlyingTypeWithoutAValueGenerator()
    {
        Guid? bound = null;
        var added = new List<BitDropdownItem<Guid>>();

        var component = RenderDynamicDropdown<BitDropdownItem<Guid>, Guid?>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.Value, bound, v => bound = v);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<Guid> item) => added.Add(item));
        });

        component.Find(".bit-drp-inp").Input("Three");

        Assert.IsEmpty(component.FindAll(".bit-drp-dyn"));

        AddDynamicItem(component, "Three");

        Assert.IsEmpty(added);
        Assert.IsNull(bound);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldNotAddADynamicItemOfTheUnderlyingTypeThatIsGivenNoValue()
    {
        Guid? bound = null;
        var added = new List<BitDropdownItem<Guid>>();

        var component = RenderDynamicDropdown<BitDropdownItem<Guid>, Guid?>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Bind(p => p.Value, bound, v => bound = v);
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<Guid>? _) => (Guid?)null);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<Guid> item) => added.Add(item));
        });

        AddDynamicItem(component, "Three");

        Assert.IsEmpty(added);
        Assert.IsNull(bound);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldAddADynamicItemOfATypeThatTheGeneratedValueConvertsTo()
    {
        var added = new List<BitDropdownItem<int>>();

        var component = RenderDynamicDropdown<BitDropdownItem<int>, long>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }]);
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<int>? _) => 42L);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<int> item) => added.Add(item));
        });

        AddDynamicItem(component, "Two");

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual(42, added[0].Value);
        Assert.AreEqual(42L, component.Instance.Value);
    }

    [TestMethod]
    public void BitDropdownShouldAddADynamicItemOfAnotherTypeThroughTheGivenValueSetter()
    {
        var added = new List<BitDropdownItem<int>>();

        var component = RenderDynamicDropdown<BitDropdownItem<int>, long>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "One", Value = 1 }]);
            parameters.Add(p => p.NameSelectors, new()
            {
                TextSetter = (text, item) => item.Text = $"New {text}",
                ValueSetter = (item, value) => item.Value = (int)value + 1
            });
            parameters.Add(p => p.DynamicValueGenerator, (BitDropdownItem<int>? _) => 42L);
            parameters.Add(p => p.OnDynamicAdd, (BitDropdownItem<int> item) => added.Add(item));
        });

        AddDynamicItem(component, "Two");

        Assert.AreEqual(1, added.Count);
        Assert.AreEqual("New Two", added[0].Text);
        Assert.AreEqual(43, added[0].Value);
        Assert.AreEqual(43L, component.Instance.Value);
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
    public void BitDropdownShouldThrowForOptionsOfAnotherTValueWhateverTheNameSelectors()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // NameSelectors say how items are read; they cannot make an option of another TValue reach the dropdown.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitDropdown<BitDropdownOption<Guid>, Guid?>>(parameters =>
            {
                parameters.Add(p => p.NameSelectors, new());
            }));
    }

    [TestMethod]
    public void BitDropdownShouldRefuseOptionsOfAnotherTValueBeforeApplyingItsDefaultValues()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var changes = new List<IEnumerable<Guid?>?>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitDropdown<BitDropdownOption<Guid>, Guid?>>(parameters =>
            {
                parameters.Add(p => p.MultiSelect, true);
                parameters.Add(p => p.DefaultValues, new Guid?[] { _oneId });
                parameters.Add(p => p.ValuesChanged, (IEnumerable<Guid?>? values) => changes.Add(values));
            }));

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    public void IBitDropdownItemShouldHaveAMemberForEveryPublicPropertyOfTheBuiltInItem()
    {
        var itemInterface = typeof(BitDropdownItem<>).Assembly.GetType("Bit.BlazorUI.IBitDropdownItem", throwOnError: true)!;
        var interfaceMembers = itemInterface.GetProperties().Select(p => p.Name).ToHashSet();

        // The value is read and set through methods typed by the TValue of the dropdown rather than as a property.
        Assert.IsNotNull(itemInterface.GetMethod("GetValue"));
        Assert.IsNotNull(itemInterface.GetMethod("TrySetValue"));
        interfaceMembers.Add(nameof(BitDropdownItem<int>.Value));

        var missing = typeof(BitDropdownItem<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Select(p => p.Name)
                                                  .Where(name => interfaceMembers.Contains(name) is false)
                                                  .ToArray();

        Assert.IsEmpty(missing, "A dropdown reads the built-in items of another T through IBitDropdownItem, which has no member for: " +
                                string.Join(", ", missing));
    }

    private IRenderedComponent<BitDropdown<TItem, TValue>> RenderDynamicDropdown<TItem, TValue>(Action<ComponentParameterCollectionBuilder<BitDropdown<TItem, TValue>>> parameterBuilder)
        where TItem : class, new()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        return RenderComponent<BitDropdown<TItem, TValue>>(parameters =>
        {
            parameters.Add(p => p.Combo, true);
            parameters.Add(p => p.Dynamic, true);
            parameterBuilder(parameters);
        });
    }

    // Enter commits the text the input holds, which the dropdown reads from the DOM.
    private void AddDynamicItem<TItem, TValue>(IRenderedComponent<BitDropdown<TItem, TValue>> component, string text)
        where TItem : class, new()
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult(text);

        component.Find(".bit-drp-inp").Input(text);
        component.Find(".bit-drp-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    }
}
