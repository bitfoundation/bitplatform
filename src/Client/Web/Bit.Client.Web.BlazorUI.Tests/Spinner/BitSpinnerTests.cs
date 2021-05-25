using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Spinner
{
    [TestClass]
    public class BitSpinnerTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, SpinnerSize.Large),
            DataRow(Visual.Cupertino, SpinnerSize.Large),
            DataRow(Visual.Material, SpinnerSize.Large),

            DataRow(Visual.Fluent, SpinnerSize.Medium),
            DataRow(Visual.Cupertino, SpinnerSize.Medium),
            DataRow(Visual.Material, SpinnerSize.Medium),

            DataRow(Visual.Fluent, SpinnerSize.Small),
            DataRow(Visual.Cupertino, SpinnerSize.Small),
            DataRow(Visual.Material, SpinnerSize.Small),

            DataRow(Visual.Fluent, SpinnerSize.XSmall),
            DataRow(Visual.Cupertino, SpinnerSize.XSmall),
            DataRow(Visual.Material, SpinnerSize.XSmall)]
        public void BitSpinnerShouldRespectSize(Visual visual, SpinnerSize size)
        {
            var component = RenderComponent<BitSpinnerTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Size, size);
            });

            var sizeClass = size == SpinnerSize.Large ? "large" : size == SpinnerSize.Medium ? "medium" : size == SpinnerSize.Small ? "small" : "xSmall";
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
