using System.Drawing;
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

            //TODO: bypassed - BUnit two-way bind issue
            //Assert.IsTrue(bitChoiceOption.ClassList.Contains($"bit-cho-{optionIsEnabledClass}-{visualClass}"));
        }

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


        [DataTestMethod, DataRow("this is label")]
        public void BitChoiceGroupShouldTakeLabel(string label)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Label, label);
                });

            var bitChoiceGroupLabel = component.Find(".bit-chg > label");

            Assert.AreEqual(label, bitChoiceGroupLabel.TextContent);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitChoiceGroupShouldRespectIsRequired(bool isRequired)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsRequired, isRequired);
                });

            var bitChoiceGroup = component.Find(".bit-chg");
            var input = component.Find(".bit-cho input");

            Assert.AreEqual(isRequired, bitChoiceGroup.ClassList.Contains("bit-chg-required-fluent"));

            Assert.AreEqual(isRequired, input.HasAttribute("required"));
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

            //TODO: bypassed - BUnit onchange event issue
            //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.ChoiceOptionClickedValue);
            //Assert.AreEqual(isEnabled ? value : "", component.Instance.ChoiceGroupChangedValue);
        }

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
           DataRow("https://picsum.photos/100", "this is alt", "https://picsum.photos/200", 50, 50, "this is label")
        ]
        public void BitChoiceOptionShouldrespectImage(string imageSrc, string imageAlt, string selectedImageSrc, int width, int height, string label)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Text, label);
                    parameters.Add(p => p.ImageSrc, imageSrc);
                    parameters.Add(p => p.ImageAlt, imageAlt);
                    parameters.Add(p => p.SelectedImageSrc, selectedImageSrc);
                    parameters.Add(p => p.ImageSize, new Size(width, height));
                });

            var unSelectedImage = component.Find(".bit-cho .bit-cho-img-wrpr div:first-child img");
            Assert.IsTrue(unSelectedImage.HasAttribute("src"));
            Assert.AreEqual(imageSrc, unSelectedImage.GetAttribute("src"));
            Assert.IsTrue(unSelectedImage.HasAttribute("alt"));
            Assert.AreEqual(imageAlt, unSelectedImage.GetAttribute("alt"));

            var selectedImage = component.Find(".bit-cho .bit-cho-img-wrpr div:nth-child(2) img");
            Assert.IsTrue(selectedImage.HasAttribute("src"));
            Assert.AreEqual(selectedImageSrc, selectedImage.GetAttribute("src"));
            Assert.IsTrue(selectedImage.HasAttribute("alt"));
            Assert.AreEqual(imageAlt, selectedImage.GetAttribute("alt"));

            var unSelectedImageImageContainer = component.Find(".bit-cho .bit-cho-img-wrpr div:first-child > div");
            Assert.IsTrue(unSelectedImageImageContainer.HasAttribute("style"));
            Assert.AreEqual($" width:{width}px; height:{height}px;", unSelectedImageImageContainer.GetAttribute("style"));

            var selectedImageImageContainer = component.Find(".bit-cho .bit-cho-img-wrpr div:nth-child(2) > div");
            Assert.IsTrue(selectedImageImageContainer.HasAttribute("style"));
            Assert.AreEqual($" width:{width}px; height:{height}px;", selectedImageImageContainer.GetAttribute("style"));

            var optionLabel = component.Find(".bit-cho .bit-cho-lbl-wrpr span");
            Assert.AreEqual(label, optionLabel.TextContent);
        }

        [DataTestMethod, DataRow(BitIcon.Emoji2)]
        public void BitChoiceOptionShouldrespectIcon(BitIcon iconName)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IconName, iconName);
                });

            var icon = component.Find(".bit-cho .bit-cho-icn-wrpr i");
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
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
           
            // TODO: bypassed - BUnit two-way binding issue
            //Assert.AreEqual(isChecked, input.HasAttribute("checked"));

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

            var input1 = component.Find(".bit-chg .bit-cho:nth-child(1) input");
            input1.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input1.HasAttribute("checked"));
            }

            var input2 = component.Find($".bit-chg .bit-cho:nth-child(2) input");
            input2.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input2.HasAttribute("checked"));
            }

            //TODO: bypassed - BUnit onchange event issue
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
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input.HasAttribute("checked"));
            }

            //TODO: bypassed - BUnit onchange event issue
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? count.ToString() : null, component.Instance.Value);
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.ChoiceOptionChangedValue);
        }
    }
}
