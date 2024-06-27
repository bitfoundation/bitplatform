using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Progress.Spinner;

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
            BitSpinnerSize.XSmall => "bit-spn-xs",
            BitSpinnerSize.Small => "bit-spn-sm",
            BitSpinnerSize.Medium => "bit-spn-md",
            BitSpinnerSize.Large => "bit-spn-lg",
            _ => throw new NotSupportedException()
        };

        var bitSpinner = component.Find(".bit-spn");

        Assert.IsTrue(bitSpinner.ClassList.Contains(sizeClass));
    }

    [DataTestMethod,
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.End),
        DataRow(BitLabelPosition.Start)
    ]
    public void BitSpinnerShouldRespectPosition(BitLabelPosition position)
    {
        var component = RenderComponent<BitSpinner>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, position);
        });

        var positionClass = position switch
        {
            BitLabelPosition.Top => "bit-spn-top",
            BitLabelPosition.End => "bit-spn-end",
            BitLabelPosition.Start => "bit-spn-srt",
            BitLabelPosition.Bottom => "bit-spn-btm",
            _ => throw new NotSupportedException()
        };

        var bitSpinner = component.Find(".bit-spn");

        Assert.IsTrue(bitSpinner.ClassList.Contains(positionClass));
    }
}
