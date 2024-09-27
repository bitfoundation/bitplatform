using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

[TestClass]
public class BitToggleTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(true, true),
       DataRow(true, false),
       DataRow(false, true),
       DataRow(false, false)
   ]
    public void BitToggleTest(bool isEnabled, bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Value, value);
        });

        var bitToggle = com.Find(".bit-tgl");

        if (isEnabled)
        {
            Assert.IsFalse(bitToggle.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitToggle.ClassList.Contains("bit-dis"));
        }

        if (value)
        {
            Assert.IsTrue(bitToggle.ClassList.Contains("bit-tgl-chk"));
        }
        else
        {
            Assert.IsFalse(bitToggle.ClassList.Contains("bit-tgl-chk"));
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitToggleInlineLabelShouldHaveClassName(bool isInlineLabel)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Inline, isInlineLabel);
        });
        var bitToggle = com.Find(".bit-tgl");

        if (isInlineLabel)
        {
            Assert.IsTrue(bitToggle.ClassList.Contains("bit-tgl-inl"));
        }
    }

    [DataTestMethod, DataRow("Detailed AriaLabel")]
    public void BitToggleAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitToggleButton = com.Find("button");
        Assert.AreEqual(bitToggleButton.GetAttribute("aria-label"), ariaLabel);
    }

    [DataTestMethod,
        DataRow(true, "on", "off", "This is the first defaultText", "This is the first label"),
        DataRow(false, "on", "off", "This is the second defaultText", "This is the second label"),
        DataRow(true, "on", "off", null, "This is the Third label"),
        DataRow(false, "on", "off", "This is the fourth defaultText", null),
        DataRow(false, "on", "off", null, null),
        DataRow(false, null, "off", "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, "on", null, "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, null, null, "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, null, null, null, null),
    ]
    public void BitToggleAriaLabelledbyTest(bool value, string onText, string offText, string defaultText, string label)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnText, onText);
            parameters.Add(p => p.OffText, offText);
            parameters.Add(p => p.Text, defaultText);
            parameters.Add(p => p.AriaLabel, string.Empty);
            parameters.Add(p => p.Label, label);
        });

        var button = com.Find("button");
        var labelId = button.Id.Replace("button", "label");
        var stateTextId = button.Id.Replace("button", "state-text");

        var ariaLabelledById = string.Empty;
        var stateText = (value ? onText : offText) ?? defaultText ?? string.Empty;

        if (label.HasValue())
        {
            ariaLabelledById = labelId;
        }

        if (stateText.HasValue())
        {
            ariaLabelledById = ariaLabelledById.HasValue() ? $"{labelId} {stateTextId}" : stateTextId;
        }

        Assert.AreEqual(button.GetAttribute("aria-labelledby"), ariaLabelledById);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleAriaCheckedTest(bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
        });

        var ariaChecked = value ? "true" : "false";
        var bitToggleButton = com.Find("button");
        Assert.AreEqual(bitToggleButton.GetAttribute("aria-checked"), ariaChecked);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("Foo"),
        DataRow("Bar")
        ]
    public void BitToggleRoleTest(string role)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            if (role is not null)
            {
                parameters.Add(p => p.Role, role);
            }
        });

        var bitToggleButton = com.Find("button");
        Assert.AreEqual(bitToggleButton.GetAttribute("role"), role ?? "switch");
    }

    [DataTestMethod, DataRow("This is label")]
    public void BitToggleLabel(string label)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitToggleLabel = com.Find("label");
        Assert.AreEqual(bitToggleLabel.TextContent, label);
    }

    [DataTestMethod, DataRow("<div>This is labelTemplate</div>")]
    public void BitToggleMarkupLabelTest(string labelTemplate)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var bitToggleLabelChild = com.Find("label").ChildNodes;
        bitToggleLabelChild.MarkupMatches(labelTemplate);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationFormTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value ? 0 : 1, com.Instance.ValidCount);
        Assert.AreEqual(value ? 1 : 0, com.Instance.InvalidCount);

        var button = com.Find("button");
        button.Click();
        form.Submit();

        Assert.AreEqual(1, com.Instance.ValidCount);
        Assert.AreEqual(1, com.Instance.InvalidCount);
        Assert.AreEqual(com.Instance.ValidCount, com.Instance.InvalidCount);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationInvalidHtmlAttributeTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var checkBox = com.Find("input[type='checkbox']");
        Assert.IsFalse(checkBox.HasAttribute("aria-invalid"));

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value, checkBox.HasAttribute("aria-invalid"));
        if (checkBox.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", checkBox.GetAttribute("aria-invalid"));
        }

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(value is false, checkBox.HasAttribute("aria-invalid"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationInvalidCssClassTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var bitToggle = com.Find(".bit-tgl");

        Assert.IsFalse(bitToggle.ClassList.Contains("bit-inv"));

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value, bitToggle.ClassList.Contains("bit-inv"));

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(value is false, bitToggle.ClassList.Contains("bit-inv"));
    }
}
