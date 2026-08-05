using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

[TestClass]
public class BitOtpInputTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputTest(bool isEnabled)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (isEnabled)
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow(4),
        DataRow(6)
    ]
    public void BitOtpInputLengthTest(int length)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, length);
        });

        var bitOtpInput = com.Find(".bit-otp-iwr");

        Assert.AreEqual(length, bitOtpInput.Children.Length);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputReversedTest(bool reversed)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (reversed)
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-otp-rvs"));
        }
        else
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-otp-rvs"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputVerticalTest(bool vertical)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (vertical)
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-otp-vrt"));
        }
        else
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-otp-vrt"));
        }
    }

    [TestMethod,
        DataRow(BitInputType.Text),
        DataRow(BitInputType.Number),
        DataRow(BitInputType.Password)
    ]
    public void BitInputTypeTest(BitInputType inputType)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 1);
            parameters.Add(p => p.Type, inputType);
        });

        // A number typed input is the wrong element for a single character box (it reports an empty value
        // for e/+/-/., it carries spin buttons and the wheel changes it), so the numeric type is rendered
        // as a text input whose numeric keyboard comes from the inputmode.
        string inputTypeAttribute = inputType switch
        {
            BitInputType.Text => "text",
            BitInputType.Number => "text",
            BitInputType.Password => "password",
            _ => string.Empty
        };

        string inputModeAttribute = inputType switch
        {
            BitInputType.Text => "text",
            BitInputType.Number => "numeric",
            BitInputType.Password => "text",
            _ => string.Empty
        };

        var bitOtpInput = com.Find(".bit-otp-inp");

        Assert.AreEqual(inputTypeAttribute, bitOtpInput.GetAttribute("type"));
        Assert.AreEqual(inputModeAttribute, bitOtpInput.GetAttribute("inputmode"));
    }

    [TestMethod]
    public void BitOtpInputShouldRespondToFocusEventsWithIndex()
    {
        int focusInIndex = -1;
        int focusOutIndex = -1;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.OnFocusIn, args => focusInIndex = args.Index);
            parameters.Add(p => p.OnFocusOut, args => focusOutIndex = args.Index);
        });

        var input = com.FindAll(".bit-otp-inp")[1];

        input.FocusIn();
        Assert.AreEqual(1, focusInIndex);

        com.FindAll(".bit-otp-inp")[1].FocusOut();
        Assert.AreEqual(1, focusOutIndex);
    }

    [TestMethod]
    public void BitOtpInputShouldToggleFocusedClassWithoutFocusCallbacks()
    {
        // The Focused class/style must be applied based purely on the input focus state,
        // independent of whether OnFocusIn/OnFocusOut delegates are attached.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Classes, new BitOtpInputClassStyles { Focused = "custom-focused" });
        });

        com.FindAll(".bit-otp-inp")[1].FocusIn();
        Assert.IsTrue(com.FindAll(".bit-otp-inp")[1].ClassList.Contains("custom-focused"));

        com.FindAll(".bit-otp-inp")[1].FocusOut();
        Assert.IsFalse(com.FindAll(".bit-otp-inp")[1].ClassList.Contains("custom-focused"));
    }

    [TestMethod]
    public void BitOtpInputShouldRespectDefaultValue()
    {
        var defaultValue = "1234";

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("1", inputs[0].GetAttribute("value"));
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));
        Assert.AreEqual("3", inputs[2].GetAttribute("value"));
        Assert.AreEqual("4", inputs[3].GetAttribute("value"));
    }

    [TestMethod,
        DataRow(0),
        DataRow(-3)
    ]
    public void BitOtpInputShouldClampLengthToAtLeastOne(int length)
    {
        // A non-positive Length used to throw while building the input ids and the element references.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, length);
        });

        Assert.AreEqual(1, com.Find(".bit-otp-iwr").Children.Length);
    }

    [TestMethod]
    public void BitOtpInputShouldFollowTheLengthChanges()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "12");
        });

        com.Render(parameters => parameters.Add(p => p.Length, 6));

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual(6, inputs.Count);
        // The characters that were already typed stay in the inputs that survive the resize.
        Assert.AreEqual("1", inputs[0].GetAttribute("value"));
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));

        com.Render(parameters => parameters.Add(p => p.Length, 2));

        Assert.AreEqual(2, com.FindAll(".bit-otp-inp").Count);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputShouldRespectReadOnly(bool readOnly)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        var bitOtpInput = com.Find(".bit-otp");
        var input = com.Find(".bit-otp-inp");

        Assert.AreEqual(readOnly, bitOtpInput.ClassList.Contains("bit-otp-rdl"));
        Assert.AreEqual(readOnly, input.HasAttribute("readonly"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotAcceptInputWhenReadOnly()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.ReadOnly, true);
        });

        await com.Find(".bit-otp-inp").InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.IsNull(com.Instance.Value);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputShouldRespectRequired(bool required)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Required, required);
        });

        Assert.AreEqual(required, com.Find(".bit-otp").ClassList.Contains("bit-otp-req"));
        Assert.AreEqual(required, com.Find(".bit-otp-inp").HasAttribute("required"));
    }

    [TestMethod]
    public void BitOtpInputShouldRenderASingleHiddenFieldForTheName()
    {
        // The value of an OTP input is the whole code, so a form must receive it as one named field
        // instead of one entry per input.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Name, "otp");
            parameters.Add(p => p.DefaultValue, "1234");
        });

        var hiddenInputs = com.FindAll("input[type=hidden]");

        Assert.AreEqual(1, hiddenInputs.Count);
        Assert.AreEqual("otp", hiddenInputs[0].GetAttribute("name"));
        Assert.AreEqual("1234", hiddenInputs[0].GetAttribute("value"));
    }

    [TestMethod]
    public void BitOtpInputShouldRenderTheInputHtmlAttributesOnEveryInput()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "data-test", "otp" } });
        });

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.AreEqual("otp", input.GetAttribute("data-test"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldSpreadAPlaceholderAsLongAsTheLength()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Placeholder, "abc");
        });

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("a", inputs[0].GetAttribute("placeholder"));
        Assert.AreEqual("b", inputs[1].GetAttribute("placeholder"));
        Assert.AreEqual("c", inputs[2].GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitOtpInputShouldRepeatAPlaceholderOfAnotherLength()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Placeholder, "-");
        });

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.AreEqual("-", input.GetAttribute("placeholder"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldRenderASeparatorBetweenTheInputs()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Separator, "-");
        });

        var separators = com.FindAll(".bit-otp-sep");

        Assert.AreEqual(3, separators.Count);
        Assert.AreEqual("-", separators[0].TextContent);
        Assert.AreEqual("true", separators[0].GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitOtpInputShouldRenderNoSeparatorByDefault()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
        });

        Assert.AreEqual(0, com.FindAll(".bit-otp-sep").Count);
    }

    [TestMethod,
        DataRow(BitVariant.Fill, "bit-otp-fil"),
        DataRow(BitVariant.Outline, "bit-otp-otl"),
        DataRow(BitVariant.Text, "bit-otp-txt"),
        DataRow(null, "bit-otp-otl")
    ]
    public void BitOtpInputShouldRespectVariant(BitVariant? variant, string variantClass)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains(variantClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-otp-pri"),
        DataRow(BitColor.Success, "bit-otp-suc"),
        DataRow(BitColor.Error, "bit-otp-err"),
        DataRow(null, "bit-otp-pri")
    ]
    public void BitOtpInputShouldRespectAccent(BitColor? accent, string accentClass)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Accent, accent);
        });

        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains(accentClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-otp-sm"),
        DataRow(BitSize.Medium, "bit-otp-md"),
        DataRow(BitSize.Large, "bit-otp-lg"),
        DataRow(null, "bit-otp-md")
    ]
    public void BitOtpInputShouldRespectSize(BitSize? size, string sizeClass)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains(sizeClass));
    }

    [TestMethod]
    public void BitOtpInputShouldClearTheInputsWhenTheValueIsSetToNull()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Value, "1234");
            parameters.Add(p => p.ValueChanged, _ => { });
        });

        Assert.AreEqual("1", com.FindAll(".bit-otp-inp")[0].GetAttribute("value"));

        com.Render(parameters => parameters.Add(p => p.Value, null));

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.IsTrue(string.IsNullOrEmpty(input.GetAttribute("value")));
        }
    }

    [TestMethod]
    public async Task BitOtpInputClearShouldEmptyEveryInputAndTheValue()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.InvokeAsync(() => com.Instance.Clear());

        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.IsTrue(string.IsNullOrEmpty(input.GetAttribute("value")));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldExposeTheInputsAsAnAccessibleGroup()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Label, "OTP");
        });

        var wrapper = com.Find(".bit-otp-iwr");
        var label = com.Find(".bit-otp-lbl");

        Assert.AreEqual("group", wrapper.GetAttribute("role"));
        Assert.AreEqual(label.Id, wrapper.GetAttribute("aria-labelledby"));

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("1 of 3", inputs[0].GetAttribute("aria-label"));
        Assert.AreEqual("3 of 3", inputs[2].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitOtpInputShouldNameTheGroupWithTheAriaLabelWhenThereIsNoLabel()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.AriaLabel, "One time code");
        });

        var wrapper = com.Find(".bit-otp-iwr");

        Assert.AreEqual("One time code", wrapper.GetAttribute("aria-label"));
        Assert.IsFalse(wrapper.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitOtpInputShouldStyleTheLabelTemplateLikeTheLabel()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Classes, new BitOtpInputClassStyles { Label = "custom-label" });
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>OTP</span>")));
        });

        var label = com.Find("label");

        Assert.IsTrue(label.ClassList.Contains("bit-otp-lbl"));
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
    }

    [TestMethod]
    public void BitOtpInputShouldMarkTheFilledInputs()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.DefaultValue, "12");
            parameters.Add(p => p.Classes, new BitOtpInputClassStyles { Filled = "custom-filled" });
        });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.IsTrue(inputs[0].ClassList.Contains("bit-otp-fld"));
        Assert.IsTrue(inputs[0].ClassList.Contains("custom-filled"));
        Assert.IsTrue(inputs[1].ClassList.Contains("bit-otp-fld"));
        Assert.IsFalse(inputs[2].ClassList.Contains("bit-otp-fld"));
        Assert.IsFalse(inputs[2].ClassList.Contains("custom-filled"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldMoveTheValueToTheNextInputWhileTyping()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });

        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldCallOnFillOnlyOncePerCompletedValue()
    {
        var onFillCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.OnFill, _ => onFillCallCount++);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });
        Assert.AreEqual(0, onFillCallCount);

        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });
        Assert.AreEqual(1, onFillCallCount);

        // Retyping the same character over an already complete code must not raise the callback again,
        // otherwise the consumer would submit the very same code twice.
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });
        Assert.AreEqual(1, onFillCallCount);

        // A different complete code is a new fill though.
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "3" });
        Assert.AreEqual(2, onFillCallCount);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRejectNonDigitsWhenTypeIsNumber()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "a" });

        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));
    }

    [TestMethod]
    public async Task BitOtpInputShouldAcceptANumericCodeLongerThanAnInt()
    {
        // The numeric check used to be an int.TryParse, which rejected any code with more digits than
        // int.MaxValue has.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 12);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("123456789012", 0));

        Assert.AreEqual("123456789012", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldDropTheNonDigitsOfAPastedNumericCode()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("123-456", 0));

        Assert.AreEqual("123456", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldPasteAShortCodeIntoTheInputThatReceivedIt()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 5);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("12", 2));

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.IsTrue(string.IsNullOrEmpty(inputs[0].GetAttribute("value")));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[1].GetAttribute("value")));
        Assert.AreEqual("1", inputs[2].GetAttribute("value"));
        Assert.AreEqual("2", inputs[3].GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldPasteAFullCodeFromTheFirstInput()
    {
        // A code that fills the whole component would lose its leading characters if it were inserted
        // where the paste happened, so it always starts at the first input.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("1234", 2));

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotAcceptAPastedValueWhenReadOnly()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.ReadOnly, true);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("1234", 0));

        Assert.IsNull(com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldShiftTheValuesOnBackspaceWhenAutoShiftIsEnabled()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.AutoShift, true);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        // The browser clears the input itself on Backspace, which is what the input event stands for
        // here; the component has to shift the remaining characters and commit the result.
        await com.FindAll(".bit-otp-inp")[1].KeyDownAsync(new KeyboardEventArgs { Code = "Backspace", Key = "Backspace" });
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "" });

        Assert.AreEqual("134", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldShiftTheValuesOnDeleteWhenAutoShiftIsEnabled()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.AutoShift, true);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.FindAll(".bit-otp-inp")[0].KeyDownAsync(new KeyboardEventArgs { Code = "Delete", Key = "Delete" });

        Assert.AreEqual("234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRejectACharacterThatDoesNotMatchThePattern()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Pattern, "^[A-F0-9]$");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "z" });
        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "A" });
        Assert.AreEqual("A", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldDropThePastedCharactersThatDoNotMatchThePattern()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Pattern, "^[A-F0-9]$");
        });

        await com.InvokeAsync(() => com.Instance._SetValue("1z2y3x4w", 0));

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldIgnoreAPatternThatDoesNotCompile()
    {
        // An unusable expression must not turn the component into a field that accepts nothing.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Pattern, "[unclosed");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual("1", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldRespectTheInputAriaLabelFormat()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.InputAriaLabelFormat, "رقم {0} از {1}");
        });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual("رقم 1 از 3", inputs[0].GetAttribute("aria-label"));
        Assert.AreEqual("رقم 3 از 3", inputs[2].GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldDropTheWhitespaceOfAPastedCode()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
        });

        await com.InvokeAsync(() => com.Instance._SetValue(" 123\n456 ", 0));

        Assert.AreEqual("123456", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldKeepTheFocusedInputOnBackspaceWhenItHoldsACharacter()
    {
        // Backspace used to move the focus back even when it had a character to delete right where it
        // was, which sent the character the user retyped into the input before it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.FindAll(".bit-otp-inp")[3].KeyDownAsync(new KeyboardEventArgs { Code = "Backspace", Key = "Backspace" });

        // The browser clears the input itself, which is what the input event stands for here. Only that
        // one input is cleared, the character before it is untouched.
        await com.FindAll(".bit-otp-inp")[3].InputAsync(new ChangeEventArgs { Value = "" });

        Assert.AreEqual("123", com.Instance.Value);

        // And retyping lands back in the very same input.
        await com.FindAll(".bit-otp-inp")[3].InputAsync(new ChangeEventArgs { Value = "9" });

        Assert.AreEqual("1239", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldDeleteThePreviousCharacterOnBackspaceInAnEmptyInput()
    {
        // An empty input gives Backspace nothing to delete, so it deletes the character before it, the
        // way the same key behaves in a single input holding the whole code.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "12");
        });

        await com.FindAll(".bit-otp-inp")[2].KeyDownAsync(new KeyboardEventArgs { Code = "Backspace", Key = "Backspace" });

        Assert.AreEqual("1", com.Instance.Value);
        Assert.IsTrue(string.IsNullOrEmpty(com.FindAll(".bit-otp-inp")[1].GetAttribute("value")));
    }

    [TestMethod]
    public async Task BitOtpInputShouldDoNothingOnBackspaceInTheFirstEmptyInput()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
        });

        await com.FindAll(".bit-otp-inp")[0].KeyDownAsync(new KeyboardEventArgs { Code = "Backspace", Key = "Backspace" });

        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotDeleteThePreviousCharacterOnBackspaceWhenReadOnly()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, "12");
        });

        await com.FindAll(".bit-otp-inp")[2].KeyDownAsync(new KeyboardEventArgs { Code = "Backspace", Key = "Backspace" });

        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldRenderTheMaskInsteadOfTheCharacters()
    {
        // The mask only changes what the inputs show: the value stays the code that was typed.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Mask, "•");
            parameters.Add(p => p.DefaultValue, "12");
        });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual("•", inputs[0].GetAttribute("value"));
        Assert.AreEqual("•", inputs[1].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[2].GetAttribute("value")));
        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRecordTheTypedCharacterBehindTheMask()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Mask, "•");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "5" });

        Assert.AreEqual("5", com.Instance.Value);
        Assert.AreEqual("•", com.FindAll(".bit-otp-inp")[0].GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotMistakeTheMaskForAPastedChunk()
    {
        // Typing with the caret behind the masking character sends both of them in the input event; the
        // masking character has to be subtracted instead of being treated as a pasted code.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Mask, "•");
            parameters.Add(p => p.DefaultValue, "1");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "•2" });

        Assert.AreEqual("2", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldGroupTheSeparatorsBySeparatorInterval()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Separator, "-");
            parameters.Add(p => p.SeparatorInterval, 3);
        });

        var wrapper = com.Find(".bit-otp-iwr");

        Assert.AreEqual(1, com.FindAll(".bit-otp-sep").Count);
        Assert.AreEqual(7, wrapper.Children.Length);
        // The single separator sits right after the third input, which is what makes the code read 123-456.
        Assert.IsTrue(wrapper.Children[3].ClassList.Contains("bit-otp-sep"));
    }

    [TestMethod,
        DataRow(0),
        DataRow(-2)
    ]
    public void BitOtpInputShouldTreatANonPositiveSeparatorIntervalAsOne(int separatorInterval)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Separator, "-");
            parameters.Add(p => p.SeparatorInterval, separatorInterval);
        });

        Assert.AreEqual(3, com.FindAll(".bit-otp-sep").Count);
    }

    [TestMethod,
        DataRow(true, "off"),
        DataRow(false, "one-time-code")
    ]
    public void BitOtpInputShouldRespectNoSmsAutoFill(bool noSmsAutoFill, string autoComplete)
    {
        // Turning the SMS auto fill off has to turn the one-time-code autofill of the browser off too,
        // otherwise the code would still be filled in by the platform on its own.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.NoSmsAutoFill, noSmsAutoFill);
        });

        Assert.AreEqual(autoComplete, com.Find(".bit-otp-inp").GetAttribute("autocomplete"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldUppercaseTheTypedCharacters()
    {
        // The conversion happens before the pattern is applied, so a pattern restricted to upper case
        // letters accepts a lower case keystroke instead of rejecting it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Uppercase, true);
            parameters.Add(p => p.Pattern, "^[A-Z]$");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "a" });

        Assert.AreEqual("A", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldUppercaseAPastedCode()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Uppercase, true);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("ab3d", 0));

        Assert.AreEqual("AB3D", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldKeepTheCaseOfTheTypedCharactersByDefault()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "a" });

        Assert.AreEqual("a", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldTruncateAValueLongerThanTheLength()
    {
        // Reporting a value that the inputs are not showing would leave the two out of sync for good.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "123456");
        });

        Assert.AreEqual("1234", com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual(4, inputs.Count);
        Assert.AreEqual("4", inputs[3].GetAttribute("value"));
    }

    [TestMethod]
    public void BitOtpInputShouldTruncateTheValueWhenTheLengthShrinksBelowIt()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        com.Render(parameters => parameters.Add(p => p.Length, 2));

        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldBlurTheInputsOnFillWhenBlurOnFillIsEnabled()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.BlurOnFill, true);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.blur"].Count);

        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.blur"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotBlurTheInputsOnFillByDefault()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 1);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.blur"].Count);
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(false, true)
    ]
    public void BitOtpInputShouldNotAskForTheSmsCodeWhenReadOnly(bool readOnly, bool smsAutoFill)
    {
        // A read-only component is showing a code rather than waiting for one, so the WebOTP request of
        // the browser has no reason to be made for it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.OtpInput.setup"];

        Assert.AreEqual(1, invocations.Count);
        Assert.AreEqual(smsAutoFill, invocations[0].Arguments[3]);
    }

    [TestMethod,
        DataRow(BitInputMode.Numeric, "numeric"),
        DataRow(BitInputMode.Tel, "tel"),
        DataRow(BitInputMode.Text, "text")
    ]
    public void BitOtpInputShouldRespectTheInputMode(BitInputMode inputMode, string expected)
    {
        // The inputmode is what decides the virtual keyboard of a phone, and it is asked for on its own so
        // that a keyboard the Type does not imply (the telephone keypad for a code of digits) can be used.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Type, BitInputType.Text);
            parameters.Add(p => p.InputMode, inputMode);
        });

        Assert.AreEqual(expected, com.Find(".bit-otp-inp").GetAttribute("inputmode"));
    }

    [TestMethod]
    public void BitOtpInputShouldFallBackToTheInputModeOfTheType()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        Assert.AreEqual("numeric", com.Find(".bit-otp-inp").GetAttribute("inputmode"));
    }

    [TestMethod]
    public void BitOtpInputShouldRenderTheSeparatorTemplateInsteadOfTheSeparator()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Separator, "-");
            parameters.Add(p => p.SeparatorInterval, 2);
            parameters.Add(p => p.SeparatorTemplate, (RenderFragment<int>)(index => builder => builder.AddMarkupContent(0, $"<i>{index}</i>")));
        });

        var separators = com.FindAll(".bit-otp-sep");

        Assert.AreEqual(1, separators.Count);
        // The context of the template is the index of the input the separator is rendered before.
        Assert.AreEqual("<i>2</i>", separators[0].InnerHtml);
    }

    [TestMethod]
    public void BitOtpInputShouldRenderTheSeparatorTemplateWithoutASeparatorText()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.SeparatorTemplate, (RenderFragment<int>)(_ => builder => builder.AddMarkupContent(0, "<i>x</i>")));
        });

        Assert.AreEqual(2, com.FindAll(".bit-otp-sep").Count);
    }

    [TestMethod]
    public void BitOtpInputShouldKeepEveryInputInTheTabOrderByDefault()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
        });

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.IsFalse(input.HasAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldBecomeASingleTabStop()
    {
        // Only the input holding the first character of the code stays reachable with the Tab key, so a
        // keyboard user tabs past the whole code in one press instead of one per character.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.SingleTabStop, true);
        });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.IsFalse(inputs[0].HasAttribute("tabindex"));
        Assert.AreEqual("-1", inputs[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", inputs[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitOtpInputShouldUppercaseTheValueThatIsAssignedToIt()
    {
        // Showing the upper case form of a code while reporting the value that was assigned would leave
        // the inputs and the value out of sync for good.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Uppercase, true);
            parameters.Add(p => p.DefaultValue, "ab1c");
        });

        Assert.AreEqual("AB1C", com.Instance.Value);
        Assert.AreEqual("A", com.FindAll(".bit-otp-inp")[0].GetAttribute("value"));
    }

    [TestMethod]
    public void BitOtpInputShouldDropTheCharactersOfAnAssignedValueThatThePatternRejects()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Pattern, "^[0-9]$");
            parameters.Add(p => p.DefaultValue, "1z2y");
        });

        Assert.AreEqual("12", com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("1", inputs[0].GetAttribute("value"));
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[2].GetAttribute("value")));
    }

    [TestMethod]
    public async Task BitOtpInputShouldCallOnFillAgainAfterTheLengthHasChanged()
    {
        // A code of another length is another code, so the completed one that the callback was raised for
        // last must not keep it from firing for the code that fills the resized component.
        var onFillCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.OnFill, _ => onFillCallCount++);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });
        await com.FindAll(".bit-otp-inp")[2].InputAsync(new ChangeEventArgs { Value = "3" });
        Assert.AreEqual(1, onFillCallCount);

        com.Render(parameters => parameters.Add(p => p.Length, 2));
        com.Render(parameters => parameters.Add(p => p.Length, 3));

        await com.FindAll(".bit-otp-inp")[2].InputAsync(new ChangeEventArgs { Value = "3" });

        Assert.AreEqual("123", com.Instance.Value);
        Assert.AreEqual(2, onFillCallCount);
    }

    [TestMethod]
    public async Task BitOtpInputClearShouldDoNothingWhenReadOnly()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.InvokeAsync(() => com.Instance.Clear());

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldBindTheLabelToTheFirstInput()
    {
        // Clicking the label has to move the focus into the code, which is what the for attribute does.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Label, "OTP");
        });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual(inputs[0].Id, com.Find(".bit-otp-lbl").GetAttribute("for"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldKeepTheFirstCharactersOfAPastedCodeThatIsTooLong()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("123456", 0));

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldIgnoreAPastedValueThatIsEntirelyRejected()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.DefaultValue, "12");
        });

        await com.InvokeAsync(() => com.Instance._SetValue("   abc   ", 0));

        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldForwardTheIndexOfTheInputThatRaisedTheKeyDown()
    {
        var keyDownIndex = -1;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.OnKeyDown, args => keyDownIndex = args.Index);
        });

        await com.FindAll(".bit-otp-inp")[2].KeyDownAsync(new KeyboardEventArgs { Code = "ArrowLeft", Key = "ArrowLeft" });

        Assert.AreEqual(2, keyDownIndex);
    }

    [TestMethod]
    public async Task BitOtpInputShouldClearASingleInputOnDeleteWithoutAutoShift()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.FindAll(".bit-otp-inp")[1].KeyDownAsync(new KeyboardEventArgs { Code = "Delete", Key = "Delete" });

        Assert.AreEqual("134", com.Instance.Value);
        Assert.AreEqual("3", com.FindAll(".bit-otp-inp")[2].GetAttribute("value"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputShouldRespectInvalid(bool invalid)
    {
        // A code that the server has rejected is only known to be wrong once it has been submitted, so
        // there is nothing for a validator to see and the error state has to be set from the outside.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Invalid, invalid);
        });

        Assert.AreEqual(invalid, com.Find(".bit-otp").ClassList.Contains("bit-inv"));

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.AreEqual(invalid ? "true" : null, input.GetAttribute("aria-invalid"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldFollowTheInvalidChanges()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains("bit-inv"));

        com.Render(parameters => parameters.Add(p => p.Invalid, false));

        Assert.IsFalse(com.Find(".bit-otp").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitOtpInputShouldKeepTheAriaInvalidOfTheInputHtmlAttributes()
    {
        // The aria-invalid of a failing validation travels in the InputHtmlAttributes, which are splatted
        // before the attribute of the Invalid parameter is rendered, so it must not be dropped by it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "aria-invalid", "true" } });
        });

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        }
    }

    [TestMethod]
    public async Task BitOtpInputShouldRaiseOnInvalidForARejectedCharacter()
    {
        (string Value, int Index)? invalidArgs = null;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.OnInvalid, args => invalidArgs = args);
        });

        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "a" });

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual("a", invalidArgs.Value.Value);
        Assert.AreEqual(1, invalidArgs.Value.Index);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotRaiseOnInvalidForAnAcceptedCharacter()
    {
        var invalidCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.OnInvalid, _ => invalidCallCount++);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual(0, invalidCallCount);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRaiseOnInvalidForAPastedCodeThatIsRejectedInFull()
    {
        (string Value, int Index)? invalidArgs = null;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.OnInvalid, args => invalidArgs = args);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("abcd", 2));

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual("abcd", invalidArgs.Value.Value);
        Assert.AreEqual(2, invalidArgs.Value.Index);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotRaiseOnInvalidWhenAPasteOnlyLosesSomeCharacters()
    {
        // A code copied with the dashes in it still fills the inputs, so it is not a rejection.
        var invalidCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.OnInvalid, _ => invalidCallCount++);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("123-456", 0));

        Assert.AreEqual("123456", com.Instance.Value);
        Assert.AreEqual(0, invalidCallCount);
    }

    [TestMethod]
    public void BitOtpInputShouldApplyAnUppercaseThatIsTurnedOnAtRuntime()
    {
        // Narrowing the set of characters the code may hold has to reach the characters that are already
        // in the inputs, otherwise the component would keep showing a code it would now reject.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "ab1c");
        });

        Assert.AreEqual("ab1c", com.Instance.Value);

        com.Render(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Uppercase, true);
        });

        Assert.AreEqual("AB1C", com.Instance.Value);
        Assert.AreEqual("A", com.FindAll(".bit-otp-inp")[0].GetAttribute("value"));
    }

    [TestMethod]
    public void BitOtpInputShouldApplyAPatternThatChangesAtRuntime()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1a2b");
        });

        Assert.AreEqual("1a2b", com.Instance.Value);

        com.Render(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Pattern, "^[0-9]$");
        });

        Assert.AreEqual("12", com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("1", inputs[0].GetAttribute("value"));
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[2].GetAttribute("value")));
    }

    [TestMethod]
    public void BitOtpInputShouldApplyATypeThatChangesAtRuntime()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1a2b");
        });

        com.Render(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        Assert.AreEqual("12", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldForwardAKeyDownItCannotNavigateWith()
    {
        // A keystroke that identifies itself with neither a key nor a code is nothing the navigation can
        // act on, but it is still a keystroke the consumer asked to be told about.
        var keyDownCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.OnKeyDown, _ => keyDownCallCount++);
        });

        await com.FindAll(".bit-otp-inp")[0].KeyDownAsync(new KeyboardEventArgs());

        Assert.AreEqual(1, keyDownCallCount);
    }

    [TestMethod]
    public void BitOtpInputShouldDisableTheHiddenFieldAlongWithTheComponent()
    {
        // A disabled component contributes nothing to the form it sits in.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Name, "otp");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, "12");
        });

        Assert.IsTrue(com.Find("input[type=hidden]").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitOtpInputShouldRenderNoHiddenFieldWithoutAName()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
        });

        Assert.AreEqual(0, com.FindAll("input[type=hidden]").Count);
    }

    [TestMethod]
    public async Task BitOtpInputClearShouldDoNothingWhenDisabled()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.InvokeAsync(() => com.Instance.Clear());

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldReportTheValueOnEveryKeystrokeThroughOnChange()
    {
        var onChangeValues = new List<string?>();

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.OnChange, v => onChangeValues.Add(v));
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });
        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "2" });

        CollectionAssert.AreEqual(new[] { "1", "12" }, onChangeValues);
    }

    [TestMethod,
        DataRow("۱۲۳۴۵۶"), // Persian
        DataRow("١٢٣٤٥٦"), // Arabic-Indic
        DataRow("１２３４５６") // full width
    ]
    public async Task BitOtpInputShouldNormalizeThePastedDigitsOfTheOtherNumberingSystems(string code)
    {
        // A code that arrives in a message written in the language of the user has to reach the server in
        // the ASCII form that it expects.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.NormalizeDigits, true);
        });

        await com.InvokeAsync(() => com.Instance._SetValue(code, 0));

        Assert.AreEqual("123456", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNormalizeATypedDigitOfAnotherNumberingSystem()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Type, BitInputType.Number);
            parameters.Add(p => p.NormalizeDigits, true);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "۵" });

        Assert.AreEqual("5", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRejectTheDigitsOfTheOtherNumberingSystemsByDefault()
    {
        // The conversion is opt-in, so a component that did not ask for it keeps rejecting them the way
        // it rejects any other character that is not an ASCII digit.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Type, BitInputType.Number);
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "۵" });

        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));
    }

    [TestMethod]
    public async Task BitOtpInputShouldKeepTheNonDigitsWhileNormalizingTheDigits()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.NormalizeDigits, true);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("a۲b۴", 0));

        Assert.AreEqual("a2b4", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotCallOnFillWhileReadOnly()
    {
        // A read-only component is showing a code rather than waiting for one, so an input event that
        // reaches it must not submit the code it is merely showing.
        var onFillCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, "12");
            parameters.Add(p => p.OnFill, _ => onFillCallCount++);
        });

        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "9" });

        Assert.AreEqual("12", com.Instance.Value);
        Assert.AreEqual(0, onFillCallCount);
    }

    [TestMethod]
    public async Task BitOtpInputShouldStillForwardOnInputWhileReadOnly()
    {
        var inputIndex = -1;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnInput, args => inputIndex = args.Index);
        });

        await com.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "9" });

        Assert.AreEqual(1, inputIndex);
    }

    [TestMethod]
    public void BitOtpInputShouldNotAskForTheSmsCodeWhenDisabled()
    {
        // A component that is turned off while the request of the browser is pending has to drop it
        // rather than leaving its permission prompt up over a component that would refuse the code.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
        });

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.OtpInput.setup"];

        Assert.AreEqual(1, invocations.Count);
        Assert.AreEqual(true, invocations[0].Arguments[3]);

        com.Render(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual(false, invocations[1].Arguments[3]);
    }

    [TestMethod]
    public void BitOtpInputShouldDescribeTheGroupWithTheDescription()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Label, "OTP");
            parameters.Add(p => p.Description, "We sent a code to your phone.");
        });

        var wrapper = com.Find(".bit-otp-iwr");
        var description = com.Find(".bit-otp-dsc");

        Assert.AreEqual("We sent a code to your phone.", description.TextContent);
        Assert.AreEqual(description.Id, wrapper.GetAttribute("aria-describedby"));

        // The hint belongs to the code as a whole, so repeating it on every single input would make a
        // screen reader read it again at every character.
        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.IsFalse(input.HasAttribute("aria-describedby"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldRenderNoDescriptionByDefault()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
        });

        Assert.AreEqual(0, com.FindAll(".bit-otp-dsc").Count);
        Assert.IsFalse(com.Find(".bit-otp-iwr").HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitOtpInputShouldPreferTheDescriptionTemplateOverTheDescription()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Description, "plain");
            parameters.Add(p => p.Classes, new BitOtpInputClassStyles { Description = "custom-description" });
            parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<b>templated</b>")));
        });

        var description = com.Find(".bit-otp-dsc");

        Assert.AreEqual("<b>templated</b>", description.InnerHtml);
        Assert.IsTrue(description.ClassList.Contains("custom-description"));
        Assert.AreEqual(description.Id, com.Find(".bit-otp-iwr").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldLowercaseTheTypedCharacters()
    {
        // The conversion happens before the pattern is applied, so a pattern restricted to lower case
        // letters accepts an upper case keystroke instead of rejecting it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
            parameters.Add(p => p.Lowercase, true);
            parameters.Add(p => p.Pattern, "^[a-z]$");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "A" });

        Assert.AreEqual("a", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldLowercaseAPastedCode()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Lowercase, true);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("AB3D", 0));

        Assert.AreEqual("ab3d", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldPreferTheUppercaseWhenBothCasesAreAskedFor()
    {
        // Asking for both at once is a contradiction rather than an order to apply, so one of them is
        // picked instead of the two undoing each other.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Uppercase, true);
            parameters.Add(p => p.Lowercase, true);
            parameters.Add(p => p.DefaultValue, "aBcD");
        });

        Assert.AreEqual("ABCD", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldApplyALowercaseThatIsTurnedOnAtRuntime()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "AB1C");
        });

        com.Render(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Lowercase, true);
        });

        Assert.AreEqual("ab1c", com.Instance.Value);
        Assert.AreEqual("a", com.FindAll(".bit-otp-inp")[0].GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldPullTheFocusBackToTheFirstEmptyInputWhenSequential()
    {
        // A character typed into the middle of an empty row would be reported as if it were the first one
        // of the code, since an empty input contributes nothing to the joined value.
        var focusInIndexes = new List<int>();

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Sequential, true);
            parameters.Add(p => p.DefaultValue, "1");
            parameters.Add(p => p.OnFocusIn, args => focusInIndexes.Add(args.Index));
        });

        await com.FindAll(".bit-otp-inp")[3].FocusInAsync(new FocusEventArgs());

        // The consumer is told about the input the user actually reached, and the focus is then corrected
        // onto the first input left to fill.
        CollectionAssert.AreEqual(new[] { 3 }, focusInIndexes);
        Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotPullTheFocusBackOnACompleteCodeWhenSequential()
    {
        // Every character of a complete code stays clickable, otherwise the last one could never be
        // corrected without clearing the whole code first.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Sequential, true);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.FindAll(".bit-otp-inp")[3].FocusInAsync(new FocusEventArgs());

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotPullTheFocusBackWhenReadOnlyOrNotSequential()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, "1");
        });

        await com.FindAll(".bit-otp-inp")[3].FocusInAsync(new FocusEventArgs());

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);

        // A read-only component is showing a code rather than waiting for one, so clicking a character of
        // it is a way of reading it rather than of typing it.
        com.Render(parameters =>
        {
            parameters.Add(p => p.Sequential, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        await com.FindAll(".bit-otp-inp")[3].FocusInAsync(new FocusEventArgs());

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotLetAPastedChunkPunchAHoleWhenSequential()
    {
        // A component asked to keep the code free of holes cannot let a paste land past the first input
        // left to fill either.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 5);
            parameters.Add(p => p.Sequential, true);
            parameters.Add(p => p.DefaultValue, "1");
        });

        await com.InvokeAsync(() => com.Instance._SetValue("23", 4));

        Assert.AreEqual("123", com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));
        Assert.AreEqual("3", inputs[2].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[4].GetAttribute("value")));
    }

    [TestMethod]
    public async Task BitOtpInputShouldPasteAChunkWhereItArrivedOnACompleteCodeWhenSequential()
    {
        // A complete code has no input left to fill, so the chunk simply overwrites where it arrived.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Sequential, true);
            parameters.Add(p => p.DefaultValue, "1234");
        });

        await com.InvokeAsync(() => com.Instance._SetValue("9", 2));

        Assert.AreEqual("1294", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldKeepTheCharactersAfterAPartiallyPastedChunk()
    {
        // A chunk that arrives at one input only replaces as many characters as it brings, so pasting a
        // single character into the middle of a code must not take the rest of it down with it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.DefaultValue, "123456");
        });

        await com.InvokeAsync(() => com.Instance._SetValue("99", 1));

        Assert.AreEqual("199456", com.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldStillReplaceTheWholeCodeWhenTheValueIsAssigned()
    {
        // A value assigned to the component is the whole code, so everything it leaves over has to go.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Value, "123456");
            parameters.Add(p => p.ValueChanged, _ => { });
        });

        com.Render(parameters => parameters.Add(p => p.Value, "99"));

        Assert.AreEqual("99", com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("9", inputs[0].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[2].GetAttribute("value")));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[5].GetAttribute("value")));
    }

    [TestMethod]
    public async Task BitOtpInputShouldPullTheCodeOutOfAPastedTextWithThePasteTransformer()
    {
        // The per character filtering cannot do this on its own for a code of letters, since the letters of
        // the words around it match just as well as the ones of the code.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 6);
            parameters.Add(p => p.Uppercase, true);
            parameters.Add(p => p.PasteTransformer, v => Regex.Match(v, "[A-Za-z0-9]{6}").Value);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("your code is A1B2C3, do not share it", 0));

        Assert.AreEqual("A1B2C3", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldApplyThePasteTransformerToAMultiCharacterInputEvent()
    {
        // An auto fill that writes the whole code into one input has to go through the very same steps as a
        // paste, so that the two can never disagree.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.PasteTransformer, v => v.Replace("code:", string.Empty));
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "code:1234" });

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldRaiseOnInvalidWhenThePasteTransformerRejectsTheChunk()
    {
        (string Value, int Index)? invalidArgs = null;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.OnInvalid, args => invalidArgs = args);
            parameters.Add(p => p.PasteTransformer, _ => string.Empty);
        });

        await com.InvokeAsync(() => com.Instance._SetValue("1234", 0));

        Assert.IsTrue(string.IsNullOrEmpty(com.Instance.Value));
        Assert.IsNotNull(invalidArgs);
        // The text is reported the way it arrived, not the way the transformer left it.
        Assert.AreEqual("1234", invalidArgs.Value.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldIgnoreAPasteTransformerThatThrows()
    {
        // The transformer runs in the middle of a paste, so an exception thrown out of it must not take the
        // whole component down with it.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.PasteTransformer, _ => throw new InvalidOperationException());
        });

        await com.InvokeAsync(() => com.Instance._SetValue("1234", 0));

        Assert.AreEqual("1234", com.Instance.Value);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotApplyThePasteTransformerToASingleTypedCharacter()
    {
        var transformerCallCount = 0;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.PasteTransformer, v => { transformerCallCount++; return v; });
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual("1", com.Instance.Value);
        Assert.AreEqual(0, transformerCallCount);
    }

    [TestMethod]
    public void BitOtpInputShouldNotSetTheJsSideUpAgainForALengthChange()
    {
        // The javascript listeners are delegated to the root element and resolve the input from the event
        // target, so repeating the setup for a Length that changed would only tear the pending WebOTP
        // request down and ask for it again, putting the permission prompt of the browser back up.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.setup"].Count);

        com.Render(parameters => parameters.Add(p => p.Length, 6));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.setup"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputBlurAsyncShouldBlurTheFocusedInput()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 2);
        });

        await com.InvokeAsync(() => com.Instance.BlurAsync());

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.blur"].Count);
    }

    [TestMethod]
    public async Task BitOtpInputShouldNotApplyTheMaskToThePlaceholderOfAnEmptyInput()
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Mask, "●");
            parameters.Add(p => p.Placeholder, "0");
        });

        await com.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "7" });

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual("●", inputs[0].GetAttribute("value"));
        Assert.IsTrue(string.IsNullOrEmpty(inputs[1].GetAttribute("value")));
        Assert.AreEqual("0", inputs[1].GetAttribute("placeholder"));
    }
}
