namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The engine tools: a pipeline run on the real engine reports what the engine did, and a metadata
/// export reports what is really in the file. These are the answers an agent chooses settings by,
/// so each one is checked against the library itself rather than against a number written here.
/// </summary>
[TestClass]
public class PipelineLabTests
{
    [TestMethod]
    public async Task Run_ReportsTheOutputThePipelinePredicted()
    {
        var run = await BmagePipelineLab.RunAsync("AutoOrient().MaxSize(400, 400)", "photo", 800, 600);

        Assert.IsNull(run.Error, run.Error);
        Assert.AreEqual(400, run.OutputWidth);
        Assert.AreEqual(300, run.OutputHeight);
        Assert.AreEqual("400 x 300", run.PredictedOutputSize);
        Assert.AreEqual(800, run.SourceWidth);
        Assert.IsGreaterThan(0, run.ExecuteMilliseconds);
        Assert.IsFalse(string.IsNullOrWhiteSpace(run.Reading));
    }

    /// <summary>
    /// The byte counts are the library's own encoders, so they are checked against an encode done
    /// here with the same codec - and a higher quality has to cost more bytes.
    /// </summary>
    [TestMethod]
    public async Task Run_EncodesEveryFormatAndQualityAskedFor_WithTheManagedCodecs()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(320, 320)", "photo", 640, 480, "jpeg, png", "0.5, 0.95");

        var jpegs = run.Encodings.Where(encoding => encoding.Format == "Jpeg").ToArray();

        Assert.HasCount(2, jpegs);
        Assert.IsLessThan(jpegs[1].Bytes!.Value, jpegs[0].Bytes!.Value, "Quality 0.5 encoded larger than 0.95.");

        var png = run.Encodings.Single(encoding => encoding.Format == "Png");
        Assert.IsNull(png.Quality, "PNG is lossless and has no quality.");

        using var source = BmagePipelineLab.CreateSource("photo", 640, 480);
        using var output = Bit.Bmage.Pipeline.ImagePipeline.Create().MaxSize(320, 320).Execute(source);
        var expected = await new Bit.Bmage.Pipeline.ImageProcessor().EncodeAsync(output, new Bit.Bmage.Codecs.ImageExportOptions { Format = ImageFormat.Png });

