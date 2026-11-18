namespace Bit.Bink;

/// <summary>One undoable step.</summary>
public sealed record BinkHistoryEntry(BinkDrawingOperation Forward, BinkDrawingOperation Inverse, string Label, DateTimeOffset Timestamp);
