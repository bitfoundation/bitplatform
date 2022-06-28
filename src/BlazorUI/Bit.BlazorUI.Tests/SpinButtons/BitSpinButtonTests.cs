﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.SpinButtons
{
    [TestClass]
    public class BitSpinButtonTests : BunitTestContext
    {
        private double BitSpinButtonTwoWayBoundValue;

        [TestInitialize]
        public void SetupJsInteropMode()
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [DataTestMethod,
            DataRow(null),
            DataRow("Button Label")
        ]
        public void BitSpinButtonShoudHaveCorrectLabel(string label)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Label, label);
            });

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
            DataRow(BitIconName.IncreaseIndentLegacy, null),
            DataRow(BitIconName.IncreaseIndentLegacy, "BitSpinButtonIcon")
        ]
        public void BitSpinButtonShoudRenderCorrectIcon(BitIconName? iconName, string iconAriaLabel)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.IconName, iconName);
                parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
            });

            if (iconName.HasValue)
            {
                var icon = component.FindAll(".bit-icon")[0];
                Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

                if (iconAriaLabel is not null) Assert.AreEqual(iconAriaLabel, icon.GetAttribute("aria-label"));
            }
            else
            {
                Assert.AreEqual(2, component.FindAll("i").Count);
            }
        }

        [DataTestMethod,
            DataRow(BitIconName.IncreaseIndentLegacy, null, true),
            DataRow(BitIconName.IncreaseIndentLegacy, null, false),

            DataRow(BitIconName.IncreaseIndentLegacy, "BitSpinButtonIcon", true),
            DataRow(BitIconName.IncreaseIndentLegacy, "BitSpinButtonIcon", false)
        ]
        public void BitSpinButtonShoudRenderCorrectIncrementButton(BitIconName iconName, string iconAriaLabel, bool isEnabled)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.IncrementButtonIconName, iconName);
                parameters.Add(p => p.IncrementButtonAriaLabel, iconAriaLabel);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var button = component.Find("button");
            var icon = component.Find("button > span > i");

            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
            Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

            if (iconAriaLabel is not null)
            {
                Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
            }
        }

        [DataTestMethod,
            DataRow(BitIconName.IncreaseIndentLegacy, null, true),
            DataRow(BitIconName.IncreaseIndentLegacy, null, false),

            DataRow(BitIconName.IncreaseIndentLegacy, "BitSpinButtonIcon", true),
            DataRow(BitIconName.IncreaseIndentLegacy, "BitSpinButtonIcon", false)
        ]
        public void BitSpinButtonShoudRenderCorrectDecrementButton(BitIconName iconName, string iconAriaLabel, bool isEnabled)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.DecrementButtonIconName, iconName);
                parameters.Add(p => p.DecrementButtonAriaLabel, iconAriaLabel);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var button = component.Find("button:last-child");
            var icon = component.Find("button:last-child > span > i");

            Assert.IsTrue(icon.ToMarkup().Contains($"bit-icon--{iconName.GetName()}"));
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
        public void BitSpinButtonShouldHaveSuffixWhenItsPropertySet(string suffix)
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
        public void BitSpinButtonShouldHaveLabelPositionClassName(Visual visual, BitSpinButtonLabelPosition labelPosition)
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
        public void BitSpinButtonInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
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
        public void BitSpinButtonWrapperShouldHaveCorrectAttributes(string title, string ariaLabel, int? ariaSetSize, int? ariaPositionInSet)
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
        public void BitSpinButtonShouldHaveCorrectMaxMin(double? min, double? max)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
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
        public void BitSpinButtonOnIncrementTest(int countOfCliks)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var increaseButton = component.FindAll("button")[0];
            for (int i = 0; i < countOfCliks; i++)
            {
                increaseButton.MouseDown();
            }

            Assert.AreEqual(countOfCliks, component.Instance.OnIncrementEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void BitSpinButtonOnDecrementTest(int countOfCliks)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var decreaseButton = component.FindAll("button")[1];
            var onDecrementEventCounterInitValue = component.Instance.OnDecrementEventCounter;
            for (int i = 0; i < countOfCliks; i++)
            {
                decreaseButton.MouseDown();
            }

            Assert.AreEqual(onDecrementEventCounterInitValue - countOfCliks, component.Instance.OnDecrementEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void BitSpinButtonInputOnBlurEventCallbackTest(int countOfBlur)
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
        public void BitSpinButtonInputOnFocusTest(int countOfFocus)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var input = component.Find("input");
            for (int i = 0; i < countOfFocus; i++)
            {
                input.Focus();
            }

            Assert.AreEqual(countOfFocus, component.Instance.OnFocusEventCounter);
        }

        [DataTestMethod,
           DataRow(4),
           DataRow(12)
        ]
        public void BitSpinButtonOnChangeTest(double inputValue)
        {
            var component = RenderComponent<BitSpinButtonTest>();

            var input = component.Find("input");
            var changeArgs = new ChangeEventArgs();
            changeArgs.Value = inputValue;
            input.Change(changeArgs);
            input.Blur();

            Assert.AreEqual(inputValue, component.Instance.OnChangeEventValue);
        }

        [DataTestMethod,
           DataRow(null),
           DataRow("AriaDescription")
        ]
        public void BitSpinButtonShoudHaveCorrectAriaDecription(string ariaDescription)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var input = component.Find("input");

            Assert.AreEqual(ariaDescription, input.GetAttribute("aria-describedby"));
        }

        [DataTestMethod,
           DataRow(3.0, null),
           DataRow(3.0, " cm"),
           DataRow(null, null)
        ]
        public void BitSpinButtonInputShoudHaveCorrectAriaValueNow(double? ariaValueNow, string suffix)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaValueNow, ariaValueNow);
                parameters.Add(p => p.Suffix, suffix);
            });

            var input = component.Find("input");
            var expectedResult = ariaValueNow is not null ? ariaValueNow : suffix.HasNoValue() ? component.Instance.Value : null;
            Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("aria-valuenow"));
        }

        [DataTestMethod,
           DataRow("3", null, 0),
           DataRow(null, " cm", 0),
           DataRow(null, null, 0)
        ]
        public void BitSpinButtonInputShoudHaveCorrectAriaValueText(string ariaValueText, string suffix, int precision)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaValueText, ariaValueText);
                parameters.Add(p => p.Suffix, suffix);
                parameters.Add(p => p.Precision, precision);
            });

            var input = component.Find("input");
            var expectedResult = ariaValueText.HasValue() ? ariaValueText : suffix.HasValue() ? $"{Normalize(component.Instance.Value, precision)}{suffix}" : null;
            Assert.AreEqual(expectedResult, input.GetAttribute("aria-valuetext"));
        }

        [DataTestMethod,
            DataRow(3, 1, 12),
            DataRow(8, 2, 10),
            DataRow(8, 1, 8),
            DataRow(8, 2, 9),
            DataRow(8, 5, 9)
        ]
        public void BitSpinButtonIncrementButtonClickTest(double defaultValue, double step, double max)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var incrementButton = component.FindAll("button")[0];
            incrementButton.MouseDown();
            var inputValue = input.GetAttribute("value");
            var expectedResult = defaultValue + step <= max ? defaultValue + step : defaultValue;

            Assert.AreEqual(expectedResult.ToString(), inputValue);
        }

        [DataTestMethod,
            DataRow(3, 1, 12),
            DataRow(8, 2, 10),
            DataRow(8, 1, 8),
            DataRow(8, 2, 9),
            DataRow(8, 5, 9)
        ]
        public void BitSpinButtonArrowUpKeyDownTest(double defaultValue, double step, double max)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var args = new KeyboardEventArgs();
            args.Key = "ArrowUp";
            input.KeyDown(args);
            var inputValue = input.GetAttribute("value");
            var expectedResult = defaultValue + step <= max ? defaultValue + step : defaultValue;

            Assert.AreEqual(expectedResult.ToString(), inputValue);
        }

        [DataTestMethod,
            DataRow(3, 1, 0),
            DataRow(2, 2, 0),
            DataRow(3, 4, 0),
            DataRow(0, 1, 0)
        ]
        public void BitSpinButtonDecrementButtonClickTest(double defaultValue, double step, double min)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var decrementButton = component.FindAll("button")[1];
            decrementButton.MouseDown();
            var inputValue = input.GetAttribute("value");
            var expectedResult = defaultValue - step >= min ? defaultValue - step : defaultValue;

            Assert.AreEqual(expectedResult.ToString(), inputValue);
        }

        [DataTestMethod,
            DataRow(3, 1, 0),
            DataRow(2, 2, 0),
            DataRow(3, 4, 0),
            DataRow(0, 1, 0)
        ]
        public void BitSpinButtonArrowDownKeyDownTest(double defaultValue, double step, double min)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var args = new KeyboardEventArgs();
            args.Key = "ArrowDown";
            input.KeyDown(args);
            var inputValue = input.GetAttribute("value");
            var expectedResult = defaultValue - step >= min ? defaultValue - step : defaultValue;

            Assert.AreEqual(expectedResult.ToString(), inputValue);
        }

        [DataTestMethod,
            DataRow(5, 0, 100, "25"),
            DataRow(5, 0, 100, "112"),
            DataRow(5, 0, 100, "-5"),
            DataRow(5, 0, 100, "text123")
        ]
        public void BitSpinButtonEnterKeyDownTest(double defaultValue, double min, double max, string userInput)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.DefaultValue, defaultValue);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.Min, min);
            });

            var input = component.Find("input");
            var changeArgs = new ChangeEventArgs();
            changeArgs.Value = userInput;
            input.Change(changeArgs);
            var keyboardArgs = new KeyboardEventArgs();
            keyboardArgs.Key = "Enter";
            input.KeyDown(keyboardArgs);
            var inputValue = component.Instance.Value;
            double expectedResult = 0;
            var isNumber = double.TryParse(userInput, out var numericValue);
            if (isNumber)
            {
                expectedResult = Normalize(numericValue, 1);
                if (expectedResult > max) expectedResult = max;
                if (expectedResult < min) expectedResult = min;
            }
            else
            {
                expectedResult = defaultValue;
            }

            Assert.AreEqual(expectedResult, inputValue);
        }

        [DataTestMethod,
            DataRow(5, 0, 100, "25"),
            DataRow(5, 0, 100, "112"),
            DataRow(5, 0, 100, "-5"),
            DataRow(5, 0, 100, "text123")
        ]
        public void BitSpinButtonOnBlurTest(double defaultValue, double min, double max, string userInput)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.DefaultValue, defaultValue);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.Min, min);
            });

            var input = component.Find("input");
            var changeArgs = new ChangeEventArgs();
            changeArgs.Value = userInput;
            input.Change(changeArgs);
            input.Blur();
            var inputValue = component.Instance.Value;
            double expectedResult = 0;
            var isNumber = double.TryParse(userInput, out var numericValue);
            if (isNumber)
            {
                expectedResult = Normalize(numericValue, 1);
                if (expectedResult > max) expectedResult = max;
                if (expectedResult < min) expectedResult = min;
            }
            else
            {
                expectedResult = defaultValue;
            }

            Assert.AreEqual(expectedResult, inputValue);
        }

        [DataTestMethod,
            DataRow(0, 100, 1, "25"),
            DataRow(0, 100, 0.1, "25.68"),
            DataRow(0, 100, 0.25, "12.6"),
            DataRow(0, 10, 0.25, "12.6"),
            DataRow(13, 100, 0.25, "12.6")
        ]
        public void BitSpinButtonPrecisionTest(double min, double max, double step, string userInput)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.Min, min);
            });

            var input = component.Find("input");
            var changeArgs = new ChangeEventArgs();
            changeArgs.Value = userInput;
            input.Change(changeArgs);
            input.Blur();
            var inputValue = component.Instance.Value;
            var precision = CalculatePrecision(step);
            var expectedResult = Normalize(double.Parse(userInput), precision);
            if (expectedResult > max) expectedResult = max;
            if (expectedResult < min) expectedResult = min;

            Assert.AreEqual(expectedResult, inputValue);
        }

        [DataTestMethod,
            DataRow(5, 2, 4),
            DataRow(1, 15, 1)
        ]
        public void BitSpinButtonTwoWayBoundWithCustomHandlerShouldWorkCurrect(double value, int countOfIncrements, double step)
        {
            BitSpinButtonTwoWayBoundValue = value;

            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Value, BitSpinButtonTwoWayBoundValue);
                parameters.Add(p => p.ValueChanged, HandleValueChanged);
            });

            var incrementButton = component.FindAll("button")[0];
            for (var i = 0; i < countOfIncrements; i++)
            {
                incrementButton.MouseDown();
            }

            var expectedValue = value + (step * countOfIncrements);

            Assert.AreEqual(expectedValue, BitSpinButtonTwoWayBoundValue);
        }

        [DataTestMethod,
            DataRow("<div>This is labelFragment</div>")
        ]
        public void BitSpinButtonLabelFragmentTest(string labelFragment)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters =>
            {
                parameters.Add(p => p.LabelFragment, labelFragment);
            });

            var spbLabelChild = component.Find("label").ChildNodes;
            spbLabelChild.MarkupMatches(labelFragment);
        }

        [DataTestMethod,
            DataRow(3, 1, 100, 475),
            DataRow(3, 1, 100, 550)
        ]
        public void BitSpinButtonContinuousIncrementOnMouseDownTest(double defaultValue, double step, double max, int timeout)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var incrementButton = component.FindAll("button")[0];
            var initialIncrementCount = timeout / 400;
            var continuousIncrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
            var expectedResult = defaultValue + step * (initialIncrementCount + continuousIncrementCount);
            incrementButton.MouseDown();

            component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")), TimeSpan.FromMilliseconds(timeout));
        }

        [DataTestMethod,
            DataRow(50, 1, 0, 475),
            DataRow(50, 1, 0, 550)
        ]
        public void BitSpinButtonContinuousDecrementOnMouseDownTest(double defaultValue, double step, double min, int timeout)
        {
            var component = RenderComponent<BitSpinButton>(parameters =>
            {
                parameters.Add(p => p.Step, step);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.DefaultValue, defaultValue);
            });

            var input = component.Find("input");
            var incrementButton = component.FindAll("button")[1];
            var initialDecrementCount = timeout / 400;
            var continuousDecrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
            var expectedResult = defaultValue - step * (initialDecrementCount + continuousDecrementCount);
            incrementButton.MouseDown();

            component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")), TimeSpan.FromMilliseconds(timeout));
        }

        [DataTestMethod,
            DataRow(4),
            DataRow(10),
            DataRow(0),
            DataRow(-1),
            DataRow(6),
            DataRow(18),
        ]
        public void BitSpinButtonValidationFormTest(double value)
        {
            var component = RenderComponent<BitSpinButtonValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitSpinButtonTestModel { Value = value });
                parameters.Add(p => p.IsEnabled, true);
            });

            var isValid = value >= 6 && value <= 18;

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
            Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

            var input = component.Find("input");
            if (isValid)
            {
                input.Change(2);
            }
            else
            {
                input.Change(8);
            }

            input.Blur();
            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, 1);
            Assert.AreEqual(component.Instance.InvalidCount, 1);
            Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
        }

        [DataTestMethod,
            DataRow(4),
            DataRow(10),
            DataRow(0),
            DataRow(-1),
            DataRow(6),
            DataRow(18),
        ]
        public void BitSpinButtonValidationInvalidHtmlAttributeTest(double value)
        {
            var component = RenderComponent<BitSpinButtonValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitSpinButtonTestModel { Value = value });
                parameters.Add(p => p.IsEnabled, true);
            });

            var isInvalid = value < 6 || value > 18;

            var input = component.Find("input");
            Assert.IsFalse(input.HasAttribute("aria-invalid"));

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(input.HasAttribute("aria-invalid"), isInvalid);
            if (input.HasAttribute("aria-invalid"))
            {
                Assert.AreEqual(input.GetAttribute("aria-invalid"), "true");
            }

            if (isInvalid)
            {
                input.Change(10);
                input.Blur();
                Assert.IsFalse(input.HasAttribute("aria-invalid"));
            }
            else
            {
                input.Change(4);
                input.Blur();
                Assert.IsTrue(input.HasAttribute("aria-invalid"));
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, 2),
            DataRow(Visual.Fluent, 8),
            DataRow(Visual.Cupertino, 2),
            DataRow(Visual.Cupertino, 8),
            DataRow(Visual.Material, 2),
            DataRow(Visual.Material, 8),
        ]
        public void BitSpinButtonValidationInvalidCssClassTest(Visual visual, double value)
        {
            var component = RenderComponent<BitSpinButtonValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitSpinButtonTestModel { Value = value });
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.Visual, visual);
            });

            var isInvalid = value < 6 || value > 18;

            var bitSpinButton = component.Find(".bit-spb");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsFalse(bitSpinButton.ClassList.Contains($"bit-spb-invalid-{visualClass}"));

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(bitSpinButton.ClassList.Contains($"bit-spb-invalid-{visualClass}"), isInvalid);

            var input = component.Find("input");
            if (isInvalid)
            {
                input.Change(10);
            }
            else
            {
                input.Change(3);
            }
            input.Blur();

            Assert.AreEqual(bitSpinButton.ClassList.Contains($"bit-spb-invalid-{visualClass}"), !isInvalid);
        }

        private double Normalize(double value, int precision) => Math.Round(value, precision);

        private int CalculatePrecision(double value)
        {
            var regex = new Regex(@"[1-9]([0]+$)|\.([0-9]*)");
            if (regex.IsMatch(value.ToString()) is false) return 0;

            var matches = regex.Matches(value.ToString());
            if (matches.Count == 0) return 0;

            var groups = matches[0].Groups;
            if (groups[1] != null && groups[1].Length != 0)
            {
                return -groups[1].Length;
            }

            if (groups[2] != null && groups[2].Length != 0)
            {
                return groups[2].Length;
            }

            return 0;
        }

        private void HandleValueChanged(double value)
        {
            BitSpinButtonTwoWayBoundValue = value;
        }
    }
}
