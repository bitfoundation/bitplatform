using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>The history's memory budget.</summary>
[TestClass]
public sealed class HistoryBudgetTests
{
    private static BinkImageElement Photo(int kilobytes)
    {
        // A data URI of about the given size: what deleting an embedded photo leaves undo holding.
        return new BinkImageElement(BinkImageSource.FromBytes(new byte[kilobytes * 768], "image/png", 10, 10), new BinkRect(0, 0, 10, 10));
    }

    [TestMethod]
    public void The_estimate_grows_with_what_steps_hold_and_follows_the_stacks()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        Assert.AreEqual(0, history.EstimatedMemory);

        document.AddElement(Photo(100));
        long one = history.EstimatedMemory;
        Assert.IsGreaterThan(200_000L, one, "The photo's data counts.");

        document.AddElement(TestData.Stroke(Vector2.Zero, new Vector2(10, 10)));
        long two = history.EstimatedMemory;
        Assert.IsGreaterThan(one, two);
        Assert.IsLessThan(one + 10_000L, two, "A small stroke is small.");

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(two, history.EstimatedMemory, "Undoing moves a step to redo; it still holds the same.");
        document.AddElement(TestData.Stroke(Vector2.Zero, new Vector2(5, 5)));
        Assert.IsLessThan(two + 1_000L, history.EstimatedMemory, "A new change drops the redo steps and what they held.");

        history.Clear();
        Assert.AreEqual(0, history.EstimatedMemory);
    }

    [TestMethod]
    public void Past_the_budget_the_oldest_steps_go_but_never_the_newest()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document) { MemoryBudget = 700_000 };
        for (int i = 0; i < 5; i++) document.AddElement(Photo(100));

        Assert.AreEqual(3, history.UndoCount, "Three steps of about 200 KB fit in 700 KB.");
        Assert.IsLessThanOrEqualTo(700_000L, history.EstimatedMemory);

        history.MemoryBudget = 1;
        Assert.AreEqual(1, history.UndoCount, "The most recent step always stays.");
        Assert.IsTrue(history.Undo());
        Assert.AreEqual(4, document.ElementCount);
    }

    [TestMethod]
    public void A_drag_s_steps_share_the_stroke_s_points_and_count_them_once()
    {
        var document = new BinkDocument();
        var buffer = new BinkStrokePointBuffer(2000);
        for (int i = 0; i < 2000; i++) buffer.Add(new BinkPoint(i * 0.5f, MathF.Sin(i / 50f) * 40f, 0.5f, i));
        var stroke = new BinkStroke(buffer.ToStrokePoints(), new BinkPenBrush());
        document.AddElement(stroke);
        using var history = new BinkHistoryManager(document, limit: 1000) { MemoryBudget = long.MaxValue };
        long pointBytes = stroke.Points.Count * 28L;

        // Sixty frames of moving it: each step refers to two versions, and every version shares one set of points.
        for (int i = 1; i <= 60; i++) document.ReplaceElement(document.GetElement(stroke.Id) with { Transform = Matrix3x2.CreateTranslation(i, 0) });

        Assert.AreEqual(60, history.UndoCount);
        Assert.IsLessThan(pointBytes * 2, history.EstimatedMemory, "The points are counted once, not for every step.");
        Assert.IsGreaterThan(60 * 2 * 160L, history.EstimatedMemory, "Every version's own record still counts.");
    }

    [TestMethod]
    public void The_estimate_kept_under_a_budget_matches_the_one_worked_out_without()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document) { MemoryBudget = long.MaxValue };
        document.AddElement(Photo(50));
        BinkStroke stroke = TestData.Stroke(Vector2.Zero, new Vector2(300, 300));
        document.AddElement(stroke);
        document.ReplaceElement(stroke with { Transform = Matrix3x2.CreateTranslation(5, 5) });
        document.RemoveElement(stroke.Id);
        Assert.IsTrue(history.Undo());
        long kept = history.EstimatedMemory;

        history.MemoryBudget = 0;
        Assert.AreEqual(kept, history.EstimatedMemory, "Walked without a budget, the steps add up to the same.");

        // Dropping steps releases exactly what only they held.
        history.MemoryBudget = long.MaxValue;
        Assert.AreEqual(kept, history.EstimatedMemory);
        history.Limit = 1;
        long trimmed = history.EstimatedMemory;
        history.MemoryBudget = 0;
        Assert.AreEqual(trimmed, history.EstimatedMemory);
    }

    [TestMethod]
    public void The_canvas_passes_its_budget_on()
    {
        using var context = new Bunit.BunitContext();
        context.JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Bunit.IRenderedComponent<BinkCanvas> canvas = context.Render<BinkCanvas>(p => p.Add(c => c.HistoryMemoryBudget, 12345L));
        Assert.AreEqual(12345L, canvas.Instance.History.MemoryBudget);
    }
}
