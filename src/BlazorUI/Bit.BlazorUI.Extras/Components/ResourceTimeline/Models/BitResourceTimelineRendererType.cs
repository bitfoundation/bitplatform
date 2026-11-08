using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Which rendering technology the timeline uses to paint its scene. All
/// renderers share the same engine (data, layout, interaction and events are
/// identical); only how the frame is drawn differs.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineRendererType>))]
public enum BitResourceTimelineRendererType
{
    /// <summary>
    /// HTML canvas (the default). Immediate-mode 2D drawing with HiDPI-aware
    /// output; the fastest option for large, dense datasets.
    /// </summary>
    Canvas,

    /// <summary>
    /// Inline SVG. Resolution-independent vector output that is styleable,
    /// inspectable in devtools and copy/print friendly. Slower than canvas on
    /// very dense scenes.
    /// </summary>
    Svg,

    /// <summary>
    /// Plain HTML elements. Each visible bar is a real DOM element (carrying
    /// its allocation id in <c>data-bar-id</c>), so the output can be styled
    /// with CSS. Slower than canvas on very dense scenes.
    /// </summary>
    Html
}
