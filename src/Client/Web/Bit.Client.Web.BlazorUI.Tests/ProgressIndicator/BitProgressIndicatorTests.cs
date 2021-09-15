using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.ProgressIndicator
{
    [TestClass]
    public class BitProgressIndicatorTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(52),
            DataRow(43)
        ]
        public void BitProgressIndicatorBarWidthShouldBeEqualPrecentCompleteValue(double percentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.PercentComplete, percentComplete);
            });

            var piBar = component.Find(".bit-pi-bar");
            var piBarStyle = piBar.GetAttribute("style");
            var expectedValue = $"width: {percentComplete}%";
            Assert.IsTrue(piBarStyle.Contains(expectedValue));
        }

        [DataTestMethod,
            DataRow(520),
            DataRow(430)
        ]
        public void BitProgressIndicatorBarWidthCanNotBeBiggerThan100(double precentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.PercentComplete, precentComplete);
            });

            var piBar = component.Find(".bit-pi-bar");
            var piBarStyle = piBar.GetAttribute("style");
            var expectedValue = $"width: 100%";
            Assert.IsTrue(piBarStyle.Contains(expectedValue));
        }

        [DataTestMethod,
            DataRow(-5),
            DataRow(-265)
        ]
        public void BitProgressIndicatorBarWidthCanNotBeSmallerThan0(double percentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.PercentComplete, percentComplete);
            });

            var piBar = component.Find(".bit-pi-bar");
            var piBarStyle = piBar.GetAttribute("style");
            var expectedValue = $"width: 0%";
            Assert.IsTrue(piBarStyle.Contains(expectedValue));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),
            DataRow(Visual.Cupertino),
            DataRow(Visual.Material)
        ]
        public void BitProgressIndicatorMustRespectVisual(Visual visual)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var pi = component.Find(".bit-pi");
            Assert.IsTrue(pi.ClassList.Contains($"bit-pi-{visualClass}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, 32.0),
            DataRow(Visual.Fluent, null),

            DataRow(Visual.Cupertino, 32.0),
            DataRow(Visual.Cupertino, null),

            DataRow(Visual.Material, 32.0),
            DataRow(Visual.Material, null)
        ]
        public void BitProgressIndicatorIndeterminateClassTest(Visual visual, double? percentComplete)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.PercentComplete, percentComplete);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var pi = component.Find(".bit-pi");
            var hasIndeterminateClass = pi.ClassList.Contains($"bit-pi-indeterminate-{visualClass}");
            Assert.AreEqual(percentComplete is null, hasIndeterminateClass);
        }
    }
}
