﻿using System.Drawing;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs
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
        public void BitChoiceGroupShouldRespectIsEnabled(Visual visual, bool groupIsEnabled, bool optionIsEnabled)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, groupIsEnabled);
                    parameters.Add(p => p.Options, new()
                    {
                        new BitChoiceGroupOption
                        {
                            Value = "key1",
                            IsEnabled = optionIsEnabled
                        }
                    });
                });

            var bitChoiceGroupOption = component.Find(".bit-chgo");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (groupIsEnabled is false || optionIsEnabled is false)
            {
                Assert.IsTrue(bitChoiceGroupOption.ClassList.Contains($"bit-chgo-disabled-{visualClass}"));
            }
            else
            {
                Assert.IsFalse(bitChoiceGroupOption.ClassList.Contains($"bit-chgo-disabled-{visualClass}"));
            }
        }

        [DataTestMethod, DataRow("key1")]
        public void BitChoiceGroupRespectDafaultValue(string defaultValue)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
               parameters =>
               {
                   parameters.Add(p => p.DefaultValue, defaultValue);
                   parameters.Add(p => p.Options, new()
                   {
                        new BitChoiceGroupOption
                        {
                            Value = "key1"
                        },
                        new BitChoiceGroupOption
                        {
                            Value = "key2"
                        }
                   });
               });

            var bitChoiceGroupOption = component.Find(".bit-chgo");

            Assert.IsTrue(bitChoiceGroupOption.ClassList.Contains($"bit-chgo-checked-fluent"));
        }

        [DataTestMethod, DataRow("ChoiceGroupName")]
        public void BitChoiceGroupShouldGiveNameBitChoiceGroupOptions(string choiceGroupName)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                 parameters =>
                 {
                     parameters.Add(p => p.Name, choiceGroupName);
                     parameters.Add(p => p.Options, new()
                     {
                        new BitChoiceGroupOption
                        {
                            Value = "key1"
                        }
                     });
                 });

            var bitChoiceGroupOption = component.Find(".bit-chgo-input");

            Assert.IsTrue(bitChoiceGroupOption.HasAttribute("name"));
            Assert.AreEqual(choiceGroupName, bitChoiceGroupOption.GetAttribute("name"));
        }

        [DataTestMethod, DataRow("this is label")]
        public void BitChoiceGroupShouldTakeLabel(string label)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                  parameters =>
                  {
                      parameters.Add(p => p.Label, label);
                  });

            var bitChoiceGroupLabel = component.Find(".bit-chg-label-fluent");

            Assert.AreEqual(label, bitChoiceGroupLabel.TextContent);
        }

        [DataTestMethod, DataRow("https://picsum.photos/100", "this is alt", 50, 50)]
        public void BitChoiceGroupShouldRespectImage(string imageSrc, string imageAlt, int width, int height)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                 parameters =>
                 {
                     parameters.Add(p => p.Options, new()
                     {
                        new BitChoiceGroupOption
                        {
                            Value = "key1",
                            ImageSrc = imageSrc,
                            ImageAlt = imageAlt,
                            ImageSize = new Size(width,height)
                        }
                     });
                 });

            var bitChoiceGroupOption = component.Find(".bit-chgo");
            Assert.IsTrue(bitChoiceGroupOption.ClassList.Contains($"bit-chgo-with-img-fluent"));

            var image = component.Find(".bit-chgo-img img");
            Assert.IsTrue(image.HasAttribute("src"));
            Assert.AreEqual(imageSrc, image.GetAttribute("src"));
            Assert.IsTrue(image.HasAttribute("alt"));
            Assert.AreEqual(imageAlt, image.GetAttribute("alt"));

            var imageContainer = component.Find(".bit-chgo-img");
            Assert.IsTrue(imageContainer.HasAttribute("style"));
            Assert.AreEqual($"width:{width}px; height:{height}px;", imageContainer.GetAttribute("style"));
        }

        [DataTestMethod, DataRow(BitIconName.Emoji2)]
        public void BitChoiceGroupShouldRespectIcon(BitIconName iconName)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Options, new()
                    {
                        new BitChoiceGroupOption
                        {
                            Value = "key1",
                            IconName = iconName
                        }
                    });
                });

            var icon = component.Find(".bit-chgo-icon-wrapper i");
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
        }

        [DataTestMethod, DataRow(true, true), DataRow(true, false), DataRow(false, true), DataRow(false, false)]
        public void BitChoiceGroupOtionOnChangeShouldWorkIfIsEnabled(bool groupIsEnabled, bool optionIsEnabled)
        {
            bool optionOnChangeValue = false;

            var component = RenderComponent<BitChoiceGroupTest>(
               parameters =>
               {
                   parameters.Add(p => p.IsEnabled, groupIsEnabled);
                   parameters.Add(p => p.Options, new()
                   {
                        new BitChoiceGroupOption
                        {
                            Value = "key1",
                            IsEnabled = optionIsEnabled,
                            OnChange = () => optionOnChangeValue = true
                        }
                   });
               });

            var bitChoiceGroupOption = component.Find(".bit-chgo-input");

            //TODO:
            //We need to call the Change event of each option based on clicking on the input.
            //Like the code below...
            //bitChoiceGroupOption.Click();

            if (groupIsEnabled is false || optionIsEnabled is false)
            {
                Assert.IsFalse(optionOnChangeValue);
            }
            else
            {
                //Assert.IsTrue(optionOnChangeValue);
            }
        }
    }
}
