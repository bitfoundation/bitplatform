using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using System.Globalization;
using Bit.Bmage.Codecs;
using Bit.Bmage.Drawing;
using Bit.Bmage.Metadata;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Demo.Server.Dtos;
using Bit.Bmage.Demo.Client.Shared;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Runs the real Bmage engine on this server, so the MCP tools can answer questions about images
/// with measurements.
/// <para>
/// Three questions about an image pipeline cannot be answered by reading documentation, and an agent
/// that guesses at them ships settings chosen by habit:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>How many bytes does this actually produce?</b> JPEG at 0.8 versus 0.9, PNG versus JPEG for a
///     screenshot, whether a 120 KB budget is reachable at all - <see cref="RunAsync"/> encodes the
///     output with the managed codecs at every quality asked for, and runs the library's own
///     MaxFileSize search.
///   </description></item>
///   <item><description>
///     <b>What does the optimiser do with it?</b> Identities dropped, crops merged, adjustments fused
///     into one pass - or nothing, because an AutoOrient in front stopped it planning. The optimised
///     operation list is read off <see cref="ImagePipeline.Optimize"/> for the real input size, and
///     both versions are timed.
///   </description></item>
///   <item><description>
///     <b>Does the export policy really drop the location?</b> <see cref="CompareMetadataPoliciesAsync"/>
///     encodes a GPS-tagged photo under each <see cref="MetadataPolicy"/> and reads the file back.
///   </description></item>
/// </list>
/// <para>
/// The input is generated, not uploaded: a photo-like scene with sensor grain, a flat-colour
/// screenshot, or a transparent graphic, drawn with the library's own canvas. That keeps the tool a
/// pure function of its arguments - the same call measures the same image - and keeps arbitrary
/// files away from a public server's decoders.
/// </para>
/// </summary>
public static class BmagePipelineLab
{
    /// <summary>The source size used when none is given: a 4 megapixel image, a typical phone photo after a first cap.</summary>
    public const int DefaultWidth = 2400;
    public const int DefaultHeight = 1600;

    /// <summary>The largest source edge and area a run accepts. This is a shared public server.</summary>
    public const int MaxEdge = 4096;
    public const long MaxPixels = 12_000_000;

    /// <summary>
    /// The limits every run executes under. The source cap alone does not bound a run: a Resize or a Pad can
    /// grow the image far past it, and the library's default limits allow 64 megapixel buffers.
    /// </summary>
    private static readonly ImageLimits RunLimits = new() { MaxWidth = MaxEdge, MaxHeight = MaxEdge, MaxPixels = MaxPixels };

    /// <summary>How long one run may take, queueing included, before it is cancelled.</summary>
    public static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(20);

    public const int MaxFormats = 4;
    public const int MaxQualities = 6;

    /// <summary>The generated inputs, by the name the tool takes.</summary>
    public static readonly string[] Sources = ["photo", "screenshot", "graphic"];

    /// <summary>
    /// At most two runs at a time. Each one holds a few full-size buffers and saturates a core for a
    /// fraction of a second; a burst of agent calls queues here rather than taking the site down.
    /// </summary>
    private static readonly SemaphoreSlim _gate = new(2, 2);

    private static readonly ImageProcessor _processor = new();

    /// <summary>
    /// Whether the engine in this process was compiled without optimisations. A Debug build runs the
    /// pixel loops several times slower, and a timing that does not say so would be read as the
    /// library's speed.
    /// </summary>
    private static readonly bool IsDebugBuild =
        typeof(ImageBuffer).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
            .OfType<DebuggableAttribute>()
            .Any(attribute => attribute.IsJITOptimizerDisabled);

