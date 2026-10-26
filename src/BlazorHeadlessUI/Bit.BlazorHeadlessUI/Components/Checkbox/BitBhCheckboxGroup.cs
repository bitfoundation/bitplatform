using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A group of checkboxes bound to one collection of values (<c>role="group"</c>).
/// Each <see cref="BitBhCheckbox"/> inside with a <c>Value</c> toggles that value.
/// Supports <c>@bind-Values</c> with <c>EditForm</c> validation, and exposes the
/// aggregate state so a "select all" checkbox can show the mixed state.
///
/// <code>
/// &lt;BitBhCheckboxGroup @bind-Values="toppings" aria-label="Toppings"&gt;
///     &lt;BitBhCheckbox SelectAll="true"&gt;All&lt;/BitBhCheckbox&gt;
///     &lt;BitBhCheckbox Value="cheese"&gt;Cheese&lt;/BitBhCheckbox&gt;
///     &lt;BitBhCheckbox Value="olives"&gt;Olives&lt;/BitBhCheckbox&gt;
/// &lt;/BitBhCheckboxGroup&gt;
/// </code>
/// </summary>
public class BitBhCheckboxGroup : BitBhComponentBase, IDisposable
{
    // The checkboxes rendered now, with the value each offers to "select all" (null: none, e.g. the select-all box)
    // and whether it is disabled ("select all" leaves a disabled box as it is).
    private readonly List<(BitBhCheckbox Box, string? Value, bool Disabled)> _boxes = new();
    private List<string> _values = new();
    private BitBhFieldBinding? _binding;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The checked values. Supplying it makes the group controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>Initial values when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<IEnumerable<string>?>>? ValuesExpression { get; set; }

    /// <summary>Fires whenever the values change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> OnValuesChange { get; set; }

    /// <summary>All values the group offers. Optional — defaults to the values of the rendered checkboxes. Used by "select all".</summary>
    [Parameter]
    public IEnumerable<string>? AllValues { get; set; }

    /// <summary>Disables every checkbox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhCheckboxGroupContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhCheckboxGroupContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    private IReadOnlyCollection<string> CurrentValues => IsProvided(nameof(Values))
        ? Values as IReadOnlyCollection<string> ?? Values?.ToArray() ?? []
        : _values;

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);

    protected override void OnInitialized()
    {
        if (DefaultValues is not null) _values = DefaultValues.ToList();
        // A group is no labelable element: the field's label names it through aria-labelledby and, clicked,
        // focuses its first checkbox (instead of a <label for> pointing at an id nothing has).
        FieldContext?.SetControlFocus(FocusAsync);
    }

    /// <summary>Focuses the first enabled checkbox.</summary>
    public Task FocusAsync()
        => _boxes.FirstOrDefault(b => !b.Box.IsBoxDisabled).Box is { } box ? Interop.FocusAsync(box.BoxId).AsTask() : Task.CompletedTask;

    protected override void OnParametersSet()
    {
        if (ValuesExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValuesExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    // Validation also changes without this control changing (a submit, an async validator): the field's
    // error message and data-invalid follow it.
    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var all = AllValues?.ToArray() ?? _boxes.Where(b => b.Value is not null).Select(b => b.Value!).Distinct(StringComparer.Ordinal).ToArray();
        var current = CurrentValues;
        var locked = _boxes.Where(b => b.Value is not null && b.Disabled).Select(b => b.Value!).ToHashSet(StringComparer.Ordinal);
        var context = new BitBhCheckboxGroupContext(this, current, all, locked, IsDisabled);

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhCheckboxGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", context);
        builder.AddComponentParameter(42, "ChildContent", BitBhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (FieldContext?.LabelledBy is { } label) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    internal void Register(BitBhCheckbox box, string? value, bool disabled)
    {
        var index = _boxes.FindIndex(b => ReferenceEquals(b.Box, box));
        if (index >= 0 && _boxes[index].Value == value && _boxes[index].Disabled == disabled) return;
        if (index >= 0) _boxes[index] = (box, value, disabled);
        else _boxes.Add((box, value, disabled));
        if (value is not null) StateHasChanged();
    }

    internal void Unregister(BitBhCheckbox box)
    {
        var index = _boxes.FindIndex(b => ReferenceEquals(b.Box, box));
        if (index < 0) return;
        var value = _boxes[index].Value;
        _boxes.RemoveAt(index);
        // A removed checkbox's value is no longer offered by "select all".
        if (value is not null) StateHasChanged();
    }

    internal async Task SetValuesAsync(IReadOnlyCollection<string> values)
    {
        if (!IsProvided(nameof(Values))) _values = values.ToList();
        await ValuesChanged.InvokeAsync(values);
        await OnValuesChange.InvokeAsync(values);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    public void Dispose()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
    }
}

/// <summary>State of a <see cref="BitBhCheckboxGroup"/>, cascaded to its checkboxes and passed to its content.</summary>
public sealed class BitBhCheckboxGroupContext
{
    private readonly BitBhCheckboxGroup _group;
    private readonly HashSet<string> _locked;
    private readonly string[] _selectable;

    internal BitBhCheckboxGroupContext(BitBhCheckboxGroup group, IReadOnlyCollection<string> values, IReadOnlyCollection<string> all, HashSet<string> locked, bool disabled)
    {
        _group = group;
        _locked = locked;
        // "Select all" judges by the values it can change; with every box disabled, by all of them.
        _selectable = all.Where(v => !locked.Contains(v)).ToArray();
        if (_selectable.Length == 0) _selectable = all.ToArray();
        Values = values;
        All = all;
        Disabled = disabled;
    }

    /// <summary>The checked values.</summary>
    public IReadOnlyCollection<string> Values { get; }

    /// <summary>All values the group offers.</summary>
    public IReadOnlyCollection<string> All { get; }

    /// <summary>Whether the group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether every value of an enabled checkbox is checked (every value, when no checkbox is enabled).</summary>
    public bool AllChecked => _selectable.Length > 0 && _selectable.All(Values.Contains);

    /// <summary>Whether some, but not all, values are checked (for a "select all" checkbox's mixed state).</summary>
    public bool SomeChecked => Values.Count > 0 && !AllChecked;

    /// <summary>Whether <paramref name="value"/> is checked.</summary>
    public bool IsChecked(string value) => Values.Contains(value);

    /// <summary>Checks or unchecks <paramref name="value"/>.</summary>
    public Task SetAsync(string value, bool isChecked)
    {
        var next = Values.ToList();
        if (isChecked && !next.Contains(value)) next.Add(value);
        if (!isChecked) next.Remove(value);
        return _group.SetValuesAsync(next);
    }

    internal void Register(BitBhCheckbox box, string? value, bool disabled) => _group.Register(box, value, disabled);

    internal void Unregister(BitBhCheckbox box) => _group.Unregister(box);

    /// <summary>
    /// Checks or unchecks every value (bind to a "select all" checkbox's <c>OnCheckedChange</c>). The values of
    /// disabled checkboxes keep their state.
    /// </summary>
    public Task SetAll(bool isChecked) => _group.SetValuesAsync(isChecked
        ? All.Where(v => !_locked.Contains(v) || Values.Contains(v)).ToArray()
        : Values.Where(_locked.Contains).ToArray());
}
