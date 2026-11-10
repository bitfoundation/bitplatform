using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// One top-level item of a <see cref="BitBhNavigationMenuList"/> - an <c>&lt;li&gt;</c> holding either a
/// <see cref="BitBhNavigationMenuTrigger"/> with its <see cref="BitBhNavigationMenuContent"/>, or a plain
/// <see cref="BitBhNavigationMenuLink"/>.
/// </summary>
public class BitBhNavigationMenuItem : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    private bool _hasContent;

    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    /// <summary>
    /// The value that identifies the item in <see cref="BitBhNavigationMenu.Value"/>. Optional: a generated one is
    /// used when it is left out (give one to open the item from code or to bind it).
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>The item's trigger and content, or its link.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    /// <summary>The item's value, or the generated one.</summary>
    internal string EffectiveValue => string.IsNullOrEmpty(Value) ? ComponentId : Value;

    internal string TriggerId => $"{ComponentId}-trigger";

    internal string ContentId => $"{ComponentId}-content";

    private bool IsOpen => MenuContext?.Value is { } open && open == EffectiveValue;

    /// <inheritdoc />
    string IBitBhCollectionItem.ItemId => ComponentId;

    /// <inheritdoc />
    bool IBitBhCollectionItem.IsItemDisabled => false;

    /// <inheritdoc />
    string? IBitBhCollectionItem.ItemText => null;

    protected override void OnInitialized() => MenuContext?.Root.RegisterItem(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // data-motion compares the items' places: learn them in document order.
        if (firstRender && MenuContext is not null)
            await MenuContext.Root.EnsureItemOrderAsync();
    }

    internal void RegisterContent(bool present)
    {
        if (_hasContent == present) return;
        _hasContent = present;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhNavigationMenuItemContext(this)
        {
            Value = EffectiveValue,
            BaseId = ComponentId,
            IsOpen = IsOpen,
            HasContent = _hasContent,
        };

        builder.OpenComponent<CascadingValue<BitBhNavigationMenuItemContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (_hasContent) SetDataState(attrs, IsOpen);
        if (MenuContext is not null) attrs["data-bit-bh-item"] = MenuContext.Root.ItemsKey;
        return attrs;
    }

    public void Dispose() => MenuContext?.Root.UnregisterItem(this);
}
