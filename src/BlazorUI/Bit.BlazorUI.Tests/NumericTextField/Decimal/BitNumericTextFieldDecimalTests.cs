﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.NumericTextField.Decimal;

[TestClass]
public class BitNumericTextFieldDecimalTests : BunitTestContext
{
    private decimal BitNumericTextFieldTwoWayBoundValue;

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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.ShowArrows, arrows);
        });

        var arrowButtonHolder = component.FindAll(".arrows");
        var arrowButtons = component.FindAll(".arrows button");

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
         DataRow(BitIconName.IncreaseIndentLegacy, null),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon")
    ]
    public void BitNumericTextFieldShouldRenderCorrectIconWithEnableArrows(BitIconName? iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
            parameters.Add(p => p.ShowArrows, true);
        });

        if (iconName.HasValue)
        {
            var icon = component.FindAll(".bit-icon")[0];
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

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
         DataRow(BitIconName.IncreaseIndentLegacy, null),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon")
    ]
    public void BitNumericTextFieldShouldRenderCorrectIcon(BitIconName? iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
        });

        if (iconName.HasValue)
        {
            var icon = component.FindAll(".bit-icon")[0];
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

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
         DataRow(BitIconName.IncreaseIndentLegacy, null, true),
         DataRow(BitIconName.IncreaseIndentLegacy, null, false),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", true),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectIncrementButton(BitIconName iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.IncrementIconName, iconName);
            parameters.Add(p => p.IncrementAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ShowArrows, true);
        });

        var button = component.Find("button");
        var icon = component.Find("button > span > i");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

        if (string.IsNullOrEmpty(iconAriaLabel) is false)
        {
            Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
        }
    }

    [DataTestMethod,
         DataRow(BitIconName.IncreaseIndentLegacy, null, true),
         DataRow(BitIconName.IncreaseIndentLegacy, null, false),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", true),
         DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectDecrementButton(BitIconName iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.DecrementIconName, iconName);
            parameters.Add(p => p.DecrementAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ShowArrows, true);
        });

        var button = component.Find("button:last-child");
        var icon = component.Find("button:last-child > span > i");

        Assert.IsTrue(icon.ToMarkup().Contains($"bit-icon--{iconName.GetName()}"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

        if (string.IsNullOrEmpty(iconAriaLabel) is false)
        {
            Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
        }
    }

    [DataTestMethod,
         DataRow(" cm"),
         DataRow(" Inch"),
         DataRow(" foot")
    ]
    public void BitNumericTextFieldShouldHaveSuffixWhenItsPropertySet(string suffix)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters => parameters.Add(p => p.Suffix, suffix));

        var input = component.Find("input");
        var inputValue = input.GetAttribute("value");

        Assert.IsTrue(inputValue.Contains(suffix));
    }

    [DataTestMethod,
         DataRow(BitNumericTextFieldLabelPosition.Left),
         DataRow(BitNumericTextFieldLabelPosition.Top)
    ]
    public void BitNumericTextFieldShouldHaveLabelPositionClassName(BitNumericTextFieldLabelPosition labelPosition)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, labelPosition);
        });

        var labelPositionClass = labelPosition == BitNumericTextFieldLabelPosition.Left ? "left" : "top";

        var numericTextFieldButton = component.Find(".bit-ntf");

        Assert.IsTrue(numericTextFieldButton.ClassList.Contains($"label-{labelPositionClass}"));
    }

    [DataTestMethod,
         DataRow("dir", "ltr"),
         DataRow("type", "number")
    ]
    public void BitNumericTextFieldInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
    {
        var inputHtmlAttributes = new Dictionary<string, object> { { attrKey, attrValue } };
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.AriaSetSize, ariaSetSize);
            parameters.Add(p => p.AriaPositionInSet, ariaPositionInSet);
        });

        var ntfWrapper = component.Find(".wrapper");

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
         DataRow(5.3, null),
         DataRow(null, 100.5),
         DataRow(0.0, 100.78),
         DataRow(50.56, 1.12)
    ]
    public void BitNumericTextFieldShouldHaveCorrectMaxMin(double? min, double? max)
    {
        var component = RenderComponent<BitNumericTextField<decimal?>>(parameters =>
        {
            parameters.Add(p => p.Min, (decimal?)min);
            parameters.Add(p => p.Max, (decimal?)max);
        });

        var input = component.Find("input");
        decimal? expectedMinValue = null;
        decimal? expectedMaxValue = null;

        if (max.HasValue)
        {
            expectedMaxValue = (decimal?)max.Value;
        }

        if (min.HasValue)
        {
            expectedMinValue = (decimal?)min.Value;
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.ShowArrows, true);
            parameters.Add(p => p.OnIncrement, () => onIncrementEventCounter++);
        });

        var increaseButton = component.Find("button.arrow-up");
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.ShowArrows, true);
            parameters.Add(p => p.OnDecrement, () => onDecrementEventCounter--);
        });

        var decreaseButton = component.Find("button.arrow-down");
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
         DataRow(4.56, 2),
         DataRow(12.22, 2)
    ]
    public void BitNumericTextFieldOnChangeTest(double inputValue, int precision)
    {
        decimal onChangeEventValue = 0;
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Precision, precision);
            parameters.Add(p => p.OnChange, (decimal value) => onChangeEventValue = value);
        });

        var input = component.Find("input");
        input.Change(inputValue);
        input.Blur();

        Assert.AreEqual(onChangeEventValue, (decimal)inputValue);
    }

    [DataTestMethod,
         DataRow(null),
         DataRow("AriaDescription")
    ]
    public void BitNumericTextFieldShouldHaveCorrectAriaDescription(string ariaDescription)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var input = component.Find("input");

        Assert.AreEqual(ariaDescription, input.GetAttribute("aria-describedby"));
    }

    [DataTestMethod,
         DataRow(3.12, null),
         DataRow(3.68, " cm"),
         DataRow(null, null)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueNow(double? ariaValueNow, string suffix)
    {
        var component = RenderComponent<BitNumericTextField<decimal?>>(parameters =>
        {
            parameters.Add(p => p.AriaValueNow, (decimal?)ariaValueNow);
            parameters.Add(p => p.Suffix, suffix);
        });

        var input = component.Find("input");
        var expectedResult = ariaValueNow.HasValue ? (decimal?)ariaValueNow : suffix.HasNoValue() ? component.Instance.Value : null;
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
         DataRow("3.25", null, 2),
         DataRow(null, " cm", 0),
         DataRow(null, null, 0)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueText(string ariaValueText, string suffix, int precision)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
         DataRow(3.2216, 1.1322, 12.36),
         DataRow(8.2216, 2.1322, 10.36),
         DataRow(8.2216, 1.1322, 8.369),
         DataRow(8.2216, 2.1322, 9.369),
         DataRow(8.2216, 5.1322, 9.369)
    ]
    public void BitNumericTextFieldIncrementButtonClickTest(double defaultValue, double step, double max)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.ShowArrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.arrow-up");
        incrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue + step <= max ? defaultValue + step : defaultValue;

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3.2216, 1.1322, 12.36),
         DataRow(8.2216, 2.1322, 10.36),
         DataRow(8.2216, 1.1322, 8.369),
         DataRow(8.2216, 2.1322, 9.369),
         DataRow(8.2216, 5.1322, 9.369)
    ]
    public void BitNumericTextFieldArrowUpKeyDownTest(double defaultValue, double step, double max)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs
        {
            Key = "ArrowUp"
        };
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue + step <= max ? defaultValue + step : defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(3.369, 1, 0, 3),
         DataRow(2.369, 2, 0, 3),
         DataRow(3.369, 4, 0, 3),
         DataRow(0, 1, 0, 0)
    ]
    public void BitNumericTextFieldDecrementButtonClickTest(double defaultValue, double step, double min, int precision)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Min, (decimal)min);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.Precision, precision);
            parameters.Add(p => p.ShowArrows, true);
        });

        var input = component.Find("input");
        var decrementButton = component.Find("button.arrow-down");
        decrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = Math.Round(defaultValue - step >= min ? defaultValue - step : defaultValue, precision);

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3.369, 1, 0, 3),
         DataRow(2.369, 2, 0, 3),
         DataRow(3.369, 4, 0, 3),
         DataRow(0, 1, 0, 0)
    ]
    public void BitNumericTextFieldArrowDownKeyDownTest(double defaultValue, double step, double min, int precision)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Min, (decimal)min);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.Precision, precision);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs
        {
            Key = "ArrowDown"
        };
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = Math.Round(defaultValue - step >= min ? defaultValue - step : defaultValue, precision);

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(5.36, 0, 100, "25.25", 2),
         DataRow(5.36, 0, 100, "112.369", 2),
         DataRow(5.36, 0, 100, "-5.36", 2),
         DataRow(5, 0, 100, "text123", 2)
    ]
    public void BitNumericTextFieldEnterKeyDownTest(double defaultValue, double min, double max, string userInput, int precision)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.Min, (decimal)min);
            parameters.Add(p => p.Precision, precision);
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
        decimal? expectedResult = 0;
        var isNumber = decimal.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = Normalize(numericValue, precision);
            if (expectedResult > (decimal)max) expectedResult = (decimal)max;
            if (expectedResult < (decimal)min) expectedResult = (decimal)min;
        }
        else
        {
            expectedResult = (decimal)defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5.36, 0, 100, "25.25", 2),
         DataRow(5.36, 0, 100, "112.369", 2),
         DataRow(5.36, 0, 100, "-5.36", 2),
         DataRow(5, 0, 100, "text123", 0)
    ]
    public void BitNumericTextFieldOnBlurTest(double defaultValue, double min, double max, string userInput, int precision)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.Min, (decimal)min);
            parameters.Add(p => p.Precision, precision);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs
        {
            Value = userInput
        };
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        decimal? expectedResult = 0;
        var isNumber = decimal.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = Normalize(numericValue, precision);
            if (expectedResult > (decimal)max) expectedResult = (decimal)max;
            if (expectedResult < (decimal)min) expectedResult = (decimal)min;
        }
        else
        {
            expectedResult = (decimal)defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
     DataRow(0, 100, 1, "25"),
     DataRow(0.65, 100, 0.1, "25.68"),
     DataRow(0, 100.89, 0.25, "12.6"),
     DataRow(0, 10, 0.25, "12.6"),
     DataRow(13, 100, 0.25, "12.6")
    ]
    public void BitNumericTextFieldPrecisionTest(double min, double max, double step, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.Min, (decimal)min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs
        {
            Value = userInput
        };
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        var precision = CalculatePrecision((decimal)step);
        var expectedResult = Normalize(decimal.Parse(userInput), precision);
        if (expectedResult > (decimal)max) expectedResult = (decimal)max;
        if (expectedResult < (decimal)min) expectedResult = (decimal)min;

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5.25, 2, 4, 2),
         DataRow(1.36, 15, 1, 2)
    ]
    public void BitNumericTextFieldTwoWayBoundWithCustomHandlerShouldWorkCorrect(double value, int countOfIncrements, double step, int precision)
    {
        BitNumericTextFieldTwoWayBoundValue = (decimal)value;

        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Value, BitNumericTextFieldTwoWayBoundValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
            parameters.Add(p => p.Precision, precision);
            parameters.Add(p => p.ShowArrows, true);
        });

        var incrementButton = component.Find("button.arrow-up");
        for (var i = 0; i < countOfIncrements; i++)
        {
            incrementButton.PointerDown();
        }

        var expectedValue = value + (step * countOfIncrements);
        var normalizeValue = Normalize((decimal)expectedValue, precision);

        Assert.AreEqual(normalizeValue, BitNumericTextFieldTwoWayBoundValue);
    }

    [DataTestMethod,
        DataRow("<div>This is labelFragment</div>")
    ]
    public void BitNumericTextFieldLabelFragmentTest(string labelFragment)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
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
    public void BitNumericTextFieldContinuousIncrementOnPointerDownTest(double defaultValue, double step, double max, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Max, (decimal)max);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.ShowArrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.arrow-up");
        var initialIncrementCount = timeout / 400;
        var continuousIncrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = (decimal)defaultValue + (decimal)step * (decimal)(initialIncrementCount + continuousIncrementCount);
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")),
            TimeSpan.FromMilliseconds(timeout));
    }

    [Ignore]
    [DataTestMethod,
         DataRow(50, 1, 0, 475),
         DataRow(50, 1, 0, 550)
    ]
    public void BitNumericTextFieldContinuousDecrementOnPointerDownTest(double defaultValue, double step, double min, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Step, (decimal)step);
            parameters.Add(p => p.Min, (decimal)min);
            parameters.Add(p => p.DefaultValue, (decimal)defaultValue);
            parameters.Add(p => p.ShowArrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.arrow-up");
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
         DataRow(6.25),
         DataRow(18),
    ]
    public void BitNumericTextFieldValidationFormTest(double value)
    {
        var component = RenderComponent<BitNumericTextFieldDecimalValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldDecimalTestModel { Value = (decimal?)value });
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
         DataRow(6.25),
         DataRow(18),
    ]
    public void BitNumericTextFieldValidationInvalidHtmlAttributeTest(double value)
    {
        var component = RenderComponent<BitNumericTextFieldDecimalValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldDecimalTestModel { Value = (decimal)value });
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
    public void BitNumericTextFieldValidationInvalidCssClassTest(double value)
    {
        var component = RenderComponent<BitNumericTextFieldDecimalValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldDecimalTestModel { Value = (decimal)value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value < 6 || value > 18;

        var NumericTextField = component.Find(".bit-ntf");

        Assert.IsFalse(NumericTextField.ClassList.Contains("invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, NumericTextField.ClassList.Contains("invalid"));

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

        Assert.AreEqual(!isInvalid, NumericTextField.ClassList.Contains("invalid"));
    }

    private decimal? Normalize(decimal? value, int precision) =>
        value.HasValue ? (decimal?)Math.Round((double)value.Value, precision) : null;

    private int CalculatePrecision(decimal value)
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

    private void HandleValueChanged(decimal value)
        => BitNumericTextFieldTwoWayBoundValue = value;
}
