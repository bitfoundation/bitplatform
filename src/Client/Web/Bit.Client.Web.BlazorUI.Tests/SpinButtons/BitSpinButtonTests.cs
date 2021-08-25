using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.SpinButtons
{
    [TestClass]
    public class BitSpinButtonTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(null),
            DataRow("Button Label")
        ]
        public void SpinButtonShoudHaveCorrectLabel(string label)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Label, label));

            if (label is null)
            {
                Assert.ThrowsException<ElementNotFoundException>(() => component.Find("label"));
            }
            else
            {
                var labelTag = component.Find("label");
                Assert.AreEqual(label, labelTag.InnerHtml);
            }
        }

        [DataTestMethod,
            DataRow(null, null),
            DataRow("IncreaseIndentLegacy", null),
            DataRow("IncreaseIndentLegacy", "BitSpinButtonIcon")
        ]
        public void SpinButtonShoudRenderCorrectIcon(string iconName, string iconAriaLabel)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.IconName, iconName);
                parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
            });

            if (iconName is null)
            {
                Assert.AreEqual(2, component.FindAll("i").Count);
            }
            else
            {
                var icon = component.FindAll(".bit-icon")[0];
                Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));

                if (iconAriaLabel is not null) Assert.AreEqual(iconAriaLabel, icon.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow("IncreaseIndentLegacy", null, true),
            DataRow("IncreaseIndentLegacy", null, false),

            DataRow("IncreaseIndentLegacy", "BitSpinButtonIcon", true),
            DataRow("IncreaseIndentLegacy", "BitSpinButtonIcon", false)
        ]
        public void SpinButtonShoudRenderCorrectIncrementButton(string iconName, string iconAriaLabel, bool isEnabled)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.IncrementButtonIconName, iconName);
                parameters.Add(p => p.IncrementButtonAriaLabel, iconAriaLabel);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var button = component.Find("button");
            var icon = component.Find("button > span > i");

            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

            if (iconAriaLabel is not null)
            {
                Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow("IncreaseIndentLegacy", null, true),
            DataRow("IncreaseIndentLegacy", null, false),

            DataRow("IncreaseIndentLegacy", "BitSpinButtonIcon", true),
            DataRow("IncreaseIndentLegacy", "BitSpinButtonIcon", false)
        ]
        public void SpinButtonShoudRenderCorrectDecrementButton(string iconName, string iconAriaLabel, bool isEnabled)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.DecrementButtonIconName, iconName);
                parameters.Add(p => p.DecrementButtonAriaLabel, iconAriaLabel);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var button = component.Find("button:last-child");
            var icon = component.Find("button:last-child > span > i");

            Assert.IsTrue(icon.ToMarkup().Contains($"bit-icon--{iconName}"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

            if (iconAriaLabel is not null)
            {
                Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow(" cm"),
            DataRow(" Inch"),
            DataRow(" foot")
        ]
        public void SpinButtonShouldHaveSuffixWhenItsPropertySet(string suffix)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Suffix, suffix));

            var input = component.Find("input");
            var inputValue = input.GetAttribute("value");

            Assert.IsTrue(inputValue.Contains(suffix));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, BitSpinButtonLabelPosition.Left),
            DataRow(Visual.Fluent, BitSpinButtonLabelPosition.Top),

            DataRow(Visual.Cupertino, BitSpinButtonLabelPosition.Left),
            DataRow(Visual.Cupertino, BitSpinButtonLabelPosition.Top),

            DataRow(Visual.Material, BitSpinButtonLabelPosition.Left),
            DataRow(Visual.Material, BitSpinButtonLabelPosition.Top),
        ]
        public void SpinButtonShouldHaveLabelPositionClassName(Visual visual, BitSpinButtonLabelPosition labelPosition)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LabelPosition, labelPosition);
            });

            var labelPositionClass = labelPosition == BitSpinButtonLabelPosition.Left ? "left" : "top";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            var spinButton = component.Find(".bit-spb");
            Assert.IsTrue(spinButton.ClassList.Contains($"bit-spb-label-{labelPositionClass}-{visualClass}"));
        }

        [DataTestMethod,
            DataRow("dir", "ltr"),
            DataRow("type", "number")
        ]
        public void SpinButtonInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
        {
            var inputHtmlAttributes = new Dictionary<string, object> {
                {attrKey, attrValue }
            };
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.InputHtmlAttributes, inputHtmlAttributes);
            });

            var input = component.Find("input");
            Assert.AreEqual(attrValue, input.GetAttribute(attrKey));
        }

        //[DataTestMethod,
        //    DataRow(2, 4, 2),
        //    DataRow(20, 22, 20)]
        //public void SpinButtonShouldRespectMaxValue(double max, double countOfClick, double expectedResult)
        //{
        //    var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Max, max));

        //    var input = component.Find("input");
        //    var upButton = component.FindAll("button").First();
        //    for (int i = 0; i < countOfClick; i++)
        //    {
        //        upButton.Click();
        //    }
        //    var inputValue = double.Parse(input.GetAttribute("value"));

        //    Assert.AreEqual(inputValue, expectedResult);
        //}

        //[DataTestMethod,
        //    DataRow(0, 4, 0),
        //    DataRow(-2, 22, -2)]
        //public void SpinButtonShouldRespectMinValue(double min, double countOfClick, double expectedResult)
        //{
        //    var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Min, min));

        //    var input = component.Find("input");
        //    var downButton = component.FindAll("button").Last();
        //    for (int i = 0; i < countOfClick; i++)
        //    {
        //        downButton.Click();
        //    }
        //    var inputValue = double.Parse(input.GetAttribute("value"));

        //    Assert.AreEqual(inputValue, expectedResult);
        //}


        [DataTestMethod,
            DataRow("IncreaseIndentLegacy", true),
            DataRow("IconName", true)]
        public void SpinButtonLabelShouldHaveIconClassName(string iconName, bool expectedResult)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.IconName, iconName));

            var IconElement = component.Find(".bit-spb > div > i");
            var hasIconClass = IconElement.ClassList.Contains($"bit-icon--{iconName}");

            Assert.AreEqual(hasIconClass, expectedResult);
        }
    }
}
