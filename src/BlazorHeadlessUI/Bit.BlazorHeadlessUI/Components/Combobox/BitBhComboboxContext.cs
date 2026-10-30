namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhCombobox{TValue}"/> to its input,
/// button, options panel and options.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class BitBhComboboxContext<TValue> : IBitBhCloseContext
{
    internal BitBhComboboxContext(
        BitBhCombobox<TValue> combobox,
        BitBhListState<TValue> state,
        bool isOpen,
        bool disabled,
        bool multiple,
        bool invalid,
        bool required,
        TValue? singleValue,
        bool hasValue,
        IReadOnlyCollection<TValue> multiValues,
        string query,
        string baseId,
        string inputId,
        string buttonId,
        string optionsId,
        BitBhFieldContext? field)
    {
        Combobox = combobox;
        State = state;
        IsOpen = isOpen;
        Disabled = disabled;
        Multiple = multiple;
        Invalid = invalid;
        Required = required;
        SingleValue = singleValue;
        HasValue = hasValue;
        MultiValues = multiValues;
        Query = query;
        BaseId = baseId;
        InputId = inputId;
        ButtonId = buttonId;
        OptionsId = optionsId;
        Field = field;
    }

    internal BitBhCombobox<TValue> Combobox { get; }

    internal BitBhFieldContext? Field { get; }

    /// <summary>The shared option-list state (highlight, options).</summary>
    public BitBhListState<TValue> State { get; }

    /// <summary>Whether the options panel is open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the combobox is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether multiple options can be selected.</summary>
    public bool Multiple { get; }

    /// <summary>Whether the combobox is invalid.</summary>
    public bool Invalid { get; }

    /// <summary>Whether the combobox is required.</summary>
    public bool Required { get; }

    /// <summary>The selected value (single-select).</summary>
    public TValue? SingleValue { get; }

    /// <summary>Whether a value is selected (single-select).</summary>
    public bool HasValue { get; }

    /// <summary>The selected values (multi-select).</summary>
    public IReadOnlyCollection<TValue> MultiValues { get; }

    /// <summary>The current query.</summary>
    public string Query { get; }

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The id of the input (<c>role="combobox"</c>).</summary>
    public string InputId { get; }

    /// <summary>The id of the optional toggle button.</summary>
    public string ButtonId { get; }

    /// <summary>The id of the options panel (<c>role="listbox"</c>).</summary>
    public string OptionsId { get; }

    /// <summary>The id of the highlighted option, or null.</summary>
    public string? ActiveId => State.ActiveId;

    /// <summary>Returns the display text for a value.</summary>
    public string DisplayValue(TValue? value) => Combobox.Display(value);

    /// <summary>Returns whether <paramref name="value"/> is selected.</summary>
    public bool IsSelected(TValue value) => State.IsSelected(value);

    /// <summary>Closes the options panel.</summary>
    public Task CloseAsync() => Combobox.CloseAsync();

    internal Task SelectAsync(TValue value) => Combobox.SelectAsync(value);
}

/// <summary>Render-fragment context exposed by <see cref="BitBhComboboxButton{TValue}"/>.</summary>
public sealed record BitBhComboboxButtonRenderContext<TValue>
{
    /// <summary>Whether the options panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the combobox is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhComboboxOption{TValue}"/>.</summary>
public sealed record BitBhComboboxOptionRenderContext<TValue>
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
