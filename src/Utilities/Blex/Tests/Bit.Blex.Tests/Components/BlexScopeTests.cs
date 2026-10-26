using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests.Components;

public sealed class GreetingSource
{
    public string Text { get; init; } = "hello";
}

/// <summary>A store with a constructor dependency, which a scope resolves from DI.</summary>
[BlexStoreAttribute(Name = "greeting")]
public partial class GreetingStore
{
    [BlexStateAttribute] private string _text;

    public GreetingStore(GreetingSource source) => _text = source.Text;

    [BlexActionAttribute]
    private void OnShout() => Text = Text.ToUpperInvariant();
}

/// <summary>
/// <see cref="BlexScope{TStore}"/>: an instance of a store for one part of the page, cascaded to its
/// descendants and gone with it.
/// </summary>
[TestClass]
public class BlexScopeTests : BunitContext
{
    /// <summary>A descendant that receives the scoped store through a cascading parameter.</summary>
    private sealed class Reader : ComponentBase
    {
        [CascadingParameter] public CounterStore? Store { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, Store is null ? "none" : $"{Store.Name}={Store.Count}");
    }

    private BlexManager Manager => Services.GetRequiredService<BlexManager>();

    public BlexScopeTests()
    {
        Services.AddBitBlexServices();
        Services.AddSingleton(new GreetingSource { Text = "hi" });
    }

    [TestMethod]
    public void The_content_gets_its_own_registered_instance()
    {
        var cut = Render<BlexScope<CounterStore>>(p => p
            .Add(x => x.Key, "a")
            .Add(x => x.ChildContent, store => $"<i>{store.Name}</i>"));

        cut.MarkupMatches("<i>counter:a</i>");
        Assert.AreSame(cut.Instance.Store, Manager.GetStore("counter:a"));
    }

    [TestMethod]
    public void Descendants_receive_the_instance_as_a_cascading_value()
    {
        var cut = Render<BlexScope<CounterStore>>(p => p
            .Add(x => x.Key, "a")
            .Add(x => x.ChildContent, (RenderFragment<CounterStore>)(_ => content =>
            {
                content.OpenComponent<Reader>(0);
                content.CloseComponent();
            })));

        cut.MarkupMatches("counter:a=0");
    }

    [TestMethod]
    public void Constructor_dependencies_come_from_DI()
    {
        var cut = Render<BlexScope<GreetingStore>>(p => p.Add(x => x.ChildContent, store => store.Text));

        cut.MarkupMatches("hi");
    }

    [TestMethod]
    public void A_factory_builds_a_store_DI_cannot()
    {
        var cut = Render<BlexScope<GreetingStore>>(p => p
            .Add(x => x.Factory, _ => new GreetingStore(new GreetingSource { Text = "custom" }))
            .Add(x => x.ChildContent, store => store.Text));

        cut.MarkupMatches("custom");
    }

    [TestMethod]
    public void A_new_key_replaces_the_instance()
    {
        var cut = Render<BlexScope<CounterStore>>(p => p
            .Add(x => x.Key, "a")
            .Add(x => x.ChildContent, store => store.Name));
        var first = cut.Instance.Store;
        first.Add(5);

        cut.Render(p => p.Add(x => x.Key, "b"));

        cut.MarkupMatches("counter:b");
        Assert.AreNotSame(first, cut.Instance.Store);
        Assert.IsNull(Manager.GetStore("counter:a"));
        Assert.AreEqual(0, cut.Instance.Store.Count);
    }

    [TestMethod]
    public void The_same_key_keeps_the_instance_across_renders()
    {
        var cut = Render<BlexScope<CounterStore>>(p => p
            .Add(x => x.Key, "a")
            .Add(x => x.ChildContent, store => store.Name));
        var first = cut.Instance.Store;

        cut.Render(p => p.Add(x => x.Key, "a"));

        Assert.AreSame(first, cut.Instance.Store);
    }

    [TestMethod]
    public async Task Removing_the_scope_unregisters_its_store()
    {
        Render<BlexScope<CounterStore>>(p => p.Add(x => x.Key, "a").Add(x => x.ChildContent, store => store.Name));
        Assert.IsNotNull(Manager.GetStore("counter:a"));

        await DisposeComponentsAsync();

        Assert.IsNull(Manager.GetStore("counter:a"));
    }

    [TestMethod]
    public void Two_scopes_side_by_side_are_independent()
    {
        var left = Render<BlexScope<CounterStore>>(p => p.Add(x => x.ChildContent, store => store.Count.ToString()));
        var right = Render<BlexScope<CounterStore>>(p => p.Add(x => x.ChildContent, store => store.Count.ToString()));

        left.Instance.Store.Add(3);

        Assert.AreEqual(3, left.Instance.Store.Count);
        Assert.AreEqual(0, right.Instance.Store.Count);
        Assert.AreNotEqual(left.Instance.Store.Name, right.Instance.Store.Name);
    }
}
