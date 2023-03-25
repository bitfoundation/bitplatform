using System;
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
        var component = RenderComponent<BitSpinner>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size switch
        {
            BitSpinnerSize.Large => "large",
            BitSpinnerSize.Medium => "medium",
            BitSpinnerSize.Small => "small",
            BitSpinnerSize.XSmall => "x-small",
            _ => throw new NotSupportedException()
        };

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
        var component = RenderComponent<BitSpinner>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, position);
        });

        var positionClass = position switch
        {
            BitLabelPosition.Top => "top",
            BitLabelPosition.Right => "right",
            BitLabelPosition.Left => "left",
            BitLabelPosition.Bottom => "bottom",
            _ => throw new NotSupportedException()
        };

        var bitSpinner = component.Find(".bit-spn");

        Assert.IsTrue(bitSpinner.ClassList.Contains(positionClass));
    }
}
