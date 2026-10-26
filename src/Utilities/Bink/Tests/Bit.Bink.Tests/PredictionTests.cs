using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Predicted pointer positions extend the live stroke and are never committed.</summary>
[TestClass]
public sealed class PredictionTests
{
    private static BinkInputSample Sample(BinkPointerPhase phase, float x, float y, double time, bool predicted = false) => new()
    {
        PointerId = 1,
        Kind = BinkPointerKind.Pen,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.6f,
        Timestamp = time,
        Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        IsPrimary = true,
        IsPredicted = predicted,
    };

    private static BinkRect LiveBounds(BinkDrawTool tool, TestToolContext context)
    {
        tool.DrawOverlay(context, new BinkCanvasRenderContext());
        return tool.LiveBounds;
    }

    [TestMethod]
    public void A_prediction_extends_the_live_stroke_but_is_never_committed()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool { SimplifyTolerance = 0 };
        context.Brush = new BinkPenBrush { Width = 4f, Smoothing = 0f };
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        for (int i = 1; i <= 10; i++) tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 10 + i * 5, 10, i * 8));
        float measuredRight = LiveBounds(tool, context).Right;

        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 70, 10, 88, predicted: true));
        float predictedRight = LiveBounds(tool, context).Right;
        Assert.IsGreaterThan(measuredRight + 5f, predictedRight, "The live stroke should reach the predicted position.");

        // The next measured sample replaces the prediction.
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 62, 10, 96));
        Assert.IsLessThan(predictedRight, LiveBounds(tool, context).Right);

        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 90, 10, 100, predicted: true));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 62, 10, 104));
        var stroke = (BinkStroke)context.Document.AllElements.Single();
        Assert.IsLessThanOrEqualTo(62.01f, stroke.Points.Bounds.Right, "No predicted point may reach the document.");
    }

    [TestMethod]
    public void A_runaway_prediction_is_held_to_a_short_reach()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        context.Brush = new BinkPenBrush { Width = 2f, Smoothing = 0f };
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 20, 10, 8));
        float before = LiveBounds(tool, context).Right;

        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 900, 10, 12, predicted: true));
        Assert.IsLessThan(before + 20f, LiveBounds(tool, context).Right);
    }

    [TestMethod]
    [DataRow(BinkEraserMode.Stroke)]
    [DataRow(BinkEraserMode.Segment)]
    public void An_eraser_never_erases_where_the_pointer_is_only_predicted_to_be(BinkEraserMode mode)
    {
        using var context = new TestToolContext();
        BinkStroke ahead = TestData.Stroke(new Vector2(100, 0), new Vector2(100, 50));
        context.Document.AddElement(ahead);
        var eraser = new BinkEraserTool { Mode = mode, Radius = 6f };
        eraser.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 25, 0));
        eraser.OnPointerMove(context, Sample(BinkPointerPhase.Move, 20, 25, 8));
        eraser.OnPointerMove(context, Sample(BinkPointerPhase.Move, 100, 25, 12, predicted: true));
        eraser.OnPointerUp(context, Sample(BinkPointerPhase.Up, 20, 25, 16));

        Assert.AreSame(ahead, context.Document.GetElement(ahead.Id));
    }

    [TestMethod]
    public void Predictions_can_be_turned_off_and_never_reach_other_tools()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool { UsePrediction = false };
        context.Brush = new BinkPenBrush { Width = 4f, Smoothing = 0f };
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 20, 10, 8));
        float before = LiveBounds(tool, context).Right;
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 40, 10, 12, predicted: true));
        Assert.AreEqual(before, LiveBounds(tool, context).Right, 0.001f);
    }
}
