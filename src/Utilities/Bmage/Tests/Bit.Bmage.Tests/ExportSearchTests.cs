using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Jpeg;
using Bit.Bmage.Memory;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// A size budget is met by searching quality, and every attempt is a full encode, which in a browser also means
/// sending the pixels across. The search must stay within the budget at nearly the best quality, in a bounded number of
/// encodes: one at each end of the quality range and six bisections between them.
/// </summary>
[TestClass]
public sealed class ExportSearchTests
{
    [TestMethod]
    public async Task ASizeBudgetIsMetAtNearlyTheBestQualityInABoundedNumberOfEncodes()
    {
        const int bisection = 8;
        using var image = TestImages.Gradient(320, 240);
        var fractions = new[] { 0.2, 0.35, 0.5, 0.7, 0.9 };
        foreach (var fraction in fractions)
        {
            var counting = new CountingEncoder();
            var registry = ImageCodecRegistry.CreateDefault().AddEncoder(counting);
            var processor = new ImageProcessor(registry);
            var largest = JpegEncoder.Encode(image, ImageExportOptions.Jpeg with { Quality = 0.95 }).Length;
            var smallest = JpegEncoder.Encode(image, ImageExportOptions.Jpeg with { Quality = 0.3 }).Length;
            var budget = (long)(smallest + (largest - smallest) * fraction);

            var result = await processor.EncodeAsync(image, ImageExportOptions.Jpeg with { Quality = 0.95, MinQuality = 0.3, MaxFileSize = budget });

            Assert.IsTrue(result.Length <= budget, $"{result.Length} bytes for a budget of {budget}");
            Assert.IsTrue(counting.Encodes <= bisection, $"{counting.Encodes} encodes for a budget of {budget}");
            // Nearly the highest quality that fits: one notch higher must not fit.
            var better = JpegEncoder.Encode(image, ImageExportOptions.Jpeg with { Quality = result.Quality!.Value + 0.03 }).Length;
            Assert.IsTrue(better > budget * 0.97, $"quality {result.Quality} leaves room: {better} bytes still fit {budget}");
        }
    }

    private sealed class CountingEncoder : IImageEncoder
    {
        public int Encodes { get; private set; }
        public IReadOnlyCollection<ImageFormat> Formats { get; } = [ImageFormat.Jpeg];
        public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        {
            Encodes++;
            return ValueTask.FromResult(JpegEncoder.Encode(image, options, cancellationToken));
        }
    }
}
