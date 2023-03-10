﻿using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs;

[TestClass]
public class BitTextFieldTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, true, true),
        DataRow(false, true, false),
        DataRow(true, false, true),
        DataRow(false, false, false)
    ]
    public void BitTextFieldShouldTakeCorrectTypeAndVisual(bool isEnabled, bool isMultiline, bool isRequired)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsMultiline, isMultiline);
            parameters.Add(p => p.IsRequired, isRequired);
        });

        var bitTextField = component.Find(".bit-txt");
        var textField = component.Find(".txt-field");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";

        if (isEnabled)
        {
            Assert.IsFalse(bitTextField.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitTextField.ClassList.Contains("disabled"));
        }

        Assert.AreEqual(isMultiline ? "TEXTAREA" : "INPUT", textField.TagName);

        Assert.AreEqual(isRequired, textField.HasAttribute("required"));
        Assert.AreEqual(bitTextField.ClassList.Contains($"required"), isRequired);
    }

    [DataTestMethod,
       DataRow(true, "hello world"),
       DataRow(false, "hello world")
   ]
    public void BitTextFieldShouldTakeValue(bool isMultiline, string value)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        if (isMultiline)
        {
            Assert.AreEqual(bitTextField.TextContent, value);
        }
        else
        {
            Assert.AreEqual(bitTextField.GetAttribute("value"), value);
        }
    }

    [DataTestMethod, DataRow("this is label")]
    public void BitTextFieldLabel(string label)
    {
        var com = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitTextFieldLabel = com.Find(".label").TextContent;

        Assert.AreEqual(label, bitTextFieldLabel);
    }

    [DataTestMethod,
        DataRow(15, true, "this is placeholder", true),
        DataRow(15, true, "this is placeholder", false),
        DataRow(15, false, "this is placeholder", true),
        DataRow(15, false, "this is placeholder", false),
    ]
    public void BitTextFieldShouldTakeBaseParameters(int maxLength, bool isMultiline, string placeholder, bool isReadOnly)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.MaxLength, maxLength);
            parameters.Add(p => p.IsMultiline, isMultiline);
            parameters.Add(p => p.Placeholder, placeholder);
            parameters.Add(p => p.IsReadOnly, isReadOnly);
        });

        var bitTextField = component.Find(".txt-field");

        Assert.IsTrue(bitTextField.HasAttribute("maxlength"));
        Assert.AreEqual(bitTextField.GetAttribute("maxlength"), maxLength.ToString());

        Assert.IsTrue(bitTextField.HasAttribute("placeholder"));
        Assert.AreEqual(bitTextField.GetAttribute("placeholder"), placeholder);

        Assert.AreEqual(isReadOnly, bitTextField.HasAttribute("readonly"));
    }

    [DataTestMethod, DataRow(BitIconName.Emoji2)]
    public void BitTextFieldShouldAcceptIcon(BitIconName iconName)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var bitTextFieldIcon = component.Find(".txt-field + .bit-icon");

        Assert.IsTrue(bitTextFieldIcon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitTextFieldCanRevealPassword(bool isEnabled)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Type, BitTextFieldType.Password);
            parameters.Add(p => p.CanRevealPassword, true);
        });

        var bitTextField = component.Find(".bit-txt");
        var textField = component.Find(".txt-field");
        var revealPasswordBtn = component.Find(".password-reveal-btn");
        var revealPasswordIcon = component.Find(".password-reveal-btn > span > i");

        Assert.AreEqual("password", textField.GetAttribute("type"));
        Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--RedEye"));

        revealPasswordBtn.Click();

        Assert.AreEqual("text", textField.GetAttribute("type"));
        Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--Hide"));
    }

    [DataTestMethod,
        DataRow(BitTextFieldType.Password),
        DataRow(BitTextFieldType.Number),
        DataRow(BitTextFieldType.Tel)
    ]
    public void BitTextFieldTypeTest(BitTextFieldType type)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Type, type);
        });

        var textField = component.Find(".txt-field");

        Assert.AreEqual(type.GetDisplayName(toLowerDisplayName: true), textField.GetAttribute("type"));
    }

    [DataTestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, false),
        DataRow(false, true),
    ]
    public void BitTextFieldMustRespondToTheClickEvent(bool isEnabled, bool isMultiline)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        bitTextField.Click();

        Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.CurrentCount);
    }

    [DataTestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, false),
        DataRow(false, true),
    ]
    public void BitTextFieldMustRespondToTheFocusEvent(bool isEnabled, bool isMultiline)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        bitTextField.Focus();
        Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.FocusedValue);

        bitTextField.FocusIn();
        Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.FocusedInValue);

        bitTextField.FocusOut();
        Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.FocusedOutValue);
    }

    [DataTestMethod,
        DataRow(true, false, "u", "d"),
        DataRow(true, true, "u", "d"),
        DataRow(false, false, "u", "d"),
        DataRow(false, true, "u", "d"),
    ]
    public void BitTextFieldMustRespondToTheKeyEvent(bool isEnabled, bool isMultiline, string keyUpValue, string keyDownValue)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        bitTextField.KeyDown(keyDownValue);
        Assert.AreEqual(isEnabled ? keyDownValue : null, component.Instance.KeyDownedValue);

        bitTextField.KeyUp(keyUpValue);
        Assert.AreEqual(isEnabled ? keyUpValue : null, component.Instance.KeyUpedValue);
    }

    [DataTestMethod,
       DataRow(true, false),
       DataRow(true, true),
       DataRow(false, false),
       DataRow(false, true),
   ]
    public void BitTextFieldMustRespondToTheChangeEvent(bool isEnabled, bool isMultiline)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        bitTextField.KeyDown("a");

        //TODO: bypassed - BUnit oninput event issue
        //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.CurrentCount);
    }

    [DataTestMethod, DataRow("Prefix")]
    public void BitTextFieldShowPrefix(string prefix)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Prefix, prefix);
        });

        var bitTextFieldPrefix = component.Find(".txt-prefix > span");
        Assert.AreEqual(prefix, bitTextFieldPrefix.TextContent);
    }

    [DataTestMethod, DataRow("Suffix")]
    public void BitTextFieldShowSuffix(string suffix)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Suffix, suffix);
        });

        var bitTextFieldSuffix = component.Find(".txt-suffix > span");
        Assert.AreEqual(suffix, bitTextFieldSuffix.TextContent);
    }

    [DataTestMethod,
        DataRow(true, null, "hello world"),
        DataRow(false, null, "hello world"),
        DataRow(true, "hello bit", "hello world"),
        DataRow(false, "hello bit", "hello world"),
    ]
    public void BitTextFieldShouldTakeDefaultValue(bool isMultiline, string value, string defaultValue)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var bitTextField = component.Find(".txt-field");

        var actualValue = string.IsNullOrEmpty(value) ? defaultValue : value;

        if (isMultiline)
        {
            Assert.AreEqual(bitTextField.TextContent, actualValue);
        }
        else
        {
            Assert.AreEqual(bitTextField.GetAttribute("value"), actualValue);
        }
    }

    [DataTestMethod, DataRow("test description")]
    public void BitTextFieldShouldTakeDescription(string description)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Description, description);
        });

        var bitTextFieldDescription = component.Find(".txt-desc > span");
        Assert.AreEqual(description, bitTextFieldDescription.TextContent);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextFieldShouldRespectUnderLineStyle(bool isUnderlined)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsUnderlined, isUnderlined);
        });

        var bitTextField = component.Find(".bit-txt");
        Assert.AreEqual(isUnderlined, bitTextField.ClassList.Contains("underlined"));
    }

    [DataTestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitTextFieldShouldRespectHasBorderStyle(bool hasBorder)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.HasBorder, hasBorder);
        });

        var bitTextField = component.Find(".bit-txt");
        Assert.AreEqual(hasBorder is false, bitTextField.ClassList.Contains("no-border"));
    }

    [DataTestMethod, DataRow(5)]
    public void BitTextFieldShouldRespectRowsNumberWhenItIsMultiline(int rows)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsMultiline, true);
            parameters.Add(p => p.Rows, rows);
        });

        var input = component.Find(".bit-txt textarea");
        Assert.AreEqual(rows.ToString(), input.GetAttribute("rows"));
    }

    [DataTestMethod, DataRow(true), DataRow(false)]
    public void BitTextFieldShouldRespectIsResizableWhenItIsMultiline(bool isResizable)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsMultiline, true);
            parameters.Add(p => p.IsResizable, isResizable);
        });

        var bitTextField = component.Find(".bit-txt");
        Assert.AreEqual(!isResizable, bitTextField.ClassList.Contains("multiline-fix"));
    }

    [DataTestMethod,
        DataRow("Detailed label", true),
        DataRow("Detailed label", false)
    ]
    public void BitTextFieldAriaLabelTest(string ariaLabel, bool isMultiline)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });


        var input = component.Find(".txt-field");

        Assert.IsTrue(input.HasAttribute("aria-label"));
        Assert.AreEqual(input.GetAttribute("aria-label"), ariaLabel);
    }

    [DataTestMethod,
        DataRow(true, "this is label"),
        DataRow(false, "this is label"),
        DataRow(true, null),
        DataRow(false, null)
    ]
    public void BitTextFieldAriaLabelledbyTest(bool isMultiline, string label)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.Label, label);
            parameters.Add(p => p.IsMultiline, isMultiline);
        });

        var input = component.Find(".txt-field");
        Assert.AreEqual(label.HasValue(), input.HasAttribute("aria-labelledby"));
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("off"),
        DataRow("email")
    ]
    public void BitTextFieldAutoCompleteTest(string autoComplete)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsMultiline, false);
            parameters.Add(p => p.AutoComplete, autoComplete);
        });

        var input = component.Find(".txt-field");

        if (autoComplete.HasValue())
        {
            Assert.IsTrue(input.HasAttribute("autocomplete"));
            Assert.AreEqual(autoComplete, input.GetAttribute("autocomplete"));
        }
        else
        {
            Assert.IsFalse(input.HasAttribute("autocomplete"));
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitTextFieldValidationFormTest(string value)
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTextFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isValid = value == "test@bit.com";

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        var input = component.Find("input");
        if (isValid)
        {
            input.Change("");
        }
        else
        {
            input.Change("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, 1);
        Assert.AreEqual(component.Instance.InvalidCount, 1);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitTextFieldValidationFormTestWhenItIsMultiline(string value)
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTextFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiline, true);
        });

        var isValid = value == "test@bit.com";

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        var textarea = component.Find("textarea");
        if (isValid)
        {
            textarea.Change("");
        }
        else
        {
            textarea.Change("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, 1);
        Assert.AreEqual(component.Instance.InvalidCount, 1);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitTextFieldValidationInvalidHtmlAttributeTest(string value)
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTextFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value != "test@bit.com";

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
            input.Change("test@bit.com");
            Assert.IsFalse(input.HasAttribute("aria-invalid"));
        }
        else
        {
            input.Change("");
            Assert.IsTrue(input.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitTextFieldValidationInvalidHtmlAttributeTestWhenItIsMultiline(string value)
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTextFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IsMultiline, true);
        });

        var isInvalid = value != "test@bit.com";

        var textarea = component.Find("textarea");
        Assert.IsFalse(textarea.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(textarea.HasAttribute("aria-invalid"), isInvalid);
        if (textarea.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual(textarea.GetAttribute("aria-invalid"), "true");
        }

        if (isInvalid)
        {
            textarea.Change("test@bit.com");
            Assert.IsFalse(textarea.HasAttribute("aria-invalid"));
        }
        else
        {
            textarea.Change("");
            Assert.IsTrue(textarea.HasAttribute("aria-invalid"));
        }
    }

    [DataTestMethod,
        DataRow("abc123"),
        DataRow("test@bit.com")
    ]
    public void BitTextFieldValidationInvalidCssClassTest(string value)
    {
        var component = RenderComponent<BitTextFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitTextFieldTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var isInvalid = value != "test@bit.com";

        var bitTextField = component.Find(".bit-txt");

        Assert.IsFalse(bitTextField.ClassList.Contains("invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(bitTextField.ClassList.Contains("invalid"), isInvalid);

        var input = component.Find("input");
        if (isInvalid)
        {
            input.Change("test@bit.com");
        }
        else
        {
            input.Change("abc123");
        }

        Assert.AreEqual(bitTextField.ClassList.Contains("invalid"), !isInvalid);
    }

    [DataTestMethod,
        DataRow("  bit"),
        DataRow("bit  "),
        DataRow(" bit component "),
        DataRow("  bit  "),
   ]
    public void BitTextFieldTrimmedDefaultValueTest(string value)
    {
        var component = RenderComponent<BitTextFieldTest>(parameters =>
        {
            parameters.Add(p => p.IsTrimmed, true);
            parameters.Add(p => p.DefaultValue, value);
        });

        var bitTextField = component.Find(".txt-field");
        var trimmedValue = bitTextField.GetAttribute("value");

        Assert.AreEqual(value.Trim(), trimmedValue);
    }

    [DataTestMethod,
        DataRow("  bit"),
        DataRow("bit  "),
        DataRow(" bit component "),
        DataRow("  bit  "),
    ]
    public void BitTextFieldTrimmedValueTest(string value)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsTrimmed, true);
        });

        var input = component.Find("input");
        input.Change(value);

        Assert.AreEqual(value.Trim(), component.Instance.Value);
    }
}
