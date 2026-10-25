using System.Numerics;
using Bunit;

namespace Bit.Bink.Tests;

/// <summary>Aligning, distributing and flipping elements, alone and in groups.</summary>
[TestClass]
public sealed class LayoutCommandTests
{
    private static BinkRectangleShape Box(float x, float y, float w, float h) => new(new BinkRect(x, y, w, h)) { Style = new BinkShapeStyle { StrokeWidth = 0f, FillColor = BinkColor.Black } };

    private static (BinkDocument Document, List<Guid> Ids) With(params BinkElement[] elements)
    {
        var document = new BinkDocument();
        foreach (BinkElement e in elements) document.AddElement(e);
        return (document, elements.Select(e => e.Id).ToList());
    }

    private static BinkRect Bounds(BinkDocument document, Guid id) => document.GetElement(id).Bounds;

    [TestMethod]
    public void Aligning_lines_up_edges_and_centres_as_one_undo_step()
    {
        (BinkDocument document, List<Guid> ids) = With(Box(10, 0, 20, 20), Box(50, 40, 40, 20), Box(120, 90, 10, 20));
        using var history = new BinkHistoryManager(document);
        float lowest = ids.Max(id => Bounds(document, id).Bottom);

        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.Left));
        foreach (Guid id in ids) Assert.AreEqual(Bounds(document, ids[0]).Left, Bounds(document, id).Left, 0.01f);
        Assert.AreEqual(1, history.UndoCount);

        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.Bottom));
        foreach (Guid id in ids) Assert.AreEqual(lowest, Bounds(document, id).Bottom, 0.01f, "The lowest bottom edge is the line.");

        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.HorizontalCenter));
        float centre = Bounds(document, ids[1]).Center.X;
        foreach (Guid id in ids) Assert.AreEqual(centre, Bounds(document, id).Center.X, 0.01f);

        Assert.IsFalse(document.AlignElements(ids, BinkAlignment.HorizontalCenter), "Already aligned: nothing to record.");
        Assert.AreEqual(3, history.UndoCount);
    }

    [TestMethod]
    public void A_single_element_aligns_only_to_a_reference()
    {
        (BinkDocument document, List<Guid> ids) = With(Box(10, 10, 20, 20));
        Assert.IsFalse(document.AlignElements(ids, BinkAlignment.HorizontalCenter));

        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.HorizontalCenter, document.Settings.PageRect));
        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.VerticalCenter, document.Settings.PageRect));
        Assert.AreEqual(document.Settings.PageRect.Center, Bounds(document, ids[0]).Center);
    }

    [TestMethod]
    public void A_group_aligns_as_a_whole()
    {
        (BinkDocument document, List<Guid> ids) = With(Box(10, 0, 20, 20), Box(60, 30, 20, 20), Box(200, 100, 20, 20));
        document.GroupElements([ids[0], ids[1]]);
        float apart = Bounds(document, ids[1]).Left - Bounds(document, ids[0]).Left;

        Assert.IsTrue(document.AlignElements(ids, BinkAlignment.Right));

        Assert.AreEqual(apart, Bounds(document, ids[1]).Left - Bounds(document, ids[0]).Left, 0.01f, "The group keeps its shape.");
        Assert.AreEqual(Bounds(document, ids[2]).Right, Bounds(document, ids[1]).Right, 0.01f);
    }

    [TestMethod]
    public void Distributing_leaves_equal_gaps_and_keeps_the_outermost_in_place()
    {
        (BinkDocument document, List<Guid> ids) = With(Box(0, 0, 20, 20), Box(30, 50, 40, 20), Box(150, 10, 10, 20), Box(200, 0, 20, 20));
        float first = Bounds(document, ids[0]).Left;
        float last = Bounds(document, ids[3]).Right;
        float top = Bounds(document, ids[1]).Top;

        Assert.IsTrue(document.DistributeElements(ids, BinkDistribution.Horizontal));

        BinkRect[] b = ids.Select(id => Bounds(document, id)).OrderBy(r => r.Left).ToArray();
        Assert.AreEqual(first, b[0].Left, 0.01f);
        Assert.AreEqual(last, b[^1].Right, 0.01f);
        float gap = b[1].Left - b[0].Right;
        Assert.AreEqual(gap, b[2].Left - b[1].Right, 0.01f);
        Assert.AreEqual(gap, b[3].Left - b[2].Right, 0.01f);
        Assert.AreEqual(top, b[1].Top, 0.01f, "Distributing horizontally leaves vertical positions alone.");
    }

    [TestMethod]
    public void Distributing_needs_three_separate_things()
    {
        (BinkDocument document, List<Guid> ids) = With(Box(0, 0, 20, 20), Box(30, 0, 20, 20), Box(100, 0, 20, 20));
        document.GroupElements([ids[0], ids[1]]);
        Assert.IsFalse(document.DistributeElements(ids, BinkDistribution.Horizontal), "A group and an element are two things.");
    }

    [TestMethod]
    public void Flipping_mirrors_about_the_selection_and_twice_restores_it()
    {
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(40, 10));
        BinkRectangleShape box = Box(100, 0, 20, 40);
        (BinkDocument document, List<Guid> ids) = With(stroke, box);
        using var history = new BinkHistoryManager(document);
        BinkRect strokeBefore = Bounds(document, stroke.Id);
        BinkRect all = strokeBefore.Union(Bounds(document, box.Id));

        Assert.IsTrue(document.FlipElements(ids, BinkFlipDirection.Horizontal));

        Assert.AreEqual(all.Right - strokeBefore.Right + all.Left, Bounds(document, stroke.Id).Left, 0.05f, "The stroke moved to the other side.");
        Assert.IsLessThan(0f, document.GetElement(stroke.Id).Transform.GetDeterminant(), "The ink is mirrored, not rewritten.");
        Assert.AreSame(stroke.Points, ((BinkStroke)document.GetElement(stroke.Id)).Points);
        Assert.AreEqual(1, history.UndoCount);

        Assert.IsTrue(document.FlipElements(ids, BinkFlipDirection.Horizontal));
        Assert.IsTrue(document.GetElement(stroke.Id).Transform.IsIdentity, "Flipping twice is the identity.");
        Assert.AreEqual(strokeBefore, Bounds(document, stroke.Id));
    }

    [TestMethod]
    public void Flipping_moves_text_without_reversing_it_and_swaps_a_callout_s_sides()
    {
        var text = new BinkTextElement(new Vector2(0, 0), "Readable") { Size = new Vector2(80, 20) };
        var callout = new BinkCalloutElement(new BinkRect(200, 0, 100, 40), new Vector2(150, 20), "Note");
        (BinkDocument document, List<Guid> ids) = With(text, callout);

        Assert.IsTrue(document.FlipElements(ids, BinkFlipDirection.Horizontal));

        BinkElement flippedText = document.GetElement(text.Id);
        Assert.IsGreaterThan(0f, flippedText.Transform.GetDeterminant(), "Text is not mirrored.");
        Assert.IsGreaterThan(150f, flippedText.Bounds.Left, "Text moved to the other side.");
        var flippedCallout = (BinkCalloutElement)document.GetElement(callout.Id);
        Assert.IsGreaterThan(flippedCallout.Box.Right, flippedCallout.Anchor.X, "The anchor is now on the right of the box.");
        Assert.AreEqual(100f, flippedCallout.Box.Width, 0.01f);
        Assert.IsTrue(flippedCallout.Transform.IsIdentity);
    }

    [TestMethod]
    public void Flipping_what_cannot_change_is_not_an_undo_step()
    {
        // Text only ever moves, and on its own it is already where flipping would put it; a rotated callout likewise.
        var text = new BinkTextElement(new Vector2(0, 0), "Readable") { Size = new Vector2(80, 20) };
        var callout = new BinkCalloutElement(new BinkRect(200, 0, 100, 40), new Vector2(150, 20), "Note") { Transform = Matrix3x2.CreateRotation(0.3f, new Vector2(250, 20)) };
        (BinkDocument document, _) = With(text, callout);
        using var history = new BinkHistoryManager(document);
        long revision = document.Revision;

        Assert.IsFalse(document.FlipElements([text.Id], BinkFlipDirection.Horizontal));
        Assert.IsFalse(document.FlipElements([callout.Id], BinkFlipDirection.Vertical));

        Assert.AreEqual(revision, document.Revision);
        Assert.AreEqual(0, history.UndoCount);
    }
}

