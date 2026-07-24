// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// A PDF command / keyword token produced by the lexer (e.g. <c>obj</c>, <c>BT</c>,
/// <c>stream</c>). Interned like <see cref="BitPdfName"/>.
/// </summary>
public sealed class BitPdfCmd
{
    private static readonly ConcurrentDictionary<string, BitPdfCmd> Cache = new(StringComparer.Ordinal);

    /// <summary>The command keyword.</summary>
    public string Value { get; }

    /// <summary>
    /// The keyword resolved to a content-stream operator code (<see cref="BitPdfOpCode.Unknown"/>
    /// for structural/non-operator keywords). Resolved once here because commands
    /// are interned, so the renderer never re-hashes the operator string per op.
    /// </summary>
    internal BitPdfOpCode OpCode { get; }

    private BitPdfCmd(string value)
    {
        Value = value;
        OpCode = BitPdfOpCodes.Resolve(value);
    }

    /// <summary>Returns the interned <see cref="BitPdfCmd"/> for <paramref name="value"/>.</summary>
    public static BitPdfCmd Get(string value) => Cache.GetOrAdd(value, static v => new BitPdfCmd(v));

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    internal static void ClearCache() => Cache.Clear();
}
