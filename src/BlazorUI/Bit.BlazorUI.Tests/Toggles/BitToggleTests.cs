using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Toggles;

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
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Value, value);
        });

        var bitToggle = com.Find(".bit-tgl");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var isCheckedClass = value ? "checked" : "unchecked";

        Assert.IsTrue(bitToggle.ClassList.Contains($"{isEnabledClass}-{isCheckedClass}"));
    }

    [DataTestMethod,
        DataRow("", ""),
        DataRow("", null),
        DataRow(null, ""),
        DataRow(null, null),
        DataRow("On", "Off"),
        DataRow("On", ""),
        DataRow("On", null),
        DataRow("", "Off"),
        DataRow(null, "Off")
    ]
    public void BitToggleWithoutOnOffTextShouldHaveClassName(string onText, string offText)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.OnText, onText);
            parameters.Add(p => p.OffText, offText);
        });
        var bitToggle = com.Find(".bit-tgl");

        if (onText.HasNoValue() || offText.HasNoValue())
        {
            Assert.IsTrue(bitToggle.ClassList.Contains($"noonoff"));
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitToggleInlineLabelShouldHaveClassName(bool isInlineLabel)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.IsInlineLabel, isInlineLabel);
        });
        var bitToggle = com.Find(".bit-tgl");

        if (isInlineLabel)
        {
            Assert.IsTrue(bitToggle.ClassList.Contains("inline"));
        }
    }

    [DataTestMethod, DataRow("Detailed AriaLabel")]
    public void BitToggleAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitToggleButton = com.Find(".bit-tgl button");
        Assert.AreEqual(bitToggleButton.GetAttribute("aria-label"), ariaLabel);
    }

    [DataTestMethod,
        DataRow(true, "on", "off", "This is the first defaultText", "This is the first label"),
        DataRow(false, "off", "on", "This is the second defaultText", "This is the second label"),
        DataRow(true, "on", "on", "This is the Third defaultText", "This is the Third label"),
        DataRow(false, "off", "off", "This is the fourth defaultText", "This is the fourth label")
    ]
    public void BitToggleAriaLabelledbyTest(bool value, string onText, string offText, string defaultText, string label)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnText, onText);
            parameters.Add(p => p.OffText, offText);
            parameters.Add(p => p.DefaultText, defaultText);
            parameters.Add(p => p.AriaLabel, string.Empty);
            parameters.Add(p => p.Label, label);
        });

        var bitToggleButton = com.Find("button");

      
        var labelId = bitToggleButton.Id.Replace("Button", "Label");
        
        var stateTextId = bitToggleButton.Id.Replace("Button", "StateText");

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

        var aa = bitToggleButton.GetAttribute("aria-labelledby");

        Assert.AreEqual(bitToggleButton.GetAttribute("aria-labelledby"), ariaLabelledById);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleAriaCheckedTest(bool value)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
        });

        var ariaChecked = value ? "true" : "false";
        var bitToggleButton = com.Find("button");
        Assert.AreEqual(bitToggleButton.GetAttribute("aria-checked"), ariaChecked);
    }

    [DataTestMethod, DataRow("Switch")]
    public void BitToggleRoleTest(string role)
    {
        var com = RenderComponent<BitToggleTest>(parameters =>
        {
            parameters.Add(p => p.Role, role);
        });

        var bitToggleButton = com.Find("button");
        Assert.AreEqual(bitToggleButton.GetAttribute("role"), role);
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
        var com = RenderComponent<BitToggleTest>(parameters =>
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

        Assert.AreEqual(com.Instance.ValidCount, value ? 0 : 1);
        Assert.AreEqual(com.Instance.InvalidCount, value ? 1 : 0);

        var button = com.Find("button");
        button.Click();
        form.Submit();

        Assert.AreEqual(com.Instance.ValidCount, 1);
        Assert.AreEqual(com.Instance.InvalidCount, 1);
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

        Assert.AreEqual(checkBox.HasAttribute("aria-invalid"), value);
        if (checkBox.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual(checkBox.GetAttribute("aria-invalid"), "true");
        }

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(checkBox.HasAttribute("aria-invalid"), !value);
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

        Assert.IsFalse(bitToggle.ClassList.Contains("invalid"));

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(bitToggle.ClassList.Contains("invalid"), value);

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(bitToggle.ClassList.Contains("invalid"), !value);
    }
}
