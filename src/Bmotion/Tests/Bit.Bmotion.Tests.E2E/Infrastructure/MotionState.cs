using System.Text.Json;

namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// What an element looks like at one instant, as the browser computes it - so it reflects a Web Animation playing
/// on the compositor exactly as it reflects an inline style the C# frame loop wrote. The transform is decomposed
/// from its matrix (translation in px, scale, rotation in degrees), which is what lets a test say "x is 120"
/// whatever transform string produced it.
/// </summary>
public sealed record MotionState(
    double X, double Y, double ScaleX, double ScaleY, double Rotate, double Opacity,
    double Left, double Top, double Width, double Height,
    string Transform, string BackgroundColor, string InlineStyle)
{
    /// <summary>When the sample was taken, in ms since sampling started (<see cref="BmotionPageTest.SampleAsync"/> only).</summary>
    public double Time { get; init; }

    /// <summary>
    /// The probe, as a JavaScript function of a selector. Shared by <see cref="BmotionPageTest.ProbeAsync"/>, the
    /// polling waits and the frame sampler, so all three measure the same thing.
    /// </summary>
    public const string ProbeFunction = """
        (sel) => {
            const el = document.querySelector(sel);
            if (!el) return null;
            const cs = getComputedStyle(el);
            const m = cs.transform && cs.transform !== 'none' ? new DOMMatrixReadOnly(cs.transform) : new DOMMatrixReadOnly();
            const r = el.getBoundingClientRect();
            return {
                x: m.m41, y: m.m42,
                scaleX: Math.hypot(m.a, m.b), scaleY: Math.hypot(m.c, m.d),
                rotate: Math.atan2(m.b, m.a) * 180 / Math.PI,
                opacity: parseFloat(cs.opacity),
                left: r.left, top: r.top, width: r.width, height: r.height,
                transform: cs.transform, backgroundColor: cs.backgroundColor,
                style: el.getAttribute('style') || ''
            };
        }
        """;

    public static MotionState? From(JsonElement json) => json.ValueKind is JsonValueKind.Object
        ? new(
            json.GetProperty("x").GetDouble(), json.GetProperty("y").GetDouble(),
            json.GetProperty("scaleX").GetDouble(), json.GetProperty("scaleY").GetDouble(),
            json.GetProperty("rotate").GetDouble(), json.GetProperty("opacity").GetDouble(),
            json.GetProperty("left").GetDouble(), json.GetProperty("top").GetDouble(),
            json.GetProperty("width").GetDouble(), json.GetProperty("height").GetDouble(),
            json.GetProperty("transform").GetString() ?? "", json.GetProperty("backgroundColor").GetString() ?? "",
            json.GetProperty("style").GetString() ?? "")
        {
            Time = json.TryGetProperty("t", out var t) ? t.GetDouble() : 0,
        }
        : null;

    public override string ToString() =>
        $"x={X:0.##} y={Y:0.##} scale=({ScaleX:0.###},{ScaleY:0.###}) rotate={Rotate:0.#} opacity={Opacity:0.###} " +
        $"box=({Left:0.#},{Top:0.#} {Width:0.#}x{Height:0.#}) bg={BackgroundColor} style=\"{InlineStyle}\"";
}
