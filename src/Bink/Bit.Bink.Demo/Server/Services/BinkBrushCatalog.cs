using System.Text;
using System.Numerics;
using System.Text.Json;
using System.Globalization;
using Bit.Bink.Demo.Server.Dtos;

namespace Bit.Bink.Demo.Server.Services;

/// <summary>
/// The brushes Bit.Bink registers by default, read out of the library itself: the kinds come from
/// <see cref="BinkBrushRegistry.Default"/>, every value from an instance its factory creates, the
/// kind of geometry from asking the brush to build a stroke, and the JSON from
/// <see cref="BinkSerializer"/> writing one.
/// <para>
/// Nothing here is a table someone keeps up to date. A brush added to the registry shows up the
/// moment it is registered, with the defaults it really has, and a default that changes is reported
/// as it is - which is the point: "the pen is 4 wide with an S-curve" is exactly the kind of fact an
/// agent half remembers from another version.
/// </para>
/// </summary>
public static class BinkBrushCatalog
{
    private static readonly Lazy<BinkBrushDto[]> _brushes = new(Build);

    /// <summary>Every brush kind of the default registry, in registration-independent (alphabetical) order.</summary>
    public static BinkBrushDto[] Brushes => _brushes.Value;

    /// <summary>One brush by its kind ("pen") or its type name ("BinkPenBrush", "PenBrush"), or null.</summary>
    public static BinkBrushDto? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var text = key.Trim();

