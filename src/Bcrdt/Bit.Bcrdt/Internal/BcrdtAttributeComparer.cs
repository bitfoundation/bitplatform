namespace Bit.Bcrdt;

/// <summary>Comparison and folding rules for rich-text attributes.</summary>
internal static class BcrdtAttributeComparer
{
    /// <summary>
    /// Whether two attribute values are interchangeable.
    /// </summary>
    /// <remarks>
    /// A missing attribute and an explicitly null one mean the same thing - "not formatted" - so
    /// they compare equal. Treating them as different would make the engine emit a mark to clear an
    /// attribute that was never set, and those redundant marks accumulate.
    /// </remarks>
    public static bool Equal(object? left, object? right) =>
        (left is null && right is null) || BcrdtAnyCodec.DeepEquals(left, right);

    /// <summary>Applies a mark to the running attribute state.</summary>
    public static void Apply(Dictionary<string, object?> current, BcrdtContentFormat format)
    {
        if (format.Value is null) current.Remove(format.Key);
        else current[format.Key] = format.Value;
    }

    /// <summary>Whether two attribute sets describe the same formatting.</summary>
    public static bool SetsEqual(
        IReadOnlyDictionary<string, object?>? left,
        IReadOnlyDictionary<string, object?>? right)
    {
        int leftCount = CountMeaningful(left);
        int rightCount = CountMeaningful(right);
        if (leftCount != rightCount) return false;
        if (leftCount == 0) return true;

        foreach ((string key, object? value) in left!)
        {
            if (value is null) continue;
            if (right is null || !right.TryGetValue(key, out object? other) || !Equal(value, other)) return false;
        }
        return true;
    }

    private static int CountMeaningful(IReadOnlyDictionary<string, object?>? attributes)
    {
        if (attributes is null) return 0;
        int count = 0;
        foreach (object? value in attributes.Values)
        {
            if (value is not null) count++;
        }
        return count;
    }
}
