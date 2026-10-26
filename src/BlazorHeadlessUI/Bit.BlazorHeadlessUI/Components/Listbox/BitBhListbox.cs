using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Listbox / custom Select implementing the WAI-ARIA
/// select-only <c>combobox</c> + <c>listbox</c> pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or multi-select</b> via <see cref="Multiple"/>; <c>@bind-Value</c> / <c>@bind-Values</c>.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> labels / descriptions, hidden inputs for plain form posts (<see cref="Name"/>).</item>
///   <item><b>Full keyboard support</b> — arrows, Home / End, PageUp / PageDown, Enter / Space, Escape, Tab and typeahead (also while closed).</item>
///   <item><b>Virtual focus</b> — focus stays on the button; the highlighted option is announced via <c>aria-activedescendant</c>.</item>
///   <item><b>Large lists</b> — pass <c>Items</c> to <see cref="BitBhListboxOptions{TValue}"/> for data-driven rendering, optionally virtualized.</item>
///   <item><b>Data attributes</b> — <c>data-state</c> on root, button and panel; <c>data-active</c>, <c>data-selected</c>, <c>data-disabled</c> on options.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhListbox TValue="string" @bind-Value="person"&gt;
///     &lt;BitBhListboxButton TValue="string"&gt;&lt;Template Context="b"&gt;@(b.Text ?? "Pick a person")&lt;/Template&gt;&lt;/BitBhListboxButton&gt;
///     &lt;BitBhListboxOptions TValue="string" Anchor="@("bottom start")"&gt;
///         &lt;BitBhListboxOption TValue="string" Value="@("alice")"&gt;Alice&lt;/BitBhListboxOption&gt;
///         &lt;BitBhListboxOption TValue="string" Value="@("bob")"&gt;Bob&lt;/BitBhListboxOption&gt;
///     &lt;/BitBhListboxOptions&gt;
/// &lt;/BitBhListbox&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The option value type. Use a nullable type to represent "no selection" for value types.</typeparam>
public class BitBhListbox<TValue> : BitBhComponentBase, IDisposable
{
    private BitBhListState<TValue>? _state;
    private BitBhFieldBinding? _binding;
    private bool _open;
    private TValue? _value;
    private List<TValue> _values = new();
    private BitBhMenuFocus? _pendingHighlight;
    private bool _pendingHighlightIgnoresSelection;
    private string? _pendingTypeahead;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The selected value (single-select). Supplying it makes the listbox controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Initial value when uncontrolled (single-select).</summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>Fires when the selected value changes (single-select).</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChange { get; set; }

    /// <summary>The selected values (multi-select). Supplying it makes the listbox controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<TValue>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Values</c>).</summary>
    [Parameter]
    public Expression<Func<IEnumerable<TValue>?>>? ValuesExpression { get; set; }

    /// <summary>Initial values when uncontrolled (multi-select).</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultValues { get; set; }

    /// <summary>Fires when the selected values change (multi-select).</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> OnValuesChange { get; set; }

    /// <summary>Fires when the options panel opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Whether multiple options can be selected at once.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Disables the listbox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the listbox invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the listbox required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Compares option values with the selection. Defaults to <see cref="EqualityComparer{T}.Default"/>.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>
    /// Optional form field name. When set the listbox renders hidden <c>&lt;input&gt;</c>(s)
    /// carrying the selected value(s) so plain HTML form posts include the selection.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Converts a value to its form-post representation. Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TValue, string?>? FormatValue { get; set; }

    /// <summary>
    /// The text that names a selected value while no option of it has rendered: an options panel with
    /// <c>Unmount</c> that has not been opened yet has no option to read the text from. Defaults to the value's
    /// <c>ToString()</c> - never the placeholder, which would say nothing is selected.
    /// </summary>
    [Parameter]
    public Func<TValue, string?>? ValueText { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="BitBhListboxButton{TValue}"/> and an
    /// <see cref="BitBhListboxOptions{TValue}"/> with <see cref="BitBhListboxOption{TValue}"/> children.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;

    private BitBhListState<TValue> State
    {
        get
        {
            if (_state is null)
            {
                _state = new BitBhListState<TValue>(OptionsId, Interop, IsSelected, () => Button?.Refresh(), () => EffectiveComparer);
                _state.Options.Changed += () => Button?.Refresh();
            }
            return _state;
        }
    }

    internal BitBhListboxButton<TValue>? Button { get; set; }

    /// <summary>Whether the options panel is open.</summary>
    public bool IsOpen => _open;

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);

