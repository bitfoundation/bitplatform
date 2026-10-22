using System.Drawing;
using System.Numerics;
using Bit.Bmage.Analysis;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Serialization;

/// <summary>
/// The codecs of every built-in operation, annotation, layer and mask. Each writes every property its type's
/// constructor and initialisers take, so reading it back gives an equal object.
/// </summary>
internal static class BuiltInCodecs
{
    public static IEnumerable<Codec> All => [.. Operations(), .. Masks(), .. Layers(), .. Annotations()];

    // ------------------------------------------------------------------ operations

    private static IEnumerable<Codec> Operations() =>
    [
        Op<AutoOrientOperation>("autoOrient", (_, _) => { }, _ => AutoOrientOperation.Instance),
        Op<CropOperation>("crop", (w, o) => w.Write("rectangle", o.Rectangle), r => new CropOperation(r.GetRectangle("rectangle"))),
        Op<AspectCropOperation>("cropToAspect",
            (w, o) => { WriteRatio(w, o.Ratio); w.WriteEnum("anchor", o.Anchor); },
            r => new AspectCropOperation(ReadRatio(r), r.GetEnum("anchor", Anchor.Center))),
        Op<SmartCropOperation>("smartCrop",
            (w, o) =>
            {
                WriteRatio(w, o.Ratio);
                w.WriteObject("options", s => s
                    .Write("analysisSize", o.Options.AnalysisSize).Write("detailWeight", o.Options.DetailWeight)
                    .Write("saturationWeight", o.Options.SaturationWeight).Write("skinWeight", o.Options.SkinWeight)
                    .Write("compositionWeight", o.Options.CompositionWeight).Write("edgePenalty", o.Options.EdgePenalty)
                    .Write("searchSteps", o.Options.SearchSteps));
            },
            r =>
            {
                var d = SmartCropOptions.Default;
                var o = r.GetObject("options");
                return new SmartCropOperation(ReadRatio(r), o is null ? d : new SmartCropOptions
                {
                    AnalysisSize = o.GetInt32("analysisSize", d.AnalysisSize),
                    DetailWeight = o.GetDouble("detailWeight", d.DetailWeight),
                    SaturationWeight = o.GetDouble("saturationWeight", d.SaturationWeight),
                    SkinWeight = o.GetDouble("skinWeight", d.SkinWeight),
                    CompositionWeight = o.GetDouble("compositionWeight", d.CompositionWeight),
                    EdgePenalty = o.GetDouble("edgePenalty", d.EdgePenalty),
                    SearchSteps = o.GetInt32("searchSteps", d.SearchSteps),
                });
            }),
        Op<ResizeOperation>("resize", WriteResize, ReadResize),
        Op<ResizeCropOperation>("resizeCrop",
            (w, o) => { w.WriteObject("resize", s => WriteResize(s, o.Resize)); w.Write("crop", o.Crop); },
            r => new ResizeCropOperation(ReadResize(r.GetObject("resize") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"resize\".")), r.GetRectangle("crop"))),
        Op<RotateOperation>("rotate",
            (w, o) => w.Write("degrees", o.Degrees).Write("background", o.Background).Write("expandCanvas", o.ExpandCanvas).WriteEnum("sampling", o.Sampling),
            r => new RotateOperation(r.GetDouble("degrees"), r.GetColor("background", default), r.GetBoolean("expandCanvas", true), r.GetEnum("sampling", WarpSampling.Bicubic))),
        Op<OrientationOperation>("orient", (w, o) => w.WriteEnum("orientation", o.Orientation), r => new OrientationOperation(r.GetEnum("orientation", Orientation.Normal))),
        Op<SkewOperation>("skew",
            (w, o) => w.Write("degreesX", o.DegreesX).Write("degreesY", o.DegreesY).Write("background", o.Background).WriteEnum("sampling", o.Sampling),
            r => new SkewOperation(r.GetDouble("degreesX", 0), r.GetDouble("degreesY", 0), r.GetColor("background", default), r.GetEnum("sampling", WarpSampling.Bilinear))),
        Op<AffineTransformOperation>("affine",
            (w, o) =>
            {
                var m = o.Matrix;
                w.Write("matrix", [m.M11, m.M12, m.M21, m.M22, m.M31, m.M32]);
                w.Write("outputSize", o.OutputSize).Write("background", o.Background).WriteEnum("sampling", o.Sampling).Write("name", o.Name);
            },
            r =>
            {
                var m = r.GetSingles("matrix") is { Length: 6 } v ? v : throw new ImageDocumentFormatException($"{r.Path}.matrix should hold six numbers.");
                return new AffineTransformOperation(new Matrix3x2(m[0], m[1], m[2], m[3], m[4], m[5]), r.GetNullableSize("outputSize"),
                    r.GetColor("background", default), r.GetEnum("sampling", WarpSampling.Bilinear), r.GetString("name", null));
            }),
        Op<PerspectiveTransformOperation>("perspective",
            (w, o) =>
            {
                w.Write("corners", [.. o.DestinationCorners.Select(c => new Vector2(c.X, c.Y))]);
                w.Write("outputSize", o.OutputSize).Write("background", o.Background).WriteEnum("sampling", o.Sampling).Write("straighten", o.IsStraightening);
            },
            r =>
            {
                var corners = r.GetPoints("corners").Select(p => new PointF(p.X, p.Y)).ToArray();
                var size = r.GetNullableSize("outputSize");
                var background = r.GetColor("background", default);
                var sampling = r.GetEnum("sampling", WarpSampling.Bilinear);
                if (!r.GetBoolean("straighten")) return new PerspectiveTransformOperation(corners, size, background, sampling);
                return PerspectiveTransformOperation.Straighten(corners, size ?? throw new ImageDocumentFormatException($"{r.Path} straightens, so it needs an \"outputSize\"."), background, sampling);
            }),

