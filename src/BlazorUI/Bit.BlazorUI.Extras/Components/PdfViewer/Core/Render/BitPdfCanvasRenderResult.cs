// Canvas rendering: the display-list front end over the shared content engine.

namespace Bit.BlazorUI;

/// <summary>The output of a <see cref="BitPdfCanvasRenderer"/> page render.</summary>
/// <param name="TextLayerHtml">
/// The page's DOM part: the positioned page <c>div</c> containing the
/// <c>&lt;canvas&gt;</c> placeholder, the selectable text layer, and link/
/// annotation overlays — everything except the painted content.
/// </param>
/// <param name="OpsJson">
/// The display list to replay onto the canvas (JSON array of drawing ops),
/// consumed by the viewer's JavaScript interpreter.
/// </param>
/// <param name="Width">Page width in CSS pixels (device space).</param>
/// <param name="Height">Page height in CSS pixels (device space).</param>
public sealed record BitPdfCanvasRenderResult(string TextLayerHtml, string OpsJson, double Width, double Height);
