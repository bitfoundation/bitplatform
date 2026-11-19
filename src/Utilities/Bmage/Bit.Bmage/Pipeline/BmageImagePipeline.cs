using System.Collections;
using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// An immutable, ordered list of operations describing a complete image transformation. Pipelines are values: every
/// builder method returns a new pipeline, so one can be created once and reused across images and threads.
/// Nothing executes until <see cref="ExecuteAsync"/> (or a synchronous <see cref="Execute"/>) is called.
/// </summary>
public sealed class BmageImagePipeline : IReadOnlyList<IBmageImageOperation>
{
    private readonly ImmutableArray<IBmageImageOperation> _operations;

    private BmageImagePipeline(ImmutableArray<IBmageImageOperation> operations) => _operations = operations;

    /// <summary>An empty pipeline.</summary>
    public static BmageImagePipeline Empty { get; } = new([]);

    /// <summary>Creates an empty pipeline to build on.</summary>
    public static BmageImagePipeline Create() => Empty;

    /// <summary>Creates a pipeline from an existing sequence of operations.</summary>
    public static BmageImagePipeline From(IEnumerable<IBmageImageOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var array = operations.ToImmutableArray();
        for (var i = 0; i < array.Length; i++)
            if (array[i] is null) throw new ArgumentException($"Operation at index {i} is null.", nameof(operations));
        return new BmageImagePipeline(array);
    }

    public int Count => _operations.Length;
    public IBmageImageOperation this[int index] => _operations[index];
    public bool IsEmpty => _operations.IsEmpty;
    public IEnumerator<IBmageImageOperation> GetEnumerator() => ((IEnumerable<IBmageImageOperation>)_operations).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Appends an operation.</summary>
    public BmageImagePipeline Add(IBmageImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new BmageImagePipeline(_operations.Add(operation));
    }

    /// <summary>Appends several operations.</summary>
    public BmageImagePipeline AddRange(IEnumerable<IBmageImageOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        return Concat(From(operations));
    }

    /// <summary>Inserts an operation at a position.</summary>
    public BmageImagePipeline Insert(int index, IBmageImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new BmageImagePipeline(_operations.Insert(index, operation));
    }

    /// <summary>Removes the operation at a position.</summary>
    public BmageImagePipeline RemoveAt(int index) => new(_operations.RemoveAt(index));

    /// <summary>Replaces the operation at a position; used for re-editing a previous step non-destructively.</summary>
    public BmageImagePipeline Replace(int index, IBmageImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Replacing a step with itself changes nothing, and keeping the instance lets renders recognise the edit.
        if ((uint)index < (uint)_operations.Length && ReferenceEquals(_operations[index], operation)) return this;
        return new BmageImagePipeline(_operations.SetItem(index, operation));
    }

    /// <summary>Appends every operation from another pipeline.</summary>
    public BmageImagePipeline Concat(BmageImagePipeline other) => other is null ? this : new BmageImagePipeline(_operations.AddRange(other._operations));

    // ---- Fluent operation builders ----

    /// <summary>Applies the EXIF orientation so the image appears upright.</summary>
    public BmageImagePipeline AutoOrient() => Add(BmageAutoOrientOperation.Instance);

    /// <summary>Crops to a rectangle in image coordinates.</summary>
    public BmageImagePipeline Crop(Rectangle rectangle) => Add(new BmageCropOperation(rectangle));

    /// <summary>Crops to a rectangle in image coordinates.</summary>
    public BmageImagePipeline Crop(int x, int y, int width, int height) => Add(new BmageCropOperation(x, y, width, height));

    /// <summary>Crops to the largest centred region with the given aspect ratio.</summary>
    public BmageImagePipeline CropToAspect(BmageAspectRatio ratio, BmageAnchor anchor = BmageAnchor.Center) => Add(new BmageAspectCropOperation(ratio, anchor));

    /// <summary>
    /// Crops to the given aspect ratio at the position that looks most interesting, rather than a fixed anchor. Avoids
    /// the classic failure of centre-cropping a portrait and cutting off the top of a head.
    /// </summary>
    public BmageImagePipeline SmartCrop(BmageAspectRatio ratio, BmageSmartCropOptions? options = null) => Add(new BmageSmartCropOperation(ratio, options));

