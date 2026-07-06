using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion;

/// <summary>Cascaded by <see cref="BmotionLayoutGroup"/> to namespace descendant LayoutIds.</summary>
internal sealed class BmotionLayoutGroupContext
{
    /// <summary>Prefix applied to descendant <c>LayoutId</c>s (<c>"{Name}:{LayoutId}"</c>).</summary>
    public string? Name { get; init; }
}

/// <summary>
/// Namespaces the <c>LayoutId</c>s of descendant Bmotion components so the same id can be
/// reused by independent groups (motion.dev's <c>LayoutGroup</c>):
/// <code>
/// &lt;BmotionLayoutGroup Name="tabs-left"&gt;
///     ... &lt;Bmotion LayoutId="underline"&gt;&lt;span class="underline" /&gt;&lt;/Bmotion&gt; ...
/// &lt;/BmotionLayoutGroup&gt;
/// </code>
/// </summary>
public sealed class BmotionLayoutGroup : ComponentBase
{
    /// <summary>The namespace prefix for descendant LayoutIds.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private BmotionLayoutGroupContext? _ctx;

    protected override void OnParametersSet()
    {
        if (_ctx?.Name != Name)
            _ctx = new BmotionLayoutGroupContext { Name = Name };
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BmotionLayoutGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", _ctx);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
