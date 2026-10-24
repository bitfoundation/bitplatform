using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// The segment eraser cuts where the eraser's circle meets the ink, measured in document space, however long the
/// stroke's segments are and however the stroke has been stretched.
/// </summary>
[TestClass]
public class EraserGeometryTests(TestContext output)
{
    /// <summary>
    /// Long segments were split into at most 128 samples, so on a 4000-unit segment the samples were 31 units
    /// apart and an eraser reaching 12 units either side passed between them. Mouse drags simplify to exactly
    /// such segments.
    /// </summary>
    [TestMethod]
    [DataRow(1000f)]
    [DataRow(1015.6f)]
    [DataRow(3.5f)]
    [DataRow(3996.5f)]
    public void A_swipe_anywhere_along_a_long_segment_cuts_it(float x)
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(4000, 0)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();

        bool changed = BinkStrokeEditor.EraseSegments(stroke, [new Vector2(x, -30), new Vector2(x, 30)], 10f, fragments);

        output.WriteLine($"x={x}: changed={changed}, " + string.Join(" | ", fragments.Select(f => $"{f.Points.PositionAt(0).X:0.###}..{f.Points.PositionAt(f.Points.Count - 1).X:0.###}")));
        Assert.IsTrue(changed, $"a swipe at x={x} passed through the stroke");
        // The eraser reaches 10 units and the ink 2 more, so the centreline is cut 12 units either side, exactly.
        foreach (BinkStroke fragment in fragments)
        {
            float first = fragment.Points.PositionAt(0).X, last = fragment.Points.PositionAt(fragment.Points.Count - 1).X;
            Assert.IsTrue(last <= x - 12f + 0.01f || first >= x + 12f - 0.01f, $"a fragment {first}..{last} overlaps the eraser");
        }
        float kept = fragments.Sum(f => f.Points.Length);
        Assert.AreEqual(4000f - (MathF.Min(x + 12f, 4000f) - MathF.Max(x - 12f, 0f)), kept, 0.05f, "the cut is wider than the eraser");
    }

    [TestMethod]
    public void Sweeping_a_long_segment_at_every_position_always_cuts()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(4000, 0)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();
        for (float x = 900f; x < 1100f; x += 0.7f)
        {
            Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(x, -30), new Vector2(x, 30)], 10f, fragments), $"missed at x={x}");
            Assert.HasCount(2, fragments);
        }
    }

    [TestMethod]
    public void A_cut_adds_only_the_points_at_its_edges()
    {
        // The cut is computed, not sampled: the fragments hold the original points plus one at each new end.
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(4000, 0), new Vector2(4000, 4000)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();

        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(2000, -50), new Vector2(2000, 50)], 10f, fragments));

        Assert.HasCount(2, fragments);
        Assert.AreEqual(2, fragments[0].Points.Count);
        Assert.AreEqual(3, fragments[1].Points.Count);
        Assert.AreEqual(stroke.Id, fragments[0].Id);
        Assert.AreNotEqual(stroke.Id, fragments[1].Id);
    }

    [TestMethod]
    public void A_near_miss_leaves_the_stroke_alone()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(4000, 0)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();

        // 12.1 units away: the eraser's edge is a tenth of a unit short of the ink.
        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(500, 12.1f), new Vector2(3500, 12.1f)], 10f, fragments));
        Assert.IsEmpty(fragments);
    }

    [TestMethod]
    public void Erasing_over_a_whole_stroke_removes_it()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(40, 0), new Vector2(40, 30)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();

        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(-20, 0), new Vector2(60, 0), new Vector2(40, 50)], 15f, fragments));
        Assert.IsEmpty(fragments);
    }

    [TestMethod]
    public void A_dot_is_erased_whole_or_not_at_all()
    {
        var dot = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(50, 50)]), new BinkPenBrush { Width = 4f });
        var fragments = new List<BinkStroke>();

        Assert.IsFalse(BinkStrokeEditor.EraseSegments(dot, [new Vector2(50, 62.5f)], 10f, fragments));
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(dot, [new Vector2(50, 61.5f)], 10f, fragments));
        Assert.IsEmpty(fragments);
    }

    /// <summary>
    /// The eraser radius was divided by the square root of the transform's determinant, which treats a stretched
    /// stroke as if it were scaled evenly. A stroke stretched three times along its length keeps its thickness,
    /// so an eraser that clearly overlaps its ink was judged to miss it.
    /// </summary>
    [TestMethod]
    public void A_stroke_stretched_along_its_length_is_cut_where_the_eraser_meets_its_ink()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), new BinkPenBrush { Width = 4f })
        {
            Transform = Matrix3x2.CreateScale(3f, 1f),
        };
        var fragments = new List<BinkStroke>();

        // In the document the line runs from 0 to 300 and its ink is 2 units either side; the eraser at 8 units
        // above it reaches 2 units past the ink's edge.
        bool erased = BinkStrokeEditor.EraseSegments(stroke, [new Vector2(150, 8)], 10f, fragments);

        Assert.IsTrue(erased);
        Assert.HasCount(2, fragments);
        // The centreline is cut where it is 12 units from the eraser: 8.94 either side of x = 150 in the document.
        float halfChord = MathF.Sqrt(12f * 12f - 8f * 8f);
        Vector2 leftEnd = Vector2.Transform(fragments[0].Points.PositionAt(fragments[0].Points.Count - 1), stroke.Transform);
        Vector2 rightStart = Vector2.Transform(fragments[1].Points.PositionAt(0), stroke.Transform);
        Assert.AreEqual(150f - halfChord, leftEnd.X, 0.05f);
        Assert.AreEqual(150f + halfChord, rightStart.X, 0.05f);
        // The fragments keep the stroke's transform and stay in its local coordinates.
        Assert.AreEqual(stroke.Transform, fragments[1].Transform);
        Assert.AreEqual(100f, fragments[1].Points.PositionAt(fragments[1].Points.Count - 1).X, 1e-4f);
    }

    /// <summary>
    /// The other half of the same mistake: a stroke stretched three times across its length is three times as
    /// thick, but its ink is still only that thick, and an eraser that stops short of it erased it.
    /// </summary>
    [TestMethod]
    public void A_stroke_stretched_across_its_length_is_not_erased_by_an_eraser_short_of_its_ink()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(0, 100)]), new BinkPenBrush { Width = 2.4f })
        {
            Transform = Matrix3x2.CreateScale(3f, 1f),
        };
        var fragments = new List<BinkStroke>();

        // The ink reaches 1.2 * 3 = 3.6 units either side of the line; the eraser at x = 14 reaches only to x = 4.
        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(14, 50)], 10f, fragments));
        // At x = 13.5 it reaches 3.5, inside the ink.
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(13.5f, 50)], 10f, fragments));
    }

    [TestMethod]
    public void A_rotated_stretched_stroke_is_measured_in_document_space()
    {
        // Stretch along the line, then rotate it to vertical: in the document it is a vertical line 300 long whose
        // ink is still 2 units either side.
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), new BinkPenBrush { Width = 4f })
        {
            Transform = Matrix3x2.CreateScale(3f, 1f) * Matrix3x2.CreateRotation(MathF.PI / 2f),
        };
        var fragments = new List<BinkStroke>();

        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(12.2f, 150)], 10f, fragments));
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(11.8f, 150)], 10f, fragments));
        Assert.HasCount(2, fragments);
    }

    [TestMethod]
    public void An_evenly_scaled_stroke_scales_its_ink_with_it()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), new BinkPenBrush { Width = 4f })
        {
            Transform = Matrix3x2.CreateScale(2f),
        };
        var fragments = new List<BinkStroke>();

        // Doubled, the ink reaches 4 units from the line: an eraser of 10 meets it from up to 14 away.
        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(100, 14.2f)], 10f, fragments));
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(100, 13.8f)], 10f, fragments));
    }

    /// <summary>
    /// The stroke eraser only needs to know whether a swipe touches a stroke, and asks with Reaches rather than
    /// computing the cut and its fragments on every move. The two must agree exactly, on and off the edge of the
    /// reach, for dots, repeated points and stretched strokes alike.
    /// </summary>
    [TestMethod]
    public void Reaches_answers_exactly_what_EraseSegments_would_change()
    {
        var fragments = new List<BinkStroke>();
        var pen = new BinkPenBrush { Width = 4f };
        BinkStroke line = new(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(200, 0)]), pen);
        BinkStroke dot = new(BinkStrokePoints.FromPositions([new Vector2(50, 50)]), pen);
        BinkStroke repeated = new(BinkStrokePoints.FromPositions([new Vector2(50, 50), new Vector2(50, 50)]), pen);
        BinkStroke stretched = new(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), pen) { Transform = Matrix3x2.CreateScale(3f, 1f) * Matrix3x2.CreateRotation(MathF.PI / 2f) };
        BinkStroke flat = new(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), pen) { Transform = Matrix3x2.CreateScale(1f, 0f) };

        (BinkStroke Stroke, Vector2[] Path)[] cases =
        [
            (line, [new Vector2(100, -30), new Vector2(100, 30)]),
            (line, [new Vector2(100, 12.1f)]),
            (line, [new Vector2(100, 11.9f)]),
            (line, [new Vector2(-12.1f, 0)]),
            (line, [new Vector2(-11.9f, 0)]),
            (line, [new Vector2(0, 50), new Vector2(200, 50)]),
            (dot, [new Vector2(50, 62.5f)]),
            (dot, [new Vector2(50, 61.5f)]),
            (repeated, [new Vector2(50, 62.5f)]),
            (repeated, [new Vector2(50, 61.5f)]),
            (stretched, [new Vector2(-30, 150), new Vector2(30, 150)]),
            (stretched, [new Vector2(-12.2f, 150)]),
            (stretched, [new Vector2(-11.8f, 150)]),
            (flat, [new Vector2(50, 0)]),
            (line, []),
        ];
        foreach ((BinkStroke stroke, Vector2[] path) in cases)
        {
            bool erased = BinkStrokeEditor.EraseSegments(stroke, path, 10f, fragments);
            Assert.AreEqual(erased, BinkStrokeEditor.Reaches(stroke, path, 10f), $"{stroke.Points.Count} points, eraser at {string.Join(" ", path)}");
        }
    }
}
