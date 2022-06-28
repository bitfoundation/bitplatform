using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Toggles
{
    [TestClass]
    public class BitToggleTests : BunitTestContext
    {
        [DataTestMethod,
           DataRow(Visual.Fluent, true, true),
           DataRow(Visual.Fluent, true, false),
           DataRow(Visual.Fluent, false, true),
           DataRow(Visual.Fluent, false, false),

           DataRow(Visual.Cupertino, true, true),
           DataRow(Visual.Cupertino, true, false),
           DataRow(Visual.Cupertino, false, true),
           DataRow(Visual.Cupertino, false, false),

           DataRow(Visual.Material, true, true),
           DataRow(Visual.Material, true, false),
           DataRow(Visual.Material, false, true),
           DataRow(Visual.Material, false, false),
       ]
        public void BitToggleTest(Visual visual, bool isEnabled, bool value)
        {
            var com = RenderComponent<BitToggleTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Value, value);
            });

            var bitToggle = com.Find(".bit-tgl");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var ischeckedClass = value ? "checked" : "unchecked";

            Assert.IsTrue(bitToggle.ClassList.Contains($"bit-tgl-{isEnabledClass}-{ischeckedClass}-{visualClass}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, "", ""),
            DataRow(Visual.Fluent, "", null),
            DataRow(Visual.Fluent, null, ""),
            DataRow(Visual.Fluent, null, null),
            DataRow(Visual.Fluent, "On", "Off"),
            DataRow(Visual.Fluent, "On", ""),
            DataRow(Visual.Fluent, "On", null),
            DataRow(Visual.Fluent, "", "Off"),
            DataRow(Visual.Fluent, null, "Off"),

            DataRow(Visual.Cupertino, "", ""),
            DataRow(Visual.Cupertino, "", null),
            DataRow(Visual.Cupertino, null, ""),
            DataRow(Visual.Cupertino, null, null),
            DataRow(Visual.Cupertino, "On", "Off"),
            DataRow(Visual.Cupertino, "On", ""),
            DataRow(Visual.Cupertino, "On", null),
            DataRow(Visual.Cupertino, "", "Off"),
            DataRow(Visual.Cupertino, null, "Off"),

            DataRow(Visual.Material, "", ""),
            DataRow(Visual.Material, "", null),
            DataRow(Visual.Material, null, ""),
            DataRow(Visual.Material, null, null),
            DataRow(Visual.Material, "On", "Off"),
            DataRow(Visual.Material, "On", ""),
            DataRow(Visual.Material, "On", null),
            DataRow(Visual.Material, "", "Off"),
            DataRow(Visual.Material, null, "Off"),
        ]
        public void BitToggle_WithoutOnOffText_ShouldHaveClassName(Visual visual, string onText, string offText)
        {
            var com = RenderComponent<BitToggleTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.OnText, onText);
                parameters.Add(p => p.OffText, offText);
            });
            var bitToggle = com.Find(".bit-tgl");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (onText.HasNoValue() || offText.HasNoValue())
            {
                Assert.IsTrue(bitToggle.ClassList.Contains($"bit-tgl-noonoff-{visualClass}"));
            }
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false),
        ]
        public void BitToggle_InlineLabrl_ShouldHaveClassName(Visual visual, bool isInlioneLabel)
        {
            var com = RenderComponent<BitToggleTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsInlineLabel, isInlioneLabel);
            });
            var bitToggle = com.Find(".bit-tgl");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (isInlioneLabel)
            {
                Assert.IsTrue(bitToggle.ClassList.Contains($"bit-tgl-inline-{visualClass}"));
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
        public void BitToggleAriaLabelledyTest(bool value, string onText, string offText, string defaultText, string label)
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

            var bitToggleButton = com.Find(".bit-tgl button");
            var bitToggleButtonId = bitToggleButton.Id;
            var labelId = bitToggleButtonId + "-label";
            var stateTextId = bitToggleButtonId + "-stateText";
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
            var bitToggleButton = com.Find(".bit-tgl button");
            Assert.AreEqual(bitToggleButton.GetAttribute("aria-checked"), ariaChecked);
        }

        [DataTestMethod, DataRow("Switch")]
        public void BitToggleRoleTest(string role)
        {
            var com = RenderComponent<BitToggleTest>(parameters =>
            {
                parameters.Add(p => p.Role, role);
            });

            var bitToggleButton = com.Find(".bit-tgl button");
            Assert.AreEqual(bitToggleButton.GetAttribute("role"), role);
        }

        [DataTestMethod, DataRow("This is label")]
        public void BitToggleLabel(string label)
        {
            var com = RenderComponent<BitToggle>(parameters =>
            {
                parameters.Add(p => p.Label, label);
            });

            var bitToggleLabel = com.Find(".bit-tgl > label");
            Assert.AreEqual(bitToggleLabel.TextContent, label);
        }

        [DataTestMethod, DataRow("<div>This is labelFragment</div>")]
        public void BitToggleMarkupLabelTest(string labelFragment)
        {
            var com = RenderComponent<BitToggleTest>(parameters =>
            {
                parameters.Add(p => p.LabelFragment, labelFragment);
            });

            var bitToggleLabelChild = com.Find(".bit-tgl > label").ChildNodes;
            bitToggleLabelChild.MarkupMatches(labelFragment);
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
            DataRow(Visual.Fluent, true),
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Cupertino, true),
            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Material, true),
            DataRow(Visual.Material, false),
        ]
        public void BitToggleValidationInvalidCssClassTest(Visual visual, bool value)
        {
            var com = RenderComponent<BitToggleValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, true);
            });

            var bitToggle = com.Find(".bit-tgl");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsFalse(bitToggle.ClassList.Contains($"bit-tgl-invalid-{visualClass}"));

            var form = com.Find("form");
            form.Submit();

            Assert.AreEqual(bitToggle.ClassList.Contains($"bit-tgl-invalid-{visualClass}"), value);

            var button = com.Find("button");
            button.Click();

            Assert.AreEqual(bitToggle.ClassList.Contains($"bit-tgl-invalid-{visualClass}"), !value);
        }
    }
}
