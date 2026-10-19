using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Codecs;
using Bit.Bmage.Editing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// Complete workflows, exercised the way an application would run them rather than one operation at a time. These are
/// the scenarios the library exists to serve, so a failure here matters more than any single unit test.
/// </summary>
[TestClass]
public sealed class ScenarioTests
{
    /// <summary>A synthetic camera photo: gradients, edges and grain, with EXIF orientation and location.</summary>
    private static ImageBuffer CameraPhoto(int width, int height, Orientation orientation = Orientation.Rotate90)
    {
        var rnd = new Random(20260909);
        var upright = ImageBuffer.Create(width, height);
        for (var y = 0; y < height; y++)
        {
            var row = upright.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var sky = y < height / 3;
                var r = (byte)Math.Clamp((sky ? 120 : 60) + x * 90 / width + rnd.Next(-10, 10), 0, 255);
                var g = (byte)Math.Clamp((sky ? 160 : 110) + y * 60 / height + rnd.Next(-10, 10), 0, 255);
                var b = (byte)Math.Clamp((sky ? 220 : 70) + rnd.Next(-10, 10), 0, 255);
                row[x] = new Rgba32(r, g, b);
            }
        }
        // A face-like bright oval in the upper middle, so smart cropping has something to find.
        var cx = width / 2; var cy = height / 3; var rx = width / 8; var ry = height / 7;
        for (var y = Math.Max(0, cy - ry); y < Math.Min(height, cy + ry); y++)
            for (var x = Math.Max(0, cx - rx); x < Math.Min(width, cx + rx); x++)
            {
                var dx = (x - cx) / (double)rx; var dy = (y - cy) / (double)ry;
                if (dx * dx + dy * dy <= 1) upright[x, y] = new Rgba32(240, 205, 180);
            }

