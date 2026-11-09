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

/// <summary>The input of a <see cref="BitBhSearchField"/> (<c>type="search"</c>, which is <c>role="searchbox"</c>).</summary>
public class BitBhSearchFieldInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSearchField? SearchField { get; set; }

    protected override string DefaultTag => "input";

    protected override void OnParametersSet() => SearchField?.SetInputId(Id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = SearchField;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.CurrentValue);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.SetValueAsync(e.Value?.ToString() ?? string.Empty)));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", owner.HandleKeyDownAsync));
            // While there is text, Escape clears it and goes no further (a dialog around stays open).
            builder.AddEventStopPropagationAttribute(33, "onkeydown", owner.CurrentValue.Length > 0);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (SearchField is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "search";
        else attrs["role"] = "searchbox";
        attrs["autocomplete"] = "off";
        if (owner.IsDisabled) attrs["disabled"] = true;
        if (owner.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        // The browser's own Escape-clears for type=search is replaced by ours (it would not tell .NET).
        attrs[BitBhKeyboard.KeysAttribute] = owner.CurrentValue.Length > 0 ? "Escape Enter" : "Enter";
        owner.AddState(attrs);
        return attrs;
    }
}

/// <summary>The clear button of a <see cref="BitBhSearchField"/>: named "Clear search", out of the tab order, disabled while empty.</summary>
public class BitBhSearchFieldClear : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSearchField? SearchField { get; set; }

    /// <summary>The content (an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => SearchField?.ClearAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = SearchField is not { } owner || owner.IsDisabled || owner.CurrentValue.Length == 0;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = Strings.ClearSearch;
        if (SearchField is { } field)
        {
            attrs["aria-controls"] = field.InputId;
            field.AddState(attrs);
        }
        attrs["data-bit-bh-interactive"] = "";
        return attrs;
    }
}
