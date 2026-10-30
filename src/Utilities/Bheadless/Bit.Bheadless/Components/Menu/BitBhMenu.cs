using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless, accessible dropdown Menu implementing the WAI-ARIA Menu Button pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Rich items</b> — <see cref="BitBhMenuItem"/>, <see cref="BitBhMenuCheckboxItem"/>, <see cref="BitBhMenuRadioGroup"/> / <see cref="BitBhMenuRadioItem"/>, groups, labels, separators.</item>
///   <item><b>Nested submenus</b> — <see cref="BitBhMenuSub"/>, <see cref="BitBhMenuSubTrigger"/>, <see cref="BitBhMenuSubContent"/> with hover intent and keyboard support.</item>
///   <item><b>Full keyboard support</b> — Enter / Space / ArrowDown / ArrowUp open; arrows, Home / End, typeahead navigate; Enter / Space activate; ArrowRight / ArrowLeft enter and leave submenus; Escape and Tab close.</item>
///   <item><b>Focus management</b> — focus moves into the menu (items are announced via <c>aria-activedescendant</c>) and returns to the button on close.</item>
///   <item><b>Dismissal</b> — pointer-down outside or Escape closes it; no invisible overlays, so the first outside click still reaches its target.</item>
///   <item><b>Menubar ready</b> — place menus inside a <see cref="BitBhMenubar"/> for an application menu bar.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c>; <c>data-active</c> / <c>data-highlighted</c>, <c>data-disabled</c>, <c>data-state="checked|unchecked"</c> on items.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhMenu&gt;
///     &lt;BitBhMenuButton&gt;Options ▾&lt;/BitBhMenuButton&gt;
///     &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;
///         &lt;BitBhMenuItem OnClick="Edit"&gt;Edit&lt;/BitBhMenuItem&gt;
///         &lt;BitBhMenuItem OnClick="Delete" Disabled="true"&gt;Delete&lt;/BitBhMenuItem&gt;
///     &lt;/BitBhMenuItems&gt;
/// &lt;/BitBhMenu&gt;
/// </code>
/// </summary>
public class BitBhMenu : BitBhComponentBase, IBitBhMenuRoot, IDisposable
{
    private bool _open;
    private BitBhMenuFocus _focusIntent;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BitBhMenubarContext? MenubarContext { get; set; }

    /// <summary>Whether the menu is open. Supplying it makes the menu controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Disables the entire menu.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (flips submenu keys). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Child content. Should contain an <see cref="BitBhMenuButton"/> and an <see cref="BitBhMenuItems"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    bool IBitBhMenuRoot.Disabled => Disabled;
    BitBhDirection IBitBhMenuRoot.Dir => Dir ?? CascadedDir;
    string? IBitBhMenuRoot.TriggerId => ButtonId;
    BitBhMenubarContext? IBitBhMenuRoot.Menubar => MenubarContext;

    private string ButtonId => $"{ComponentId}-button";
    private string ItemsId => $"{ComponentId}-items";

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
        MenubarContext?.Register(this);
    }

    public void Dispose() => MenubarContext?.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhMenuContext(this, IsOpen, Disabled, ComponentId, ButtonId, ItemsId, MenubarContext);

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhMenuContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal BitBhMenuFocus ConsumeFocusIntent()
    {
        var intent = _focusIntent;
        _focusIntent = BitBhMenuFocus.Container;
        return intent;
    }

    internal BitBhMenuFocus PeekFocusIntent() => _focusIntent;

    /// <summary>Opens the menu, highlighting the item selected by <paramref name="focus"/>.</summary>
    public async Task OpenAsync(BitBhMenuFocus focus = BitBhMenuFocus.Container)
    {
        if (Disabled) return;
        _focusIntent = focus;
        if (IsOpen) return;
        await SetOpenAsync(true);
        if (MenubarContext is not null)
            await MenubarContext.OnMenuOpenedAsync(this);
    }

    /// <summary>Closes the menu (and every open submenu).</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    internal Task ToggleAsync(BitBhMenuFocus focus) => IsOpen ? CloseAsync() : OpenAsync(focus);

    private async Task SetOpenAsync(bool value)
    {
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    internal Task FocusButtonAsync() => Interop.FocusAsync(ButtonId).AsTask();

    internal bool IsDisabled => Disabled;

    internal void Refresh() => StateHasChanged();
}
