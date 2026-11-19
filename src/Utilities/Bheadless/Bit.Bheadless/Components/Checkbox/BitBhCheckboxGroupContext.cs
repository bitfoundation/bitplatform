namespace Bit.Bheadless;

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
