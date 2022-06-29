using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Spinner
{
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
            DataRow(Visual.Fluent, BitSpinnerLabelPosition.Top),
            DataRow(Visual.Cupertino, BitSpinnerLabelPosition.Top),
            DataRow(Visual.Material, BitSpinnerLabelPosition.Top),

            DataRow(Visual.Fluent, BitSpinnerLabelPosition.Right),
            DataRow(Visual.Cupertino, BitSpinnerLabelPosition.Right),
            DataRow(Visual.Material, BitSpinnerLabelPosition.Right),

            DataRow(Visual.Fluent, BitSpinnerLabelPosition.Left),
            DataRow(Visual.Cupertino, BitSpinnerLabelPosition.Left),
            DataRow(Visual.Material, BitSpinnerLabelPosition.Left),

            DataRow(Visual.Fluent, BitSpinnerLabelPosition.Bottom),
            DataRow(Visual.Cupertino, BitSpinnerLabelPosition.Bottom),
            DataRow(Visual.Material, BitSpinnerLabelPosition.Bottom)]
        public void BitSpinnerShouldRespectPosition(Visual visual, BitSpinnerLabelPosition position)
        {
            var component = RenderComponent<BitSpinnerTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LabelPosition, position);
            });

            var positionClass = position == BitSpinnerLabelPosition.Top ? "top" : position == BitSpinnerLabelPosition.Right ? "right" : position == BitSpinnerLabelPosition.Left ? "left" : "bottom";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            var bitSpinner = component.Find($".bit-spn-{visualClass}");

            Assert.IsTrue(bitSpinner.ClassList.Contains($"bit-spn-{positionClass}-{visualClass}"));
        }
    }
}
