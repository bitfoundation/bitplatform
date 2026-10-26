using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blex.Tests.Components;

/// <summary>
/// <see cref="BlexSelect{TValue}"/>: content that follows one value of a store, re-rendering only
/// itself and only when that value changes.
/// </summary>
[TestClass]
public class BlexSelectTests : BunitContext
{
    /// <summary>A host that counts its own renders and selects a field chosen by a parameter.</summary>
    private sealed class Host : ComponentBase
    {
        [Parameter] public ProfileStore Store { get; set; } = default!;

        [Parameter] public string Field { get; set; } = "age";

        public int Renders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            builder.OpenComponent<BlexSelect<string?>>(0);
            builder.AddComponentParameter(1, nameof(BlexSelect<string?>.Store), Store);
            builder.AddComponentParameter(2, nameof(BlexSelect<string?>.Selector),
                (Func<string?>)(() => Field == "age" ? Store.Age.ToString() : Store.UserName));
            builder.AddComponentParameter(3, nameof(BlexSelect<string?>.ChildContent),
                (RenderFragment<string?>)(value => content => content.AddContent(0, $"[{value}]")));
            builder.CloseComponent();
        }
    }

    [TestMethod]
    public void It_renders_the_selected_value()
    {
        var store = new CounterStore();
        store.Add(4);

        var cut = Render<BlexSelect<int>>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.Selector, () => store.Count)
            .Add(x => x.ChildContent, value => $"<b>{value}</b>"));

        cut.MarkupMatches("<b>4</b>");
        Assert.AreEqual(4, cut.Instance.Value);
    }

    [TestMethod]
    public void Only_a_change_to_the_selected_value_re_renders_the_content()
    {
        var store = new CounterStore();
        var renders = 0;

        var cut = Render<BlexSelect<int>>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.Selector, () => store.Count)
            .Add(x => x.ChildContent, (RenderFragment<int>)(value => content =>
            {
                renders++;
                content.AddContent(0, value);
            })));

        cut.InvokeAsync(() => store.Label = "unrelated");
        Assert.AreEqual(1, renders);

        cut.InvokeAsync(store.Increment);
        cut.MarkupMatches("1");
        Assert.AreEqual(2, renders);
    }

    [TestMethod]
    public void The_host_does_not_re_render()
    {
        var store = new ProfileStore();
        var cut = Render<Host>(p => p.Add(x => x.Store, store));

        cut.InvokeAsync(() => store.Age = 30);

        cut.MarkupMatches("[30]");
        Assert.AreEqual(1, cut.Instance.Renders);
    }

    [TestMethod]
    public void A_new_selector_from_the_host_is_compared_against_what_is_on_screen()
    {
        var store = new ProfileStore();
        var cut = Render<Host>(p => p.Add(x => x.Store, store).Add(x => x.Field, "age"));
        cut.MarkupMatches("[0]");

        cut.Render(p => p.Add(x => x.Field, "name"));
        cut.MarkupMatches("[anonymous]");

        // Equal to what the *previous* selector last saw. Comparing against that would miss it.
        cut.InvokeAsync(() => store.SignIn("0"));
        cut.MarkupMatches("[0]");
    }

    [TestMethod]
    public void A_comparer_decides_what_counts_as_a_change()
    {
        var store = new TodoStore();
        var renders = 0;

        Render<BlexSelect<IEnumerable<string>?>>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.Selector, () => store.Items.ToList())
            .Add(x => x.Comparer, BlexComparers.Sequence<string>())
            .Add(x => x.ChildContent, (RenderFragment<IEnumerable<string>?>)(_ => _ => renders++)));

        store.Items = [.. store.Items];
        Assert.AreEqual(1, renders);
    }

    [TestMethod]
    public async Task Disposal_detaches_from_the_store()
    {
        var store = new CounterStore();
        var renders = 0;

        Render<BlexSelect<int>>(p => p
            .Add(x => x.Store, store)
            .Add(x => x.Selector, () => store.Count)
            .Add(x => x.ChildContent, (RenderFragment<int>)(_ => _ => renders++)));

        await DisposeComponentsAsync();
        store.Increment();

        Assert.AreEqual(1, renders);
    }
}
