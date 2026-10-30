namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhListbox{TValue}"/> to its button,
/// options panel and options. Carries open state, the selection and ARIA ids.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class BitBhListboxContext<TValue> : IBitBhCloseContext
{
    internal BitBhListboxContext(
        BitBhListbox<TValue> listbox,
        BitBhListState<TValue> state,
        bool isOpen,
        bool disabled,
        bool multiple,
        bool invalid,
        bool required,
        TValue? singleValue,
        bool hasValue,
        IReadOnlyCollection<TValue> multiValues,
        string baseId,
        string buttonId,
        string optionsId,
        BitBhFieldContext? field)
    {
        Listbox = listbox;
        State = state;
        IsOpen = isOpen;
        Disabled = disabled;
        Multiple = multiple;
        Invalid = invalid;
        Required = required;
        SingleValue = singleValue;
        HasValue = hasValue;
        MultiValues = multiValues;
        BaseId = baseId;
        ButtonId = buttonId;
        OptionsId = optionsId;
        Field = field;
    }

    internal BitBhListbox<TValue> Listbox { get; }

    internal BitBhFieldContext? Field { get; }

    /// <summary>The shared option-list state (highlight, options).</summary>
    public BitBhListState<TValue> State { get; }

    /// <summary>Whether the options panel is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the listbox is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the listbox allows multiple selection.</summary>
    public bool Multiple { get; }

    /// <summary>Whether the listbox is invalid.</summary>
    public bool Invalid { get; }

    /// <summary>Whether the listbox is required.</summary>
    public bool Required { get; }

    /// <summary>The selected value (single-select).</summary>
    public TValue? SingleValue { get; }

    /// <summary>Whether a value is selected (single-select).</summary>
    public bool HasValue { get; }

    /// <summary>The selected values (multi-select).</summary>
    public IReadOnlyCollection<TValue> MultiValues { get; }

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The id of the button (<c>role="combobox"</c>).</summary>
    public string ButtonId { get; }

    /// <summary>The id of the options panel (<c>role="listbox"</c>).</summary>
    public string OptionsId { get; }

    /// <summary>The id of the highlighted option, or null.</summary>
    public string? ActiveId => State.ActiveId;

    /// <summary>Returns whether <paramref name="value"/> is selected.</summary>
    public bool IsSelected(TValue value) => State.IsSelected(value);

    /// <summary>Closes the options panel.</summary>
    public Task CloseAsync() => Listbox.CloseAsync();

    internal Task SelectAsync(TValue value) => Listbox.SelectAsync(value);
}

/// <summary>Render-fragment context exposed by <see cref="BitBhListboxButton{TValue}"/>.</summary>
public sealed record BitBhListboxButtonRenderContext<TValue>
{
    /// <summary>Whether the options panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the listbox is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The selected value (single-select).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The selected values (multi-select).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }

    /// <summary>The label(s) of the selected option(s), comma-separated, or null when nothing is selected.</summary>
    public string? Text { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhListboxOption{TValue}"/>.</summary>
public sealed record BitBhListboxOptionRenderContext<TValue>
{
    /// <summary>The option's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Whether this option is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this option is highlighted.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this option is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by the listbox root (legacy).</summary>
public sealed record BitBhListboxRenderContext<TValue>
{
    /// <summary>Whether the listbox panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>The selected value (single-select).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The selected values (multi-select).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }

    /// <summary>Closes the listbox panel.</summary>
    public required Action Close { get; init; }
}
