using System.Collections;

namespace Bit.BevTools;

/// <summary>
/// One value of a log entry captured before BevTools was attached. An immutable scalar (a string, a number, a date, an
/// enum, a <see cref="Guid"/>, a <see cref="Uri"/>) is kept as itself, so the attached session describes it exactly as it
/// describes a value logged after the attach. Anything else is kept as what it was when it was logged - its type, its
/// <c>ToString</c> when short, its count - and never as the object: holding it would keep whatever it references alive
/// until an attach that may never come, and an object read later shows its state then, not when it was logged.
/// </summary>
internal readonly struct BevToolsAutoStartLogValue
{
    /// <summary>
    /// Longer than the inspector ever shows an object's <c>ToString</c> as its summary (120 characters for a reference
    /// type), so a longer text is known to be too long without being kept whole.
    /// </summary>
    internal const int MaxTextLength = 500;

    private BevToolsAutoStartLogValue(string name, object? value, Type? objectType, string? text, int? count)
    {
        Name = name;
        Value = value;
        ObjectType = objectType;
        Text = text;
        Count = count;
    }

    public string Name { get; }

    /// <summary>The value itself when it is null or an immutable scalar; null otherwise.</summary>
    public object? Value { get; }

    /// <summary>The type of a value that was not kept, or null when <see cref="Value"/> is the value.</summary>
    public Type? ObjectType { get; }

    /// <summary>
    /// The <c>ToString</c> of a value that was not kept, when it fits <see cref="MaxTextLength"/>; null when it did not
    /// (too long to be shown as a summary anyway).
    /// </summary>
    public string? Text { get; }

    /// <summary>The number of items of a collection that was not kept, when it says so without being enumerated.</summary>
    public int? Count { get; }

    public static BevToolsAutoStartLogValue Capture(string name, object? value)
    {
        if (IsScalar(value))
        {
            return new(name, value, null, null, null);
        }

        var text = SafeToString(value!);
        return new(name, null, value!.GetType(), text.Length <= MaxTextLength ? text : null, value is ICollection collection ? SafeCount(collection) : null);
    }

    /// <summary>
    /// The value's name and type only, without running any of its code (no <c>ToString</c>, no count): what is kept where
    /// the session that attaches will read no values anyway (<see cref="BevToolsAutoStartLogEntry.ValuesRead"/>).
    /// </summary>
    public static BevToolsAutoStartLogValue Unread(string name, object? value) => new(name, null, value?.GetType(), null, null);

    /// <summary>
    /// Values whose instances never change and reference nothing else: keeping one keeps nothing alive and shows it as
    /// it was. The inspector describes each of them from the value alone.
    /// </summary>
    internal static bool IsScalar(object? value) => value is null or string or bool or char or byte or sbyte or short or ushort or int or uint or long
        or ulong or float or double or decimal or nint or nuint or Half or Int128 or UInt128 or DateTime or DateTimeOffset or DateOnly or TimeOnly
        or TimeSpan or Guid or Version or Uri or Enum;

    internal static string SafeToString(object value)
    {
        try
        {
            return value.ToString() ?? "";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // What the inspector shows for a ToString that throws, so the attached session reads it the same way.
            return "«ToString threw " + ex.GetType().Name + "»";
        }
    }

    private static int? SafeCount(ICollection collection)
    {
        try
        {
            return collection.Count;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }
}