        Assert.AreEqual(expected.Length, png.Bytes, "The PNG measured is not the PNG the library encodes.");
    }

    [TestMethod]
    public async Task Run_BrowserFormats_AreReportedWithGuidance_NotMeasured()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(200, 200)", "photo", 400, 300, "jpeg, webp, avif, gif", "0.8");

        foreach (var format in new[] { "WebP", "Avif", "Gif" })
        {
            var encoding = run.Encodings.Single(entry => entry.Format == format);

            Assert.IsNull(encoding.Bytes, $"{format} was measured, on a server that cannot encode it.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(encoding.Note));
        }

        // WebP's estimate is drawn from the JPEG measured beside it.
        StringAssert.Contains(run.Encodings.Single(entry => entry.Format == "WebP").Note, "against the JPEG at 0.8");
    }

    [TestMethod]
    public async Task Run_ABudget_IsSearchedByTheLibrary_AndAnImpossibleOneIsExplained()
    {
        // Half of what quality 0.9 produces: reachable by lowering the quality, not at 0.9 itself.
        var atFull = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "jpeg", "0.9");
        var budget = atFull.Encodings.Single().Bytes!.Value / 2;

        var reachable = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "jpeg", "0.9", maxFileSize: budget);

        var met = reachable.Budgets!.Single();

        Assert.IsTrue(met.Met, met.Error);
        Assert.IsLessThanOrEqualTo(budget, met.Bytes!.Value);
        Assert.IsLessThan(0.9, met.Quality!.Value, "The budget was met without lowering the quality from 0.9.");
        Assert.AreEqual(400, met.Width, "Quality alone could reach this budget, so the dimensions should not have moved.");

        var impossible = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "png", maxFileSize: 10);

        var missed = impossible.Budgets!.Single();

        Assert.IsFalse(missed.Met);
        StringAssert.Contains(missed.Error, "Cannot reach");
    }

    [TestMethod]
    public async Task Run_TheOptimisersWork_IsReportedAndTimed()
    {
        var run = await BmagePipelineLab.RunAsync("Brightness(0).MaxSize(300, 300).Brightness(0.1f).Contrast(0.1f).Vibrance(0.2f)", "photo", 600, 400, "jpeg", "0.8");

        Assert.IsNull(run.Error, run.Error);
        Assert.HasCount(5, run.Operations);
        Assert.HasCount(2, run.OptimizedOperations, string.Join(" | ", run.OptimizedOperations));
        Assert.IsTrue(run.OptimizerNotes.Any(note => note.StartsWith("Dropped 1", StringComparison.Ordinal)), string.Join(" | ", run.OptimizerNotes));
        Assert.IsTrue(run.OptimizerNotes.Any(note => note.StartsWith("Fused into a single pass", StringComparison.Ordinal)), string.Join(" | ", run.OptimizerNotes));
        Assert.IsNotNull(run.UnoptimizedMilliseconds, "A pipeline the optimiser rewrote was not timed without it.");
    }

    [TestMethod]
    public async Task Run_SplitAdjustmentsAndAnAutoOrientBlockingFusion_AreExplained()
    {
        var split = await BmagePipelineLab.RunAsync("Brightness(0.1f).MaxSize(300, 300).Contrast(0.1f)", "photo", 600, 400, "jpeg", "0.8");

        Assert.IsTrue(split.OptimizerNotes.Any(note => note.Contains("separate runs", StringComparison.Ordinal)), string.Join(" | ", split.OptimizerNotes));

        var blocked = await BmagePipelineLab.RunAsync("AutoOrient().Resize(300, 300).Crop(0, 0, 200, 200)", "photo", 600, 400, "jpeg", "0.8");

        Assert.IsTrue(blocked.OptimizerNotes.Any(note => note.Contains("AutoOrient", StringComparison.Ordinal)), string.Join(" | ", blocked.OptimizerNotes));

        var fused = await BmagePipelineLab.RunAsync("Resize(300, 300).Crop(0, 0, 200, 200)", "photo", 600, 400, "jpeg", "0.8");

        Assert.IsTrue(fused.OptimizerNotes.Any(note => note.StartsWith("Fused a resize with the crop", StringComparison.Ordinal)), string.Join(" | ", fused.OptimizerNotes));
    }

    [TestMethod]
    public async Task Run_TheSources_CompressTheWayTheirKindDoes()
    {
        var photo = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "photo", 600, 400, "jpeg, png", "0.85");
        var screenshot = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "screenshot", 600, 400, "jpeg, png", "0.85");
        var graphic = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "graphic", 600, 400, "jpeg, png", "0.85");

        long Bytes(BmagePipelineRunDto run, string format) => run.Encodings.Single(encoding => encoding.Format == format).Bytes!.Value;

        Assert.IsGreaterThan(Bytes(photo, "Jpeg"), Bytes(photo, "Png"), "A photo compressed better as PNG than as JPEG.");
        Assert.IsLessThan(Bytes(screenshot, "Jpeg"), Bytes(screenshot, "Png"), "A flat-colour screenshot compressed better as JPEG than as PNG.");

        Assert.IsTrue(graphic.OutputHasTransparency);
        StringAssert.Contains(graphic.Encodings.Single(encoding => encoding.Format == "Jpeg").Note, "flattened");
        Assert.IsFalse(photo.OutputHasTransparency);
    }

    [TestMethod]
    public async Task Run_AnOversizedSource_IsClamped_AndSaysSo()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(100, 100)", "photo", 9000, 9000, "jpeg", "0.8");

        Assert.IsLessThanOrEqualTo(BmagePipelineLab.MaxEdge, run.SourceWidth);
        Assert.IsLessThanOrEqualTo(BmagePipelineLab.MaxPixels, (long)run.SourceWidth * run.SourceHeight);
        Assert.IsTrue(run.Warnings.Any(warning => warning.Contains("9000 x 9000", StringComparison.Ordinal)), string.Join(" | ", run.Warnings));
    }

    [TestMethod]
    public async Task Run_ArgumentSlips_AreCorrected_AndReported()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(100, 100)", "hologram", 300, 200, "jpeg, tiff", "80, 150, nope");

        Assert.IsNull(run.Error, run.Error);
        Assert.AreEqual("photo", run.Source);

        // "80" is a percentage and read as 0.8; "150" is no quality at all; "nope" is not a number; tiff is not a format.
        CollectionAssert.AreEqual(new double?[] { 0.8 }, run.Encodings.Select(encoding => encoding.Quality).ToArray());
        Assert.HasCount(4, run.Warnings, string.Join(" | ", run.Warnings));
    }

    /// <summary>
    /// A pipeline that fails at run time - a crop outside the image - returns the library's own
    /// message, which names the step.
    /// </summary>
    [TestMethod]
    public async Task Run_APipelineThatFails_ReturnsTheLibrarysMessage_AsData()
    {
        var run = await BmagePipelineLab.RunAsync("Crop(5000, 5000, 10, 10)", "photo", 400, 300);

        Assert.IsNotNull(run.Error);
        StringAssert.Contains(run.Error, "Crop");
        Assert.IsEmpty(run.Encodings);
    }

    /// <summary>
    /// The source cap does not bound a run on its own: a pipeline can grow the image past it. The run's
    /// own limits refuse that instead of letting a public call allocate the library's 64 megapixel default.
    /// </summary>
    [TestMethod]
    public async Task Run_APipelineThatGrowsPastTheServersLimits_IsRefused()
    {
        var run = await BmagePipelineLab.RunAsync("Resize(8000, 6000)", "photo", 400, 300);

        Assert.IsNotNull(run.Error);
        Assert.IsEmpty(run.Encodings);
    }

    [TestMethod]
    public async Task Run_APipelineChainingTooManyCalls_IsNotRun()
    {
        var spec = string.Join(".", Enumerable.Repeat("Blur(1)", BmagePipelineSpec.MaxOperations + 1));

        var run = await BmagePipelineLab.RunAsync(spec, "photo", 400, 300);

        StringAssert.Contains(run.Error, $"at most {BmagePipelineSpec.MaxOperations}");
    }

    [TestMethod]
    public async Task Run_AnUnreadablePipeline_ReturnsTheReadersExplanation()
    {
        var run = await BmagePipelineLab.RunAsync("Teleport(1)");

        StringAssert.Contains(run.Error, "no method called 'Teleport'");
    }

    [TestMethod]
    [DataRow("jpeg")]
    [DataRow("png")]
    public async Task Metadata_EachPolicy_KeepsWhatItIsDocumentedToKeep(string format)
    {
        var comparison = await BmagePipelineLab.CompareMetadataPoliciesAsync(format);

        Assert.IsNull(comparison.Error, comparison.Error);
        Assert.HasCount(8, comparison.Original);

        var policies = comparison.Policies.ToDictionary(policy => policy.Policy);

        Assert.IsEmpty(policies["Remove"].Kept);
        Assert.IsFalse(policies["Remove"].LeaksLocation);

        var stripped = policies["StripSensitive"];
        Assert.IsFalse(stripped.LeaksLocation);
        Assert.IsFalse(stripped.LeaksIdentity);
        Assert.IsTrue(stripped.Kept.Any(field => field.StartsWith("Camera:", StringComparison.Ordinal)), "StripSensitive dropped the camera.");
        CollectionAssert.IsSubsetOf(new[] { "Location", "Serial number", "Owner" }, stripped.Removed);

        Assert.IsTrue(policies["Preserve"].LeaksLocation);
        Assert.IsTrue(policies["Preserve"].LeaksIdentity);
    }

    [TestMethod]
    public async Task Metadata_ABrowserOrUnknownFormat_IsExplained()
    {
        StringAssert.Contains((await BmagePipelineLab.CompareMetadataPoliciesAsync("webp")).Error, "browser");
        StringAssert.Contains((await BmagePipelineLab.CompareMetadataPoliciesAsync("tiff")).Error, "'jpeg', 'png' or 'bmp'");
    }
}
