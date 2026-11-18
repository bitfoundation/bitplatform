namespace Bit.Borm;

/// <summary>One function an expression may call.</summary>
internal sealed class BormExpressionFunction
{
    public required string Name { get; init; }
    public int MinArgs { get; init; }
    public int MaxArgs { get; init; } = int.MaxValue;

    /// <summary>Called with every argument already evaluated.</summary>
    public Func<IReadOnlyList<object?>, object?>? Body { get; init; }

    /// <summary>For a function that decides which arguments to evaluate at all — <c>iif</c>.</summary>
    public Func<IReadOnlyList<BormExpressionNode>, IBormDataReader, object?>? Lazy { get; init; }

    /// <summary>A check run when the expression is parsed; returns the problem, or null.</summary>
    public Func<IReadOnlyList<BormExpressionNode>, string?>? Check { get; init; }

    public bool BuiltIn { get; init; }
}