    /// <summary>Resizes the image.</summary>
    public BmageImagePipeline Resize(int? width = null, int? height = null, BmageResizeMode mode = BmageResizeMode.Fit, BmageResamplingFilter filter = BmageResamplingFilter.Auto, BmageAnchor anchor = BmageAnchor.Center, BmageRgba32 background = default)
        => Add(new BmageResizeOperation(width, height, mode, filter, anchor, background));

    /// <summary>Shrinks the image so neither dimension exceeds the given bounds. Never enlarges.</summary>
    public BmageImagePipeline MaxSize(int maxWidth, int maxHeight, BmageResamplingFilter filter = BmageResamplingFilter.Auto)
        => Add(new BmageResizeOperation(maxWidth, maxHeight, BmageResizeMode.Max, filter));

    /// <summary>Scales the image so it covers the target size, cropping the overflow.</summary>
    public BmageImagePipeline Cover(int width, int height, BmageAnchor anchor = BmageAnchor.Center, BmageResamplingFilter filter = BmageResamplingFilter.Auto)
        => Add(new BmageResizeOperation(width, height, BmageResizeMode.Cover, filter, anchor));

    /// <summary>Rotates clockwise by an arbitrary angle.</summary>
    public BmageImagePipeline Rotate(double degrees, BmageRgba32 background = default, bool expandCanvas = true) => Add(new BmageRotateOperation(degrees, background, expandCanvas));

    /// <summary>Applies one of the eight lossless orientations.</summary>
    public BmageImagePipeline Orient(BmageOrientation orientation) => Add(new BmageOrientationOperation(orientation));

    /// <summary>Mirrors horizontally.</summary>
    public BmageImagePipeline FlipHorizontal() => Add(BmageOrientationOperation.FlipHorizontal);

    /// <summary>Mirrors vertically.</summary>
    public BmageImagePipeline FlipVertical() => Add(BmageOrientationOperation.FlipVertical);

    /// <summary>Shears the image by the given angles in degrees.</summary>
    public BmageImagePipeline Skew(double degreesX, double degreesY, BmageRgba32 background = default) => Add(new BmageSkewOperation(degreesX, degreesY, background));

    /// <summary>
    /// Levels a tilt of up to 45 degrees either way, cropping to the largest rectangle of the image's aspect ratio the
    /// rotated image covers, so no corner is left empty.
    /// </summary>
    public BmageImagePipeline Straighten(double degrees) => Add(new BmageStraightenOperation(degrees));

    /// <summary>
    /// Sets the canvas to an absolute size without scaling the image, placing it by <paramref name="anchor"/>: the new
    /// area is filled with <paramref name="background"/>, and an image larger than the canvas is cut at its edges.
    /// </summary>
    public BmageImagePipeline CanvasSize(int width, int height, BmageAnchor anchor = BmageAnchor.Center, BmageRgba32 background = default)
        => Add(new BmageCanvasSizeOperation(width, height, anchor, background));

    /// <summary>Adds a border of <paramref name="all"/> pixels on every side, filled with <paramref name="background"/>.</summary>
    public BmageImagePipeline Pad(int all, BmageRgba32 background = default) => Add(BmageCanvasSizeOperation.Margins(all, background));

    /// <summary>Adds margins to each side, filled with <paramref name="background"/>. A negative margin cuts into the image.</summary>
    public BmageImagePipeline Pad(int left, int top, int right, int bottom, BmageRgba32 background = default)
        => Add(BmageCanvasSizeOperation.Margins(left, top, right, bottom, background));

    /// <summary>Adds a constant to the RGB channels. Amount in [-1,1].</summary>
    public BmageImagePipeline Brightness(float amount) => Add(new BmageBrightnessAdjustment(amount));

    /// <summary>Scales contrast around mid grey. Amount in [-1,1].</summary>
    public BmageImagePipeline Contrast(float amount) => Add(new BmageContrastAdjustment(amount));

    /// <summary>Adjusts saturation. Amount in [-1,1].</summary>
    public BmageImagePipeline Saturation(float amount) => Add(new BmageSaturationAdjustment(amount));

