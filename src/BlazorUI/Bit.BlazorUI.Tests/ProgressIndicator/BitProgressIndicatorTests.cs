using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.ProgressIndicator
{
    [TestClass]
    public class BitProgressIndicatorTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(3),
            DataRow(12)
        ]
        public void BitProgressIndicatorBarHeightTest(int barHeight)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.BarHeight, barHeight);
            });

            var piWrapper = component.Find(".bit-pi-wrapper");
            var piWrapperStyle = piWrapper.GetAttribute("style");
            var expectedValue = $"height: {barHeight}px";
            Assert.IsTrue(piWrapperStyle.Contains(expectedValue));
        }

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

        [DataTestMethod,
            DataRow("Label"),
            DataRow(null),
        ]
        public void BitProgressIndicatorLabelTest(string label)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.Label, label);
            });

            var piBar = component.Find(".bit-pi-bar");
            if (label is not null)
            {
                var piLabel = component.Find(".bit-pi-lbl");
                Assert.AreEqual(label, piLabel.TextContent);
                Assert.IsNotNull(piBar.GetAttribute("aria-labelledby"));
            }
            else
            {
                Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pi-lbl"));
                Assert.IsNull(piBar.GetAttribute("aria-labelledby"));
            }
        }

        [DataTestMethod,
            DataRow("Description"),
            DataRow(null),
        ]
        public void BitProgressIndicatorDescriptionTest(string description)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.Description, description);
            });

            var piBar = component.Find(".bit-pi-bar");
            if (description is not null)
            {
                var piDescription = component.Find(".bit-pi-dsc");
                Assert.AreEqual(description, piDescription.TextContent);
                Assert.IsNotNull(piBar.GetAttribute("aria-describedby"));
            }
            else
            {
                Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pi-dsc"));
                Assert.IsNull(piBar.GetAttribute("aria-describedby"));
            }
        }

        [DataTestMethod,
            DataRow("Aria Value Text"),
            DataRow(null),
        ]
        public void BitProgressIndicatorAriaValueTextTest(string txt)
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.AriaValueText, txt);
            });

            var piBar = component.Find(".bit-pi-bar");
            if (txt is not null)
            {
                Assert.AreEqual(txt, piBar.GetAttribute("aria-valuetext"));
            }
            else
            {
                Assert.IsNull(piBar.GetAttribute("aria-valuetext"));
            }
        }

        [DataTestMethod]
        public void BitProgressIndicatorIsProgressHiddenTest()
        {
            var component = RenderComponent<BitProgressIndicatorTest>(parameters =>
            {
                parameters.Add(p => p.IsProgressHidden, true);
            });

            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-pi-wrapper"));
        }

        [DataTestMethod,
            DataRow("<h1>this is a custom label</h1>")
        ]
        public void BitProgressIndicatorLabelFragmentTest(string labelFragment)
        {
            var component = RenderComponent<BitProgressIndicator>(parameters =>
            {
                parameters.Add(p => p.LabelFragment, labelFragment);
            });

            var labelChildNodes = component.Find(".bit-pi-lbl").ChildNodes;
            labelChildNodes.MarkupMatches(labelFragment);
        }

        [DataTestMethod,
            DataRow("<h1>this is a custom description</h1>"),
        ]
        public void BitProgressIndicatorDescriptionFragmentTest(string descriptionFragment)
        {
            var component = RenderComponent<BitProgressIndicator>(parameters =>
            {
                parameters.Add(p => p.DescriptionFragment, descriptionFragment);
            });

            var descriptionChildNodes = component.Find(".bit-pi-dsc").ChildNodes;
            descriptionChildNodes.MarkupMatches(descriptionFragment);
        }
    }
}
