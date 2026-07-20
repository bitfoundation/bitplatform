// Axial/radial shading handling, emitted as CSS gradients for the HTML renderer.

using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Translates PDF axial (type 2) and radial (type 3) shadings into CSS
/// <c>linear-gradient</c>/<c>radial-gradient</c> background values, sampling the
/// shading's color function into gradient stops. Coordinates are expressed in
/// device pixels relative to the page box (origin top-left).
/// </summary>
internal static class BitPdfCssShadingBuilder
{
    private const int StopCount = 24;

    /// <summary>
    /// Builds a CSS background value for <paramref name="shading"/>, or
    /// <c>null</c> if the shading type is unsupported.
    /// </summary>
    public static string? Build(BitPdfDict shading, IBitPdfXRef xref, BitPdfDict? resources, in BitPdfMatrix ctm,
        double viewW, double viewH)
    {
        int type = shading.Get("ShadingType") is double d ? (int)d : 0;
        var cs = BitPdfColorSpace.Create(shading.Get("ColorSpace"), xref, resources);
        var fn = BitPdfFunction.Create(shading.Get("Function"), xref);

        double[] domain = ReadNumbers(shading.Get("Domain"), xref);
        if (domain.Length < 2)
        {
            domain = [0, 1];
        }
        double[] coords = ReadNumbers(shading.Get("Coords"), xref);

        // /Extend [before after]: whether the shading continues past its ends.
        bool extendBefore = false, extendAfter = false;
        if (shading.Get("Extend") is List<object?> ext && ext.Count >= 2)
        {
            extendBefore = ext[0] is bool b0 && b0;
            extendAfter = ext[1] is bool b1 && b1;
        }

        return type switch
        {
            2 when coords.Length >= 4 => BuildAxial(coords, domain, cs, fn, ctm, viewW, viewH, extendBefore, extendAfter),
            3 when coords.Length >= 6 => BuildRadial(coords, domain, cs, fn, ctm, extendBefore, extendAfter),
            // Function-based (type 1) and mesh (types 4-7) shadings are not
            // rasterized; paint an averaged solid colour so the region is at least
            // tinted rather than left blank.
            _ => BuildFallback(shading, domain, cs, fn),
        };
    }

    /// <summary>
    /// Builds a canvas display-list gradient descriptor for
    /// <paramref name="shading"/>: <c>[kind, coords, stops]</c> where kind 2/3 map
    /// to <c>createLinearGradient</c>/<c>createRadialGradient</c> (coords in device
    /// px, stops as <c>[pos 0..1, cssColor]</c> pairs) and kind 0 is a sampled
    /// solid fallback (<c>coords = null</c>, stops = the color string). Returns
    /// <c>null</c> when nothing can be painted.
    /// </summary>
    public static object?[]? BuildCanvasOp(BitPdfDict shading, IBitPdfXRef xref, BitPdfDict? resources, in BitPdfMatrix ctm)
    {
        int type = shading.Get("ShadingType") is double d ? (int)d : 0;
        var cs = BitPdfColorSpace.Create(shading.Get("ColorSpace"), xref, resources);
        var fn = BitPdfFunction.Create(shading.Get("Function"), xref);

        double[] domain = ReadNumbers(shading.Get("Domain"), xref);
        if (domain.Length < 2)
        {
            domain = [0, 1];
        }
        double[] coords = ReadNumbers(shading.Get("Coords"), xref);

        bool extendBefore = false, extendAfter = false;
        if (shading.Get("Extend") is List<object?> ext && ext.Count >= 2)
        {
            extendBefore = ext[0] is bool b0 && b0;
            extendAfter = ext[1] is bool b1 && b1;
        }

