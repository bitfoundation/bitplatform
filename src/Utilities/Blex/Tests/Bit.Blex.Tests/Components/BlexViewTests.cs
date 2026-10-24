using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Tests.Components;

/// <summary><see cref="BlexView"/>: content kept current with whole stores, without a base class.</summary>
[TestClass]
public class BlexViewTests : BunitContext
{
    [TestMethod]
    public void The_content_follows_its_store()
    {
        var store = new CounterStore();

        var cut = Render<BlexView>(p => p
            .Add(x => x.Store, store)
            .AddChildContent(content => content.AddContent(0, $"{store.Count}/{store.Label}")));

        cut.InvokeAsync(store.Increment);
        cut.InvokeAsync(() => store.Label = "busy");

        cut.MarkupMatches("1/busy");
    }

    [TestMethod]
    public void Several_stores_are_followed_at_once()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var renders = 0;

        Render<BlexView>(p => p
            .Add(x => x.Store, counter)
            .Add(x => x.Stores, new IBlexStore[] { profile, counter })
            .AddChildContent(_ => renders++));

        counter.Increment();
        profile.SignIn("ada");

        Assert.AreEqual(3, renders, "Initial render plus one per change; the store listed twice is followed once.");
    }

    [TestMethod]
    public void A_store_dropped_from_the_parameters_is_no_longer_followed()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var renders = 0;
        RenderFragment content = _ => renders++;

        var cut = Render<BlexView>(p => p.Add(x => x.Store, counter).AddChildContent(content));
        cut.Render(p => p.Add(x => x.Store, profile).AddChildContent(content));
        var before = renders;

        counter.Increment();
        Assert.AreEqual(before, renders);

        profile.SignIn("ada");
        Assert.AreEqual(before + 1, renders);
    }

    [TestMethod]
    public async Task Disposal_detaches_from_every_store()
    {
        var counter = new CounterStore();
        var renders = 0;

        Render<BlexView>(p => p.Add(x => x.Stores, new IBlexStore[] { counter }).AddChildContent(_ => renders++));
        await DisposeComponentsAsync();
        counter.Increment();

        Assert.AreEqual(1, renders);
    }
}
