namespace Bit.BlazorUI;

/// <summary>
/// One term of a parsed query: its <paramref name="Text"/> without the operator characters, how it is matched, and its
/// <paramref name="Group"/> - terms sharing a group are alternatives (<c>a | b</c>), of which one has to hold.
/// </summary>
public readonly record struct BitCommandPaletteQueryTerm(string Text, BitCommandPaletteQueryOperator Operator, int Group);
