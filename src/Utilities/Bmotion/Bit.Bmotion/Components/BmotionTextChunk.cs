namespace Bit.Bmotion;

/// <summary>
/// One piece of a split text run. A chunk is either a <see cref="IsGap"/> run of whitespace
/// (rendered verbatim, never animated, so the text still wraps and reads normally) or a group of
/// one or more animated <see cref="Units"/>.
/// </summary>
/// <param name="Text">The chunk's raw text - the whitespace itself for a gap, the word/line otherwise.</param>
/// <param name="IsGap">Whether this chunk is inter-unit whitespace rather than animated content.</param>
/// <param name="Units">The individually animated units of this chunk (empty for a gap).</param>
internal sealed record BmotionTextChunk(string Text, bool IsGap, IReadOnlyList<string> Units);
