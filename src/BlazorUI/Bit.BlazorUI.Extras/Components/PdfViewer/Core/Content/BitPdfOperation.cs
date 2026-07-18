namespace Bit.BlazorUI;

/// <summary>
/// A single content-stream instruction: an operator keyword together with the
/// operands that preceded it (PDF content uses postfix notation).
/// </summary>
public readonly struct BitPdfOperation
{
    /// <summary>The operator keyword (e.g. <c>Tj</c>, <c>re</c>, <c>cm</c>).</summary>
    public string Operator { get; }

    /// <summary>The operands collected before the operator.</summary>
    public IReadOnlyList<object?> Operands { get; }

    /// <summary>The operator resolved to a compact code for jump-table dispatch.</summary>
    internal BitPdfOpCode Code { get; }

    public BitPdfOperation(string op, IReadOnlyList<object?> operands)
        : this(op, BitPdfOpCodes.Resolve(op), operands)
    {
    }

    /// <summary>
    /// Fast path for the parser, which already holds the resolved code from the
    /// interned <see cref="BitPdfCmd"/> and so avoids re-resolving the keyword.
    /// </summary>
    internal BitPdfOperation(string op, BitPdfOpCode code, IReadOnlyList<object?> operands)
    {
        Operator = op;
        Code = code;
        Operands = operands;
    }

    /// <summary>Returns operand <paramref name="index"/> as a double, or <paramref name="fallback"/>.</summary>
    public double Num(int index, double fallback = 0)
        => index >= 0 && index < Operands.Count && Operands[index] is double d ? d : fallback;

    public int Count => Operands.Count;
}
