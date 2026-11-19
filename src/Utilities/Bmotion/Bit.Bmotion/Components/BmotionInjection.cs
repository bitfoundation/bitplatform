// The RenderTree types are public-but-framework-internal (BL0006): Blazor documents them as
// subject to change between releases. They are used deliberately here - rewriting the child
// content's frames is the only way to inject attributes into consumer-authored markup at render
// time (Blazor has no React-style cloneElement), which is what keeps the Bmotion API free of an
// explicit @attributes splat while remaining FOUC-free and prerender-safe. Revisit this file when
// bumping the supported TFMs.
#pragma warning disable BL0006

namespace Bit.Bmotion;

/// <summary>
/// The attributes a <see cref="Bmotion"/> injects into the animated element.
/// <see cref="MotionStyle"/> is the motion-only initial style; the rewriter places it before any
/// consumer-authored declarations so the consumer's win conflicts.
/// </summary>
internal readonly record struct BmotionInjection(string Id, string? MotionStyle, bool AddPathLength);
