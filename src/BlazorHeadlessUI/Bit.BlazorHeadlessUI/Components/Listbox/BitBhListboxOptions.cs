using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The popup containing a <see cref="BitBhListbox{TValue}"/>'s options
/// (<c>role="listbox"</c>). Hidden while closed.
///
/// <para><b>Two ways to provide options:</b></para>
/// <list type="bullet">
///   <item><b>Components</b> — place <see cref="BitBhListboxOption{TValue}"/> children inside.</item>
///   <item><b>Data</b> — set <see cref="Items"/> and <see cref="ItemTemplate"/>; add
///     <see cref="Virtualize"/> for thousands of options (the panel must then be a
///     scroll container with a fixed height, and options a fixed <see cref="ItemSize"/>).
///     Options get <c>aria-setsize</c> / <c>aria-posinset</c>.</item>
/// </list>
///
/// <para>Set <see cref="BitBhFloatingContentBase.Anchor"/> to position it under the button
/// (<c>--button-width</c> matches the trigger width).</para>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhListboxOptions<TValue> : BitBhFloatingContentBase
{
    private int _contentVersion;

    [CascadingParameter]
    private BitBhListboxContext<TValue>? ListboxContext { get; set; }

    /// <summary>Option components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Data-driven options (alternative to child components).</summary>
    [Parameter]
    public IReadOnlyList<TValue>? Items { get; set; }

    /// <summary>Renders the content of each data-driven option.</summary>
    [Parameter]
    public RenderFragment<BitBhListboxOptionRenderContext<TValue>>? ItemTemplate { get; set; }

    /// <summary>Returns whether a data-driven option is disabled.</summary>
    [Parameter]
    public Func<TValue, bool>? ItemDisabled { get; set; }

    /// <summary>Returns the text of a data-driven option (typeahead, button text). Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TValue, string?>? ItemText { get; set; }

    /// <summary>Virtualize data-driven options (only the visible ones are rendered).</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>Height (px) of each option when virtualized. Default: 36.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 36;

    /// <summary>Extra options rendered above / below the viewport when virtualized. Default: 5.</summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    protected override string DefaultTag => "ul";

    protected override bool IsOpen => ListboxContext?.IsOpen ?? false;

    private string PanelId => ListboxContext?.OptionsId ?? ComponentId;

    protected override void OnParametersSet()
    {
        _contentVersion++;
        ListboxContext?.State.SetItems(Items, ItemDisabled, ItemText, ItemSize);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The highlighted option scrolls into view once the panel shows: right after this render, or for an
        // anchored / portalled panel once the runtime has opened (positioned and revealed) it.
        ListboxContext?.State.SetPanelRevealed(!IsHidden);
        await base.OnAfterRenderAsync(firstRender);
        ListboxContext?.State.SetPanelRevealed(!IsHidden);
        if (IsOpen) ListboxContext?.State.Reconcile(fallbackToFirst: false);
    }

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(ListboxContext?.ButtonId);
        return new BitBhFloatingRequest
        {
            ContentId = PanelId,
            ReferenceId = ListboxContext?.ButtonId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => ListboxContext?.CloseAsync() ?? Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ListboxContext?.State.SetOptionsMounted(ShouldRenderContent);
        if (!ShouldRenderContent) return;

        Scope.Owner = PanelId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", PanelId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(30, "hidden", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        BitBhFloatingScope.Render(builder, 50, Scope, (RenderFragment)(inner =>
            BitBhRenderBoundary.Render(inner, 0, _contentVersion, RenderOptions)));
        builder.CloseElement();
    }

    private void RenderOptions(RenderTreeBuilder builder)
    {
        if (Items is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        if (Virtualize)
        {
            builder.OpenComponent<Virtualize<BitBhIndexed<TValue>>>(1);
            builder.AddComponentParameter(2, nameof(Virtualize<BitBhIndexed<TValue>>.Items), new BitBhIndexedList<TValue>(Items));
            builder.AddComponentParameter(3, nameof(Virtualize<BitBhIndexed<TValue>>.ItemSize), ItemSize);
            builder.AddComponentParameter(4, nameof(Virtualize<BitBhIndexed<TValue>>.OverscanCount), OverscanCount);
            builder.AddComponentParameter(5, nameof(Virtualize<BitBhIndexed<TValue>>.SpacerElement), Tag.Equals("ul", StringComparison.OrdinalIgnoreCase) ? "li" : "div");
            builder.AddComponentParameter(6, nameof(Virtualize<BitBhIndexed<TValue>>.ItemContent), (RenderFragment<BitBhIndexed<TValue>>)(entry => b => RenderDataOption(b, entry.Index, entry.Value)));
            builder.CloseComponent();
            return;
        }

        for (var i = 0; i < Items.Count; i++)
            RenderDataOption(builder, i, Items[i]);
    }

    private void RenderDataOption(RenderTreeBuilder builder, int index, TValue value)
    {
        builder.OpenComponent<BitBhListboxOption<TValue>>(10);
        builder.SetKey(index);
        builder.AddComponentParameter(11, nameof(BitBhListboxOption<TValue>.Value), value);
        builder.AddComponentParameter(12, nameof(BitBhListboxOption<TValue>.DataIndex), index);
        if (ItemDisabled is not null)
            builder.AddComponentParameter(13, nameof(BitBhListboxOption<TValue>.Disabled), ItemDisabled(value));
        if (ItemTemplate is not null)
            builder.AddComponentParameter(14, nameof(BitBhListboxOption<TValue>.Template), ItemTemplate);
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "listbox";
        if (ListboxContext is not null)
            attrs["aria-labelledby"] = ListboxContext.Field?.LabelledBy ?? ListboxContext.ButtonId;
        if (ListboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";
        SetDataState(attrs, IsOpen);
        return attrs;
    }
}
