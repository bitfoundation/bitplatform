using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitTextFieldTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, true),
            DataRow(Visual.Fluent, false, true),
            DataRow(Visual.Fluent, true, false),
            DataRow(Visual.Fluent, false, false),

            DataRow(Visual.Cupertino, true, true),
            DataRow(Visual.Cupertino, false, true),
            DataRow(Visual.Cupertino, true, false),
            DataRow(Visual.Cupertino, false, false),

            DataRow(Visual.Material, true, true),
            DataRow(Visual.Material, false, true),
            DataRow(Visual.Material, true, false),
            DataRow(Visual.Material, false, false)
        ]
        public void BitTextFieldShouldTakeCorrectTypeAndVisual(Visual visual, bool isEnabled, bool isMultiLine)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                });

            var bitTextField = component.Find(".bit-txt");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitTextField.ClassList.Contains($"bit-txt-{isEnabledClass}-{visualClass}"));

            Assert.AreEqual(isMultiLine ? "TEXTAREA" : "INPUT", bitTextField.FirstElementChild.TagName);
        }

        [DataTestMethod,
           DataRow(true, "hello world"),
           DataRow(false, "hello world")
       ]
        public void BitTextFieldShouldTaketValue(bool isMultiLine, string value)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                });

            var bitTextField = component.Find(".bit-txt");

            if (isMultiLine)
            {
                Assert.AreEqual(bitTextField.FirstElementChild.InnerHtml, value);
            }
            else
            {
                Assert.AreEqual(bitTextField.FirstElementChild.GetAttribute("value"), value);
            }
        }

        [DataTestMethod,
            DataRow(15, true),
            DataRow(15, false)
        ]
        public void BitTextFieldShouldTakeMaxLength(int maxLength, bool isMultiLine)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.MaxLength, maxLength);
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                });

            var bitTextField = component.Find(".bit-txt");

            Assert.IsTrue(bitTextField.FirstElementChild.HasAttribute("maxlength"));
            Assert.AreEqual(bitTextField.FirstElementChild.GetAttribute("maxlength"), maxLength.ToString());
        }

        [DataTestMethod,
            DataRow("this is placeholder", true),
            DataRow("this is placeholder", false),
        ]
        public void BitTextFieldShouldTakePlaceHolder(string placeholder, bool isMultiLine)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.Placeholder, placeholder);
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                });

            var bitTextField = component.Find(".bit-txt");

            Assert.IsTrue(bitTextField.FirstElementChild.HasAttribute("placeholder"));

            Assert.AreEqual(bitTextField.FirstElementChild.GetAttribute("placeholder"), placeholder);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitTextFieldShouldRespectReadOnly(bool isMultiLine)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                    parameters.Add(p => p.IsReadOnly, true);
                });

            var bitTextField = component.Find(".bit-txt");

            Assert.IsTrue(bitTextField.FirstElementChild.HasAttribute("readonly"));            
        }

        [DataTestMethod,
            DataRow("Emoji2")
        ]
        public void BitTextFieldShouldAcceptIcon(string iconName)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IconName, iconName);
                });

            var bitTextFieldIcon = component.Find(".bit-txt span i");

            Assert.IsTrue(bitTextFieldIcon.ClassList.Contains($"bit-icon--{iconName}"));
        }

        [DataTestMethod,
            DataRow(true, false),
            DataRow(true, true),
            DataRow(false, false),
            DataRow(false, true),
        ]
        public void BitTextFieldCanRevealPassword(bool isEnabled, bool isMultiLine)
        {
            var component = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.IsMultiLine, isMultiLine);
                    parameters.Add(p => p.Type, TextFieldType.Password);
                    parameters.Add(p => p.CanRevealPassword, true);
                });

            var bitTextField = component.Find(".bit-txt");
            var bitTextFieldRevealPassword = component.Find(".bit-txt > span");

            Assert.AreEqual("Password", bitTextField.FirstElementChild.GetAttribute("type"));
            Assert.IsTrue(bitTextFieldRevealPassword.FirstElementChild.ClassList.Contains($"bit-icon--RedEye"));

            bitTextFieldRevealPassword.Click();

            Assert.AreEqual("Text", bitTextField.FirstElementChild.GetAttribute("type"));
            Assert.IsTrue(bitTextFieldRevealPassword.FirstElementChild.ClassList.Contains($"bit-icon--Hide"));
        }
    }
}
