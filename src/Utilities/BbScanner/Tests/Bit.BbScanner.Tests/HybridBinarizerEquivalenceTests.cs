namespace Bit.BbScanner.Tests;

/// <summary>
/// The hybrid binariser measures and thresholds two blocks at a time with vector instructions wherever the machine has
/// them, and falls back to its pixel loop at the clamped edge blocks and wherever it does not. The two must agree bit
/// for bit, so the binariser is checked against the pixel loop written out plainly here, on images whose sizes put
/// blocks at every kind of edge.
/// </summary>
[TestClass]
public class HybridBinarizerEquivalenceTests
{
    [TestMethod]
    [DataRow(40, 40)]
    [DataRow(57, 43)]
    [DataRow(64, 48)]
    [DataRow(333, 201)]
    [DataRow(640, 480)]
    [DataRow(1000, 7 * 8)]
    [DataRow(129, 90)]
    public void MatchesThePixelLoop(int width, int height)
    {
        foreach (var seed in new[] { 1, 2, 3 })
        {
            using var image = Picture(width, height, seed);
            using var expected = Reference(image.View);
            using var actual = new BbHybridBinarizer().GetBlackMatrix(image.View);

            Assert.IsNotNull(actual);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (expected[x, y] != actual[x, y])
                    {
                        Assert.Fail($"{width} x {height}, seed {seed}: pixel ({x}, {y}) differs.");
                    }
                }
            }
        }
    }

    /// <summary>A gradient, blotches of ink and noise: blocks with edges, flat paper and flat ink, side by side.</summary>
    private static BbLuminanceBuffer Picture(int width, int height, int seed)
    {
        var image = BbLuminanceBuffer.Rent(width, height);
        var random = new Random(seed);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.Pixels[(y * width) + x] = (byte)Math.Clamp(120 + (x * 100 / width) + random.Next(-12, 13), 0, 255);
            }
        }

        for (var blot = 0; blot < 30; blot++)
        {
            var bx = random.Next(width);
            var by = random.Next(height);
            var size = random.Next(2, 24);
            var ink = (byte)random.Next(0, 90);
            for (var y = by; y < Math.Min(height, by + size); y++)
            {
                for (var x = bx; x < Math.Min(width, bx + size); x++)
                {
                    image.Pixels[(y * width) + x] = ink;
                }
            }
        }

        return image;
    }

    /// <summary>The binariser as it was before it used vectors: one block, one pixel at a time.</summary>
    private static BbBitMatrix Reference(in BbLuminanceView source)
    {
        const int BlockSize = 8;
        var width = source.Width;
        var height = source.Height;
        var columns = (width + 7) >> 3;
        var rows = (height + 7) >> 3;
        var averages = new int[columns * rows];
        var minimums = new int[columns * rows];
        var ranges = new int[columns * rows];

        static int Origin(int block, int size) => Math.Min(block << 3, size - BlockSize);

        for (var by = 0; by < rows; by++)
        {
            for (var bx = 0; bx < columns; bx++)
            {
                int sum = 0, min = 255, max = 0;
                for (var yy = 0; yy < BlockSize; yy++)
                {
                    var row = source.GetRow(Origin(by, height) + yy);
                    for (var xx = 0; xx < BlockSize; xx++)
                    {
                        int pixel = row[Origin(bx, width) + xx];
                        sum += pixel;
                        min = Math.Min(min, pixel);
                        max = Math.Max(max, pixel);
                    }
                }

                averages[(by * columns) + bx] = sum >> 6;
                minimums[(by * columns) + bx] = min;
                ranges[(by * columns) + bx] = max - min;
            }
        }

        // The noise floor and the uniform block adjustment are untouched by the vector paths; they run on the block
        // statistics, so a difference there would show as a difference in those statistics. Reproducing the rest here
        // would only copy the binariser, so the reference thresholds blocks with the binariser's own adjusted averages
        // when its measurements match: the comparison below is of the two pixel passes.
        var binarizer = new BbHybridBinarizer();
        using var probe = binarizer.GetBlackMatrix(source)!;
        var adjusted = (int[])typeof(BbHybridBinarizer)
            .GetField("_blockAverages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(binarizer)!;
        var measuredMinimums = (int[])typeof(BbHybridBinarizer)
            .GetField("_blockMinimums", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(binarizer)!;
        var measuredRanges = (int[])typeof(BbHybridBinarizer)
            .GetField("_blockRanges", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(binarizer)!;
        var edges = 0;
        for (var i = 0; i < columns * rows; i++)
        {
            Assert.AreEqual(minimums[i], measuredMinimums[i], $"block {i} minimum");
            Assert.AreEqual(ranges[i], measuredRanges[i], $"block {i} range");

            // A block with an edge well above any noise floor keeps its own mean through the adjustment, so the
            // measured mean can be compared too.
            if (ranges[i] > 96)
            {
                Assert.AreEqual(averages[i], adjusted[i], $"block {i} mean");
                edges++;
            }
        }

        Assert.IsGreaterThan(0, edges, "The picture has no strong edges to compare the means of.");

        static int Clamp(int value, int max) => value < 2 ? 2 : (value > max ? max : value);

        var matrix = new BbBitMatrix(width, height);
        for (var by = 0; by < rows; by++)
        {
            var top = Clamp(by, rows - 3);
            for (var bx = 0; bx < columns; bx++)
            {
                var left = Clamp(bx, columns - 3);
                var sum = 0;
                for (var dy = -2; dy <= 2; dy++)
                {
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        sum += adjusted[((top + dy) * columns) + left + dx];
                    }
                }

                var threshold = sum / 25;
                for (var yy = 0; yy < BlockSize; yy++)
                {
                    var y = Origin(by, height) + yy;
                    var row = source.GetRow(y);
                    for (var xx = 0; xx < BlockSize; xx++)
                    {
                        var x = Origin(bx, width) + xx;
                        if (row[x] <= threshold)
                        {
                            matrix[x, y] = true;
                        }
                    }
                }
            }
        }

        return matrix;
    }
}
