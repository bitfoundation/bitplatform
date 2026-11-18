using System.Buffers.Binary;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

/// <summary>Regression tests for defects found in a code review, one per defect.</summary>
[TestClass]
public sealed class ReviewRegressionTests
{
    // ---------------------------------------------------------------- drawing

    [TestMethod]
    public void RoundJoinsLeaveNoHoleWhereTheyOverlapOneSegment()
    {
        using var canvas = BmageImageCanvas.Create(100, 100);
        canvas.DrawPolyline([new Vector2(10, 50), new Vector2(50, 50), new Vector2(50, 90)], BmageRgba32.Black,
            new BmageStrokeStyle { Width = 20, Join = BmageLineJoin.Round });
        // Inside the second segment and the join circle, but not the first segment.
        Assert.AreEqual(255, canvas.Buffer[52, 52].A);
    }

    [TestMethod]
    public void AnOpenPolylineThatReturnsToItsStartDrawsItsLastSegment()
    {
        using var canvas = BmageImageCanvas.Create(100, 100);
        canvas.DrawPolyline([new Vector2(10, 10), new Vector2(90, 10), new Vector2(90, 90), new Vector2(10, 10)], BmageRgba32.Black,
            new BmageStrokeStyle { Width = 4 });
        Assert.IsTrue(canvas.Buffer[50, 50].A > 0, "the diagonal back to the start was not drawn");
    }

    [TestMethod]
    public void ANegativeDashOffsetShiftsThePattern()
    {
        static BmageImageBuffer Draw(float offset)
        {
            var canvas = BmageImageCanvas.Create(100, 10);
            canvas.DrawLine(new Vector2(0, 5), new Vector2(100, 5), BmageRgba32.Black,
                new BmageStrokeStyle { Width = 4, Cap = BmageLineCap.Butt, DashPattern = [10f, 10f], DashOffset = offset });
            return canvas.Snapshot();
        }
        using var zero = Draw(0);
        using var shifted = Draw(-5);
        Assert.IsFalse(zero.Bytes.SequenceEqual(shifted.Bytes));
    }

    [TestMethod]
    public void NonFiniteCoordinatesDoNotCrashOrBlankTheRestOfThePath()
    {
        using var canvas = BmageImageCanvas.Create(100, 100);
        var path = new BmagePathBuilder()
            .AddRectangle(new RectangleF(10, 10, 50, 50))
            .AddPolygon([new Vector2(0, 0), new Vector2(float.NaN, 20), new Vector2(30, float.NaN)])
            .Build();
        canvas.FillPath(path, BmageRgba32.Black);
        Assert.AreEqual(255, canvas.Buffer[30, 30].A);
    }

