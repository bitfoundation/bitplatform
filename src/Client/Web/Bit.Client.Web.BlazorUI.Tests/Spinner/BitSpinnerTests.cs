using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Spinner
{
    [TestClass]
    public class BitSpinnerTests : BunitTestContext
    {
        [DataTestMethod, DataRow(SpinnerSize.Large, true)]
        public void BitSpinnerShouldRespectLargeSize(SpinnerSize size, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.Size, size));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-large-fluent"));
        }

        [DataTestMethod, DataRow(null, true), DataRow(SpinnerSize.Medium, true)]
        public void BitSpinnerShouldRespectMediumSize(SpinnerSize size, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.Size, size));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-medium-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerSize.Small, true)]
        public void BitSpinnerShouldRespectSmallSize(SpinnerSize size, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.Size, size));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-small-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerSize.XSmall, true)]
        public void BitSpinnerShouldRespectXSmallSize(SpinnerSize size, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.Size, size));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-xSmall-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerLabelPosition.Bottom, true)]
        public void BitSpinnerShouldRespectBottomPosition(SpinnerLabelPosition position, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.LabelPosition, position));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-bottom-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerLabelPosition.Top, true)]
        public void BitSpinnerShouldRespectTopPosition(SpinnerLabelPosition position, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.LabelPosition, position));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-top-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerLabelPosition.Right, true)]
        public void BitSpinnerShouldRespectRightPosition(SpinnerLabelPosition position, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.LabelPosition, position));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-right-fluent"));
        }

        [DataTestMethod, DataRow(SpinnerLabelPosition.Left, true)]
        public void BitSpinnerShouldRespectLeftPosition(SpinnerLabelPosition position, bool expectedResult)
        {
            var component = RenderComponent<BitSpinner>(parameters => parameters.Add(p => p.LabelPosition, position));

            var bitLabel = component.Find(".bit-spn-fluent");

            Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-spn-left-fluent"));
        }
    }
}
