using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitCascadingValueOfTTests : BunitTestContext
{
    [TestMethod]
    public void ShouldCascadeAsTheTypeArgument()
    {
        var value = new BitCascadingValue<int?>(null, "Count", isFixed: true, enabled: false);

        Assert.AreEqual(typeof(int?), value.ValueType);
        Assert.IsNull(value.Value);
        Assert.AreEqual("Count", value.Name);
        Assert.IsTrue(value.IsFixed);
        Assert.IsFalse(value.Enabled);
    }

    [TestMethod]
    public void ShouldCreateTypedValuesFromTheFactories()
    {
        // The factories keep returning BitCascadingValue, as they did before BitCascadingValue<T> existed,
        // so the typed members are reached through the instance they create.
        var from = (BitCascadingValue<int>)BitCascadingValue.From(1);
        var fromFixedFlag = (BitCascadingValue<int>)BitCascadingValue.From(2, true);
        var fixedValue = (BitCascadingValue<string>)BitCascadingValue.Fixed("fixed");
        var lazy = (BitCascadingValue<int>)BitCascadingValue.Lazy(() => 3);
        var computed = (BitCascadingValue<int>)BitCascadingValue.Computed(() => 4);
        var observed = (BitCascadingValue<List<int>>)BitCascadingValue.Observed(new List<int>());

        Assert.AreEqual(1, from.Value);
        Assert.IsTrue(fromFixedFlag.IsFixed);
        Assert.IsTrue(fixedValue.IsFixed);
        Assert.IsFalse(lazy.IsValueCreated);
        Assert.AreEqual(3, lazy.Value);
        Assert.IsTrue(lazy.IsValueCreated);
        Assert.IsTrue(computed.IsComputed);
        Assert.AreEqual(4, computed.Value);
        Assert.IsTrue(observed.AutoNotify);
    }

    [TestMethod]
    public void ShouldAddTypedValuesThroughTheListHelpers()
    {
        var list = new BitCascadingValueList();

        list.Add(1, "One");
        list.AddIf(true, "two", "Two");
        list.AddFixed(3.0, "Three");
        list.Set<int?>(4, "Four");

        Assert.IsInstanceOfType<BitCascadingValue<int>>(list.Find<int>("One"));
        Assert.IsInstanceOfType<BitCascadingValue<string>>(list.Find<string>("Two"));
        Assert.IsInstanceOfType<BitCascadingValue<double>>(list.Find<double>("Three"));
        Assert.IsInstanceOfType<BitCascadingValue<int?>>(list.Find<int?>("Four"));
    }

    [TestMethod]
    public void ShouldCreateTypedValuesFromTheImplicitConversions()
    {
        BitCascadingValue number = 5;
        BitCascadingValue nullableNumber = ((int?)null, "Count");
        BitCascadingValue text = (string?)null;
        BitCascadingValue culture = CultureInfo.InvariantCulture;

        Assert.IsInstanceOfType<BitCascadingValue<int>>(number);
        Assert.IsInstanceOfType<BitCascadingValue<int?>>(nullableNumber);
        Assert.IsInstanceOfType<BitCascadingValue<string>>(text);
        Assert.IsInstanceOfType<BitCascadingValue<CultureInfo>>(culture);
        Assert.AreEqual(typeof(int?), nullableNumber.ValueType);
        Assert.AreEqual("Count", nullableNumber.Name);
    }

    [TestMethod]
    public void ShouldAddATypedValueAsItIsRatherThanCascadingTheValueObject()
    {
        var calls = 0;
        var typed = new BitCascadingValue<int>(1, "One");
        var text = new BitCascadingValue<string>("two", "Two");

        var list = new BitCascadingValueList
        {
            new BitCascadingValue<double>(3.0, "Three")
        };

        list.Add(typed);
        list.AddIf(true, text);
        list.AddIf(false, new BitCascadingValue<int>(4, "Four"));
        list.Add((BitCascadingValue<int>?)null);
        list.AddIf(true, (BitCascadingValue<int>?)null);
        list.AddIf(true, (BitCascadingValue<int?>)BitCascadingValue.Lazy<int?>(() => { calls++; return 5; }, "Five"));

        Assert.AreEqual(4, list.Count);
        Assert.AreSame(typed, list.Find<int>("One"));
        Assert.AreSame(text, list.Find<string>("Two"));
        Assert.IsNotNull(list.Find<double>("Three"));
        Assert.IsNotNull(list.Find<int?>("Five"));
        Assert.IsFalse(list.Any(item => item.ValueType.IsGenericType && item.ValueType.GetGenericTypeDefinition() == typeof(BitCascadingValue<>)));
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void ShouldOfferNoUntypedNewValueOverloadOnTheTypedValue()
    {
        // A NotifyChangedAsync(object?) the typed one did not hide would take a value of any type at compile
        // time and only throw at runtime.
        var overloads = typeof(BitCascadingValue<int>).GetMethods()
                                                       .Where(m => m.Name == nameof(BitCascadingValue.NotifyChangedAsync))
                                                       .Select(m => m.GetParameters())
                                                       .Where(p => p.Length == 1)
                                                       .ToList();

        Assert.AreEqual(1, overloads.Count);
        Assert.AreEqual(typeof(int), overloads[0][0].ParameterType);
    }

    [TestMethod]
    public void ShouldReadAndWriteTheSameValueThroughTheTypedAndTheUntypedProperty()
    {
        var value = BitCascadingValue.From("hello");
        BitCascadingValue untyped = value;

        value.Value = "typed";
        Assert.AreEqual("typed", untyped.Value);

        untyped.Value = "untyped";
        Assert.AreEqual("untyped", value.Value);

        Assert.ThrowsExactly<ArgumentException>(() => untyped.Value = 5);
    }

    [TestMethod]
    public void ShouldRaiseChangedWhenTheTypedValueIsAssigned()
    {
        var value = BitCascadingValue.From(1);
        var raised = 0;

        value.Changed += _ => raised++;

        value.Value = 1;
        Assert.AreEqual(0, raised);

        value.Value = 2;
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public async Task ShouldRefreshTheConsumersThroughTheTypedNotifyChangedAsync()
    {
        var greeting = new BitCascadingValue<string?>("hello", "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, [greeting]);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NullableConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("-hello", component.Markup);

        await greeting.NotifyChangedAsync("bye");

        Assert.AreEqual("-bye", component.Markup);

        await greeting.NotifyChangedAsync(null);

        Assert.AreEqual("-", component.Markup);
    }

    [TestMethod]
    public void ShouldStopTrackingTheSourceOnceANewValueIsNotified()
    {
        var source = "Light";
        var value = (BitCascadingValue<string>)BitCascadingValue.Computed(() => source, "Theme");

        Assert.AreEqual("Light", value.Value);

        _ = value.NotifyChangedAsync("Dark");
        source = "Blue";

        Assert.IsFalse(value.IsComputed);
        Assert.AreEqual("Dark", value.Value);
    }
}
