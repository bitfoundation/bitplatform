﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.NumericTextField.Int;

[TestClass]
public class BitNumericTextFieldIntTests : BunitTestContext
{
    private int BitNumericTextFieldTwoWayBoundValue;

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod,
         DataRow(null),
         DataRow("The Label")
    ]
    public void BitNumericTextFieldShouldHaveCorrectLabel(string label)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        if (string.IsNullOrEmpty(label))
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
         DataRow(true),
         DataRow(false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectlyWithArrows(bool arrows)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, arrows);
        });

        var arrowButtonHolder = component.FindAll(".bit-ntf-act");
        var arrowButtons = component.FindAll(".bit-ntf-act button");

        if (arrows)
        {
            Assert.AreEqual(1, arrowButtonHolder.Count);
            Assert.AreEqual(2, arrowButtons.Count);
        }
        else
        {
            Assert.AreEqual(0, arrowButtonHolder.Count);
            Assert.AreEqual(0, arrowButtons.Count);
        }
    }

    [DataTestMethod,
         DataRow(null, null),
         DataRow("IncreaseIndentLegacy", null),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon")
    ]
    public void BitNumericTextFieldShouldRenderCorrectIconWithEnableArrows(string iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
            parameters.Add(p => p.ShowButtons, true);
        });

        if (iconName.HasValue())
        {
            var icon = component.FindAll(".bit-icon")[0];
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));

            if (string.IsNullOrEmpty(iconAriaLabel) is false)
            {
                Assert.AreEqual(iconAriaLabel, icon.GetAttribute("aria-label"));
            }
        }
        else
        {
            Assert.AreEqual(2, component.FindAll("i").Count);
        }
    }

    [DataTestMethod,
         DataRow(null, null),
         DataRow("IncreaseIndentLegacy", null),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon")
    ]
    public void BitNumericTextFieldShouldRenderCorrectIcon(string iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
        });

        if (iconName.HasValue())
        {
            var icon = component.FindAll(".bit-icon")[0];
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));

            if (string.IsNullOrEmpty(iconAriaLabel) is false)
            {
                Assert.AreEqual(iconAriaLabel, icon.GetAttribute("aria-label"));
            }
        }
        else
        {
            Assert.AreEqual(0, component.FindAll("i").Count);
        }
    }

    [DataTestMethod,
         DataRow("IncreaseIndentLegacy", null, true),
         DataRow("IncreaseIndentLegacy", null, false),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon", true),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon", false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectIncrementButton(string iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.IncrementIconName, iconName);
            parameters.Add(p => p.IncrementAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ShowButtons, true);
        });

        var button = component.Find("button");
        var icon = component.Find("button > span > i");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

        if (string.IsNullOrEmpty(iconAriaLabel) is false)
        {
            Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
        }
    }

    [DataTestMethod,
         DataRow("IncreaseIndentLegacy", null, true),
         DataRow("IncreaseIndentLegacy", null, false),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon", true),
         DataRow("IncreaseIndentLegacy", "BitNumericTextFieldButtonIcon", false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectDecrementButton(string iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.DecrementIconName, iconName);
            parameters.Add(p => p.DecrementAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ShowButtons, true);
        });

        var button = component.Find("button:last-child");
        var icon = component.Find("button:last-child > span > i");

        Assert.IsTrue(icon.ToMarkup().Contains($"bit-icon--{iconName}"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

        if (string.IsNullOrEmpty(iconAriaLabel) is false)
        {
            Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
        }
    }

    [DataTestMethod,
         DataRow("{0} cm", 0),
         DataRow("{0} Inch", 24),
         DataRow("{0} foot", 100),
         DataRow("{0:0} foot", 1000)
    ]
    public void BitNumericTextFieldShouldHaveNumberFormaWhenItsPropertySet(string numberFormat, int defaultValue)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.NumberFormat, numberFormat);
        });

        var input = component.Find("input");
        var inputValue = input.GetAttribute("value");
        var expectedValue = string.Format(numberFormat, defaultValue);

        Assert.AreEqual(expectedValue, inputValue);
    }

    [DataTestMethod,
         DataRow(BitNumericTextFieldLabelPosition.Left),
         DataRow(BitNumericTextFieldLabelPosition.Top)
    ]
    public void BitNumericTextFieldShouldHaveLabelPositionClassName(BitNumericTextFieldLabelPosition labelPosition)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, labelPosition);
        });

        var labelPositionClass = labelPosition == BitNumericTextFieldLabelPosition.Left ? "lf" : "tp";

        var numericTextFieldButton = component.Find(".bit-ntf");

        Assert.IsTrue(numericTextFieldButton.ClassList.Contains($"bit-ntf-l{labelPositionClass}"));
    }

    [DataTestMethod,
         DataRow("dir", "ltr"),
         DataRow("foo", "bar")
    ]
    public void BitNumericTextFieldInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
    {
        var inputHtmlAttributes = new Dictionary<string, object> { { attrKey, attrValue } };
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, inputHtmlAttributes);
        });

        var input = component.Find("input");
        Assert.AreEqual(attrValue, input.GetAttribute(attrKey));
    }

    [DataTestMethod,
         DataRow("NtfWrapper", null, null, null),
         DataRow(null, "NftAriaLabel", null, null),
         DataRow("NtfWrapper", "NftAriaLabel", 5, 3),
         DataRow(null, null, 5, 3)
    ]
    public void BitNumericTextFieldWrapperShouldHaveCorrectAttributes(string title, string ariaLabel, int? ariaSetSize, int? ariaPositionInSet)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.AriaSetSize, ariaSetSize);
            parameters.Add(p => p.AriaPositionInSet, ariaPositionInSet);
        });

        var ntfWrapper = component.Find(".bit-ntf-wrp");

        if (string.IsNullOrEmpty(title) is false)
        {
            Assert.AreEqual(title, ntfWrapper.GetAttribute("title"));
        }

        if (string.IsNullOrEmpty(ariaLabel) is false)
        {
            Assert.AreEqual(ariaLabel, ntfWrapper.GetAttribute("aria-label"));
        }

        if (ariaSetSize is not null)
        {
            Assert.AreEqual(ariaSetSize.ToString(), ntfWrapper.GetAttribute("aria-setsize"));
            Assert.AreEqual(ariaPositionInSet.ToString(), ntfWrapper.GetAttribute("aria-posinset"));
        }
    }

    [DataTestMethod,
         DataRow(null, null),
         DataRow(5, null),
         DataRow(null, 100),
         DataRow(0, 100),
         DataRow(50, 1)
    ]
    public void BitNumericTextFieldShouldHaveCorrectMaxMin(int? min, int? max)
    {
        var component = RenderComponent<BitNumericTextField<int?>>(parameters =>
        {
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        var input = component.Find("input");
        int? expectedMinValue = null;
        int? expectedMaxValue = null;

        if (max.HasValue)
        {
            expectedMaxValue = max.Value;
        }

        if (min.HasValue)
        {
            expectedMinValue = min.Value;
        }

        Assert.AreEqual(expectedMinValue.HasValue ? expectedMinValue.ToString() : null, input.GetAttribute("aria-valuemin"));
        Assert.AreEqual(expectedMaxValue.HasValue ? expectedMaxValue.ToString() : null, input.GetAttribute("aria-valuemax"));
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumericTextFieldOnIncrementTest(int countOfClicks)
    {
        int onIncrementEventCounter = 0;
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, true);
            parameters.Add(p => p.OnIncrement, () => onIncrementEventCounter++);
        });

        var increaseButton = component.Find("button.bit-ntf-aup");
        for (int i = 0; i < countOfClicks; i++)
        {
            increaseButton.PointerDown();
            increaseButton.PointerUp();
        }

        Assert.AreEqual(countOfClicks, onIncrementEventCounter);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumericTextFieldOnDecrementTest(int countOfClicks)
    {
        int onDecrementEventCounter = 20;
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, true);
            parameters.Add(p => p.OnDecrement, () => onDecrementEventCounter--);
        });

        var decreaseButton = component.Find("button.bit-ntf-adn");
        for (int i = 0; i < countOfClicks; i++)
        {
            decreaseButton.PointerDown();
            decreaseButton.PointerUp();
        }

        Assert.AreEqual(20 - countOfClicks, onDecrementEventCounter);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumericTextFieldInputOnBlurEventCallbackTest(int countOfBlur)
    {
        int onBlurEventCounter = 0;
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.OnBlur, () => onBlurEventCounter++);
        });

        var input = component.Find("input");
        for (int i = 0; i < countOfBlur; i++)
        {
            input.Blur();
        }

        Assert.AreEqual(onBlurEventCounter, countOfBlur);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumericTextFieldInputOnFocusTest(int countOfFocus)
    {
        int onFocusEventCounter = 0;
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => onFocusEventCounter++);
        });

        var input = component.Find("input");
        for (int i = 0; i < countOfFocus; i++)
        {
            input.Focus();
        }

        Assert.AreEqual(onFocusEventCounter, countOfFocus);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumericTextFieldOnChangeTest(int inputValue)
    {
        int onChangeEventValue = 0;
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.OnChange, (int value) => onChangeEventValue = value);
        });

        var input = component.Find("input");
        input.Change(inputValue);
        input.Blur();

        Assert.AreEqual(onChangeEventValue, inputValue);
    }

    [DataTestMethod,
         DataRow(null),
         DataRow("AriaDescription")
    ]
    public void BitNumericTextFieldShouldHaveCorrectAriaDescription(string ariaDescription)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var input = component.Find("input");

        Assert.AreEqual(ariaDescription, input.GetAttribute("aria-describedby"));
    }

    [DataTestMethod,
         DataRow(3, null),
         DataRow(3, "{0} cm"),
         DataRow(null, null)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueNow(int? ariaValueNow, string numberFormat)
    {
        var component = RenderComponent<BitNumericTextField<int?>>(parameters =>
        {
            parameters.Add(p => p.AriaValueNow, ariaValueNow);
            if (numberFormat.HasValue())
            {
                parameters.Add(p => p.NumberFormat, numberFormat);
            }
        });

        var input = component.Find("input");
        var expectedResult = ariaValueNow.HasValue ? ariaValueNow : numberFormat.HasNoValue() ? component.Instance.Value : null;
        var attributeValue = input.GetAttribute("aria-valuenow");

        if (expectedResult.HasValue is false)
        {
            Assert.IsNull(attributeValue);
        }
        else
        {
            Assert.AreEqual(expectedResult.ToString(), attributeValue);
        }
    }

    [DataTestMethod,
         DataRow("3", null, 0),
         DataRow(null, "{0} cm", 0),
         DataRow(null, null, 0)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueText(string ariaValueText, string numberFormat, int precision)
    {
        var component = RenderComponent<BitNumericTextField<double>>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, ariaValueText);
            parameters.Add(p => p.Precision, precision);
            if (numberFormat.HasValue())
            {
                parameters.Add(p => p.NumberFormat, numberFormat);
            }
        });

        var input = component.Find("input");
        var expectedResult = ariaValueText.HasValue() ? ariaValueText : numberFormat.HasValue() ? string.Format(numberFormat, Normalize(component.Instance.Value, precision)) : component.Instance.Value.ToString();
        Assert.AreEqual(expectedResult, input.GetAttribute("aria-valuetext"));
    }

    [DataTestMethod,
         DataRow(3, 1, 12),
         DataRow(8, 2, 10),
         DataRow(8, 1, 8),
         DataRow(8, 2, 9),
         DataRow(8, 5, 9)
    ]
    public void BitNumericTextFieldIncrementButtonClickTest(int defaultValue, int step, int max)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-ntf-aup");
        incrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue + step <= max
            ? defaultValue + step
            : defaultValue;

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3, 1, 12),
         DataRow(8, 2, 10),
         DataRow(8, 1, 8),
         DataRow(8, 2, 9),
         DataRow(8, 5, 9)
    ]
    public void BitNumericTextFieldArrowUpKeyDownTest(int defaultValue, int step, int max)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs
        {
            Key = "ArrowUp"
        };
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue + step <= max
            ? defaultValue + step
            : defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(3, 1, 0),
         DataRow(2, 2, 0),
         DataRow(3, 4, 0),
         DataRow(0, 1, 0)
    ]
    public void BitNumericTextFieldDecrementButtonClickTest(int defaultValue, int step, int min)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var decrementButton = component.Find("button.bit-ntf-adn");
        decrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue - step >= min
            ? defaultValue - step
            : defaultValue;

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3, 1, 0),
         DataRow(2, 2, 0),
         DataRow(3, 4, 0),
         DataRow(0, 1, 0)
    ]
    public void BitNumericTextFieldArrowDownKeyDownTest(int defaultValue, int step, int min)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs
        {
            Key = "ArrowDown"
        };
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue - step >= min
            ? defaultValue - step
            : defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(50.02, 0, 100, "25"),
         DataRow(50.02, 0, 100, "112.2"),
         DataRow(50.02, 0, 100, "62.72"),
         DataRow(50.02, 0, 100, "-5"),
         DataRow(50.02, 0, 100, "text123")
    ]
    public void BitNumericTextFieldEnterKeyDownTest(double defaultValue, int min, int max, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<double>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.Min, min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs
        {
            Value = userInput
        };
        input.Change(changeArgs);
        var keyboardArgs = new KeyboardEventArgs
        {
            Key = "Enter"
        };
        input.KeyDown(keyboardArgs);
        var inputValue = component.Instance.Value;
        double? expectedResult = 0;
        var isNumber = double.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = Normalize(numericValue, 0);
            if (expectedResult > max) expectedResult = max;
            if (expectedResult < min) expectedResult = min;
        }
        else
        {
            expectedResult = Normalize(defaultValue, 0);
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5, 0, 100, "25"),
         DataRow(5, 0, 100, "112"),
         DataRow(5, 0, 100, "-5"),
         DataRow(5, 0, 100, "text123")
    ]
    public void BitNumericTextFieldOnBlurTest(double defaultValue, int min, int max, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<double>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.Min, min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs
        {
            Value = userInput
        };
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        double? expectedResult = 0;
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
         DataRow(0, 100, 2, "25"),
         DataRow(0, 100, 25, "12"),
         DataRow(0, 10, 52, "12"),
         DataRow(13, 100, 523, "12")
    ]
    public void BitNumericTextFieldPrecisionTest(int min, int max, int step, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.Min, min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs
        {
            Value = userInput
        };
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        var precision = CalculatePrecision(step);
        var expectedResult = Normalize(int.Parse(userInput), precision);
        if (expectedResult > max) expectedResult = max;
        if (expectedResult < min) expectedResult = min;

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5, 2, 4),
         DataRow(1, 15, 1)
    ]
    public void BitNumericTextFieldTwoWayBoundWithCustomHandlerShouldWorkCorrect(int value, int countOfIncrements, int step)
    {
        BitNumericTextFieldTwoWayBoundValue = value;

        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Value, BitNumericTextFieldTwoWayBoundValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
            parameters.Add(p => p.ShowButtons, true);
        });

        var incrementButton = component.Find("button.bit-ntf-aup");
        for (var i = 0; i < countOfIncrements; i++)
        {
            incrementButton.PointerDown();
        }

        var expectedValue = value + (step * countOfIncrements);

        Assert.AreEqual(expectedValue, BitNumericTextFieldTwoWayBoundValue);
    }

    [DataTestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitNumericTextFieldLabelFragmentTest(string labelFragment)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelFragment);
        });

        var ntfLabelChild = component.Find("label").ChildNodes;
        ntfLabelChild.MarkupMatches(labelFragment);
    }

    [Ignore]
    [DataTestMethod,
         DataRow(3, 1, 100, 475),
         DataRow(3, 1, 100, 550)
    ]
    public void BitNumericTextFieldContinuousIncrementOnPointerDownTest(int defaultValue, int step, int max, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-ntf-aup");
        var initialIncrementCount = timeout / 400;
        var continuousIncrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = defaultValue + step * (initialIncrementCount + continuousIncrementCount);
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")),
            TimeSpan.FromMilliseconds(timeout));
    }

    [Ignore]
    [DataTestMethod,
         DataRow(50, 1, 0, 475),
         DataRow(50, 1, 0, 550)
    ]
    public void BitNumericTextFieldContinuousDecrementOnPointerDownTest(int defaultValue, int step, int min, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-ntf-aup");
        var initialDecrementCount = timeout / 400;
        var continuousDecrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = defaultValue - step * (initialDecrementCount + continuousDecrementCount);
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")),
            TimeSpan.FromMilliseconds(timeout));
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(10),
         DataRow(0),
         DataRow(-1),
         DataRow(6),
         DataRow(18),
    ]
    public void BitNumericTextFieldValidationFormTest(int value)
    {
        var component = RenderComponent<BitNumericTextFieldIntValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldIntTestModel { Value = value });
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

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
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
    public void BitNumericTextFieldValidationInvalidHtmlAttributeTest(int value)
    {
        var component = RenderComponent<BitNumericTextFieldIntValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldIntTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value < 6 || value > 18;

        var input = component.Find("input");
        Assert.IsFalse(input.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, input.HasAttribute("aria-invalid"));
        if (input.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
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
         DataRow(2),
         DataRow(8)
    ]
    public void BitNumericTextFieldValidationInvalidCssClassTest(int value)
    {
        var component = RenderComponent<BitNumericTextFieldIntValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldIntTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value < 6 || value > 18;

        var NumericTextField = component.Find(".bit-ntf");

        Assert.IsFalse(NumericTextField.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, NumericTextField.ClassList.Contains("bit-inv"));

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

        Assert.AreEqual(!isInvalid, NumericTextField.ClassList.Contains("bit-inv"));
    }

    private double? Normalize(double? value, int precision) =>
        value.HasValue ? (double?)Math.Round(value.Value, precision) : null;

    private int CalculatePrecision(int value)
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

    private void HandleValueChanged(int value)
        => BitNumericTextFieldTwoWayBoundValue = value;
}
