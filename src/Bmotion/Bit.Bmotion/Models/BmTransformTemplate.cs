namespace Bit.Bmotion;

/// <summary>
/// Rewrites the CSS <c>transform</c> string Bmotion composes for an element - motion.dev's
/// <c>transformTemplate</c>. Use it to change the order the components are applied in, or to keep a
/// transform of your own in front of the animated ones:
/// <code>
/// // keep the element centred on its own anchor, whatever the animation does
/// TransformTemplate="(_, generated) =&gt; $\"translate(-50%, -50%) {generated}\""
///
/// // rotate first, then translate (CSS applies transforms right to left)
/// TransformTemplate="(c, _) =&gt; $\"rotate({c.GetValueOrDefault(\"rotate\")}deg) translateX({c.GetValueOrDefault(\"x\")}px)\""
/// </code>
/// </summary>
/// <param name="components">
/// The element's live transform components by engine name - <c>x</c>, <c>y</c>, <c>z</c>,
/// <c>scale</c>, <c>scaleX</c>, <c>scaleY</c>, <c>rotate</c>, <c>rotateX</c>, <c>rotateY</c>,
/// <c>rotateZ</c>, <c>skewX</c>, <c>skewY</c>, <c>perspective</c>. Only components the element
/// actually carries are present; look them up with <c>GetValueOrDefault</c>. Lengths are in px and
/// angles in degrees, matching what the composer would emit.
/// </param>
/// <param name="generated">
/// The transform string Bmotion composed, e.g. <c>"translate(10px,0px) scale(1.2)"</c>. Empty when
/// every component is at its identity.
/// </param>
/// <returns>
/// The transform string to write to the element. Returning <paramref name="generated"/> unchanged
/// is the same as not setting a template at all.
/// </returns>
/// <remarks>
/// The delegate runs on every frame the transform changes, so keep it allocation-light. It is
/// applied everywhere Bmotion writes a transform - the per-frame loop, the pre-first-paint inline
/// style, instant <c>Set</c> calls, and the keyframes handed to the compositor - so the element
/// never flickers between a templated and an untemplated transform.
/// </remarks>
public delegate string BmTransformTemplate(IReadOnlyDictionary<string, double> components, string generated);