    /// <summary>Runs <paramref name="spec"/> on a generated image and measures the result.</summary>
    public static async Task<BmagePipelineRunDto> RunAsync(
        string? spec,
        string? source = "photo",
        int width = DefaultWidth,
        int height = DefaultHeight,
        string? formats = null,
        string? qualities = null,
        long? maxFileSize = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = BmagePipelineSpec.Parse(spec);
        var kind = NormalizeSource(source);
        var warnings = new List<string>(parsed.Warnings);

        var (sourceWidth, sourceHeight) = ClampSize(width, height, warnings);

        if (parsed.Pipeline is not { } pipeline) return Failed(spec, kind, sourceWidth, sourceHeight, parsed.Error!, warnings);

        if (kind is null)
        {
            warnings.Add($"'{source}' is not a known source; used 'photo'. The sources are: {string.Join(", ", Sources)}.");
            kind = "photo";
        }

        var (encodeFormats, formatWarnings) = ParseFormats(formats);
        warnings.AddRange(formatWarnings);

        var (encodeQualities, qualityWarnings) = ParseQualities(qualities);
        warnings.AddRange(qualityWarnings);

        var size = new Size(sourceWidth, sourceHeight);
        var predicted = pipeline.TryGetOutputSize(size, out var predictedSize) ? $"{predictedSize.Width} x {predictedSize.Height}" : "cannot be computed for this input";
        var optimized = pipeline.Optimize(size);

        var callerToken = cancellationToken;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        timeout.CancelAfter(RunTimeout);
        cancellationToken = timeout.Token;

        try
        {
            await _gate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested is false)
        {
            return Failed(spec, kind, sourceWidth, sourceHeight, $"The server is busy with other runs; none started within {RunTimeout.TotalSeconds:0} seconds. Try again shortly.", warnings);
        }

        try
        {
            using var input = CreateSource(kind, sourceWidth, sourceHeight);

            ImageBuffer? output = null;
            double executeMs, unoptimizedMs = 0;

            try
            {
                // Twice, and the faster one kept: the first run in a fresh process includes the JIT
                // compiling the operations, which says something about the server and nothing about them.
                (output, executeMs) = await TimeAsync(() => pipeline.ExecuteAsync(input, new PipelineExecutionOptions { Limits = RunLimits, YieldBetweenOperations = false, CancellationToken = cancellationToken }));
                output.Dispose();
                output = null;
                (output, var second) = await TimeAsync(() => pipeline.ExecuteAsync(input, new PipelineExecutionOptions { Limits = RunLimits, YieldBetweenOperations = false, CancellationToken = cancellationToken }));
                executeMs = Math.Min(executeMs, second);

                if (SameOperations(pipeline, optimized) is false)
                {
                    var (unoptimized, ms) = await TimeAsync(() => pipeline.ExecuteAsync(input, new PipelineExecutionOptions { Limits = RunLimits, Optimize = false, YieldBetweenOperations = false, CancellationToken = cancellationToken }));
                    unoptimized.Dispose();
                    unoptimizedMs = ms;
                }
            }
            catch (ImageException ex)
            {
                // The second run may have finished before the unoptimised one failed; its buffer goes back to the pool.
                output?.Dispose();

                // The library's own message names the step that failed: "Operation 'Crop' (step 2 of 4) failed: ...".
                return Failed(spec, kind, sourceWidth, sourceHeight, ex.Message, warnings) with
                {
                    Pipeline = parsed.Canonical,
                    Operations = Describe(pipeline),
                    OptimizedOperations = Describe(optimized),
                    PredictedOutputSize = predicted,
                };
            }

            using (output)
            {
                var transparent = output.HasTransparency();
                var encodings = await EncodeAllAsync(output, encodeFormats, encodeQualities, transparent, cancellationToken);
                var budgets = maxFileSize is { } budget ? await RunBudgetsAsync(output, encodeFormats, encodeQualities, budget, warnings, cancellationToken) : null;

                return new BmagePipelineRunDto
                {
                    Pipeline = parsed.Canonical,
                    Operations = Describe(pipeline),
                    OptimizedOperations = Describe(optimized),
                    OptimizerNotes = OptimizerNotes(pipeline, optimized),
                    Source = kind,
                    SourceWidth = sourceWidth,
                    SourceHeight = sourceHeight,
                    PredictedOutputSize = predicted,
                    OutputWidth = output.Width,
                    OutputHeight = output.Height,
                    ExecuteMilliseconds = Math.Round(executeMs, 1),
                    UnoptimizedMilliseconds = unoptimizedMs > 0 ? Math.Round(unoptimizedMs, 1) : null,
                    OutputHasTransparency = transparent,
                    Encodings = encodings,
                    Budgets = budgets,
                    Reading = Read(kind, output, executeMs, unoptimizedMs, encodings, budgets),
                    Warnings = [.. warnings],
                };
            }
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested is false)
        {
            return Failed(spec, kind, sourceWidth, sourceHeight, $"The run took longer than the {RunTimeout.TotalSeconds:0} seconds this public server allows. Use a smaller source, fewer formats or qualities, or a shorter pipeline.", warnings);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Encodes a camera photo carrying a GPS position, a serial number, an owner name, camera and
    /// lens, ISO, a date and a copyright through the real encoder under each <see cref="MetadataPolicy"/>,
    /// and decodes each file again to report what is really in it.
    /// </summary>
    public static async Task<BmageMetadataComparisonDto> CompareMetadataPoliciesAsync(string? format = "jpeg", CancellationToken cancellationToken = default)
    {
        var target = ParseFormat(format);

        if (target is not (ImageFormat.Jpeg or ImageFormat.Png or ImageFormat.Bmp))
        {
            return new BmageMetadataComparisonDto
            {
                Format = format ?? string.Empty,
                Original = [],
                Policies = [],
                Reading = string.Empty,
                Error = target is ImageFormat.WebP or ImageFormat.Avif
                    ? $"{target} is encoded by the browser, which this server does not have. Bmage re-inserts metadata into a " +
                      "browser-encoded WebP by rewriting its container, under the same policies - the answer for 'jpeg' is the " +
                      "answer for WebP. AVIF is not written with metadata by this library at all."
                    : $"'{format}' is not a format this can test. Use 'jpeg', 'png' or 'bmp'."
            };
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            using var photo = SampleImages.Landscape(800, 533);
            photo.Metadata = new ImageMetadata(SampleExif());

            var original = Fields.Select(field => (field.Label, Value: field.Read(photo.Metadata.Exif!))).Where(field => field.Value is not null).ToArray();
            var results = new List<BmageMetadataPolicyDto>();

            foreach (var policy in Enum.GetValues<MetadataPolicy>())
            {
                var encoded = await _processor.EncodeAsync(photo, new ImageExportOptions { Format = target, Quality = 0.85, Metadata = policy }, cancellationToken);

                // Decoded again from the bytes: the report is what the file contains, not what the policy is documented to do.
                using var back = await _processor.DecodeAsync(encoded.Data, cancellationToken: cancellationToken);
                var exif = back.Metadata?.Exif ?? new ExifData();

                var kept = Fields.Where(field => field.Read(exif) is not null).Select(field => $"{field.Label}: {field.Read(exif)}").ToArray();
                var removed = original.Where(field => Fields.First(f => f.Label == field.Label).Read(exif) is null).Select(field => field.Label).ToArray();

                results.Add(new BmageMetadataPolicyDto
                {
                    Policy = policy.ToString(),
                    Format = target.ToString(),
                    Bytes = encoded.Length,
                    Kept = kept,
                    Removed = removed,
                    LeaksLocation = exif.Location is not null,
                    LeaksIdentity = exif.Exif.Get(ExifTag.BodySerialNumber) is not null || exif.Exif.Get(ExifTag.OwnerName) is not null,
                });
            }

            return new BmageMetadataComparisonDto
            {
                Format = target.ToString(),
                Original = [.. original.Select(field => $"{field.Label}: {field.Value}")],
                Policies = [.. results],
                Reading = ReadPolicies(target, results),
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    // -- Sources ---------------------------------------------------------------

    /// <summary>The canonical source name, or null when it is not one of <see cref="Sources"/>.</summary>
    public static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return "photo";

        var key = source.Trim().ToLowerInvariant();

        return key switch
        {
            "photo" or "photograph" or "camera" or "picture" => "photo",
            "screenshot" or "screen" or "ui" or "diagram" => "screenshot",
            "graphic" or "logo" or "transparent" or "icon" => "graphic",
            _ => null
        };
    }

    /// <summary>Draws the test image. The caller owns the buffer.</summary>
    public static ImageBuffer CreateSource(string kind, int width, int height) => kind switch
    {
        "screenshot" => Screenshot(width, height),
        "graphic" => Graphic(width, height),
        _ => Photo(width, height)
    };

    /// <summary>
    /// The demo's sunset scene with sensor grain on top. Without the grain a vector drawing compresses
    /// an order of magnitude better than any photograph, and the byte counts would mislead.
    /// </summary>
    private static ImageBuffer Photo(int width, int height)
    {
        using var scene = SampleImages.Landscape(width, height);

        return ImagePipeline.Create().Noise(0.04f, monochrome: false, seed: 7).Execute(scene, new PipelineExecutionOptions { YieldBetweenOperations = false });
    }

    /// <summary>A light-theme app window: flat fills, hard edges and rows of text-like glyph runs.</summary>
    private static ImageBuffer Screenshot(int width, int height)
    {
        using var canvas = ImageCanvas.Create(width, height, new Rgba32(246, 247, 249));
        var random = new Random(42);
        var unit = Math.Max(1f, height / 100f);

        canvas.FillRectangle(new RectangleF(0, 0, width, unit * 6), new Rgba32(33, 37, 48));
        canvas.FillRectangle(new RectangleF(0, unit * 6, width * 0.2f, height), new Rgba32(232, 235, 240));

        for (var i = 0; i < 8; i++)
        {
            canvas.FillRoundedRectangle(new RectangleF(unit * 2, unit * (10 + i * 5), width * 0.2f - unit * 4, unit * 3), unit, i == 1 ? new Rgba32(105, 65, 198, 60) : new Rgba32(210, 214, 222));
        }

        // Paragraphs of "text": runs of short dark blocks, the way glyphs read to an encoder.
        var left = width * 0.24f;
        for (var y = unit * 12; y < height * 0.62f; y += unit * 2.6f)
        {
            var x = left;
            var lineEnd = left + (width * 0.7f) * (0.55f + (float)random.NextDouble() * 0.45f);
            while (x < lineEnd)
            {
                var word = unit * (1.5f + (float)random.NextDouble() * 4);
                canvas.FillRectangle(new RectangleF(x, y, word, unit * 1.3f), new Rgba32(52, 56, 66));
                x += word + unit * 0.8f;
            }
        }

        // A button, a card and a bar chart: the flat colour areas PNG is good at.
        canvas.FillRoundedRectangle(new RectangleF(left, height * 0.66f, unit * 16, unit * 5), unit * 1.2f, new Rgba32(105, 65, 198));
        canvas.FillRoundedRectangle(new RectangleF(width * 0.55f, height * 0.66f, width * 0.4f, height * 0.3f), unit, Rgba32.White);
        for (var i = 0; i < 7; i++)
        {
            var bar = height * 0.05f + (float)random.NextDouble() * height * 0.2f;
            canvas.FillRectangle(new RectangleF(width * 0.57f + i * width * 0.05f, height * 0.94f - bar, width * 0.035f, bar), new Rgba32(66, 133, 244));
        }

        return canvas.Snapshot();
    }

    /// <summary>A logo on a transparent background: flat shapes, a gradient and anti-aliased edges.</summary>
    private static ImageBuffer Graphic(int width, int height)
    {
        using var canvas = ImageCanvas.Create(width, height);
        var cx = width / 2f;
        var cy = height / 2f;
        var radius = Math.Min(width, height) * 0.42f;

        canvas.FillPath(new PathBuilder().AddEllipse(new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2)).Build(),
            new LinearGradientPaint(new Vector2(cx - radius, cy - radius), new Vector2(cx + radius, cy + radius),
                [(0f, new Rgba32(124, 58, 237)), (1f, new Rgba32(37, 99, 235))]));
        canvas.FillPolygon(
        [
            new(cx - radius * 0.45f, cy + radius * 0.35f), new(cx, cy - radius * 0.55f), new(cx + radius * 0.45f, cy + radius * 0.35f),
        ], Rgba32.White);
        canvas.FillCircle(new Vector2(cx, cy + radius * 0.05f), radius * 0.16f, new Rgba32(124, 58, 237));

        return canvas.Snapshot();
    }

    // -- Measuring -------------------------------------------------------------

    private static async Task<(ImageBuffer Result, double Milliseconds)> TimeAsync(Func<ValueTask<ImageBuffer>> run)
    {
        var watch = Stopwatch.StartNew();
        var result = await run();

        return (result, watch.Elapsed.TotalMilliseconds);
    }

    private static async Task<BmageEncodingDto[]> EncodeAllAsync(ImageBuffer output, ImageFormat[] formats, double[] qualities, bool transparent, CancellationToken cancellationToken)
    {
        var results = new List<BmageEncodingDto>();
        var pixels = (double)output.PixelCount;

        foreach (var format in formats)
        {
            if (format is ImageFormat.WebP or ImageFormat.Avif)
            {
                var jpeg = results.FirstOrDefault(result => result.Format == "Jpeg" && result.Bytes is not null && result.Quality is >= 0.75 and <= 0.85);

                results.Add(new BmageEncodingDto
                {
                    Format = format.ToString(),
                    Note = $"Not measured: {format} is a browser codec, and this server runs the managed ones a headless host has. " +
                           (format == ImageFormat.WebP
                               ? "Bmage's formats guide puts WebP at 0.8 roughly 25 to 35% smaller than JPEG at similar quality" +
                                 (jpeg is not null ? $" - around {jpeg.Bytes * 0.65:N0} to {jpeg.Bytes * 0.75:N0} bytes against the JPEG at {jpeg.Quality:0.##} here." : ".") +
                                 " Most browsers can encode it; export with EncodeWithFallbackAsync([WebP, Jpeg])."
                               : "Few browsers can encode AVIF at all; always pair it with a fallback.")
                });

                continue;
            }

            if (format == ImageFormat.Gif)
            {
                results.Add(new BmageEncodingDto { Format = "Gif", Note = "Not measured: GIF encoding is not implemented by Bmage or by any browser canvas. Use PNG or WebP." });
                continue;
            }

            foreach (var quality in format.IsLossy() ? qualities : [double.NaN])
            {
                var options = new ImageExportOptions { Format = format, Quality = double.IsNaN(quality) ? 0.85 : quality };
                var watch = Stopwatch.StartNew();
                var encoded = await _processor.EncodeAsync(output, options, cancellationToken);
                var ms = watch.Elapsed.TotalMilliseconds;

                results.Add(new BmageEncodingDto
                {
                    Format = format.ToString(),
                    Quality = double.IsNaN(quality) ? null : quality,
                    Bytes = encoded.Length,
                    BitsPerPixel = Math.Round(encoded.Length * 8 / pixels, 3),
                    EncodeMilliseconds = Math.Round(ms, 1),
                    Note = transparent && format.SupportsAlpha() is false
                        ? $"{format} has no alpha channel: the transparent pixels were flattened onto ImageExportOptions.Background (white by default)."
                        : format == ImageFormat.Png ? "Lossless, compression level 6 (the default); level 1 encodes about 3.4 times faster for a larger file." : null
                });
            }
        }

        return [.. results];
    }

    private static async Task<BmageBudgetDto[]> RunBudgetsAsync(ImageBuffer output, ImageFormat[] formats, double[] qualities, long budget, List<string> warnings, CancellationToken cancellationToken)
    {
        if (budget <= 0)
        {
            warnings.Add("maxFileSize has to be a positive number of bytes; the budget search was skipped.");
            return [];
        }

        var results = new List<BmageBudgetDto>();

        foreach (var format in formats.Where(format => format is ImageFormat.Jpeg or ImageFormat.Png))
        {
            try
            {
                var encoded = await _processor.EncodeAsync(output, new ImageExportOptions
                {
                    Format = format,
                    // The search starts at the highest quality asked about, as real code passes its preferred quality.
                    Quality = qualities.Max(),
                    MaxFileSize = budget,
                }, cancellationToken);

                results.Add(new BmageBudgetDto
                {
                    Format = format.ToString(),
                    MaxFileSize = budget,
                    Met = true,
                    Bytes = encoded.Length,
                    Quality = format.IsLossy() ? Math.Round(encoded.Quality ?? 0, 3) : null,
                    Width = encoded.Width,
                    Height = encoded.Height,
                });
            }
            catch (ImageEncodeException ex)
            {
                results.Add(new BmageBudgetDto { Format = format.ToString(), MaxFileSize = budget, Met = false, Error = ex.Message });
            }
        }

        if (results.Count == 0) warnings.Add("The budget search runs for jpeg and png here; add one of them to 'formats' to run it.");

        return [.. results];
    }

    private static bool SameOperations(ImagePipeline a, ImagePipeline b)
        => a.Count == b.Count && a.Zip(b).All(pair => ReferenceEquals(pair.First, pair.Second));

    private static string[] Describe(ImagePipeline pipeline) => [.. pipeline.Select(operation => operation.ToString() ?? operation.Name)];

    /// <summary>What the optimiser changed, read off the two operation lists - and what stopped it.</summary>
    private static string[] OptimizerNotes(ImagePipeline original, ImagePipeline optimized)
    {
        var notes = new List<string>();

        var identities = original.Where(operation => operation.IsIdentity).ToArray();
        if (identities.Length > 0)
        {
            notes.Add($"Dropped {identities.Length} operation(s) that cannot change a pixel: {string.Join(", ", identities.Select(op => op.ToString()))}.");
        }

        foreach (var fused in optimized.Where(operation => operation.GetType().Name.Contains("Fused", StringComparison.Ordinal)))
        {
            notes.Add($"Fused into a single pass over the pixels: {fused}.");
        }

        if (optimized.OfType<ResizeCropOperation>().Any())
        {
            notes.Add("Fused a resize with the crop after it: only the surviving region is resampled, bit-identical to resizing everything and cropping.");
        }

        var remaining = original.Count - identities.Length;
        if (optimized.Count < remaining && notes.Count == (identities.Length > 0 ? 1 : 0))
        {
            notes.Add($"Merged {remaining} steps into {optimized.Count} (consecutive crops, orientations or resizes).");
        }

        // AutoOrient's output size depends on metadata, so nothing after it can be planned.
        var autoOrient = original.ToList().FindIndex(operation => operation is AutoOrientOperation);
        if (autoOrient >= 0)
        {
            for (var i = autoOrient + 1; i + 1 < original.Count; i++)
            {
                if (original[i] is ResizeOperation && original[i + 1] is CropOperation && optimized.OfType<ResizeCropOperation>().Any() is false)
                {
                    notes.Add("The resize and the crop after AutoOrient were not fused: AutoOrient's output size depends on the image's EXIF " +
                              "orientation, so the optimiser cannot plan past it. An image from ImageLoader is already upright " +
                              "(DecodeOptions.AutoOrient is on by default), so on those the step can go - and the fusion is about 2.4 times faster.");
                    break;
                }
            }
        }

        // Per-pixel runs separated by geometry cannot fuse.
        var runs = 0;
        var inRun = false;
        foreach (var operation in original.Where(operation => operation.IsIdentity is false))
        {
            var point = operation is PointOperation;
            if (point && inRun is false) runs++;
            inRun = point;
        }

        if (runs > 1)
        {
            notes.Add($"The per-pixel adjustments form {runs} separate runs because geometric steps sit between them; grouped together " +
                      "they would fuse into one pass (about 1.55 times faster than separate passes).");
        }

        if (notes.Count == 0) notes.Add("The optimiser had nothing to rewrite in this pipeline; it runs as written.");

        return [.. notes];
    }

    private static string Read(string kind, ImageBuffer output, double executeMs, double unoptimizedMs, BmageEncodingDto[] encodings, BmageBudgetDto[]? budgets)
    {
        var parts = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"The pipeline produced {output.Width} x {output.Height} ({output.PixelCount / 1_000_000.0:0.##} MP) from the generated {kind} " +
                $"in {executeMs:0.#} ms on this server's desktop .NET.")
        };

        if (unoptimizedMs > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture,
                $"Without the optimiser it took {unoptimizedMs:0.#} ms ({(executeMs > 0 ? unoptimizedMs / executeMs : 1):0.##} times as long)."));
        }