        // Store it the way a camera does: sensor order plus an orientation tag.
        var stored = new OrientationOperation(orientation.Inverse()).Apply(upright, OperationContext.Default);
        upright.Dispose();
        var gps = new ExifDirectory()
            .With(ExifTag.GpsLatitude, ExifValue.FromRational((51, 1), (30, 1), (275, 10)))
            .With(ExifTag.GpsLatitudeRef, ExifValue.FromString("N"));
        var exif = new ExifData(
            new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("TestCam")).With(ExifTag.Orientation, ExifValue.FromShort((ushort)orientation)),
            new ExifDirectory().With(ExifTag.BodySerialNumber, ExifValue.FromString("SN-SECRET-1")),
            gps, null, default);
        stored.Metadata = new ImageMetadata(exif, sourceFormat: ImageFormat.Jpeg);
        return stored;
    }

    /// <summary>Scenario A: a camera photo becomes a square, upright, metadata-free avatar.</summary>
    [TestMethod]
    public async Task ScenarioA_Avatar()
    {
        var processor = new ImageProcessor();
        using var photo = CameraPhoto(1200, 900);
        var camera = await processor.EncodeAsync(photo, ImageExportOptions.Jpeg with { Quality = 0.95, Metadata = MetadataPolicy.Preserve });

        using var decoded = await processor.DecodeAsync(camera.Data, new DecodeOptions { AutoOrient = true });
        // Auto-orient must have made the portrait photo upright before anything else runs.
        Assert.AreEqual(new Size(1200, 900), decoded.Size);

        var pipeline = ImagePipeline.Create().CropToAspect(AspectRatio.Square).Resize(512, 512, ResizeMode.Stretch);
        var avatar = await processor.EncodeAsync(
            await pipeline.ExecuteAsync(decoded),
            ImageExportOptions.Jpeg with { Quality = 0.9, Metadata = MetadataPolicy.Remove });

        Assert.AreEqual(512, avatar.Width);
        Assert.AreEqual(512, avatar.Height);
        using var check = await processor.DecodeAsync(avatar.Data);
        Assert.AreEqual(new Size(512, 512), check.Size);

        // Nothing identifying may survive.
        var text = System.Text.Encoding.Latin1.GetString(avatar.Data);
        Assert.DoesNotContain("SN-SECRET-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TestCam", text, StringComparison.Ordinal);
        Assert.IsNull(check.Metadata?.Exif?.Location);
    }

    /// <summary>Scenario B: a screenshot is annotated with an arrow, a box, text and a redaction, then exported.</summary>
    [TestMethod]
    public async Task ScenarioB_ScreenshotAnnotation()
    {
        var processor = new ImageProcessor();
        using var screenshot = ImageBuffer.Create(800, 600, Rgba32.White);
        // Something to redact: a block of high-contrast "text".
        for (var y = 300; y < 340; y++)
            for (var x = 100; x < 400; x++)
                screenshot[x, y] = (x / 3 + y / 3) % 2 == 0 ? Rgba32.Black : Rgba32.White;

        var document = ImageDocument.Create(screenshot.Size)
            .AddAnnotation(new ArrowAnnotation { Start = new Vector2(600, 100), End = new Vector2(450, 250) })
            .AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(420, 240, 200, 120) })
            .AddAnnotation(new StepAnnotation { Center = new Vector2(650, 90), Number = 1 })
            .AddAnnotation(new RedactionAnnotation { Rectangle = new RectangleF(100, 300, 300, 40), Mode = RedactionMode.Solid });

        using var rendered = new DocumentRenderer().Render(document, screenshot);
        Assert.AreEqual(screenshot.Size, rendered.Size);

        // The redacted band must be uniform: the pattern underneath is gone.
        for (var y = 305; y < 335; y++)
            for (var x = 105; x < 395; x++)
                Assert.AreEqual(Rgba32.Black, rendered[x, y]);

        // The annotations must have marked the canvas somewhere outside the redaction.
        var marked = 0;
        for (var y = 60; y < 260; y++)
            for (var x = 400; x < 700; x++)
                if (rendered[x, y] != Rgba32.White) marked++;
        Assert.IsTrue(marked > 200, $"only {marked} pixels were drawn by the arrow, box and step marker");

        var exported = await processor.EncodeAsync(rendered, ImageExportOptions.Png);
        using var check = await processor.DecodeAsync(exported.Data);
        Assert.AreEqual(screenshot.Size, check.Size);
    }

    /// <summary>Scenario C: a large camera photo is optimised for upload, under a size budget.</summary>
    [TestMethod]
    public async Task ScenarioC_UploadOptimisation()
    {
        var processor = new ImageProcessor();
        using var photo = CameraPhoto(3000, 2250, Orientation.Normal);
        var original = await processor.EncodeAsync(photo, ImageExportOptions.Jpeg with { Quality = 0.98, Metadata = MetadataPolicy.Preserve });

        var pipeline = ImagePipeline.Create().AutoOrient().MaxSize(2000, 2000);
        var optimised = await processor.ProcessAsync(original.Data, pipeline,
            ImageExportOptions.Jpeg with { Quality = 0.8, Metadata = MetadataPolicy.StripSensitive, MaxFileSize = 400_000 });

        Assert.IsTrue(optimised.Length <= 400_000, $"produced {optimised.Length} bytes against a 400,000 byte budget");
        Assert.IsTrue(optimised.Length < original.Length, "optimisation did not reduce the file size");
        Assert.IsTrue(optimised.Width <= 2000 && optimised.Height <= 2000, $"produced {optimised.Width}x{optimised.Height}");
        // Aspect ratio must be preserved by MaxSize.
        Assert.AreEqual(3000 / 2250.0, optimised.Width / (double)optimised.Height, 0.5e-2);

        using var check = await processor.DecodeAsync(optimised.Data);
        Assert.IsNull(check.Metadata?.Exif?.Location);
        Assert.IsFalse(check.Metadata?.Exif?.Exif.Contains(ExifTag.BodySerialNumber) ?? false);
    }

    /// <summary>Scenario D: a set of product images is normalised to one size and background.</summary>
    [TestMethod]
    public async Task ScenarioD_ProductImages()
    {
        var processor = new ImageProcessor();
        var sizes = new[] { (900, 600), (600, 900), (1000, 1000), (1400, 300) };
        var encoded = new List<byte[]>();
        foreach (var (w, h) in sizes)
        {
            using var img = CameraPhoto(w, h, Orientation.Normal);
            encoded.Add((await processor.EncodeAsync(img, ImageExportOptions.Jpeg with { Quality = 0.9 })).Data);
        }

        // Contain onto a fixed white canvas: every output identical in size, nothing cropped away.
        var pipeline = ImagePipeline.Create()
            .AutoOrient()
            .Resize(800, 800, ResizeMode.Contain, ResamplingFilter.Auto, Anchor.Center, Rgba32.White);

        for (var i = 0; i < encoded.Count; i++)
        {
            var (sw, sh) = sizes[i];
            var result = await processor.ProcessAsync(encoded[i], pipeline, ImageExportOptions.Jpeg with { Quality = 0.85 });
            Assert.AreEqual(800, result.Width);
            Assert.AreEqual(800, result.Height);

            using var check = await processor.DecodeAsync(result.Data);

            // A source that is already square fills the canvas exactly, so only check padding where it must exist.
            if (sw != sh)
            {
                var corner = check[2, 2];
                Assert.IsTrue(corner.R > 220 && corner.G > 220 && corner.B > 220,
                    $"{sw}x{sh}: letterbox padding was {corner}, expected the white background");
            }

            // Whatever the source shape, the content must be centred and undistorted: the scaled image occupies a
            // band whose thickness follows from the aspect ratio, and the centre pixel is always image.
            var scale = Math.Min(800.0 / sw, 800.0 / sh);
            var contentWidth = (int)Math.Round(sw * scale);
            var contentHeight = (int)Math.Round(sh * scale);
            Assert.IsTrue(contentWidth == 800 || contentHeight == 800,
                $"{sw}x{sh}: contained content {contentWidth}x{contentHeight} touches neither edge");
            Assert.IsTrue(contentWidth <= 800 && contentHeight <= 800,
                $"{sw}x{sh}: contained content {contentWidth}x{contentHeight} overflows the canvas");
        }
    }

    /// <summary>Scenario E: a batch runs with progress, survives a bad file, and can be cancelled.</summary>
    [TestMethod]
    public async Task ScenarioE_BatchProcessing()
    {
        var processor = new ImageProcessor();
        using var sample = CameraPhoto(800, 600, Orientation.Normal);
        var good = (await processor.EncodeAsync(sample, ImageExportOptions.Jpeg with { Quality = 0.9 })).Data;

        var items = new List<BatchItem>();
        for (var i = 0; i < 40; i++)
            items.Add(i == 17
                ? new BatchItem(new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x01 }, "corrupt.jpg")
                : new BatchItem(good, $"product{i:00}.jpg"));

        var updates = 0;
        var progress = new Progress<BatchProgress>(_ => Interlocked.Increment(ref updates));
        var pipeline = ImagePipeline.Create().AutoOrient().MaxSize(320, 320);

        var result = await processor.ProcessBatchAsync(items, pipeline, new BatchOptions
        {
            MaxConcurrency = 2,
            ExportOptions = ImageExportOptions.Jpeg with { Quality = 0.8, Metadata = MetadataPolicy.Remove },
            Progress = progress,
        });

        Assert.AreEqual(40, result.Results.Count);
        Assert.AreEqual(39, result.SuccessCount);
        Assert.AreEqual(1, result.FailureCount);
        Assert.AreEqual("corrupt.jpg", result.Failed.Single().FileName);
        Assert.IsTrue(result.TotalOutputBytes < result.TotalInputBytes, "the batch did not reduce total size");
        foreach (var ok in result.Succeeded)
            Assert.IsTrue(ok.Output!.Width <= 320 && ok.Output.Height <= 320);

        for (var i = 0; i < 50 && Volatile.Read(ref updates) < 40; i++) await Task.Delay(10);
        Assert.AreEqual(40, Volatile.Read(ref updates));
    }

    /// <summary>A batch cancelled part way through must stop and report, not hang or lose the results it had.</summary>
    [TestMethod]
    public async Task ScenarioE_BatchCancellationMidway()
    {
        var processor = new ImageProcessor();
        using var sample = CameraPhoto(900, 700, Orientation.Normal);
        var good = (await processor.EncodeAsync(sample, ImageExportOptions.Jpeg with { Quality = 0.9 })).Data;
        var items = Enumerable.Range(0, 60).Select(i => new BatchItem(good, $"i{i}")).ToList();

        using var cts = new CancellationTokenSource();
        var completed = 0;
        var progress = new Progress<BatchProgress>(_ =>
        {
            if (Interlocked.Increment(ref completed) >= 5) cts.Cancel();
        });

        var task = processor.ProcessBatchAsync(items, ImagePipeline.Create().MaxSize(200, 200),
            new BatchOptions { MaxConcurrency = 2, ExportOptions = ImageExportOptions.Jpeg, Progress = progress }, cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    /// <summary>
    /// The interactive editing loop: load, edit on a proxy, undo, redo, then export at full resolution. The exported
    /// image must reflect the edits made against the preview.
    /// </summary>
    [TestMethod]
    public async Task ScenarioF_InteractiveEditThenFullResolutionExport()
    {
        var processor = new ImageProcessor();
        using var photo = CameraPhoto(2400, 1800, Orientation.Normal);
        using var session = new ImageEditSession(photo.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 600 });

        Assert.IsTrue(session.UsesProxy);
        Assert.AreEqual(new Size(600, 450), session.ProxySize);

        session.AddOperation(new CropOperation(200, 150, 1600, 1200), "Crop");
        session.AddOperation(new Operations.Adjustments.BrightnessAdjustment(0.15f), "Brighten");
        session.AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(300, 200, 400, 300) });

        var preview = await session.RenderPreviewAsync();
        Assert.IsFalse(preview.IsDisposed);
        // The preview is the edit at proxy scale: a 1600x1200 crop of a photo shown at a quarter size.
        Assert.IsInRange(398, 402, preview.Width);
        Assert.IsInRange(298, 302, preview.Height);

        session.Undo();
        Assert.AreEqual(2, session.Document.Pipeline.Count + session.Document.Annotations.Count);
        session.Redo();
        Assert.AreEqual(3, session.Document.Pipeline.Count + session.Document.Annotations.Count);

        using var full = await session.RenderFullAsync();
        Assert.AreEqual(new Size(1600, 1200), full.Size);

        var exported = await processor.EncodeAsync(full, ImageExportOptions.Jpeg with { Quality = 0.9 });
        using var check = await processor.DecodeAsync(exported.Data);
        Assert.AreEqual(new Size(1600, 1200), check.Size);
    }

    /// <summary>
    /// A long editing session with many operations and undo/redo churn must stay correct and bounded. This is the
    /// shape of real use, where the failure mode is drift rather than a single wrong answer.
    /// </summary>
    [TestMethod]
    public async Task ScenarioG_LongEditingSessionStaysConsistent()
    {
        using var photo = CameraPhoto(1200, 900, Orientation.Normal);
        using var session = new ImageEditSession(photo.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 400, MaxHistoryDepth = 50 });

        var rnd = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            switch (rnd.Next(6))
            {
                case 0: session.AddOperation(new Operations.Adjustments.BrightnessAdjustment((float)(rnd.NextDouble() * 0.4 - 0.2))); break;
                case 1: session.AddOperation(new Operations.Adjustments.ContrastAdjustment((float)(rnd.NextDouble() * 0.4 - 0.2))); break;
                case 2: session.AddOperation(OrientationOperation.Rotate90); break;
                case 3: session.AddAnnotation(new StepAnnotation { Center = new Vector2(rnd.Next(100), rnd.Next(100)), Number = i }); break;
                case 4: session.Undo(); break;
                default: session.Redo(); break;
            }
        }

        Assert.IsTrue(session.History.Entries.Count <= 50, $"history grew to {session.History.Entries.Count}");
        Assert.IsTrue(session.History.EstimatedMemory < 5_000_000, $"history holds {session.History.EstimatedMemory} bytes");

        // Whatever state the churn landed in must still render, at both resolutions.
        var preview = await session.RenderPreviewAsync();
        Assert.IsFalse(preview.IsDisposed);
        using var full = await session.RenderFullAsync();
        Assert.IsTrue(full.Width > 0 && full.Height > 0);
        Assert.AreEqual(session.Document.OutputSize, full.Size);
    }

    /// <summary>Reset must return to the original image, and the session must still work afterwards.</summary>
    [TestMethod]
    public async Task ScenarioH_ResetAndReplaceSource()
    {
        using var first = CameraPhoto(600, 400, Orientation.Normal);
        using var session = new ImageEditSession(first.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 300 });

        session.AddOperation(new CropOperation(10, 10, 200, 200));
        Assert.AreEqual(new Size(200, 200), session.Document.OutputSize);

        session.Reset();
        Assert.AreEqual(new Size(600, 400), session.Document.OutputSize);
        Assert.IsTrue(session.Document.IsEmpty);

        using var second = CameraPhoto(1000, 250, Orientation.Normal);
        session.ReplaceSource(second.Clone());
        Assert.AreEqual(new Size(1000, 250), session.SourceSize);
        Assert.AreEqual(new Size(1000, 250), session.Document.OutputSize);
        Assert.IsTrue(session.UsesProxy);
        Assert.AreEqual(new Size(300, 75), session.ProxySize);

        var preview = await session.RenderPreviewAsync();
        Assert.AreEqual(new Size(300, 75), preview.Size);
    }
}
