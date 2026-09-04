using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public partial class BitCascadingValueProviderReactivityTests : BunitTestContext
{
    [TestMethod]
    public void ShouldReTargetTheConsumersWhenACascadedValueIsRenamed()
    {
        var greeting = new BitCascadingValue("hello", "First");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<DualNameConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("hello-none");

        greeting.Name = "Second";

        component.WaitForAssertion(() => component.MarkupMatches("none-hello"));

        greeting.Name = "First";

        component.WaitForAssertion(() => component.MarkupMatches("hello-none"));
    }

    [TestMethod]
    public void ShouldKeepTheOtherValuesWorkingWhenOneOfThemIsRenamed()
    {
        var greeting = new BitCascadingValue("hello", "First");
        var number = new BitCascadingValue(7);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting, number });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual(7, component.FindComponent<CascadingConsumer>().Instance.Number);

        greeting.Name = "Greeting";

        component.WaitForAssertion(() => Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting));
        Assert.AreEqual(7, component.FindComponent<CascadingConsumer>().Instance.Number);
    }

    [TestMethod]
    public void ShouldReReadAComputedValueOnEveryRender()
    {
        var count = 0;
        var greeting = BitCascadingValue.Computed(() => $"render-{++count}", "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var initial = component.Markup;

        Assert.IsTrue(count >= 1);
        Assert.AreEqual($"0-render-{count}", initial);

        greeting.NotifyChanged();

        component.WaitForAssertion(() => Assert.AreNotEqual(initial, component.Markup));
        Assert.AreEqual($"0-render-{count}", component.Markup);
    }

    [TestMethod]
    public void ShouldRefreshTheConsumersWhenAnObservedValueReportsAPropertyChange()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { value });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NotifyingStateConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("initial");

        state.Text = "updated";

        component.WaitForAssertion(() => component.MarkupMatches("updated"));
    }

    [TestMethod]
    public void ShouldRefreshTheConsumersWhenAnObservedCollectionChanges()
    {
        var names = new ObservableCollection<string> { "a" };
        var value = BitCascadingValue.Observed(names);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { value });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<ObservableNamesConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("a");

        names.Add("b");

        component.WaitForAssertion(() => component.MarkupMatches("a,b"));
    }

    [TestMethod]
    public void ShouldStopWatchingAnObservedValueOnceTheProviderIsDisposed()
    {
        var state = new NotifyingCascadingState();
        var value = BitCascadingValue.Observed(state);

        RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { value });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NotifyingStateConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(state.HasSubscribers);

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        Assert.IsFalse(state.HasSubscribers);

        state.Text = "after-dispose";
    }

    [TestMethod]
    public async Task ShouldCompleteTheNotifyChangedTaskOnceTheConsumersHaveBeenRefreshed()
    {
        var service = new MutableCascadingDemoService();
        var value = BitCascadingValue.From<ICascadingDemoService>(service);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { value });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingServiceConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("initial");

        service.Name = "mutated";

        await value.NotifyChangedAsync();

        component.MarkupMatches("mutated");
    }

    [TestMethod]
    public void ShouldCascadeAValueThatIsListedMoreThanOnceExactlyOnce()
    {
        var greeting = new BitCascadingValue("hello", "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting, greeting, greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual(1, component.FindComponents<CascadingValue<string>>().Count);
        Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting);

        greeting.Value = "changed";

        component.WaitForAssertion(() => Assert.AreEqual("changed", component.FindComponent<CascadingConsumer>().Instance.Greeting));
    }

    [TestMethod]
    public void ShouldKeepTheRemainingValuesIntactWhenAnEarlierOneIsDisabled()
    {
        var first = new BitCascadingValue("hello", "Greeting");
        var second = new BitCascadingValue(7);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { first, second });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("7-hello");

        first.Enabled = false;

        component.WaitForAssertion(() => component.MarkupMatches("7-"));

        first.Enabled = true;

        component.WaitForAssertion(() => component.MarkupMatches("7-hello"));
    }

    [TestMethod]
    public void ShouldNotReuseACascadingValueForADifferentValueThatTakesItsPlace()
    {
        var values = new List<BitCascadingValue>
        {
            new("first", "First"),
            new("second", "Second")
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, values);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<DualNameConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("first-second");

        values[0].Enabled = false;

        component.WaitForAssertion(() => component.MarkupMatches("none-second"));
    }
}
