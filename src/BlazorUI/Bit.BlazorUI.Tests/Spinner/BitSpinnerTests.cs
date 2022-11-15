using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Spinner;

[TestClass]
public class BitSpinnerTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, BitElementSize.Large),
        DataRow(Visual.Cupertino, BitElementSize.Large),
        DataRow(Visual.Material, BitElementSize.Large),

        DataRow(Visual.Fluent, BitElementSize.Medium),
        DataRow(Visual.Cupertino, BitElementSize.Medium),
        DataRow(Visual.Material, BitElementSize.Medium),

        DataRow(Visual.Fluent, BitElementSize.Small),
        DataRow(Visual.Cupertino, BitElementSize.Small),
        DataRow(Visual.Material, BitElementSize.Small),

        DataRow(Visual.Fluent, BitElementSize.XSmall),
        DataRow(Visual.Cupertino, BitElementSize.XSmall),
        DataRow(Visual.Material, BitElementSize.XSmall)]
    public void BitSpinnerShouldRespectSize(Visual visual, BitElementSize size)
    {
        var component = RenderComponent<BitSpinnerTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.Size, size);
        });

        var sizeClass = size == BitElementSize.Large ? "large" : size == BitElementSize.Medium ? "medium" : size == BitElementSize.Small ? "small" : "xSmall";
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
