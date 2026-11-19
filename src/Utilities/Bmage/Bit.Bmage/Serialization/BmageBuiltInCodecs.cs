using System.Buffers.Binary;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// The codecs of every built-in operation, annotation, layer and mask. Each writes every property its type's
/// constructor and initialisers take, so reading it back gives an equal object.
/// </summary>
internal static class BmageBuiltInCodecs
{
    public static IEnumerable<BmageCodec> All => [.. Operations(), .. Masks(), .. Layers(), .. Annotations()];

    // ------------------------------------------------------------------ operations

    private static IEnumerable<BmageCodec> Operations() =>
    [
        Op<BmageAutoOrientOperation>("autoOrient", (_, _) => { }, _ => BmageAutoOrientOperation.Instance),
        Op<BmageCropOperation>("crop", (w, o) => w.Write("rectangle", o.Rectangle), r => new BmageCropOperation(r.GetRectangle("rectangle"))),
        Op<BmageAspectCropOperation>("cropToAspect",
            (w, o) => { WriteRatio(w, o.Ratio); w.WriteEnum("anchor", o.Anchor); },
            r => new BmageAspectCropOperation(ReadRatio(r), r.GetEnum("anchor", BmageAnchor.Center))),
        Op<BmageSmartCropOperation>("smartCrop",
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
                var d = BmageSmartCropOptions.Default;
                var o = r.GetObject("options");
                return new BmageSmartCropOperation(ReadRatio(r), o is null ? d : new BmageSmartCropOptions
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
        Op<BmageResizeOperation>("resize", WriteResize, ReadResize),
        Op<BmageResizeCropOperation>("resizeCrop",
            (w, o) => { w.WriteObject("resize", s => WriteResize(s, o.Resize)); w.Write("crop", o.Crop); },
            r => new BmageResizeCropOperation(ReadResize(r.GetObject("resize") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"resize\".")), r.GetRectangle("crop"))),
        Op<BmageRotateOperation>("rotate",
            (w, o) => w.Write("degrees", o.Degrees).Write("background", o.Background).Write("expandCanvas", o.ExpandCanvas).WriteEnum("sampling", o.Sampling),
            r => new BmageRotateOperation(r.GetDouble("degrees"), r.GetColor("background", default), r.GetBoolean("expandCanvas", true), r.GetEnum("sampling", BmageWarpSampling.Bicubic))),
        Op<BmageOrientationOperation>("orient", (w, o) => w.WriteEnum("orientation", o.Orientation), r => new BmageOrientationOperation(r.GetEnum("orientation", BmageOrientation.Normal))),
        Op<BmageStraightenOperation>("straighten",
            (w, o) => w.Write("degrees", o.Degrees).WriteEnum("sampling", o.Sampling),
            r => new BmageStraightenOperation(r.GetDouble("degrees"), r.GetEnum("sampling", BmageWarpSampling.Bicubic))),
        Op<BmageCanvasSizeOperation>("canvasSize",
            (w, o) =>
            {
                if (o.Size is { } size) w.Write("size", size).WriteEnum("anchor", o.Anchor);
                else w.Write("left", o.Left).Write("top", o.Top).Write("right", o.Right).Write("bottom", o.Bottom);
                w.Write("background", o.Background);
            },
            r =>
            {
                var background = r.GetColor("background", default);
                if (r.GetNullableSize("size") is { } size) return new BmageCanvasSizeOperation(size.Width, size.Height, r.GetEnum("anchor", BmageAnchor.Center), background);
                return BmageCanvasSizeOperation.Margins(r.GetInt32("left", 0), r.GetInt32("top", 0), r.GetInt32("right", 0), r.GetInt32("bottom", 0), background);
            }),
        Op<BmageSkewOperation>("skew",
            (w, o) => w.Write("degreesX", o.DegreesX).Write("degreesY", o.DegreesY).Write("background", o.Background).WriteEnum("sampling", o.Sampling),
            r => new BmageSkewOperation(r.GetDouble("degreesX", 0), r.GetDouble("degreesY", 0), r.GetColor("background", default), r.GetEnum("sampling", BmageWarpSampling.Bilinear))),
        Op<BmageAffineTransformOperation>("affine",
            (w, o) =>
            {
                var m = o.Matrix;
                w.Write("matrix", [m.M11, m.M12, m.M21, m.M22, m.M31, m.M32]);
                w.Write("outputSize", o.OutputSize).Write("background", o.Background).WriteEnum("sampling", o.Sampling).Write("name", o.Name);
            },
            r =>
            {
                var m = r.GetSingles("matrix") is { Length: 6 } v ? v : throw new BmageImageDocumentFormatException($"{r.Path}.matrix should hold six numbers.");
                return new BmageAffineTransformOperation(new Matrix3x2(m[0], m[1], m[2], m[3], m[4], m[5]), r.GetNullableSize("outputSize"),
                    r.GetColor("background", default), r.GetEnum("sampling", BmageWarpSampling.Bilinear), r.GetString("name", null));
            }),
        Op<BmagePerspectiveTransformOperation>("perspective",
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
                var sampling = r.GetEnum("sampling", BmageWarpSampling.Bilinear);
                if (!r.GetBoolean("straighten")) return new BmagePerspectiveTransformOperation(corners, size, background, sampling);
                return BmagePerspectiveTransformOperation.Straighten(corners, size ?? throw new BmageImageDocumentFormatException($"{r.Path} straightens, so it needs an \"outputSize\"."), background, sampling);
            }),

        Amount<BmageBrightnessAdjustment>("brightness", o => o.Amount, v => new BmageBrightnessAdjustment(v)),
        Amount<BmageContrastAdjustment>("contrast", o => o.Amount, v => new BmageContrastAdjustment(v)),
        Op<BmageExposureAdjustment>("exposure", (w, o) => w.Write("ev", o.Ev), r => new BmageExposureAdjustment(r.GetSingle("ev"))),
        Amount<BmageSaturationAdjustment>("saturation", o => o.Amount, v => new BmageSaturationAdjustment(v)),
        Op<BmageHueAdjustment>("hue", (w, o) => w.Write("degrees", o.Degrees), r => new BmageHueAdjustment(r.GetSingle("degrees"))),
        Amount<BmageTemperatureAdjustment>("temperature", o => o.Amount, v => new BmageTemperatureAdjustment(v)),
        Amount<BmageTintAdjustment>("tint", o => o.Amount, v => new BmageTintAdjustment(v)),
        Op<BmageOpacityAdjustment>("opacity", (w, o) => w.Write("opacity", o.Opacity), r => new BmageOpacityAdjustment(r.GetSingle("opacity"))),
        Amount<BmageGrayscaleFilter>("grayscale", o => o.Amount, v => new BmageGrayscaleFilter(v)),
        Amount<BmageSepiaFilter>("sepia", o => o.Amount, v => new BmageSepiaFilter(v)),
        Amount<BmageInvertFilter>("invert", o => o.Amount, v => new BmageInvertFilter(v)),
        Amount<BmageVibranceAdjustment>("vibrance", o => o.Amount, v => new BmageVibranceAdjustment(v)),
        Op<BmageGammaAdjustment>("gamma", (w, o) => w.Write("gamma", o.Gamma), r => new BmageGammaAdjustment(r.GetSingle("gamma"))),
        Op<BmageLevelsAdjustment>("levels",
            (w, o) => w.Write("inputBlack", o.InputBlack).Write("inputWhite", o.InputWhite).Write("gamma", o.Gamma)
                .Write("outputBlack", o.OutputBlack).Write("outputWhite", o.OutputWhite).WriteEnum("channels", o.Channels),
            r => new BmageLevelsAdjustment(r.GetSingle("inputBlack", 0), r.GetSingle("inputWhite", 1), r.GetSingle("gamma", 1),
                r.GetSingle("outputBlack", 0), r.GetSingle("outputWhite", 1), r.GetEnum("channels", BmageColorChannels.Rgb))),
        Op<BmagePosterizeFilter>("posterize", (w, o) => w.Write("levels", o.Levels), r => new BmagePosterizeFilter(r.GetInt32("levels"))),
        Op<BmageThresholdFilter>("threshold", (w, o) => w.Write("level", o.Level), r => new BmageThresholdFilter(r.GetSingle("level", 0.5f))),
        Op<BmageAdaptiveThresholdFilter>("adaptiveThreshold",
            (w, o) => w.Write("radius", o.Radius).Write("bias", o.Bias).WriteEnum("method", o.Method).Write("region", o.Region),
            r => new BmageAdaptiveThresholdFilter(r.GetSingle("radius", 16), r.GetSingle("bias", 0.2f), r.GetEnum("method", BmageAdaptiveThresholdMethod.Sauvola), r.GetNullableRectangle("region"))),
        Op<BmageColorLutAdjustment>("colorLut",
            (w, o) => w.Write("size", o.Table.Size).Write("title", o.Table.Title).Write("values", EncodeLut(o.Table)).Write("strength", o.Strength),
            r => new BmageColorLutAdjustment(DecodeLut(r), r.GetSingle("strength", 1))),
        Op<BmageShadowsHighlightsAdjustment>("shadowsHighlights",
            (w, o) => w.Write("shadows", o.Shadows).Write("highlights", o.Highlights),
            r => new BmageShadowsHighlightsAdjustment(r.GetSingle("shadows", 0), r.GetSingle("highlights", 0))),
        Op<BmageWhitesBlacksAdjustment>("whitesBlacks",
            (w, o) => w.Write("whites", o.Whites).Write("blacks", o.Blacks),
            r => new BmageWhitesBlacksAdjustment(r.GetSingle("whites", 0), r.GetSingle("blacks", 0))),
        Op<BmageHslAdjustment>("hsl",
            (w, o) => w.WriteObjects("bands", o.Shifts, (b, s) => b.WriteEnum("band", s.Key)
                .Write("hue", s.Value.Hue).Write("saturation", s.Value.Saturation).Write("lightness", s.Value.Lightness)),
            r => new BmageHslAdjustment(r.GetObjects("bands").Select(b => new KeyValuePair<BmageHueBand, BmageHslShift>(
                b.GetEnum("band", BmageHueBand.Reds), new BmageHslShift(b.GetSingle("hue", 0), b.GetSingle("saturation", 0), b.GetSingle("lightness", 0)))))),
        Op<BmageWhiteBalanceAdjustment>("whiteBalance",
            (w, o) => w.Write("redGain", o.RedGain).Write("greenGain", o.GreenGain).Write("blueGain", o.BlueGain),
            r => new BmageWhiteBalanceAdjustment(r.GetSingle("redGain", 1), r.GetSingle("greenGain", 1), r.GetSingle("blueGain", 1))),
        Op<BmageAutoWhiteBalanceOperation>("autoWhiteBalance", (w, o) => w.Write("strength", o.Strength), r => new BmageAutoWhiteBalanceOperation(r.GetSingle("strength", 0.8f))),
        Op<BmageChannelMixerAdjustment>("channelMixer",
            (w, o) => w.Write("red", [o.Red.X, o.Red.Y, o.Red.Z]).Write("green", [o.Green.X, o.Green.Y, o.Green.Z])
                .Write("blue", [o.Blue.X, o.Blue.Y, o.Blue.Z]).Write("constant", [o.Constant.X, o.Constant.Y, o.Constant.Z]),
            r => new BmageChannelMixerAdjustment(ReadVector3(r, "red", Vector3.UnitX), ReadVector3(r, "green", Vector3.UnitY), ReadVector3(r, "blue", Vector3.UnitZ), ReadVector3(r, "constant", Vector3.Zero))),
        Op<BmageColorizeAdjustment>("colorize",
            (w, o) => w.Write("hue", o.Hue).Write("saturation", o.Saturation).Write("lightness", o.Lightness).Write("amount", o.Amount),
            r => new BmageColorizeAdjustment(r.GetSingle("hue"), r.GetSingle("saturation", 0.25f), r.GetSingle("lightness", 0), r.GetSingle("amount", 1))),
        Op<BmageGradientMapOperation>("gradientMap",
            (w, o) => w.WriteObjects("stops", o.Stops, (s, stop) => s.Write("offset", stop.Offset).Write("color", stop.Color)).Write("amount", o.Amount),
            r => new BmageGradientMapOperation([.. r.GetObjects("stops").Select(s => (s.GetSingle("offset"), s.GetColor("color")))], r.GetSingle("amount", 1))),
        Op<BmageColorMatrixOperation>("colorMatrix",
            (w, o) => w.Write("matrix", o.Matrix.ToArray()).Write("name", o.Name),
            r => new BmageColorMatrixOperation(new BmageColorMatrix(r.GetSingles("matrix") is { Length: 20 } m ? m : throw new BmageImageDocumentFormatException($"{r.Path}.matrix should hold twenty numbers.")), r.GetString("name", null))),
        Op<BmageCurveOperation>("curve",
            (w, o) => w.Write("red", o.Red).Write("green", o.Green).Write("blue", o.Blue).Write("alpha", o.Alpha).Write("name", o.Name),
            r => new BmageCurveOperation(r.GetSingles("red"), r.GetSingles("green"), r.GetSingles("blue"), r.GetSingles("alpha"), r.GetString("name", null))),
        Op<BmageFusedPointOperation>("fused",
            (w, o) => w.Write("stages", BmageImagePipeline.From(o.Stages)),
            r => new BmageFusedPointOperation([.. r.GetPipeline("stages").Select(s => s as BmagePointOperation ?? throw new BmageImageDocumentFormatException($"{r.Path}.stages may only hold per-pixel operations, not '{s.Name}'."))])),
        Op<BmageAutoLevelsOperation>("autoLevels",
            (w, o) => w.Write("clipFraction", o.ClipFraction).Write("perChannel", o.PerChannel).Write("strength", o.Strength),
            r => new BmageAutoLevelsOperation(r.GetDouble("clipFraction", 0.005), r.GetBoolean("perChannel"), r.GetSingle("strength", 1))),
        Op<BmageAutoContrastOperation>("autoContrast",
            (w, o) => w.Write("targetMean", o.TargetMean).Write("strength", o.Strength),
            r => new BmageAutoContrastOperation(r.GetSingle("targetMean", 0.5f), r.GetSingle("strength", 0.7f))),

        Op<BmageBlurFilter>("blur", (w, o) => w.Write("radius", o.Radius).Write("region", o.Region), r => new BmageBlurFilter(r.GetSingle("radius"), r.GetNullableRectangle("region"))),
        Op<BmageSharpenFilter>("sharpen",
            (w, o) => w.Write("amount", o.Amount).Write("radius", o.Radius).Write("threshold", o.Threshold).Write("region", o.Region),
            r => new BmageSharpenFilter(r.GetSingle("amount", 0.5f), r.GetSingle("radius", 1), r.GetSingle("threshold", 0), r.GetNullableRectangle("region"))),
        Op<BmagePixelateFilter>("pixelate", (w, o) => w.Write("blockSize", o.BlockSize).Write("region", o.Region), r => new BmagePixelateFilter(r.GetInt32("blockSize"), r.GetNullableRectangle("region"))),
        Op<BmageNoiseFilter>("noise",
            (w, o) => w.Write("amount", o.Amount).Write("monochrome", o.Monochrome).Write("seed", o.Seed).Write("region", o.Region),
            r => new BmageNoiseFilter(r.GetSingle("amount"), r.GetBoolean("monochrome"), r.GetInt32("seed", 0), r.GetNullableRectangle("region"))),
        Op<BmageMotionBlurFilter>("motionBlur",
            (w, o) => w.Write("distance", o.Distance).Write("angle", o.Angle).Write("region", o.Region),
            r => new BmageMotionBlurFilter(r.GetSingle("distance"), r.GetSingle("angle", 0), r.GetNullableRectangle("region"))),
        Op<BmageRadialBlurFilter>("radialBlur",
            (w, o) => w.Write("amount", o.Amount).WriteEnum("mode", o.Mode).Write("center", o.Center).Write("region", o.Region),
            r => new BmageRadialBlurFilter(r.GetSingle("amount"), r.GetEnum("mode", BmageRadialBlurMode.Zoom), r.Has("center") ? r.GetPointF("center") : null, r.GetNullableRectangle("region"))),
        Op<BmageHighPassFilter>("highPass", (w, o) => w.Write("radius", o.Radius).Write("region", o.Region), r => new BmageHighPassFilter(r.GetSingle("radius", 3), r.GetNullableRectangle("region"))),
        Op<BmageMedianFilter>("median", (w, o) => w.Write("radius", o.Radius).Write("region", o.Region), r => new BmageMedianFilter(r.GetInt32("radius", 1), r.GetNullableRectangle("region"))),
        Op<BmageConvolutionFilter>("convolution",
            (w, o) => w.Write("kernel", o.Kernel).Write("kernelWidth", o.KernelWidth).Write("kernelHeight", o.KernelHeight)
                .Write("divisor", o.Divisor).Write("bias", o.Bias).Write("region", o.Region).Write("name", o.Name),
            r => new BmageConvolutionFilter(r.GetSingles("kernel") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"kernel\"."),
                r.GetInt32("kernelWidth"), r.GetInt32("kernelHeight"), r.GetSingle("divisor", 1), r.GetSingle("bias", 0), r.GetNullableRectangle("region"), r.GetString("name", null))),
        Op<BmageVignetteFilter>("vignette",
            (w, o) => w.Write("amount", o.Amount).Write("radius", o.Radius).Write("softness", o.Softness).Write("color", o.Color),
            r => new BmageVignetteFilter(r.GetSingle("amount", 0.5f), r.GetSingle("radius", 0.75f), r.GetSingle("softness", 0.5f), r.GetColor("color", BmageRgba32.Black))),