    /// <summary>Rotates hue by degrees.</summary>
    public BmageImagePipeline Hue(float degrees) => Add(new BmageHueAdjustment(degrees));

    /// <summary>Applies exposure compensation in stops.</summary>
    public BmageImagePipeline Exposure(float ev) => Add(new BmageExposureAdjustment(ev));

    /// <summary>Applies a gamma curve.</summary>
    public BmageImagePipeline Gamma(float gamma) => Add(new BmageGammaAdjustment(gamma));

    /// <summary>Warms (positive) or cools (negative) the image. Amount in [-1,1].</summary>
    public BmageImagePipeline Temperature(float amount) => Add(new BmageTemperatureAdjustment(amount));

    /// <summary>Shifts towards magenta (positive) or green (negative). Amount in [-1,1].</summary>
    public BmageImagePipeline Tint(float amount) => Add(new BmageTintAdjustment(amount));

    /// <summary>Multiplies the alpha channel. Opacity in [0,1].</summary>
    public BmageImagePipeline Opacity(float opacity) => Add(new BmageOpacityAdjustment(opacity));

    /// <summary>Boosts muted colours without over-saturating already vivid ones.</summary>
    public BmageImagePipeline Vibrance(float amount) => Add(new BmageVibranceAdjustment(amount));

    /// <summary>Moves the brightest and darkest tones, each amount in [-1,1]: where highlights clip to white and shadows to black.</summary>
    public BmageImagePipeline WhitesBlacks(float whites, float blacks) => Add(new BmageWhitesBlacksAdjustment(whites, blacks));

    /// <summary>Changes the hue (degrees), saturation and lightness ([-1,1]) of one band of colours, leaving the others.</summary>
    public BmageImagePipeline Hsl(BmageHueBand band, float hue = 0f, float saturation = 0f, float lightness = 0f)
        => Add(new BmageHslAdjustment().With(band, new BmageHslShift(hue, saturation, lightness)));

    /// <summary>Makes <paramref name="neutral"/>, a colour that should be grey, grey: a white balance from a picked colour.</summary>
    public BmageImagePipeline WhiteBalance(BmageRgba32 neutral, float strength = 1f) => Add(BmageWhiteBalanceAdjustment.FromNeutral(neutral, strength));

    /// <summary>Removes a colour cast by making the average of the mid-tones grey, by <paramref name="strength"/> in [0,1].</summary>
    public BmageImagePipeline AutoWhiteBalance(float strength = 0.8f) => Add(new BmageAutoWhiteBalanceOperation(strength));

    /// <summary>Mixes each output channel from the input channels with the given weights.</summary>
    public BmageImagePipeline ChannelMixer(Vector3 red, Vector3 green, Vector3 blue) => Add(new BmageChannelMixerAdjustment(red, green, blue));

    /// <summary>A black and white conversion mixing the channels in the given proportions.</summary>
    public BmageImagePipeline BlackAndWhite(float red = 0.3f, float green = 0.59f, float blue = 0.11f) => Add(BmageChannelMixerAdjustment.Monochrome(red, green, blue));

    /// <summary>Gives the image one hue (degrees) and saturation, keeping its lightness: a tint of any colour.</summary>
    public BmageImagePipeline Colorize(float hue, float saturation = 0.25f, float lightness = 0f, float amount = 1f)
        => Add(new BmageColorizeAdjustment(hue, saturation, lightness, amount));

    /// <summary>Maps the shadows to <paramref name="shadows"/> and the highlights to <paramref name="highlights"/>.</summary>
    public BmageImagePipeline Duotone(BmageRgba32 shadows, BmageRgba32 highlights, float amount = 1f) => Add(BmageGradientMapOperation.Duotone(shadows, highlights, amount));

    /// <summary>Lifts shadows and recovers highlights. Both amounts in [-1,1].</summary>
    public BmageImagePipeline ShadowsHighlights(float shadows, float highlights) => Add(new BmageShadowsHighlightsAdjustment(shadows, highlights));

