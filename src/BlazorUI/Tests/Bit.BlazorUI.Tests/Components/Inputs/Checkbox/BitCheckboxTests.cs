using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
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
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var chbInput = component.Find("input");

        Assert.AreEqual(required && isEnabled, chbInput.HasAttribute("required"));
        Assert.AreEqual(required && isEnabled, component.Find(".bit-chb").ClassList.Contains("bit-chb-req"));
    }

    /// <summary>
    /// The native disabled attribute is what keeps a disabled checkbox out of its form and out of that
    /// form's validation, so a checkbox kept focusable - which trades that attribute away - has to be kept
    /// out of both by hand. An unchecked required one would otherwise block the very submit it cannot join.
    /// </summary>
    [TestMethod]
    public void BitCheckboxDisabledButFocusableIsNotSubmittedOrRequiredTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.Name, "terms");
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AllowDisabledFocus, true);
        });

        var input = component.Find("input");

        Assert.IsFalse(input.HasAttribute("disabled"));
        Assert.IsFalse(input.HasAttribute("name"));
        Assert.IsFalse(input.HasAttribute("required"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));

        Assert.AreEqual("terms", component.Find("input").GetAttribute("name"));
        Assert.IsTrue(component.Find("input").HasAttribute("required"));
    }

    /// <summary>
    /// The asterisk is drawn after the label, so a checkbox with no label of any kind has nowhere to put
    /// one and says it is required through the native attribute alone. A templated label is a label too.
    /// </summary>
    [TestMethod]
    public void BitCheckboxRequiredMarkerNeedsALabelTest()
    {
        var unlabeled = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.AriaLabel, "Accept");
        });

        Assert.IsTrue(unlabeled.Find("input").HasAttribute("required"));
        Assert.IsFalse(unlabeled.Find(".bit-chb").ClassList.Contains("bit-chb-req"));

        var templated = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(builder => builder.AddContent(0, "Terms")));
        });

        Assert.IsTrue(templated.Find(".bit-chb").ClassList.Contains("bit-chb-req"));
        Assert.IsTrue(templated.Find(".bit-chb-tpl").TextContent.Contains("Terms"));
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

    /// <summary>
    /// The visible description is a line of its own outside the label - inside it the sentence would join
    /// the accessible name of the checkbox instead of being read after it - and it is pointed at through
    /// aria-describedby.
    /// </summary>
    [TestMethod]
    public void BitCheckboxDescriptionTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Auto renew");
            parameters.Add(p => p.Description, "Renewed one day before it expires.");
        });

        var description = component.Find(".bit-chb-des");

        Assert.AreEqual("Renewed one day before it expires.", description.TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-chb-hds"));
        Assert.IsNull(description.Closest("label"));

        var describedBy = component.Find("input").GetAttribute("aria-describedby");

        Assert.IsNotNull(describedBy);
        CollectionAssert.Contains(describedBy.Split(' '), description.Id);
    }

    [TestMethod]
    public void BitCheckboxDescriptionTemplateTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Auto renew");
            parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Templated");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("Templated", component.Find(".bit-chb-des b").TextContent);
    }

    /// <summary>
    /// aria-describedby is a list of ids, so everything that describes the checkbox ends up in it together,
    /// in the order the three are read: the visible line, the screen-reader-only one, then whatever the page
    /// already shows and pointed at by hand.
    /// </summary>
    [TestMethod]
    public void BitCheckboxDescribedByComposesEveryDescriptionTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Auto renew");
            parameters.Add(p => p.Description, "Visible");
            parameters.Add(p => p.AriaDescription, "Announced");
            parameters.Add(p => p.AriaDescribedby, "external-hint");
        });

        var ids = component.Find("input").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(3, ids.Length);
        Assert.AreEqual(component.Find(".bit-chb-des").Id, ids[0]);
        Assert.AreEqual(component.Find(".bit-chb-dsc").Id, ids[1]);
        Assert.AreEqual("external-hint", ids[2]);
    }

    /// <summary>
    /// A null written over a splatted attribute removes it rather than leaving it alone, so the aria-* the
    /// component also writes itself are resolved against what InputHtmlAttributes wrote.
    /// </summary>
    [TestMethod]
    public void BitCheckboxKeepsTheAriaAttributesWrittenByHandTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object>
            {
                { "aria-label", "Written by hand" },
                { "aria-describedby", "external-hint" }
            });
        });

        var input = component.Find("input");

        Assert.AreEqual("Written by hand", input.GetAttribute("aria-label"));
        Assert.AreEqual("external-hint", input.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCheckboxAriaLabelParameterWinsOverTheSplattedOneTest()
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "From the parameter");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object>
            {
                { "aria-label", "Written by hand" }
            });
        });

        Assert.AreEqual("From the parameter", component.Find("input").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxFullWidthTest(bool fullWidth)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        Assert.AreEqual(fullWidth, component.Find(".bit-chb").ClassList.Contains("bit-chb-fwi"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxNoWrapTest(bool noWrap)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "A label long enough to need more than one line");
            parameters.Add(p => p.NoWrap, noWrap);
        });

        Assert.AreEqual(noWrap, component.Find(".bit-chb").ClassList.Contains("bit-chb-nwr"));
    }

    /// <summary>
    /// The focus ring is drawn around the box, which a custom face replaces - so the root says it has none
    /// and the stylesheet can put the ring around the whole face instead of nowhere.
    /// </summary>
    [TestMethod]
    public void BitCheckboxCustomContentIsMarkedAsHavingNoBoxTest()
    {
        var withBox = RenderComponent<BitCheckbox>(parameters => parameters.Add(p => p.Label, "Label"));

        Assert.IsFalse(withBox.Find(".bit-chb").ClassList.Contains("bit-chb-cct"));

        var custom = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Face")));
        });

        Assert.IsTrue(custom.Find(".bit-chb").ClassList.Contains("bit-chb-cct"));
    }

    [TestMethod,
        DataRow("3"),
        DataRow(null)
    ]
    public void BitCheckboxTabIndexTest(string tabIndex)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        Assert.AreEqual(tabIndex, component.Find("input").GetAttribute("tabindex"));
    }

    /// <summary>
    /// A disabled checkbox is out of the tab order, which is also where an autofocus must not pull the
    /// focus - unless AllowDisabledFocus keeps it in, which trades the native attribute for aria-disabled.
    /// </summary>
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitCheckboxAllowDisabledFocusTest(bool allowDisabledFocus)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.Label, "Label");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var input = component.Find("input");

        Assert.AreEqual(allowDisabledFocus is false, input.HasAttribute("disabled"));
        Assert.AreEqual(allowDisabledFocus, input.HasAttribute("autofocus"));
        Assert.AreEqual("true", input.GetAttribute("aria-disabled"));
    }

    /// <summary>
    /// A disabled checkbox that stays focusable carries no native disabled attribute, so its click has to
    /// be turned away by the handler rather than by the browser.
    /// </summary>
    [TestMethod]
    public void BitCheckboxDisabledButFocusableDoesNotToggleTest()
    {
        var changed = false;
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AllowDisabledFocus, true);
            parameters.Add(p => p.OnChange, () => changed = true);
        });

        component.Find("input").Click();

        Assert.IsFalse(changed);
        Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-ckd"));
    }

    [TestMethod]
    public void BitCheckboxFocusCallbacksTest()
    {
        var focused = false;
        var blurred = false;
        var focusedIn = false;
        var focusedOut = false;

        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => focused = true);
            parameters.Add(p => p.OnFocusIn, () => focusedIn = true);
            parameters.Add(p => p.OnFocusOut, () => focusedOut = true);
            parameters.Add(p => p.OnBlur, () => blurred = true);
        });

        var input = component.Find("input");

        input.Focus();
        input.FocusIn();
        input.FocusOut();
        input.Blur();

        Assert.IsTrue(focused);
        Assert.IsTrue(focusedIn);
        Assert.IsTrue(focusedOut);
        Assert.IsTrue(blurred);
    }

    /// <summary>
    /// The glyph is decorative next to the state the input itself announces, so it is hidden from assistive
    /// technologies - unless it was given a name of its own, which is an explicit ask for it to be read.
    /// </summary>
    [TestMethod,
        DataRow("Agreed"),
        DataRow(null)
    ]
    public void BitCheckboxCheckIconIsHiddenUnlessNamedTest(string ariaLabel)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.CheckIconAriaLabel, ariaLabel);
        });

        var icon = component.Find(".bit-chb-box i");

        if (ariaLabel is null)
        {
            Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
            Assert.IsNull(icon.GetAttribute("role"));
        }
        else
        {
            Assert.IsNull(icon.GetAttribute("aria-hidden"));
            Assert.AreEqual("img", icon.GetAttribute("role"));
        }
    }

    /// <summary>
    /// The value ends up in the attribute a form posts, so it is the invariant "true"/"false" rather than
    /// whatever ToString would give it.
    /// </summary>
    [TestMethod,
        DataRow(true, "true"),
        DataRow(false, "false")
    ]
    public void BitCheckboxPostedValueTest(bool value, string expected)
    {
        var component = RenderComponent<BitCheckbox>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, value);
        });

        Assert.AreEqual(expected, component.Find("input").GetAttribute("value"));
    }

    private void HandleValueChanged(bool isChecked) => BitCheckBoxIsChecked = isChecked;

    private void HandleIsIndeterminateChanged(bool isIndeterminate) => BitCheckBoxIsIndeterminate = isIndeterminate;
}
