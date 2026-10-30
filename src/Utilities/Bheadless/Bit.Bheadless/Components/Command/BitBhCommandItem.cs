using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// An action in a <see cref="BitBhCommand"/> list (<c>role="option"</c>). Matched
/// against the search by its <see cref="Value"/> (defaults to its text) and
/// <see cref="Keywords"/>; not rendered while filtered out. Emits
/// <c>data-selected</c> (highlighted) and <c>data-disabled</c>.
/// </summary>
public class BitBhCommandItem : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    private bool _wasVisible = true;
    private bool _forceMount;

    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    [CascadingParameter]
    private BitBhCommandGroup? ParentGroup { get; set; }

    /// <summary>The searchable value. Defaults to the rendered text.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Extra search terms (aliases, synonyms).</summary>
    [Parameter]
    public IReadOnlyList<string>? Keywords { get; set; }

    /// <summary>Disables the item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Always render, regardless of the search.</summary>
    [Parameter]
    public bool ForceMount { get; set; }

    /// <summary>Invoked when the item is chosen (click or Enter).</summary>
    [Parameter]
    public EventCallback OnSelect { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>bool</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override string DefaultTag => "div";

    internal BitBhCommandGroup? Group => ParentGroup;

    /// <summary>The value used for filtering.</summary>
    public string ItemValue => Value ?? CommandContext?.Items.GetRenderedText(ItemId) ?? string.Empty;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled;

    /// <inheritdoc />
    public string? ItemText => ItemValue;

    private bool IsVisible => CommandContext?.IsVisible(this) ?? true;

    protected override void OnInitialized() => CommandContext?.Items.Register(this);

    protected override void OnParametersSet()
    {
        // A filtered-out item shows when force-mounted: the context's cached display order has it or not.
        if (ForceMount == _forceMount) return;
        _forceMount = ForceMount;
        CommandContext?.InvalidateOrder();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && CommandContext is not null)
            await CommandContext.Items.EnsureOrderAsync(Interop);
    }

    internal void Refresh()
    {
        // Skip renders for items that stay hidden.
        var visible = IsVisible;
        if (!visible && !_wasVisible) return;
        _wasVisible = visible;
        StateHasChanged();
    }

    internal async Task SelectAsync()
    {
        if (Disabled) return;
        var page = CommandContext?.Page;
        await OnSelect.InvokeAsync();
        if (CommandContext is not null)
            await CommandContext.Command.OnItemSelectedAsync(page);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsVisible) return;

        var active = CommandContext?.IsActive(this) ?? false;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", SelectAsync));
        builder.AddAttribute(31, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
        builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        builder.AddAttribute(33, "onpointermove",
            On<PointerEventArgs>("onpointermove", HandlePointerMove));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, active));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var active = CommandContext?.IsActive(this) ?? false;
        attrs["role"] = "option";
        SetAriaBool(attrs, "aria-selected", active);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (CommandContext is not null)
        {
            attrs["data-bit-bh-item"] = CommandContext.Items.Key;
            if (CommandContext.Sort && !string.IsNullOrWhiteSpace(CommandContext.Search))
                attrs["style"] = $"order:{CommandContext.Rank(this).ToString(CultureInfo.InvariantCulture)}";
        }
        if (Value is not null) attrs["data-value"] = Value;
        SetDataFlag(attrs, "selected", active);
        SetDataFlag(attrs, "active", active);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private void HandlePointerMove(PointerEventArgs e)
    {
        if (Disabled || CommandContext is null || CommandContext.IsActive(this)) return;
        CommandContext.SetActive(this, scroll: false);
    }

    public void Dispose()
    {
        if (CommandContext is null) return;
        if (CommandContext.IsActive(this)) CommandContext.SetActive(null, scroll: false);
        CommandContext.Items.Unregister(this);
    }
}
