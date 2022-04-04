using System.Drawing;
using Bit.Client.Web.BlazorUI.Components.ChoiceGroup;
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
                            Key = "key1",
                            IsEnabled = optionIsEnabled
                        }
                    });
                });

            var bitChoiceGroupOptions = component.Find(".bit-chgo");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (groupIsEnabled is false || optionIsEnabled is false)
            {
                Assert.IsTrue(bitChoiceGroupOptions.ClassList.Contains($"bit-chgo-disabled-{visualClass}"));
            }
            else
            {
                Assert.IsFalse(bitChoiceGroupOptions.ClassList.Contains($"bit-chgo-disabled-{visualClass}"));
            }
        }

        [DataTestMethod, DataRow("key1")]
        public void BitChoiceGroupRespectDafaultSelectedKey(string defaultSelectedKey)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
               parameters =>
               {
                   parameters.Add(p => p.DefaultSelectedKey, defaultSelectedKey);
                   parameters.Add(p => p.Options, new()
                   {
                        new BitChoiceGroupOption
                        {
                            Key = "key1"
                        },
                        new BitChoiceGroupOption
                        {
                            Key = "key2"
                        }
                   });
               });

            var bitChoiceGroupOptions = component.Find(".bit-chgo");

            Assert.IsTrue(bitChoiceGroupOptions.ClassList.Contains($"bit-chgo-checked-fluent"));
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
                            Key = "key1"
                        }
                     });
                 });

            var bitChoiceGroupOptions = component.Find(".bit-chgo-input");

            Assert.IsTrue(bitChoiceGroupOptions.HasAttribute("name"));
            Assert.AreEqual(choiceGroupName, bitChoiceGroupOptions.GetAttribute("name"));
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
                            Key = "key1",
                            ImageSrc = imageSrc,
                            ImageAlt = imageAlt,
                            ImageSize = new Size(width,height)
                        }
                     });
                 });

            var bitChoiceGroupOptions = component.Find(".bit-chgo");
            Assert.IsTrue(bitChoiceGroupOptions.ClassList.Contains($"bit-chgo-with-img-fluent"));

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
        public void BitChoiceGroupShouldrespectIcon(BitIconName iconName)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Options, new()
                    {
                        new BitChoiceGroupOption
                        {
                            Key = "key1",
                            IconName = iconName
                        }
                    });
                });

            var icon = component.Find(".bit-chgo-icon-wrapper i");
            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
        }
    }
}
