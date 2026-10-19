using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Coordinate and viewport regressions: drawing, hit testing and zooming have to agree at every zoom level,
/// and a viewport built with an object initializer must not be able to divide by zero.
/// </summary>
[TestClass]
public class ViewportRegressionTests(TestContext output)
{
    [TestMethod]
    [DataRow(0.05f)]
    [DataRow(0.5f)]
    [DataRow(1f)]
    [DataRow(1.5f)]
    [DataRow(2f)]
    [DataRow(3.7f)]
    [DataRow(16f)]
    [DataRow(64f)]
    public void Drawing_at_any_zoom_lands_at_the_same_document_position(float zoom)
    {
        using var context = new TestToolContext();
        context.Viewport = BinkViewport.Identity.ZoomTo(zoom, new Vector2(400, 300)).Pan(new Vector2(37, -19));
        var tool = new BinkDrawTool();
        tool.Activate(context);

        Vector2 docFrom = new(120, 90), docTo = new(260, 210);
        Vector2 viewFrom = context.Viewport.ToView(docFrom), viewTo = context.Viewport.ToView(docTo);
        context.Line(tool, viewFrom, viewTo, 40);

        BinkStroke stroke = Assert.IsExactInstanceOfType<BinkStroke>(Assert.ContainsSingle(context.Document.AllElements));
        Vector2 first = stroke.Points.PositionAt(0);
        Vector2 last = stroke.Points.PositionAt(stroke.Points.Count - 1);
        output.WriteLine($"zoom {zoom}: first={first} last={last} n={stroke.Points.Count}");
        Assert.IsTrue(Vector2.Distance(first, docFrom) < 1.0f, $"start off by {Vector2.Distance(first, docFrom)} at zoom {zoom}");
        Assert.IsTrue(Vector2.Distance(last, docTo) < 1.0f, $"end off by {Vector2.Distance(last, docTo)} at zoom {zoom}");
    }

    [TestMethod]
    [DataRow(0.05f)]
    [DataRow(1f)]
    [DataRow(2.5f)]
    [DataRow(64f)]
    public void Hit_testing_finds_a_stroke_at_any_zoom(float zoom)
    {
        using var context = new TestToolContext();
        context.Document.AddElement(TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100), new BinkPenBrush { Width = 4 }));
        context.Viewport = BinkViewport.Identity.ZoomTo(zoom, new Vector2(400, 300));
        var select = new BinkSelectTool();
        select.Activate(context);
        Vector2 view = context.Viewport.ToView(new Vector2(200, 100));
        context.Gesture(select, BinkPointerKind.Mouse, (view.X, view.Y, 0.5f), (view.X, view.Y, 0.5f));
        Assert.AreEqual(1, context.Selection.Count);
    }

    [TestMethod]
    public void Zooming_keeps_the_anchor_point_fixed()
    {
        var vp = new BinkViewport(1f, new Vector2(10, -5));
        var anchor = new Vector2(321, 177);
        Vector2 before = vp.ToDocument(anchor);
        for (int i = 0; i < 40; i++)
        {
            vp = vp.ZoomBy(1.19f, anchor);
            Vector2 after = vp.ToDocument(anchor);
            Assert.IsTrue(Vector2.Distance(before, after) < 0.01f, $"drifted {Vector2.Distance(before, after)} at step {i} zoom {vp.Zoom}");
        }
    }

    [TestMethod]
    public void A_degenerate_viewport_cannot_be_built_with_an_object_initializer()
    {
        // A record struct's init accessors and its default value both bypass the constructor, so the guard has to
        // live on the property. A zoom of zero would turn every document coordinate into infinity.
        BinkViewport bad = BinkViewport.Identity with { Zoom = 0f };
        Vector2 p = bad.ToDocument(new Vector2(10, 10));
        output.WriteLine($"zoom=0 -> {p}");
        Assert.IsTrue(float.IsFinite(p.X) && float.IsFinite(p.Y), "zoom 0 produced non-finite document coordinates");

        Vector2 fromDefault = default(BinkViewport).ToDocument(new Vector2(10, 10));
        Assert.IsTrue(float.IsFinite(fromDefault.X) && float.IsFinite(fromDefault.Y), "default(Viewport) produced non-finite coordinates");
    }

    [TestMethod]
    public void Negative_zoom_does_not_mirror_the_document()
    {
        BinkViewport bad = BinkViewport.Identity with { Zoom = -2f };
        output.WriteLine($"negative zoom read back as: {bad.Zoom}");
        Assert.IsTrue(bad.Zoom > 0, "a negative zoom was accepted through a with-expression");
    }

    [TestMethod]
    public void A_non_finite_offset_is_refused()
    {
        BinkViewport bad = BinkViewport.Identity with { Offset = new Vector2(float.NaN, float.PositiveInfinity) };
        Assert.IsTrue(float.IsFinite(bad.Offset.X) && float.IsFinite(bad.Offset.Y));
    }

    [TestMethod]
    public void A_transform_cannot_push_an_element_beyond_the_coordinate_limit()
    {
        // Validating only the local box would let an imported document scale modest coordinates millions of units
        // away, which makes zoom-to-fit and export meaningless.
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        var scaled = (BinkStroke)stroke.TransformBy(Matrix3x2.CreateScale(1_000_000f));
        output.WriteLine($"local={scaled.LocalBounds} document={scaled.Bounds}");
        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(scaled));
    }
}
