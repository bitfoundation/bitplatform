using System.Globalization;
using System.Text;

namespace Bit.BbScanner.Tests;

/// <summary>
/// GS1 DataBar Limited and Expanded, and the stacked forms of the family: the test kit's encoders against the
/// independent one, round trips through every character group and encodation mode, and reading at the angles a
/// camera produces. The reference fixtures are read as written in <c>ReferenceSymbolTests</c>.
/// </summary>
[TestClass]
public class DataBarFamilyTests
{
    private const char GroupSeparator = '\u001d';

    [TestMethod]
    public void TheLimitedEncoderWritesWhatTheIndependentEncoderWrites()
    {
        // BWIPP draws the left guard's module of light and the right guard's five; the test kit leaves both to the
        // quiet zone, as it does for DataBar.
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.DataBarLimited))
        {
            var widths = symbol.Widths!;
            Assert.AreEqual(0, widths[0], symbol.Id);
            Assert.AreEqual(5, widths[^2], symbol.Id);
            var expected = SyntheticImage.ExpandWidths(widths.AsSpan(2, widths.Length - 4));

            var written = LinearEncoders.DataBarLimited(symbol.Expected![2..]);

            CollectionAssert.AreEqual(expected, written, symbol.Id);
        }
    }

    [TestMethod]
    public void LimitedRoundTripsGtinsFromEveryCharacterGroup()
    {
        // The group boundaries of both characters, and random GTINs between: every one starts with 0 or 1.
        long[] edges = [0, 183063, 183064, 820063, 820064, 1000775, 1000776, 1491020, 1491021, 1979844, 1979845, 1996938, 1996939, 2013570];
        var random = new Random(89);
        var values = edges.SelectMany(left => new[] { (left * 2013571) + edges[random.Next(edges.Length)] })
            .Concat(Enumerable.Range(0, 40).Select(_ => random.NextInt64(2_000_000_000_000L)))
            .Where(value => value < 2_000_000_000_000L);

        foreach (var value in values)
        {
            var digits = value.ToString("D13", CultureInfo.InvariantCulture);
            var gtin = digits + LinearEncoders.EanCheckDigit(digits);
            using var image = SyntheticImage.FromLinear(LinearEncoders.DataBarLimited(gtin), scale: 3, height: 10 * 3, quietZoneModules: 6);

            Assert.AreEqual("01" + gtin, Decode(image, BbBarcodeFormat.DataBarLimited, still: true)?.Text, gtin);
        }
    }

    [TestMethod]
    public void ExpandedRoundTripsElementStringsThroughEveryMode()
    {
        // A GTIN or not, then elements of digits, of the alphanumeric set and of the ISO/IEC 646 one, with the separator
        // after every variable length element but the last: numeric pairs, the latches between the three modes, a
        // function 1 inside each of them, an odd last digit and the padding after each mode.
        string[] elements =
        [
            "10ABC123", "21aZ-/%*+,.:;=?_\"!&'<>", "11241231", "17261231", "2112345", "91XYZ", "8005123456", "7001123456789012",
            "92A", "10A1B2C3", "400PO-7/12", "2201234", "98abcdefgh", "3922795", "2509",
        ];
        var random = new Random(211);
        var tested = 0;
        for (var attempt = 0; attempt < 200 && tested < 80; attempt++)
        {
            var builder = new StringBuilder();
            if (random.Next(2) == 0)
            {
                var digits = random.NextInt64(10_000_000_000_000L).ToString("D13", CultureInfo.InvariantCulture);
                builder.Append("01").Append(digits).Append(LinearEncoders.EanCheckDigit(digits));
            }

            var count = random.Next(builder.Length == 0 ? 1 : 0, 4);
            for (var i = 0; i < count; i++)
            {
                var element = elements[random.Next(elements.Length)];
                builder.Append(element);
                if (i < count - 1 && !IsPredefinedLength(element))
                {
                    builder.Append(GroupSeparator);
                }
            }

            var text = builder.ToString();
            bool[] modules;
            try
            {
                modules = LinearEncoders.DataBarExpanded(text);
            }
            catch (ArgumentException)
            {
                continue; // Too long for one symbol.
            }

            using var image = SyntheticImage.FromLinear(modules, scale: 2, height: 34 * 2, quietZoneModules: 6);
            Assert.AreEqual(Visible(text), Visible(Decode(image, BbBarcodeFormat.DataBarExpanded, still: true)?.Text), Visible(text));
            tested++;
        }

        Assert.IsGreaterThanOrEqualTo(60, tested);
    }

    [TestMethod]
    public void ExpandedIsReportedAsGs1()
    {
        using var image = SyntheticImage.FromLinear(LinearEncoders.DataBarExpanded("010952123454321310ABC123"), scale: 3, height: 34 * 3, quietZoneModules: 6);
        var result = Decode(image, BbBarcodeFormat.DataBarExpanded, still: false);

        Assert.AreEqual("010952123454321310ABC123", result?.Text);
        Assert.IsTrue(result!.Metadata!.IsGs1);
        Assert.AreEqual(BbBarcodeFormat.DataBarExpanded, result.Format);
    }

    [TestMethod]
    [DataRow("databar-limited-1", 10)]
    [DataRow("databar-limited-3", 10)]
    [DataRow("databar-expanded-gtin-lot", 34)]
    [DataRow("databar-expanded-weight-date", 34)]
    [DataRow("databar-expandedstacked-1", 0)]
    [DataRow("databar-expandedstacked-2", 0)]
    [DataRow("databar-stacked-1", 0)]
    [DataRow("databar-stackedomni-1", 0)]
    public void ReadsTurnedLiveWithinFourFramesAndFromAStillImage(string id, int modulesTall)
    {
        // Upright and sideways, either way up, and turned a few degrees off each, at three pixels a module: what a hand
        // holding a phone over a label produces. A symbol no single line crosses whole - the stacked forms, and the long
        // Expanded symbols turned - is put together from several.
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = modulesTall == 0
            ? SyntheticImage.FromMatrix(symbol.ToMatrix(), 3, quietZoneModules: 2)
            : SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), 3, modulesTall * 3, quietZoneModules: 4);
        var failures = new List<string>();
        foreach (var degrees in new[] { 0, 4, -4, 90, 94, 180, 184, 270 })
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            using var frame = ImageTransforms.Frame(rotated, Math.Max(640, rotated.Width), Math.Max(480, rotated.Height));
            if (DecodeLive(frame, symbol.BarcodeFormat)?.Text != symbol.Expected)
            {
                failures.Add($"{degrees} degrees live");
            }

            if (Decode(rotated, symbol.BarcodeFormat, still: true)?.Text != symbol.Expected)
            {
                failures.Add($"{degrees} degrees still");
            }
        }

        Assert.IsEmpty(failures, $"{id}: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void TheFamilyIsNotMisreadThroughBlurNoiseAndRotation()
    {
        // Limited's modulo 89, Expanded's modulo 211, and the halves and rows the readers put together from several
        // lines, pushed through blur and noise, turned, live and still: every value read has to be the right one. A wider
        // sweep run once over every family fixture - 12 636 decodes, nine angles to 240 degrees, two to four pixels a
        // module, blur to 1.3, noise to 45 - read 10 976 and misread none; it takes many minutes, so it is not kept here.
        var misreads = new List<string>();
        var (reads, attempts) = (0, 0);
        foreach (var symbol in ReferenceSymbols.All.Where(s =>
                     s.Id is "databar-limited-2" or "databar-limited-4" or "databar-expanded-long" or "databar-expanded-price-currency" or
                         "databar-expandedstacked-0" or "databar-expandedstacked-3" or "databar-stacked-2" or "databar-stackedomni-2"))
        {
            using var rendered = Render(symbol, 3);
            foreach (var degrees in new[] { 0, 9, 33, 187 })
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees);
                foreach (var (blur, noise) in new[] { (0.7, 25), (1.2, 40) })
                {
                    using var blurred = ImageTransforms.Blur(rotated, blur);
                    SyntheticImage.AddNoise(blurred, noise, seed: degrees + noise);
                    using var frame = ImageTransforms.Frame(blurred, Math.Max(640, blurred.Width), Math.Max(480, blurred.Height));
                    foreach (var live in new[] { true, false })
                    {
                        attempts++;
                        if ((live ? DecodeLive(frame, Family) : Decode(blurred, Family, still: true)) is not { } result)
                        {
                            continue;
                        }

                        reads++;
                        if (result.Text != symbol.Expected)
                        {
                            misreads.Add($"{symbol.Id} {degrees} degrees, blur {blur}, noise {noise}, {(live ? "live" : "still")}: {result.Text}");
                        }
                    }
                }
            }
        }

        Assert.IsEmpty(misreads, $"{reads} of {attempts} read; misread: {string.Join("; ", misreads)}");
        Assert.IsGreaterThan(attempts / 2, reads, $"Only {reads} of {attempts} read");
    }

    [TestMethod]
    public void LabelsOfOneProductAreNotMixedUp()
    {
        // Labels of one product carry the same GTIN and so the same first characters at the same places; with their
        // lots, weights and dates different, a reader that kept characters by place alone would fill one label's places
        // with another's wherever its own were not read. Four to a sheet, close together, turned: every value read has to
        // be one of the four. Run once over 60 sheets, it read 919 values and none wrong.
        var random = new Random(7);
        var wrong = new List<string>();
        var reads = 0;
        for (var sheet = 0; sheet < 12; sheet++)
        {
            var texts = Enumerable.Range(0, 4).Select(_ => random.Next(3) switch
            {
                0 => "0109521234543213" + $"10LOT{random.Next(100, 999)}",
                1 => "0109521234543213" + $"3103{random.Next(0, 32767):D6}",
                _ => "0109521234543213" + $"17{random.Next(24, 30):D2}{random.Next(1, 13):D2}{random.Next(1, 29):D2}" + $"10B{random.Next(10, 99)}",
            }).Distinct().ToArray();
            var scale = 2 + random.Next(2);
            var gap = random.Next(1, 6) * scale;
            var images = texts.Select(t => SyntheticImage.FromLinear(LinearEncoders.DataBarExpanded(t), scale, 34 * scale, quietZoneModules: 2)).ToArray();
            try
            {
                var columns = random.Next(1, 3);
                var cellWidth = images.Max(i => i.Width) + gap;
                var cellHeight = images.Max(i => i.Height) + gap;
                using var page = BbLuminanceBuffer.Rent((columns * cellWidth) + 20, ((images.Length + columns - 1) / columns * cellHeight) + 20);
                page.Pixels.Fill(255);
                for (var k = 0; k < images.Length; k++)
                {
                    ImageTransforms.Paste(images[k], page, 10 + ((k % columns) * cellWidth), 10 + ((k / columns) * cellHeight));
                }

                using var rotated = ImageTransforms.Rotate(page, random.Next(-25, 26));
                using var frame = ImageTransforms.Frame(rotated, Math.Max(640, rotated.Width), Math.Max(480, rotated.Height));
                foreach (var live in new[] { true, false })
                {
                    var options = live ? new BbScannerOptions() : BbScannerOptions.ForStillImages();
                    options.Formats = Family;
                    options.DuplicateSuppressionWindow = TimeSpan.Zero;
                    using var decoder = new BbBarcodeDecoder(options);
                    foreach (var result in decoder.DecodeAll(frame.View, 4, suppressDuplicates: false))
                    {
                        reads++;
                        if (!texts.Contains(result.Text))
                        {
                            wrong.Add($"sheet {sheet} ({string.Join(", ", texts)}) {(live ? "live" : "still")}: {result.Text}");
                        }
                    }
                }
            }
            finally
            {
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("; ", wrong));
        Assert.IsGreaterThan(20, reads);
    }

    private const BbBarcodeFormat Family = BbBarcodeFormat.DataBar | BbBarcodeFormat.DataBarLimited | BbBarcodeFormat.DataBarExpanded;

    /// <summary>A family fixture at its own height: Truncated 13 modules, Limited 10, the other linear forms 34.</summary>
    private static BbLuminanceBuffer Render(ReferenceSymbol symbol, int scale)
    {
        if (!symbol.IsLinear)
        {
            return SyntheticImage.FromMatrix(symbol.ToMatrix(), scale, quietZoneModules: 2);
        }

        var height = symbol.Encoder.StartsWith("databartruncated", StringComparison.Ordinal) ? 13
            : symbol.Encoder.StartsWith("databarlimited", StringComparison.Ordinal) ? 10
            : 34;
        return SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), scale, height * scale, quietZoneModules: 4);
    }

    /// <summary>Up to four live frames of the same image through one decoder, as a camera held still delivers them.</summary>
    private static BbBarcodeResult? DecodeLive(BbLuminanceBuffer frame, BbBarcodeFormat format)
    {
        var options = new BbScannerOptions { Formats = format, DuplicateSuppressionWindow = TimeSpan.Zero };
        using var decoder = new BbBarcodeDecoder(options);
        for (var i = 0; i < 4; i++)
        {
            if (decoder.Decode(frame.View, suppressDuplicates: false) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private static bool IsPredefinedLength(string element) =>
        element[..2] is "00" or "01" or "02" or "03" or "04" or "11" or "12" or "13" or "15" or "16" or "17" or "18" or "19" or "20" or "41" ||
        element[..2] is "31" or "32" or "33" or "34" or "35" or "36";

    private static string? Visible(string? text) => text?.Replace(GroupSeparator, '|');

    private static BbBarcodeResult? Decode(BbLuminanceBuffer image, BbBarcodeFormat format, bool still)
    {
        var options = still ? BbScannerOptions.ForStillImages() : new BbScannerOptions();
        options.Formats = format;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        using var decoder = new BbBarcodeDecoder(options);
        return decoder.Decode(image.View, suppressDuplicates: false);
    }
}
