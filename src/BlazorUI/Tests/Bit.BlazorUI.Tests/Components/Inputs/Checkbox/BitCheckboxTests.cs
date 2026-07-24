using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

[TestClass]
public class BitCheckboxTests : BunitTestContext
{
    private bool BitCheckBoxIsChecked;
    private bool BitCheckBoxIsIndeterminate = true;

    [TestMethod,
        DataRow(true, true),
        DataRow(false, true),
        DataRow(true, false),
        DataRow(false, false),
    ]
    public void BitCheckboxOnClickShouldWorkIfIsEnabled(bool defaultValue, bool isEnabled)
    {
        var clicked = false;
        var changed = false;
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.OnClick, () => clicked = true);
            parameters.Add(p => p.OnChange, () => changed = true);
        });

        var checkBox = component.Find(".bit-chb");
        var chbCheckbox = component.Find("input");

        if (isEnabled)
        {
            Assert.IsFalse(checkBox.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(checkBox.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(defaultValue, checkBox.ClassList.Contains("bit-chb-ckd"));

        chbCheckbox.Click();

        Assert.AreEqual(isEnabled, clicked);
        Assert.AreEqual(isEnabled, changed);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true),
    ]
    public void BitCheckboxReversedTest(bool reversed)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var checkBox = component.Find(".bit-chb");

        if (reversed)
        {
            Assert.IsTrue(checkBox.ClassList.Contains("bit-chb-rvs"));
        }
        else
        {
            Assert.IsFalse(checkBox.ClassList.Contains("bit-chb-rvs"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void IndeterminateBitCheckboxShouldHaveCorrectClassNameIfIsEnabled(bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.DefaultIndeterminate, true);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var chb = component.Find(".bit-chb");
        var chbCheckbox = component.Find("input");
        chbCheckbox.Click();

        if (isEnabled)
        {
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-ind"));
        }
    }

    [TestMethod,
        DataRow("Detailed label"),
        DataRow(null)
    ]
    public void BitCheckboxAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var chbInput = component.Find("input");

        if (string.IsNullOrEmpty(ariaLabel) is false)
        {
            Assert.IsTrue(chbInput?.GetAttribute("aria-label")?.Equals(ariaLabel));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-label"));
        }
    }

    [TestMethod,
        DataRow("Detailed description"),
        DataRow(null)
    ]
    public void BitCheckboxAriaDescriptionTest(string ariaDescription)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var chbInput = component.Find("input");

        if (string.IsNullOrEmpty(ariaDescription) is false)
        {
            var descriptionId = chbInput.GetAttribute("aria-describedby");

            Assert.IsNotNull(descriptionId);

            var description = component.Find($"[id='{descriptionId}']");

            Assert.IsTrue(description.ClassList.Contains("bit-chb-dsc"));
            Assert.AreEqual(ariaDescription, description.TextContent);
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-describedby"));
        }
    }

    [TestMethod,
        DataRow("Detailed label"),
        DataRow(null)
    ]
    public void BitCheckboxAriaLabelledbyTest(string ariaLabelledby)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaLabelledby, ariaLabelledby);
        });

        var chbInput = component.Find("input");

        if (string.IsNullOrEmpty(ariaLabelledby) is false)
        {
            Assert.IsTrue(chbInput?.GetAttribute("aria-labelledby")?.Equals(ariaLabelledby));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-labelledby"));
        }
    }

    [TestMethod,
        DataRow("Title"),
        DataRow(null)
    ]
    public void BitCheckboxTitleTest(string title)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        var chbInput = component.Find("label");

        if (string.IsNullOrEmpty(title) is false)
        {
            Assert.IsTrue(chbInput?.GetAttribute("title")?.Equals(title));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("title"));
        }
    }

    [TestMethod,
        DataRow("Name"),
        DataRow(null)
    ]
    public void BitCheckboxNameTest(string name)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Name, name);
        });

        var chbInput = component.Find("input");

        if (name is not null)
        {
            Assert.IsTrue(chbInput?.GetAttribute("name")?.Equals(name));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("name"));
        }
    }

    [TestMethod,
        DataRow(3),
        DataRow(null)
    ]
    public void BitCheckboxAriaPosInSetTest(int? ariaPosInSet)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaPositionInSet, ariaPosInSet);
        });

        var chbInput = component.Find("input");

        if (ariaPosInSet is not null)
        {
            Assert.IsTrue(chbInput?.GetAttribute("aria-posinset")?.Equals(ariaPosInSet.ToString()));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-posinset"));
        }
    }

    [TestMethod,
        DataRow(3),
        DataRow(null)
    ]
    public void BitCheckboxAriaSetSizeTest(int? ariaSetSize)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaSetSize, ariaSetSize);
        });

        var chbInput = component.Find("input");

        if (ariaSetSize is not null)
        {
            Assert.IsTrue(chbInput?.GetAttribute("aria-setsize")?.Equals(ariaSetSize.ToString()));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-setsize"));
        }
    }

    [TestMethod,
        DataRow("Emoji2"),
        DataRow("MicrosoftFlowLogo"),
    ]
    public void BitCheckboxCustomCheckmarkIconTest(string checkmarkIconName)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.CheckIconName, checkmarkIconName);
        });

        var icon = component.Find(".bit-chb-box i.bit-icon");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{checkmarkIconName}"));
    }

    [TestMethod,
        DataRow("Icon aria-label"),
        DataRow(null)
    ]
    public void BitCheckboxCheckmarkIconAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.CheckIconAriaLabel, ariaLabel);
        });

        var icon = component.Find(".bit-chb-box i.bit-icon");

        if (ariaLabel is not null)
        {
            Assert.AreEqual(ariaLabel, icon.GetAttribute("aria-label"));
        }
        else
        {
            Assert.IsNull(icon.GetAttribute("aria-label"));
        }
    }

    [TestMethod,
        DataRow("Child with label tag <label>This is the label</label>"),
        DataRow("BitCheckBox (controlled)")
    ]
    public void BitCheckboxCustomChildContentTest(string childContent)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.ChildContent, childContent);
        });

        var chbChildContent = component.Find("label").ChildNodes;
        chbChildContent.MarkupMatches(childContent);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckBoxIsCheckedTwoWayBoundWithCustomHandlerTest(bool value)
    {
        BitCheckBoxIsChecked = value;

        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, HandleValueChanged);
        });

        var chb = component.Find("input");
        chb.Click();

        var expectedValue = !value;

        Assert.AreEqual(expectedValue, BitCheckBoxIsChecked);
    }

    [TestMethod]
    public void BitCheckBoxIsIndeterminateTwoWayBoundWithCustomHandlerTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.IndeterminateChanged, HandleIsIndeterminateChanged);
        });

        var chb = component.Find("input");
        chb.Click();

        Assert.IsFalse(BitCheckBoxIsIndeterminate);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckBoxValidationFormTest(bool value)
    {
        var component = RenderComponent<BitCheckboxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitCheckboxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(value ? 0 : 1, component.Instance.ValidCount);
        Assert.AreEqual(value ? 1 : 0, component.Instance.InvalidCount);

        var checkbox = component.Find("input");
        checkbox.Click();
        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckBoxValidationInvalidHtmlAttributeTest(bool value)
    {
        var component = RenderComponent<BitCheckboxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitCheckboxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var checkBoxInput = component.Find("input[type='checkbox']");
        Assert.IsFalse(checkBoxInput.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(checkBoxInput.HasAttribute("aria-invalid"), value);
        if (checkBoxInput.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", checkBoxInput.GetAttribute("aria-invalid"));
        }

        var checkBox = component.Find("input");
        checkBox.Click();

        Assert.AreEqual(value is false, checkBoxInput.HasAttribute("aria-invalid"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitCheckBoxValidationInvalidCssClassTest(bool value)
    {
        var component = RenderComponent<BitCheckboxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitCheckboxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var bitCheckBox = component.Find(".bit-chb");

        Assert.IsFalse(bitCheckBox.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(value, bitCheckBox.ClassList.Contains("bit-inv"));

        var checkBox = component.Find("input");
        checkBox.Click();

        Assert.AreEqual(value is false, bitCheckBox.ClassList.Contains("bit-inv"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxReadOnlyTest(bool readOnly)
    {
        var clicked = false;
        var changed = false;
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.OnClick, () => clicked = true);
            parameters.Add(p => p.OnChange, () => changed = true);
        });

        var chbInput = component.Find("input");

        if (readOnly)
        {
            Assert.AreEqual("true", chbInput.GetAttribute("aria-readonly"));
            Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-chb-rdl"));
        }
        else
        {
            Assert.IsNull(chbInput.GetAttribute("aria-readonly"));
            Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-rdl"));
        }

        chbInput.Click();

        Assert.AreEqual(readOnly is false, clicked);
        Assert.AreEqual(readOnly is false, changed);
        Assert.AreEqual(readOnly is false, component.Find(".bit-chb").ClassList.Contains("bit-chb-ckd"));
    }

    [TestMethod]
    public void BitCheckboxThreeStateShouldCycleThroughStates()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.ThreeState, true);
        });

        var chbInput = component.Find("input");

        var chb = component.Find(".bit-chb");
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ckd"));
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ind"));

        chbInput.Click();
        chb = component.Find(".bit-chb");
        Assert.IsTrue(chb.ClassList.Contains("bit-chb-ckd"));
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ind"));

        chbInput.Click();
        chb = component.Find(".bit-chb");
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ckd"));
        Assert.IsTrue(chb.ClassList.Contains("bit-chb-ind"));

        chbInput.Click();
        chb = component.Find(".bit-chb");
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ckd"));
        Assert.IsFalse(chb.ClassList.Contains("bit-chb-ind"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxOnChangingShouldBeCancellable(bool cancel)
    {
        var changed = false;
        BitCheckboxChangeArgs changingArgs = default!;
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.OnChanging, (BitCheckboxChangeArgs args) =>
            {
                changingArgs = args;
                args.Cancel = cancel;
            });
            parameters.Add(p => p.OnChange, () => changed = true);
        });

        component.Find("input").Click();

        Assert.IsNotNull(changingArgs);
        Assert.IsTrue(changingArgs.Value);
        Assert.IsFalse(changingArgs.Indeterminate);

        Assert.AreEqual(cancel is false, changed);
        Assert.AreEqual(cancel is false, component.Find(".bit-chb").ClassList.Contains("bit-chb-ckd"));
    }

    [TestMethod,
        DataRow(BitLabelPosition.Top, "bit-chb-ltp"),
        DataRow(BitLabelPosition.Bottom, "bit-chb-lbt"),
        DataRow(BitLabelPosition.Start, "bit-chb-lst"),
        DataRow(BitLabelPosition.End, "bit-chb-lnd"),
        DataRow(null, null)
    ]
    public void BitCheckboxLabelPositionTest(BitLabelPosition? labelPosition, string expectedClass)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, labelPosition);
        });

        var chb = component.Find(".bit-chb");

        if (expectedClass is not null)
        {
            Assert.IsTrue(chb.ClassList.Contains(expectedClass));
        }
        else
        {
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-ltp"));
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-lbt"));
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-lst"));
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-lnd"));
        }
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitCheckboxRequiredTest(bool required, bool isEnabled)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var chbInput = component.Find("input");

        Assert.AreEqual(required, chbInput.HasAttribute("required"));
        Assert.AreEqual(required && isEnabled, component.Find(".bit-chb").ClassList.Contains("bit-chb-req"));
    }

    [TestMethod]
    public void BitCheckboxUncheckedIconTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.UncheckedIconName, "Cancel");
        });

        Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-chb-uci"));
        Assert.IsTrue(component.Find(".bit-chb-box i").ClassList.Contains("bit-icon--Cancel"));

        component.Find("input").Click();

        Assert.IsTrue(component.Find(".bit-chb-box i").ClassList.Contains("bit-icon--Accept"));
    }

    [TestMethod,
        DataRow("Blocked"),
        DataRow(null)
    ]
    public void BitCheckboxIndeterminateIconTest(string indeterminateIconName)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Indeterminate, true);
            parameters.Add(p => p.IndeterminateIconName, indeterminateIconName);
        });

        var chb = component.Find(".bit-chb");
        var icon = component.Find(".bit-chb-box i");

        if (indeterminateIconName is not null)
        {
            Assert.IsTrue(chb.ClassList.Contains("bit-chb-cii"));
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{indeterminateIconName}"));
            Assert.IsFalse(icon.ClassList.Contains("bit-chb-idi"));
        }
        else
        {
            Assert.IsFalse(chb.ClassList.Contains("bit-chb-cii"));
            Assert.IsTrue(icon.ClassList.Contains("bit-chb-idi"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxAutoFocusTest(bool autoFocus)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, autoFocus);
        });

        Assert.AreEqual(autoFocus, component.Find("input").HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitCheckboxInputShouldBeFocusable()
    {
        var component = RenderComponent<BitCheckbox>();

        var chbInput = component.Find("input");

        Assert.IsFalse(chbInput.HasAttribute("hidden"));
        Assert.IsTrue(chbInput.ClassList.Contains("bit-chb-inp"));
    }

    private void HandleValueChanged(bool isChecked) => BitCheckBoxIsChecked = isChecked;

    private void HandleIsIndeterminateChanged(bool isIndeterminate) => BitCheckBoxIsIndeterminate = isIndeterminate;
}
