using System.Collections.Generic;
using System.Drawing;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Inputs;

[TestClass]
public class BitChoiceGroupTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(true, true),
       DataRow(true, false),
       DataRow(false, true),
       DataRow(false, false)
   ]
    public void BitChoiceGroupShouldRespectIsEnabled(bool isEnabled, bool optionIsEnabled)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new()
                {
                    Value = "key1",
                    IsEnabled = optionIsEnabled
                }
            });
        });

        var isEnabledClass = isEnabled ? "enabled" : "disabled";

        var choiceGroup = component.Find(".bit-chg");
        var option = component.Find(".option");

        if (isEnabled)
        {
            Assert.IsFalse(choiceGroup.ClassList.Contains($"disabled"));
            if (optionIsEnabled)
            {
                Assert.IsFalse(option.ClassList.Contains($"disabled"));
            }
            else
            {
                Assert.IsTrue(option.ClassList.Contains($"disabled"));
            }
        }
        else
        {
            Assert.IsTrue(choiceGroup.ClassList.Contains($"disabled"));
            Assert.IsTrue(option.ClassList.Contains($"disabled"));
        }
    }

    [DataTestMethod, DataRow("key1")]
    public void BitChoiceGroupRespectDafaultValue(string defaultValue)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new()
                {
                    Value = "key1"
                },
                new()
                {
                    Value = "key2"
                }
            });
        });

        var bitChoiceGroupOption = component.Find(".option");

        Assert.IsTrue(bitChoiceGroupOption.ClassList.Contains("checked"));
    }

    [DataTestMethod, DataRow("ChoiceGroupName")]
    public void BitChoiceGroupShouldGiveNameBitChoiceGroupOptions(string choiceGroupName)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.Name, choiceGroupName);
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new()
                {
                    Value = "key1"
                }
            });
        });

        var bitChoiceGroupOption = component.Find(".input");

        Assert.IsTrue(bitChoiceGroupOption.HasAttribute("name"));
        Assert.AreEqual(choiceGroupName, bitChoiceGroupOption.GetAttribute("name"));
    }

    [DataTestMethod, DataRow("this is label")]
    public void BitChoiceGroupShouldTakeLabel(string label)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitChoiceGroupLabel = component.Find(".label");

        Assert.AreEqual(label, bitChoiceGroupLabel.TextContent);
    }

    [DataTestMethod, DataRow("https://picsum.photos/100", "this is alt", 50, 50)]
    public void BitChoiceGroupShouldRespectImage(string imageSrc, string imageAlt, int width, int height)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new()
                {
                    Value = "key1",
                    ImageSrc = imageSrc,
                    ImageAlt = imageAlt,
                    ImageSize = new Size(width,height)
                }
            });
        });

        var bitChoiceGroupOption = component.Find(".option");
        Assert.IsTrue(bitChoiceGroupOption.ClassList.Contains("has-img"));

        var image = component.Find(".image img");
        Assert.IsTrue(image.HasAttribute("src"));
        Assert.AreEqual(imageSrc, image.GetAttribute("src"));
        Assert.IsTrue(image.HasAttribute("alt"));
        Assert.AreEqual(imageAlt, image.GetAttribute("alt"));

        var imageContainer = component.Find(".image");
        Assert.IsTrue(imageContainer.HasAttribute("style"));
        Assert.AreEqual($"width:{width}px; height:{height}px;", imageContainer.GetAttribute("style"));
    }

    [DataTestMethod, DataRow(BitIconName.Emoji2)]
    public void BitChoiceGroupShouldRespectIcon(BitIconName iconName)
    {
        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new BitChoiceGroupOption
                {
                    Value = "key1",
                    IconName = iconName
                }
            });
        });

        var icon = component.Find(".icon-wrapper i");
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitChoiceGroupOnChangeShouldWorkIfIsEnabled(bool groupIsEnabled, bool optionIsEnabled)
    {
        bool optionOnChangeValue = false;

        var component = RenderComponent<BitChoiceGroup>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, groupIsEnabled);
            parameters.Add(p => p.Options, new List<BitChoiceGroupOption>
            {
                new()
                {
                    Value = "key1",
                    IsEnabled = optionIsEnabled
                }
            });
        });

        var bitChoiceGroupOption = component.Find(".input");

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
