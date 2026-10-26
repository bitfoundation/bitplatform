using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Groups <see cref="BitBhMenuRadioItem"/>s so that exactly one is checked
/// (<c>role="group"</c>). Supports <c>@bind-Value</c>. Inside a <see cref="BitBhMenuGroup"/>
/// it adds no group of its own (a menu's group holds items only): the enclosing group, and its
/// <see cref="BitBhMenuLabel"/>, group and name the radio items.
///
/// <code>
/// &lt;BitBhMenuRadioGroup @bind-Value="sort"&gt;
///     &lt;BitBhMenuRadioItem Value="name"&gt;Name&lt;/BitBhMenuRadioItem&gt;
///     &lt;BitBhMenuRadioItem Value="date"&gt;Date&lt;/BitBhMenuRadioItem&gt;
/// &lt;/BitBhMenuRadioGroup&gt;
/// </code>
/// </summary>
public class BitBhMenuRadioGroup : BitBhComponentBase
{
    private string? _value;

    [CascadingParameter]
    private BitBhMenuGroupContext? MenuGroup { get; set; }

    /// <summary>The checked item's value. Supplying it makes the group controlled.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Initial value when uncontrolled.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Fires whenever the value changes.</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>Optional accessible label for the group.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>The radio items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The checked value.</summary>
    public string? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    protected override void OnInitialized() => _value = DefaultValue;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhMenuRadioGroupContext(CurrentValue, SelectAsync);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhMenuRadioGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", context);
        builder.AddComponentParameter(42, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (MenuGroup is not null)
        {
            attrs["role"] = "none";
            return attrs;
        }
        attrs["role"] = "group";
        if (AriaLabel is not null)
            attrs["aria-label"] = AriaLabel;
        return attrs;
    }

    private async Task SelectAsync(string value)
    {
        if (CurrentValue == value) return;
        if (!IsProvided(nameof(Value)))
            _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        StateHasChanged();
    }
}

/// <summary>Cascading context of a <see cref="BitBhMenuRadioGroup"/>.</summary>
public sealed class BitBhMenuRadioGroupContext
{
    private readonly Func<string, Task> _select;

    internal BitBhMenuRadioGroupContext(string? value, Func<string, Task> select)
    {
        Value = value;
        _select = select;
    }

    /// <summary>The checked value.</summary>
    public string? Value { get; }

    internal Task SelectAsync(string value) => _select(value);
}
