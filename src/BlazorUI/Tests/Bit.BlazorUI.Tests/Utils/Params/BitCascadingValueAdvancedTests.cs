using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitCascadingValueAdvancedTests
{
    [TestMethod]
    public void ShouldRunTheComputedFactoryOnEveryRead()
    {
        var calls = 0;
        var value = BitCascadingValue.Computed(() => ++calls);

        Assert.IsTrue(value.IsComputed);
        Assert.IsTrue(value.IsValueCreated);
        Assert.AreEqual(typeof(int), value.ValueType);

        Assert.AreEqual(1, value.Value);
        Assert.AreEqual(2, value.Value);
        Assert.AreEqual(3, value.Value);
        Assert.AreEqual(3, calls);
    }

    [TestMethod]
    public void ShouldCreateAComputedValueWithAnExplicitValueType()
    {
        var source = 7;
        var value = BitCascadingValue.Computed(() => (int?)source, typeof(int?), "Count", true);

        Assert.AreEqual(typeof(int?), value.ValueType);
        Assert.AreEqual("Count", value.Name);
        Assert.IsTrue(value.IsFixed);
        Assert.AreEqual(7, value.Value);

        source = 9;

        Assert.AreEqual(9, value.Value);
    }

    [TestMethod]
    public void ShouldValidateTheValueTheComputedFactoryProduces()
    {
        var value = BitCascadingValue.Computed(() => (object?)"not an int", typeof(int));

        Assert.ThrowsExactly<ArgumentException>(() => _ = value.Value);
    }

    [TestMethod]
    public void ShouldThrowWhenTheComputedFactoryIsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitCascadingValue.Computed<int>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => BitCascadingValue.Computed(null!, typeof(int)));
    }

    [TestMethod]
    public void ShouldDropTheComputedFactoryWhenTheValueIsAssigned()
    {
        var calls = 0;
        var raised = 0;
        var value = BitCascadingValue.Computed(() => ++calls);

        value.Changed += _ => raised++;

        value.Value = 42;

        Assert.IsFalse(value.IsComputed);
        Assert.AreEqual(42, value.Value);
        Assert.AreEqual(42, value.Value);
        Assert.AreEqual(0, calls);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void ShouldMarkAComputedValueInTheStringRepresentation()
    {
        var value = BitCascadingValue.Computed(() => 3, "Count");

        Assert.AreEqual("Count: Int32 = 3 (computed)", value.ToString());
    }

    [TestMethod]
    public void ShouldRunTheLazyFactoryOnlyOnceUnderConcurrentReads()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var calls = 0;
            var value = BitCascadingValue.Lazy(() =>
            {
                Interlocked.Increment(ref calls);

                Thread.Sleep(1);

                return new object();
            });

            var results = new object?[8];

            Parallel.For(0, results.Length, i => results[i] = value.Value);

            Assert.AreEqual(1, calls);

            foreach (var result in results)
            {
                Assert.IsNotNull(result);
                Assert.AreSame(results[0], result);
            }
        }
    }

    [TestMethod]
    public void ShouldNotReRunALazyFactoryThatReadsItsOwnValue()
    {
        var calls = 0;
        BitCascadingValue? value = null;

        value = BitCascadingValue.Lazy<object?>(() =>
        {
            calls++;

            return value!.Value;
        });

        Assert.IsNull(value.Value);
        Assert.AreEqual(1, calls);
        Assert.IsTrue(value.IsValueCreated);
    }

    [TestMethod]
    public void ShouldMirrorAnotherValueThroughAComputedOne()
    {
        var source = new BitCascadingValue(new CascadingDemoService(), typeof(CascadingDemoService));
        var alias = BitCascadingValue.Computed(() => (ICascadingDemoService?)source.Value, typeof(ICascadingDemoService), "Alias");

        Assert.AreSame(source.Value, alias.Value);

        source.Value = new CascadingDemoServiceDecorator();

        Assert.AreSame(source.Value, alias.Value);
    }

    [TestMethod]
    public void ShouldCompleteTheNotifyChangedTaskWhenNothingIsListening()
    {
        var value = new BitCascadingValue(1);

        var task = value.NotifyChangedAsync();

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ShouldAwaitEveryAsyncListenerOfNotifyChangedAsync()
    {
        var value = new BitCascadingValue(1);
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        var syncRaised = 0;

        value.Changed += _ => syncRaised++;
        value.ChangedAsync += _ => first.Task;
        value.ChangedAsync += _ => second.Task;

        var task = value.NotifyChangedAsync();

        Assert.AreEqual(1, syncRaised);
        Assert.IsFalse(task.IsCompleted);

        first.SetResult();

        Assert.IsFalse(task.IsCompleted);

        second.SetResult();

        task.GetAwaiter().GetResult();

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ShouldTreatANullTaskFromAnAsyncListenerAsACompletedOne()
    {
        var value = new BitCascadingValue(1);

        value.ChangedAsync += _ => null!;

        Assert.IsTrue(value.NotifyChangedAsync().IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ShouldRaiseTheAsyncEventWhenAPropertyIsAssigned()
    {
        var value = new BitCascadingValue(1);
        var raised = 0;

        value.ChangedAsync += _ =>
        {
            raised++;

            return Task.CompletedTask;
        };

        value.Value = 2;
        value.Name = "Number";
        value.IsFixed = true;
        value.Enabled = false;

        Assert.AreEqual(4, raised);
    }

    [TestMethod]
    public void ShouldNotWatchTheValueWhenAutoNotifyIsOff()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.From(state);
        var raised = 0;

        value.Changed += _ => raised++;

        state.Text = "changed";

        Assert.IsFalse(value.AutoNotify);
        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void ShouldRaiseChangedWhenAnObservedValueReportsAPropertyChange()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state);
        var raised = 0;

        Assert.IsTrue(value.AutoNotify);

        void Handler(BitCascadingValue _) => raised++;

        value.Changed += Handler;

        state.Text = "first";
        state.Text = "second";

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public void ShouldRaiseChangedWhenAnObservedCollectionChanges()
    {
        var names = new ObservableCollection<string> { "a" };
        var value = BitCascadingValue.Observed(names);
        var raised = 0;

        value.Changed += _ => raised++;

        names.Add("b");
        names.RemoveAt(0);

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public void ShouldOnlyWatchAnObservedValueWhileSomethingIsListening()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state);
        var raised = 0;

        void Handler(BitCascadingValue _) => raised++;

        state.Text = "before";

        Assert.AreEqual(0, raised);

        value.Changed += Handler;

        state.Text = "while";

        Assert.AreEqual(1, raised);

        value.Changed -= Handler;

        state.Text = "after";

        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void ShouldMoveTheObservationToTheNewlyAssignedValue()
    {
        var first = new NotifyingCascadingState();
        var second = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(first);
        var raised = 0;

        value.Changed += _ => raised++;

        value.Value = second;

        Assert.AreEqual(1, raised);

        first.Text = "ignored";

        Assert.AreEqual(1, raised);

        second.Text = "watched";

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public void ShouldStartWatchingAnObservedValueOnlyOnceItsLazyFactoryHasRun()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Lazy(() => state);
        var raised = 0;

        value.AutoNotify = true;
        value.Changed += _ => raised++;

        state.Text = "before";

        Assert.IsFalse(value.IsValueCreated);
        Assert.AreEqual(0, raised);

        _ = value.Value;

        state.Text = "after";

        Assert.IsTrue(value.IsValueCreated);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void ShouldTurnTheObservationOffAgainWithTheAutoNotifyFlag()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state);
        var raised = 0;

        value.Changed += _ => raised++;

        state.Text = "watched";

        Assert.AreEqual(1, raised);

        value.AutoNotify = false;

        state.Text = "ignored";

        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void ShouldCreateAnObservedValueThatIsNamedAndDisabled()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state, "State", false);

        Assert.AreEqual("State", value.Name);
        Assert.IsFalse(value.Enabled);
        Assert.IsTrue(value.AutoNotify);
        Assert.AreEqual(typeof(NotifyingCascadingState), value.ValueType);
    }

    [TestMethod]
    public void ShouldCreateAFixedTypedValueWithTheIsFixedOverloadOfFrom()
    {
        var value = BitCascadingValue.From<int?>(null, true);

        Assert.AreEqual(typeof(int?), value.ValueType);
        Assert.IsTrue(value.IsFixed);
        Assert.IsTrue(value.Enabled);
        Assert.IsNull(value.Name);
        Assert.IsNull(value.Value);
    }

    [TestMethod]
    public void ShouldCreateADisabledTypedValueWithTheIsFixedOverloadOfFrom()
    {
        var value = BitCascadingValue.From("hello", true, false);

        Assert.IsTrue(value.IsFixed);
        Assert.IsFalse(value.Enabled);
    }
}
