using System;
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

        [DataTestMethod,
            DataRow("SpbWrapper", null, null, null),
            DataRow(null, "SpbAriaLabel", null, null),
            DataRow("SpbWrapper", "SpbAriaLabel", 5, 3),
            DataRow(null, null, 5, 3)
        ]
        public void SpinButtonWrapperShouldHaveCorrectAttributes(string title, string ariaLabel, int? ariaSetSize, int? ariaPositionInSet)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.Title, title);
                parameters.Add(p => p.AriaLabel, ariaLabel);
                parameters.Add(p => p.AriaSetSize, ariaSetSize);
                parameters.Add(p => p.AriaPositionInSet, ariaPositionInSet);
            });

            var spbWrapper = component.Find(".bit-spb-wrapper");

            if (title is not null)
            {
                Assert.AreEqual(title, spbWrapper.GetAttribute("title"));
            }

            if (ariaLabel is not null)
            {
                Assert.AreEqual(ariaLabel, spbWrapper.GetAttribute("aria-label"));
            }

            if (ariaSetSize is not null)
            {
                Assert.AreEqual(ariaSetSize.ToString(), spbWrapper.GetAttribute("aria-setsize"));
                Assert.AreEqual(ariaPositionInSet.ToString(), spbWrapper.GetAttribute("aria-posinset"));
            }
        }

        [DataTestMethod,
            DataRow(null, null),
            DataRow(5.0, null),
            DataRow(null, 100.0),
            DataRow(0.0, 100.0),
            DataRow(50.0, 1.0)
        ]
        public void SpinButtonShouldHaveCorrectMaxMin(double? min, double? max)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
            });

            var input = component.Find("input");
            double expectedMinValue = 0;
            double expectedMaxValue = 0;
            if (min is null && max is null)
            {
                expectedMinValue = double.MinValue;
                expectedMaxValue = double.MaxValue;
            }

            if (min is null && max is not null)
            {
                expectedMinValue = double.MinValue;
                expectedMaxValue = max.Value;
            }

            if (min is not null && max is null)
            {
                expectedMinValue = min.Value;
                expectedMaxValue = double.MaxValue;
            }

            if (min is not null && max is not null)
            {
                if (min > max)
                {
                    min = min + max;
                    max = min - max;
                    min = min - max;
                }

                expectedMinValue = min.Value;
                expectedMaxValue = max.Value;
            }

            Assert.AreEqual(expectedMinValue.ToString(), input.GetAttribute("aria-valuemin"));
            Assert.AreEqual(expectedMaxValue.ToString(), input.GetAttribute("aria-valuemax"));
        }

        [DataTestMethod,
            DataRow(4),
            DataRow(12)
        ]
        public void SpinButtonOnIncrementTest(int countOfCliks)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var increaseButton = component.FindAll("button")[0];
            for (int i = 0; i < countOfCliks; i++)
            {
                increaseButton.Click();
            }

            Assert.AreEqual(countOfCliks, component.Instance.OnIncrementEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void SpinButtonOnDecrementTest(int countOfCliks)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var decreaseButton = component.FindAll("button")[1];
            var onDecrementEventCounterInitValue = component.Instance.OnDecrementEventCounter;
            for (int i = 0; i < countOfCliks; i++)
            {
                decreaseButton.Click();
            }

            Assert.AreEqual(onDecrementEventCounterInitValue - countOfCliks, component.Instance.OnDecrementEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void SpinButtonInputOnBlurTest(int countOfBlur)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var input = component.Find("input");
            for (int i = 0; i < countOfBlur; i++)
            {
                input.Blur();
            }

            Assert.AreEqual(countOfBlur, component.Instance.OnBlurEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void SpinButtonInputOnFocusTest(int countOfFocus)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var input = component.Find("input");
            for (int i = 0; i < countOfFocus; i++)
            {
                input.Focus();
            }

            Assert.AreEqual(countOfFocus, component.Instance.OnFocusEventCounter);
        }

        //[DataTestMethod,
        //    DataRow(3, 1, 12),
        //    DataRow(8, 2, 10),
        //    DataRow(8, 1, 8),
        //    DataRow(8, 2, 9)
        //]
        //public void SpinButtonIncreasementButtonTestClick(double value, double step, double max)
        //{
        //    var component = RenderComponent<BitSpinButtonTest>(parameters =>
        //    {
        //        parameters.Add(p => p.Step, step);
        //        parameters.Add(p => p.Max, max);
        //        parameters.Add(p => p.Value, value);
        //    });

        //    var input = component.Find("input");
        //    var increaseButton = component.FindAll("button")[0];
        //    increaseButton.Click();
        //    var inputValue = input.GetAttribute("value");
        //    var expectedResult = value + step > max ? max : value + step;

        //    Assert.AreEqual(expectedResult.ToString(), inputValue);
        //}
    }
}
