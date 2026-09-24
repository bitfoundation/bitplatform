using System;
using System.Globalization;
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

    [TestMethod, DataRow("The placeholder")]
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

    [TestMethod,
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
            Assert.Throws<ElementNotFoundException>(() => component.Find("label"));
        }
        else
        {
            var labelTag = component.Find("label");
            Assert.AreEqual(label, labelTag.InnerHtml);
        }
    }

    [TestMethod,
         DataRow(null),
         DataRow(BitSpinButtonMode.Compact),
         DataRow(BitSpinButtonMode.Inline),
         DataRow(BitSpinButtonMode.Spread)
    ]
    public void BitNumberFieldShouldRenderCorrectlyWithArrows(BitSpinButtonMode? mode)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, mode);
        });

        var arrowButtons = component.FindAll("button");

        if (mode.HasValue)
        {
            Assert.AreEqual(2, arrowButtons.Count);
        }
        else
        {
            Assert.AreEqual(0, arrowButtons.Count);
        }
    }

    [TestMethod,
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
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
        });

        if (iconName.HasValue())
        {
            var icon = component.FindAll(".bit-nfl-lic")[0];
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

    [TestMethod,
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

    [TestMethod,
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
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
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

    [TestMethod,
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
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
        });

        var button = component.Find("button:last-child");
        var icon = component.Find("button:last-child > span > i");

        Assert.IsTrue(icon.OuterHtml.Contains($"bit-icon--{iconName}"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
        Assert.AreEqual(!isEnabled, button.HasAttribute("aria-disabled"));

        if (string.IsNullOrEmpty(iconAriaLabel) is false)
        {
            Assert.AreEqual(iconAriaLabel, button.GetAttribute("aria-label"));
        }
    }

    [TestMethod,
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

    [TestMethod,
         DataRow(null),
         DataRow(BitLabelPosition.Start),
         DataRow(BitLabelPosition.End),
         DataRow(BitLabelPosition.Top),
         DataRow(BitLabelPosition.Bottom)
    ]
    public void BitNumberFieldShouldHaveLabelPositionClassName(BitLabelPosition? labelPosition)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, labelPosition);
        });

        var lblClass = labelPosition switch
        {
            BitLabelPosition.Bottom => "bit-nfl-lbt",
            BitLabelPosition.Start => "bit-nfl-lst",
            BitLabelPosition.End => "bit-nfl-led",
            _ => "bit-nfl-ltp"
        };

        var numberFieldButton = component.Find(".bit-nfl");

        Assert.IsTrue(numberFieldButton.ClassList.Contains(lblClass));
    }

    [TestMethod,
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

    [TestMethod,
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

        var ntfWrapper = component.Find(".bit-nfl-cnt");
        var input = component.Find("input");

        if (string.IsNullOrEmpty(title) is false)
        {
            Assert.AreEqual(title, ntfWrapper.GetAttribute("title"));
        }

        // The wrapper is a plain box with no role of its own, which makes every aria-* attribute on it
        // invalid and ignored; the name and the set position belong on the element carrying the
        // spinbutton role, and naming the wrapper as well would have the field read out twice.
        Assert.IsNull(ntfWrapper.GetAttribute("aria-label"));
        Assert.IsNull(ntfWrapper.GetAttribute("aria-setsize"));
        Assert.IsNull(ntfWrapper.GetAttribute("aria-posinset"));

        if (string.IsNullOrEmpty(ariaLabel) is false)
        {
            Assert.AreEqual(ariaLabel, input.GetAttribute("aria-label"));
        }

        if (ariaSetSize is not null)
        {
            Assert.AreEqual(ariaSetSize.ToString(), input.GetAttribute("aria-setsize"));
            Assert.AreEqual(ariaPositionInSet.ToString(), input.GetAttribute("aria-posinset"));
        }
    }

    [TestMethod,
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

        // A misconfigured Min greater than Max describes the same (swapped) range to the clamping, so
        // the announced bounds are the ordered ones - aria-valuemin above aria-valuemax would be a
        // contradiction for assistive technologies and would not match what the field enforces.
        var lower = min is null ? null : (max is null ? int.Parse(min) : Math.Min(int.Parse(min), int.Parse(max))).ToString();
        var upper = max is null ? null : (min is null ? int.Parse(max) : Math.Max(int.Parse(min), int.Parse(max))).ToString();

        // The aria-valuemin/max attributes only render when an explicit Min/Max is provided;
        // announcing the underlying type's extremes (e.g. -2147483648) would just be noise.
        Assert.AreEqual(lower, input.GetAttribute("aria-valuemin"));
        Assert.AreEqual(upper, input.GetAttribute("aria-valuemax"));
    }

    [TestMethod,
         DataRow(3),
         DataRow(5)
    ]
    public void BitNumberFieldOnIncrementTest(int countOfClicks)
    {
        var onIncrementEventCounter = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            // A long delay keeps the press-and-hold spin out of the picture, so each press contributes
            // exactly one increment no matter how long the test itself takes.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Add(p => p.OnIncrement, () => onIncrementEventCounter++);
        });

        var increaseButton = component.Find("button.bit-nfl-aup");
        for (var i = 0; i < countOfClicks; i++)
        {
            increaseButton.PointerDown();
            increaseButton.PointerUp();
        }

        Assert.AreEqual(countOfClicks, onIncrementEventCounter);
        Assert.AreEqual(countOfClicks, component.Instance.Value);
    }

    [TestMethod,
         DataRow(3),
         DataRow(5)
    ]
    public void BitNumberFieldOnDecrementTest(int countOfClicks)
    {
        var onDecrementEventCounter = 20;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Add(p => p.OnDecrement, () => onDecrementEventCounter--);
        });

        var decreaseButton = component.Find("button.bit-nfl-adn");
        for (var i = 0; i < countOfClicks; i++)
        {
            decreaseButton.PointerDown();
            decreaseButton.PointerUp();
        }

        Assert.AreEqual(20 - countOfClicks, onDecrementEventCounter);
        Assert.AreEqual(-countOfClicks, component.Instance.Value);
    }

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
         DataRow("۱۲۳", 123),   // Persian / Extended Arabic-Indic digits (U+06F0-U+06F9)
         DataRow("١٢٣", 123),   // Arabic-Indic digits (U+0660-U+0669)
         DataRow("۴۵۶", 456),
         DataRow("123", 123)    // Latin digits remain unchanged
    ]
    public void BitNumberFieldShouldNormalizeNonLatinDigitsWhenEnabled(string userInput, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod,
         DataRow("۱۲٫۵", 12.5),   // Persian digits with Arabic decimal separator (U+066B)
         DataRow("٣٫٢٥", 3.25)     // Arabic-Indic digits with Arabic decimal separator
    ]
    public void BitNumberFieldShouldNormalizeNonLatinDecimalsWhenEnabled(string userInput, double expectedValue)
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Precision, 2);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod, DataRow("۱۲۳")]
    public void BitNumberFieldShouldNotNormalizeNonLatinDigitsWhenDisabled(string userInput)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, false);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        // Parsing fails for non-Latin digits, so the value remains the default.
        Assert.AreEqual(0, component.Instance.Value);
    }

    [TestMethod,
         DataRow("۱۲۳", "۱۲۳"),   // Persian / Extended Arabic-Indic digits (U+06F0-U+06F9)
         DataRow("١٢٣", "١٢٣")    // Arabic-Indic digits (U+0660-U+0669)
    ]
    public void BitNumberFieldShouldPreserveOriginalTextInDisplayWhenNormalizeDigitsEnabled(string userInput, string expectedDisplay)
    {
        // The bound .NET value is the normalized Latin number while the input keeps showing the
        // exact characters the user typed, avoiding a jarring visible conversion of the digits.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(123, component.Instance.Value);
        Assert.AreEqual(expectedDisplay, component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldShouldShowFormattedValueWhenNumberFormatIsSetWithNormalizeDigits()
    {
        // When NumberFormat is set the formatted string takes precedence over the preserved
        // user-typed digits, so the input shows the formatted value rather than the raw input.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.NumberFormat, "000");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۱۲۳" });

        Assert.AreEqual(123, component.Instance.Value);
        Assert.AreEqual("123", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldShouldRevertDisplayToLatinAfterChangeValueWhenNormalizeDigitsEnabled()
    {
        // After spinning the value, the preserved user-typed digits are discarded and the regular
        // (Latin) formatted value is shown again.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Step, "1");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۱۲۳" });

        // Preconditions: the Persian digits are preserved in the display.
        Assert.AreEqual("۱۲۳", component.Find("input").GetAttribute("value"));

        // Increment via the up arrow key triggers ChangeValue, which clears the preserved text.
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(124, component.Instance.Value);
        Assert.AreEqual("124", component.Find("input").GetAttribute("value"));
    }

    [TestMethod,
         DataRow("۱٬۲۳۴", 1234),   // Persian digits with the Arabic thousands separator (U+066C)
         DataRow("١٬٢٣٤", 1234)     // Arabic-Indic digits with the Arabic thousands separator
    ]
    public void BitNumberFieldShouldStripArabicThousandsSeparatorWhenNormalizeDigitsEnabled(string userInput, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod,
         DataRow("۱٬۲۳۴", 1234),   // Persian digits with a Persian thousands separator and stripping
         DataRow("1 000", 1000)     // Latin digits with a space group separator
    ]
    public void BitNumberFieldShouldUseCustomDigitsNormalizerWhenProvided(string userInput, int expectedValue)
    {
        static string? normalizer(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (c is ' ' or ',' or '٬') continue;
                var digit = System.Globalization.CharUnicodeInfo.GetDecimalDigitValue(c);
                sb.Append(digit >= 0 ? (char)('0' + digit) : c);
            }
            return sb.ToString();
        }

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DigitsNormalizer, normalizer);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod, DataRow("۹۹")]
    public void BitNumberFieldCustomDigitsNormalizerShouldTakePrecedenceOverNormalizeDigits(string userInput)
    {
        // The custom normalizer forces a constant value, proving it overrides the built-in NormalizeDigits.
        static string? normalizer(string? value) => "42";

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.DigitsNormalizer, normalizer);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(42, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldDiscardPreservedDisplayTextWhenValueChangesFromParent()
    {
        // Repro for the stale-display bug: after the user types non-Latin digits (which are preserved
        // in the input), a parent that resets and then reloads the same numeric value must not cause
        // the old user-typed text to reappear.
        var boundValue = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Value, boundValue);
            parameters.Add(p => p.ValueChanged, (int v) => boundValue = v);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۱۲۳" });

        // The typed Persian digits are preserved while the bound value is the normalized Latin number.
        Assert.AreEqual(123, component.Instance.Value);
        Assert.AreEqual("۱۲۳", component.Find("input").GetAttribute("value"));

        // The parent resets the bound value to 0.
        component.Render(parameters => parameters.Add(p => p.Value, 0));
        Assert.AreEqual("0", component.Find("input").GetAttribute("value"));

        // The parent then loads 123 again; the stale Persian text must NOT reappear.
        component.Render(parameters => parameters.Add(p => p.Value, 123));
        Assert.AreEqual("123", component.Find("input").GetAttribute("value"));
    }

    [TestMethod,
         DataRow("5", 10),    // below the normalized min (۱۰) -> clamped up to 10
         DataRow("25", 20),   // above the normalized max (۲۰) -> clamped down to 20
         DataRow("15", 15)    // within range -> unchanged
    ]
    public void BitNumberFieldShouldNormalizeNonLatinMinAndMaxWhenNormalizeDigitsEnabled(string userInput, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Min, "۱۰");
            parameters.Add(p => p.Max, "۲۰");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = userInput });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNormalizeNonLatinStepWhenNormalizeDigitsEnabled()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Step, "۵");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "10" });
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        // The step "۵" is normalized to 5, so incrementing 10 yields 15.
        Assert.AreEqual(15, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNormalizeMinMaxUsingCustomDigitsNormalizer()
    {
        // The custom normalizer maps any Unicode decimal digit to Latin and is applied to the
        // Min/Max parameters too, not just user input.
        static string? normalizer(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                var digit = System.Globalization.CharUnicodeInfo.GetDecimalDigitValue(c);
                sb.Append(digit >= 0 ? (char)('0' + digit) : c);
            }
            return sb.ToString();
        }

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DigitsNormalizer, normalizer);
            parameters.Add(p => p.Min, "۵");
            parameters.Add(p => p.Max, "۵۰");
        });

        var input = component.Find("input");

        input.Change(new ChangeEventArgs { Value = "100" });
        Assert.AreEqual(50, component.Instance.Value);   // clamped to the normalized max (50)

        input.Change(new ChangeEventArgs { Value = "1" });
        Assert.AreEqual(5, component.Instance.Value);    // clamped to the normalized min (5)
    }

    [TestMethod]
    public void BitNumberFieldShouldNormalizeMinRegardlessOfParameterOrder()
    {
        // Min is provided before NormalizeDigits to ensure normalization is still applied even if the
        // Min CallOnSet handler runs before NormalizeDigits has been assigned.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "۱۰");
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "5" });

        Assert.AreEqual(10, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldShowCanonicalValueWhenCustomNormalizerIsNotDigitEquivalent()
    {
        // Repro for the value/display divergence: a custom normalizer that maps the typed text to a
        // different number (here a constant "42") must NOT leave the original "۹۹" visible while
        // binding 42. The display has to reflect the bound value.
        static string? normalizer(string? value) => "42";

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DigitsNormalizer, normalizer);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۹۹" });

        Assert.AreEqual(42, component.Instance.Value);
        Assert.AreEqual("42", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldShouldShowCanonicalValueWhenNormalizerStripsNonDigitContent()
    {
        // A normalizer that strips units/symbols (here non-digit characters) is not a pure
        // digit-equivalent transformation, so the canonical value must be displayed, not the raw text.
        static string? normalizer(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (char.IsDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DigitsNormalizer, normalizer);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "12kg" });

        Assert.AreEqual(12, component.Instance.Value);
        Assert.AreEqual("12", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldAriaValueTextShouldMatchPreservedDisplayWhenNormalizeDigitsEnabled()
    {
        // When the typed non-Latin digits are preserved in the input, the aria-valuetext must match
        // the visible text so a screen reader announces what the user sees (not the Latin form).
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۱۲۳" });

        var refreshed = component.Find("input");
        Assert.AreEqual("۱۲۳", refreshed.GetAttribute("value"));
        Assert.AreEqual("۱۲۳", refreshed.GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void BitNumberFieldShouldNotClearNullableValueWhenInputNormalizesToEmpty()
    {
        // A lone Arabic thousands separator (U+066C) normalizes to an empty string. For a nullable
        // type this must be treated as an invalid (unparsable) entry rather than silently clearing
        // the existing value.
        var boundValue = (int?)5;
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Value, boundValue);
            parameters.Add(p => p.ValueChanged, (int? v) => boundValue = v);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "٬" });

        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldDerivePrecisionFromNormalizedStep()
    {
        // Step "۰٫۱" normalizes to 0.1 -> precision of 1 decimal place. If precision were not
        // recomputed after normalization it would stay 0 and round 1.23 to 1 instead of 1.2.
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Step, "۰٫۱");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "۱٫۲۳" });

        Assert.AreEqual(1.2, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldResetNormalizedMinWhenNormalizationDisabled()
    {
        // While normalization is enabled, Min "۱۰" parses to 10 and clamps 5 up to 10.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Min, "۱۰");
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "5" });
        Assert.AreEqual(10, component.Instance.Value);

        // Disabling normalization (with the same non-Latin Min) must drop the previously parsed Min,
        // since "۱۰" no longer parses, falling back to the type minimum (no clamping).
        component.Render(parameters => parameters.Add(p => p.NormalizeDigits, false));
        component.Find("input").Change(new ChangeEventArgs { Value = "5" });
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNormalizeSupplementaryPlaneDigits()
    {
        // Mathematical Bold Digits (U+1D7CE..U+1D7D7) live in the Unicode supplementary plane and are
        // encoded as surrogate pairs, so they exercise the surrogate-aware normalization path.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "\U0001D7CF\U0001D7D0\U0001D7D1" }); // bold 1, 2, 3

        Assert.AreEqual(123, component.Instance.Value);
    }

    [TestMethod,
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

        // aria-describedby is an id reference, so the description text is rendered into a visually
        // hidden element of its own and the input points at that element.
        // The always-present live region shares the visually hidden class, so the description is the one
        // carrying an id.
        if (ariaDescription is null)
        {
            Assert.IsNull(input.GetAttribute("aria-describedby"));
            Assert.AreEqual(0, component.FindAll(".bit-nfl-dsc[id]").Count);
        }
        else
        {
            var describedById = input.GetAttribute("aria-describedby");
            Assert.IsFalse(string.IsNullOrEmpty(describedById));

            var description = component.Find(".bit-nfl-dsc[id]");
            Assert.AreEqual(describedById, description.Id);
            Assert.AreEqual(ariaDescription, description.TextContent);
        }
    }

    [TestMethod,
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

    [TestMethod,
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

        // aria-valuetext only renders when it adds information over the plain aria-valuenow number:
        // either an explicitly provided text or a formatted display that differs from the raw value.
        var expectedResult = ariaValueText.HasValue()
            ? ariaValueText
            : numberFormat.HasValue()
                ? component.Instance.Value.ToString(numberFormat)
                : null;
        Assert.AreEqual(expectedResult, input.GetAttribute("aria-valuetext"));
    }

    [TestMethod,
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
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
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

    [TestMethod,
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

    [TestMethod,
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
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
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

    [TestMethod,
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

    [TestMethod]
    public void BitNumberFieldPageUpAndPageDownShouldChangeValueByTenTimesStepByDefault()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.Step, "2");
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        Assert.AreEqual(70, component.Instance.Value);

        input.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldPageUpAndPageDownShouldUseProvidedPageStep()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.Step, "2");
            parameters.Add(p => p.PageStep, "25");
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        Assert.AreEqual(75, component.Instance.Value);

        input.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldPageUpShouldClampAtMax()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 95);
            parameters.Add(p => p.Max, "100");
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

        Assert.AreEqual(100, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldPageKeysShouldBeIgnoredWithModifiers()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp", ShiftKey = true });
        input.KeyDown(new KeyboardEventArgs { Key = "PageUp", CtrlKey = true });

        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldPageKeysShouldInvokeIncrementAndDecrementCallbacks()
    {
        var incremented = 0;
        var decremented = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.OnIncrement, (int _) => incremented++);
            parameters.Add(p => p.OnDecrement, (int _) => decremented++);
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.AreEqual(1, incremented);
        Assert.AreEqual(1, decremented);
    }

    [TestMethod]
    public void BitNumberFieldHomeAndEndShouldJumpToExplicitMinAndMax()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.Min, "10");
            parameters.Add(p => p.Max, "90");
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual(10, component.Instance.Value);

        input.KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual(90, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldHomeAndEndShouldBeIgnoredWithoutExplicitBounds()
    {
        // Without an explicit Min/Max, Home/End must not jump to the underlying type's extremes
        // (e.g. int.MinValue) and must keep their standard text-caret behavior.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        input.KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldHomeAndEndShouldBeIgnoredWithModifiers()
    {
        // Shift+Home/Shift+End select text; they must not be hijacked as value commands.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.Min, "10");
            parameters.Add(p => p.Max, "90");
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "Home", ShiftKey = true });
        input.KeyDown(new KeyboardEventArgs { Key = "End", CtrlKey = true });

        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldHomeShouldBeIgnoredWithUnparsableMin()
    {
        // An unparsable Min falls back to the type minimum as an overflow guard, but it is not an
        // explicit bound: Home must not jump the value to int.MinValue (and no aria-valuemin renders).
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.Min, "abc");
        });

        var input = component.Find("input");

        Assert.IsFalse(input.HasAttribute("aria-valuemin"));

        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldKeyboardShouldNotChangeValueWhenReadOnly()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 50);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Min, "10");
            parameters.Add(p => p.Max, "90");
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        input.KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.AreEqual(50, component.Instance.Value);
    }

    [TestMethod,
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
            parameters.Add(p => p.Precision, 2);
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
         DataRow(5, 2, "4"),
         DataRow(1, 15, "1")
    ]
    public void BitNumberFieldTwoWayBoundWithCustomHandlerShouldWorkCorrect(int value, int countOfIncrements, string step)
    {
        BitNumberFieldTwoWayBoundValue = value;

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, step);
            parameters.Add(p => p.Value, BitNumberFieldTwoWayBoundValue);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        for (var i = 0; i < countOfIncrements; i++)
        {
            incrementButton.PointerDown();
            incrementButton.PointerUp();
        }

        var expectedValue = value + int.Parse(step) * countOfIncrements;

        Assert.AreEqual(expectedValue, BitNumberFieldTwoWayBoundValue);
    }

    [TestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitNumberFieldLabelFragmentTest(string labelFragment)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelFragment);
        });

        var ntfLabelChild = component.Find("label").ChildNodes;
        ntfLabelChild.MarkupMatches(labelFragment);
    }

    [TestMethod]
    public async Task BitNumberFieldContinuousIncrementOnPointerDownTest()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "1");
            parameters.Add(p => p.Max, "10000");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 20);
            parameters.Add(p => p.ContinuousSpinInterval, 5);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");

        // The press itself performs a single step, and the pointerdown handler must return right away
        // instead of staying pending for the whole duration of the press.
        incrementButton.PointerDown();
        Assert.AreEqual(1, component.Instance.Value);

        // Holding the button down then keeps the value climbing on its own.
        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.Value >= 5), TimeSpan.FromSeconds(10));

        incrementButton.PointerUp();

        // ... and releasing it stops the spin for good. The value is read only after a first delay,
        // so that a tick already in flight when the button came up has settled before the two reads
        // that must agree are taken.
        await Task.Delay(100);
        var valueAfterRelease = component.Instance.Value;
        await Task.Delay(100);
        Assert.AreEqual(valueAfterRelease, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldContinuousDecrementOnPointerDownTest()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "1");
            parameters.Add(p => p.Min, "-10000");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 20);
            parameters.Add(p => p.ContinuousSpinInterval, 5);
        });

        var decrementButton = component.Find("button.bit-nfl-adn");

        decrementButton.PointerDown();
        Assert.AreEqual(-1, component.Instance.Value);

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.Value <= -5), TimeSpan.FromSeconds(10));

        decrementButton.PointerUp();

        await Task.Delay(100);
        var valueAfterRelease = component.Instance.Value;
        await Task.Delay(100);
        Assert.AreEqual(valueAfterRelease, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldContinuousSpinShouldStopAtTheBound()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "1");
            parameters.Add(p => p.Max, "5");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 20);
            parameters.Add(p => p.ContinuousSpinInterval, 5);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Value), TimeSpan.FromSeconds(10));

        await Task.Delay(100);
        Assert.AreEqual(5, component.Instance.Value);

        incrementButton.PointerUp();
    }

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
         DataRow(null, "bit-nfl-pri"),
         DataRow(BitColor.Secondary, "bit-nfl-sec"),
         DataRow(BitColor.Tertiary, "bit-nfl-ter"),
         DataRow(BitColor.Info, "bit-nfl-inf"),
         DataRow(BitColor.Success, "bit-nfl-suc"),
         DataRow(BitColor.Warning, "bit-nfl-wrn"),
         DataRow(BitColor.SevereWarning, "bit-nfl-swr"),
         DataRow(BitColor.Error, "bit-nfl-err"),
         DataRow(BitColor.PrimaryBackground, "bit-nfl-pbg"),
         DataRow(BitColor.SecondaryBackground, "bit-nfl-sbg"),
         DataRow(BitColor.TertiaryBackground, "bit-nfl-tbg"),
         DataRow(BitColor.PrimaryForeground, "bit-nfl-pfg"),
         DataRow(BitColor.SecondaryForeground, "bit-nfl-sfg"),
         DataRow(BitColor.TertiaryForeground, "bit-nfl-tfg"),
         DataRow(BitColor.PrimaryBorder, "bit-nfl-pbr"),
         DataRow(BitColor.SecondaryBorder, "bit-nfl-sbr"),
         DataRow(BitColor.TertiaryBorder, "bit-nfl-tbr")
    ]
    public void BitNumberFieldShouldHaveCorrectAccentClass(BitColor? accent, string expectedClass)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Accent, accent);
        });

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
         DataRow(null, "bit-nfl-bpr"),
         DataRow(BitColorKind.Secondary, "bit-nfl-bse"),
         DataRow(BitColorKind.Tertiary, "bit-nfl-btr"),
         DataRow(BitColorKind.Transparent, "bit-nfl-btn")
    ]
    public void BitNumberFieldShouldHaveCorrectBackgroundClass(BitColorKind? background, string expectedClass)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Background, background);
        });

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
         DataRow(null, "bit-nfl-brp"),
         DataRow(BitColorKind.Secondary, "bit-nfl-brs"),
         DataRow(BitColorKind.Tertiary, "bit-nfl-brt"),
         DataRow(BitColorKind.Transparent, "bit-nfl-brn")
    ]
    public void BitNumberFieldShouldHaveCorrectBorderClass(BitColorKind? border, string expectedClass)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Border, border);
        });

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitNumberFieldShouldReflectTheAccentChangeOnRerender()
    {
        var component = RenderComponent<BitNumberField<int>>();

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-pri"));

        component.Render(parameters => parameters.Add(p => p.Accent, BitColor.Error));

        var cssClass = component.Find(".bit-nfl").ClassList;
        Assert.IsTrue(cssClass.Contains("bit-nfl-err"));
        Assert.IsFalse(cssClass.Contains("bit-nfl-pri"));
    }

    [TestMethod,
         DataRow(true, true, true),
         DataRow(false, false, false)
    ]
    public void BitNumberFieldShouldHaveCorrectVariantClasses(bool fullWidth, bool noBorder, bool underlined)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
            parameters.Add(p => p.NoBorder, noBorder);
            parameters.Add(p => p.Underlined, underlined);
        });

        var cssClass = component.Find(".bit-nfl").ClassList;

        Assert.AreEqual(fullWidth, cssClass.Contains("bit-nfl-fwd"));
        Assert.AreEqual(noBorder, cssClass.Contains("bit-nfl-nbd"));
        Assert.AreEqual(underlined, cssClass.Contains("bit-nfl-und"));
    }

    [TestMethod]
    public void BitNumberFieldShouldRenderDescriptionAndReferenceItFromTheInput()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Description, "Between 1 and 10");
        });

        var description = component.Find(".bit-nfl-des");
        Assert.AreEqual("Between 1 and 10", description.TextContent.Trim());

        // The description has to be referenced by the input, otherwise a screen reader user never
        // hears the very hint the sighted user is reading right under the field.
        var input = component.Find("input");
        Assert.AreEqual(description.Id, input.GetAttribute("aria-describedby"));

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));
    }

    [TestMethod]
    public void BitNumberFieldShouldRenderDescriptionTemplate()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "custom description");
                builder.CloseElement();
            }));
        });

        var description = component.Find(".bit-nfl-des");
        Assert.AreEqual("custom description", description.TextContent.Trim());
        Assert.AreEqual(description.Id, component.Find("input").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitNumberFieldShouldReferenceBothDescriptionsWhenBothAreProvided()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Description, "Visible hint");
            parameters.Add(p => p.AriaDescription, "Screen reader only hint");
        });

        var describedBy = component.Find("input").GetAttribute("aria-describedby");
        Assert.IsNotNull(describedBy);

        var ids = describedBy!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, ids.Length);
        Assert.AreEqual("Visible hint", component.Find($"[id='{ids[0]}']").TextContent.Trim());
        Assert.AreEqual("Screen reader only hint", component.Find($"[id='{ids[1]}']").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRenderDescriptionElementWhenNoDescriptionIsProvided()
    {
        var component = RenderComponent<BitNumberField<int>>();

        Assert.AreEqual(0, component.FindAll(".bit-nfl-des").Count);
        Assert.IsNull(component.Find("input").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));
    }

    [TestMethod,
         DataRow("Enter"),
         DataRow("Escape")
    ]
    public void BitNumberFieldOnEnterShouldOnlyFireForTheEnterKey(string key)
    {
        var enterCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.OnEnter, () => enterCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(key is "Enter" ? 1 : 0, enterCount);
    }

    [TestMethod]
    public void BitNumberFieldOnEnterShouldBeIgnoredWithModifiers()
    {
        var enterCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.OnEnter, () => enterCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.AreEqual(0, enterCount);
    }

    [TestMethod]
    public void BitNumberFieldOnEnterShouldStillFireWhenReadOnly()
    {
        var enterCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnEnter, () => enterCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, enterCount);
    }

    [TestMethod]
    public void BitNumberFieldShouldInvokeOnMaxReachedOnlyOnTheStepThatReachesIt()
    {
        var maxReachedCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Max, "2");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.OnMaxReached, () => maxReachedCount++);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs { Key = "ArrowUp" };

        input.KeyDown(args);
        Assert.AreEqual(0, maxReachedCount);

        input.KeyDown(args);
        Assert.AreEqual(1, maxReachedCount);

        // Already sitting on the bound: nothing changes, so nothing is announced again.
        input.KeyDown(args);
        Assert.AreEqual(1, maxReachedCount);
    }

    [TestMethod]
    public void BitNumberFieldShouldInvokeOnMinReachedOnlyOnTheStepThatReachesIt()
    {
        var minReachedCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.DefaultValue, 2);
            parameters.Add(p => p.OnMinReached, () => minReachedCount++);
        });

        var input = component.Find("input");
        var args = new KeyboardEventArgs { Key = "ArrowDown" };

        input.KeyDown(args);
        Assert.AreEqual(0, minReachedCount);

        input.KeyDown(args);
        Assert.AreEqual(1, minReachedCount);

        input.KeyDown(args);
        Assert.AreEqual(1, minReachedCount);
    }

    [TestMethod]
    public void BitNumberFieldShouldInvokeBoundReachedCallbacksForHomeAndEndKeys()
    {
        var minReachedCount = 0;
        var maxReachedCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.Max, "10");
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.OnMinReached, () => minReachedCount++);
            parameters.Add(p => p.OnMaxReached, () => maxReachedCount++);
        });

        var input = component.Find("input");

        input.KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual(1, maxReachedCount);
        Assert.AreEqual(0, minReachedCount);

        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual(1, maxReachedCount);
        Assert.AreEqual(1, minReachedCount);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotInvokeBoundReachedCallbacksWithoutExplicitBounds()
    {
        var minReachedCount = 0;
        var maxReachedCount = 0;
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (byte)254);
            parameters.Add(p => p.OnMinReached, () => minReachedCount++);
            parameters.Add(p => p.OnMaxReached, () => maxReachedCount++);
        });

        // 255 is the type's own extreme, not a bound the consumer asked to be told about.
        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(255, component.Instance.Value);
        Assert.AreEqual(0, maxReachedCount);
        Assert.AreEqual(0, minReachedCount);
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonsShouldBeMarkedInoperableAtTheBounds()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.Max, "2");
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        var decrementButton = component.Find("button.bit-nfl-adn");

        Assert.IsFalse(incrementButton.ClassList.Contains("bit-nfl-sbb"));
        Assert.IsFalse(decrementButton.ClassList.Contains("bit-nfl-sbb"));
        Assert.IsNull(incrementButton.GetAttribute("aria-disabled"));

        component.Render(parameters => parameters.Add(p => p.Value, 2));

        incrementButton = component.Find("button.bit-nfl-aup");
        Assert.IsTrue(incrementButton.ClassList.Contains("bit-nfl-sbb"));
        Assert.AreEqual("true", incrementButton.GetAttribute("aria-disabled"));
        // The button stays operable as an element - it is not given the disabled attribute, so it
        // keeps its place in the accessibility tree (and its focus in HideInput mode).
        Assert.IsFalse(incrementButton.HasAttribute("disabled"));
        Assert.IsFalse(component.Find("button.bit-nfl-adn").ClassList.Contains("bit-nfl-sbb"));

        component.Render(parameters => parameters.Add(p => p.Value, 0));

        Assert.IsFalse(component.Find("button.bit-nfl-aup").ClassList.Contains("bit-nfl-sbb"));
        Assert.IsTrue(component.Find("button.bit-nfl-adn").ClassList.Contains("bit-nfl-sbb"));
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonShouldNotRaiseEventsWhileAtTheBound()
    {
        var incrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Max, "5");
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Add(p => p.OnIncrement, () => incrementCount++);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        incrementButton.PointerDown();
        incrementButton.PointerUp();

        Assert.AreEqual(0, incrementCount);
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonsShouldStayOperableWithoutExplicitBounds()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
        });

        Assert.IsFalse(component.Find("button.bit-nfl-aup").ClassList.Contains("bit-nfl-sbb"));
        Assert.IsFalse(component.Find("button.bit-nfl-adn").ClassList.Contains("bit-nfl-sbb"));
    }

    [TestMethod]
    public async Task BitNumberFieldPublicIncrementAndDecrementShouldChangeTheValue()
    {
        var incrementCount = 0;
        var decrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "3");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.OnIncrement, () => incrementCount++);
            parameters.Add(p => p.OnDecrement, () => decrementCount++);
        });

        await component.Instance.IncrementAsync();
        Assert.AreEqual(3, component.Instance.Value);
        Assert.AreEqual(1, incrementCount);

        await component.Instance.DecrementAsync();
        Assert.AreEqual(0, component.Instance.Value);
        Assert.AreEqual(1, decrementCount);
    }

    [TestMethod]
    public async Task BitNumberFieldPublicIncrementShouldRespectTheBoundsAndTheDisabledState()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Max, "1");
            parameters.Add(p => p.DefaultValue, 1);
        });

        await component.Instance.IncrementAsync();
        Assert.AreEqual(1, component.Instance.Value);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Max, "10");
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.Instance.IncrementAsync();
        Assert.AreEqual(1, component.Instance.Value);

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        await component.Instance.IncrementAsync();
        Assert.AreEqual(1, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldPublicClearShouldResetTheValueAndInvokeOnClear()
    {
        var clearCount = 0;
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 7);
            parameters.Add(p => p.OnClear, () => clearCount++);
        });

        // The clear button is not shown, yet the value can still be cleared from code.
        Assert.AreEqual(0, component.FindAll("button.bit-nfl-cbt").Count);

        await component.Instance.ClearAsync();

        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(1, clearCount);
        Assert.IsTrue(string.IsNullOrEmpty(component.Find("input").GetAttribute("value")));
    }

    [TestMethod]
    public void BitNumberFieldHideInputShouldRenderAnAccessibleValueLiveRegion()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
            parameters.Add(p => p.DefaultValue, 4);
        });

        var liveRegion = component.Find("span[role=status]");
        Assert.AreEqual("4", liveRegion.TextContent.Trim());

        component.Find("button.bit-nfl-sbn:last-of-type").PointerDown();

        Assert.AreEqual("5", component.Find("span[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldHideInputShouldRenderHiddenInputAndFocusableButtons()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
        });

        Assert.AreEqual("hidden", component.Find("input").GetAttribute("type"));

        foreach (var button in component.FindAll("button.bit-nfl-sbn"))
        {
            Assert.AreEqual("0", button.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonsShouldRespondToANonPointerActivation()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
            parameters.Add(p => p.DefaultValue, 1);
        });

        // Enter and Space on the focused button, an assistive technology's activation gesture and a
        // programmatic click all arrive as a bare click with a detail of zero and no pointer sequence
        // behind them, which is the only thing that reaches the button in those cases.
        // Inline mode renders the decrement button first and the increment button second.
        component.FindAll("button.bit-nfl-sbn")[1].Click(new MouseEventArgs { Detail = 0 });

        Assert.AreEqual(2, component.Instance.Value);

        component.FindAll("button.bit-nfl-sbn")[0].Click(new MouseEventArgs { Detail = 0 });

        Assert.AreEqual(1, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonsShouldNotStepTwiceForOnePointerPress()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.DefaultValue, 1);
        });

        var incrementButton = component.Find(".bit-nfl-aup");

        // A real press steps on the pointerdown (which is what the press-and-hold spin needs), so the
        // click the browser fires after it - carrying a detail of at least one - must not step again.
        incrementButton.PointerDown();
        incrementButton.PointerUp();
        incrementButton.Click(new MouseEventArgs { Detail = 1 });

        Assert.AreEqual(2, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldHideInputShouldFocusTheIncrementButton()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
        });

        // A hidden input cannot take the focus, so the public FocusAsync has to land on the button the
        // stepper is operated from. An element reference that was never captured throws here, which is
        // what makes this assert the button and not the hidden input.
        await component.InvokeAsync(async () => await component.Instance.FocusAsync());
    }

    [TestMethod,
         DataRow(7, true, 5),
         DataRow(7, false, 7)
    ]
    public void BitNumberFieldSnapToStepShouldSnapTypedValues(int userInput, bool snapToStep, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "5");
            parameters.Add(p => p.SnapToStep, snapToStep);
        });

        component.Find("input").Change(userInput.ToString());

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldSnapToStepShouldAnchorTheGridAtTheMin()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "2");
            parameters.Add(p => p.Step, "3");
            parameters.Add(p => p.SnapToStep, true);
        });

        // With Min=2 and Step=3 the reachable values are 2, 5, 8, ... so 7 snaps up to 8.
        component.Find("input").Change("7");

        Assert.AreEqual(8, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldSnapToStepShouldWorkWithFractionalSteps()
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Step, "0.25");
            parameters.Add(p => p.SnapToStep, true);
        });

        component.Find("input").Change("0.3");

        Assert.AreEqual(0.25, component.Instance.Value);
    }

    [TestMethod,
         DataRow(true, 500),
         DataRow(false, 100)
    ]
    public void BitNumberFieldNoClampShouldKeepOutOfRangeTypedValues(bool noClamp, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.Max, "100");
            parameters.Add(p => p.NoClamp, noClamp);
        });

        component.Find("input").Change("500");

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldNoClampShouldStillClampWhenStepping()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.Max, "100");
            parameters.Add(p => p.NoClamp, true);
            parameters.Add(p => p.DefaultValue, 100);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(100, component.Instance.Value);
    }

    [TestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public void BitNumberFieldShouldRenderClearButtonOnlyWhenThereIsSomethingToClear(bool showClearButton)
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, showClearButton);
            parameters.Add(p => p.DefaultValue, 3);
        });

        Assert.AreEqual(showClearButton ? 1 : 0, component.FindAll("button.bit-nfl-cbt").Count);

        if (showClearButton is false) return;

        component.Find("button.bit-nfl-cbt").Click();

        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(0, component.FindAll("button.bit-nfl-cbt").Count);
    }

    [TestMethod]
    public void BitNumberFieldClearButtonShouldNotRenderWhenReadOnly()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 3);
        });

        Assert.AreEqual(0, component.FindAll("button.bit-nfl-cbt").Count);
    }

    [TestMethod]
    public void BitNumberFieldEscapeShouldClearTheValueWhenTheClearButtonIsShown()
    {
        var clearCount = 0;
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 3);
            parameters.Add(p => p.OnClear, () => clearCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(1, clearCount);
    }

    [TestMethod]
    public void BitNumberFieldEscapeShouldBeIgnoredWithoutTheClearButton()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(3, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldIsInputReadOnlyShouldBlockTypingButNotStepping()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.IsInputReadOnly, true);
            parameters.Add(p => p.DefaultValue, 1);
        });

        var input = component.Find("input");
        Assert.IsTrue(input.HasAttribute("readonly"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(2, component.Instance.Value);
    }

    [TestMethod,
         DataRow(-1d, false, 2),
         DataRow(1d, false, 0),
         DataRow(-1d, true, 0),
         DataRow(1d, true, 2)
    ]
    public void BitNumberFieldShiftWheelShouldSpinTheValue(double deltaY, bool invert, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.InvertMouseWheel, invert);
        });

        var input = component.Find("input");
        input.Focus();
        input.Wheel(new WheelEventArgs { DeltaY = deltaY, ShiftKey = true });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldWheelShouldBeIgnoredWithoutShiftOrFocusOrWhenDisabledByParameter()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
        });

        var input = component.Find("input");

        // No Shift key: the page is being scrolled, not the value spun.
        input.Focus();
        input.Wheel(new WheelEventArgs { DeltaY = -1, ShiftKey = false });
        Assert.AreEqual(1, component.Instance.Value);

        // Shift, but the field is merely hovered rather than focused. Only focusout clears the focus
        // state the wheel handler looks at, so blurring alone would leave the field still "focused".
        input.Blur();
        await component.InvokeAsync(() => input.TriggerEventAsync("onfocusout", new FocusEventArgs()));
        input.Wheel(new WheelEventArgs { DeltaY = -1, ShiftKey = true });
        Assert.AreEqual(1, component.Instance.Value);

        component.Render(parameters => parameters.Add(p => p.NoMouseWheel, true));
        input = component.Find("input");
        input.Focus();
        input.Wheel(new WheelEventArgs { DeltaY = -1, ShiftKey = true });
        Assert.AreEqual(1, component.Instance.Value);
    }

    [TestMethod,
         DataRow("ArrowUp"),
         DataRow("PageUp")
    ]
    public void BitNumberFieldIncrementKeysShouldNotRaiseEventsWhileAtTheMax(string key)
    {
        var incrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Max, "5");
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.OnIncrement, () => incrementCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(0, incrementCount);
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod,
         DataRow("ArrowDown"),
         DataRow("PageDown")
    ]
    public void BitNumberFieldDecrementKeysShouldNotRaiseEventsWhileAtTheMin(string key)
    {
        var decrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Min, "0");
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.OnDecrement, () => decrementCount++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(0, decrementCount);
        Assert.AreEqual(0, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShiftWheelShouldInvokeIncrementAndDecrementCallbacks()
    {
        var incrementCount = 0;
        var decrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.OnIncrement, () => incrementCount++);
            parameters.Add(p => p.OnDecrement, () => decrementCount++);
        });

        var input = component.Find("input");
        input.Focus();

        input.Wheel(new WheelEventArgs { DeltaY = -1, ShiftKey = true });
        input.Wheel(new WheelEventArgs { DeltaY = 1, ShiftKey = true });

        Assert.AreEqual(1, incrementCount);
        Assert.AreEqual(1, decrementCount);
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldContinuousSpinShouldStopWhenAStepNoLongerMovesTheValue()
    {
        // Max=10 is not a multiple of Step=3, so the snapped value pins at 9 and never reaches the
        // bound: the spin has to notice it stopped moving instead of looping on forever.
        var incrementCount = 0;
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Step, "3");
            parameters.Add(p => p.Max, "10");
            parameters.Add(p => p.SnapToStep, true);
            parameters.Add(p => p.DefaultValue, 0);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ContinuousSpinDelay, 20);
            parameters.Add(p => p.ContinuousSpinInterval, 5);
            parameters.Add(p => p.OnIncrement, () => incrementCount++);
        });

        var incrementButton = component.Find("button.bit-nfl-aup");
        incrementButton.PointerDown();

        component.WaitForAssertion(() => Assert.AreEqual(9, component.Instance.Value), TimeSpan.FromSeconds(10));

        await Task.Delay(150);

        Assert.AreEqual(9, component.Instance.Value);
        // 0 -> 3 -> 6 -> 9, then the one step that found no room left and ended the press.
        Assert.IsTrue(incrementCount <= 5, $"The continuous spin kept running: {incrementCount} increments.");

        incrementButton.PointerUp();
    }

    [TestMethod]
    public void BitNumberFieldShouldRenderPrefixAndSuffix()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Prefix, "Distance:");
            parameters.Add(p => p.Suffix, "km");
        });

        Assert.AreEqual("Distance:", component.Find(".bit-nfl-pre").TextContent.Trim());
        Assert.AreEqual("km", component.Find(".bit-nfl-suf").TextContent.Trim());
    }

    private void HandleValueChanged(int value)
        => BitNumberFieldTwoWayBoundValue = value;

    [TestMethod]
    public async Task BitNumberFieldDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitNumberField<int>>(p =>
        {
            p.Add(x => x.Mode, BitSpinButtonMode.Compact);
            p.Add(x => x.Value, 1);
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitNumberFieldDisposeDuringPointerDownShouldNotThrow()
    {
        var component = RenderComponent<BitNumberField<int>>(p =>
        {
            p.Add(x => x.Mode, BitSpinButtonMode.Compact);
            p.Add(x => x.Value, 1);
        });

        var buttons = component.FindAll("button");
        Assert.IsTrue(buttons.Count >= 2);

        await component.InvokeAsync(() => buttons[0].TriggerEvent("onpointerdown", new PointerEventArgs()));

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitNumberFieldDisposeFromCallbackShouldNotThrow()
    {
        IRenderedComponent<BitNumberField<int>>? comp = null;

        comp = RenderComponent<BitNumberField<int>>(p =>
        {
            p.Add(x => x.Mode, BitSpinButtonMode.Spread);
            p.Add(x => x.Value, 5);
            p.Add(x => x.OnIncrement, async v =>
            {
                if (comp is not null)
                {
                    await comp.Instance.DisposeAsync();
                }
            });
        });

        var buttons = comp.FindAll("button");

        await comp.InvokeAsync(() => buttons[^1].TriggerEvent("onpointerdown", new PointerEventArgs()));
    }

    [TestMethod,
         DataRow(null, "bit-nfl-md"),
         DataRow(BitSize.Small, "bit-nfl-sm"),
         DataRow(BitSize.Medium, "bit-nfl-md"),
         DataRow(BitSize.Large, "bit-nfl-lg")
    ]
    public void BitNumberFieldShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
         DataRow(null, ""),
         DataRow(BitSpinButtonMode.Compact, "bit-nfl-mcp"),
         DataRow(BitSpinButtonMode.Inline, "bit-nfl-min"),
         DataRow(BitSpinButtonMode.Spread, "bit-nfl-msp")
    ]
    public void BitNumberFieldShouldRenderTheModeClass(BitSpinButtonMode? mode, string expectedClass)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, mode);
        });

        var classList = component.Find(".bit-nfl").ClassList;

        if (expectedClass.HasValue())
        {
            Assert.IsTrue(classList.Contains(expectedClass));
        }
        else
        {
            Assert.IsFalse(classList.Contains("bit-nfl-mcp"));
            Assert.IsFalse(classList.Contains("bit-nfl-min"));
            Assert.IsFalse(classList.Contains("bit-nfl-msp"));
        }
    }

    [TestMethod]
    public void BitNumberFieldErrorMessageShouldMarkTheFieldInvalidAndDescribeIt()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Too many items.");
        });

        var error = component.Find(".bit-nfl-erm");
        Assert.AreEqual("Too many items.", error.TextContent.Trim());

        var input = component.Find("input");
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(input.GetAttribute("aria-describedby").Split(' ').Contains(error.Id));

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-inv"));

        // The message is announced by the single live region rather than by a live role of its own, so it
        // is never read out twice.
        Assert.AreEqual("Too many items.", component.Find("span[role=status]").TextContent.Trim());
        Assert.IsNull(error.GetAttribute("role"));
    }

    [TestMethod]
    public void BitNumberFieldErrorMessageTemplateShouldRenderInPlaceOfTheMessage()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Out of range");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("Out of range", component.Find(".bit-nfl-erm b").TextContent);
        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-invalid"));
        // The template carries no text the live region can read, so it announces the rejection itself.
        Assert.AreEqual("Invalid input", component.Find("[role=\"status\"]").TextContent);
    }

    [TestMethod]
    public void BitNumberFieldInvalidShouldMarkTheFieldWithoutAMessage()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-nfl-erm").Count);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRenderAriaInvalidWhenValid()
    {
        var component = RenderComponent<BitNumberField<int>>();

        Assert.IsNull(component.Find("input").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitNumberFieldPrefixAndSuffixShouldBeReferencedByTheInput()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Prefix, "$");
            parameters.Add(p => p.Suffix, "per month");
        });

        var ids = component.Find("input").GetAttribute("aria-describedby").Split(' ');

        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-pre span").Id));
        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-suf span").Id));
    }

    [TestMethod]
    public void BitNumberFieldShouldKeepAConsumerWrittenAriaDescribedBy()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Description, "Between 1 and 9.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "aria-describedby", "outside-hint" } });
        });

        var ids = component.Find("input").GetAttribute("aria-describedby").Split(' ');

        Assert.IsTrue(ids.Contains("outside-hint"));
        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-des").Id));
    }

    [TestMethod]
    public void BitNumberFieldHideInputShouldGroupAndNameItsButtons()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
            parameters.Add(p => p.Label, "Quantity");
        });

        var container = component.Find(".bit-nfl-cnt");

        Assert.AreEqual("group", container.GetAttribute("role"));
        Assert.AreEqual(component.Find("label").Id, container.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitNumberFieldCascadingParametersShouldFillWhatTheFieldLeftUnset()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[]
            {
                new BitNumberFieldParams
                {
                    Size = BitSize.Large,
                    Mode = BitSpinButtonMode.Compact,
                    Min = "2",
                    Max = "8",
                    Step = "2",
                    Suffix = "kg",
                    Underlined = true
                }
            });
            parameters.AddChildContent<BitNumberField<int>>();
        });

        var root = component.Find(".bit-nfl");

        Assert.IsTrue(root.ClassList.Contains("bit-nfl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-mcp"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-und"));
        Assert.AreEqual("kg", component.Find(".bit-nfl-suf span").TextContent.Trim());

        // Min/Max are strings parsed by setters of their own, so a cascaded value only reaches the bounds
        // when the params object calls them.
        var input = component.Find("input");
        Assert.AreEqual("2", input.GetAttribute("aria-valuemin"));
        Assert.AreEqual("8", input.GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitNumberFieldCascadingParametersShouldNotOverrideTheFieldsOwnValues()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[]
            {
                new BitNumberFieldParams { Size = BitSize.Large, Underlined = true }
            });
            parameters.AddChildContent<BitNumberField<int>>(p => p.Add(x => x.Size, BitSize.Small));
        });

        var root = component.Find(".bit-nfl");

        Assert.IsTrue(root.ClassList.Contains("bit-nfl-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-nfl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-und"));
    }

    [TestMethod]
    public void BitNumberFieldCascadingParametersShouldCarryTheBaseParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[]
            {
                new BitNumberFieldParams { Dir = BitDir.Rtl, IsEnabled = false }
            });
            parameters.AddChildContent<BitNumberField<int>>();
        });

        var root = component.Find(".bit-nfl");

        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }


    [TestMethod]
    public void BitNumberFieldLoadingShouldRenderABusyIndicatorAndAnnounceIt()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.LoadingAriaLabel, "Recalculating");
        });

        var indicator = component.Find(".bit-nfl-lod");
        Assert.AreEqual("true", indicator.GetAttribute("aria-hidden"));

        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-busy"));
        Assert.AreEqual("Recalculating", component.Find("span[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldLoadingShouldRenderForAStepperOnlyFieldToo()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
            parameters.Add(p => p.Loading, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-nfl-lod").Count);
        Assert.AreEqual("Loading", component.Find("span[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldLoadingTemplateShouldReplaceTheSpinner()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "em");
                builder.AddContent(1, "wait");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("wait", component.Find(".bit-nfl-lod em").TextContent);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRenderABusyIndicatorWhenNotLoading()
    {
        var component = RenderComponent<BitNumberField<int>>();

        Assert.AreEqual(0, component.FindAll(".bit-nfl-lod").Count);
        Assert.IsNull(component.Find("input").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitNumberFieldRequiredShouldRenderAriaRequired()
    {
        var required = RenderComponent<BitNumberField<int>>(parameters => parameters.Add(p => p.Required, true));
        Assert.AreEqual("true", required.Find("input").GetAttribute("aria-required"));

        var optional = RenderComponent<BitNumberField<int>>();
        Assert.IsNull(optional.Find("input").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitNumberFieldAriaLabelShouldNotBeShadowedByTheVisibleLabel()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Label, "The visible label");
            parameters.Add(p => p.AriaLabel, "A name of its own");
        });

        var input = component.Find("input");

        // aria-labelledby wins over aria-label, so it is left off where a name of its own was given.
        Assert.IsNull(input.GetAttribute("aria-labelledby"));
        Assert.AreEqual("A name of its own", input.GetAttribute("aria-label"));

        // The label still points at the input, which is what associates the two for a click.
        Assert.AreEqual(input.Id, component.Find("label").GetAttribute("for"));
    }

    [TestMethod]
    public void BitNumberFieldHideInputWithoutAModeShouldStillRenderItsButtons()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters => parameters.Add(p => p.HideInput, true));

        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-mcp"));
        Assert.AreEqual(2, component.FindAll(".bit-nfl-cnt button").Count);

        // Without HideInput no mode still means no buttons.
        var plain = RenderComponent<BitNumberField<int>>();
        Assert.AreEqual(0, plain.FindAll(".bit-nfl-cnt button").Count);
    }

    [TestMethod]
    public void BitNumberFieldReadOnlyShouldRenderAriaReadOnly()
    {
        var readOnly = RenderComponent<BitNumberField<int>>(parameters => parameters.Add(p => p.ReadOnly, true));
        Assert.AreEqual("true", readOnly.Find("input").GetAttribute("aria-readonly"));

        var editable = RenderComponent<BitNumberField<int>>();
        Assert.IsNull(editable.Find("input").GetAttribute("aria-readonly"));

        // Only the text is protected here - the buttons, the arrows and the wheel still change the value,
        // so the widget is not read-only.
        var inputReadOnly = RenderComponent<BitNumberField<int>>(parameters => parameters.Add(p => p.IsInputReadOnly, true));
        Assert.IsNull(inputReadOnly.Find("input").GetAttribute("aria-readonly"));
        Assert.IsTrue(inputReadOnly.Find("input").HasAttribute("readonly"));
    }

    [TestMethod]
    public void BitNumberFieldEmptyValueShouldNotRenderAnAriaValueText()
    {
        var component = RenderComponent<BitNumberField<int?>>();

        var input = component.Find("input");

        Assert.IsNull(input.GetAttribute("aria-valuetext"));
        Assert.IsNull(input.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitNumberFieldShouldTurnOffTheTextEditingHelpersOfASoftKeyboard()
    {
        var component = RenderComponent<BitNumberField<int>>();

        var input = component.Find("input");

        Assert.AreEqual("false", input.GetAttribute("spellcheck"));
        Assert.AreEqual("off", input.GetAttribute("autocorrect"));
        Assert.AreEqual("off", input.GetAttribute("autocapitalize"));
        Assert.IsNull(input.GetAttribute("enterkeyhint"));
    }

    [TestMethod]
    public void BitNumberFieldShouldRenderTheEnterKeyHintAndLetInputHtmlAttributesWin()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.EnterKeyHint, "done");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "spellcheck", "true" } });
        });

        var input = component.Find("input");

        Assert.AreEqual("done", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("true", input.GetAttribute("spellcheck"));
    }

    [TestMethod]
    public void BitNumberFieldClearButtonTemplateShouldReplaceTheIconOnly()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 7);
            parameters.Add(p => p.ClearButtonTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<em>wipe</em>")));
        });

        var button = component.Find(".bit-nfl-cbt");

        Assert.AreEqual("wipe", button.QuerySelector("em").TextContent);
        Assert.IsNull(button.QuerySelector("i"));
        Assert.AreEqual("Clear value", button.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNumberFieldEscapeShouldInvokeOnEscapeBeforeClearing()
    {
        var escapes = 0;

        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 7);
            parameters.Add(p => p.OnEscape, () => escapes++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldEscapeShouldReachOnEscapeOnAReadOnlyField()
    {
        var escapes = 0;

        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 7);
            parameters.Add(p => p.OnEscape, () => escapes++);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.AreEqual("7", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldCultureShouldFormatAndParseInThatCulture()
    {
        var component = RenderComponent<BitNumberField<double?>>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("de-DE"));
            parameters.Add(p => p.DefaultValue, 1234.5);
        });

        var input = component.Find("input");

        Assert.AreEqual("1234,5", input.GetAttribute("value"));

        // ARIA takes a plain invariant number, so what is on screen is carried by aria-valuetext instead.
        Assert.AreEqual("1234.5", input.GetAttribute("aria-valuenow"));
        Assert.AreEqual("1234,5", input.GetAttribute("aria-valuetext"));

        input.Change("1.234,5");

        Assert.AreEqual(1234.5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldCultureShouldDriveTheNumberFormat()
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("de-DE"));
            parameters.Add(p => p.NumberFormat, "N2");
            parameters.Add(p => p.DefaultValue, 1234.5);
        });

        Assert.AreEqual("1.234,50", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldWithoutACultureShouldStayInvariant()
    {
        var component = RenderComponent<BitNumberField<double?>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1234.5);
        });

        Assert.AreEqual("1234.5", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldCascadingParametersShouldCarryTheCultureAndTheEnterKeyHint()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[]
            {
                new BitNumberFieldParams
                {
                    EnterKeyHint = "next",
                    Culture = CultureInfo.GetCultureInfo("de-DE")
                }
            });
            parameters.AddChildContent<BitNumberField<double>>(p => p.Add(x => x.DefaultValue, 1234.5));
        });

        var input = component.Find("input");

        Assert.AreEqual("next", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("1234,5", input.GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldHideInputLabelShouldNotPointAtTheHiddenInput()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Inline);
            parameters.Add(p => p.Label, "Quantity");
        });

        Assert.IsNull(component.Find("label").GetAttribute("for"));
    }

    [TestMethod]
    public void BitNumberFieldHideInputGroupShouldCarryWhatDescribesTheField()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.Description, "How many boxes");
            parameters.Add(p => p.ErrorMessage, "Too many");
            parameters.Add(p => p.Suffix, "boxes");
        });

        // The hidden input is not exposed to assistive technologies at all, so the group around the
        // buttons is what the hint, the error message and the suffix have to be announced through.
        var ids = component.Find(".bit-nfl-cnt").GetAttribute("aria-describedby").Split(' ');

        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-erm").Id));
        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-des").Id));
        Assert.IsTrue(ids.Contains(component.Find(".bit-nfl-suf span").Id));
    }

    [TestMethod]
    public void BitNumberFieldWithoutHideInputShouldLeaveTheDescriptionsOnTheInputAlone()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Description, "How many boxes");
        });

        // With the input exposed, naming the wrapper as well would have it read twice.
        Assert.IsNull(component.Find(".bit-nfl-cnt").GetAttribute("aria-describedby"));
        Assert.IsNotNull(component.Find("input").GetAttribute("aria-describedby"));
    }

    [TestMethod,
         DataRow(-1d, false, 2),
         DataRow(1d, false, 0),
         DataRow(-1d, true, 0),
         DataRow(1d, true, 2)
    ]
    public void BitNumberFieldShiftWheelShouldSpinOnTheHorizontalDeltaToo(double deltaX, bool invert, int expectedValue)
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.InvertMouseWheel, invert);
        });

        var input = component.Find("input");
        input.Focus();

        // Holding Shift turns a vertical scroll into a horizontal one on macOS (and on a mouse with a
        // tilt wheel), so the gesture arrives on deltaX there and has to spin the value just the same.
        input.Wheel(new WheelEventArgs { DeltaX = deltaX, DeltaY = 0, ShiftKey = true });

        Assert.AreEqual(expectedValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldSpinButtonsShouldStopTheContinuousSpinOnACancelledPointer()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
        });

        // A pointer the browser takes over - a long-press context menu, a scroll gesture - is cancelled
        // rather than released, so without this handler the held button would go on spinning forever.
        component.Find(".bit-nfl-aup").TriggerEvent("onpointercancel", new PointerEventArgs());
        component.Find(".bit-nfl-adn").TriggerEvent("onpointercancel", new PointerEventArgs());
    }

    [TestMethod]
    public void BitNumberFieldHideInputShouldAutoFocusTheIncrementButton()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.HideInput, true);
            parameters.Add(p => p.AutoFocus, true);
        });

        // There is no input left to autofocus in this mode, so the parameter lands on the button the
        // stepper opens on instead of doing nothing at all.
        Assert.IsTrue(component.Find(".bit-nfl-aup").HasAttribute("autofocus"));
        Assert.IsFalse(component.Find(".bit-nfl-adn").HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitNumberFieldShouldClampTextThatOverflowsTheValueType()
    {
        // byte, sbyte, short, ushort, uint and ulong have no fast path in BindConverter, so their
        // TypeConverter is what parses the text - and it THROWS on a number out of range instead of
        // reporting a failure. A number typed past the end of the type's range is out of range like
        // any other, so it clamps there rather than taking the field (or the app) down.
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (byte)5);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "300" });
        Assert.AreEqual((byte)255, component.Instance.Value);

        component.Find("input").Change(new ChangeEventArgs { Value = "-5" });
        Assert.AreEqual((byte)0, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldReportTextThatIsNotANumberOnATypeConverterType()
    {
        // The same converter throws for text that is no number at all, which must come back as the
        // ordinary parse failure every other type reports: the value is left alone and the typed text
        // stays in the input to be corrected.
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (byte)7);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "abc" });

        Assert.AreEqual((byte)7, component.Instance.Value);
        Assert.AreEqual("abc", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldNoClampShouldRejectTextThatDoesNotFitTheValueType()
    {
        // NoClamp asks for an out-of-range value to reach the validator instead of being corrected,
        // but a number the type cannot hold cannot be handed to one either, so it stays a parse error.
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.NoClamp, true);
            parameters.Add(p => p.DefaultValue, (byte)7);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "300" });

        Assert.AreEqual((byte)7, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldReportTheValueInAriaValueNowForANonNullableType()
    {
        // AriaValueNow is a TValue?, which for a non-nullable TValue is that very type and so is never
        // null: a null check would answer with its default - 0 - for every field not bound to a
        // nullable type, whatever the field actually holds.
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 42);
        });

        Assert.AreEqual("42", component.Find("input").GetAttribute("aria-valuenow"));

        component.Find("input").Change(new ChangeEventArgs { Value = "7" });

        Assert.AreEqual("7", component.Find("input").GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitNumberFieldImmediateShouldKeepTheTypedTextWhileItsCommitIsPending()
    {
        // The input's value is bound, so every render writes the component's value back into the
        // element. While a DebounceTime waits the commit out that is still the value from before the
        // keystroke, and writing it back would wipe what is being typed.
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 3000);
        });

        component.Find("input").Focus();
        component.Find("input").Input(new ChangeEventArgs { Value = "12" });

        component.Render();

        Assert.AreEqual("12", component.Find("input").GetAttribute("value"));
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldShowTheCommittedValueAgainOnceTheFieldIsLeft()
    {
        // ... and the text kept visible for that pending commit is history the moment the field is
        // left, where the committed (and possibly formatted) value is what must show.
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 3000);
            parameters.Add(p => p.DefaultValue, 5);
        });

        component.Find("input").Focus();
        component.Find("input").Input(new ChangeEventArgs { Value = "12" });
        component.Find("input").FocusOut();

        Assert.AreEqual("5", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldShouldClampTextThatOverflowsAFastPathType()
    {
        // short, int and long go through BindConverter's fast paths, which report an overflow as a
        // plain false rather than throwing - they must clamp exactly like the TypeConverter-backed types.
        var component = RenderComponent<BitNumberField<short>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (short)5);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "40000" });
        Assert.AreEqual(short.MaxValue, component.Instance.Value);

        component.Find("input").Change(new ChangeEventArgs { Value = "-40000" });
        Assert.AreEqual(short.MinValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldClampOverflowingTextThatCarriesDigitGroupSpaces()
    {
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (byte)5);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "3 000" });

        Assert.AreEqual((byte)255, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldStillRejectAFractionInsideTheRangeOfAnIntegralType()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
        });

        component.Find("input").Change(new ChangeEventArgs { Value = "1.5" });

        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public async Task BitNumberFieldImmediateShouldCommitThePendingTypedTextBeforeAStep()
    {
        // Text typed in Immediate mode whose DebounceTime has not run out yet is what the input shows,
        // and a step taken meanwhile applies to it - and is not undone when the debounce fires later.
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("12");

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DebounceTime, 200);
            parameters.Add(p => p.DefaultValue, 5);
        });

        var input = component.Find("input");
        input.Focus();
        input.Input(new ChangeEventArgs { Value = "12" });
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(13, component.Instance.Value);

        await Task.Delay(500);

        Assert.AreEqual(13, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldCommitRetypedTextThatMatchesTheValueBeforeTheLastStep()
    {
        // After a step has rendered, text the user types is theirs even when it happens to equal what
        // the input showed before that step.
        var liveValue = Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true);
        liveValue.SetResult("5");

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
        });

        var input = component.Find("input");
        input.Focus();
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(6, component.Instance.Value);

        // the user retypes "5" (no event is raised for it outside the Immediate mode) and steps again
        liveValue.SetResult("5");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(6, component.Instance.Value);
    }
}