        parts.Add("In a browser, expect 1.2 to 2.4 times these timings with RunAOTCompilation and 10 to 35 times without it.");

        if (IsDebugBuild)
        {
            parts.Add("This server runs a Debug build of Bmage, so its timings are several times slower than a Release build's: " +
                      "compare them with each other, not with the published benchmarks.");
        }

        var measured = encodings.Where(encoding => encoding.Bytes is not null).ToArray();

        if (measured.Length > 0)
        {
            var smallest = measured.MinBy(encoding => encoding.Bytes)!;
            parts.Add(string.Create(CultureInfo.InvariantCulture,
                $"The smallest file was {smallest.Format}{(smallest.Quality is { } q ? $" at {q:0.##}" : string.Empty)}: {smallest.Bytes:N0} bytes ({smallest.BitsPerPixel:0.##} bits per pixel)."));

            var jpegs = measured.Where(encoding => encoding.Format == "Jpeg").OrderBy(encoding => encoding.Quality).ToArray();
            if (jpegs.Length > 1)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture,
                    $"JPEG went from {jpegs[0].Bytes:N0} bytes at {jpegs[0].Quality:0.##} to {jpegs[^1].Bytes:N0} at {jpegs[^1].Quality:0.##}."));
            }

            var png = measured.FirstOrDefault(encoding => encoding.Format == "Png");
            var jpeg = jpegs.LastOrDefault();
            if (png is not null && jpeg is not null)
            {
                parts.Add(png.Bytes < jpeg.Bytes
                    ? "PNG beat JPEG here, which is what flat colour and hard edges do - use PNG (or WebP lossless) for this kind of image."
                    : "PNG is larger than JPEG here, as it is for photographic content - PNG only earns its size for transparency or flat graphics.");
            }
        }

