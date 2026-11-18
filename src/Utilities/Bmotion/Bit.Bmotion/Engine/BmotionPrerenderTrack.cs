namespace Bit.Bmotion;

/// <summary>
/// One animated CSS property: its keyframe values, plus the timing it plays with normally and under
/// reduced motion (<c>null</c> = that variant doesn't animate it - the property just sits at rest).
/// </summary>
internal sealed record BmotionPrerenderTrack(string CssProperty, string[] Frames, BmotionPrerenderTiming? Full, BmotionPrerenderTiming? Reduced);
