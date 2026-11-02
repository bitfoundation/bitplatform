namespace Bit.Bdnd.Tests.Core;

/// <summary>The offset helpers a drag layer positions its preview with: kept to an axis, snapped to a grid, held in an area.</summary>
[TestClass]
public class BdndXYCoordTests
{
    private static readonly BdndXYCoord Origin = new(100, 50);

    [TestMethod]
    public void KeptToAnAxisTheOtherCoordinateIsTheOrigins()
    {
        var offset = new BdndXYCoord(160, 90);

        Assert.AreEqual(new BdndXYCoord(100, 90), offset.KeepTo(BdndAxis.Vertical, Origin));
        Assert.AreEqual(new BdndXYCoord(160, 50), offset.KeepTo(BdndAxis.Horizontal, Origin));
        Assert.AreEqual(offset, offset.KeepTo(BdndAxis.None, Origin));
    }

    [TestMethod]
    [DataRow(119, 64, 100, 50)] // under half a cell from the origin
    [DataRow(121, 76, 140, 80)] // past half a cell
    [DataRow(61, 21, 60, 20)] // before the origin
    public void SnappedToAGridItIsOnTheNearestCorner(double x, double y, double expectedX, double expectedY)
        => Assert.AreEqual(new BdndXYCoord(expectedX, expectedY), new BdndXYCoord(x, y).SnapTo(40, 30, Origin));

    [TestMethod]
    public void ACellSizeOfZeroLeavesThatCoordinate()
        => Assert.AreEqual(new BdndXYCoord(123, 60), new BdndXYCoord(123, 61).SnapTo(0, 20));

    [TestMethod]
    [DataRow(50, 10, 100, 40)]
    [DataRow(150, 70, 150, 70)]
    [DataRow(500, 900, 300, 200)]
    public void ClampedItStaysInTheArea(double x, double y, double expectedX, double expectedY)
        => Assert.AreEqual(new BdndXYCoord(expectedX, expectedY), new BdndXYCoord(x, y).Clamp(new(100, 40), new(300, 200)));

    [TestMethod]
    public void AnAreaSmallerThanThePreviewHoldsItAtItsStart()
        => Assert.AreEqual(new BdndXYCoord(100, 40), new BdndXYCoord(500, 900).Clamp(new(100, 40), new(80, 20)));

    [TestMethod]
    public void TheLayerStateKeepsTheSourceOffsetToAnAxis()
    {
        var monitor = new BdndDragDropManager().Monitor;
        var state = new BdndDragLayerState<string>
        {
            Monitor = monitor,
            InitialSourceClientOffset = Origin,
            SourceClientOffset = new BdndXYCoord(160, 90),
        };

        Assert.AreEqual(new BdndXYCoord(100, 90), state.SourceClientOffsetOn(BdndAxis.Vertical));
        Assert.IsNull(new BdndDragLayerState<string> { Monitor = monitor }.SourceClientOffsetOn(BdndAxis.Vertical));
    }
}
