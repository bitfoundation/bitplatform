﻿using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitTextFieldTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, true, true),
            DataRow(Visual.Fluent, false, true, false),
            DataRow(Visual.Fluent, true, false, true),
            DataRow(Visual.Fluent, false, false, false),

            DataRow(Visual.Cupertino, true, true, true),
            DataRow(Visual.Cupertino, false, true, false),
            DataRow(Visual.Cupertino, true, false, true),
            DataRow(Visual.Cupertino, false, false, false),

            DataRow(Visual.Material, true, true, true),
            DataRow(Visual.Material, false, true, false),
            DataRow(Visual.Material, true, false, true),
            DataRow(Visual.Material, false, false, false)
        ]
        public void BitTextFieldShouldTakeCorrectTypeAndVisual(Visual visual, bool isEnabled, bool isMultiline, bool isRequired)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.IsMultiline, isMultiline);
                    parameters.Add(p => p.IsRequired, isRequired);
                });

            var bitTextField = component.Find(".bit-txt");
            var textField = component.Find(".txt-field");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitTextField.ClassList.Contains($"bit-txt-{isEnabledClass}-{visualClass}"));

            Assert.AreEqual(isMultiline ? "TEXTAREA" : "INPUT", textField.TagName);

            Assert.AreEqual(isRequired, textField.HasAttribute("required"));
            Assert.AreEqual(bitTextField.ClassList.Contains($"bit-txt-required-{visualClass}"), isRequired);
        }

        [DataTestMethod,
           DataRow(true, "hello world"),
           DataRow(false, "hello world")
       ]
        public void BitTextFieldShouldTakeValue(bool isMultiline, string value)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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

            var bitTextFieldLabel = com.Find(".bit-txt-label").TextContent;

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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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

        [DataTestMethod, DataRow(BitIcon.Emoji2)]
        public void BitTextFieldShouldAcceptIcon(BitIcon iconName)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.Type, BitTextFieldType.Password);
                    parameters.Add(p => p.CanRevealPassword, true);
                });

            var bitTextField = component.Find(".bit-txt");
            var textField = component.Find(".txt-field");
            var revealPasswordBtn = component.Find(".password-reveal-btn");
            var revealPasswordIcon = component.Find(".password-reveal-btn > span > i");

            Assert.AreEqual("Password", textField.GetAttribute("type"));
            Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--RedEye"));

            revealPasswordBtn.Click();

            Assert.AreEqual("Text", textField.GetAttribute("type"));
            Assert.IsTrue(revealPasswordIcon.ClassList.Contains($"bit-icon--Hide"));
        }

        [DataTestMethod,
            DataRow(true, false),
            DataRow(true, true),
            DataRow(false, false),
            DataRow(false, true),
        ]
        public void BitTextFieldMustRespondToTheClickEvent(bool isEnabled, bool isMultiline)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Prefix, prefix);
                });

            var bitTextFieldPrefix = component.Find(".txt-prefix > span");
            Assert.AreEqual(prefix, bitTextFieldPrefix.TextContent);
        }

        [DataTestMethod, DataRow("Suffix")]
        public void BitTextFieldShowSuffix(string suffix)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Suffix, suffix);
                });

            var bitTextFieldSuffix = component.Find(".txt-suffix > span");
            Assert.AreEqual(suffix, bitTextFieldSuffix.TextContent);
        }

        [DataTestMethod,
            DataRow(true, "hello world"),
            DataRow(false, "hello world")
        ]
        public void BitTextFieldShouldTakeDefaultValue(bool isMultiline, string defaultValue)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.DefaultValue, defaultValue);
                    parameters.Add(p => p.IsMultiline, isMultiline);
                });

            var bitTextField = component.Find(".txt-field");

            if (isMultiline)
            {
                Assert.AreEqual(bitTextField.TextContent, defaultValue);
            }
            else
            {
                Assert.AreEqual(bitTextField.GetAttribute("value"), defaultValue);
            }
        }

        [DataTestMethod, DataRow("test description")]
        public void BitTextFieldShouldTakeDescription(string description)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsUnderlined, isUnderlined);
                });

            var bitTextField = component.Find(".bit-txt");
            Assert.AreEqual(isUnderlined, bitTextField.ClassList.Contains($"bit-txt-underlined-fluent"));
        }

        [DataTestMethod,
           DataRow(true),
           DataRow(false)
        ]
        public void BitTextFieldShouldRespectHasBorderStyle(bool hasBorder)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.HasBorder, hasBorder);
                });

            var bitTextField = component.Find(".bit-txt");
            Assert.AreEqual(!hasBorder, bitTextField.ClassList.Contains($"bit-txt-no-border-fluent"));
        }

        [DataTestMethod, DataRow(5)]
        public void BitTextFieldShouldRespectRowsNumberWhenItIsMultiline(int rows)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
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
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsMultiline, true);
                    parameters.Add(p => p.IsResizable, isResizable);
                });

            var bitTextField = component.Find(".bit-txt");
            Assert.AreEqual(!isResizable, bitTextField.ClassList.Contains("bit-txt-multiline-fix-fluent"));
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
    }
}