        Amount<BrightnessAdjustment>("brightness", o => o.Amount, v => new BrightnessAdjustment(v)),
        Amount<ContrastAdjustment>("contrast", o => o.Amount, v => new ContrastAdjustment(v)),
        Op<ExposureAdjustment>("exposure", (w, o) => w.Write("ev", o.Ev), r => new ExposureAdjustment(r.GetSingle("ev"))),
        Amount<SaturationAdjustment>("saturation", o => o.Amount, v => new SaturationAdjustment(v)),
        Op<HueAdjustment>("hue", (w, o) => w.Write("degrees", o.Degrees), r => new HueAdjustment(r.GetSingle("degrees"))),
        Amount<TemperatureAdjustment>("temperature", o => o.Amount, v => new TemperatureAdjustment(v)),
        Amount<TintAdjustment>("tint", o => o.Amount, v => new TintAdjustment(v)),
        Op<OpacityAdjustment>("opacity", (w, o) => w.Write("opacity", o.Opacity), r => new OpacityAdjustment(r.GetSingle("opacity"))),
        Amount<GrayscaleFilter>("grayscale", o => o.Amount, v => new GrayscaleFilter(v)),
        Amount<SepiaFilter>("sepia", o => o.Amount, v => new SepiaFilter(v)),
        Amount<InvertFilter>("invert", o => o.Amount, v => new InvertFilter(v)),
        Amount<VibranceAdjustment>("vibrance", o => o.Amount, v => new VibranceAdjustment(v)),
        Op<GammaAdjustment>("gamma", (w, o) => w.Write("gamma", o.Gamma), r => new GammaAdjustment(r.GetSingle("gamma"))),
        Op<LevelsAdjustment>("levels",
            (w, o) => w.Write("inputBlack", o.InputBlack).Write("inputWhite", o.InputWhite).Write("gamma", o.Gamma)
                .Write("outputBlack", o.OutputBlack).Write("outputWhite", o.OutputWhite).WriteEnum("channels", o.Channels),
            r => new LevelsAdjustment(r.GetSingle("inputBlack", 0), r.GetSingle("inputWhite", 1), r.GetSingle("gamma", 1),
                r.GetSingle("outputBlack", 0), r.GetSingle("outputWhite", 1), r.GetEnum("channels", ColorChannels.Rgb))),
        Op<PosterizeFilter>("posterize", (w, o) => w.Write("levels", o.Levels), r => new PosterizeFilter(r.GetInt32("levels"))),
        Op<ThresholdFilter>("threshold", (w, o) => w.Write("level", o.Level), r => new ThresholdFilter(r.GetSingle("level", 0.5f))),
        Op<ShadowsHighlightsAdjustment>("shadowsHighlights",
            (w, o) => w.Write("shadows", o.Shadows).Write("highlights", o.Highlights),
            r => new ShadowsHighlightsAdjustment(r.GetSingle("shadows", 0), r.GetSingle("highlights", 0))),
        Op<ColorMatrixOperation>("colorMatrix",
            (w, o) => w.Write("matrix", o.Matrix.ToArray()).Write("name", o.Name),
            r => new ColorMatrixOperation(new ColorMatrix(r.GetSingles("matrix") is { Length: 20 } m ? m : throw new ImageDocumentFormatException($"{r.Path}.matrix should hold twenty numbers.")), r.GetString("name", null))),
        Op<CurveOperation>("curve",
            (w, o) => w.Write("red", o.Red).Write("green", o.Green).Write("blue", o.Blue).Write("alpha", o.Alpha).Write("name", o.Name),
            r => new CurveOperation(r.GetSingles("red"), r.GetSingles("green"), r.GetSingles("blue"), r.GetSingles("alpha"), r.GetString("name", null))),
        Op<FusedPointOperation>("fused",
            (w, o) => w.Write("stages", ImagePipeline.From(o.Stages)),
            r => new FusedPointOperation([.. r.GetPipeline("stages").Select(s => s as PointOperation ?? throw new ImageDocumentFormatException($"{r.Path}.stages may only hold per-pixel operations, not '{s.Name}'."))])),
        Op<AutoLevelsOperation>("autoLevels",
            (w, o) => w.Write("clipFraction", o.ClipFraction).Write("perChannel", o.PerChannel).Write("strength", o.Strength),
            r => new AutoLevelsOperation(r.GetDouble("clipFraction", 0.005), r.GetBoolean("perChannel"), r.GetSingle("strength", 1))),
        Op<AutoContrastOperation>("autoContrast",
            (w, o) => w.Write("targetMean", o.TargetMean).Write("strength", o.Strength),
            r => new AutoContrastOperation(r.GetSingle("targetMean", 0.5f), r.GetSingle("strength", 0.7f))),

