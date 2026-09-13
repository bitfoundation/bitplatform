namespace Bit.BlazorUI;

/// <summary>
/// A slice of a suggest item text along with whether it matches the current search term,
/// used to render the highlighted parts of the suggest items of the <see cref="BitSearchBox"/>.
/// </summary>
internal sealed record BitSearchBoxHighlightSegment(string Text, bool IsMatch);
