using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bortable;

/// <summary>
/// One item's template, in a component of its own (see <see cref="Bortable{TItem}.IsolateItems"/>): it renders again
/// only when its item, the template or the list's <see cref="Version"/> changes - not when the list re-renders for a
/// step of a drag, which only moves the item wrappers around.
/// </summary>
internal sealed class BortableItemView<TItem> : ComponentBase
{
    private static readonly EqualityComparer<TItem> Comparer = EqualityComparer<TItem>.Default;

    private bool _rendered;
    private TItem _renderedItem = default!;
    private RenderFragment<TItem>? _renderedTemplate;
    private int _renderedVersion;

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    /// <summary>Bumped by the list whenever the page gives it its parameters again, which may have changed what the template shows.</summary>
    [Parameter] public int Version { get; set; }

    protected override bool ShouldRender()
        => !_rendered || !Comparer.Equals(_renderedItem, Item) || !ReferenceEquals(_renderedTemplate, Template) || _renderedVersion != Version;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _rendered = true;
        _renderedItem = Item;
        _renderedTemplate = Template;
        _renderedVersion = Version;
        if (Template is not null) builder.AddContent(0, Template(Item));
    }
}
