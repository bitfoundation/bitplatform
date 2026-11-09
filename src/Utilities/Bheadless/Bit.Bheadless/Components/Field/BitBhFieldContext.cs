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
    // The controls that aren't labelable elements, in the order they registered: the first one is the label's.
    private readonly List<(object Owner, Func<Task> Focus)> _focusControls = new();

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

    /// <summary>Whether the field's control has been left at least once.</summary>
    public bool Touched { get; private set; }

    /// <summary>Whether the field's value was changed by the user.</summary>
    public bool Dirty { get; private set; }

    /// <summary>Called by text controls as they are left and changed (the field's <c>data-touched</c> / <c>data-dirty</c>).</summary>
    internal void ReportInteraction(bool touched, bool dirty)
    {
        if (Touched == touched && Dirty == dirty) return;
        Touched = touched;
        Dirty = dirty;
        _changed();
    }

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
    internal bool LabelTargetsControl => _focusControls.Count == 0;

    /// <summary>
    /// Registered by a control that isn't a labelable element (<paramref name="owner"/>): how to focus it when the label
    /// is clicked. A control registering again replaces its own registration. With several - another control mounted
    /// later in the same field, such as a calendar in a popover - the first one keeps the label, and a control going away
    /// (<see cref="ReleaseControlFocus"/>) takes only its own registration with it.
    /// </summary>
    internal void SetControlFocus(object owner, Func<Task> focus)
    {
        var index = _focusControls.FindIndex(c => ReferenceEquals(c.Owner, owner));
        if (index >= 0)
        {
            _focusControls[index] = (owner, focus);
            return;
        }
        _focusControls.Add((owner, focus));
        if (_focusControls.Count == 1) _changed();
    }

    /// <summary>Withdraws the registration of <paramref name="owner"/> (a control going away), if it has one.</summary>
    internal void ReleaseControlFocus(object owner)
    {
        var index = _focusControls.FindIndex(c => ReferenceEquals(c.Owner, owner));
        if (index < 0) return;
        _focusControls.RemoveAt(index);
        if (index == 0) _changed();
    }

    internal Task FocusControlAsync() => _focusControls.Count > 0 ? _focusControls[0].Focus() : Task.CompletedTask;

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
