using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A headless search field (React Aria SearchField): a <c>type="search"</c> input (<c>role="searchbox"</c>) with a clear
/// button. Escape clears the text (and stays in the field: a dialog around it doesn't close) - pressed again on the
/// empty field it does what Escape does there; Enter submits (<see cref="OnSubmit"/>); the clear button is out of the
/// tab order, named "Clear search", and puts the focus back in the input.
///
/// <code>
/// &lt;BitBhSearchField @bind-Value="query" OnSubmit="SearchAsync" class="search"&gt;
///     &lt;BitBhSearchFieldInput aria-label="Search docs" placeholder="Search…" /&gt;
///     &lt;BitBhSearchFieldClear&gt;×&lt;/BitBhSearchFieldClear&gt;
/// &lt;/BitBhSearchField&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-empty</c>, <c>data-disabled</c> on every part (hide the clear button while empty).</para>
/// </summary>
public class BitBhSearchField : BitBhComponentBase
{
    private string _value = string.Empty;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The text. Supplying it makes the field controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/> (called as the text changes).</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Initial text when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Fires when Enter is pressed, with the text.</summary>
    [Parameter]
    public EventCallback<string> OnSubmit { get; set; }

    /// <summary>Fires when the text is cleared (Escape or the clear button).</summary>
    [Parameter]
    public EventCallback OnClear { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The input and the clear button.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current text.</summary>
    public string CurrentValue => IsProvided(nameof(Value)) ? Value ?? string.Empty : _value;

    internal bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private string? _inputId;

    internal string InputId => _inputId ?? FieldContext?.ControlId ?? $"{ComponentId}-input";

    internal void SetInputId(string? id)
    {
        if (_inputId == id) return;
        _inputId = id;
        StateHasChanged();
    }
    internal BitBhFieldContext? Field => FieldContext;

    protected override void OnInitialized() => _value = DefaultValue ?? string.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhSearchField>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", false);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddState(attrs);
        return attrs;
    }

    internal void AddState(Dictionary<string, object> attrs)
    {
        SetDataFlag(attrs, "empty", CurrentValue.Length == 0);
        SetDataFlag(attrs, "disabled", IsDisabled);
    }

    internal async Task SetValueAsync(string value)
    {
        if (value == CurrentValue) return;
        if (!IsProvided(nameof(Value))) _value = value;
        await ValueChanged.InvokeAsync(value);
        StateHasChanged();
    }

    /// <summary>Clears the text and focuses the input.</summary>
    public async Task ClearAsync()
    {
        if (IsDisabled) return;
        await SetValueAsync(string.Empty);
        await OnClear.InvokeAsync();
        await Interop.FocusAsync(InputId);
    }

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await OnSubmit.InvokeAsync(CurrentValue);
        else if (e.Key == "Escape" && CurrentValue.Length > 0)
        {
            await SetValueAsync(string.Empty);
            await OnClear.InvokeAsync();
        }
    }
}
