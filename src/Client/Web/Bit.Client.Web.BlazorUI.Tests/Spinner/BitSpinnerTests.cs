using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Spinner
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
            DataRow(Visual.Fluent, SpinnerLabelPosition.Top),
            DataRow(Visual.Cupertino, SpinnerLabelPosition.Top),
            DataRow(Visual.Material, SpinnerLabelPosition.Top),

            DataRow(Visual.Fluent, SpinnerLabelPosition.Right),
            DataRow(Visual.Cupertino, SpinnerLabelPosition.Right),
            DataRow(Visual.Material, SpinnerLabelPosition.Right),

            DataRow(Visual.Fluent, SpinnerLabelPosition.Left),
            DataRow(Visual.Cupertino, SpinnerLabelPosition.Left),
            DataRow(Visual.Material, SpinnerLabelPosition.Left),

            DataRow(Visual.Fluent, SpinnerLabelPosition.Bottom),
            DataRow(Visual.Cupertino, SpinnerLabelPosition.Bottom),
            DataRow(Visual.Material, SpinnerLabelPosition.Bottom)]
        public void BitSpinnerShouldRespectPosition(Visual visual, SpinnerLabelPosition position)
        {
            var component = RenderComponent<BitSpinnerTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LabelPosition, position);
            });

            var positionClass = position == SpinnerLabelPosition.Top ? "top" : position == SpinnerLabelPosition.Right ? "right" : position == SpinnerLabelPosition.Left ? "left" : "bottom";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            var bitSpinner = component.Find($".bit-spn-{visualClass}");

            Assert.IsTrue(bitSpinner.ClassList.Contains($"bit-spn-{positionClass}-{visualClass}"));
        }
    }
}
