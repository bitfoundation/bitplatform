using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>
/// Keyframe payload for <see cref="ElementReferenceAnimationExtensions.Animate"/>. Each entry is a
/// dictionary of CSS property → string value, e.g. <c>{ "opacity", "0" }</c>, <c>{ "transform", "translateX(0px)" }</c>.
/// </summary>
public class AnimationKeyframes : List<Dictionary<string, string>> { }