    /// <summary>Converts towards grayscale.</summary>
    public BmageImagePipeline Grayscale(float amount = 1f) => Add(new BmageGrayscaleFilter(amount));

    /// <summary>Applies a sepia tone.</summary>
    public BmageImagePipeline Sepia(float amount = 1f) => Add(new BmageSepiaFilter(amount));

    /// <summary>Inverts colours.</summary>
    public BmageImagePipeline Invert(float amount = 1f) => Add(new BmageInvertFilter(amount));

    /// <summary>Reduces each channel to a fixed number of levels.</summary>
    public BmageImagePipeline Posterize(int levels) => Add(new BmagePosterizeFilter(levels));

    /// <summary>Converts to pure black and white at the given luminance level.</summary>
    public BmageImagePipeline Threshold(float level = 0.5f) => Add(new BmageThresholdFilter(level));

    /// <summary>
    /// Converts to black and white by comparing each pixel with its surroundings, the window of <c>2 × radius + 1</c>
    /// pixels around it: a page lit unevenly, a receipt, a barcode on a glossy label. See <see cref="BmageAdaptiveThresholdFilter"/>.
    /// </summary>
    public BmageImagePipeline AdaptiveThreshold(float radius = 16, float bias = 0.2f, BmageAdaptiveThresholdMethod method = BmageAdaptiveThresholdMethod.Sauvola, Rectangle? region = null)
        => Add(new BmageAdaptiveThresholdFilter(radius, bias, method, region));

    /// <summary>Applies a gaussian blur, optionally limited to a region.</summary>
    public BmageImagePipeline Blur(float radius, Rectangle? region = null) => Add(new BmageBlurFilter(radius, region));

    /// <summary>Applies unsharp-mask sharpening.</summary>
    public BmageImagePipeline Sharpen(float amount = 0.5f, float radius = 1f, float threshold = 0f, Rectangle? region = null) => Add(new BmageSharpenFilter(amount, radius, threshold, region));

    /// <summary>Replaces blocks of pixels with their average colour.</summary>
    public BmageImagePipeline Pixelate(int blockSize, Rectangle? region = null) => Add(new BmagePixelateFilter(blockSize, region));

    /// <summary>Adds deterministic noise.</summary>
    public BmageImagePipeline Noise(float amount, bool monochrome = false, int seed = 0, Rectangle? region = null) => Add(new BmageNoiseFilter(amount, monochrome, seed, region));

    /// <summary>
    /// Blurs along a line <paramref name="distance"/> pixels long at <paramref name="angle"/> degrees (0 horizontal, 90
    /// vertical), as motion during an exposure does.
    /// </summary>
    public BmageImagePipeline MotionBlur(float distance, float angle = 0, Rectangle? region = null) => Add(new BmageMotionBlurFilter(distance, angle, region));

    /// <summary>
    /// Blurs towards (<see cref="BmageRadialBlurMode.Zoom"/>) or around (<see cref="BmageRadialBlurMode.Spin"/>) a centre, given as
    /// fractions of the width and height, leaving the centre sharp. <paramref name="amount"/> is in [0,1].
    /// </summary>
    public BmageImagePipeline RadialBlur(float amount, BmageRadialBlurMode mode = BmageRadialBlurMode.Zoom, PointF? center = null, Rectangle? region = null)
        => Add(new BmageRadialBlurFilter(amount, mode, center, region));

    /// <summary>Keeps only detail finer than <paramref name="radius"/>, around mid grey: edges and texture.</summary>
    public BmageImagePipeline HighPass(float radius = 3f, Rectangle? region = null) => Add(new BmageHighPassFilter(radius, region));

    /// <summary>Applies a median filter for noise reduction.</summary>
    public BmageImagePipeline DenoiseMedian(int radius = 1, Rectangle? region = null) => Add(new BmageMedianFilter(radius, region));

    /// <summary>Darkens the corners.</summary>
    public BmageImagePipeline Vignette(float amount = 0.5f, float radius = 0.75f, float softness = 0.5f) => Add(new BmageVignetteFilter(amount, radius, softness));

