namespace Bit.BlazorUI;

/// <summary>
/// What a custom mathematics renderer (<see cref="BitAiMathRenderer"/>) is given.
/// </summary>
/// <param name="Expression">The expression source, without its dollar delimiters.</param>
/// <param name="IsBlock"><see langword="true"/> for display mathematics, <see langword="false"/> for an inline expression.</param>
public sealed record BitAiMathContext(string Expression, bool IsBlock);