        foreach (var budget in budgets ?? [])
        {
            parts.Add(budget.Met
                ? string.Create(CultureInfo.InvariantCulture,
                    $"A {budget.MaxFileSize:N0}-byte budget in {budget.Format} was met at {budget.Bytes:N0} bytes{(budget.Quality is { } q ? $", quality {q:0.##}" : string.Empty)}, " +
                    $"{budget.Width} x {budget.Height}{(budget.Width < output.Width ? " - smaller than the output, so quality alone could not reach it" : string.Empty)}.")
                : $"A {budget.MaxFileSize:N0}-byte budget in {budget.Format} cannot be met: {budget.Error}");
        }

        parts.Add("The test image is generated, so absolute sizes are indicative; the ratios between formats, qualities and settings carry over to real images of the same kind.");

        return string.Join(' ', parts);
    }

    private static string ReadPolicies(ImageFormat format, List<BmageMetadataPolicyDto> results)
    {
        var parts = results.Select(result =>
            $"{result.Policy}: {(result.LeaksLocation ? "the GPS position IS still in the file" : "no location")}, " +
            $"{(result.LeaksIdentity ? "identifying fields kept" : "no identifying fields")}, {result.Kept.Length} field(s) kept, {result.Bytes:N0} bytes.");

        return $"Encoded as {format} by the real encoder and read back: {string.Join(' ', parts)} " +
               "Remove is the default and the right choice for uploads and avatars; StripSensitive keeps the camera data a gallery " +
               "shows; Preserve publishes where the photo was taken.";
    }