    [TestMethod]
    public void HugeCoordinatesStillPaintTheVisiblePart()
    {
        using var canvas = BmageImageCanvas.Create(100, 100);
        canvas.FillPolygon([new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 3e9f), new Vector2(0, 3e9f)], BmageRgba32.Black);
        Assert.AreEqual(255, canvas.Buffer[50, 50].A);
    }

    // ---------------------------------------------------------------- annotations

    [TestMethod]
    public void RotatedLinesAreHitWhereTheyAreDrawn()
    {
        var line = new BmageLineAnnotation { Start = new Vector2(0, 50), End = new Vector2(100, 50), Rotation = 90 };
        Assert.IsTrue(line.HitTest(new Vector2(50, 5)));
        Assert.IsFalse(line.HitTest(new Vector2(10, 50)));
    }

    [TestMethod]
    public void RotatedPixelateRedactionCoversTheRotatedRegion()
    {
        using var image = TestImages.Gradient(200, 200);
        using var original = image.Clone();
        var redaction = new BmageRedactionAnnotation
        {
            Rectangle = new RectangleF(50, 90, 100, 20),
            Mode = BmageRedactionMode.Pixelate,
            Strength = 8,
            Rotation = 90,
        };
        using var canvas = BmageImageCanvas.FromBuffer(image);
        redaction.RenderWithTransform(canvas, new BmageAnnotationRenderContext());

        // Rotated a quarter turn the strip runs vertically through the centre: the top of it must have changed, and a
        // point on the unrotated strip's far end, now outside it, must not.
        Assert.AreNotEqual(original[103, 55], image[103, 55]);
        Assert.AreEqual(original[55, 100], image[55, 100]);
    }

    // ---------------------------------------------------------------- pipeline

    [TestMethod]
    public void MergingACropWithANegativeOriginKeepsTheOutputSize()
    {
        using var source = TestImages.Gradient(200, 200);
        var pipeline = BmageImagePipeline.Create().Crop(-10, -10, 100, 100).Crop(0, 0, 50, 50);
        using var plain = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var optimised = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(plain.Size, optimised.Size);
        Assert.IsTrue(plain.Bytes.SequenceEqual(optimised.Bytes));
    }

    [TestMethod]
    public void ResizeCropFusionIsNotPlannedPastAutoOrient()
    {
        using var source = TestImages.Gradient(400, 300);
        source.Metadata = new BmageImageMetadata(BmageExifData.FromOrientation(BmageOrientation.Rotate90));
        var pipeline = BmageImagePipeline.Create().AutoOrient().Resize(100, 100, BmageResizeMode.Fit).Crop(0, 0, 50, 100);
        using var plain = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var optimised = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(new Size(50, 100), plain.Size);
        Assert.AreEqual(plain.Size, optimised.Size);
    }

    [TestMethod]
    public void AQuarterTurnWithoutExpandingKeepsTheCanvasSize()
    {
        using var source = TestImages.Gradient(400, 300);
        using var rotated = new BmageRotateOperation(90, expandCanvas: false).Apply(source, BmageOperationContext.Default);
        Assert.AreEqual(source.Size, rotated.Size);
    }

    [TestMethod]
    public void TheZerothPercentileIsTheDarkestLevelPresent()
    {
        using var image = BmageImageBuffer.Create(120, 1);
        for (var x = 0; x < 120; x++) image[x, 0] = new BmageRgba32((byte)(60 + x), (byte)(60 + x), (byte)(60 + x));
        var stats = BmageImageStatistics.Measure(image, maxSamples: 0);
        Assert.AreEqual(stats.Red.Minimum, stats.Red.GetPercentile(0));
    }

    // ---------------------------------------------------------------- history

    [TestMethod]
    public void UndoAfterADragReturnsToTheStateBeforeIt()
    {
        using var session = new BmageImageEditSession(TestImages.Gradient(64, 64));
        session.AddOperation(new BmageCropOperation(0, 0, 32, 32), "Crop");
        var beforeDrag = session.Document;

        for (var i = 1; i <= 3; i++)
        {
            var amount = i / 10f;
            session.ApplyTransient(d => d.AddOperation(new BmageBrightnessAdjustment(amount)));
        }
        session.Commit("Brightness");

        Assert.AreEqual(3, session.History.Entries.Count);
        Assert.AreEqual("Brightness", session.History.CurrentEntry.Label);
        session.Undo();
        Assert.AreSame(beforeDrag, session.Document);
    }

    [TestMethod]
    public void ApplyingTheFinalValueOfADragAddsNoSecondEntry()
    {
        using var session = new BmageImageEditSession(TestImages.Gradient(64, 64));
        session.ApplyTransient(d => d.AddOperation(new BmageBrightnessAdjustment(0.1f)));
        session.ApplyTransient(d => d.WithPipeline(d.Pipeline.Replace(0, new BmageBrightnessAdjustment(0.2f))));
        session.Apply(d => d.WithPipeline(d.Pipeline.Replace(0, new BmageBrightnessAdjustment(0.2f))), "Brightness");

        Assert.AreEqual(2, session.History.Entries.Count);
        Assert.AreEqual("Brightness", session.History.CurrentEntry.Label);
    }

    [TestMethod]
    public void CommittingWithoutADragAddsNothing()
    {
        using var session = new BmageImageEditSession(TestImages.Gradient(64, 64));
        session.AddOperation(new BmageBrightnessAdjustment(0.2f));
        session.Undo();
        session.Commit("Select");
        Assert.IsTrue(session.History.CanRedo, "a click that changed nothing discarded the redo trail");
    }

    [TestMethod]
    public void AHistoryDepthOfOneStillKeepsTheLatestEdit()
    {
        var original = BmageImageDocument.Create(new Size(10, 10));
        var history = new BmageEditHistory(original, maxDepth: 1);
        var edited = original.AddOperation(new BmageBrightnessAdjustment(0.5f));
        history.Push(edited, "edit");
        Assert.AreSame(edited, history.Current);
    }

    [TestMethod]
    public async Task APreviewRenderedBeforeAnEditIsStillStale()
    {
        using var session = new BmageImageEditSession(TestImages.Gradient(64, 64));
        var render = session.RenderPreviewAsync();
        session.AddOperation(new BmageBrightnessAdjustment(0.4f));
        await render;
        Assert.IsTrue(session.IsPreviewStale);
    }

    [TestMethod]
    public async Task DisposingDuringARenderDoesNotFaultIt()
    {
        var session = new BmageImageEditSession(TestImages.Gradient(512, 512));
        session.AddOperation(new BmageBlurFilter(8));
        var render = session.RenderPreviewAsync().AsTask();
        session.Dispose();
        var error = await Capture.ExceptionAsync(() => render);
        Assert.IsTrue(error is null or OperationCanceledException, $"unexpected {error?.GetType().Name}: {error?.Message}");
    }

    /// <summary>Holds a render at its first row until released, and says when it got there.</summary>
    private sealed class GateOperation : BmagePointOperation
    {
        public ManualResetEventSlim Reached { get; } = new();

        public ManualResetEventSlim Open { get; } = new();

        public override string Name => "Gate";

        public override void ProcessRow(Span<Vector4> pixels)
        {
            Reached.Set();
            Open.Wait(TimeSpan.FromSeconds(30));
        }
    }

    [TestMethod]
    public async Task DisposingDuringAnExportKeepsTheOriginalUntilTheExportIsDone()
    {
        var gate = new GateOperation();
        var session = new BmageImageEditSession(TestImages.Gradient(64, 64));
        session.AddOperation(gate);
        var original = session.Original;
        var export = Task.Run(async () => await session.RenderFullAsync());
        Assert.IsTrue(gate.Reached.Wait(TimeSpan.FromSeconds(30)), "the export started");

        session.Dispose();
        Assert.IsFalse(original.IsDisposed, "the export is still reading the original");

        gate.Open.Set();
        using (await export) { }
        for (var i = 0; i < 200 && !original.IsDisposed; i++) await Task.Delay(10);
        Assert.IsTrue(original.IsDisposed, "and it is released once the export is done");
    }

    // ---------------------------------------------------------------- codecs

    [TestMethod]
    public void JpegDecodesNeutralGreyWithoutAGreenCast()
    {
        using var source = BmageImageBuffer.Create(32, 32, new BmageRgba32(100, 100, 100));
        var encoded = BmageJpegEncoder.Encode(source, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 1.0 });
        using var decoded = BmageJpegDecoder.Decode(encoded.Data);
        var pixel = decoded[16, 16];
        Assert.AreEqual(pixel.R, pixel.G);
        Assert.AreEqual(pixel.B, pixel.G);
    }

    [TestMethod]
    public void BmpWithTheMinimumHeightIsRejectedCleanly()
    {
        var bmp = new byte[64];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), int.MinValue);
        BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(28), 24);
        Assert.ThrowsExactly<BmageImageDecodeException>(() => BmageBmpDecoder.Decode(bmp));
    }

    [TestMethod]
    public void BmpWithAnOverflowingHeaderSizeIsRejectedCleanly()
    {
        var bmp = new byte[64];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), int.MaxValue - 5);
        Assert.ThrowsExactly<BmageImageDecodeException>(() => BmageBmpDecoder.Decode(bmp));
    }

    [TestMethod]
    public void PngChunkLengthsThatWouldOverflowAreIgnored()
    {
        using var source = TestImages.Gradient(8, 8);
        var png = BmagePngEncoder.Encode(source).Data.ToArray();
        // Corrupt the IHDR length to just below int.MaxValue.
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(8), 0x7FFFFFF3);
        var error = Capture.Exception(() => BmagePngDecoder.Decode(png));
        Assert.IsTrue(error is null or BmageImageException, $"unexpected {error?.GetType().Name}");
    }

    [TestMethod]
    public void CloningIgnoresTheDefaultLimitsTheSourceWasAllowedPast()
    {
        using var wide = BmageImageBuffer.Create(40_000, 2, limits: BmageImageLimits.Unrestricted);
        using var copy = wide.Clone();
        Assert.AreEqual(wide.Size, copy.Size);
    }

    [TestMethod]
    public void JpegRefusesDimensionsItCannotStore()
    {
        using var wide = BmageImageBuffer.Create(70_000, 1, limits: BmageImageLimits.Unrestricted);
        Assert.ThrowsExactly<BmageImageEncodeException>(() => BmageJpegEncoder.Encode(wide, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg }));
    }

    // ---------------------------------------------------------------- text

    [TestMethod]
    public void ALongWordAfterTheFirstIsStillBrokenToFit()
    {
        var font = BmageFontFile.Load(File.ReadAllBytes("Assets/BmageTest.ttf"));
        var rasterizer = new BmageFontTextRasterizer().Register("Test", font);
        var metrics = rasterizer.Measure("AB AAAAAAAAAAAA", new BmageTextStyle { FontFamily = "Test", FontSize = 10, MaxWidth = 40 });
        Assert.IsTrue(metrics.Width <= 40.5f, $"a line is {metrics.Width}px wide, past the 40px limit");
    }
}
