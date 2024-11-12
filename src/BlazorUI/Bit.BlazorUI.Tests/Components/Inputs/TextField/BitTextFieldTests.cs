using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

[TestClass]
public class BitTextFieldTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, true, true),
        DataRow(false, true, false),
        DataRow(true, false, true),
        DataRow(false, false, false)
    ]
    public void BitTextFieldShouldTakeCorrectTypeAndVisual(bool isEnabled, bool multiline, bool required)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.Required, required);
        });

        var bitTextField = component.Find(".bit-tfl");
        var textField = component.Find(".bit-tfl-inp");

        if (isEnabled)
        {
            Assert.IsFalse(bitTextField.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitTextField.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(multiline ? "TEXTAREA" : "INPUT", textField.TagName);

        Assert.AreEqual(required, textField.HasAttribute("required"));
        Assert.AreEqual(required, bitTextField.ClassList.Contains("bit-tfl-req"));
    }

    [DataTestMethod,
       DataRow(true, "hello world"),
       DataRow(false, "hello world")
    ]
    public void BitTextFieldShouldTakeValue(bool multiline, string value)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Multiline, multiline);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        Assert.AreEqual(bitTextField.GetAttribute("value"), value);
    }

    [DataTestMethod, DataRow("this is label")]
    public void BitTextFieldLabel(string label)
    {
        var com = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitTextFieldLabel = com.Find(".bit-tfl-lbl").TextContent;

        Assert.AreEqual(label, bitTextFieldLabel);
    }

    [DataTestMethod,
        DataRow(15, true, "this is placeholder", true),
        DataRow(15, true, "this is placeholder", false),
        DataRow(15, false, "this is placeholder", true),
        DataRow(15, false, "this is placeholder", false),
    ]
    public void BitTextFieldShouldTakeBaseParameters(int maxLength, bool multiline, string placeholder, bool readOnly)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.MaxLength, maxLength);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.Placeholder, placeholder);
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        Assert.IsTrue(bitTextField.HasAttribute("maxlength"));
        Assert.AreEqual(bitTextField.GetAttribute("maxlength"), maxLength.ToString());

        Assert.IsTrue(bitTextField.HasAttribute("placeholder"));
        Assert.AreEqual(bitTextField.GetAttribute("placeholder"), placeholder);

        Assert.AreEqual(readOnly, bitTextField.HasAttribute("readonly"));
    }

    [DataTestMethod, DataRow("Emoji2")]
    public void BitTextFieldShouldAcceptIcon(string iconName)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var bitTextFieldIcon = component.Find(".bit-tfl-inp + .bit-icon");

        Assert.IsTrue(bitTextFieldIcon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitTextFieldCanRevealPassword(bool isEnabled)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Type, BitInputType.Password);
            parameters.Add(p => p.CanRevealPassword, true);
        });

        var textField = component.Find(".bit-tfl-inp");
        var revealPasswordBtn = component.Find(".bit-tfl-prb");
        var revealPasswordIcon = component.Find(".bit-tfl-prb > span > i");

        Assert.AreEqual("password", textField.GetAttribute("type"));
        Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--View"));

        revealPasswordBtn.Click();

        Assert.AreEqual("text", textField.GetAttribute("type"));
        Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--Hide3"));
    }

    [DataTestMethod,
        DataRow(BitInputType.Password),
        DataRow(BitInputType.Number),
        DataRow(BitInputType.Tel)
    ]
    public void BitInputTypeTest(BitInputType type)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Type, type);
        });

        var textField = component.Find(".bit-tfl-inp");

        Assert.AreEqual(type.GetDisplayName(toLowerDisplayName: true), textField.GetAttribute("type"));
    }

    [DataTestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, false),
        DataRow(false, true),
    ]
    public void BitTextFieldMustRespondToTheClickEvent(bool isEnabled, bool multiline)
    {
        int currentCount = 0;
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.OnClick, () => currentCount++);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        bitTextField.Click();

        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [DataTestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, false),
        DataRow(false, true),
    ]
    public void BitTextFieldMustRespondToTheFocusEvent(bool isEnabled, bool multiline)
    {
        int focusedValue = 0;
        int focusedInValue = 0;
        int focusedOutValue = 0;
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.OnFocus, () => focusedValue++);
            parameters.Add(p => p.OnFocusIn, () => focusedInValue++);
            parameters.Add(p => p.OnFocusOut, () => focusedOutValue++);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        bitTextField.Focus();
        Assert.AreEqual(isEnabled ? 1 : 0, focusedValue);

        bitTextField.FocusIn();
        Assert.AreEqual(isEnabled ? 1 : 0, focusedInValue);

        bitTextField.FocusOut();
        Assert.AreEqual(isEnabled ? 1 : 0, focusedOutValue);
    }

    [DataTestMethod,
        DataRow(true, false, "u", "d"),
        DataRow(true, true, "u", "d"),
        DataRow(false, false, "u", "d"),
        DataRow(false, true, "u", "d"),
    ]
    public void BitTextFieldMustRespondToTheKeyEvent(bool isEnabled, bool multiline, string keyUpValue, string keyDownValue)
    {
        string? keyUppedValue = null;
        string? keyDownedValue = null;
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.OnKeyUp, (KeyboardEventArgs e) => keyUppedValue = e.Key);
            parameters.Add(p => p.OnKeyDown, (KeyboardEventArgs e) => keyDownedValue = e.Key);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        bitTextField.KeyUp(keyUpValue);
        Assert.AreEqual(isEnabled ? keyUpValue : null, keyUppedValue);

        bitTextField.KeyDown(keyDownValue);
        Assert.AreEqual(isEnabled ? keyDownValue : null, keyDownedValue);
    }

    [DataTestMethod,
       DataRow(true, false),
       DataRow(true, true),
       DataRow(false, false),
       DataRow(false, true),
    ]
    public void BitTextFieldMustRespondToTheChangeEvent(bool isEnabled, bool multiline)
    {
        int currentCount = 0;
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.OnChange, () => currentCount++);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        bitTextField.Change("a");

        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [DataTestMethod, DataRow("Prefix")]
    public void BitTextFieldShowPrefix(string prefix)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Prefix, prefix);
        });

        var bitTextFieldPrefix = component.Find(".bit-tfl-pre > span");
        Assert.AreEqual(prefix, bitTextFieldPrefix.TextContent);
    }

    [DataTestMethod, DataRow("Suffix")]
    public void BitTextFieldShowSuffix(string suffix)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Suffix, suffix);
        });

        var bitTextFieldSuffix = component.Find(".bit-tfl-suf > span");
        Assert.AreEqual(suffix, bitTextFieldSuffix.TextContent);
    }

    [DataTestMethod,
        DataRow(true, "hello world"),
        DataRow(false, "hello world"),
    ]
    public void BitTextFieldShouldTakeDefaultValue(bool multiline, string defaultValue)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Multiline, multiline);
        });

        var bitTextField = component.Find(".bit-tfl-inp");

        Assert.AreEqual(defaultValue, bitTextField.GetAttribute("value"));
    }

    [DataTestMethod, DataRow("test description")]
    public void BitTextFieldShouldTakeDescription(string description)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Description, description);
        });

        var bitTextFieldDescription = component.Find(".bit-tfl-des > span");
        Assert.AreEqual(description, bitTextFieldDescription.TextContent);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextFieldShouldRespectUnderLineStyle(bool underlined)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Underlined, underlined);
        });

        var bitTextField = component.Find(".bit-tfl");
        Assert.AreEqual(underlined, bitTextField.ClassList.Contains("bit-tfl-und"));
    }

    [DataTestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitTextFieldShouldRespectHasBorderStyle(bool noBorder)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.NoBorder, noBorder);
        });

        var bitTextField = component.Find(".bit-tfl");
        Assert.AreEqual(noBorder, bitTextField.ClassList.Contains("bit-tfl-nbd"));
    }

    [DataTestMethod, DataRow(5)]
    public void BitTextFieldShouldRespectRowsNumberWhenItIsMultiline(int rows)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Multiline, true);
            parameters.Add(p => p.Rows, rows);
        });

        var input = component.Find(".bit-tfl textarea");
        Assert.AreEqual(rows.ToString(), input.GetAttribute("rows"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextFieldShouldRespectIsResizableWhenItIsMultiline(bool resizable)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Multiline, true);
            parameters.Add(p => p.Resizable, resizable);
        });

        var bitTextField = component.Find(".bit-tfl");
        Assert.AreEqual(!resizable, bitTextField.ClassList.Contains("bit-tfl-mlf"));
    }

    [DataTestMethod,
        DataRow("Detailed label", true),
        DataRow("Detailed label", false)
    ]
    public void BitTextFieldAriaLabelTest(string ariaLabel, bool multiline)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.Add(p => p.Multiline, multiline);
        });


        var input = component.Find(".bit-tfl-inp");

        Assert.IsTrue(input.HasAttribute("aria-label"));
        Assert.AreEqual(input.GetAttribute("aria-label"), ariaLabel);
    }

    [DataTestMethod,
        DataRow(true, "this is label"),
        DataRow(false, "this is label"),
        DataRow(true, null),
        DataRow(false, null)
    ]
    public void BitTextFieldAriaLabelledbyTest(bool multiline, string label)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Label, label);
            parameters.Add(p => p.Multiline, multiline);
        });

        var input = component.Find(".bit-tfl-inp");
        Assert.AreEqual(label.HasValue(), input.HasAttribute("aria-labelledby"));
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("off"),
        DataRow("email")
    ]
    public void BitTextFieldAutoCompleteTest(string autoComplete)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Multiline, false);
            parameters.Add(p => p.AutoComplete, autoComplete);
        });

        var input = component.Find(".bit-tfl-inp");

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

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
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
            parameters.Add(p => p.Multiline, true);
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

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
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
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
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
            parameters.Add(p => p.Multiline, true);
        });

        var isInvalid = value != "test@bit.com";

        var textarea = component.Find("textarea");
        Assert.IsFalse(textarea.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(textarea.HasAttribute("aria-invalid"), isInvalid);
        if (textarea.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", textarea.GetAttribute("aria-invalid"));
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

        var bitTextField = component.Find(".bit-tfl");

        Assert.IsFalse(bitTextField.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, bitTextField.ClassList.Contains("bit-inv"));

        var input = component.Find("input");

        if (isInvalid)
        {
            input.Change("test@bit.com");
        }
        else
        {
            input.Change("abc123");
        }

        Assert.AreEqual(isInvalid is false, bitTextField.ClassList.Contains("bit-inv"));
    }

    [DataTestMethod,
        DataRow("  bit"),
        DataRow("bit  "),
        DataRow(" bit component "),
        DataRow("  bit  "),
    ]
    public void BitTextFieldTrimmedDefaultValueTest(string value)
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Trim, true);
            parameters.Add(p => p.DefaultValue, value);
        });

        var bitTextField = component.Find(".bit-tfl-inp");
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
            parameters.Add(p => p.Trim, true);
        });

        var input = component.Find("input");
        input.Change(value);

        Assert.AreEqual(value.Trim(), component.Instance.Value);
    }
}
