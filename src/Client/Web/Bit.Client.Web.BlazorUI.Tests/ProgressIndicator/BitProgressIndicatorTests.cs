using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.ProgressIndicator
{
    [TestClass]
    public class BitProgressIndicatorTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(52),
            DataRow(43)]
        public void BitProgressIndicatorTrackerWidthShouldBeEqualPrecentCompleteValue(double precentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(
                parameters => parameters.Add(p => p.PercentComplete, precentComplete));

            var piTracker = component.Find(".bit-pi>div:nth-child(2)>div:nth-child(2)");
            var piTrackerWidth = piTracker.GetAttribute("style").Replace(" ", string.Empty);
            var expectedValue = $"width:{precentComplete}%";
            Assert.AreEqual(piTrackerWidth, expectedValue);
        }

        [DataTestMethod,
            DataRow(520),
            DataRow(430)]
        public void BitProgressIndicatorTrackerWidthCanNotBeBiggerThan100(double precentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(
                parameters => parameters.Add(p => p.PercentComplete, precentComplete));

            var piTracker = component.Find(".bit-pi>div:nth-child(2)>div:nth-child(2)");
            var piTrackerWidth = piTracker.GetAttribute("style").Replace(" ", string.Empty);
            var expectedValue = $"width:100%";
            Assert.AreEqual(piTrackerWidth, expectedValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),
            DataRow(Visual.Cupertino),
            DataRow(Visual.Material)]
        public void BitProgressIndicatorTrackerWidthCanNotBeLessThan0(Visual visual)
        {
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var component = RenderComponent<IndeterminateBitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
            });

            var pi = component.Find(".bit-pi");
            var hasIdeterminateClass = pi.ClassList.Contains($"bit-pi-indeterminate-{visualClass}");
            Assert.IsTrue(hasIdeterminateClass);
        }
    }
}
