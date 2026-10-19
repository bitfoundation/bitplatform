using System.Globalization;
namespace Bit.Blow.Tests;

[TestClass]
public class EdgePathTests
{
    [TestMethod]
    public void Straight_DrawsALineAndCentersTheLabel()
    {
        var result = BlowEdgePath.Straight(0, 0, 100, 50);

        Assert.AreEqual("M 0,0 L 100,50", result.Path);
        Assert.AreEqual(50, result.LabelX);
        Assert.AreEqual(25, result.LabelY);
    }

    [TestMethod]
    public void Bezier_PlacesTheLabelOnTheCurveMidpoint()
    {
        // Source at the bottom, target at the top, directly below: the curve is symmetric so
        // the t=0.5 point sits exactly halfway between the endpoints.
        var result = BlowEdgePath.Bezier(0, 0, BlowPosition.Bottom, 0, 100, BlowPosition.Top);

        Assert.AreEqual(0, result.LabelX, 1e-6);
        Assert.AreEqual(50, result.LabelY, 1e-6);
        Assert.StartsWith("M0,0 C", result.Path);
    }

    [TestMethod]
    public void Bezier_CurvatureChangesTheControlPoints()
    {
        var gentle = BlowEdgePath.Bezier(0, 0, BlowPosition.Right, -100, 0, BlowPosition.Left, curvature: 0.1);
        var strong = BlowEdgePath.Bezier(0, 0, BlowPosition.Right, -100, 0, BlowPosition.Left, curvature: 0.9);

        Assert.AreNotEqual(gentle.Path, strong.Path);
    }

    [TestMethod]
    public void Step_ProducesSharpCornersAndSmoothStep_ProducesArcs()
    {
        var step = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top);
        var smooth = BlowEdgePath.Compute(BlowEdgeType.SmoothStep, 0, 0, BlowPosition.Bottom, 100, 100, BlowPosition.Top);

        Assert.DoesNotContain("Q", step.Path);
        Assert.Contains("Q", smooth.Path);
    }

    [TestMethod]
    [DataRow(BlowEdgeType.Bezier)]
    [DataRow(BlowEdgeType.Straight)]
    [DataRow(BlowEdgeType.Step)]
    [DataRow(BlowEdgeType.SmoothStep)]
    public void AllTypes_ProduceAMoveToCommandAndNoNaN(BlowEdgeType type)
    {
        var result = BlowEdgePath.Compute(type, 12.5, -7.25, BlowPosition.Right, 300.75, 180.5, BlowPosition.Left);

        Assert.StartsWith("M", result.Path);
        Assert.DoesNotContain("NaN", result.Path);
        Assert.IsFalse(double.IsNaN(result.LabelX));
        Assert.IsFalse(double.IsNaN(result.LabelY));
    }

    [TestMethod]
    [DataRow(BlowEdgeType.Bezier)]
    [DataRow(BlowEdgeType.Straight)]
    [DataRow(BlowEdgeType.SmoothStep)]
    public void Paths_UseInvariantNumberFormattingUnderACommaDecimalCulture(BlowEdgeType type)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // de-DE writes 1,5 instead of 1.5 - an SVG path formatted that way is corrupt.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var result = BlowEdgePath.Compute(type, 1.5, 2.5, BlowPosition.Bottom, 10.25, 20.75, BlowPosition.Top);

            Assert.Contains(".", result.Path);
            Assert.DoesNotContain("1,5 ", result.Path);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void SmoothStep_IsSymmetricWhenEndpointsAreSwapped()
    {
        var forward = BlowEdgePath.SmoothStep(0, 0, BlowPosition.Bottom, 100, 200, BlowPosition.Top);
        var backward = BlowEdgePath.SmoothStep(100, 200, BlowPosition.Top, 0, 0, BlowPosition.Bottom);

        Assert.AreEqual(forward.LabelX, backward.LabelX, 1e-6);
        Assert.AreEqual(forward.LabelY, backward.LabelY, 1e-6);
    }

    [TestMethod]
    public void SmoothStep_HandlesCoincidentEndpointsWithoutNaN()
    {
        var result = BlowEdgePath.SmoothStep(50, 50, BlowPosition.Bottom, 50, 50, BlowPosition.Top);

        Assert.DoesNotContain("NaN", result.Path);
    }
}
