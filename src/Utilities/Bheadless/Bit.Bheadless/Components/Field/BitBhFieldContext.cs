namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhField"/>. It generates the control
/// id and tracks which labels, descriptions and error messages are actually
/// rendered, so controls only reference ids that exist
/// (<c>aria-labelledby</c>, <c>aria-describedby</c>).
///
/// <para>
/// Every Bheadless form control (inputs, <see cref="BitBhListbox{TValue}"/>,
/// <see cref="BitBhCombobox{TValue}"/>, <see cref="BitBhCheckbox"/>, <see cref="BitBhSwitch"/>,
/// <see cref="BitBhRadioGroup"/>, <see cref="BitBhSlider"/> …) picks it up automatically.
/// </para>
/// </summary>
public sealed class BitBhFieldContext
{
    private readonly List<string> _descriptions = new();
    private readonly List<string> _errors = new();
    private readonly Action _changed;
    private int _labels;
    private int _seq;
    private string[] _messages = [];
    private bool _controlInvalid;
    private Func<Task>? _focusControl;

    internal BitBhFieldContext(string baseId, Action changed)
    {
        BaseId = baseId;
        _changed = changed;
    }

    /// <summary>The field's base id.</summary>
    public string BaseId { get; }

    /// <summary>The id of the field's control (input, listbox button, checkbox …).</summary>
    public string ControlId => $"{BaseId}-control";

    /// <summary>Back-compat alias of <see cref="ControlId"/>.</summary>
    public string InputId => ControlId;

    /// <summary>The id of the field's label.</summary>
    public string LabelId => $"{BaseId}-label";

    /// <summary>The id of the first description (back-compat).</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>Whether the field (or an enclosing fieldset) is disabled.</summary>
    public bool Disabled { get; internal set; }

    /// <summary>Whether the field is marked invalid (explicitly, or by its control's validation).</summary>
    public bool Invalid => ExplicitInvalid || _controlInvalid;

    /// <summary>Whether the field is required.</summary>
    public bool Required { get; internal set; }

    internal bool ExplicitInvalid { get; set; }

    /// <summary>Whether a label is rendered.</summary>
    public bool HasLabel => _labels > 0;

    /// <summary>The validation messages reported by the field's control.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>
    /// Space-separated ids of the rendered descriptions and error messages, for
    /// <c>aria-describedby</c>; null when there are none.
    /// </summary>
    public string? DescribedBy
    {
        get
        {
            var ids = _descriptions.Concat(Invalid ? _errors : []).ToArray();
            return ids.Length == 0 ? null : string.Join(' ', ids);
        }
    }

    /// <summary>
    /// Whether <c>&lt;label for&gt;</c> can target the control. False when the control isn't a
    /// labelable element (radio group, slider …); the label then focuses it on click instead.
    /// </summary>
    internal bool LabelTargetsControl => _focusControl is null;

    /// <summary>
    /// Registered by controls that aren't labelable elements: how to focus them when the
    /// label is clicked. Pass null when the control goes away.
    /// </summary>
    internal void SetControlFocus(Func<Task>? focus)
    {
        if (ReferenceEquals(_focusControl, focus)) return;
        _focusControl = focus;
        _changed();
    }

    internal Task FocusControlAsync() => _focusControl?.Invoke() ?? Task.CompletedTask;

    /// <summary>Id of the label when one is rendered, for <c>aria-labelledby</c>.</summary>
    public string? LabelledBy => HasLabel ? LabelId : null;

    /// <summary>Allocates an id for a description or error message.</summary>
    internal string NextId(string kind) => $"{BaseId}-{kind}-{++_seq}";

    internal void RegisterLabel(bool present)
    {
        _labels = Math.Max(0, _labels + (present ? 1 : -1));
        _changed();
    }

    internal void RegisterDescription(string id, bool present) => Toggle(_descriptions, id, present);

    internal void RegisterError(string id, bool present) => Toggle(_errors, id, present);

    /// <summary>Called by controls bound to an <c>EditContext</c> to report validation state.</summary>
    internal void ReportValidation(bool invalid, IEnumerable<string> messages)
    {
        var array = messages.ToArray();
        if (invalid == _controlInvalid && array.SequenceEqual(_messages)) return;
        _controlInvalid = invalid;
        _messages = array;
        _changed();
    }

    private void Toggle(List<string> list, string id, bool present)
    {
        if (present ? list.Contains(id) : !list.Contains(id)) return;
        if (present) list.Add(id); else list.Remove(id);
        _changed();
    }
}
