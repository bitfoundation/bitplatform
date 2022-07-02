using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.NumericTextField.UShort;

[TestClass]
public class BitNumericTextFieldUShortTests : BunitTestContext
{
    private ushort BitNumericTextFieldTwoWayBoundValue;

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
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
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

    [DataTestMethod]
    public void BitNumericTextFieldShouldRenderArrowsWithEnableArrows()
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Arrows, true);
        });

        var arrowButtonHolder = component.FindAll(".bit-ntf-arrows");
        Assert.AreEqual(1, arrowButtonHolder.Count);

        var arrowButtons = component.FindAll(".bit-ntf-arrows button");

        Assert.AreEqual(2, arrowButtons.Count);
    }

    [DataTestMethod,
     DataRow(null, null),
     DataRow(BitIconName.IncreaseIndentLegacy, null),
     DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon")
    ]
    public void BitNumericTextFieldShouldRenderCorrectIconWithEnableArrows(BitIconName? iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IconAriaLabel, iconAriaLabel);
            parameters.Add(p => p.Arrows, true);
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
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
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
    public void BitNumericTextFieldShouldRenderCorrectIncrementButton(BitIconName iconName, string iconAriaLabel,
        bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.IncrementButtonIconName, iconName);
            parameters.Add(p => p.IncrementButtonAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Arrows, true);
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
     DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", true),
     DataRow(BitIconName.IncreaseIndentLegacy, "BitNumericTextFieldButtonIcon", false)
    ]
    public void BitNumericTextFieldShouldRenderCorrectDecrementButton(BitIconName iconName, string iconAriaLabel,
        bool isEnabled)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.DecrementButtonIconName, iconName);
            parameters.Add(p => p.DecrementButtonAriaLabel, iconAriaLabel);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Arrows, true);
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
        var component =
            RenderComponent<BitNumericTextFieldUShortTest>(parameters => parameters.Add(p => p.Suffix, suffix));

        var input = component.Find("input");
        var inputValue = input.GetAttribute("value");

        Assert.IsTrue(inputValue.Contains(suffix));
    }

    [DataTestMethod,
     DataRow(Visual.Fluent, BitNumericTextFieldLabelPosition.Left),
     DataRow(Visual.Fluent, BitNumericTextFieldLabelPosition.Top),
     DataRow(Visual.Cupertino, BitNumericTextFieldLabelPosition.Left),
     DataRow(Visual.Cupertino, BitNumericTextFieldLabelPosition.Top),
     DataRow(Visual.Material, BitNumericTextFieldLabelPosition.Left),
     DataRow(Visual.Material, BitNumericTextFieldLabelPosition.Top),
    ]
    public void BitNumericTextFieldShouldHaveLabelPositionClassName(Visual visual,
        BitNumericTextFieldLabelPosition labelPosition)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.LabelPosition, labelPosition);
        });

        var labelPositionClass = labelPosition == BitNumericTextFieldLabelPosition.Left ? "left" : "top";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        var numericTextFieldButton = component.Find(".bit-ntf");
        Assert.IsTrue(numericTextFieldButton.ClassList.Contains($"bit-ntf-label-{labelPositionClass}-{visualClass}"));
    }

    [DataTestMethod,
     DataRow("dir", "ltr"),
     DataRow("type", "number")
    ]
    public void BitNumericTextFieldInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
    {
        var inputHtmlAttributes = new Dictionary<string, object> { { attrKey, attrValue } };
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
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
    public void BitNumericTextFieldWrapperShouldHaveCorrectAttributes(string title, string ariaLabel, int? ariaSetSize,
        int? ariaPositionInSet)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.AriaSetSize, ariaSetSize);
            parameters.Add(p => p.AriaPositionInSet, ariaPositionInSet);
        });

        var ntfWrapper = component.Find(".bit-ntf-wrapper");

        if (title is not null)
        {
            Assert.AreEqual(title, ntfWrapper.GetAttribute("title"));
        }

        if (ariaLabel is not null)
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
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.Min, (ushort?)min);
            parameters.Add(p => p.Max, (ushort?)max);
        });

        var input = component.Find("input");
        ushort? expectedMinValue = null;
        ushort? expectedMaxValue = null;

        if (max is not null)
        {
            expectedMaxValue = (ushort)max.Value;
        }

        if (min is not null)
        {
            expectedMinValue = (ushort)min.Value;
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
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.Arrows, true);
        });

        var increaseButton = component.FindAll("button")[0];
        for (int i = 0; i < countOfClicks; i++)
        {
            increaseButton.MouseDown();
        }

        Assert.AreEqual(countOfClicks, component.Instance.OnIncrementEventCounter);
    }

    [DataTestMethod,
     DataRow(4),
     DataRow(12)
    ]
    public void BitNumericTextFieldOnDecrementTest(int countOfClicks)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.Arrows, true);
        });

        var decreaseButton = component.FindAll("button")[1];
        var onDecrementEventCounterInitValue = component.Instance.OnDecrementEventCounter;
        for (int i = 0; i < countOfClicks; i++)
        {
            decreaseButton.MouseDown();
        }

        Assert.AreEqual(onDecrementEventCounterInitValue - countOfClicks, component.Instance.OnDecrementEventCounter);
    }

    [DataTestMethod,
     DataRow(4),
     DataRow(12)
    ]
    public void BitNumericTextFieldInputOnBlurEventCallbackTest(int countOfBlur)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>();

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
    public void BitNumericTextFieldInputOnFocusTest(int countOfFocus)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>();

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
    public void BitNumericTextFieldOnChangeTest(int inputValue)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>();

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs();
        changeArgs.Value = (ushort)inputValue;
        input.Change((ushort)inputValue);
        input.Blur();

        Assert.AreEqual((ushort)inputValue, component.Instance.OnChangeEventValue);
    }

    [DataTestMethod,
     DataRow(null),
     DataRow("AriaDescription")
    ]
    public void BitNumericTextFieldShouldHaveCorrectAriaDecription(string ariaDescription)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var input = component.Find("input");

        Assert.AreEqual(ariaDescription, input.GetAttribute("aria-describedby"));
    }

    [DataTestMethod,
     DataRow(3, null),
     DataRow(3, " cm"),
     DataRow(null, null)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueNow(int? ariaValueNow, string suffix)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.AriaValueNow, (ushort?)ariaValueNow);
            parameters.Add(p => p.Suffix, suffix);
        });

        var input = component.Find("input");
        var expectedResult = ariaValueNow is not null ? (ushort?)ariaValueNow :
            suffix.HasNoValue() ? component.Instance.Value : null;
        var attributeValue = input.GetAttribute("aria-valuenow");

        if (expectedResult is null)
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
     DataRow(null, " cm", 0),
     DataRow(null, null, 0)
    ]
    public void BitNumericTextFieldInputShouldHaveCorrectAriaValueText(string ariaValueText, string suffix,
        int precision)
    {
        var component = RenderComponent<BitNumericTextFieldUShortTest>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, ariaValueText);
            parameters.Add(p => p.Suffix, suffix);
            parameters.Add(p => p.Precision, precision);
        });

        var input = component.Find("input");
        var expectedResult = ariaValueText.HasValue() ? ariaValueText :
            suffix.HasValue() ? $"{Normalize(component.Instance.Value, precision)}{suffix}" : null;
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
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Arrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.FindAll("button")[0];
        incrementButton.MouseDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = (ushort)defaultValue + (ushort)step <= (ushort)max
            ? (ushort)defaultValue + (ushort)step
            : (ushort)defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
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
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs();
        args.Key = "ArrowUp";
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = (ushort)defaultValue + (ushort)step <= (ushort)max
            ? (ushort)defaultValue + (ushort)step
            : (ushort)defaultValue;

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
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Min, (ushort)min);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Arrows, true);
        });

        var input = component.Find("input");
        var decrementButton = component.FindAll("button")[1];
        decrementButton.MouseDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = (ushort)defaultValue - (ushort)step >= (ushort)min
            ? (ushort)defaultValue - (ushort)step
            : (ushort)defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
     DataRow(3, 1, 0),
     DataRow(2, 2, 0),
     DataRow(3, 4, 0),
     DataRow(0, 1, 0)
    ]
    public void BitNumericTextFieldArrowDownKeyDownTest(int defaultValue, int step, int min)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Min, (ushort)min);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs();
        args.Key = "ArrowDown";
        input.KeyDown(args);
        var inputValue = input.GetAttribute("value");
        var expectedResult = (ushort)defaultValue - (ushort)step >= (ushort)min
            ? (ushort)defaultValue - (ushort)step
            : (ushort)defaultValue;

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
     DataRow(5, 0, 100, "25"),
     DataRow(5, 0, 100, "112"),
     DataRow(5, 0, 100, "3"),
     DataRow(5, 0, 100, "text123")
    ]
    public void BitNumericTextFieldEnterKeyDownTest(int defaultValue, int min, int max, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.Min, (ushort)min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs();
        changeArgs.Value = userInput;
        input.Change(changeArgs);
        var keyboardArgs = new KeyboardEventArgs();
        keyboardArgs.Key = "Enter";
        input.KeyDown(keyboardArgs);
        var inputValue = component.Instance.Value;
        ushort? expectedResult = 0;
        var isNumber = ushort.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = Normalize(numericValue, 1);
            if (expectedResult > max) expectedResult = (ushort)max;
            if (expectedResult < min) expectedResult = (ushort)min;
        }
        else
        {
            expectedResult = (ushort)defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
     DataRow(5, 0, 100, "25"),
     DataRow(5, 0, 100, "112"),
     DataRow(5, 0, 100, "5"),
     DataRow(5, 0, 100, "text123")
    ]
    public void BitNumericTextFieldOnBlurTest(int defaultValue, int min, int max, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.Min, (ushort)min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs();
        changeArgs.Value = userInput;
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        ushort? expectedResult = 0;
        var isNumber = ushort.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = Normalize(numericValue, 1);
            if (expectedResult > max) expectedResult = (ushort)max;
            if (expectedResult < min) expectedResult = (ushort)min;
        }
        else
        {
            expectedResult = (ushort)defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
     DataRow(0, 100, 1, "25"),
     DataRow(0, 100, 3, "96"),
     DataRow(0, 100, 5, "13"),
     DataRow(0, 10, 1, "12"),
     DataRow(13, 100, 3, "18")
    ]
    public void BitNumericTextFieldPrecisionTest(int min, int max, int step, string userInput)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.Min, (ushort)min);
        });

        var input = component.Find("input");
        var changeArgs = new ChangeEventArgs();
        changeArgs.Value = userInput;
        input.Change(changeArgs);
        input.Blur();
        var inputValue = component.Instance.Value;
        var precision = CalculatePrecision((ushort)step);
        var expectedResult = Normalize(ushort.Parse(userInput), precision);
        if (expectedResult > max) expectedResult = (ushort)max;
        if (expectedResult < min) expectedResult = (ushort)min;

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
     DataRow(5, 2, 4),
     DataRow(1, 15, 1)
    ]
    public void BitNumericTextFieldTwoWayBoundWithCustomHandlerShouldWorkCurrect(int value, int countOfIncrements,
        int step)
    {
        BitNumericTextFieldTwoWayBoundValue = (ushort)value;

        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Value, BitNumericTextFieldTwoWayBoundValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
            parameters.Add(p => p.Arrows, true);
        });

        var incrementButton = component.FindAll("button")[0];
        for (var i = 0; i < countOfIncrements; i++)
        {
            incrementButton.MouseDown();
        }

        var expectedValue = value + (step * countOfIncrements);

        Assert.AreEqual((ushort)expectedValue, BitNumericTextFieldTwoWayBoundValue);
    }

    [DataTestMethod,
     DataRow("<div>This is labelFragment</div>")
    ]
    public void BitNumericTextFieldLabelFragmentTest(string labelFragment)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.LabelFragment, labelFragment);
        });

        var ntfLabelChild = component.Find("label").ChildNodes;
        ntfLabelChild.MarkupMatches(labelFragment);
    }

    [DataTestMethod,
     DataRow(3, 1, 100, 475),
     DataRow(3, 1, 100, 550)
    ]
    public void BitNumericTextFieldContinuousIncrementOnMouseDownTest(int defaultValue, int step, int max, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Max, (ushort)max);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Arrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.FindAll("button")[0];
        var initialIncrementCount = timeout / 400;
        var continuousIncrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = (ushort)defaultValue +
                             (ushort)step * (ushort)(initialIncrementCount + continuousIncrementCount);
        incrementButton.MouseDown();

        component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")),
            TimeSpan.FromMilliseconds(timeout));
    }

    [DataTestMethod,
     DataRow(50, 1, 0, 475),
     DataRow(50, 1, 0, 550)
    ]
    public void BitNumericTextFieldContinuousDecrementOnMouseDownTest(int defaultValue, int step, int min, int timeout)
    {
        var component = RenderComponent<BitNumericTextField<ushort>>(parameters =>
        {
            parameters.Add(p => p.Step, (ushort)step);
            parameters.Add(p => p.Min, (ushort)min);
            parameters.Add(p => p.DefaultValue, (ushort)defaultValue);
            parameters.Add(p => p.Arrows, true);
        });

        var input = component.Find("input");
        var incrementButton = component.FindAll("button")[1];
        var initialDecrementCount = timeout / 400;
        var continuousDecrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = (ushort)defaultValue -
                             (ushort)step * (ushort)(initialDecrementCount + continuousDecrementCount);
        incrementButton.MouseDown();

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
        var component = RenderComponent<BitNumericTextFieldUShortValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldUShortTestModel { Value = (ushort)value });
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
            input.Change((ushort)2);
        }
        else
        {
            input.Change((ushort)8);
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
    public void BitNumericTextFieldValidationInvalidHtmlAttributeTest(int value)
    {
        var component = RenderComponent<BitNumericTextFieldUShortValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldUShortTestModel { Value = (ushort)value });
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
            input.Change((ushort)10);
            input.Blur();
            Assert.IsFalse(input.HasAttribute("aria-invalid"));
        }
        else
        {
            input.Change((ushort)4);
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
    public void BitNumericTextFieldValidationInvalidCssClassTest(Visual visual, int value)
    {
        var component = RenderComponent<BitNumericTextFieldUShortValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumericTextFieldUShortTestModel { Value = (ushort)value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Visual, visual);
        });

        var isInvalid = value < 6 || value > 18;

        var NumericTextField = component.Find(".bit-ntf");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsFalse(NumericTextField.ClassList.Contains($"bit-ntf-invalid-{visualClass}"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(NumericTextField.ClassList.Contains($"bit-ntf-invalid-{visualClass}"), isInvalid);

        var input = component.Find("input");
        if (isInvalid)
        {
            input.Change((ushort)10);
        }
        else
        {
            input.Change((ushort)3);
        }

        input.Blur();

        Assert.AreEqual(NumericTextField.ClassList.Contains($"bit-ntf-invalid-{visualClass}"), !isInvalid);
    }

    private ushort? Normalize(ushort? value, int precision) =>
        value is null ? null : (ushort?)Math.Round((double)value.Value, precision);

    private int CalculatePrecision(ushort value)
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

    private void HandleValueChanged(ushort value)
    {
        BitNumericTextFieldTwoWayBoundValue = value;
    }
}
