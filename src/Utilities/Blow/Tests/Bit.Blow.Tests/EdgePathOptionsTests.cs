namespace Bit.Blow.Tests;

[TestClass]
public class EdgePathOptionsTests
{
    [TestMethod]
    public void SimpleBezier_KeepsControlPointsOnTheHandleAxis()
    {
        // Left/Right handles: control points share the endpoint's Y, so the curve stays flat.
        var result = BlowEdgePath.SimpleBezier(0, 0, BlowPosition.Right, 200, 0, BlowPosition.Left);

        Assert.AreEqual("M0,0 C100,0 100,0 200,0", result.Path);
        Assert.AreEqual(100, result.LabelX, 1e-6);
        Assert.AreEqual(0, result.LabelY, 1e-6);
    }

    [TestMethod]
    public void SimpleBezier_DiffersFromTheStandardBezierWhenHandlesFaceAway()
    {
        var simple = BlowEdgePath.Compute(BlowEdgeType.SimpleBezier, 0, 0, BlowPosition.Right, -200, 0, BlowPosition.Left);
        var standard = BlowEdgePath.Compute(BlowEdgeType.Bezier, 0, 0, BlowPosition.Right, -200, 0, BlowPosition.Left);

        Assert.AreNotEqual(simple.Path, standard.Path);
    }

    [TestMethod]
    public void BorderRadiusOfZeroRemovesTheCornerArcs()
    {
        var rounded = BlowEdgePath.Compute(BlowEdgeType.SmoothStep, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top,
            new BlowEdgePathOptions { BorderRadius = 12 });
        var sharp = BlowEdgePath.Compute(BlowEdgeType.SmoothStep, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top,
            new BlowEdgePathOptions { BorderRadius = 0 });

        Assert.Contains("Q", rounded.Path);
        Assert.DoesNotContain("Q", sharp.Path);
    }

    [TestMethod]
    public void StepPositionSlidesTheBendTowardTheSourceOrTarget()
    {
        var nearSource = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 200, 200, BlowPosition.Top,
            new BlowEdgePathOptions { StepPosition = 0.1 });
        var nearTarget = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 200, 200, BlowPosition.Top,
            new BlowEdgePathOptions { StepPosition = 0.9 });
        var middle = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 200, 200, BlowPosition.Top);

        Assert.AreNotEqual(nearSource.Path, nearTarget.Path);
        Assert.AreNotEqual(nearSource.Path, middle.Path);
    }

    [TestMethod]
    public void StepPositionIsClampedToTheZeroToOneRange()
    {
        var beyondEnd = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 200, 200, BlowPosition.Top,
            new BlowEdgePathOptions { StepPosition = 5 });
        var atEnd = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 200, 200, BlowPosition.Top,
            new BlowEdgePathOptions { StepPosition = 1 });

        Assert.AreEqual(atEnd.Path, beyondEnd.Path);
    }

    [TestMethod]
    public void OffsetChangesHowFarTheEdgeLeavesTheHandle()
    {
        var tight = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top,
            new BlowEdgePathOptions { Offset = 2 });
        var loose = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top,
            new BlowEdgePathOptions { Offset = 60 });

        Assert.AreNotEqual(tight.Path, loose.Path);
    }

    [TestMethod]
    public void NullOptionsFallBackToTheAlgorithmDefaults()
    {
        var explicitDefaults = BlowEdgePath.Compute(BlowEdgeType.SmoothStep, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top,
            new BlowEdgePathOptions { BorderRadius = 5, Offset = 20, StepPosition = 0.5 });
        var implicitDefaults = BlowEdgePath.Compute(BlowEdgeType.SmoothStep, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top);

        Assert.AreEqual(explicitDefaults.Path, implicitDefaults.Path);
    }

    [TestMethod]
    public void CurvatureOptionIsForwardedToTheBezier()
    {
        var viaOptions = BlowEdgePath.Compute(BlowEdgeType.Bezier, 0, 0, BlowPosition.Right, -100, 0, BlowPosition.Left,
            new BlowEdgePathOptions { Curvature = 0.8 });
        var direct = BlowEdgePath.Bezier(0, 0, BlowPosition.Right, -100, 0, BlowPosition.Left, curvature: 0.8);

        Assert.AreEqual(direct.Path, viaOptions.Path);
    }
}
