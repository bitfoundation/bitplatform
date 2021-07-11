using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitChoiceGroupTests : BunitTestContext
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
          DataRow(Visual.Material, false, false)

      ]
        public void BitChoiceGroupShouldTakeCorrectVisual(Visual visual, bool groupIsEnabled, bool optionIsEnabled)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.ChoiceGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.ChoiceOptionIsEnabled, optionIsEnabled);
                });

            var bitChoiceGroup = component.Find(".bit-chg");
            var bitChoiceOption = component.Find(".bit-cho");

            var groupIsEnabledClass = groupIsEnabled ? "enabled" : "disabled";
            var optionIsEnabledClass = (optionIsEnabled && groupIsEnabled) ? "enabled" : "disabled";

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-chg-{groupIsEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitChoiceOption.ClassList.Contains($"bit-cho-{optionIsEnabledClass}-{visualClass}"));
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="name"></param>
        [DataTestMethod, DataRow("groupName", "optionName")]
        public void BitChoiceGroupShouldGiveNameToChoiceOptions(string groupName, string optionName)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.ChoiceGroupName, groupName);
                    parameters.Add(p => p.ChoiceOptionName, optionName);
                });

            var bitChoiceOption = component.Find(".bit-chg .bit-cho");

            Assert.IsTrue(bitChoiceOption.FirstElementChild.HasAttribute("name"));
            Assert.AreEqual(optionName, bitChoiceOption.FirstElementChild.GetAttribute("name"));
        }

        [DataTestMethod,
           DataRow(true, "value1"),
           DataRow(false, "value2")
        ]
        public void BitChoiceOptionMustRespondToTheClickAndChangeValueEvent(bool isEnabled, string value)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                    parameters.Add(p => p.ChoiceGroupIsEnabled, isEnabled);
                });

            var input = component.Find(".bit-chg .bit-cho input");

            input.Click();

            //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.ChoiceOptionClickedValue);
            //Assert.AreEqual(isEnabled ? value : "", component.Instance.ChoiceGroupChangedValue);
        }



        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////

        [DataTestMethod, DataRow("test value")]
        public void BitChoiceOptionShouldTakeCorrectValue(string value)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                });

            var input = component.Find(".bit-cho input");

            Assert.IsTrue(input.HasAttribute("value"));
            Assert.AreEqual(value, input.GetAttribute("value"));
        }

        [DataTestMethod, DataRow("hello world")]
        public void BitChoiceOptionShouldUseTextInLabel(string text)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Text, text);
                });

            var bitChoiceOptionLabelText = component.Find(".bit-cho label span");

            Assert.AreEqual(text, bitChoiceOptionLabelText.TextContent);
        }

        [DataTestMethod,
          DataRow(true),
          DataRow(false)
        ]
        public void BitChoiceOptionShouldAcceptGivenCheckStatus(bool isChecked)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsChecked, isChecked);
                });

            var input = component.Find(".bit-cho input");

            Assert.AreEqual(isChecked, input.HasAttribute("checked"));

        }

        [DataTestMethod,
           DataRow(true, true),
           DataRow(true, false),
           DataRow(false, true),
           DataRow(false, false),
        ]
        public void BitChoiceOptionMustRespondToTheClickEvent(bool groupIsEnabled, bool optionIsEnabled)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.ChoiceGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.ChoiceOptionIsEnabled, optionIsEnabled);
                });

            var bitChoiceOptionInput = component.Find(".bit-cho input");

            bitChoiceOptionInput.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //Assert.IsTrue(bitChoiceOptionInput.HasAttribute("checked"));
            }

            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.ChoiceOptionClickedValue);
        }

        [DataTestMethod,
          DataRow(true, true, 2),
           DataRow(true, false, 2),
           DataRow(false, true, 2),
           DataRow(false, false, 2),
       ]
        public void BitChoiceOptionMustRespondToTheChangeEvent(bool groupIsEnabled, bool optionIsEnabled, int count)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.ChoiceGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.ChoiceOptionIsEnabled, optionIsEnabled);
                });

            var input = component.Find(".bit-cho input");

            input.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //Assert.IsTrue(input.HasAttribute("checked"));
            }

            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? count.ToString() : null, component.Instance.Value);
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.ChoiceOptionChangedValue);
        }
    }
}
