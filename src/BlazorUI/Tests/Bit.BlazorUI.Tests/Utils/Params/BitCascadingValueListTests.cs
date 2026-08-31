using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitCascadingValueListTests
{
    [TestMethod]
    public void ShouldAddATypedValueUsingTheStaticTypeOfTheArgument()
    {
        var list = new BitCascadingValueList();
        list.Add<int?>(5, "Number", isFixed: true);

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(typeof(int?), list[0].ValueType);
        Assert.AreEqual(5, list[0].Value);
        Assert.AreEqual("Number", list[0].Name);
        Assert.IsTrue(list[0].IsFixed);
        Assert.IsTrue(list[0].Enabled);
    }

    [TestMethod]
    public void ShouldAddADisabledTypedValue()
    {
        var list = new BitCascadingValueList();
        list.Add("hi", "Greeting", enabled: false);

        Assert.IsFalse(list[0].Enabled);
    }

    [TestMethod]
    public void ShouldAddANullTypedValue()
    {
        var list = new BitCascadingValueList();
        list.Add<string?>(null, "Greeting");
        list.Add<int?>(null);

        Assert.AreEqual(typeof(string), list[0].ValueType);
        Assert.IsNull(list[0].Value);
        Assert.AreEqual(typeof(int?), list[1].ValueType);
        Assert.IsNull(list[1].Value);
    }

    [TestMethod]
    public void ShouldAddAnAlreadyCreatedCascadingValueAsIs()
    {
        var cascadingValue = new BitCascadingValue("hi", "Greeting");

        var list = new BitCascadingValueList { cascadingValue };

        Assert.AreEqual(1, list.Count);
        Assert.AreSame(cascadingValue, list[0]);
        Assert.AreEqual(typeof(string), list[0].ValueType);
    }

    [TestMethod]
    public void ShouldIgnoreANullCascadingValue()
    {
        var list = new BitCascadingValueList();
        list.Add((BitCascadingValue?)null);

        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void ShouldAddAValueWithAnExplicitValueType()
    {
        var list = new BitCascadingValueList();
        list.Add(new CascadingDemoServiceDecorator(), typeof(ICascadingDemoService), "Service", isFixed: true);

        Assert.AreEqual(typeof(ICascadingDemoService), list[0].ValueType);
        Assert.AreEqual("Service", list[0].Name);
        Assert.IsTrue(list[0].IsFixed);
    }

    [TestMethod]
    public void ShouldAddConditionally()
    {
        var list = new BitCascadingValueList();
        list.AddIf(true, "kept", "Greeting");
        list.AddIf(false, "dropped", "Greeting");

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("kept", list[0].Value);
    }

    [TestMethod]
    public void ShouldAddAFixedValue()
    {
        var list = new BitCascadingValueList();
        list.AddFixed<int?>(5, "Number");

        Assert.AreEqual(typeof(int?), list[0].ValueType);
        Assert.IsTrue(list[0].IsFixed);
        Assert.AreEqual("Number", list[0].Name);
    }

    [TestMethod]
    public void ShouldSupportTheCollectionInitializerSyntax()
    {
        var list = new BitCascadingValueList
        {
            { "hi", "Greeting" },
            { 5 },
            { (int?)null, "Count" }
        };

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(typeof(string), list[0].ValueType);
        Assert.AreEqual(typeof(int), list[1].ValueType);
        Assert.AreEqual(typeof(int?), list[2].ValueType);
        Assert.IsNull(list[2].Value);
    }

    [TestMethod]
    public void ShouldSupportTheEnumerableConstructor()
    {
        var source = new List<BitCascadingValue> { new(5), new("hi", "Greeting") };

        var list = new BitCascadingValueList(source);

        Assert.AreEqual(2, list.Count);
        Assert.AreSame(source[0], list[0]);
    }

    [TestMethod]
    public void ShouldThrowWhenTheValueAndTheExplicitValueTypeDoNotMatch()
    {
        var list = new BitCascadingValueList();

        Assert.ThrowsExactly<ArgumentException>(() => list.Add("not-a-service", typeof(ICascadingDemoService)));
    }

    [TestMethod]
    public void ShouldAddADisabledValueWithAnExplicitValueType()
    {
        var list = new BitCascadingValueList();
        list.Add(new CascadingDemoServiceDecorator(), typeof(ICascadingDemoService), "Service", enabled: false);

        Assert.IsFalse(list[0].Enabled);
    }

    [TestMethod]
    public void ShouldAddAnAlreadyCreatedCascadingValueConditionally()
    {
        var kept = new BitCascadingValue("kept", "Greeting");
        var dropped = new BitCascadingValue("dropped", "Greeting");

        var list = new BitCascadingValueList();
        list.AddIf(true, kept);
        list.AddIf(false, dropped);
        list.AddIf(true, (BitCascadingValue?)null);

        Assert.AreEqual(1, list.Count);
        Assert.AreSame(kept, list[0]);
    }

    [TestMethod]
    public void ShouldAddAFixedValueWithAnExplicitValueType()
    {
        var list = new BitCascadingValueList();
        list.AddFixed(new CascadingDemoServiceDecorator(), typeof(ICascadingDemoService), "Service");

        Assert.AreEqual(typeof(ICascadingDemoService), list[0].ValueType);
        Assert.AreEqual("Service", list[0].Name);
        Assert.IsTrue(list[0].IsFixed);
    }

    [TestMethod]
    public void ShouldAddALazyValueWhoseFactoryHasNotRunYet()
    {
        var calls = 0;

        var list = new BitCascadingValueList();
        list.AddLazy<int?>(() => { calls++; return 5; }, "Number", isFixed: true, enabled: false);

        Assert.AreEqual(typeof(int?), list[0].ValueType);
        Assert.AreEqual("Number", list[0].Name);
        Assert.IsTrue(list[0].IsFixed);
        Assert.IsFalse(list[0].Enabled);
        Assert.IsFalse(list[0].IsValueCreated);
        Assert.AreEqual(0, calls);

        Assert.AreEqual(5, list[0].Value);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void ShouldKeepALazyValueUnbuiltWhenItsConditionDoesNotHold()
    {
        var calls = 0;

        var list = new BitCascadingValueList();
        list.AddIf(false, BitCascadingValue.Lazy<int?>(() => { calls++; return 5; }));

        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(0, calls);
    }
}