    /// <summary>
    /// Stretches the tonal range so the darkest pixels become black and the brightest become white. The most useful
    /// single automatic improvement for an under-exposed or flat photo.
    /// </summary>
    public BmageImagePipeline AutoLevels(double clipFraction = 0.005, bool perChannel = false, float strength = 1f)
        => Add(new BmageAutoLevelsOperation(clipFraction, perChannel, strength));

    /// <summary>Moves the mean luminance towards a target. Gentler than <see cref="AutoLevels"/>.</summary>
    public BmageImagePipeline AutoContrast(float targetMean = 0.5f, float strength = 0.7f)
        => Add(new BmageAutoContrastOperation(targetMean, strength));

    /// <summary>Applies an arbitrary colour matrix.</summary>
    public BmageImagePipeline ColorMatrix(BmageColorMatrix matrix, string? name = null) => Add(new BmageColorMatrixOperation(matrix, name));

    /// <summary>
    /// Maps every colour through a three-dimensional lookup table, such as a film look read from a <c>.cube</c> file
    /// with <see cref="BmageColorLookupTable.ParseCube"/>. <paramref name="strength"/> blends the result with the original.
    /// </summary>
    public BmageImagePipeline ColorLut(BmageColorLookupTable table, float strength = 1f) => Add(new BmageColorLutAdjustment(table, strength));

    /// <summary>Applies a custom per-pixel operation.</summary>
    public BmageImagePipeline Apply(IBmageImageOperation operation) => Add(operation);

    // ---- Masks ----

    /// <summary>
    /// Applies <paramref name="operation"/> only where <paramref name="mask"/> covers the image, fading with its
    /// coverage. The operation must keep the image's size.
    /// </summary>
    public BmageImagePipeline Masked(BmageMask mask, IBmageImageOperation operation) => Add(new BmageMaskedOperation(operation, mask));

    /// <summary>
    /// Applies the operations <paramref name="edit"/> adds only where <paramref name="mask"/> covers the image: a
    /// selective adjustment. Adjustments run in one pass, and only over the rows and columns the mask reaches.
    /// </summary>
    /// <example>
    /// <code>
    /// // Darken and enrich the sky, fading out towards the horizon.
    /// pipeline.Masked(BmageMask.Gradient(new(0, 0), new(0, 0.55f)), sky => sky.Exposure(-0.6f).Saturation(0.25f));
    /// </code>
    /// </example>
    public BmageImagePipeline Masked(BmageMask mask, Func<BmageImagePipeline, BmageImagePipeline> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        var inner = edit(Empty) ?? throw new InvalidOperationException("The edit returned no pipeline.");
        return Add(new BmageMaskedOperation(inner.Count == 1 ? inner[0] : new BmagePipelineOperation(inner), mask));
    }

    /// <summary>
    /// Multiplies the image's alpha by <paramref name="mask"/>, making everything outside it transparent: for example
    /// <c>.Cover(256, 256).ApplyMask(BmageMask.Ellipse())</c> for a round avatar.
    /// </summary>
    public BmageImagePipeline ApplyMask(BmageMask mask) => Add(new BmageAlphaMaskOperation(mask));

    /// <summary>
    /// Makes a plain background transparent: the region of the corners' colour, within <paramref name="tolerance"/>,
    /// flooded in from the four corners. For product photos, logos and scans on a uniform backdrop; a subject that touches
    /// a corner in the backdrop's colour is taken with it.
    /// </summary>
    public BmageImagePipeline RemoveBackground(float tolerance = 0.1f, float feather = 0.6f)
        => Add(new BmageAlphaMaskOperation(new BmageFloodMask([new(0, 0), new(1, 0), new(0, 1), new(1, 1)])
        {
            Units = BmageMaskUnits.Relative,
            Tolerance = tolerance,
            Feather = feather,
            Inverted = true,
        }));

    // ---- Analysis ----

    /// <summary>Computes the output size for a given input size without touching pixels.</summary>
    /// <exception cref="ArgumentException">
    /// An operation cannot be applied to the size it would receive, for example a crop that falls entirely outside the
    /// image it follows. Use <see cref="TryGetOutputSize"/> when the pipeline may be in a state the user is still
    /// editing and an exception would be inconvenient.
    /// </exception>
    public Size GetOutputSize(Size inputSize)
    {
        var size = inputSize;
        foreach (var op in _operations) size = op.GetOutputSize(size);
        return size;
    }

