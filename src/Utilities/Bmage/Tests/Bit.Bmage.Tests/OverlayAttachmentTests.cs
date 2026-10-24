using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

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
    private static ImageBuffer Marker()
    {
        var image = ImageBuffer.Create(Source.Width, Source.Height, Rgba32.Black);
        image[(int)Marked.X, (int)Marked.Y] = Rgba32.White;
        return image;
    }

    /// <summary>The centre of the brightest pixels: where the marker landed.</summary>
    private static Vector2 FindMarker(ImageBuffer image)
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

    private static IEnumerable<object[]> GeometricEdits() => new (string, IImageOperation)[]
    {
        ("crop", new CropOperation(20, 10, 120, 80)),
        ("quarter turn", OrientationOperation.Rotate90),
        ("half turn", OrientationOperation.FromRotation(180)),
        ("horizontal flip", OrientationOperation.FlipHorizontal),
        ("vertical flip", OrientationOperation.FlipVertical),
        ("resize, both axes apart", new ResizeOperation(150, 120, ResizeMode.Stretch)),
        ("canvas margins", CanvasSizeOperation.Margins(15, 5, 0, 20)),
        ("canvas size", new CanvasSizeOperation(260, 90, Anchor.BottomRight)),
        ("straighten", new StraightenOperation(-8)),
        ("rotate", new RotateOperation(30)),
    }.Select(e => new object[] { e.Item1, e.Item2 });

    [TestMethod]
    [DynamicData(nameof(GeometricEdits))]
    public void AnArrowKeepsPointingAtWhatItMarked(string name, IImageOperation edit)
    {
        using var marker = Marker();
        var document = ImageDocument.Create(Source).AddAnnotation(new ArrowAnnotation { Start = new Vector2(10, 90), End = Marked });
        var edited = document.AddOperation(edit, keepOverlaysAttached: true);
        using var output = edited.Pipeline.Execute(marker);
        var arrow = (ArrowAnnotation)edited.Annotations[0];
        Assert.IsLessThan(1.5f, Vector2.Distance(FindMarker(output), arrow.End), $"{name}: the arrow points at {arrow.End}, the content is at {FindMarker(output)}");
    }

    [TestMethod]
    [DynamicData(nameof(GeometricEdits))]
    public void EveryKindOfOverlayFollowsTheContent(string name, IImageOperation edit)
    {
        using var marker = Marker();
        var box = new RectangleF(Marked.X - 10, Marked.Y - 6, 20, 12);
        var document = ImageDocument.Create(Source)
            .AddAnnotation(new RectangleAnnotation { Rectangle = box })
            .AddAnnotation(new EllipseAnnotation { Rectangle = box, Rotation = 20 })
            .AddAnnotation(new HighlightAnnotation { Rectangle = box })
            .AddAnnotation(new RedactionAnnotation { Rectangle = box })
            .AddAnnotation(new TextAnnotation { Text = "x", Position = Marked, MeasuredSize = new SizeF(10, 10), Style = new TextStyle { Alignment = TextAlignment.Center, Baseline = TextBaseline.Middle } })
            .AddAnnotation(new StepAnnotation { Center = Marked, Number = 1 })
            .AddAnnotation(new CalloutAnnotation { Bubble = new RectangleF(120, 60, 50, 30), Target = Marked })
            .AddAnnotation(new FreehandAnnotation { Points = [Marked - new Vector2(5, 0), Marked, Marked + new Vector2(5, 3)] })
            .AddAnnotation(new PolygonAnnotation { Points = [Marked - new Vector2(4, 4), Marked + new Vector2(4, -4), Marked + new Vector2(0, 5)] });
        var edited = document.AddOperation(edit, keepOverlaysAttached: true);
        using var output = edited.Pipeline.Execute(marker);
        var content = FindMarker(output);

        foreach (var annotation in edited.Annotations)
        {
            var at = annotation switch
            {
                CalloutAnnotation callout => callout.Target,
                FreehandAnnotation freehand => freehand.Points[1],
                _ => Centre(annotation.GetBounds()),
            };
            Assert.IsLessThan(2.5f, Vector2.Distance(content, at), $"{name}: the {annotation.Kind} is at {at}, the content at {content}");
        }
    }

    [TestMethod]
    public void AQuarterTurnTurnsAnUprightBoxIntoAnUprightBox()
    {
        var document = ImageDocument.Create(Source).AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(10, 20, 40, 10) });
        var turned = (RectangleAnnotation)document.AddOperation(OrientationOperation.Rotate90, keepOverlaysAttached: true).Annotations[0];
        Assert.AreEqual(0f, turned.Rotation);
        Assert.AreEqual(10f, turned.Rectangle.Width, 1e-3f);
        Assert.AreEqual(40f, turned.Rectangle.Height, 1e-3f);
    }

    [TestMethod]
    public void AMirrorMovesTextWithoutMirroringIt()
    {
        var document = ImageDocument.Create(Source).AddAnnotation(new TextAnnotation { Text = "Label", Position = new Vector2(20, 20), MeasuredSize = new SizeF(40, 12) });
        foreach (var flip in new[] { OrientationOperation.FlipHorizontal, OrientationOperation.FlipVertical })
        {
            var text = (TextAnnotation)document.AddOperation(flip, keepOverlaysAttached: true).Annotations[0];
            Assert.AreEqual(0f, text.Rotation, $"{flip}: the text stays upright and readable");
        }
    }

    [TestMethod]
    public void AStraightenReEditedMovesTheOverlaysOnlyByTheChange()
    {
        var document = ImageDocument.Create(Source)
            .AddOperation(new StraightenOperation(4), keepOverlaysAttached: true)
            .AddAnnotation(new ArrowAnnotation { Start = new Vector2(20, 20), End = new Vector2(70, 40) });
        // Dragging the slider replaces the step: the overlays follow from the old angle to the new one, not from zero.
        var redone = document.WithPipeline(document.Pipeline.Replace(0, new StraightenOperation(9)), keepOverlaysAttached: true);
        var direct = ImageDocument.Create(Source)
            .AddAnnotation(new ArrowAnnotation { Start = new Vector2(20, 20), End = new Vector2(70, 40) }.Transform(Inverse(document.Pipeline, Source)))
            .AddOperation(new StraightenOperation(9), keepOverlaysAttached: true);
        var a = (ArrowAnnotation)redone.Annotations[0];
        var b = (ArrowAnnotation)direct.Annotations[0];
        Assert.IsLessThan(0.01f, Vector2.Distance(a.End, b.End));
        Assert.IsLessThan(0.01f, Vector2.Distance(a.Start, b.Start));
    }

    private static Matrix3x2 Inverse(ImagePipeline pipeline, Size size)
    {
        Assert.IsTrue(pipeline.TryGetTransform(size, out var m));
        Assert.IsTrue(Matrix3x2.Invert(m, out var inverse));
        return inverse;
    }

    [TestMethod]
    public void PixelAdjustmentsDoNotMoveAnything()
    {
        var arrow = new ArrowAnnotation { Start = new Vector2(10, 10), End = new Vector2(50, 50) };
        var document = ImageDocument.Create(Source).AddAnnotation(arrow);
        var adjusted = document.AddOperation(new ExposureAdjustment(0.4f), keepOverlaysAttached: true);
        Assert.AreSame(arrow, adjusted.Annotations[0], "a step that moves nothing leaves the overlays as they are");
    }

    [TestMethod]
    public void WithoutAttachmentOrWithANonAffineStepOverlaysStayInOutputCoordinates()
    {
        var arrow = new ArrowAnnotation { Start = new Vector2(10, 10), End = new Vector2(50, 50) };
        var document = ImageDocument.Create(Source).AddAnnotation(arrow);
        Assert.AreSame(arrow, document.AddOperation(new CropOperation(20, 20, 100, 60)).Annotations[0], "AddOperation keeps its meaning");
        var perspective = new PerspectiveTransformOperation([new PointF(5, 2), new PointF(190, 10), new PointF(200, 95), new PointF(0, 100)]);
        Assert.AreSame(arrow, document.AddOperation(perspective, keepOverlaysAttached: true).Annotations[0], "a perspective warp has no affine map");
    }

    [TestMethod]
    public void LayersAndTheirMasksFollowTheContent()
    {
        using var logo = ImageBuffer.Create(10, 10, Rgba32.White);
        var document = ImageDocument.Create(Source)
            .AddLayer(new ImageLayer { Image = logo, Bounds = new RectangleF(50, 20, 20, 20) })
            .AddLayer(new PaintLayer([new PaintStroke([new(40, 30), Marked], 6, Rgba32.White)]))
            .AddLayer(new FillLayer(Rgba32.White) { Mask = new EllipseMask(new RectangleF(50, 20, 20, 20)) })
            .AddLayer(new FillLayer(Rgba32.White) { Mask = Mask.Ellipse() });
        var crop = new CropOperation(30, 10, 120, 80);
        var cropped = document.AddOperation(crop, keepOverlaysAttached: true);
        Assert.AreEqual(new RectangleF(20, 10, 20, 20), ((ImageLayer)cropped.Layers[0]).Bounds);
        Assert.AreEqual(Marked - new Vector2(30, 10), ((PaintLayer)cropped.Layers[1]).Strokes[0].Points[1]);
        Assert.AreEqual(new RectangleF(20, 10, 20, 20), ((EllipseMask)cropped.Layers[2].Mask!).Rectangle);
        Assert.AreSame(document.Layers[3].Mask, cropped.Layers[3].Mask, "a relative mask fits whatever image it is applied to");

        // A resize stretching the axes apart maps an upright layer's bounds exactly.
        var stretched = document.AddOperation(new ResizeOperation(400, 50, ResizeMode.Stretch), keepOverlaysAttached: true);
        Assert.AreEqual(new RectangleF(100, 10, 40, 10), ((ImageLayer)stretched.Layers[0]).Bounds);
    }

    [TestMethod]
    public void AMirroredImageLayerMirrorsItsPixelsToo()
    {
        // A layer half red, half blue: after a horizontal flip of the image, blue must be on the left.
        using var picture = ImageBuffer.Create(20, 10, new Rgba32(255, 0, 0));
        picture.Fill(new Rectangle(10, 0, 10, 10), new Rgba32(0, 0, 255));
        using var source = ImageBuffer.Create(Source.Width, Source.Height, Rgba32.Black);
        var document = ImageDocument.Create(Source).AddLayer(new ImageLayer { Image = picture, Bounds = new RectangleF(40, 40, 20, 10) });
        var flipped = document.AddOperation(OrientationOperation.FlipHorizontal, keepOverlaysAttached: true);
        using var rendered = new DocumentRenderer().Render(flipped, source);
        var layer = (ImageLayer)flipped.Layers[0];
        var left = rendered[(int)layer.Bounds.Left + 2, 45];
        var right = rendered[(int)layer.Bounds.Right - 3, 45];
        Assert.IsGreaterThan(200, (int)left.B, $"left {left}");
        Assert.IsGreaterThan(200, (int)right.R, $"right {right}");
    }

    [TestMethod]
    public void ARedactionStillCoversWhatItHidAfterATurn()
    {
        using var marker = Marker();
        var document = ImageDocument.Create(Source).AddAnnotation(new RedactionAnnotation { Rectangle = new RectangleF(Marked.X - 4, Marked.Y - 4, 8, 8), Color = new Rgba32(255, 0, 255) });
        foreach (var edit in new IImageOperation[] { OrientationOperation.Rotate90, new StraightenOperation(13), CanvasSizeOperation.Margins(30) })
        {
            var edited = document.AddOperation(edit, keepOverlaysAttached: true);
            using var plain = edited.Pipeline.Execute(marker);
            var at = FindMarker(plain);
            using var rendered = new DocumentRenderer().Render(edited, marker);
            var covered = rendered[(int)at.X, (int)at.Y];
            Assert.AreEqual(new Rgba32(255, 0, 255), covered, $"{edit}: the hidden pixel shows through");
        }
    }

    [TestMethod]
    public void APipelineReportsTheAffineMapOfItsSteps()
    {
        var pipeline = ImagePipeline.Create().Crop(10, 10, 100, 50).Brightness(0.2f).FlipHorizontal().Pad(5);
        Assert.IsTrue(pipeline.TryGetTransform(Source, out var m));
        // (10,10) is the crop's corner, lands at x = 100 after the flip, then 5 further for the margin.
        var p = Vector2.Transform(new Vector2(10, 10), m);
        Assert.AreEqual(105f, p.X, 1e-3f);
        Assert.AreEqual(5f, p.Y, 1e-3f);
        Assert.IsFalse(ImagePipeline.Create().AutoOrient().TryGetTransform(Source, out _), "an orientation from metadata is not known in advance");
        Assert.IsFalse(ImagePipeline.Create().SmartCrop(AspectRatio.Square).TryGetTransform(Source, out _), "a content-aware crop is not either");
    }

    private static Vector2 Centre(RectangleF r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);
}
