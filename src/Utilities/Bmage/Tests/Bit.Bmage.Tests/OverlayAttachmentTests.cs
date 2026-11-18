using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

/// <summary>
/// Annotations and layers live in output coordinates; a geometric edit applied with its overlays attached must move them
/// with the content they mark.
/// </summary>
[TestClass]
public sealed class OverlayAttachmentTests
{
    private static readonly Size Source = new(200, 100);
    private static readonly Vector2 Marked = new(60.5f, 30.5f);

    /// <summary>Black, with one white pixel at <see cref="Marked"/>: wherever the white goes, the content went.</summary>
    private static BmageImageBuffer Marker()
    {
        var image = BmageImageBuffer.Create(Source.Width, Source.Height, BmageRgba32.Black);
        image[(int)Marked.X, (int)Marked.Y] = BmageRgba32.White;
        return image;
    }

    /// <summary>The centre of the brightest pixels: where the marker landed.</summary>
    private static Vector2 FindMarker(BmageImageBuffer image)
    {
        var best = 0;
        for (var y = 0; y < image.Height; y++) foreach (var p in image.GetRow(y)) best = Math.Max(best, p.R);
        var sum = Vector2.Zero;
        var count = 0;
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
                if (image[x, y].R >= best - 2) { sum += new Vector2(x + 0.5f, y + 0.5f); count++; }
        return sum / count;
    }

    private static IEnumerable<object[]> GeometricEdits() => new (string, IBmageImageOperation)[]
    {
        ("crop", new BmageCropOperation(20, 10, 120, 80)),
        ("quarter turn", BmageOrientationOperation.Rotate90),
        ("half turn", BmageOrientationOperation.FromRotation(180)),
        ("horizontal flip", BmageOrientationOperation.FlipHorizontal),
        ("vertical flip", BmageOrientationOperation.FlipVertical),
        ("resize, both axes apart", new BmageResizeOperation(150, 120, BmageResizeMode.Stretch)),
        ("canvas margins", BmageCanvasSizeOperation.Margins(15, 5, 0, 20)),
        ("canvas size", new BmageCanvasSizeOperation(260, 90, BmageAnchor.BottomRight)),
        ("straighten", new BmageStraightenOperation(-8)),
        ("rotate", new BmageRotateOperation(30)),
    }.Select(e => new object[] { e.Item1, e.Item2 });

    [TestMethod]
    [DynamicData(nameof(GeometricEdits))]
    public void AnArrowKeepsPointingAtWhatItMarked(string name, IBmageImageOperation edit)
    {
        using var marker = Marker();
        var document = BmageImageDocument.Create(Source).AddAnnotation(new BmageArrowAnnotation { Start = new Vector2(10, 90), End = Marked });
        var edited = document.AddOperation(edit, keepOverlaysAttached: true);
        using var output = edited.Pipeline.Execute(marker);
        var arrow = (BmageArrowAnnotation)edited.Annotations[0];
        Assert.IsLessThan(1.5f, Vector2.Distance(FindMarker(output), arrow.End), $"{name}: the arrow points at {arrow.End}, the content is at {FindMarker(output)}");
    }

    [TestMethod]
    [DynamicData(nameof(GeometricEdits))]
    public void EveryKindOfOverlayFollowsTheContent(string name, IBmageImageOperation edit)
    {
        using var marker = Marker();
        var box = new RectangleF(Marked.X - 10, Marked.Y - 6, 20, 12);
        var document = BmageImageDocument.Create(Source)
            .AddAnnotation(new BmageRectangleAnnotation { Rectangle = box })
            .AddAnnotation(new BmageEllipseAnnotation { Rectangle = box, Rotation = 20 })
            .AddAnnotation(new BmageHighlightAnnotation { Rectangle = box })
            .AddAnnotation(new BmageRedactionAnnotation { Rectangle = box })
            .AddAnnotation(new BmageTextAnnotation { Text = "x", Position = Marked, MeasuredSize = new SizeF(10, 10), Style = new BmageTextStyle { Alignment = BmageTextAlignment.Center, Baseline = BmageTextBaseline.Middle } })
            .AddAnnotation(new BmageStepAnnotation { Center = Marked, Number = 1 })
            .AddAnnotation(new BmageCalloutAnnotation { Bubble = new RectangleF(120, 60, 50, 30), Target = Marked })
            .AddAnnotation(new BmageFreehandAnnotation { Points = [Marked - new Vector2(5, 0), Marked, Marked + new Vector2(5, 3)] })
            .AddAnnotation(new BmagePolygonAnnotation { Points = [Marked - new Vector2(4, 4), Marked + new Vector2(4, -4), Marked + new Vector2(0, 5)] });
        var edited = document.AddOperation(edit, keepOverlaysAttached: true);
        using var output = edited.Pipeline.Execute(marker);
        var content = FindMarker(output);

        foreach (var annotation in edited.Annotations)
        {
            var at = annotation switch
            {
                BmageCalloutAnnotation callout => callout.Target,
                BmageFreehandAnnotation freehand => freehand.Points[1],
                _ => Centre(annotation.GetBounds()),
            };
            Assert.IsLessThan(2.5f, Vector2.Distance(content, at), $"{name}: the {annotation.Kind} is at {at}, the content at {content}");
        }
    }

