using System.Drawing;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitRadioButtonGroupTests : BunitTestContext
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
        public void BitRadioButtonGroupShouldTakeCorrectVisual(Visual visual, bool groupIsEnabled, bool optionIsEnabled)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.RadioButtonGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.RadioButtonOptionIsEnabled, optionIsEnabled);
                });

            var bitRadioButtonGroup = component.Find(".bit-rbg");
            var bitRadioButtonOption = component.Find(".bit-rbo");

            var groupIsEnabledClass = groupIsEnabled ? "enabled" : "disabled";
            var optionIsEnabledClass = (optionIsEnabled && groupIsEnabled) ? "enabled" : "disabled";

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitRadioButtonGroup.ClassList.Contains($"bit-rbg-{groupIsEnabledClass}-{visualClass}"));

            //TODO: bypassed - BUnit two-way bind issue
            //Assert.IsTrue(bitRadioButtonOption.ClassList.Contains($"bit-rbo-{optionIsEnabledClass}-{visualClass}"));
        }

        [DataTestMethod, DataRow("groupName", "optionName")]
        public void BitRadioButtonGroupShouldGiveNameToRadioButtonOptions(string groupName, string optionName)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.RadioButtonGroupName, groupName);
                    parameters.Add(p => p.RadioButtonOptionName, optionName);
                });

            var bitRadioButtonOptionInput = component.Find(".bit-rbo-input");

            Assert.IsTrue(bitRadioButtonOptionInput.HasAttribute("name"));
            Assert.AreEqual(optionName, bitRadioButtonOptionInput.GetAttribute("name"));
        }


        [DataTestMethod, DataRow("this is label")]
        public void BitRadioButtonGroupShouldTakeLabel(string label)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Label, label);
                });

            var bitRadioButtonGroupLabel = component.Find(".bit-rbg-label-fluent");

            Assert.AreEqual(label, bitRadioButtonGroupLabel.TextContent);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitRadioButtonGroupShouldRespectIsRequired(bool isRequired)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsRequired, isRequired);
                });

            var bitRadioButtonGroup = component.Find(".bit-rbg");
            var input = component.Find(".bit-rbo-input");

            Assert.AreEqual(isRequired, bitRadioButtonGroup.ClassList.Contains("bit-rbg-required-fluent"));

            Assert.AreEqual(isRequired, input.HasAttribute("required"));
        }

        [DataTestMethod,
           DataRow(true, "value1"),
           DataRow(false, "value2")
        ]
        public void BitRadioButtonOptionMustRespondToTheClickAndChangeValueEvent(bool isEnabled, string value)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                    parameters.Add(p => p.RadioButtonGroupIsEnabled, isEnabled);
                });

            var input = component.Find(".bit-rbo-input");

            input.Click();

            //TODO: bypassed - BUnit onchange event issue
            //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.RadioButtonOptionClickedValue);
            //Assert.AreEqual(isEnabled ? value : "", component.Instance.RadioButtonGroupChangedValue);
        }

        [DataTestMethod, DataRow("test value")]
        public void BitRadioButtonOptionShouldTakeCorrectValue(string value)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                });

            var input = component.Find(".bit-rbo-input");

            Assert.IsTrue(input.HasAttribute("value"));
            Assert.AreEqual(value, input.GetAttribute("value"));
        }

        [DataTestMethod, DataRow("hello world")]
        public void BitRadioButtonOptionShouldUseTextInLabel(string text)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Text, text);
                });

            var bitRadioButtonOptionLabelText = component.Find(".bit-rbo-txt");

            Assert.AreEqual(text, bitRadioButtonOptionLabelText.TextContent);
        }

        [DataTestMethod,
           DataRow("https://picsum.photos/100", "this is alt", 50, 50, "this is label")
        ]
        public void BitRadioButtonOptionShouldRespectImage(string imageSrc, string imageAlt, int width, int height, string label)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Text, label);
                    parameters.Add(p => p.ImageSrc, imageSrc);
                    parameters.Add(p => p.ImageAlt, imageAlt);
                    parameters.Add(p => p.ImageSize, new Size(width, height));
                });

            var image = component.Find(".bit-rbo-img img");
            Assert.IsTrue(image.HasAttribute("src"));
            Assert.AreEqual(imageSrc, image.GetAttribute("src"));
            Assert.IsTrue(image.HasAttribute("alt"));
            Assert.AreEqual(imageAlt, image.GetAttribute("alt"));

            var imageContainer = component.Find(".bit-rbo-img");
            Assert.IsTrue(imageContainer.HasAttribute("style"));
            Assert.AreEqual($" width:{width}px; height:{height}px;", imageContainer.GetAttribute("style"));

            var optionLabel = component.Find(".bit-rbo-txt");
            Assert.AreEqual(label, optionLabel.TextContent);
        }

        [DataTestMethod, DataRow(BitIconName.Emoji2)]
        public void BitRadioButtonOptionShouldrespectIcon(BitIconName iconName)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IconName, iconName);
                });

            var icon = component.Find(".bit-rbo-icon-wrapper i");
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
        }

        [DataTestMethod,
          DataRow(true),
          DataRow(false)
        ]
        public void BitRadioButtonOptionShouldAcceptGivenCheckStatus(bool isChecked)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsChecked, isChecked);
                });

            var input = component.Find(".bit-rbo-input");
           
            // TODO: bypassed - BUnit two-way binding issue
            //Assert.AreEqual(isChecked, input.HasAttribute("checked"));

        }

        [DataTestMethod,
           DataRow(true, true),
           DataRow(true, false),
           DataRow(false, true),
           DataRow(false, false),
        ]
        public void BitRadioButtonOptionMustRespondToTheClickEvent(bool groupIsEnabled, bool optionIsEnabled)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.RadioButtonGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.RadioButtonOptionIsEnabled, optionIsEnabled);
                });

            var input1 = component.Find(".bit-rbo:nth-child(1) input");
            input1.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input1.HasAttribute("checked"));
            }

            var input2 = component.Find($".bit-rbo:nth-child(2) input");
            input2.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input2.HasAttribute("checked"));
            }

            //TODO: bypassed - BUnit onchange event issue
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.RadioButtonOptionClickedValue);
        }

        [DataTestMethod,
          DataRow(true, true, 2),
           DataRow(true, false, 2),
           DataRow(false, true, 2),
           DataRow(false, false, 2),
       ]
        public void BitRadioButtonOptionMustRespondToTheChangeEvent(bool groupIsEnabled, bool optionIsEnabled, int count)
        {
            var component = RenderComponent<BitRadioButtonGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.RadioButtonGroupIsEnabled, groupIsEnabled);
                    parameters.Add(p => p.RadioButtonOptionIsEnabled, optionIsEnabled);
                });

            var input = component.Find(".bit-rbo-input");

            input.Click();

            if (groupIsEnabled && optionIsEnabled)
            {
                //TODO: bypassed - BUnit onchange event issue
                //Assert.IsTrue(input.HasAttribute("checked"));
            }

            //TODO: bypassed - BUnit onchange event issue
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? count.ToString() : null, component.Instance.Value);
            //Assert.AreEqual(groupIsEnabled && optionIsEnabled ? 1 : 0, component.Instance.RadioButtonOptionChangedValue);
        }

        [DataTestMethod,
            DataRow(null),
            DataRow("B")
        ]
        public void BitRadioButtonGroupValidationFormTest(string value)
        {
            var component = RenderComponent<BitRadioButtonGroupValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitRadioButtonGroupTestModel { Value = value });
            });

            var isValid = value.HasValue();

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
            Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

            if (isValid is false)
            {
                // select first item
                var option = component.Find(".bit-rbo-wrapper");
                var radioInput = option.FirstElementChild;
                radioInput.Click();

                form.Submit();

                Assert.AreEqual(component.Instance.ValidCount, 1);
                Assert.AreEqual(component.Instance.InvalidCount, 1);
                Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
            }
        }

        [DataTestMethod,
            DataRow(null),
            DataRow("B")
        ]
        public void BitRadioButtonGroupValidationInvalidHtmlAttributeTest(string value)
        {
            var component = RenderComponent<BitRadioButtonGroupValidationTest>(parameters =>
            {
                parameters.Add(p => p.TestModel, new BitRadioButtonGroupTestModel { Value = value });
            });

            var isInvalid = value.HasNoValue();

            var options = component.FindAll("input", true);
            foreach (var option in options)
            {
                Assert.IsFalse(option.HasAttribute("aria-invalid"));
            }

            var form = component.Find("form");
            form.Submit();

            foreach (var option in options)
            {
                Assert.AreEqual(option.HasAttribute("aria-invalid"), isInvalid);
                if (option.HasAttribute("aria-invalid"))
                {
                    Assert.AreEqual(option.GetAttribute("aria-invalid"), "true");
                }
            }

            if (isInvalid)
            {
                // select first item
                var firstOption = component.Find(".bit-rbo-wrapper");
                var radioInput = firstOption.FirstElementChild;
                radioInput.Click();

                foreach (var option in options)
                {
                    Assert.IsFalse(option.HasAttribute("aria-invalid"));
                }
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, "B"),
            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, "B"),
            DataRow(Visual.Material, null),
            DataRow(Visual.Material, "B")
        ]
        public void BitRadioButtonGroupValidationInvalidCssClassTest(Visual visual, string value)
        {
            var component = RenderComponent<BitRadioButtonGroupValidationTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.TestModel, new BitRadioButtonGroupTestModel { Value = value });
            });

            var isInvalid = value.HasNoValue();

            var bitRadioButtonGroup = component.Find(".bit-rbg");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsFalse(bitRadioButtonGroup.ClassList.Contains($"bit-rbg-invalid-{visualClass}"));

            var form = component.Find("form");
            form.Submit();

            Assert.AreEqual(bitRadioButtonGroup.ClassList.Contains($"bit-rbg-invalid-{visualClass}"), isInvalid);

            if (isInvalid)
            {
                // select first item
                var firstOption = component.Find(".bit-rbo-wrapper");
                var radioInput = firstOption.FirstElementChild;
                radioInput.Click();
            }

            Assert.IsFalse(bitRadioButtonGroup.ClassList.Contains($"bit-rbg-invalid-{visualClass}"));
        }
    }
}