        Op<BlurFilter>("blur", (w, o) => w.Write("radius", o.Radius).Write("region", o.Region), r => new BlurFilter(r.GetSingle("radius"), r.GetNullableRectangle("region"))),
        Op<SharpenFilter>("sharpen",
            (w, o) => w.Write("amount", o.Amount).Write("radius", o.Radius).Write("threshold", o.Threshold).Write("region", o.Region),
            r => new SharpenFilter(r.GetSingle("amount", 0.5f), r.GetSingle("radius", 1), r.GetSingle("threshold", 0), r.GetNullableRectangle("region"))),
        Op<PixelateFilter>("pixelate", (w, o) => w.Write("blockSize", o.BlockSize).Write("region", o.Region), r => new PixelateFilter(r.GetInt32("blockSize"), r.GetNullableRectangle("region"))),
        Op<NoiseFilter>("noise",
            (w, o) => w.Write("amount", o.Amount).Write("monochrome", o.Monochrome).Write("seed", o.Seed).Write("region", o.Region),
            r => new NoiseFilter(r.GetSingle("amount"), r.GetBoolean("monochrome"), r.GetInt32("seed", 0), r.GetNullableRectangle("region"))),
        Op<MedianFilter>("median", (w, o) => w.Write("radius", o.Radius).Write("region", o.Region), r => new MedianFilter(r.GetInt32("radius", 1), r.GetNullableRectangle("region"))),
        Op<ConvolutionFilter>("convolution",
            (w, o) => w.Write("kernel", o.Kernel).Write("kernelWidth", o.KernelWidth).Write("kernelHeight", o.KernelHeight)
                .Write("divisor", o.Divisor).Write("bias", o.Bias).Write("region", o.Region).Write("name", o.Name),
            r => new ConvolutionFilter(r.GetSingles("kernel") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"kernel\"."),
                r.GetInt32("kernelWidth"), r.GetInt32("kernelHeight"), r.GetSingle("divisor", 1), r.GetSingle("bias", 0), r.GetNullableRectangle("region"), r.GetString("name", null))),
        Op<VignetteFilter>("vignette",
            (w, o) => w.Write("amount", o.Amount).Write("radius", o.Radius).Write("softness", o.Softness).Write("color", o.Color),
            r => new VignetteFilter(r.GetSingle("amount", 0.5f), r.GetSingle("radius", 0.75f), r.GetSingle("softness", 0.5f), r.GetColor("color", Rgba32.Black))),

        Op<MaskedOperation>("masked",
            (w, o) => w.Write("operation", o.Operation).Write("mask", o.Mask),
            r => new MaskedOperation(r.GetOperation("operation") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"operation\"."), RequireMask(r))),
        Op<AlphaMaskOperation>("applyMask", (w, o) => w.Write("mask", o.Mask), r => new AlphaMaskOperation(RequireMask(r))),
        Op<PipelineOperation>("pipeline",
            (w, o) => { w.Write("operations", o.Pipeline); if (o.Name != "Pipeline") w.Write("name", o.Name); },
            r => new PipelineOperation(r.GetPipeline("operations"), r.GetString("name", null))),
    ];

    private static Codec Op<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : IImageOperation
        => new(typeof(IImageOperation), typeof(T), name, (w, value) => write(w, (T)value), r => read(r));

    private static Codec Amount<T>(string name, Func<T, float> amount, Func<float, T> create) where T : IImageOperation
        => Op<T>(name, (w, o) => w.Write("amount", amount(o)), r => create(r.GetSingle("amount")));

    private static void WriteResize(BmageJsonWriter w, ResizeOperation o)
        => w.Write("width", o.Width).Write("height", o.Height).WriteEnum("mode", o.Mode).WriteEnum("filter", o.Filter)
            .WriteEnum("anchor", o.Anchor).Write("background", o.Background);

    private static ResizeOperation ReadResize(BmageJsonReader r)
        => new(r.GetNullableInt32("width"), r.GetNullableInt32("height"), r.GetEnum("mode", ResizeMode.Fit), r.GetEnum("filter", ResamplingFilter.Auto),
            r.GetEnum("anchor", Anchor.Center), r.GetColor("background", default));

    private static void WriteRatio(BmageJsonWriter w, AspectRatio ratio)
    {
        w.Json.WriteStartArray("ratio");
        w.WriteNumberValue(ratio.Width);
        w.WriteNumberValue(ratio.Height);
        w.Json.WriteEndArray();
    }

    private static AspectRatio ReadRatio(BmageJsonReader r)
    {
        if (!r.Element.TryGetProperty("ratio", out var value) || value.ValueKind != System.Text.Json.JsonValueKind.Array || value.GetArrayLength() != 2)
            throw new ImageDocumentFormatException($"{r.Path}.ratio should be [width, height].");
        return new AspectRatio(BmageJsonReader.ReadDouble(value[0]), BmageJsonReader.ReadDouble(value[1]));
    }

    private static Mask RequireMask(BmageJsonReader r) => r.GetMask("mask") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"mask\".");

    // ------------------------------------------------------------------ masks

    private static IEnumerable<Codec> Masks() =>
    [
        MaskOf<RectangleMask>("rectangle", (w, m) => w.Write("rectangle", m.Rectangle).Write("cornerRadius", m.CornerRadius),
            r => new RectangleMask(r.GetRectangleF("rectangle")) { CornerRadius = r.GetSingle("cornerRadius", 0) }),
        MaskOf<EllipseMask>("ellipse", (w, m) => w.Write("rectangle", m.Rectangle), r => new EllipseMask(r.GetRectangleF("rectangle"))),
        MaskOf<PolygonMask>("polygon", (w, m) => w.Write("points", m.Points).WriteEnum("rule", m.Rule),
            r => new PolygonMask(r.GetPoints("points")) { Rule = r.GetEnum("rule", FillRule.NonZero) }),
        MaskOf<PathMask>("path",
            (w, m) =>
            {
                w.Json.WriteStartArray("contours");
                foreach (var contour in m.Path.Contours)
                {
                    w.Json.WriteStartArray();
                    foreach (var p in contour)
                    {
                        w.WriteNumberValue(p.X);
                        w.WriteNumberValue(p.Y);
                    }
                    w.Json.WriteEndArray();
                }
                w.Json.WriteEndArray();
                w.Json.WriteStartArray("closed");
                foreach (var closed in m.Path.Closed) w.Json.WriteBooleanValue(closed);
                w.Json.WriteEndArray();
                w.WriteEnum("rule", m.Rule);
            },
            r => new PathMask(ReadPath(r)) { Rule = r.GetEnum("rule", FillRule.NonZero) }),
        MaskOf<BrushMask>("brush",
            (w, m) => w.WriteObjects("strokes", m.Strokes, (s, stroke) => s.Write("points", stroke.Points).Write("width", stroke.Width).Write("opacity", stroke.Opacity).Write("erase", stroke.Erase)),
            r => new BrushMask([.. r.GetObjects("strokes").Select(s => new BrushStroke(s.GetPoints("points"), s.GetSingle("width"))
            {
                Opacity = s.GetSingle("opacity", 1),
                Erase = s.GetBoolean("erase"),
            })])),
        MaskOf<LinearGradientMask>("linearGradient", (w, m) => w.Write("start", m.Start).Write("end", m.End), r => new LinearGradientMask(r.GetVector2("start"), r.GetVector2("end"))),
        MaskOf<RadialGradientMask>("radialGradient",
            (w, m) => w.Write("center", m.Center).Write("radiusX", m.RadiusX).Write("radiusY", m.RadiusY).Write("hardness", m.Hardness).Write("rotation", m.Rotation),
            r => new RadialGradientMask(r.GetVector2("center"), r.GetSingle("radiusX"), r.GetSingle("radiusY"))
            {
                Hardness = r.GetSingle("hardness", 0.5f),
                Rotation = r.GetSingle("rotation", 0),
            }),
        MaskOf<LuminanceRangeMask>("luminanceRange", (w, m) => w.Write("low", m.Low).Write("high", m.High).Write("softness", m.Softness),
            r => new LuminanceRangeMask(r.GetSingle("low"), r.GetSingle("high")) { Softness = r.GetSingle("softness", 0.1f) }),
        MaskOf<ColorRangeMask>("colorRange", (w, m) => w.Write("color", m.Color).Write("tolerance", m.Tolerance).Write("softness", m.Softness),
            r => new ColorRangeMask(r.GetColor("color")) { Tolerance = r.GetSingle("tolerance", 0.15f), Softness = r.GetSingle("softness", 0.1f) }),
        MaskOf<BitmapMask>("bitmap", (w, m) => w.WriteImage("image", m.Image).WriteEnum("channel", m.Channel),
            r => new BitmapMask(r.GetImage("image")) { Channel = r.GetEnum("channel", MaskChannel.Alpha) }),
        MaskOf<CombinedMask>("combined", (w, m) => w.Write("first", m.First).Write("second", m.Second).WriteEnum("mode", m.Mode), ReadCombined),
    ];

    /// <summary>
    /// A combination, refused when drawing it would hold more masks at once than <see cref="Mask.MaxMasksHeld"/>: each is
    /// the size of the image, so a document nesting a hundred of them, as only a hostile one would, could otherwise take
    /// gigabytes from a server that replays it.
    /// </summary>
    private static CombinedMask ReadCombined(BmageJsonReader r)
    {
        var mask = new CombinedMask(r.GetMask("first") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"first\"."),
            r.GetMask("second") ?? throw new ImageDocumentFormatException($"{r.Path} has no \"second\"."), r.GetEnum("mode", MaskCombineMode.Union));
        // The combination's own feather is set once this returns, and holds a working copy of its own.
        var feathered = r.GetSingle("feather", 0) >= 0.05f ? 1 : 0;
        if (mask.MasksHeld() + feathered > Mask.MaxMasksHeld)
            throw new ImageDocumentFormatException($"{r.Path} nests its masks too deeply: drawing it would hold more than {Mask.MaxMasksHeld} masks at once. Combine them a step at a time, a.Union(b).Union(c), rather than inside one another.");
        return mask;
    }

    private static Codec MaskOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : Mask
        => new(typeof(Mask), typeof(T), name,
            (w, value) =>
            {
                var mask = (Mask)value;
                if (mask.Inverted) w.Write("inverted", true);
                if (mask.Feather != 0) w.Write("feather", mask.Feather);
                if (mask.Opacity != 1) w.Write("opacity", mask.Opacity);
                if (mask.Units != MaskUnits.Pixels) w.WriteEnum("units", mask.Units);
                write(w, (T)value);
            },
            r => read(r) with
            {
                Inverted = r.GetBoolean("inverted"),
                Feather = r.GetSingle("feather", 0),
                Opacity = r.GetSingle("opacity", 1),
                Units = r.GetEnum("units", MaskUnits.Pixels),
            });

    private static VectorPath ReadPath(BmageJsonReader r)
    {
        var contours = new List<Vector2[]>();
        if (r.Element.TryGetProperty("contours", out var array) && array.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var contour in array.EnumerateArray())
            {
                if (contour.ValueKind != System.Text.Json.JsonValueKind.Array || contour.GetArrayLength() % 2 != 0)
                    throw new ImageDocumentFormatException($"{r.Path}.contours should hold arrays of x, y pairs.");
                var points = new Vector2[contour.GetArrayLength() / 2];
                for (var i = 0; i < points.Length; i++) points[i] = new Vector2(BmageJsonReader.ReadSingle(contour[2 * i]), BmageJsonReader.ReadSingle(contour[2 * i + 1]));
                contours.Add(points);
            }
        }
        var closed = new bool[contours.Count];
        if (r.Element.TryGetProperty("closed", out var flags) && flags.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var i = 0;
            foreach (var flag in flags.EnumerateArray())
                if (i < closed.Length) closed[i++] = flag.ValueKind == System.Text.Json.JsonValueKind.True;
        }
        return new VectorPath(contours, closed);
    }

    // ------------------------------------------------------------------ layers

    private static IEnumerable<Codec> Layers() =>
    [
        LayerOf<ImageLayer>("image",
            (w, l) => w.WriteImage("image", l.Image).Write("bounds", l.Bounds).Write("rotation", l.Rotation).Write("pipeline", l.Pipeline).WriteEnum("filter", l.Filter),
            r => new ImageLayer
            {
                Image = r.GetImage("image"),
                Bounds = r.GetRectangleF("bounds"),
                Rotation = r.GetSingle("rotation", 0),
                Pipeline = r.GetPipeline("pipeline"),
                Filter = r.GetEnum("filter", ResamplingFilter.Auto),
            }),
        LayerOf<FillLayer>("fill", (w, l) => w.Write("color", l.Color), r => new FillLayer(r.GetColor("color"))),
        LayerOf<AdjustmentLayer>("adjustment", (w, l) => w.Write("pipeline", l.Pipeline), r => new AdjustmentLayer(r.GetPipeline("pipeline"))),
    ];

    private static Codec LayerOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : Layer
        => new(typeof(Layer), typeof(T), name,
            (w, value) =>
            {
                var layer = (Layer)value;
                w.Write("id", layer.Id);
                if (layer.Name is not null) w.Write("name", layer.Name);
                if (layer.BlendMode != BlendMode.SourceOver) w.WriteEnum("blendMode", layer.BlendMode);
                if (layer.Opacity != 1) w.Write("opacity", layer.Opacity);
                if (layer.Mask is not null) w.Write("mask", layer.Mask);
                if (!layer.IsVisible) w.Write("isVisible", false);
                if (layer.IsLocked) w.Write("isLocked", true);
                write(w, (T)value);
            },
            r =>
            {
                var layer = read(r);
                return layer with
                {
                    Id = r.GetGuid("id", layer.Id),
                    Name = r.GetString("name", null),
                    BlendMode = r.GetEnum("blendMode", BlendMode.SourceOver),
                    Opacity = r.GetSingle("opacity", 1),
                    Mask = r.GetMask("mask"),
                    IsVisible = r.GetBoolean("isVisible", true),
                    IsLocked = r.GetBoolean("isLocked"),
                };
            });

    // ------------------------------------------------------------------ annotations

    private static IEnumerable<Codec> Annotations() =>
    [
        AnnotationOf<RectangleAnnotation>("rectangle",
            (w, a) => { w.Write("rectangle", a.Rectangle).Write("cornerRadius", a.CornerRadius); WriteShape(w, a.Style); },
            r => new RectangleAnnotation { Rectangle = r.GetRectangleF("rectangle"), CornerRadius = r.GetSingle("cornerRadius", 0), Style = ReadShape(r, ShapeStyle.Default) }),
        AnnotationOf<EllipseAnnotation>("ellipse",
            (w, a) => { w.Write("rectangle", a.Rectangle); WriteShape(w, a.Style); },
            r => new EllipseAnnotation { Rectangle = r.GetRectangleF("rectangle"), Style = ReadShape(r, ShapeStyle.Default) }),
        AnnotationOf<LineAnnotation>("line",
            (w, a) => { w.Write("start", a.Start).Write("end", a.End); WriteShape(w, a.Style); },
            r => new LineAnnotation { Start = r.GetVector2("start"), End = r.GetVector2("end"), Style = ReadShape(r, ShapeStyle.Default) }),
        AnnotationOf<ArrowAnnotation>("arrow",
            (w, a) => { w.Write("start", a.Start).Write("end", a.End).Write("headLengthFactor", a.HeadLengthFactor).Write("headWidthFactor", a.HeadWidthFactor); WriteShape(w, a.Style); },
            r => new ArrowAnnotation
            {
                Start = r.GetVector2("start"),
                End = r.GetVector2("end"),
                HeadLengthFactor = r.GetSingle("headLengthFactor", 4),
                HeadWidthFactor = r.GetSingle("headWidthFactor", 3),
                Style = ReadShape(r, ShapeStyle.Default),
            }),
        AnnotationOf<FreehandAnnotation>("freehand",
            (w, a) => { w.Write("points", a.Points).Write("closed", a.Closed); WriteShape(w, a.Style); },
            r =>
            {
                var plain = new FreehandAnnotation { Points = r.GetPoints("points") };
                return plain with { Closed = r.GetBoolean("closed"), Style = ReadShape(r, plain.Style) };
            }),
        AnnotationOf<PolygonAnnotation>("polygon",
            (w, a) => { w.Write("points", a.Points).Write("closed", a.Closed); WriteShape(w, a.Style); },
            r => new PolygonAnnotation { Points = r.GetPoints("points"), Closed = r.GetBoolean("closed", true), Style = ReadShape(r, ShapeStyle.Default) }),
        AnnotationOf<TextAnnotation>("text",
            (w, a) =>
            {
                w.Write("text", a.Text).Write("position", a.Position).Write("backgroundColor", a.BackgroundColor)
                    .Write("backgroundPadding", a.BackgroundPadding).Write("backgroundCornerRadius", a.BackgroundCornerRadius);
                if (a.MeasuredSize is { } size) w.Write("measuredSize", size);
                WriteText(w, "style", a.Style);
            },
            r => new TextAnnotation
            {
                Text = r.GetString("text"),
                Position = r.GetVector2("position"),
                BackgroundColor = r.GetNullableColor("backgroundColor"),
                BackgroundPadding = r.GetSingle("backgroundPadding", 6),
                BackgroundCornerRadius = r.GetSingle("backgroundCornerRadius", 4),
                MeasuredSize = r.GetNullableSizeF("measuredSize"),
                Style = ReadText(r, "style", new TextStyle()),
            }),
        AnnotationOf<HighlightAnnotation>("highlight",
            (w, a) => w.Write("rectangle", a.Rectangle).Write("color", a.Color),
            r => new HighlightAnnotation { Rectangle = r.GetRectangleF("rectangle"), Color = r.GetColor("color", new Rgba32(255, 235, 59, 110)) }),
        AnnotationOf<RedactionAnnotation>("redaction",
            (w, a) => w.Write("rectangle", a.Rectangle).WriteEnum("mode", a.Mode).Write("color", a.Color).Write("strength", a.Strength),
            r => new RedactionAnnotation
            {
                Rectangle = r.GetRectangleF("rectangle"),
                Mode = r.GetEnum("mode", RedactionMode.Solid),
                Color = r.GetColor("color", Rgba32.Black),
                Strength = r.GetSingle("strength", 12),
            }),
        AnnotationOf<CalloutAnnotation>("callout",
            (w, a) =>
            {
                w.Write("bubble", a.Bubble).Write("target", a.Target).Write("text", a.Text).Write("cornerRadius", a.CornerRadius);
                WriteText(w, "textStyle", a.TextStyle);
                WriteShape(w, a.Style);
            },
            r =>
            {
                var plain = new CalloutAnnotation { Bubble = r.GetRectangleF("bubble"), Target = r.GetVector2("target") };
                return plain with
                {
                    Text = r.GetString("text", string.Empty) ?? string.Empty,
                    CornerRadius = r.GetSingle("cornerRadius", 6),
                    TextStyle = ReadText(r, "textStyle", plain.TextStyle),
                    Style = ReadShape(r, plain.Style),
                };
            }),
        AnnotationOf<StepAnnotation>("step",
            (w, a) =>
            {
                w.Write("center", a.Center).Write("number", a.Number).Write("radius", a.Radius)
                    .Write("backgroundColor", a.BackgroundColor).Write("foregroundColor", a.ForegroundColor);
                WriteText(w, "textStyle", a.TextStyle);
            },
            r =>
            {
                var plain = new StepAnnotation { Center = r.GetVector2("center"), Number = r.GetInt32("number") };
                return plain with
                {
                    Radius = r.GetSingle("radius", 18),
                    BackgroundColor = r.GetColor("backgroundColor", plain.BackgroundColor),
                    ForegroundColor = r.GetColor("foregroundColor", plain.ForegroundColor),
                    TextStyle = ReadText(r, "textStyle", plain.TextStyle),
                };
            }),
        AnnotationOf<ImageAnnotation>("image",
            (w, a) => w.WriteImage("image", a.Image).Write("rectangle", a.Rectangle),
            r => new ImageAnnotation { Image = r.GetImage("image"), Rectangle = r.GetRectangleF("rectangle") }),
    ];

    private static Codec AnnotationOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : Annotation
        => new(typeof(Annotation), typeof(T), name,
            (w, value) =>
            {
                var annotation = (Annotation)value;
                w.Write("id", annotation.Id);
                if (annotation.ZIndex != 0) w.Write("zIndex", annotation.ZIndex);
                if (annotation.Rotation != 0) w.Write("rotation", annotation.Rotation);
                if (annotation.Opacity != 1) w.Write("opacity", annotation.Opacity);
                if (!annotation.IsVisible) w.Write("isVisible", false);
                if (annotation.IsLocked) w.Write("isLocked", true);
                if (annotation.Tag is not null) w.Write("tag", annotation.Tag);
                write(w, (T)value);
            },
            r =>
            {
                var annotation = read(r);
                return annotation with
                {
                    Id = r.GetGuid("id", annotation.Id),
                    ZIndex = r.GetInt32("zIndex", 0),
                    Rotation = r.GetSingle("rotation", 0),
                    Opacity = r.GetSingle("opacity", 1),
                    IsVisible = r.GetBoolean("isVisible", true),
                    IsLocked = r.GetBoolean("isLocked"),
                    Tag = r.GetString("tag", null),
                };
            });

    private static void WriteStroke(BmageJsonWriter w, string name, StrokeStyle stroke)
        => w.WriteObject(name, s =>
        {
            s.Write("width", stroke.Width).WriteEnum("cap", stroke.Cap).WriteEnum("join", stroke.Join).Write("miterLimit", stroke.MiterLimit);
            if (stroke.DashPattern is { } dashes) s.Write("dashPattern", dashes).Write("dashOffset", stroke.DashOffset);
        });

    private static StrokeStyle ReadStroke(BmageJsonReader r, string name, StrokeStyle fallback)
    {
        if (r.GetObject(name) is not { } s) return fallback;
        return new StrokeStyle
        {
            Width = s.GetSingle("width", fallback.Width),
            Cap = s.GetEnum("cap", fallback.Cap),
            Join = s.GetEnum("join", fallback.Join),
            MiterLimit = s.GetSingle("miterLimit", fallback.MiterLimit),
            DashPattern = s.GetSingles("dashPattern"),
            DashOffset = s.GetSingle("dashOffset", 0),
        };
    }

    private static void WriteShape(BmageJsonWriter w, ShapeStyle style)
        => w.WriteObject("style", s =>
        {
            s.Write("strokeColor", style.StrokeColor).Write("fillColor", style.FillColor);
            WriteStroke(s, "stroke", style.Stroke);
        });

    private static ShapeStyle ReadShape(BmageJsonReader r, ShapeStyle fallback)
    {
        if (r.GetObject("style") is not { } s) return fallback;
        return new ShapeStyle
        {
            StrokeColor = s.GetNullableColor("strokeColor", fallback.StrokeColor),
            FillColor = s.GetNullableColor("fillColor", fallback.FillColor),
            Stroke = ReadStroke(s, "stroke", fallback.Stroke),
        };
    }

    private static void WriteText(BmageJsonWriter w, string name, TextStyle style)
        => w.WriteObject(name, s =>
        {
            s.Write("fontFamily", style.FontFamily).Write("fontSize", style.FontSize).Write("bold", style.Bold).Write("italic", style.Italic)
                .Write("fillColor", style.FillColor).Write("strokeColor", style.StrokeColor)
                .WriteEnum("alignment", style.Alignment).WriteEnum("baseline", style.Baseline)
                .Write("lineHeight", style.LineHeight).Write("letterSpacing", style.LetterSpacing).Write("maxWidth", style.MaxWidth);
            WriteStroke(s, "stroke", style.Stroke);
        });

    private static TextStyle ReadText(BmageJsonReader r, string name, TextStyle fallback)
    {
        if (r.GetObject(name) is not { } s) return fallback;
        return new TextStyle
        {
            FontFamily = s.GetString("fontFamily", fallback.FontFamily) ?? fallback.FontFamily,
            FontSize = s.GetSingle("fontSize", fallback.FontSize),
            Bold = s.GetBoolean("bold", fallback.Bold),
            Italic = s.GetBoolean("italic", fallback.Italic),
            FillColor = s.GetNullableColor("fillColor", fallback.FillColor),
            StrokeColor = s.GetNullableColor("strokeColor", fallback.StrokeColor),
            Alignment = s.GetEnum("alignment", fallback.Alignment),
            Baseline = s.GetEnum("baseline", fallback.Baseline),
            LineHeight = s.GetSingle("lineHeight", fallback.LineHeight),
            LetterSpacing = s.GetSingle("letterSpacing", fallback.LetterSpacing),
            MaxWidth = s.GetNullableSingle("maxWidth"),
            Stroke = ReadStroke(s, "stroke", fallback.Stroke),
        };
    }
}