    [TestMethod]
    public void AQuarterTurnTurnsAnUprightBoxIntoAnUprightBox()
    {
        var document = BmageImageDocument.Create(Source).AddAnnotation(new BmageRectangleAnnotation { Rectangle = new RectangleF(10, 20, 40, 10) });
        var turned = (BmageRectangleAnnotation)document.AddOperation(BmageOrientationOperation.Rotate90, keepOverlaysAttached: true).Annotations[0];
        Assert.AreEqual(0f, turned.Rotation);
        Assert.AreEqual(10f, turned.Rectangle.Width, 1e-3f);
        Assert.AreEqual(40f, turned.Rectangle.Height, 1e-3f);
    }

    [TestMethod]
    public void AMirrorMovesTextWithoutMirroringIt()
    {
        var document = BmageImageDocument.Create(Source).AddAnnotation(new BmageTextAnnotation { Text = "Label", Position = new Vector2(20, 20), MeasuredSize = new SizeF(40, 12) });
        foreach (var flip in new[] { BmageOrientationOperation.FlipHorizontal, BmageOrientationOperation.FlipVertical })
        {
            var text = (BmageTextAnnotation)document.AddOperation(flip, keepOverlaysAttached: true).Annotations[0];
            Assert.AreEqual(0f, text.Rotation, $"{flip}: the text stays upright and readable");
        }
    }

    [TestMethod]
    public void AStraightenReEditedMovesTheOverlaysOnlyByTheChange()
    {
        var document = BmageImageDocument.Create(Source)
            .AddOperation(new BmageStraightenOperation(4), keepOverlaysAttached: true)
            .AddAnnotation(new BmageArrowAnnotation { Start = new Vector2(20, 20), End = new Vector2(70, 40) });
        // Dragging the slider replaces the step: the overlays follow from the old angle to the new one, not from zero.
        var redone = document.WithPipeline(document.Pipeline.Replace(0, new BmageStraightenOperation(9)), keepOverlaysAttached: true);
        var direct = BmageImageDocument.Create(Source)
            .AddAnnotation(new BmageArrowAnnotation { Start = new Vector2(20, 20), End = new Vector2(70, 40) }.Transform(Inverse(document.Pipeline, Source)))
            .AddOperation(new BmageStraightenOperation(9), keepOverlaysAttached: true);
        var a = (BmageArrowAnnotation)redone.Annotations[0];
        var b = (BmageArrowAnnotation)direct.Annotations[0];
        Assert.IsLessThan(0.01f, Vector2.Distance(a.End, b.End));
        Assert.IsLessThan(0.01f, Vector2.Distance(a.Start, b.Start));
    }

    private static Matrix3x2 Inverse(BmageImagePipeline pipeline, Size size)
    {
        Assert.IsTrue(pipeline.TryGetTransform(size, out var m));
        Assert.IsTrue(Matrix3x2.Invert(m, out var inverse));
        return inverse;
    }

    [TestMethod]
    public void PixelAdjustmentsDoNotMoveAnything()
    {
        var arrow = new BmageArrowAnnotation { Start = new Vector2(10, 10), End = new Vector2(50, 50) };
        var document = BmageImageDocument.Create(Source).AddAnnotation(arrow);
        var adjusted = document.AddOperation(new BmageExposureAdjustment(0.4f), keepOverlaysAttached: true);
        Assert.AreSame(arrow, adjusted.Annotations[0], "a step that moves nothing leaves the overlays as they are");
    }

    [TestMethod]
    public void WithoutAttachmentOrWithANonAffineStepOverlaysStayInOutputCoordinates()
    {
        var arrow = new BmageArrowAnnotation { Start = new Vector2(10, 10), End = new Vector2(50, 50) };
        var document = BmageImageDocument.Create(Source).AddAnnotation(arrow);
        Assert.AreSame(arrow, document.AddOperation(new BmageCropOperation(20, 20, 100, 60)).Annotations[0], "AddOperation keeps its meaning");
        var perspective = new BmagePerspectiveTransformOperation([new PointF(5, 2), new PointF(190, 10), new PointF(200, 95), new PointF(0, 100)]);
        Assert.AreSame(arrow, document.AddOperation(perspective, keepOverlaysAttached: true).Annotations[0], "a perspective warp has no affine map");
    }

