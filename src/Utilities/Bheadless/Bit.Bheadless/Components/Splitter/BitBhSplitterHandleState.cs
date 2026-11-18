namespace Bit.Bheadless;

/// <summary>What a <see cref="BitBhSplitterHandle"/> controls: the panels on either side and the before panel's size range.</summary>
internal sealed record BitBhSplitterHandleState(int Index, BitBhSplitterPanel Before, BitBhSplitterPanel After, double Now, double Min, double Max);