        Op<BmageMaskedOperation>("masked",
            (w, o) => w.Write("operation", o.Operation).Write("mask", o.Mask),
            r => new BmageMaskedOperation(r.GetOperation("operation") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"operation\"."), RequireMask(r))),
        Op<BmageAlphaMaskOperation>("applyMask", (w, o) => w.Write("mask", o.Mask), r => new BmageAlphaMaskOperation(RequireMask(r))),
        Op<BmagePipelineOperation>("pipeline",
            (w, o) => { w.Write("operations", o.Pipeline); if (o.Name != "Pipeline") w.Write("name", o.Name); },
            r => new BmagePipelineOperation(r.GetPipeline("operations"), r.GetString("name", null))),
    ];

    private static BmageCodec Op<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : IBmageImageOperation
        => new(typeof(IBmageImageOperation), typeof(T), name, (w, value) => write(w, (T)value), r => read(r));

    private static Vector3 ReadVector3(BmageJsonReader r, string name, Vector3 fallback) => r.GetSingles(name) switch
    {
        null => fallback,
        { Length: 3 } v => new Vector3(v[0], v[1], v[2]),
        _ => throw new BmageImageDocumentFormatException($"{r.Path}.{name} should hold three numbers."),
    };

    private static BmageCodec Amount<T>(string name, Func<T, float> amount, Func<float, T> create) where T : IBmageImageOperation
        => Op<T>(name, (w, o) => w.Write("amount", amount(o)), r => create(r.GetSingle("amount")));

    private static void WriteResize(BmageJsonWriter w, BmageResizeOperation o)
        => w.Write("width", o.Width).Write("height", o.Height).WriteEnum("mode", o.Mode).WriteEnum("filter", o.Filter)
            .WriteEnum("anchor", o.Anchor).Write("background", o.Background);

    private static BmageResizeOperation ReadResize(BmageJsonReader r)
        => new(r.GetNullableInt32("width"), r.GetNullableInt32("height"), r.GetEnum("mode", BmageResizeMode.Fit), r.GetEnum("filter", BmageResamplingFilter.Auto),
            r.GetEnum("anchor", BmageAnchor.Center), r.GetColor("background", default));

    /// <summary>A table's values as base64 of little-endian 32-bit floats: exact, and a third of the size of a number array.</summary>
    private static string EncodeLut(BmageColorLookupTable table)
    {
        var bytes = new byte[table.Values.Length * 4];
        for (var i = 0; i < table.Values.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), table.Values[i]);
        return Convert.ToBase64String(bytes);
    }

    private static BmageColorLookupTable DecodeLut(BmageJsonReader r)
    {
        var size = r.GetInt32("size");
        if (size is < BmageColorLookupTable.MinSize or > BmageColorLookupTable.MaxSize)
            throw new BmageImageDocumentFormatException($"{r.Path}.size must be between {BmageColorLookupTable.MinSize} and {BmageColorLookupTable.MaxSize}.");
        var text = r.GetString("values");
        var expected = size * size * size * 3;
        // Checked before decoding: the text's length bounds the bytes, so a wrong one is refused without allocating them.
        if (text.Length != (expected * 4 + 2) / 3 * 4) throw new BmageImageDocumentFormatException($"{r.Path}.values does not hold {expected} values.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(text); }
        catch (FormatException) { throw new BmageImageDocumentFormatException($"{r.Path}.values is not base64."); }
        if (bytes.Length != expected * 4) throw new BmageImageDocumentFormatException($"{r.Path}.values does not hold {expected} values.");
        var values = new float[expected];
        for (var i = 0; i < expected; i++) values[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 4));
        try { return BmageColorLookupTable.FromValues(size, values, r.GetString("title", null)); }
        catch (ArgumentException ex) { throw new BmageImageDocumentFormatException($"{r.Path}.values: {ex.Message}"); }
    }

    private static void WriteRatio(BmageJsonWriter w, BmageAspectRatio ratio)
    {
        w.Json.WriteStartArray("ratio");
        w.WriteNumberValue(ratio.Width);
        w.WriteNumberValue(ratio.Height);
        w.Json.WriteEndArray();
    }

    private static BmageAspectRatio ReadRatio(BmageJsonReader r)
    {
        if (!r.Element.TryGetProperty("ratio", out var value) || value.ValueKind != System.Text.Json.JsonValueKind.Array || value.GetArrayLength() != 2)
            throw new BmageImageDocumentFormatException($"{r.Path}.ratio should be [width, height].");
        return new BmageAspectRatio(BmageJsonReader.ReadDouble(value[0]), BmageJsonReader.ReadDouble(value[1]));
    }

    private static BmageMask RequireMask(BmageJsonReader r) => r.GetMask("mask") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"mask\".");

    // ------------------------------------------------------------------ masks

    private static IEnumerable<BmageCodec> Masks() =>
    [
        MaskOf<BmageRectangleMask>("rectangle", (w, m) => w.Write("rectangle", m.Rectangle).Write("cornerRadius", m.CornerRadius),
            r => new BmageRectangleMask(r.GetRectangleF("rectangle")) { CornerRadius = r.GetSingle("cornerRadius", 0) }),
        MaskOf<BmageEllipseMask>("ellipse", (w, m) => w.Write("rectangle", m.Rectangle), r => new BmageEllipseMask(r.GetRectangleF("rectangle"))),
        MaskOf<BmagePolygonMask>("polygon", (w, m) => w.Write("points", m.Points).WriteEnum("rule", m.Rule),
            r => new BmagePolygonMask(r.GetPoints("points")) { Rule = r.GetEnum("rule", BmageFillRule.NonZero) }),
        MaskOf<BmagePathMask>("path",
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
            r => new BmagePathMask(ReadPath(r)) { Rule = r.GetEnum("rule", BmageFillRule.NonZero) }),
        MaskOf<BmageBrushMask>("brush",
            (w, m) => w.WriteObjects("strokes", m.Strokes, (s, stroke) => s.Write("points", stroke.Points).Write("width", stroke.Width).Write("opacity", stroke.Opacity).Write("erase", stroke.Erase)),
            r => new BmageBrushMask([.. r.GetObjects("strokes").Select(s => new BmageBrushStroke(s.GetPoints("points"), s.GetSingle("width"))
            {
                Opacity = s.GetSingle("opacity", 1),
                Erase = s.GetBoolean("erase"),
            })])),
        MaskOf<BmageLinearGradientMask>("linearGradient", (w, m) => w.Write("start", m.Start).Write("end", m.End), r => new BmageLinearGradientMask(r.GetVector2("start"), r.GetVector2("end"))),
        MaskOf<BmageRadialGradientMask>("radialGradient",
            (w, m) => w.Write("center", m.Center).Write("radiusX", m.RadiusX).Write("radiusY", m.RadiusY).Write("hardness", m.Hardness).Write("rotation", m.Rotation),
            r => new BmageRadialGradientMask(r.GetVector2("center"), r.GetSingle("radiusX"), r.GetSingle("radiusY"))
            {
                Hardness = r.GetSingle("hardness", 0.5f),
                Rotation = r.GetSingle("rotation", 0),
            }),
        MaskOf<BmageLuminanceRangeMask>("luminanceRange", (w, m) => w.Write("low", m.Low).Write("high", m.High).Write("softness", m.Softness),
            r => new BmageLuminanceRangeMask(r.GetSingle("low"), r.GetSingle("high")) { Softness = r.GetSingle("softness", 0.1f) }),
        MaskOf<BmageColorRangeMask>("colorRange", (w, m) => w.Write("color", m.Color).Write("tolerance", m.Tolerance).Write("softness", m.Softness),
            r => new BmageColorRangeMask(r.GetColor("color")) { Tolerance = r.GetSingle("tolerance", 0.15f), Softness = r.GetSingle("softness", 0.1f) }),
        MaskOf<BmageBitmapMask>("bitmap", (w, m) => w.WriteImage("image", m.Image).WriteEnum("channel", m.Channel),
            r => new BmageBitmapMask(r.GetImage("image")) { Channel = r.GetEnum("channel", BmageMaskChannel.Alpha) }),
        MaskOf<BmageCombinedMask>("combined", (w, m) => w.Write("first", m.First).Write("second", m.Second).WriteEnum("mode", m.Mode), ReadCombined),
        MaskOf<BmageFloodMask>("flood",
            (w, m) => w.Write("seeds", m.Seeds).Write("tolerance", m.Tolerance).Write("softness", m.Softness).Write("contiguous", m.Contiguous),
            r => new BmageFloodMask(r.GetPoints("seeds"))
            {
                Tolerance = r.GetSingle("tolerance", 0.12f),
                Softness = r.GetSingle("softness", 0.04f),
                Contiguous = r.GetBoolean("contiguous", true),
            }),
        MaskOf<BmageMorphedMask>("morphed", (w, m) => w.Write("source", m.Source).WriteEnum("morphology", m.Morphology).Write("distance", m.Distance), ReadMorphed),
    ];

    /// <summary>A morphed mask, refused when drawing it would hold more masks at once than <see cref="BmageMask.MaxMasksHeld"/>.</summary>
    private static BmageMorphedMask ReadMorphed(BmageJsonReader r)
    {
        var mask = new BmageMorphedMask(r.GetMask("source") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"source\"."),
            r.GetEnum("morphology", BmageMaskMorphology.Expand), r.GetSingle("distance"));
        var feathered = r.GetSingle("feather", 0) >= 0.05f ? 1 : 0;
        if (mask.MasksHeld() + feathered > BmageMask.MaxMasksHeld)
            throw new BmageImageDocumentFormatException($"{r.Path} nests its masks too deeply: drawing it would hold more than {BmageMask.MaxMasksHeld} masks at once.");
        return mask;
    }

    /// <summary>
    /// A combination, refused when drawing it would hold more masks at once than <see cref="BmageMask.MaxMasksHeld"/>: each is
    /// the size of the image, so a document nesting a hundred of them, as only a hostile one would, could otherwise take
    /// gigabytes from a server that replays it.
    /// </summary>
    private static BmageCombinedMask ReadCombined(BmageJsonReader r)
    {
        var mask = new BmageCombinedMask(r.GetMask("first") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"first\"."),
            r.GetMask("second") ?? throw new BmageImageDocumentFormatException($"{r.Path} has no \"second\"."), r.GetEnum("mode", BmageMaskCombineMode.Union));
        // The combination's own feather is set once this returns, and holds a working copy of its own.
        var feathered = r.GetSingle("feather", 0) >= 0.05f ? 1 : 0;
        if (mask.MasksHeld() + feathered > BmageMask.MaxMasksHeld)
            throw new BmageImageDocumentFormatException($"{r.Path} nests its masks too deeply: drawing it would hold more than {BmageMask.MaxMasksHeld} masks at once. Combine them a step at a time, a.Union(b).Union(c), rather than inside one another.");
        return mask;
    }

    private static BmageCodec MaskOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : BmageMask
        => new(typeof(BmageMask), typeof(T), name,
            (w, value) =>
            {
                var mask = (BmageMask)value;
                if (mask.Inverted) w.Write("inverted", true);
                if (mask.Feather != 0) w.Write("feather", mask.Feather);
                if (mask.Opacity != 1) w.Write("opacity", mask.Opacity);
                if (mask.Units != BmageMaskUnits.Pixels) w.WriteEnum("units", mask.Units);
                write(w, (T)value);
            },
            r => read(r) with
            {
                Inverted = r.GetBoolean("inverted"),
                Feather = r.GetSingle("feather", 0),
                Opacity = r.GetSingle("opacity", 1),
                Units = r.GetEnum("units", BmageMaskUnits.Pixels),
            });

    private static BmageVectorPath ReadPath(BmageJsonReader r)
    {
        var contours = new List<Vector2[]>();
        if (r.Element.TryGetProperty("contours", out var array) && array.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var contour in array.EnumerateArray())
            {
                if (contour.ValueKind != System.Text.Json.JsonValueKind.Array || contour.GetArrayLength() % 2 != 0)
                    throw new BmageImageDocumentFormatException($"{r.Path}.contours should hold arrays of x, y pairs.");
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
        return new BmageVectorPath(contours, closed);
    }

    // ------------------------------------------------------------------ layers

    private static IEnumerable<BmageCodec> Layers() =>
    [
        LayerOf<BmageImageLayer>("image",
            (w, l) => w.WriteImage("image", l.Image).Write("bounds", l.Bounds).Write("rotation", l.Rotation).Write("pipeline", l.Pipeline).WriteEnum("filter", l.Filter),
            r => new BmageImageLayer
            {
                Image = r.GetImage("image"),
                Bounds = r.GetRectangleF("bounds"),
                Rotation = r.GetSingle("rotation", 0),
                Pipeline = r.GetPipeline("pipeline"),
                Filter = r.GetEnum("filter", BmageResamplingFilter.Auto),
            }),
        LayerOf<BmageFillLayer>("fill", (w, l) => w.Write("color", l.Color), r => new BmageFillLayer(r.GetColor("color"))),
        LayerOf<BmageAdjustmentLayer>("adjustment", (w, l) => w.Write("pipeline", l.Pipeline), r => new BmageAdjustmentLayer(r.GetPipeline("pipeline"))),
        LayerOf<BmagePaintLayer>("paint",
            (w, l) => w.WriteObjects("strokes", l.Strokes, (s, stroke) =>
            {
                s.Write("points", stroke.Points).Write("size", stroke.Size).Write("color", stroke.Color);
                if (stroke.Pressures is { } pressures) s.Write("pressures", pressures);
                s.Write("opacity", stroke.Opacity).Write("hardness", stroke.Hardness).Write("erase", stroke.Erase)
                    .Write("pressureAffectsSize", stroke.PressureAffectsSize).Write("pressureAffectsOpacity", stroke.PressureAffectsOpacity);
            }),
            r => new BmagePaintLayer([.. r.GetObjects("strokes").Select(ReadPaintStroke)])),
        LayerOf<BmageGroupLayer>("group",
            (w, l) => { w.Write("layers", l.Layers); if (l.Isolated) w.Write("isolated", true); },
            r => new BmageGroupLayer(r.GetArray<BmageLayer>("layers")) { Isolated = r.GetBoolean("isolated") }),
    ];

    private static BmagePaintStroke ReadPaintStroke(BmageJsonReader s)
    {
        var points = s.GetPoints("points");
        var pressures = s.GetSingles("pressures");
        if (pressures is not null && pressures.Length != points.Length)
            throw new BmageImageDocumentFormatException($"{s.Path}.pressures has {pressures.Length} values for {points.Length} points.");
        return new BmagePaintStroke(points, s.GetSingle("size"), s.GetColor("color", BmageRgba32.Black))
        {
            Pressures = pressures,
            Opacity = s.GetSingle("opacity", 1),
            Hardness = s.GetSingle("hardness", 0.8f),
            Erase = s.GetBoolean("erase"),
            PressureAffectsSize = s.GetBoolean("pressureAffectsSize", true),
            PressureAffectsOpacity = s.GetBoolean("pressureAffectsOpacity"),
        };
    }

    private static BmageCodec LayerOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : BmageLayer
        => new(typeof(BmageLayer), typeof(T), name,
            (w, value) =>
            {
                var layer = (BmageLayer)value;
                w.Write("id", layer.Id);
                if (layer.Name is not null) w.Write("name", layer.Name);
                if (layer.BlendMode != BmageBlendMode.SourceOver) w.WriteEnum("blendMode", layer.BlendMode);
                if (layer.Opacity != 1) w.Write("opacity", layer.Opacity);
                if (layer.Mask is not null) w.Write("mask", layer.Mask);
                if (!layer.IsVisible) w.Write("isVisible", false);
                if (layer.IsLocked) w.Write("isLocked", true);
                if (layer.ClipToLayerBelow) w.Write("clipToLayerBelow", true);
                write(w, (T)value);
            },
            r =>
            {
                var layer = read(r);
                return layer with
                {
                    Id = r.GetGuid("id", layer.Id),
                    Name = r.GetString("name", null),
                    BlendMode = r.GetEnum("blendMode", BmageBlendMode.SourceOver),
                    Opacity = r.GetSingle("opacity", 1),
                    Mask = r.GetMask("mask"),
                    IsVisible = r.GetBoolean("isVisible", true),
                    IsLocked = r.GetBoolean("isLocked"),
                    ClipToLayerBelow = r.GetBoolean("clipToLayerBelow"),
                };
            });

    // ------------------------------------------------------------------ annotations

    private static IEnumerable<BmageCodec> Annotations() =>
    [
        AnnotationOf<BmageRectangleAnnotation>("rectangle",
            (w, a) => { w.Write("rectangle", a.Rectangle).Write("cornerRadius", a.CornerRadius); WriteShape(w, a.Style); },
            r => new BmageRectangleAnnotation { Rectangle = r.GetRectangleF("rectangle"), CornerRadius = r.GetSingle("cornerRadius", 0), Style = ReadShape(r, BmageShapeStyle.Default) }),
        AnnotationOf<BmageEllipseAnnotation>("ellipse",
            (w, a) => { w.Write("rectangle", a.Rectangle); WriteShape(w, a.Style); },
            r => new BmageEllipseAnnotation { Rectangle = r.GetRectangleF("rectangle"), Style = ReadShape(r, BmageShapeStyle.Default) }),
        AnnotationOf<BmageLineAnnotation>("line",
            (w, a) => { w.Write("start", a.Start).Write("end", a.End); WriteShape(w, a.Style); },
            r => new BmageLineAnnotation { Start = r.GetVector2("start"), End = r.GetVector2("end"), Style = ReadShape(r, BmageShapeStyle.Default) }),
        AnnotationOf<BmageArrowAnnotation>("arrow",
            (w, a) => { w.Write("start", a.Start).Write("end", a.End).Write("headLengthFactor", a.HeadLengthFactor).Write("headWidthFactor", a.HeadWidthFactor); WriteShape(w, a.Style); },
            r => new BmageArrowAnnotation
            {
                Start = r.GetVector2("start"),
                End = r.GetVector2("end"),
                HeadLengthFactor = r.GetSingle("headLengthFactor", 4),
                HeadWidthFactor = r.GetSingle("headWidthFactor", 3),
                Style = ReadShape(r, BmageShapeStyle.Default),
            }),
        AnnotationOf<BmageFreehandAnnotation>("freehand",
            (w, a) => { w.Write("points", a.Points).Write("closed", a.Closed); WriteShape(w, a.Style); },
            r =>
            {
                var plain = new BmageFreehandAnnotation { Points = r.GetPoints("points") };
                return plain with { Closed = r.GetBoolean("closed"), Style = ReadShape(r, plain.Style) };
            }),
        AnnotationOf<BmagePolygonAnnotation>("polygon",
            (w, a) => { w.Write("points", a.Points).Write("closed", a.Closed); WriteShape(w, a.Style); },
            r => new BmagePolygonAnnotation { Points = r.GetPoints("points"), Closed = r.GetBoolean("closed", true), Style = ReadShape(r, BmageShapeStyle.Default) }),
        AnnotationOf<BmageTextAnnotation>("text",
            (w, a) =>
            {
                w.Write("text", a.Text).Write("position", a.Position).Write("backgroundColor", a.BackgroundColor)
                    .Write("backgroundPadding", a.BackgroundPadding).Write("backgroundCornerRadius", a.BackgroundCornerRadius);
                if (a.MeasuredSize is { } size) w.Write("measuredSize", size);
                WriteText(w, "style", a.Style);
            },
            r => new BmageTextAnnotation
            {
                Text = r.GetString("text"),
                Position = r.GetVector2("position"),
                BackgroundColor = r.GetNullableColor("backgroundColor"),
                BackgroundPadding = r.GetSingle("backgroundPadding", 6),
                BackgroundCornerRadius = r.GetSingle("backgroundCornerRadius", 4),
                MeasuredSize = r.GetNullableSizeF("measuredSize"),
                Style = ReadText(r, "style", new BmageTextStyle()),
            }),
        AnnotationOf<BmageHighlightAnnotation>("highlight",
            (w, a) => w.Write("rectangle", a.Rectangle).Write("color", a.Color),
            r => new BmageHighlightAnnotation { Rectangle = r.GetRectangleF("rectangle"), Color = r.GetColor("color", new BmageRgba32(255, 235, 59, 110)) }),
        AnnotationOf<BmageRedactionAnnotation>("redaction",
            (w, a) => w.Write("rectangle", a.Rectangle).WriteEnum("mode", a.Mode).Write("color", a.Color).Write("strength", a.Strength),
            r => new BmageRedactionAnnotation
            {
                Rectangle = r.GetRectangleF("rectangle"),
                Mode = r.GetEnum("mode", BmageRedactionMode.Solid),
                Color = r.GetColor("color", BmageRgba32.Black),
                Strength = r.GetSingle("strength", 12),
            }),
        AnnotationOf<BmageCalloutAnnotation>("callout",
            (w, a) =>
            {
                w.Write("bubble", a.Bubble).Write("target", a.Target).Write("text", a.Text).Write("cornerRadius", a.CornerRadius);
                WriteText(w, "textStyle", a.TextStyle);
                WriteShape(w, a.Style);
            },
            r =>
            {
                var plain = new BmageCalloutAnnotation { Bubble = r.GetRectangleF("bubble"), Target = r.GetVector2("target") };
                return plain with
                {
                    Text = r.GetString("text", string.Empty) ?? string.Empty,
                    CornerRadius = r.GetSingle("cornerRadius", 6),
                    TextStyle = ReadText(r, "textStyle", plain.TextStyle),
                    Style = ReadShape(r, plain.Style),
                };
            }),
        AnnotationOf<BmageStepAnnotation>("step",
            (w, a) =>
            {
                w.Write("center", a.Center).Write("number", a.Number).Write("radius", a.Radius)
                    .Write("backgroundColor", a.BackgroundColor).Write("foregroundColor", a.ForegroundColor);
                WriteText(w, "textStyle", a.TextStyle);
            },
            r =>
            {
                var plain = new BmageStepAnnotation { Center = r.GetVector2("center"), Number = r.GetInt32("number") };
                return plain with
                {
                    Radius = r.GetSingle("radius", 18),
                    BackgroundColor = r.GetColor("backgroundColor", plain.BackgroundColor),
                    ForegroundColor = r.GetColor("foregroundColor", plain.ForegroundColor),
                    TextStyle = ReadText(r, "textStyle", plain.TextStyle),
                };
            }),
        AnnotationOf<BmageImageAnnotation>("image",
            (w, a) => w.WriteImage("image", a.Image).Write("rectangle", a.Rectangle),
            r => new BmageImageAnnotation { Image = r.GetImage("image"), Rectangle = r.GetRectangleF("rectangle") }),
    ];

    private static BmageCodec AnnotationOf<T>(string name, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : BmageAnnotation
        => new(typeof(BmageAnnotation), typeof(T), name,
            (w, value) =>
            {
                var annotation = (BmageAnnotation)value;
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

    private static void WriteStroke(BmageJsonWriter w, string name, BmageStrokeStyle stroke)
        => w.WriteObject(name, s =>
        {
            s.Write("width", stroke.Width).WriteEnum("cap", stroke.Cap).WriteEnum("join", stroke.Join).Write("miterLimit", stroke.MiterLimit);
            if (stroke.DashPattern is { } dashes) s.Write("dashPattern", dashes).Write("dashOffset", stroke.DashOffset);
        });

    private static BmageStrokeStyle ReadStroke(BmageJsonReader r, string name, BmageStrokeStyle fallback)
    {
        if (r.GetObject(name) is not { } s) return fallback;
        return new BmageStrokeStyle
        {
            Width = s.GetSingle("width", fallback.Width),
            Cap = s.GetEnum("cap", fallback.Cap),
            Join = s.GetEnum("join", fallback.Join),
            MiterLimit = s.GetSingle("miterLimit", fallback.MiterLimit),
            DashPattern = s.GetSingles("dashPattern"),
            DashOffset = s.GetSingle("dashOffset", 0),
        };
    }

    private static void WriteShape(BmageJsonWriter w, BmageShapeStyle style)
        => w.WriteObject("style", s =>
        {
            s.Write("strokeColor", style.StrokeColor).Write("fillColor", style.FillColor);
            WriteStroke(s, "stroke", style.Stroke);
        });

    private static BmageShapeStyle ReadShape(BmageJsonReader r, BmageShapeStyle fallback)
    {
        if (r.GetObject("style") is not { } s) return fallback;
        return new BmageShapeStyle
        {
            StrokeColor = s.GetNullableColor("strokeColor", fallback.StrokeColor),
            FillColor = s.GetNullableColor("fillColor", fallback.FillColor),
            Stroke = ReadStroke(s, "stroke", fallback.Stroke),
        };
    }

    private static void WriteText(BmageJsonWriter w, string name, BmageTextStyle style)
        => w.WriteObject(name, s =>
        {
            s.Write("fontFamily", style.FontFamily).Write("fontSize", style.FontSize).Write("bold", style.Bold).Write("italic", style.Italic)
                .Write("fillColor", style.FillColor).Write("strokeColor", style.StrokeColor)
                .WriteEnum("alignment", style.Alignment).WriteEnum("baseline", style.Baseline)
                .Write("lineHeight", style.LineHeight).Write("letterSpacing", style.LetterSpacing).Write("maxWidth", style.MaxWidth);
            WriteStroke(s, "stroke", style.Stroke);
        });

    private static BmageTextStyle ReadText(BmageJsonReader r, string name, BmageTextStyle fallback)
    {
        if (r.GetObject(name) is not { } s) return fallback;
        return new BmageTextStyle
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