    [TestMethod]
    public void LayersAndTheirMasksFollowTheContent()
    {
        using var logo = BmageImageBuffer.Create(10, 10, BmageRgba32.White);
        var document = BmageImageDocument.Create(Source)
            .AddLayer(new BmageImageLayer { Image = logo, Bounds = new RectangleF(50, 20, 20, 20) })
            .AddLayer(new BmagePaintLayer([new BmagePaintStroke([new(40, 30), Marked], 6, BmageRgba32.White)]))
            .AddLayer(new BmageFillLayer(BmageRgba32.White) { Mask = new BmageEllipseMask(new RectangleF(50, 20, 20, 20)) })
            .AddLayer(new BmageFillLayer(BmageRgba32.White) { Mask = BmageMask.Ellipse() });
        var crop = new BmageCropOperation(30, 10, 120, 80);
        var cropped = document.AddOperation(crop, keepOverlaysAttached: true);
        Assert.AreEqual(new RectangleF(20, 10, 20, 20), ((BmageImageLayer)cropped.Layers[0]).Bounds);
        Assert.AreEqual(Marked - new Vector2(30, 10), ((BmagePaintLayer)cropped.Layers[1]).Strokes[0].Points[1]);
        Assert.AreEqual(new RectangleF(20, 10, 20, 20), ((BmageEllipseMask)cropped.Layers[2].Mask!).Rectangle);
        Assert.AreSame(document.Layers[3].Mask, cropped.Layers[3].Mask, "a relative mask fits whatever image it is applied to");

        // A resize stretching the axes apart maps an upright layer's bounds exactly.
        var stretched = document.AddOperation(new BmageResizeOperation(400, 50, BmageResizeMode.Stretch), keepOverlaysAttached: true);
        Assert.AreEqual(new RectangleF(100, 10, 40, 10), ((BmageImageLayer)stretched.Layers[0]).Bounds);
    }

    [TestMethod]
    public void AMirroredImageLayerMirrorsItsPixelsToo()
    {
        // A layer half red, half blue: after a horizontal flip of the image, blue must be on the left.
        using var picture = BmageImageBuffer.Create(20, 10, new BmageRgba32(255, 0, 0));
        picture.Fill(new Rectangle(10, 0, 10, 10), new BmageRgba32(0, 0, 255));
        using var source = BmageImageBuffer.Create(Source.Width, Source.Height, BmageRgba32.Black);
        var document = BmageImageDocument.Create(Source).AddLayer(new BmageImageLayer { Image = picture, Bounds = new RectangleF(40, 40, 20, 10) });
        var flipped = document.AddOperation(BmageOrientationOperation.FlipHorizontal, keepOverlaysAttached: true);
        using var rendered = new BmageDocumentRenderer().Render(flipped, source);
        var layer = (BmageImageLayer)flipped.Layers[0];
        var left = rendered[(int)layer.Bounds.Left + 2, 45];
        var right = rendered[(int)layer.Bounds.Right - 3, 45];
        Assert.IsGreaterThan(200, (int)left.B, $"left {left}");
        Assert.IsGreaterThan(200, (int)right.R, $"right {right}");
    }

    [TestMethod]
    public void ARedactionStillCoversWhatItHidAfterATurn()
    {
        using var marker = Marker();
        var document = BmageImageDocument.Create(Source).AddAnnotation(new BmageRedactionAnnotation { Rectangle = new RectangleF(Marked.X - 4, Marked.Y - 4, 8, 8), Color = new BmageRgba32(255, 0, 255) });
        foreach (var edit in new IBmageImageOperation[] { BmageOrientationOperation.Rotate90, new BmageStraightenOperation(13), BmageCanvasSizeOperation.Margins(30) })
        {
            var edited = document.AddOperation(edit, keepOverlaysAttached: true);
            using var plain = edited.Pipeline.Execute(marker);
            var at = FindMarker(plain);
            using var rendered = new BmageDocumentRenderer().Render(edited, marker);
            var covered = rendered[(int)at.X, (int)at.Y];
            Assert.AreEqual(new BmageRgba32(255, 0, 255), covered, $"{edit}: the hidden pixel shows through");
        }
    }

    [TestMethod]
    public void APipelineReportsTheAffineMapOfItsSteps()
    {
        var pipeline = BmageImagePipeline.Create().Crop(10, 10, 100, 50).Brightness(0.2f).FlipHorizontal().Pad(5);
        Assert.IsTrue(pipeline.TryGetTransform(Source, out var m));
        // (10,10) is the crop's corner, lands at x = 100 after the flip, then 5 further for the margin.
        var p = Vector2.Transform(new Vector2(10, 10), m);
        Assert.AreEqual(105f, p.X, 1e-3f);
        Assert.AreEqual(5f, p.Y, 1e-3f);
        Assert.IsFalse(BmageImagePipeline.Create().AutoOrient().TryGetTransform(Source, out _), "an orientation from metadata is not known in advance");
        Assert.IsFalse(BmageImagePipeline.Create().SmartCrop(BmageAspectRatio.Square).TryGetTransform(Source, out _), "a content-aware crop is not either");
    }

    private static Vector2 Centre(RectangleF r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);
}