    /// <summary>
    /// Computes the output size, returning false instead of throwing when some operation cannot be applied to the size
    /// it would receive. Intended for UI code that displays the output size while an edit is being composed.
    /// </summary>
    public bool TryGetOutputSize(Size inputSize, out Size outputSize)
    {
        try
        {
            outputSize = GetOutputSize(inputSize);
            return true;
        }
        catch (ArgumentException)
        {
            outputSize = Size.Empty;
            return false;
        }
    }

    /// <summary>
    /// The affine map from coordinates of an input of <paramref name="inputSize"/> to coordinates of the output: where
    /// the pipeline's crops, turns, flips, resizes and warps move each point of the image. Steps that keep the size
    /// without being geometric, such as adjustments, filters and masked edits, leave every point where it is. False
    /// when a step's effect on coordinates is not affine or not known: a perspective warp, a content-aware crop, an
    /// orientation read from metadata, or an operation that changes the size without saying how.
    /// </summary>
    public bool TryGetTransform(Size inputSize, out Matrix3x2 transform)
    {
        transform = Matrix3x2.Identity;
        var size = inputSize;
        foreach (var operation in _operations)
        {
            Size next;
            Matrix3x2 step;
            try
            {
                next = operation.GetOutputSize(size);
                switch (operation)
                {
                    case BmagePipelineOperation nested:
                        if (!nested.Pipeline.TryGetTransform(size, out step)) return false;
                        break;
                    case IBmageGeometricOperation geometric:
                        if (geometric.GetTransform(size) is not { } matrix) return false;
                        step = matrix;
                        break;
                    case BmageAutoOrientOperation:
                        return false;
                    default:
                        if (next != size) return false;
                        step = Matrix3x2.Identity;
                        break;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            transform *= step;
            size = next;
        }
        return true;
    }

    /// <summary>
    /// Returns an equivalent pipeline for an image scaled by <paramref name="scale"/>. Used to preview full-resolution
    /// edits on a downscaled proxy.
    /// </summary>
    public BmageImagePipeline ForScale(double scale)
    {
        if (scale == 1 || _operations.IsEmpty) return this;
        var builder = ImmutableArray.CreateBuilder<IBmageImageOperation>(_operations.Length);
        foreach (var op in _operations) builder.Add(op.ForScale(scale));
        return new BmageImagePipeline(builder.MoveToImmutable());
    }

    /// <summary>
    /// Estimates the pixel memory a run on an image of <paramref name="inputSize"/> allocates at its peak, and the size
    /// of its result, without touching a pixel. The source image itself is not counted: the caller already holds it.
    /// </summary>
    /// <remarks>
    /// The estimate follows the buffers each step holds: the image it receives, the one it produces, and the working
    /// copies the heavier operations take (a mask, a copy of a region for a median or convolution). It is what a
    /// <see cref="BmageBudgetedPixelAllocator"/> would see, to within the small rows of scratch operations rent from the
    /// shared pool, and it is what <see cref="BmageBatchOptions.MaxMemoryBytes"/> admits images by. An
    /// <see cref="AutoOrient"/> step is taken to cost nothing, since decoders already turn the pixels upright.
    /// </remarks>
    /// <exception cref="ArgumentException">An operation cannot be applied to the size it would receive.</exception>
    public BmagePipelineMemoryEstimate EstimateMemory(Size inputSize, bool optimize = true)
    {
        var steps = optimize ? Optimize(inputSize) : this;
        static long Bytes(Size size) => 4L * size.Width * size.Height;
        if (steps.IsEmpty) return new BmagePipelineMemoryEstimate(Bytes(inputSize), Bytes(inputSize), inputSize);

        var current = inputSize;
        long peak = 0;
        // Until a step has produced a buffer of the run's own, steps read the caller's.
        var owned = false;
        foreach (var operation in steps)
        {
            // Decoders apply the EXIF orientation as they decode, and a run skips orienting pixels that are upright.
            if (operation is BmageAutoOrientOperation) continue;
            var output = operation.GetOutputSize(current);
            var held = owned ? Bytes(current) : 0;
            // A per-pixel step on a buffer the run owns writes over it rather than allocating another.
            var produced = owned && output == current && operation is BmagePointOperation ? 0 : Bytes(output);
            peak = Math.Max(peak, held + produced + WorkingBytes(operation, current));
            current = output;
            owned = true;
        }
        // Nothing ran, so the result is a copy of the source.
        return new BmagePipelineMemoryEstimate(owned ? peak : Bytes(current), Bytes(current), current);
    }

    /// <summary>
    /// What rendering a mask holds at once: its own byte per pixel, the masks drawing it holds beside that (see
    /// <see cref="BmageMask.MasksHeld"/>), each taken at the image's size, and the largest of its bitmaps' mattes at
    /// their own size. A mask drawn only over the area an edit reaches holds less.
    /// </summary>
    private static long MaskBytes(BmageMask mask, long pixels) => pixels * (1 + mask.MasksHeld()) + MatteBytes(mask);

    private static long MatteBytes(BmageMask mask) => mask switch
    {
        BmageCombinedMask combined => Math.Max(MatteBytes(combined.First), MatteBytes(combined.Second)),
        BmageBitmapMask bitmap => (long)bitmap.Image.Width * bitmap.Image.Height,
        _ => 0,
    };

    /// <summary>What an operation allocates beyond its output while it runs.</summary>
    private static long WorkingBytes(IBmageImageOperation operation, Size input)
    {
        var pixels = (long)input.Width * input.Height;
        return operation switch
        {
            // The rendered mask, and whatever the masked edit itself works with.
            BmageMaskedOperation masked => MaskBytes(masked.Mask, pixels) + WorkingBytes(masked.Operation, input),
            BmageAlphaMaskOperation alpha => MaskBytes(alpha.Mask, pixels),
            // Both read a copy of the region they filter.
            BmageMedianFilter or BmageConvolutionFilter => 4 * pixels,
            // A byte of luminance a pixel.
            BmageAdaptiveThresholdFilter => pixels,
            // Two premultiplied copies at 16 bits a channel.
            BmageRadialBlurFilter => 16 * pixels,
            BmagePipelineOperation nested => nested.Pipeline.EstimateMemory(input).PeakBytes,
            _ => 0,
        };
    }

    /// <summary>
    /// Returns an equivalent but cheaper pipeline: identity operations are dropped, consecutive crops, orientations and
    /// resizes are merged, and neighbouring per-pixel operations are fused into a single pass.
    /// </summary>
    public BmageImagePipeline Optimize(Size? inputSize = null) => BmagePipelineOptimizer.Optimize(this, inputSize);

    /// <summary>A short human readable description, e.g. "AutoOrient → Resize(1920x auto) → Sharpen".</summary>
    public override string ToString() => _operations.IsEmpty ? "(empty pipeline)" : string.Join(" → ", _operations.Select(o => o.ToString()));

    // ---- Execution ----

    /// <summary>
    /// Runs the pipeline on a copy of the source and returns the result. The source is never modified; the caller owns
    /// the returned buffer.
    /// </summary>
    public BmageImageBuffer Execute(BmageImageBuffer source, BmagePipelineExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= BmagePipelineExecutionOptions.Default;
        var pipeline = options.Optimize ? Optimize(source.Size) : this;
        if (pipeline.IsEmpty) return source.Clone(options.Allocator);
        var result = Run(pipeline, source, new BmageOperationContext(options.Allocator, options.Limits, options.CancellationToken, options.Progress, canMutateSource: false, options.MaxDegreeOfParallelism));
        return ReferenceEquals(result, source) ? source.Clone(options.Allocator) : result;
    }

    /// <summary>
    /// Runs <paramref name="steps"/>, already optimised, one after another: the loop of <see cref="Execute"/>, which a
    /// nested pipeline runs too. The source is changed in place only when <paramref name="context"/> allows it, and
    /// is returned itself when no step produced another buffer; every buffer in between is released.
    /// </summary>
    internal static BmageImageBuffer Run(BmageImagePipeline steps, BmageImageBuffer source, BmageOperationContext context)
    {
        var current = source;
        try
        {
            for (var i = 0; i < steps.Count; i++) current = RunStep(steps, i, current, source, context);
            return current;
        }
        catch
        {
            if (!ReferenceEquals(current, source)) current.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Runs step <paramref name="i"/> of <paramref name="steps"/> on <paramref name="current"/> and returns what the
    /// pipeline holds after it: the step's result, or <paramref name="current"/> itself when the step changed nothing or
    /// was skipped. A buffer between steps is released as soon as the next one replaces it; the source is the caller's.
    /// </summary>
    private static BmageImageBuffer RunStep(BmageImagePipeline steps, int i, BmageImageBuffer current, BmageImageBuffer source, BmageOperationContext context)
    {
        var op = steps[i];
        context.ThrowIfCancellationRequested();
        if (!IsUprightAlready(op, current))
        {
            var next = ApplyStep(op, current, i, steps.Count, ReferenceEquals(current, source) ? context.CanMutateSource : true, context);
            if (!ReferenceEquals(next, current) && !ReferenceEquals(current, source)) current.Dispose();
            current = next;
        }
        context.ReportProgress((i + 1) / (double)steps.Count);
        return current;
    }

    /// <summary>
    /// Applies step <paramref name="index"/> of <paramref name="count"/>, reporting its progress within the step's share
    /// of <paramref name="context"/>'s, and naming the step in any failure that is not already an image error.
    /// </summary>
    private static BmageImageBuffer ApplyStep(IBmageImageOperation op, BmageImageBuffer current, int index, int count, bool canMutate, BmageOperationContext context)
    {
        var stageProgress = context.Progress is null ? null : new StageProgress(context.Progress, index, count);
        try
        {
            return op.Apply(current, new BmageOperationContext(context.Allocator, context.Limits, context.CancellationToken, stageProgress, canMutate, context.MaxDegreeOfParallelism));
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or BmageImageException))
        {
            throw new BmageImageException($"Operation '{op.Name}' (step {index + 1} of {count}) failed: {ex.Message}", ex);
        }
    }

    /// <summary>Runs the pipeline, yielding to the scheduler between operations so the UI stays responsive.</summary>
    public async ValueTask<BmageImageBuffer> ExecuteAsync(BmageImageBuffer source, BmagePipelineExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= BmagePipelineExecutionOptions.Default;
        var pipeline = options.Optimize ? Optimize(source.Size) : this;
        if (pipeline.IsEmpty) return source.Clone(options.Allocator);

        // The loop of Run, with a yield between the steps.
        var current = source;
        var count = pipeline.Count;
        var run = new BmageOperationContext(options.Allocator, options.Limits, options.CancellationToken, options.Progress, canMutateSource: false, options.MaxDegreeOfParallelism);
        try
        {
            for (var i = 0; i < count; i++)
            {
                current = RunStep(pipeline, i, current, source, run);
                if (options.YieldBetweenOperations && i + 1 < count) await options.Yield().ConfigureAwait(false);
            }
            return ReferenceEquals(current, source) ? source.Clone(options.Allocator) : current;
        }
        catch
        {
            if (!ReferenceEquals(current, source)) current.Dispose();
            throw;
        }
    }

    /// <summary>
    /// An auto-orientation of pixels that are already upright changes nothing, but as the first step, where the source
    /// may not be modified, it would copy the whole image just to return it: a 48 MB copy for every upright photo.
    /// </summary>
    private static bool IsUprightAlready(IBmageImageOperation op, BmageImageBuffer current)
        => op is BmageAutoOrientOperation && current.Metadata?.Exif?.Orientation is null or BmageOrientation.Normal or BmageOrientation.Unspecified;

    /// <summary>Maps an operation's own progress into the pipeline's overall progress range.</summary>
    private sealed class StageProgress : IProgress<double>
    {
        private readonly IProgress<double> _inner;
        private readonly int _index, _count;
        public StageProgress(IProgress<double> inner, int index, int count) { _inner = inner; _index = index; _count = count; }
        public void Report(double value) => _inner.Report((_index + Math.Clamp(value, 0, 1)) / _count);
    }
}