        if (type == 2 && coords.Length >= 4)
        {
            var (x0, y0) = ctm.Apply(coords[0], coords[1]);
            var (x1, y1) = ctm.Apply(coords[2], coords[3]);
            return [2, new[] { Math.Round(x0, 2), Math.Round(y0, 2), Math.Round(x1, 2), Math.Round(y1, 2) },
                CanvasStops(domain, cs, fn, extendBefore, extendAfter)];
        }
        if (type == 3 && coords.Length >= 6)
        {
            double scale = ctm.ScaleFactor;
            var (cx, cy) = ctm.Apply(coords[3], coords[4]);
            double r0 = Math.Max(coords[2] * scale, 0);
            double r1 = Math.Max(coords[5] * scale, 0.01);
            // Canvas radial gradients clamp beyond both circles (an "extend both"
            // approximation, like the CSS backend's single-circle mapping).
            return [3, new[] { Math.Round(cx, 2), Math.Round(cy, 2), Math.Round(r0, 2), Math.Round(r1, 2) },
                CanvasStops(domain, cs, fn, extendBefore: true, extendAfter: true)];
        }

        string? solid = BuildFallback(shading, domain, cs, fn);
        return solid is null ? null : [0, null, solid];
    }

    /// <summary>
    /// Samples the shading function into canvas gradient stops. A non-extended end
    /// gets a transparent stop hard against the boundary so the gradient does not
    /// clamp its end color across the rest of the fill (canvas pads past 0/1).
    /// </summary>
    private static object?[] CanvasStops(double[] domain, BitPdfColorSpace cs, BitPdfFunction? fn,
        bool extendBefore, bool extendAfter)
    {
        var stops = new List<object?>(StopCount + 2);
        var (r0, g0, b0) = SampleRgb(domain, 0, cs, fn);
        if (!extendBefore)
        {
            stops.Add(new object?[] { 0.0, $"rgba({r0},{g0},{b0},0)" });
        }
        for (int i = 0; i < StopCount; i++)
        {
            double frac = (double)i / (StopCount - 1);
            // Squeeze the sampled ramp just inside any transparent guard stops.
            double pos = extendBefore && extendAfter ? frac : 0.001 + frac * 0.998;
            var (r, g, b) = SampleRgb(domain, frac, cs, fn);
            stops.Add(new object?[] { Math.Round(pos, 4), $"rgb({r},{g},{b})" });
        }
        if (!extendAfter)
        {
            var (r1, g1, b1) = SampleRgb(domain, 1, cs, fn);
            stops.Add(new object?[] { 1.0, $"rgba({r1},{g1},{b1},0)" });
        }
        return stops.ToArray();
    }

    private static string? BuildFallback(BitPdfDict shading, double[] domain, BitPdfColorSpace cs, BitPdfFunction? fn)
    {
        if (fn is not null)
        {
            long r = 0, g = 0, b = 0;
            const int samples = 9;
            for (int i = 0; i < samples; i++)
            {
                var (sr, sg, sb) = SampleRgb(domain, (double)i / (samples - 1), cs, fn);
                r += sr;
                g += sg;
                b += sb;
            }
            return $"rgb({r / samples},{g / samples},{b / samples})";
        }
        // No function (mesh shading): honour /Background if present.
        if (shading.Get("Background") is List<object?> bg && bg.Count > 0)
        {
            var comps = new double[bg.Count];
            for (int i = 0; i < bg.Count; i++)
            {
                comps[i] = bg[i] is double d ? d : 0;
            }
            var (r, g, b) = cs.GetRgb(comps);
            return $"rgb({r},{g},{b})";
        }
        return null;
    }

    private static string BuildAxial(double[] c, double[] domain, BitPdfColorSpace cs, BitPdfFunction? fn,
        in BitPdfMatrix ctm, double viewW, double viewH, bool extendBefore, bool extendAfter)
    {
        var (x0, y0) = ctm.Apply(c[0], c[1]);
        var (x1, y1) = ctm.Apply(c[2], c[3]);
        double dx = x1 - x0, dy = y1 - y0;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6)
        {
            // Degenerate axis: fall back to the mid color.
            var (r, g, b) = SampleRgb(domain, 0.5, cs, fn);
            return $"rgb({r},{g},{b})";
        }

        double ux = dx / len, uy = dy / len;
        // CSS gradient angle: 0deg points up (-y), increasing clockwise toward +x.
        double angleDeg = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        double a = angleDeg * Math.PI / 180.0;
        double gradientLen = Math.Abs(viewW * Math.Sin(a)) + Math.Abs(viewH * Math.Cos(a));
        if (gradientLen < 1e-6)
        {
            gradientLen = len;
        }
        double cx = viewW / 2.0, cy = viewH / 2.0;