    // -- Arguments -------------------------------------------------------------

    private static (int Width, int Height) ClampSize(int width, int height, List<string> warnings)
    {
        var w = width <= 0 ? DefaultWidth : width;
        var h = height <= 0 ? DefaultHeight : height;

        var clampedW = Math.Clamp(w, 16, MaxEdge);
        var clampedH = Math.Clamp(h, 16, MaxEdge);

        if ((long)clampedW * clampedH > MaxPixels)
        {
            var scale = Math.Sqrt(MaxPixels / ((double)clampedW * clampedH));
            clampedW = (int)(clampedW * scale);
            clampedH = (int)(clampedH * scale);
        }

        if (clampedW != w || clampedH != h)
        {
            warnings.Add($"The source was {w} x {h}; it was measured at {clampedW} x {clampedH} instead, the most this shared server " +
                         $"generates ({MaxEdge}px per side, {MaxPixels / 1_000_000} MP). Per-pixel costs and bits per pixel scale with it.");
        }

        return (clampedW, clampedH);
    }

    private static (ImageFormat[] Formats, string[] Warnings) ParseFormats(string? formats)
    {
        if (string.IsNullOrWhiteSpace(formats)) return ([ImageFormat.Jpeg, ImageFormat.Png], []);

        var warnings = new List<string>();
        var result = new List<ImageFormat>();

        foreach (var name in formats.Split([',', ';', ' ', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var format = ParseFormat(name);

            if (format == ImageFormat.Unknown)
            {
                warnings.Add($"'{name}' is not a format; it was skipped. Use jpeg, png, bmp, webp or avif.");
                continue;
            }

            if (result.Contains(format) is false) result.Add(format);
        }

        if (result.Count > MaxFormats)
        {
            warnings.Add($"Only the first {MaxFormats} formats were measured.");
            result = result[..MaxFormats];
        }

        return (result.Count == 0 ? [ImageFormat.Jpeg, ImageFormat.Png] : [.. result], [.. warnings]);
    }

    private static ImageFormat ParseFormat(string? name) => (name ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant() switch
    {
        "jpeg" or "jpg" => ImageFormat.Jpeg,
        "png" => ImageFormat.Png,
        "bmp" => ImageFormat.Bmp,
        "webp" => ImageFormat.WebP,
        "avif" => ImageFormat.Avif,
        "gif" => ImageFormat.Gif,
        _ => ImageFormat.Unknown
    };

    private static (double[] Qualities, string[] Warnings) ParseQualities(string? qualities)
    {
        if (string.IsNullOrWhiteSpace(qualities)) return ([0.7, 0.8, 0.9], []);

        var warnings = new List<string>();
        var result = new List<double>();

        foreach (var text in qualities.Split([',', ';', ' ', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) is false)
            {
                warnings.Add($"'{text}' is not a quality; it was skipped. Qualities are numbers in [0, 1], e.g. 0.8.");
                continue;
            }

            // 80 means 0.8: a quality written as a percentage is the commonest slip there is.
            if (value > 1 && value <= 100) value /= 100;

            if (value is <= 0 or > 1)
            {
                warnings.Add($"{text} is outside (0, 1]; it was skipped.");
                continue;
            }

            if (result.Contains(value) is false) result.Add(value);
        }

        if (result.Count > MaxQualities)
        {
            warnings.Add($"Only the first {MaxQualities} qualities were measured.");
            result = result[..MaxQualities];
        }

        return (result.Count == 0 ? [0.7, 0.8, 0.9] : [.. result.Order()], [.. warnings]);
    }

    private static BmagePipelineRunDto Failed(string? spec, string? kind, int width, int height, string error, List<string> warnings) => new()
    {
        Pipeline = spec ?? string.Empty,
        Operations = [],
        OptimizedOperations = [],
        OptimizerNotes = [],
        Source = kind ?? "photo",
        SourceWidth = width,
        SourceHeight = height,
        PredictedOutputSize = string.Empty,
        OutputWidth = 0,
        OutputHeight = 0,
        ExecuteMilliseconds = 0,
        OutputHasTransparency = false,
        Encodings = [],
        Reading = string.Empty,
        Warnings = [.. warnings],
        Error = error
    };

    // -- The test photo's metadata --------------------------------------------

    /// <summary>One field of the report: how to read it out of EXIF.</summary>
    private sealed record Field(string Label, Func<ExifData, string?> Read);

    private static readonly Field[] Fields =
    [
        new("Camera", e => $"{e.Make} {e.Model}".Trim() is { Length: > 0 } c ? c : null),
        new("Lens", e => e.LensModel),
        new("ISO", e => e.Iso?.ToString(CultureInfo.InvariantCulture)),
        new("Taken", e => e.DateTaken?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
        new("Copyright", e => e.Copyright),
        new("Serial number", e => e.Exif.Get(ExifTag.BodySerialNumber)?.GetString()),
        new("Owner", e => e.Exif.Get(ExifTag.OwnerName)?.GetString()),
        new("Location", e => e.Location?.ToString()),
    ];

    /// <summary>What a phone writes into a photo: who made the camera, how it was exposed, when, whose it is and where it was.</summary>
    private static ExifData SampleExif() => new ExifData()
        .WithPrimary(new ExifDirectory()
            .With(ExifTag.Make, ExifValue.FromString("Bmage"))
            .With(ExifTag.Model, ExifValue.FromString("Demo Phone 15"))
            .With(ExifTag.Copyright, ExifValue.FromString("© 2026 Sam Rivera")))
        .WithExif(new ExifDirectory()
            .With(ExifTag.DateTimeOriginal, ExifValue.FromString("2026:06:21 19:42:07"))
            .With(ExifTag.Iso, ExifValue.FromShort(125))
            .With(ExifTag.LensModel, ExifValue.FromString("Demo Phone 15 back camera 6.9mm f/1.8"))
            .With(ExifTag.BodySerialNumber, ExifValue.FromString("DP15-0042-7731"))
            .With(ExifTag.OwnerName, ExifValue.FromString("Sam Rivera")))
        .WithGps(new ExifDirectory()
            .With(ExifTag.GpsLatitudeRef, ExifValue.FromString("N"))
            .With(ExifTag.GpsLatitude, ExifValue.FromRational((53, 1), (12, 1), (3720, 100)))
            .With(ExifTag.GpsLongitudeRef, ExifValue.FromString("E"))
            .With(ExifTag.GpsLongitude, ExifValue.FromRational((5, 1), (47, 1), (5280, 100))));
}