        return Brushes.FirstOrDefault(brush => string.Equals(brush.Kind, text, StringComparison.OrdinalIgnoreCase))
            ?? Brushes.FirstOrDefault(brush => string.Equals(brush.TypeName, text, StringComparison.OrdinalIgnoreCase))
            ?? Brushes.FirstOrDefault(brush => string.Equals(brush.TypeName, $"Bink{text}", StringComparison.OrdinalIgnoreCase))
            ?? Brushes.FirstOrDefault(brush => string.Equals(brush.TypeName, $"Bink{text}Brush", StringComparison.OrdinalIgnoreCase))
            ?? Brushes.FirstOrDefault(brush => string.Equals(brush.Kind.Replace("-", string.Empty, StringComparison.Ordinal), text.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The index of the brushes as one Markdown table, as GetBinkBrushes answers when it is called without a kind.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bink built-in brushes").AppendLine();
        builder.AppendLine($"The kinds `BinkBrushRegistry.Default` registers, each with the defaults a new instance really has. Call ")
               .AppendLine("`GetBinkBrushes(kind: \"...\")` for one of them in full, with its documentation and the JSON a stroke stores it as.")
               .AppendLine()
               .AppendLine("| Kind | Type | Width | Opacity | Blend | Pressure curve | Geometry |")
               .AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

        foreach (var brush in Brushes)
        {
            builder.AppendLine($"| `{brush.Kind}` | `{brush.TypeName}` | {Number(brush.Width)} | {Number(brush.Opacity)} | {brush.BlendMode} | {brush.PressureCurve} | {brush.Geometry} |");
        }

        builder.AppendLine()
               .AppendLine("Brushes are immutable records: derive a variant with `with` (`new BinkPenBrush() with { Color = BinkColor.Parse(\"#1d4ed8\"), Width = 6f }`) ")
               .AppendLine("and pass it to `<BinkCanvas Brush=\"...\">`. \"Fill\" geometry is a variable-width outline that follows pressure; \"Centerline\" ")
               .AppendLine("is a constant-width stroked path. A custom brush derives from `BinkBrush`, gives itself a unique `Kind` and is registered ")
               .AppendLine("with `BinkBrushRegistry` so documents that use it load back as that brush rather than as a pen - see ")
               .AppendLine("`GetBinkGuideSection(heading: \"A custom brush\")`.");

        return builder.ToString();
    }

    /// <summary>The full description of one brush as Markdown, or null when no registered kind or type goes by that name.</summary>
    public static string? Render(string key)
    {
        var brush = Find(key);
        if (brush is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# {brush.TypeName} (`{brush.Kind}`)").AppendLine();

        if (brush.Summary is not null) builder.AppendLine(brush.Summary).AppendLine();

        builder.AppendLine("## Defaults").AppendLine()
               .AppendLine($"Read off `new {brush.TypeName}()`. Every one of them is an `init` property of `BinkBrush`, set with an object initializer or `with`.")
               .AppendLine()
               .AppendLine($"- **Color** = `{brush.Color}`")
               .AppendLine($"- **Width** = `{Number(brush.Width)}` (document units, reached at full pressure)")
               .AppendLine($"- **Opacity** = `{Number(brush.Opacity)}`")
               .AppendLine($"- **BlendMode** = `BinkBlendMode.{brush.BlendMode}`")
               .AppendLine($"- **PressureCurve** = {brush.PressureCurve}")
               .AppendLine($"- **Smoothing** = `{Number(brush.Smoothing)}`")
               .AppendLine($"- **Spacing** = `{Number(brush.Spacing)}` (fraction of the width)")
               .AppendLine($"- **SimulatePressure** = `{(brush.SimulatePressure ? "true" : "false")}`")
               .AppendLine($"- **TaperStart** = `{Number(brush.TaperStart)}`")
               .AppendLine($"- **TaperEnd** = `{Number(brush.TaperEnd)}`")
               .AppendLine()
               .AppendLine("## Geometry").AppendLine()
               .AppendLine(brush.Geometry == nameof(BinkStrokeGeometryMode.Fill)
                   ? $"A filled, variable-width outline: the width follows the pressure curve, so a pen with pressure (or simulated pressure) draws thick and thin. Caps are {brush.LineCap.ToLowerInvariant()}."
                   : $"A constant-width centerline stroked at the brush width, which keeps the geometry small; pressure does not change the width. Caps are {brush.LineCap.ToLowerInvariant()}.")
               .AppendLine()
               .AppendLine("## Serialized").AppendLine()
               .AppendLine("How `BinkSerializer` writes this brush into a stroke. Properties at their format default (opacity 1, smoothing 0.5, ")
               .AppendLine("spacing 0.25, simulated pressure on, no taper, normal blend) are left out; a kind the reading registry does not ")
               .AppendLine("know is loaded as a pen with these properties.")
               .AppendLine()
               .AppendLine("```json")
               .AppendLine(brush.Serialized)
               .AppendLine("```")
               .AppendLine()
               .AppendLine("## Usage").AppendLine()
               .AppendLine("```razor")
               .AppendLine("<BinkCanvas Brush=\"_brush\" Height=\"400px\" />")
               .AppendLine()
               .AppendLine("@code {")
               .AppendLine($"    private BinkBrush _brush = new {brush.TypeName}();")
               .AppendLine()
               .AppendLine($"    private void Recolor(string css) => _brush = _brush with {{ Color = BinkColor.Parse(css) }};")
               .AppendLine("}")
               .AppendLine("```");

        return builder.ToString();
    }

    private static BinkBrushDto[] Build()
    {
        var brushes = new List<BinkBrushDto>();

        foreach (var kind in BinkBrushRegistry.Default.Kinds.Order(StringComparer.Ordinal))
        {
            if (BinkBrushRegistry.Default.TryCreate(kind, out var brush) is false || brush is null) continue;

            var geometry = SampleGeometry(brush);

            brushes.Add(new BinkBrushDto
            {
                Kind = brush.Kind,
                TypeName = brush.GetType().Name,
                Summary = BinkXmlDocs.GetSummary($"T:{brush.GetType().FullName}"),
                Color = brush.Color.ToCss(),
                Width = brush.Width,
                Opacity = brush.Opacity,
                BlendMode = brush.BlendMode.ToString(),
                PressureCurve = Describe(brush.PressureCurve),
                Smoothing = brush.Smoothing,
                Spacing = brush.Spacing,
                SimulatePressure = brush.SimulatePressure,
                TaperStart = brush.TaperStart,
                TaperEnd = brush.TaperEnd,
                Geometry = geometry.Mode.ToString(),
                LineCap = geometry.Cap.ToString(),
                Serialized = SerializedBrush(brush)
            });
        }

        return [.. brushes];
    }

    /// <summary>
    /// The geometry the brush builds for a short, gently curved stroke - long enough for an outline
    /// brush to produce an outline and for a centerline brush to produce a spline, rather than the
    /// dot either draws for a single tap.
    /// </summary>
    private static BinkStrokeGeometry SampleGeometry(BinkBrush brush)
    {
        var points = BinkStrokePoints.FromPositions([new Vector2(10, 40), new Vector2(40, 20), new Vector2(80, 30), new Vector2(120, 60)]);

        return brush.BuildGeometry(points.AsView());
    }

    /// <summary>
    /// The brush as a stroke in a document stores it: one stroke is serialized and its brush object is
    /// taken back out of the JSON, so the answer is the serializer's own output rather than a copy of it.
    /// </summary>
    private static string SerializedBrush(BinkBrush brush)
    {
        var document = new BinkDocument();
        document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(10, 10)]), brush));

        using var json = JsonDocument.Parse(BinkSerializer.Serialize(document));

        var element = json.RootElement.GetProperty("layers")[0].GetProperty("elements")[0].GetProperty("brush");

        return JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string Describe(BinkPressureCurve curve)
    {
        return curve.Kind == BinkPressureCurveKind.Constant
            ? "Constant (pressure ignored)"
            : $"{curve.Kind} {Number(curve.MinFactor)}..{Number(curve.MaxFactor)}";
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
