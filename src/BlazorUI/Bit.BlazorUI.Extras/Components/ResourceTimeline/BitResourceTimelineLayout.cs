namespace Bit.BlazorUI;

/// <summary>
/// The layout dimensions (in pixels) the rendering engine of the <see cref="BitResourceTimeline"/> works with, read
/// back after it starts and after every change of its options, so the parts drawn in markup (the corner and the
/// resource template overlay) are sized like the parts the engine draws. <see cref="ResourceHeight"/> is the minimum
/// row height; a row grows taller when overlapping bars stack (each row of the overlay carries its own height).
/// </summary>
internal readonly record struct BitResourceTimelineLayout(int ResourceAxisWidth, int TimeAxisHeight, int ResourceHeight, int ResourceIndent = 16);
