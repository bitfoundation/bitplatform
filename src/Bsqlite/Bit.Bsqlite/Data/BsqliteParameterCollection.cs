using System.Collections;
using System.Data.Common;

namespace Bit.Bsqlite.Data;

/// <summary>Parameter collection for <see cref="BsqliteCommand"/>.</summary>
/// <remarks>
/// Names match with or without their sigil, because callers write <c>p0</c> as often as
/// <c>@p0</c> and SQLite accepts <c>@</c>, <c>$</c>, and <c>:</c> alike.
/// </remarks>
public sealed class BsqliteParameterCollection : DbParameterCollection, IEnumerable<BsqliteParameter>
{
    private readonly List<BsqliteParameter> _parameters = [];

    public override int Count => _parameters.Count;

    public override object SyncRoot { get; } = new();

    public new BsqliteParameter this[int index]
    {
        get => _parameters[index];
        set => _parameters[index] = value;
    }

    public new BsqliteParameter this[string parameterName]
    {
        get => _parameters[RequireIndex(parameterName)];
        set => _parameters[RequireIndex(parameterName)] = value;
    }

    /// <summary>Adds a parameter by name and value and returns it.</summary>
    public BsqliteParameter Add(string parameterName, object? value)
    {
        var parameter = new BsqliteParameter(parameterName, value);
        _parameters.Add(parameter);
        return parameter;
    }

    /// <inheritdoc cref="Add(string, object?)"/>
    public BsqliteParameter AddWithValue(string parameterName, object? value) => Add(parameterName, value);

    public override int Add(object value)
    {
        _parameters.Add(Cast(value));
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var value in values)
        {
            Add(value!);
        }
    }

    public override void Clear() => _parameters.Clear();

    public override bool Contains(object value) => _parameters.Contains(Cast(value));

    public override bool Contains(string value) => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

    IEnumerator<BsqliteParameter> IEnumerable<BsqliteParameter>.GetEnumerator() => _parameters.GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf(Cast(value));

    public override int IndexOf(string parameterName)
    {
        if (parameterName is null)
        {
            return -1;
        }

        var index = _parameters.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.Ordinal));
        if (index >= 0)
        {
            return index;
        }

        var bare = StripSigil(parameterName);
        return _parameters.FindIndex(p => string.Equals(StripSigil(p.ParameterName), bare, StringComparison.Ordinal));
    }

    public override void Insert(int index, object value) => _parameters.Insert(index, Cast(value));

    public override void Remove(object value) => _parameters.Remove(Cast(value));

    public override void RemoveAt(int index) => _parameters.RemoveAt(index);

    public override void RemoveAt(string parameterName) => RemoveAt(RequireIndex(parameterName));

    protected override DbParameter GetParameter(int index) => _parameters[index];

    protected override DbParameter GetParameter(string parameterName) => _parameters[RequireIndex(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = Cast(value);

    protected override void SetParameter(string parameterName, DbParameter value)
        => _parameters[RequireIndex(parameterName)] = Cast(value);

    private int RequireIndex(string parameterName)
    {
        var index = IndexOf(parameterName);
        return index >= 0
            ? index
            : throw new IndexOutOfRangeException($"No parameter named '{parameterName}'.");
    }

    private static string StripSigil(string name)
        => name.Length > 0 && name[0] is '@' or '$' or ':' ? name[1..] : name;

    private static BsqliteParameter Cast(object value)
        => value as BsqliteParameter
            ?? throw new InvalidCastException(
                $"Expected {nameof(BsqliteParameter)} but received {value?.GetType().Name ?? "null"}.");
}
