namespace Bit.Blow;

/// <summary>
/// What a node's handler produced: the value each of its outputs carries on. An output that carries nothing leaves the
/// edges from it inactive - which is how a node branches.
/// </summary>
public sealed class BlowNodeResult
{
    private BlowNodeResult(object? value, IReadOnlyDictionary<string, object?>? outputs)
    {
        Value = value;
        _outputs = outputs;
    }

    /// <summary>Succeeds with nothing going on: no edge from the node carries a value.</summary>
    public static BlowNodeResult Stop() => new(null, EmptyOutputs);

    /// <summary>Succeeds, every edge from the node carrying <paramref name="value"/>.</summary>
    public static BlowNodeResult Done(object? value = null) => new(value, null);

    /// <summary>Succeeds with one output carrying the value: the edges from that source handle are active, the others not.</summary>
    /// <param name="handleId">The source handle; null or empty for the node's unnamed one.</param>
    /// <param name="value">What its edges carry.</param>
    public static BlowNodeResult Branch(string? handleId, object? value = null)
        => new(null, new Dictionary<string, object?>(StringComparer.Ordinal) { [handleId ?? ""] = value });

    /// <summary>Succeeds with the given outputs, by source handle id (an empty key for the unnamed handle).</summary>
    public static BlowNodeResult Outputs(IReadOnlyDictionary<string, object?> outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        return new(null, new Dictionary<string, object?>(outputs, StringComparer.Ordinal));
    }

    /// <summary>The value every edge carries, when the result is <see cref="Done"/>.</summary>
    public object? Value { get; }

    /// <summary>The values per source handle, or null when every edge carries <see cref="Value"/>.</summary>
    public IReadOnlyDictionary<string, object?>? OutputValues => _outputs;

    private readonly IReadOnlyDictionary<string, object?>? _outputs;

    /// <summary>A message for the node's run state - a result to show, a count.</summary>
    public string? Message { get; init; }

    /// <summary>Whether edges from the given source handle carry a value, and which.</summary>
    public bool TryGetOutput(string? handleId, out object? value)
    {
        if (_outputs is null)
        {
            value = Value;
            return true;
        }
        return _outputs.TryGetValue(handleId ?? "", out value);
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyOutputs = new Dictionary<string, object?>();
}
