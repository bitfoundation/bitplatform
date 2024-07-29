using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

[TestClass]
public class BitNumberFieldTests : BunitTestContext
{
    private int BitNumberFieldTwoWayBoundValue;

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod, DataRow("The placeholder")]
    public void BitNumberFieldShouldHaveCorrectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var numericTextFieldPlaceholder = component.Find(".bit-nfl-inp");

        Assert.IsTrue(numericTextFieldPlaceholder.HasAttribute("placeholder"));
        Assert.AreEqual(numericTextFieldPlaceholder.GetAttribute("placeholder"), placeholder);
    }

    [DataTestMethod,
         DataRow(null),
         DataRow("The Label")
    ]
    public void BitNumberFieldShouldHaveCorrectLabel(string label)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
    public void BitNumberFieldShouldRenderCorrectlyWithArrows(bool arrows)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, arrows);
        });

        var arrowButtonHolder = component.FindAll(".bit-nfl-act");
        var arrowButtons = component.FindAll(".bit-nfl-act button");

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
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon")
    ]
    public void BitNumberFieldShouldRenderCorrectIconWithEnableArrows(string iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon")
    ]
    public void BitNumberFieldShouldRenderCorrectIcon(string iconName, string iconAriaLabel)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon", true),
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon", false)
    ]
    public void BitNumberFieldShouldRenderCorrectIncrementButton(string iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon", true),
         DataRow("IncreaseIndentLegacy", "BitNumberFieldButtonIcon", false)
    ]
    public void BitNumberFieldShouldRenderCorrectDecrementButton(string iconName, string iconAriaLabel, bool isEnabled)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
         DataRow("0", 11),
         DataRow("C2", 100),
         DataRow("0:00000", 1363)
    ]
    public void BitNumberFieldShouldHaveNumberFormaWhenItsPropertySet(string numberFormat, int defaultValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.NumberFormat, numberFormat);
        });

        var input = component.Find("input");
        var inputValue = input.GetAttribute("value");
        var expectedValue = defaultValue.ToString(numberFormat);

        Assert.AreEqual(expectedValue, inputValue);
    }

    [DataTestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public void BitNumberFieldShouldHaveLabelPositionClassName(bool inlineLabel)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.InlineLabel, inlineLabel);
        });

        var lblClass = inlineLabel ? "ilb" : "tlb";

        var numberFieldButton = component.Find(".bit-nfl");

        Assert.IsTrue(numberFieldButton.ClassList.Contains($"bit-nfl-{lblClass}"));
    }

    [DataTestMethod,
         DataRow("dir", "ltr"),
         DataRow("foo", "bar")
    ]
    public void BitNumberFieldInputShouldHaveHtmlAttributes(string attrKey, string attrValue)
    {
        var inputHtmlAttributes = new Dictionary<string, object> { { attrKey, attrValue } };
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
    public void BitNumberFieldWrapperShouldHaveCorrectAttributes(string title, string ariaLabel, int? ariaSetSize, int? ariaPositionInSet)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.AriaSetSize, ariaSetSize);
            parameters.Add(p => p.AriaPositionInSet, ariaPositionInSet);
        });

        var ntfWrapper = component.Find(".bit-nfl-wrp");

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
         DataRow("0", null),
         DataRow("10", null),
         DataRow(null, "0"),
         DataRow(null, "10"),
         DataRow("0", "10"),
         DataRow("-10", "0"),
         DataRow("10", "0"),
         DataRow("0", "-10"),
    ]
    public void BitNumberFieldShouldHaveCorrectMaxMin(string min, string max)
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.Max, max);
        });

        var input = component.Find("input");
        int? expectedMinValue = int.MinValue;
        int? expectedMaxValue = int.MaxValue;

        if (max is not null)
        {
            expectedMaxValue = int.Parse(max);
        }

        if (min is not null)
        {
            expectedMinValue = int.Parse(min);
        }

        Assert.AreEqual(expectedMinValue.HasValue ? expectedMinValue.ToString() : null, input.GetAttribute("aria-valuemin"));
        Assert.AreEqual(expectedMaxValue.HasValue ? expectedMaxValue.ToString() : null, input.GetAttribute("aria-valuemax"));
    }

    [DataTestMethod,
         DataRow(3),
         DataRow(5)
    ]
    public async Task BitNumberFieldOnIncrementTest(int countOfClicks)
    {
        var onIncrementEventCounter = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, true);
            parameters.Add(p => p.OnIncrement, () => onIncrementEventCounter++);
        });

        var increaseButton = component.Find("button.bit-nfl-aup");
        for (var i = 0; i < countOfClicks; i++)
        {
            increaseButton.PointerDown();
            await Task.Delay(1);
            increaseButton.PointerUp();
        }

        Assert.AreEqual(countOfClicks, onIncrementEventCounter);
    }

    [DataTestMethod,
         DataRow(3),
         DataRow(5)
    ]
    public async Task BitNumberFieldOnDecrementTest(int countOfClicks)
    {
        var onDecrementEventCounter = 20;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ShowButtons, true);
            parameters.Add(p => p.OnDecrement, () => onDecrementEventCounter--);
        });

        var decreaseButton = component.Find("button.bit-nfl-adn");
        for (var i = 0; i < countOfClicks; i++)
        {
            decreaseButton.PointerDown();
            await Task.Delay(1);
            decreaseButton.PointerUp();
        }

        Assert.AreEqual(20 - countOfClicks, onDecrementEventCounter);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumberFieldInputOnBlurEventCallbackTest(int countOfBlur)
    {
        var onBlurEventCounter = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.OnBlur, () => onBlurEventCounter++);
        });

        var input = component.Find("input");
        for (var i = 0; i < countOfBlur; i++)
        {
            input.Blur();
        }

        Assert.AreEqual(onBlurEventCounter, countOfBlur);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumberFieldInputOnFocusTest(int countOfFocus)
    {
        var onFocusEventCounter = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => onFocusEventCounter++);
        });

        var input = component.Find("input");
        for (var i = 0; i < countOfFocus; i++)
        {
            input.Focus();
        }

        Assert.AreEqual(onFocusEventCounter, countOfFocus);
    }

    [DataTestMethod,
         DataRow(4),
         DataRow(12)
    ]
    public void BitNumberFieldOnChangeTest(int inputValue)
    {
        var onChangeEventValue = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.OnChange, (int value) => onChangeEventValue = value);
        });

        var input = component.Find("input");
        input.Change(inputValue);

        Assert.AreEqual(onChangeEventValue, inputValue);
    }

    [DataTestMethod,
         DataRow(null),
         DataRow("AriaDescription")
    ]
    public void BitNumberFieldShouldHaveCorrectAriaDescription(string ariaDescription)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
    public void BitNumberFieldInputShouldHaveCorrectAriaValueNow(int? ariaValueNow, string numberFormat)
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
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
         DataRow("3", null),
         DataRow(null, "00"),
         DataRow(null, null)
    ]
    public void BitNumberFieldInputShouldHaveCorrectAriaValueText(string ariaValueText, string numberFormat)
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.AriaValueText, ariaValueText);
            if (numberFormat.HasValue())
            {
                parameters.Add(p => p.NumberFormat, numberFormat);
            }
        });

        var input = component.Find("input");
        var expectedResult = ariaValueText.HasValue()
            ? ariaValueText
            : numberFormat.HasValue()
                ? component.Instance.Value.ToString(numberFormat)
                : component.Instance.Value.ToString();
        Assert.AreEqual(expectedResult, input.GetAttribute("aria-valuetext"));
    }

    [DataTestMethod,
         DataRow(3, "1", "12"),
         DataRow(8, "2", "10"),
         DataRow(8, "1", "8"),
         DataRow(8, "2", "9"),
         DataRow(8, "5", "9")
    ]
    public void BitNumberFieldIncrementButtonClickTest(int defaultValue, string step, string max)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-nfl-aup");
        incrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue + int.Parse(step) <= int.Parse(max)
            ? defaultValue + int.Parse(step)
            : int.Parse(max);

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3, "1", "12"),
         DataRow(8, "2", "10"),
         DataRow(8, "1", "8"),
         DataRow(8, "2", "9"),
         DataRow(8, "5", "9")
    ]
    public void BitNumberFieldArrowUpKeyDownTest(int defaultValue, string step, string max)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
        var expectedResult = defaultValue + int.Parse(step) <= int.Parse(max)
            ? defaultValue + int.Parse(step)
            : int.Parse(max);

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(3, "1", "0"),
         DataRow(2, "2", "0"),
         DataRow(3, "4", "0"),
         DataRow(0, "1", "0")
    ]
    public void BitNumberFieldDecrementButtonClickTest(int defaultValue, string step, string min)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var decrementButton = component.Find("button.bit-nfl-adn");
        decrementButton.PointerDown();
        var inputValue = input.GetAttribute("value");
        var expectedResult = defaultValue - int.Parse(step) >= int.Parse(min)
            ? defaultValue - int.Parse(step)
            : int.Parse(min);

        Assert.AreEqual(inputValue, expectedResult.ToString());
    }

    [DataTestMethod,
         DataRow(3, "1", "0"),
         DataRow(2, "2", "0"),
         DataRow(3, "4", "0"),
         DataRow(0, "1", "0")
    ]
    public void BitNumberFieldArrowDownKeyDownTest(int defaultValue, string step, string min)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
        var expectedResult = defaultValue - int.Parse(step) >= int.Parse(min)
            ? defaultValue - int.Parse(step)
            : int.Parse(min);

        Assert.AreEqual(expectedResult.ToString(), inputValue);
    }

    [DataTestMethod,
         DataRow(50.02, "0", "100", "25"),
         DataRow(50.02, "0", "100", "112.2"),
         DataRow(50.02, "0", "100", "62.72"),
         DataRow(50.02, "0", "100", "-5"),
         DataRow(50.02, "0", "100", "text123")
    ]
    public void BitNumberFieldEnterKeyDownTest(double defaultValue, string min, string max, string userInput)
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
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
            expectedResult = numericValue;
            if (expectedResult > int.Parse(max)) expectedResult = int.Parse(max);
            if (expectedResult < int.Parse(min)) expectedResult = int.Parse(min);
        }
        else
        {
            expectedResult = defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5, "0", "100", "25"),
         DataRow(5, "0", "100", "112"),
         DataRow(5, "0", "100", "-5"),
         DataRow(5, "-100", "0", "-25"),
         DataRow(5, "-100", "0", "-112"),
         DataRow(5, "-100", "0", "5"),
         DataRow(5, "10", "20", "text123")
    ]
    public void BitNumberFieldOnBlurTest(double defaultValue, string min, string max, string userInput)
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
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
        var inputValue = component.Instance.Value;
        double? expectedResult = 0;
        var isNumber = double.TryParse(userInput, out var numericValue);
        if (isNumber)
        {
            expectedResult = numericValue;
            if (expectedResult > int.Parse(max)) expectedResult = int.Parse(max);
            if (expectedResult < int.Parse(min)) expectedResult = int.Parse(min);
        }
        else
        {
            expectedResult = defaultValue;
        }

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow("0", "100", "1", "25"),
         DataRow("0", "100", "2", "25"),
         DataRow("0", "100", "25", "12"),
         DataRow("0", "10", "52", "12"),
         DataRow("13", "100", "523", "12")
    ]
    public void BitNumberFieldPrecisionTest(string min, string max, string step, string userInput)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
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
        var inputValue = component.Instance.Value;
        var expectedResult = int.Parse(userInput);
        if (expectedResult > int.Parse(max)) expectedResult = int.Parse(max);
        if (expectedResult < int.Parse(min)) expectedResult = int.Parse(min);

        Assert.AreEqual(expectedResult, inputValue);
    }

    [DataTestMethod,
         DataRow(5, 2, "4"),
         DataRow(1, 15, "1")
    ]
    public async Task BitNumberFieldTwoWayBoundWithCustomHandlerShouldWorkCorrect(int value, int countOfIncrements, string step)
    {
        BitNumberFieldTwoWayBoundValue = value;

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Value, BitNumberFieldTwoWayBoundValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
            parameters.Add(p => p.ShowButtons, true);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        for (var i = 0; i < countOfIncrements; i++)
        {
            incrementButton.PointerDown();

            await Task.Delay(1);
        }

        var expectedValue = value + int.Parse(step) * countOfIncrements;

        Assert.AreEqual(expectedValue, BitNumberFieldTwoWayBoundValue);
    }

    [DataTestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitNumberFieldLabelFragmentTest(string labelFragment)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelFragment);
        });

        var ntfLabelChild = component.Find("label").ChildNodes;
        ntfLabelChild.MarkupMatches(labelFragment);
    }

    [Ignore]
    [DataTestMethod,
         DataRow(3, "1", "100", 475),
         DataRow(3, "1", "100", 550)
    ]
    public void BitNumberFieldContinuousIncrementOnPointerDownTest(int defaultValue, string step, string max, int timeout)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-nfl-aup");
        var initialIncrementCount = timeout / 400;
        var continuousIncrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = defaultValue + int.Parse(step) * (initialIncrementCount + continuousIncrementCount);
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(expectedResult.ToString(), input.GetAttribute("value")),
            TimeSpan.FromMilliseconds(timeout));
    }

    [Ignore]
    [DataTestMethod,
         DataRow(50, "1", "0", 475),
         DataRow(50, "1", "0", 550)
    ]
    public void BitNumberFieldContinuousDecrementOnPointerDownTest(int defaultValue, string step, string min, int timeout)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Min, min);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.ShowButtons, true);
        });

        var input = component.Find("input");
        var incrementButton = component.Find("button.bit-nfl-aup");
        var initialDecrementCount = timeout / 400;
        var continuousDecrementCount = timeout >= 400 ? (timeout - 400) / 75 : 0;
        var expectedResult = defaultValue - int.Parse(step) * (initialDecrementCount + continuousDecrementCount);
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
    public void BitNumberFieldValidationFormTest(int value)
    {
        var component = RenderComponent<BitNumberFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumberFieldTestModel { Value = value });
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
    public void BitNumberFieldValidationInvalidHtmlAttributeTest(int value)
    {
        var component = RenderComponent<BitNumberFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumberFieldTestModel { Value = value });
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
            Assert.IsFalse(input.HasAttribute("aria-invalid"));
        }
        else
        {
            input.Change(4);
            Assert.IsTrue(input.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
         DataRow(2),
         DataRow(8)
    ]
    public void BitNumberFieldValidationInvalidCssClassTest(int value)
    {
        var component = RenderComponent<BitNumberFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumberFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value < 6 || value > 18;

        var numberField = component.Find(".bit-nfl");

        Assert.IsFalse(numberField.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, numberField.ClassList.Contains("bit-inv"));

        var input = component.Find("input");

        if (isInvalid)
        {
            input.Change(10);
        }
        else
        {
            input.Change(3);
        }

        Assert.AreEqual(!isInvalid, numberField.ClassList.Contains("bit-inv"));
    }

    private void HandleValueChanged(int value)
        => BitNumberFieldTwoWayBoundValue = value;
}
