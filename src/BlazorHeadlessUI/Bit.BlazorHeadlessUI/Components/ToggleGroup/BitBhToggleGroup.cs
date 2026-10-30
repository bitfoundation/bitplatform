using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>Whether a <see cref="BitBhToggleGroup"/> allows one or many pressed items.</summary>
public enum BitBhToggleGroupType
{
    /// <summary>At most one item is on (behaves like a radio group).</summary>
    Single,

    /// <summary>Any number of items can be on.</summary>
    Multiple,
}

/// <summary>
/// A set of two-state buttons — text alignment, view modes, filters. A single
/// tab stop with arrow-key navigation. A <see cref="BitBhToggleGroupType.Single"/> group
/// that always keeps one item on (<see cref="AllowEmpty"/> false) is a <c>radiogroup</c>
/// of <c>radio</c>s; otherwise it is a <c>group</c> of <c>aria-pressed</c> buttons (a radio
/// can't be unchecked). Inside a <see cref="BitBhToolbar"/> its items join the toolbar's
/// arrow-key navigation.
///
/// <code>
/// &lt;BitBhToggleGroup @bind-Value="align" aria-label="Alignment"&gt;
///     &lt;BitBhToggleGroupItem Value="left" aria-label="Left"&gt;⇤&lt;/BitBhToggleGroupItem&gt;
///     &lt;BitBhToggleGroupItem Value="center" aria-label="Center"&gt;↔&lt;/BitBhToggleGroupItem&gt;
/// &lt;/BitBhToggleGroup&gt;
/// </code>
/// </summary>
public class BitBhToggleGroup : BitBhComponentBase
{
    private BitBhRovingFocusContext? _ownRoving;
    private string? _value;
    private List<string> _values = new();

    [CascadingParameter]
    private BitBhRovingFocusContext? ParentRoving { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Single (default) or multiple selection.</summary>
    [Parameter]
    public BitBhToggleGroupType Type { get; set; } = BitBhToggleGroupType.Single;

    /// <summary>The pressed item (single). Supplying it makes the group controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Initial pressed item when uncontrolled (single).</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The pressed items (multiple). Supplying it makes the group controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>Initial pressed items when uncontrolled (multiple).</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    /// <summary>
    /// In single mode, whether clicking the pressed item turns it off (leaving none). Default: true.
    /// Set it to false for radio semantics (<c>radiogroup</c> / <c>radio</c>, selection follows focus).
    /// </summary>
    [Parameter]
    public bool AllowEmpty { get; set; } = true;

    /// <summary>Disables every item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Arrow-key orientation (ignored inside a toolbar). Default: horizontal.</summary>
    [Parameter]
    public BitBhRovingOrientation Orientation { get; set; } = BitBhRovingOrientation.Horizontal;

    /// <summary>Whether arrow keys wrap. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool InToolbar => ParentRoving is { IsToolbar: true };

    private bool IsDisabled => Disabled || (FieldsetContext?.Disabled ?? false);

    /// <summary>Whether the items are radios: one is always on, which a radio can't undo by itself.</summary>
    internal bool IsRadioGroup => Type == BitBhToggleGroupType.Single && !AllowEmpty;

    /// <summary>
    /// Radio groups select on arrow keys (APG). Inside a toolbar arrow keys only move focus,
    /// and Space / Enter select (APG toolbar); toggle buttons never select on focus.
    /// </summary>
    internal bool SelectsOnFocus => IsRadioGroup && !InToolbar;

    private BitBhRovingFocusContext Roving => InToolbar ? ParentRoving! : _ownRoving ??= new BitBhRovingFocusContext(Interop, BitBhId.New("bit-bh-toggles"));

    private IReadOnlyCollection<string> PressedValues => Type == BitBhToggleGroupType.Single
        ? (CurrentValue is null ? [] : [CurrentValue])
        : IsProvided(nameof(Values)) ? Values as IReadOnlyCollection<string> ?? Values?.ToArray() ?? [] : _values;

    private string? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        if (DefaultValues is not null) _values = DefaultValues.ToList();
    }

    protected override void OnParametersSet()
    {
        if (!InToolbar)
        {
            Roving.Orientation = Orientation;
            Roving.Loop = Loop;
            Roving.Dir = Dir ?? CascadedDir;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhToggleGroupContext(this, Type, PressedValues, IsDisabled);

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhRovingFocusContext>>(40);
        builder.AddComponentParameter(41, "Value", Roving);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenComponent<CascadingValue<BitBhToggleGroupContext>>(0);
            inner.AddComponentParameter(1, "Value", context);
            inner.AddComponentParameter(2, "ChildContent", ChildContent);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = IsRadioGroup ? "radiogroup" : "group";
        if (!InToolbar && Orientation != BitBhRovingOrientation.Both)
            SetDataValue(attrs, "orientation", Orientation == BitBhRovingOrientation.Vertical ? "vertical" : "horizontal");
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    internal async Task ToggleAsync(string value)
    {
        if (IsDisabled) return;
        if (Type == BitBhToggleGroupType.Single)
        {
            var next = CurrentValue == value ? (AllowEmpty ? null : value) : value;
            if (next == CurrentValue) return;
            if (!IsProvided(nameof(Value))) _value = next;
            await ValueChanged.InvokeAsync(next);
        }
        else
        {
            var list = PressedValues.ToList();
            if (!list.Remove(value)) list.Add(value);
            if (!IsProvided(nameof(Values))) _values = list;
            await ValuesChanged.InvokeAsync(list);
        }
        StateHasChanged();
    }
}

/// <summary>State of a <see cref="BitBhToggleGroup"/> cascaded to its items.</summary>
public sealed class BitBhToggleGroupContext
{
    internal BitBhToggleGroupContext(BitBhToggleGroup group, BitBhToggleGroupType type, IReadOnlyCollection<string> pressed, bool disabled)
    {
        Group = group;
        Type = type;
        Pressed = pressed;
        Disabled = disabled;
    }

    internal BitBhToggleGroup Group { get; }

    /// <summary>Single or multiple.</summary>
    public BitBhToggleGroupType Type { get; }

    /// <summary>The pressed values.</summary>
    public IReadOnlyCollection<string> Pressed { get; }

    /// <summary>Whether the group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether <paramref name="value"/> is pressed.</summary>
    public bool IsPressed(string value) => Pressed.Contains(value);
}
