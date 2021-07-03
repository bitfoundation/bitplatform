using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitChoiceOptionTests : BunitTestContext
    {

        [DataTestMethod,
           DataRow(Visual.Fluent, true),
           DataRow(Visual.Fluent, false),

           DataRow(Visual.Cupertino, true),
           DataRow(Visual.Cupertino, false),

           DataRow(Visual.Material, true),
           DataRow(Visual.Material, false)
       ]
        public void BitChoiceOptionShouldTakeCorrectVisual(Visual visual, bool isEnabled)
        {
            var component = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitChoiceOption = component.Find(".bit-cho");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitChoiceOption.ClassList.Contains($"bit-cho-{isEnabledClass}-{visualClass}"));
        }

        [DataTestMethod, DataRow("test value")]
        public void BitChoiceOptionShouldTakeCorrectValue(string value)
        {
            var component = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                });

            var bitChoiceOptionInput = component.Find(".bit-cho input");

            Assert.IsTrue(bitChoiceOptionInput.HasAttribute("value"));
            Assert.AreEqual(value, bitChoiceOptionInput.GetAttribute("value"));
        }

        [DataTestMethod, DataRow("hello world")]
        public void BitChoiceOptionShouldUseTextInLabel(string text)
        {
            var component = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.Text, text);
                });

            var bitChoiceOptionLabelText = component.Find(".bit-cho label span");

            Assert.AreEqual(text, bitChoiceOptionLabelText.TextContent);
        }

        [DataTestMethod, DataRow("testName")]
        public void BitChoiceOptionShouldAcceptGivenName(string name)
        {
            var component = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.Name, name);
                });

            var bitChoiceOptionInput = component.Find(".bit-cho input");

            Assert.IsTrue(bitChoiceOptionInput.HasAttribute("name"));
            Assert.AreEqual(name, bitChoiceOptionInput.GetAttribute("name"));
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitChoiceOptionShouldAcceptGivenCheckStatus(bool isChecked)
        {
            var component = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsChecked, isChecked);
                });

            var bitChoiceOptionInput = component.Find(".bit-cho input");

            Assert.AreEqual(isChecked, bitChoiceOptionInput.HasAttribute("checked"));

        }
    }
}