    private TValue? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    private IReadOnlyCollection<TValue> CurrentValues => IsProvided(nameof(Values))
        ? Values as IReadOnlyCollection<TValue> ?? Values?.ToArray() ?? []
        : _values;

    private bool HasSingleValue => IsProvided(nameof(Value)) ? Value is not null : _hasValue;
    private bool _hasValue;

    private string ButtonId => Id is null && FieldContext is not null ? FieldContext.ControlId : $"{ComponentId}-button";
    private string OptionsId => $"{ComponentId}-options";

    private bool IsSelected(TValue value)
        => Multiple
            ? CurrentValues.Contains(value, EffectiveComparer)
            : HasSingleValue && EffectiveComparer.Equals(CurrentValue!, value);

    protected override void OnInitialized()
    {
        // Not a null check: for a value type (int, an enum) that would always select default(TValue).
        if (IsProvided(nameof(DefaultValue)))
        {
            _value = DefaultValue;
            _hasValue = true;
        }
        if (DefaultValues is not null)
            _values = DefaultValues.ToList();
    }

    protected override void OnParametersSet()
    {
        LambdaExpression? expression = Multiple ? ValuesExpression : ValueExpression;
        if (expression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, expression);
            _binding.ReportTo(FieldContext);
        }
    }

    // Validation also changes without this control changing (a submit, an async validator): the field's
    // error message and data-invalid follow it as well as the button's aria-invalid.
    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_open) return;

        if (_pendingHighlight is { } highlight)
        {
            _pendingHighlight = null;
            await State.EnsureOrderAsync();
            var selected = _pendingHighlightIgnoresSelection ? -1 : State.FirstSelected();
            var index = highlight switch
            {
                BitBhMenuFocus.First => selected >= 0 ? selected : State.FirstEnabled(),
                BitBhMenuFocus.Last => selected >= 0 ? selected : State.LastEnabled(),
                _ => selected,
            };
            State.SetActiveIndex(index);
        }

        if (_pendingTypeahead is { } key)
        {
            _pendingTypeahead = null;
            await State.EnsureOrderAsync();
            State.Search(key);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        State.PanelId = OptionsId;
        var context = new BitBhListboxContext<TValue>(
            this, State, _open, IsDisabled, Multiple, IsInvalid, IsRequired,
            CurrentValue, HasSingleValue, CurrentValues, ComponentId, ButtonId, OptionsId, FieldContext);

        builder.OpenComponent<CascadingValue<BitBhListState<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", State);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(outer =>
        {
            outer.OpenComponent<CascadingValue<BitBhListboxContext<TValue>>>(0);
            outer.AddComponentParameter(1, "Value", context);
            outer.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                RenderHiddenInputs(inner, 50);
                inner.CloseElement();
            }));
            outer.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void RenderHiddenInputs(RenderTreeBuilder builder, int sequence)
    {
        // Like a native disabled control, a disabled listbox posts nothing.
        if (string.IsNullOrEmpty(Name) || IsDisabled) return;
        string? Format(TValue v) => BitBhListState<TValue>.FormatForPost(v, FormatValue);

        builder.OpenRegion(sequence);
        if (Multiple)
        {
            foreach (var v in CurrentValues)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "hidden");
                builder.AddAttribute(2, "name", Name);
                builder.AddAttribute(3, "value", Format(v));
                builder.CloseElement();
            }
        }
        else if (HasSingleValue)
        {
            builder.OpenElement(10, "input");
            builder.AddAttribute(11, "type", "hidden");
            builder.AddAttribute(12, "name", Name);
            builder.AddAttribute(13, "value", Format(CurrentValue!));
            builder.CloseElement();
        }
        builder.CloseRegion();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _open);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        return attrs;
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    internal async Task OpenAsync(BitBhMenuFocus highlight, bool ignoreSelection = false)
    {
        if (IsDisabled) return;
        _pendingHighlight = highlight;
        _pendingHighlightIgnoresSelection = ignoreSelection;
        if (_open)
        {
            StateHasChanged();
            return;
        }
        _open = true;
        StateHasChanged();
        await OnOpenChange.InvokeAsync(true);
    }

    /// <summary>Closes the options panel.</summary>
    public async Task CloseAsync()
    {
        if (!_open) return;
        _open = false;
        State.SetActiveId(null, scroll: false);
        State.Typeahead.Reset();
        StateHasChanged();
        await OnOpenChange.InvokeAsync(false);
    }

    internal Task ToggleAsync() => _open ? CloseAsync() : OpenAsync(BitBhMenuFocus.None);

    /// <summary>The text naming <paramref name="value"/> when no rendered option names it (see <see cref="ValueText"/>).</summary>
    internal string? TextOfValue(TValue value) => ValueText is not null ? ValueText(value) : value?.ToString();

    // ── Selection ────────────────────────────────────────────────────────────

    internal async Task SelectAsync(TValue value)
    {
        if (IsDisabled) return;

        if (Multiple)
        {
            var list = CurrentValues.ToList();
            var index = list.FindIndex(v => EffectiveComparer.Equals(v, value));
            if (index >= 0) list.RemoveAt(index);
            else list.Add(value);

            if (!IsProvided(nameof(Values)))
                _values = list;
            await ValuesChanged.InvokeAsync(list);
            await OnValuesChange.InvokeAsync(list);
        }
        else
        {
            if (!IsProvided(nameof(Value)))
            {
                _value = value;
                _hasValue = true;
            }
            await ValueChanged.InvokeAsync(value);
            await OnValueChange.InvokeAsync(value);
            await CloseAsync();
        }

        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    // ── Keyboard (focus stays on the button) ─────────────────────────────────

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsDisabled) return;

        if (!_open)
        {
            switch (e.Key)
            {
                case "ArrowDown":
                case "Enter":
                case " ":
                    await OpenAsync(BitBhMenuFocus.First);
                    break;
                case "ArrowUp":
                    await OpenAsync(BitBhMenuFocus.Last);
                    break;
                // Highlighted once the panel rendered: options removed while closed (Unmount) are back by then.
                case "Home":
                    await OpenAsync(BitBhMenuFocus.First, ignoreSelection: true);
                    break;
                case "End":
                    await OpenAsync(BitBhMenuFocus.Last, ignoreSelection: true);
                    break;
                default:
                    if (BitBhTypeahead.IsTypeaheadKey(e.Key, e.CtrlKey || e.MetaKey, e.AltKey))
                    {
                        _pendingTypeahead = e.Key;
                        await OpenAsync(BitBhMenuFocus.None);
                    }
                    break;
            }
            return;
        }

        if (e.Key == " " && State.Typeahead.IsActive)
        {
            State.Search(" ");
            return;
        }

        switch (e.Key)
        {
            case "ArrowDown":
                if (e.AltKey) break;
                State.SetActiveIndex(State.Step(State.ActiveIndex, +1, loop: false));
                break;
            case "ArrowUp":
                if (e.AltKey)
                {
                    if (State.TryGetActiveValue(out var v) && !Multiple) await SelectAsync(v);
                    else await CloseAsync();
                    break;
                }
                State.SetActiveIndex(State.Step(State.ActiveIndex, -1, loop: false));
                break;
            case "Home":
                State.SetActiveIndex(State.FirstEnabled());
                break;
            case "End":
                State.SetActiveIndex(State.LastEnabled());
                break;
            case "PageDown":
                State.SetActiveIndex(State.Offset(State.ActiveIndex, 10));
                break;
            case "PageUp":
                State.SetActiveIndex(State.Offset(State.ActiveIndex, -10));
                break;
            case "Enter":
            case " ":
                // A held key repeats: it would toggle (multiple) or select, reopen and select again.
                if (e.Repeat) break;
                if (State.TryGetActiveValue(out var value) && !State.DisabledAt(State.ActiveIndex))
                    await SelectAsync(value);
                break;
            case "Tab":
                // Select-only combobox (APG): Tab accepts the highlighted option, then moves on.
                if (!Multiple && State.TryGetActiveValue(out var active) && !State.DisabledAt(State.ActiveIndex))
                    await SelectAsync(active);
                else
                    await CloseAsync();
                break;
            case "Escape":
                await CloseAsync();
                break;
            default:
                if (BitBhTypeahead.IsTypeaheadKey(e.Key, e.CtrlKey || e.MetaKey, e.AltKey))
                    State.Search(e.Key);
                break;
        }
    }

    public void Dispose() => _binding?.Dispose();
}