        double Pct(double px, double py)
            => ((px - cx) * ux + (py - cy) * uy + gradientLen / 2.0) / gradientLen * 100.0;

        double p0 = Pct(x0, y0);
        double p1 = Pct(x1, y1);

        var stops = new List<(double Pos, int R, int G, int B)>(StopCount);
        for (int i = 0; i < StopCount; i++)
        {
            double frac = (double)i / (StopCount - 1);
            double pos = p0 + frac * (p1 - p0);
            var (r, g, b) = SampleRgb(domain, frac, cs, fn);
            stops.Add((pos, r, g, b));
        }
        stops.Sort(static (l, r) => l.Pos.CompareTo(r.Pos));

        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"linear-gradient({angleDeg:0.##}deg"));
        // When an end is not extended, insert a hard transparent stop just outside
        // the axis so the shading does not bleed a solid colour across the page.
        if (!extendBefore)
        {
            var f = stops[0];
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $",rgba({f.R},{f.G},{f.B},0) {f.Pos:0.##}%"));
        }
        foreach (var s in stops)
        {
            sb.Append(string.Create(CultureInfo.InvariantCulture, $",rgb({s.R},{s.G},{s.B}) {s.Pos:0.##}%"));
        }
        if (!extendAfter)
        {
            var l = stops[^1];
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $",rgba({l.R},{l.G},{l.B},0) {l.Pos:0.##}%"));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static string BuildRadial(double[] c, double[] domain, BitPdfColorSpace cs, BitPdfFunction? fn,
        in BitPdfMatrix ctm, bool extendBefore, bool extendAfter)
    {
        double scale = ctm.ScaleFactor;
        var (cx, cy) = ctm.Apply(c[3], c[4]);
        double r0 = Math.Max(c[2] * scale, 0);      // inner radius
        double r1 = Math.Max(c[5] * scale, 0.01);   // outer radius
        // Map colour fraction 0..1 onto the annulus [r0, r1] instead of [0, r1],
        // so a non-zero inner radius is honoured (a single-circle approximation).
        double r0Frac = r1 > 0 ? Math.Clamp(r0 / r1, 0, 0.999) : 0;

        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"radial-gradient(circle {r1:0.##}px at {cx:0.##}px {cy:0.##}px"));
        // Fill the inner disc: solid first colour if extended, else transparent.
        var (ir, ig, ib) = SampleRgb(domain, 0, cs, fn);
        if (r0Frac > 0)
        {
            sb.Append(extendBefore
                ? string.Create(CultureInfo.InvariantCulture, $",rgb({ir},{ig},{ib}) 0%")
                : string.Create(CultureInfo.InvariantCulture, $",rgba({ir},{ig},{ib},0) {r0Frac * 100:0.##}%"));
        }
        for (int i = 0; i < StopCount; i++)
        {
            double frac = (double)i / (StopCount - 1);
            double pos = (r0Frac + frac * (1 - r0Frac)) * 100;
            var (rr, gg, bb) = SampleRgb(domain, frac, cs, fn);
            sb.Append(string.Create(CultureInfo.InvariantCulture, $",rgb({rr},{gg},{bb}) {pos:0.##}%"));
        }
        if (!extendAfter)
        {
            var (er, eg, eb) = SampleRgb(domain, 1, cs, fn);
            sb.Append(string.Create(CultureInfo.InvariantCulture, $",rgba({er},{eg},{eb},0) 100%"));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static (int R, int G, int B) SampleRgb(double[] domain, double frac, BitPdfColorSpace cs, BitPdfFunction? fn)
    {
        double t = domain[0] + frac * (domain[1] - domain[0]);
        double[] comps = fn?.Eval([t]) ?? [t];
        return cs.GetRgb(comps);
    }

    private static double[] ReadNumbers(object? value, IBitPdfXRef? xref = null)
    {
        if (value is not List<object?> arr)
        {
            return [];
        }
        var result = new double[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            // Elements may be indirect references (1.26).
            result[i] = BitPdfPrimitives.ResolveNumber(xref, arr[i]);
        }
        return result;
    }
}