/// <summary>The layout commands on the canvas and the select tool's flip keys.</summary>
[TestClass]
public sealed class BinkCanvasLayoutTests : BunitContext
{
    public BinkCanvasLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void The_canvas_aligns_distributes_and_flips_its_selection()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkSelectTool()));
        var shapes = new[] { new BinkRectangleShape(new BinkRect(0, 0, 20, 20)), new BinkRectangleShape(new BinkRect(50, 30, 20, 20)), new BinkRectangleShape(new BinkRect(200, 60, 20, 20)) };
        canvas.InvokeAsync(() =>
        {
            foreach (BinkRectangleShape s in shapes) canvas.Instance.CurrentDocument.AddElement(s);
            canvas.Instance.Selection.Set(shapes.Select(s => s.Id));
        });
        BinkDocument document = canvas.Instance.CurrentDocument;

        bool aligned = false, distributed = false, flipped = false;
        canvas.InvokeAsync(() =>
        {
            aligned = canvas.Instance.AlignSelection(BinkAlignment.Top);
            distributed = canvas.Instance.DistributeSelection(BinkDistribution.Horizontal);
            flipped = canvas.Instance.FlipSelection(BinkFlipDirection.Vertical);
        });
        Assert.IsTrue(aligned && distributed && flipped);
        Assert.AreEqual(6, canvas.Instance.History.UndoCount, "Three additions, then one step per command.");
        Assert.AreEqual(3, canvas.Instance.Selection.Count, "The selection survives the commands.");
    }

    [TestMethod]
    public void Shift_H_flips_the_selection_through_the_select_tool()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkSelectTool()));
        BinkStroke stroke = TestData.Stroke(new Vector2(10, 10), new Vector2(60, 20));
        canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(stroke);
            canvas.Instance.Selection.Set(stroke.Id);
        });

        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnKey("H", false, true, false));

        Assert.IsLessThan(0f, canvas.Instance.CurrentDocument.GetElement(stroke.Id).Transform.GetDeterminant());
        Assert.AreEqual("Flip horizontally", canvas.Instance.History.UndoLabel);
    }
}
