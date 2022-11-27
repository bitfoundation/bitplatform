using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Spinner;

[TestClass]
public class BitSpinnerTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, BitSpinnerSize.Large),
        DataRow(Visual.Cupertino, BitSpinnerSize.Large),
        DataRow(Visual.Material, BitSpinnerSize.Large),

        DataRow(Visual.Fluent, BitSpinnerSize.Medium),
        DataRow(Visual.Cupertino, BitSpinnerSize.Medium),
        DataRow(Visual.Material, BitSpinnerSize.Medium),

        DataRow(Visual.Fluent, BitSpinnerSize.Small),
        DataRow(Visual.Cupertino, BitSpinnerSize.Small),
        DataRow(Visual.Material, BitSpinnerSize.Small),

        DataRow(Visual.Fluent, BitSpinnerSize.XSmall),
        DataRow(Visual.Cupertino, BitSpinnerSize.XSmall),
        DataRow(Visual.Material, BitSpinnerSize.XSmall)]
    public void BitSpinnerShouldRespectSize(Visual visual, BitSpinnerSize size)
    {
        var component = RenderComponent<BitSpinnerTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size == BitSpinnerSize.Large ? "large" : size == BitSpinnerSize.Medium ? "medium" : size == BitSpinnerSize.Small ? "small" : "xSmall";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        var bitSpinner = component.Find($".bit-spn-{visualClass}");

        Assert.IsTrue(bitSpinner.ClassList.Contains($"bit-spn-{sizeClass}-{visualClass}"));
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, BitLabelPosition.Top),
        DataRow(Visual.Cupertino, BitLabelPosition.Top),
        DataRow(Visual.Material, BitLabelPosition.Top),

        DataRow(Visual.Fluent, BitLabelPosition.Right),
        DataRow(Visual.Cupertino, BitLabelPosition.Right),
        DataRow(Visual.Material, BitLabelPosition.Right),

        DataRow(Visual.Fluent, BitLabelPosition.Left),
        DataRow(Visual.Cupertino, BitLabelPosition.Left),
        DataRow(Visual.Material, BitLabelPosition.Left),

        DataRow(Visual.Fluent, BitLabelPosition.Bottom),
        DataRow(Visual.Cupertino, BitLabelPosition.Bottom),
        DataRow(Visual.Material, BitLabelPosition.Bottom)]
    public void BitSpinnerShouldRespectPosition(Visual visual, BitLabelPosition position)
    {
        var component = RenderComponent<BitSpinnerTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.LabelPosition, position);
        });

        var positionClass = position == BitLabelPosition.Top ? "top" : position == BitLabelPosition.Right ? "right" : position == BitLabelPosition.Left ? "left" : "bottom";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        var bitSpinner = component.Find($".bit-spn-{visualClass}");

        Assert.IsTrue(bitSpinner.ClassList.Contains($"bit-spn-{positionClass}-{visualClass}"));
    }
}
