namespace Bit.Bmotion;

/// <summary>
/// What a <see cref="Bmotion"/> renders while the page is prerendered or statically rendered on the
/// server - the pass where no browser is attached and the C# engine can't run. Set globally via
/// <see cref="BitBmotionOptions.Prerender"/> and per element via <see cref="Bmotion.Prerender"/>.
/// <para>
/// <see cref="Final"/> and <see cref="Animate"/> also change what happens when the runtime takes over:
/// Blazor replaces the prerendered markup with a fresh render, and an element that was prerendered at
/// rest renders straight into that resting state there too, instead of jumping back to
/// <c>Initial</c> and playing its enter animation a second time.
/// </para>
/// </summary>
public enum BmPrerenderMode
{
    /// <summary>
    /// <b>Default.</b> The prerendered element shows its <c>Initial</c> state and the enter animation
    /// starts once the runtime is interactive. Content that starts hidden (<c>opacity: 0</c>) stays
    /// hidden until then.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// The prerendered element shows the state its enter animation settles on - visible, indexable
    /// content with no animation. When the runtime takes over, the element is already at rest and the
    /// enter animation is skipped (motion.dev's <c>initial={false}</c>, applied to the prerendered pass).
    /// </summary>
    Final = 1,

    /// <summary>
    /// The prerendered element plays its enter animation as a pure CSS animation, so it runs the moment
    /// the HTML arrives - before the runtime loads, and on pages that never become interactive. When the
    /// runtime takes over, the element continues from where the CSS animation got to (on WebAssembly) or
    /// lands at rest, and the enter animation is not replayed.
    /// <para>
    /// Requires the library stylesheet in the page's <c>&lt;head&gt;</c>:
    /// <c>&lt;link rel="stylesheet" href="_content/Bit.Bmotion/bit-bmotion.css" /&gt;</c>. Without it the
    /// element simply renders at rest, as with <see cref="Final"/>. Targets or transitions CSS can't
    /// express (inertia, arcs, a spring with initial velocity, …) also render at rest.
    /// </para>
    /// </summary>
    Animate = 2,
}
