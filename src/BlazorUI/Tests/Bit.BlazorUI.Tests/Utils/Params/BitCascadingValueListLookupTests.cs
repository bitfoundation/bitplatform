using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitCascadingValueListLookupTests
{
    [TestMethod]
    public void ShouldAddADisabledValueConditionally()
    {
        var list = new BitCascadingValueList();

        list.AddIf(true, "hello", "Greeting", false, false);
        list.AddIf(false, "skipped", "Skipped");

        Assert.AreEqual(1, list.Count);
        Assert.IsFalse(list[0].Enabled);
        Assert.AreEqual("Greeting", list[0].Name);
    }

    [TestMethod]
    public void ShouldAddALazyValueWithAnExplicitValueType()
    {
        var calls = 0;
        var list = new BitCascadingValueList();

        list.AddLazy(() => { calls++; return (object?)null; }, typeof(string), "Greeting", true);

        Assert.AreEqual(typeof(string), list[0].ValueType);
        Assert.AreEqual("Greeting", list[0].Name);
        Assert.IsTrue(list[0].IsFixed);
        Assert.IsFalse(list[0].IsValueCreated);
        Assert.AreEqual(0, calls);

        Assert.IsNull(list[0].Value);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void ShouldAddAComputedValue()
    {
        var source = 1;
        var list = new BitCascadingValueList();

        list.AddComputed(() => source, "Count");

        Assert.IsTrue(list[0].IsComputed);
        Assert.AreEqual(typeof(int), list[0].ValueType);
        Assert.AreEqual(1, list[0].Value);

        source = 5;

        Assert.AreEqual(5, list[0].Value);
    }

    [TestMethod]
    public void ShouldAddAComputedValueWithAnExplicitValueType()
    {
        var source = "a";
        var list = new BitCascadingValueList();

        list.AddComputed(() => source, typeof(string), "Greeting", true);

        Assert.AreEqual(typeof(string), list[0].ValueType);
        Assert.IsTrue(list[0].IsFixed);
        Assert.AreEqual("a", list[0].Value);

        source = "b";

        Assert.AreEqual("b", list[0].Value);
    }

    [TestMethod]
    public void ShouldAddAnObservedValue()
    {
        var state = new NotifyingCascadingState();
        var list = new BitCascadingValueList();
        var raised = 0;

        list.AddObserved(state, "State");

        Assert.IsTrue(list[0].AutoNotify);
        Assert.AreEqual("State", list[0].Name);

        list[0].Changed += _ => raised++;

        state.Text = "changed";

        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void ShouldFindTheEntryThatATypeAndANameResolveTo()
    {
        var list = new BitCascadingValueList
        {
            { "first", "Greeting" },
            { "second", "Greeting" },
            { 5 },
            { "unnamed" }
        };

        Assert.AreEqual("second", list.Find<string>("Greeting")?.Value);
        Assert.AreEqual("second", list.Find(typeof(string), "GREETING")?.Value);
        Assert.AreEqual("unnamed", list.Find<string>()?.Value);
        Assert.AreEqual(5, list.Find<int>()?.Value);
        Assert.IsNull(list.Find<int>("Greeting"));
        Assert.IsNull(list.Find<bool>());
    }

    [TestMethod]
    public void ShouldTreatAnEmptyNameAsNoNameWhenLookingUp()
    {
        var list = new BitCascadingValueList { { "unnamed" } };

        Assert.IsNotNull(list.Find<string>("   "));
        Assert.IsTrue(list.Contains<string>(""));
    }

    [TestMethod]
    public void ShouldThrowWhenTheLookedUpValueTypeIsNull()
    {
        var list = new BitCascadingValueList();

        Assert.ThrowsExactly<ArgumentNullException>(() => list.Find(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => list.Remove(null!));
    }

    [TestMethod]
    public void ShouldReportWhetherTheListHoldsAValue()
    {
        var list = new BitCascadingValueList();

        list.Add("hello", "Greeting");
        list.Add(5, name: null, isFixed: false, enabled: false);

        Assert.IsTrue(list.Contains<string>("Greeting"));
        Assert.IsTrue(list.Contains<string>("greeting"));
        Assert.IsFalse(list.Contains<string>());
        Assert.IsTrue(list.Contains<int>());
    }

    [TestMethod]
    public void ShouldRemoveEveryEntryOfATypeAndAName()
    {
        var list = new BitCascadingValueList
        {
            { "first", "Greeting" },
            { "second", "GREETING" },
            { "unnamed" },
            { 5 }
        };

        Assert.IsTrue(list.Remove<string>("Greeting"));
        Assert.AreEqual(2, list.Count);
        Assert.IsFalse(list.Contains<string>("Greeting"));
        Assert.IsTrue(list.Contains<string>());
        Assert.IsTrue(list.Contains<int>());

        Assert.IsFalse(list.Remove<string>("Greeting"));
    }

    [TestMethod]
    public void ShouldReplaceEveryMatchingEntryWhenSetting()
    {
        var list = new BitCascadingValueList
        {
            { "first", "Greeting" },
            { "second", "Greeting" },
            { 5 }
        };

        list.Set("third", "Greeting", true);

        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("third", list[0].Value);
        Assert.IsTrue(list[0].IsFixed);
        Assert.AreEqual(5, list[1].Value);
    }

    [TestMethod]
    public void ShouldAddTheValueWhenSettingSomethingTheListDoesNotHold()
    {
        var list = new BitCascadingValueList();

        list.Set<int?>(null, "Count");

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(typeof(int?), list[0].ValueType);
        Assert.AreEqual("Count", list[0].Name);
        Assert.IsNull(list[0].Value);
    }
}
