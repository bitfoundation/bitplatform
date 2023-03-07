using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Spinner;

[TestClass]
public class BitSpinnerTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(BitSpinnerSize.Large),
        DataRow(BitSpinnerSize.Medium),
        DataRow(BitSpinnerSize.Small),
        DataRow(BitSpinnerSize.XSmall)
    ]
    public void BitSpinnerShouldRespectSize(BitSpinnerSize size)
    {
        var component = RenderComponent<BitSpinnerTest>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size == BitSpinnerSize.Large ? "large" : size == BitSpinnerSize.Medium ? "medium" : size == BitSpinnerSize.Small ? "small" : "x-small";

        var bitSpinner = component.Find(".bit-spn");

        Assert.IsTrue(bitSpinner.ClassList.Contains(sizeClass));
    }

    [DataTestMethod,
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Right),
        DataRow(BitLabelPosition.Left)
    ]
    public void BitSpinnerShouldRespectPosition(BitLabelPosition position)
    {
        var component = RenderComponent<BitSpinnerTest>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, position);
        });

        var positionClass = position == BitLabelPosition.Top ? "top" : position == BitLabelPosition.Right ? "right" : position == BitLabelPosition.Left ? "left" : "bottom";

        var bitSpinner = component.Find(".bit-spn");

        Assert.IsTrue(bitSpinner.ClassList.Contains(positionClass));
    }
}
