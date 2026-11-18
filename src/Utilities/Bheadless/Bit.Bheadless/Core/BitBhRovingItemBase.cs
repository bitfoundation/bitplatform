using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Base class for items of a roving-tabindex group (toolbar buttons, toggle-group
/// items, generic <see cref="BitBhRovingFocusItem"/>s). Handles registration,
/// <c>tabindex</c>, arrow-key navigation and tab-stop tracking.
/// </summary>
public abstract class BitBhRovingItemBase : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhRovingFocusContext? RovingContext { get; set; }

    /// <summary>The group this item belongs to (null when used standalone).</summary>
    protected BitBhRovingFocusContext? Roving => RovingContext;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public abstract bool IsItemDisabled { get; }

    /// <inheritdoc />
    public virtual string? ItemText => null;

    /// <summary>Whether this item should be the initial tab stop (e.g. the pressed item of a single toggle group).</summary>
    protected internal virtual bool PreferredTabStop => false;

    internal void Refresh() => StateHasChanged();

    /// <summary>Whether the last render gave this item <c>tabindex="0"</c>.</summary>
    internal bool RenderedAsTabStop { get; private set; }

    /// <summary>Called after arrow keys moved focus to this item (radio-like groups select it).</summary>
    protected internal virtual Task OnRovedToAsync() => Task.CompletedTask;

    protected override void OnInitialized() => RovingContext?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RovingContext is not null)
            await RovingContext.Items.EnsureOrderAsync(Interop);
    }

    /// <summary>Adds the roving behaviour (keydown + focus handlers) to the element being built.</summary>
    protected void AddRovingHandlers(RenderTreeBuilder builder, int sequence)
    {
        if (RovingContext is null) return;
        builder.AddAttribute(sequence, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleRovingKeyDown));
        builder.AddAttribute(sequence + 1, "onfocus",
            On<FocusEventArgs>("onfocus", () => RovingContext.SetCurrent(this)));
    }

    /// <summary>Additional key handling (Enter / Space for non-button elements …).</summary>
    protected virtual Task OnKeyDownAsync(KeyboardEventArgs e) => Task.CompletedTask;

    private async Task HandleRovingKeyDown(KeyboardEventArgs e)
    {
        if (RovingContext is not null && await RovingContext.HandleKeyDownAsync(this, e)) return;
        await OnKeyDownAsync(e);
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (RovingContext is not null)
        {
            RenderedAsTabStop = ReferenceEquals(RovingContext.TabStop, this);
            attrs["tabindex"] = RenderedAsTabStop ? "0" : "-1";
            attrs["data-bit-bh-item"] = RovingContext.Items.Key;
            attrs[BitBhKeyboard.KeysAttribute] = RovingContext.KeysAttribute;
        }
        return attrs;
    }

    public virtual void Dispose() => RovingContext?.Items.Unregister(this);
}
