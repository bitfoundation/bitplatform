using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AngleSharp.Dom;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

[TestClass]
public class BitChoiceGroupTests : BunitTestContext
{
    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitChoiceGroupShouldTakeCorrectEnableStyle(bool isEnabled)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitChoiceGroup = component.Find(".bit-chg");

        if (isEnabled)
        {
            Assert.IsFalse(bitChoiceGroup.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains("bit-dis"));
        }
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldGenerateAllItems()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroup = component.FindAll(".bit-chg-icn");

        Assert.AreEqual(bitChoiceGroup.Count, choiceGroupItems.Count);
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldTakeCorrectIconName()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroupIcons = component.FindAll(".bit-chg .bit-icon");

        foreach ((IElement item, int index) element in bitChoiceGroupIcons.Select((item, index) => (item, index)))
        {
            Assert.IsTrue(element.item.ClassList.Contains($"bit-icon--{choiceGroupItems[element.index].IconName}"));
        }
    }

    [DataTestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitChoiceGroupShouldRespectInputClick(bool isEnabled)
    {
        var choiceGroupItems = GetChoiceGroupItems();
        //var value = choiceGroupItems[1].Value;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            //parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.Items, choiceGroupItems);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var itemContainers = component.FindAll(".bit-chg-icn", true);
        var index = 0;
        foreach (var itemContainer in itemContainers)
        {
            var item = choiceGroupItems[index++];
            if (isEnabled is false || item.IsEnabled is false)
            {                
                Assert.IsTrue(itemContainer.ClassList.Contains("bit-chg-ids"));
            }
            else
            {
                Assert.IsFalse(itemContainer.ClassList.Contains("bit-chg-ids"));

                //var input = itemContainer.GetElementsByTagName("input").First();
                //input.Click();
                // TODO: bypassed - BUnit 2-way bound parameters issue
                //Assert.IsTrue(itemContainer.ClassList.Contains($"bit-chg-ich"));
            }
        }
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldTakeCorrectImageName()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroupImages = component.FindAll(".bit-chg img", true);

        foreach ((IElement item, int index) element in bitChoiceGroupImages.Select((item, index) => (item, index)))
        {
            Assert.AreEqual(element.item.GetAttribute("src"), choiceGroupItems[element.index].ImageSrc);
            Assert.AreEqual(element.item.GetAttribute("alt"), choiceGroupItems[element.index].ImageAlt);

            var bitChoiceGroup = component.Find(".bit-chg-icn");
            var bitChoiceGroupInput = bitChoiceGroup.GetElementsByTagName("input").First();

            bitChoiceGroupInput.Click();

            // TODO: bypassed - BUnit 2-way bound parameters issue
            // Assert.AreEqual(element.item.GetAttribute("src"), choiceGroupItems[element.index].SelectedImageName);
        }
    }

    [DataTestMethod,
      DataRow("Detailed label")
    ]
    public void BitChoiceGroupShouldTakeCorrectLabel(string label)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Label, label);
        });

        var bitChoiceGroupLabel = component.Find(".bit-chg label");
        Assert.IsTrue(bitChoiceGroupLabel.InnerHtml.Contains(label));
    }

    [DataTestMethod,
      DataRow("<span>I am a span</span>")
    ]
    public void BitChoiceGroupShouldTakeCorrectLabelContent(string labelContent)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.LabelTemplate, labelContent);
        });

        var bitChoiceGroupLabelContent = component.Find(".bit-chg label").ChildNodes;
        bitChoiceGroupLabelContent.MarkupMatches(labelContent);
    }

    [DataTestMethod,
      DataRow("This is a AriaLabelledBy")
    ]
    public void BitChoiceGroupShouldTakeCorrectAria(string ariaLabelledBy)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.AriaLabelledBy, ariaLabelledBy);
        });

        var bitChoiceGroup = component.Find(".bit-chg");
        Assert.AreEqual(bitChoiceGroup.GetAttribute("aria-labelledby"), ariaLabelledBy);
    }

    [DataTestMethod,
      DataRow("color:red;")
    ]
    public void BitChoiceGroupShouldTakeCustomStyle(string customStyle)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Style, customStyle);
        });

        var bitChoiceGroup = component.Find(".bit-chg");
        Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains(customStyle));
    }

    [DataTestMethod,
      DataRow("custom-class")
    ]
    public void BitChoiceGroupShouldTakeCustomClass(string customClass)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Class, customClass);
        });

        var bitChoiceGroup = component.Find(".bit-chg");
        Assert.IsTrue(bitChoiceGroup.ClassList.Contains(customClass));
    }

    [DataTestMethod,
      DataRow(BitVisibility.Visible),
      DataRow(BitVisibility.Hidden),
      DataRow(BitVisibility.Collapsed),
    ]
    public void BitChoiceGroupShouldTakeCustomVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Visibility, visibility);
        });

        var bitChoiceGroup = component.Find($".bit-chg");

        switch (visibility)
        {
            case BitVisibility.Visible:
                Assert.IsFalse(bitChoiceGroup.HasAttribute("style"));
                break;
            case BitVisibility.Hidden:
                Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains("visibility:hidden"));
                break;
            case BitVisibility.Collapsed:
                Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains("display:none"));
                break;
        }
    }

    private List<BitChoiceGroupItem<string>> GetChoiceGroupItems()
    {
        return new List<BitChoiceGroupItem<string>>()
        {
            new()
            {
                Text = "Female",
                Value = "v-female",
                IconName = "ContactHeart",
                ImageSrc = "https://bit.com/female_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-female_icon.svg.png",
                ImageAlt = "female-icon",
            },
            new()
            {
                Text = "Male",
                Value = "v-male",
                IconName = "FrontCamera",
                ImageSrc = "https://bit.com/male_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-male_icon.svg.png",
                ImageAlt = "male-icon",
            },
            new()
            {
                Text = "Other",
                Value = "v-other",
                IconName = "Group",
                ImageSrc = "https://bit.com/other_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-other_icon.svg.png",
                ImageAlt = "other-icon",
                IsEnabled = false
            },
            new()
            {
                Text = "Prefer not to say",
                Value = "v-nosay",
                IconName = "Emoji2",
                ImageSrc = "https://bit.com/nottosay_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-nottosay_icon.svg.png",
                ImageAlt = "nottosay-icon",
            },
        };
    }
}
