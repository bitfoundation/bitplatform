using System.Drawing;

namespace Bit.Bmage.Analysis;

/// <summary>
/// Measures how much fine detail an image holds, to turn away a blurred or shaken photo at upload, pick the sharpest
/// of a burst, or ask for a document to be photographed again before reading it.
/// </summary>
/// <remarks>
/// <para>
/// The measure is the variance of the Laplacian of the luminance: sharp edges give large second differences of both
/// signs, blur flattens them. It is taken at a fixed analysis size, the image reduced to at most
/// <c>analysisSize</c> pixels across, so a photo and its thumbnail score alike and the cost does not grow with the
/// resolution.
/// </para>
/// <para>
/// The value depends on the content as well as on the focus: a plain wall scores low however sharp it is, and a page of
/// crisp black text far higher than a crisp portrait. There is no threshold that suits every kind of image, so compare
/// images of the same kind, or set the threshold from a few known good and bad examples of what your users upload.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var photo = await loader.LoadAsync(file);
/// // A threshold chosen from sample uploads of the same kind, such as ID cards photographed with a phone.
/// if (ImageSharpness.Measure(photo) &lt; options.MinimumSharpness)
///     message = "This photo looks blurred. Hold the camera still and try again.";
/// </code>
/// </example>
public static class ImageSharpness
{
    /// <summary>The analysis size used when none is given.</summary>
    public const int DefaultAnalysisSize = 512;

    /// <summary>
    /// The variance of the luminance's Laplacian, on 0..255 values, at the analysis size: higher is sharper. Zero for
    /// an image of one tone or one smaller than three pixels either way.
    /// </summary>
    /// <param name="image">The image to measure. Transparent pixels count as the colour they carry.</param>
    /// <param name="region">The part of the image to measure, such as a face or a document, or null for all of it.</param>
    /// <param name="analysisSize">The longest side, in pixels, the image is reduced to before measuring; at least 16.</param>
    /// <param name="cancellationToken">Cancels the measurement.</param>
    public static double Measure(ImageBuffer image, Rectangle? region = null, int analysisSize = DefaultAnalysisSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThan(analysisSize, 16);
        var area = region ?? image.Bounds;
        area.Intersect(image.Bounds);
        if (area.Width < 3 || area.Height < 3) return 0;

        var plane = LuminancePlane.Create(image, area, analysisSize, cancellationToken);
        int width = plane.Width, height = plane.Height;
        if (width < 3 || height < 3) return 0;
        var v = plane.Values;
        double sum = 0, squares = 0;
        for (var y = 1; y < height - 1; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = y * width;
            for (var x = 1; x < width - 1; x++)
            {
                var i = row + x;
                double laplacian = v[i - 1] + v[i + 1] + v[i - width] + v[i + width] - 4 * v[i];
                sum += laplacian;
                squares += laplacian * laplacian;
            }
        }
        var count = (double)(width - 2) * (height - 2);
        var mean = sum / count;
        return Math.Max(0, squares / count - mean * mean);
    }
}
