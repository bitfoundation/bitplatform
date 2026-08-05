using System;
using System.Collections.Generic;
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
}
