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
        var run = await BmagePipelineLab.RunAsync("AutoOrient().MaxSize(400, 400)", "photo", 800, 600, cancellationToken: TestContext.CancellationToken);

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
        var run = await BmagePipelineLab.RunAsync("MaxSize(320, 320)", "photo", 640, 480, "jpeg, png", "0.5, 0.95", cancellationToken: TestContext.CancellationToken);

        var jpegs = run.Encodings.Where(encoding => encoding.Format == "Jpeg").ToArray();

        Assert.HasCount(2, jpegs);
        Assert.IsLessThan(jpegs[1].Bytes!.Value, jpegs[0].Bytes!.Value, "Quality 0.5 encoded larger than 0.95.");

        var png = run.Encodings.Single(encoding => encoding.Format == "Png");
        Assert.IsNull(png.Quality, "PNG is lossless and has no quality.");

        using var source = BmagePipelineLab.CreateSource("photo", 640, 480);
        using var output = Bit.Bmage.Pipeline.ImagePipeline.Create().MaxSize(320, 320).Execute(source);
        var expected = await new Bit.Bmage.Pipeline.ImageProcessor().EncodeAsync(output, new Bit.Bmage.Codecs.ImageExportOptions { Format = ImageFormat.Png }, TestContext.CancellationToken);

        Assert.AreEqual(expected.Length, png.Bytes, "The PNG measured is not the PNG the library encodes.");
    }

    [TestMethod]
    public async Task Run_WebPIsMeasured_AvifAndGif_AreReportedWithGuidance()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(200, 200)", "photo", 400, 300, "jpeg, webp, avif, gif", "0.8", cancellationToken: TestContext.CancellationToken);

        foreach (var format in new[] { "Avif", "Gif" })
        {
            var encoding = run.Encodings.Single(entry => entry.Format == format);

            Assert.IsNull(encoding.Bytes, $"{format} was measured, on a server that cannot encode it.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(encoding.Note));
        }

        // The managed encoder writes WebP on the server, so it is measured like JPEG, and comes out smaller.
        var webp = run.Encodings.Single(entry => entry.Format == "WebP");
        var jpeg = run.Encodings.Single(entry => entry.Format == "Jpeg");
        Assert.IsNotNull(webp.Bytes);
        Assert.IsLessThan(jpeg.Bytes!.Value, webp.Bytes!.Value);
    }

    [TestMethod]
    public async Task Run_ABudget_IsSearchedByTheLibrary_AndAnImpossibleOneIsExplained()
    {
        // Half of what quality 0.9 produces: reachable by lowering the quality, not at 0.9 itself.
        var atFull = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "jpeg", "0.9", cancellationToken: TestContext.CancellationToken);
        var budget = atFull.Encodings.Single().Bytes!.Value / 2;

        var reachable = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "jpeg", "0.9", maxFileSize: budget, cancellationToken: TestContext.CancellationToken);

        var met = reachable.Budgets!.Single();

        Assert.IsTrue(met.Met, met.Error);
        Assert.IsLessThanOrEqualTo(budget, met.Bytes!.Value);
        Assert.IsLessThan(0.9, met.Quality!.Value, "The budget was met without lowering the quality from 0.9.");
        Assert.AreEqual(400, met.Width, "Quality alone could reach this budget, so the dimensions should not have moved.");

        var impossible = await BmagePipelineLab.RunAsync("MaxSize(400, 400)", "photo", 800, 600, "png", maxFileSize: 10, cancellationToken: TestContext.CancellationToken);

        var missed = impossible.Budgets!.Single();

        Assert.IsFalse(missed.Met);
        Assert.Contains("Cannot reach", missed.Error!);
    }

    [TestMethod]
    public async Task Run_TheOptimisersWork_IsReportedAndTimed()
    {
        var run = await BmagePipelineLab.RunAsync("Brightness(0).MaxSize(300, 300).Brightness(0.1f).Contrast(0.1f).Vibrance(0.2f)", "photo", 600, 400, "jpeg", "0.8", cancellationToken: TestContext.CancellationToken);

        Assert.IsNull(run.Error, run.Error);
        Assert.HasCount(5, run.Operations);
        Assert.HasCount(2, run.OptimizedOperations, string.Join(" | ", run.OptimizedOperations));
        Assert.Contains(note => note.StartsWith("Dropped 1", StringComparison.Ordinal), run.OptimizerNotes, string.Join(" | ", run.OptimizerNotes));
        Assert.Contains(note => note.StartsWith("Fused into a single pass", StringComparison.Ordinal), run.OptimizerNotes, string.Join(" | ", run.OptimizerNotes));
        Assert.IsNotNull(run.UnoptimizedMilliseconds, "A pipeline the optimiser rewrote was not timed without it.");
    }

    [TestMethod]
    public async Task Run_SplitAdjustmentsAndAnAutoOrientBlockingFusion_AreExplained()
    {
        var split = await BmagePipelineLab.RunAsync("Brightness(0.1f).MaxSize(300, 300).Contrast(0.1f)", "photo", 600, 400, "jpeg", "0.8", cancellationToken: TestContext.CancellationToken);

        Assert.Contains(note => note.Contains("separate runs", StringComparison.Ordinal), split.OptimizerNotes, string.Join(" | ", split.OptimizerNotes));

        var blocked = await BmagePipelineLab.RunAsync("AutoOrient().Resize(300, 300).Crop(0, 0, 200, 200)", "photo", 600, 400, "jpeg", "0.8", cancellationToken: TestContext.CancellationToken);

        Assert.Contains(note => note.Contains("AutoOrient", StringComparison.Ordinal), blocked.OptimizerNotes, string.Join(" | ", blocked.OptimizerNotes));

        var fused = await BmagePipelineLab.RunAsync("Resize(300, 300).Crop(0, 0, 200, 200)", "photo", 600, 400, "jpeg", "0.8", cancellationToken: TestContext.CancellationToken);

        Assert.Contains(note => note.StartsWith("Fused a resize with the crop", StringComparison.Ordinal), fused.OptimizerNotes, string.Join(" | ", fused.OptimizerNotes));
    }

    [TestMethod]
    public async Task Run_TheSources_CompressTheWayTheirKindDoes()
    {
        var photo = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "photo", 600, 400, "jpeg, png", "0.85", cancellationToken: TestContext.CancellationToken);
        var screenshot = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "screenshot", 600, 400, "jpeg, png", "0.85", cancellationToken: TestContext.CancellationToken);
        var graphic = await BmagePipelineLab.RunAsync("MaxSize(600, 600)", "graphic", 600, 400, "jpeg, png", "0.85", cancellationToken: TestContext.CancellationToken);

        long Bytes(BmagePipelineRunDto run, string format) => run.Encodings.Single(encoding => encoding.Format == format).Bytes!.Value;

        Assert.IsGreaterThan(Bytes(photo, "Jpeg"), Bytes(photo, "Png"), "A photo compressed better as PNG than as JPEG.");
        Assert.IsLessThan(Bytes(screenshot, "Jpeg"), Bytes(screenshot, "Png"), "A flat-colour screenshot compressed better as JPEG than as PNG.");

        Assert.IsTrue(graphic.OutputHasTransparency);
        Assert.Contains("flattened", graphic.Encodings.Single(encoding => encoding.Format == "Jpeg").Note!);
        Assert.IsFalse(photo.OutputHasTransparency);
    }

    [TestMethod]
    public async Task Run_AnOversizedSource_IsClamped_AndSaysSo()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(100, 100)", "photo", 9000, 9000, "jpeg", "0.8", cancellationToken: TestContext.CancellationToken);

        Assert.IsLessThanOrEqualTo(BmagePipelineLab.MaxEdge, run.SourceWidth);
        Assert.IsLessThanOrEqualTo(BmagePipelineLab.MaxPixels, (long)run.SourceWidth * run.SourceHeight);
        Assert.Contains(warning => warning.Contains("9000 x 9000", StringComparison.Ordinal), run.Warnings, string.Join(" | ", run.Warnings));
    }

    [TestMethod]
    public async Task Run_ArgumentSlips_AreCorrected_AndReported()
    {
        var run = await BmagePipelineLab.RunAsync("MaxSize(100, 100)", "hologram", 300, 200, "jpeg, tiff", "80, 150, nope", cancellationToken: TestContext.CancellationToken);

        Assert.IsNull(run.Error, run.Error);
        Assert.AreEqual("photo", run.Source);

        // "80" is a percentage and read as 0.8; "150" is no quality at all; "nope" is not a number; tiff is not a format.
        Assert.AreSequenceEqual(new double?[] { 0.8 }, run.Encodings.Select(encoding => encoding.Quality).ToArray());
        Assert.HasCount(4, run.Warnings, string.Join(" | ", run.Warnings));
    }

    /// <summary>
    /// A pipeline that fails at run time - a crop outside the image - returns the library's own
    /// message, which names the step.
    /// </summary>
    [TestMethod]
    public async Task Run_APipelineThatFails_ReturnsTheLibrarysMessage_AsData()
    {
        var run = await BmagePipelineLab.RunAsync("Crop(5000, 5000, 10, 10)", "photo", 400, 300, cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(run.Error);
        Assert.Contains("Crop", run.Error);
        Assert.IsEmpty(run.Encodings);
    }

    /// <summary>
    /// The source cap does not bound a run on its own: a pipeline can grow the image past it. The run's
    /// own limits refuse that instead of letting a public call allocate the library's 64 megapixel default.
    /// </summary>
    [TestMethod]
    public async Task Run_APipelineThatGrowsPastTheServersLimits_IsRefused()
    {
        var run = await BmagePipelineLab.RunAsync("Resize(8000, 6000)", "photo", 400, 300, cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(run.Error);
        Assert.IsEmpty(run.Encodings);
    }

    [TestMethod]
    public async Task Run_APipelineChainingTooManyCalls_IsNotRun()
    {
        var spec = string.Join(".", Enumerable.Repeat("Blur(1)", BmagePipelineSpec.MaxOperations + 1));

        var run = await BmagePipelineLab.RunAsync(spec, "photo", 400, 300, cancellationToken: TestContext.CancellationToken);

        Assert.Contains($"at most {BmagePipelineSpec.MaxOperations}", run.Error!);
    }

    [TestMethod]
    public async Task Run_AnUnreadablePipeline_ReturnsTheReadersExplanation()
    {
        var run = await BmagePipelineLab.RunAsync("Teleport(1)", cancellationToken: TestContext.CancellationToken);

        Assert.Contains("no method called 'Teleport'", run.Error!);
    }

    [TestMethod]
    [DataRow("jpeg")]
    [DataRow("png")]
    public async Task Metadata_EachPolicy_KeepsWhatItIsDocumentedToKeep(string format)
    {
        var comparison = await BmagePipelineLab.CompareMetadataPoliciesAsync(format, TestContext.CancellationToken);

        Assert.IsNull(comparison.Error, comparison.Error);
        Assert.HasCount(8, comparison.Original);

        var policies = comparison.Policies.ToDictionary(policy => policy.Policy);

        Assert.IsEmpty(policies["Remove"].Kept);
        Assert.IsFalse(policies["Remove"].LeaksLocation);

        var stripped = policies["StripSensitive"];
        Assert.IsFalse(stripped.LeaksLocation);
        Assert.IsFalse(stripped.LeaksIdentity);
        Assert.Contains(field => field.StartsWith("Camera:", StringComparison.Ordinal), stripped.Kept, "StripSensitive dropped the camera.");
        CollectionAssert.IsSubsetOf(new[] { "Location", "Serial number", "Owner" }, stripped.Removed);

        Assert.IsTrue(policies["Preserve"].LeaksLocation);
        Assert.IsTrue(policies["Preserve"].LeaksIdentity);
    }

    [TestMethod]
    public async Task Metadata_ABrowserOrUnknownFormat_IsExplained()
    {
        Assert.Contains("browser", (await BmagePipelineLab.CompareMetadataPoliciesAsync("avif", TestContext.CancellationToken)).Error!);
        Assert.Contains("'jpeg', 'png', 'bmp' or 'webp'", (await BmagePipelineLab.CompareMetadataPoliciesAsync("tiff", TestContext.CancellationToken)).Error!);
    }

    [TestMethod]
    public async Task Metadata_WebP_IsWrittenAndReadBackOnTheServer()
    {
        var comparison = await BmagePipelineLab.CompareMetadataPoliciesAsync("webp", TestContext.CancellationToken);
        var policies = comparison.Policies.ToDictionary(policy => policy.Policy);

        Assert.IsNull(comparison.Error);
        Assert.IsFalse(policies["Remove"].LeaksLocation);
        Assert.IsTrue(policies["Preserve"].LeaksLocation);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
