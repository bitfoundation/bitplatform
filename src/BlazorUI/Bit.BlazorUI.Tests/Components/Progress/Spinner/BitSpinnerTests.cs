using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Progress.Spinner;

[TestClass]
public class BitSpinnerTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(BitSize.Large),
        DataRow(BitSize.Medium),
        DataRow(BitSize.Small),
    ]
    public void BitSpinnerShouldRespectSize(BitSize size)
    {
        var component = RenderComponent<BitSpinner>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size switch
        {
            BitSize.Small => "bit-spn-sm",
            BitSize.Medium => "bit-spn-md",
            BitSize.Large => "bit-spn-lg",
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
