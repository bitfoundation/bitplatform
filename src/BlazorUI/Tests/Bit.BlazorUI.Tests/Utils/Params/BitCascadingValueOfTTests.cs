using System;
using System.Collections.Generic;
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
        BitCascadingValue<int> from = BitCascadingValue.From(1);
        BitCascadingValue<int> fromFixedFlag = BitCascadingValue.From(2, true);
        BitCascadingValue<string> fixedValue = BitCascadingValue.Fixed("fixed");
        BitCascadingValue<int> lazy = BitCascadingValue.Lazy(() => 3);
        BitCascadingValue<int> computed = BitCascadingValue.Computed(() => 4);
        BitCascadingValue<List<int>> observed = BitCascadingValue.Observed(new List<int>());

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
        var greeting = BitCascadingValue.From<string?>("hello", "Greeting");

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
}
