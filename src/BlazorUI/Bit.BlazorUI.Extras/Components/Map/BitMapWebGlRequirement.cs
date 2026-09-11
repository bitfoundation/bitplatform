namespace Bit.BlazorUI;

/// <summary>
/// The WebGL context a <see cref="IBitMapProvider"/> backend needs before it can draw anything.
/// The numeric values ARE the context versions, so a provider's requirement is compared directly
/// against the highest version the browser hands out.
/// </summary>
public enum BitMapWebGlRequirement
{
    /// <summary>The backend draws without WebGL (a raster/DOM renderer such as Leaflet).</summary>
    None = 0,

    /// <summary>A <c>webgl</c> context is enough.</summary>
    WebGl = 1,

    /// <summary>Needs a <c>webgl2</c> context; a WebGL 1-only browser paints a blank canvas.</summary>
    WebGl2 = 2,
}
