using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests.Components;

/// <summary>
/// <c>RenderInterval</c>: a store that changes many times a second re-renders its components at most once per
/// interval -- the first change at once, the rest collapsed into one trailing render of the latest state.
/// </summary>
[TestClass]
public class RenderThrottleTests : BunitContext
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    /// <summary>Follows a counter with a throttle, counting its renders.</summary>
    private sealed class Ticker : BlexComponentBase
    {
        [Parameter] public CounterStore Store { get; set; } = default!;

        public int Renders { get; private set; }

        protected override void OnInitialized()
        {
            RenderInterval = Interval;
            Subscribe(Store);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            builder.AddContent(0, Store.Count);
        }
    }

    private (CounterStore Store, FakeTimeProvider Clock) App()
    {
        var clock = new FakeTimeProvider();
        var manager = new BlexManager { TimeProvider = clock };
        var store = new CounterStore();
        manager.Register(store);
        Services.AddSingleton(manager);
        return (store, clock);
    }

    [TestMethod]
    public void BlexSelect_renders_the_first_change_at_once_and_the_rest_as_one_trailing_render()
    {
        var (store, clock) = App();
        var renders = 0;
        var cut = Render<BlexSelect<int>>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.Selector, () => store.Count)
            .Add(x => x.RenderInterval, Interval)
            .Add(x => x.ChildContent, (RenderFragment<int>)(value => content =>
            {
                renders++;
                content.AddContent(0, value);
            })));

        cut.InvokeAsync(store.Increment);
        Assert.AreEqual(2, renders, "the first change after a quiet interval renders at once");

        cut.InvokeAsync(store.Increment);
        cut.InvokeAsync(store.Increment);
        cut.InvokeAsync(store.Increment);
        Assert.AreEqual(2, renders, "within the interval: no render yet");
        cut.MarkupMatches("1");

        clock.Advance(Interval);
        cut.WaitForAssertion(() => cut.MarkupMatches("4"));
        Assert.AreEqual(3, renders, "one trailing render, of the latest value");
    }

    [TestMethod]
    public void BlexView_is_throttled_the_same_way()
    {
        var (store, clock) = App();
        var renders = 0;
        var cut = Render<BlexView>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.RenderInterval, Interval)
            .Add(x => x.ChildContent, (RenderFragment)(content =>
            {
                renders++;
                content.AddContent(0, store.Count);
            })));

        for (var i = 0; i < 5; i++)
            cut.InvokeAsync(store.Increment);

        Assert.AreEqual(2, renders);
        clock.Advance(Interval);
        cut.WaitForAssertion(() => cut.MarkupMatches("5"));
        Assert.AreEqual(3, renders);
    }

    [TestMethod]
    public void A_component_base_follows_its_RenderInterval_on_the_managers_clock()
    {
        var (store, clock) = App();
        var cut = Render<Ticker>(p => p.Add(x => x.Store, store));

        for (var i = 0; i < 10; i++)
            cut.InvokeAsync(store.Increment);

        Assert.AreEqual(2, cut.Instance.Renders);
        clock.Advance(Interval);
        cut.WaitForAssertion(() => cut.MarkupMatches("10"));
        Assert.AreEqual(3, cut.Instance.Renders);

        clock.Advance(Interval);   // quiet again: the next change renders at once
        cut.InvokeAsync(store.Increment);
        cut.MarkupMatches("11");
        Assert.AreEqual(4, cut.Instance.Renders);
    }

    [TestMethod]
    public async Task A_trailing_render_still_due_is_dropped_with_the_component()
    {
        var (store, clock) = App();
        var cut = Render<Ticker>(p => p.Add(x => x.Store, store));
        var ticker = cut.Instance;
        await cut.InvokeAsync(store.Increment);
        await cut.InvokeAsync(store.Increment);   // a trailing render is now due

        await DisposeComponentsAsync();
        clock.Advance(Interval);

        Assert.AreEqual(2, ticker.Renders);
    }

    [TestMethod]
    public void Without_an_interval_every_change_renders()
    {
        var (store, _) = App();
        var renders = 0;
        var cut = Render<BlexView>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.ChildContent, (RenderFragment)(_ => renders++)));

        for (var i = 0; i < 3; i++)
            cut.InvokeAsync(store.Increment);

        Assert.AreEqual(4, renders);
    }
}
