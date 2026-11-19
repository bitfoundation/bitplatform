namespace Bit.Blow;

/// <summary>The pane's rectangle as a pointer went down on it, and now.</summary>
/// <param name="Down">Read in the press's own event; null when that press was not seen.</param>
/// <param name="Now">Read as asked for.</param>
internal sealed record BlowPaneRectsAtPointerDown(BlowDomRect? Down, BlowDomRect? Now);
