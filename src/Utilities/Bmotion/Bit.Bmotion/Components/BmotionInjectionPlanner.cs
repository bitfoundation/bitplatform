// The RenderTree types are public-but-framework-internal (BL0006): Blazor documents them as
// subject to change between releases. They are used deliberately here - rewriting the child
// content's frames is the only way to inject attributes into consumer-authored markup at render
// time (Blazor has no React-style cloneElement), which is what keeps the Bmotion API free of an
// explicit @attributes splat while remaining FOUC-free and prerender-safe. Revisit this file when
// bumping the supported TFMs.
#pragma warning disable BL0006

namespace Bit.Bmotion;

/// <summary>
/// Decides what to inject into the first root element of the child content, given the element's
/// tag name and any consumer-authored <c>id</c> / <c>style</c> attribute values found on it.
/// </summary>
internal delegate BmotionInjection BmotionInjectionPlanner(string tagName, string? authorId, string? authorStyle);
