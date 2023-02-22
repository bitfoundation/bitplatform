using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.ChoiceGroup;

[TestClass]
public class BitChoiceGroupTests : BunitTestContext
{
    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Material, true),

      DataRow(Visual.Fluent, false),
      DataRow(Visual.Cupertino, false),
      DataRow(Visual.Material, false),
    ]
    public void BitChoiceGroupShouldTakeCorrectVisualStyle(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Visual, visual);
        });

        var bitChoiceGroup = component.Find(".bit-chg");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        if (isEnabled)
        {
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-chg-{visualClass}"));
        }
        else
        {
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-chg-disabled-{visualClass}"));
        }
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldGenerateAllItems()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroup = component.FindAll(".bit-chgi");

        Assert.AreEqual(bitChoiceGroup.Count, choiceGroupItems.Count);
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldTakeCorrectIconName()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
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
      DataRow(Visual.Fluent, false),
      DataRow(Visual.Cupertino, false),
      DataRow(Visual.Material, false),
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Material, true),
    ]
    public void BitChoiceGroupShouldRespectInputClick(Visual visual, bool isEnabled)
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Visual, visual);
        });

        var bitChoiceGroupImages = component.FindAll(".bit-chgi", true);

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        foreach (var element in bitChoiceGroupImages)
        {
            if (isEnabled)
            {
                var bitChoiceGroup = component.Find(".bit-chgi");
                var bitChoiceGroupInput = bitChoiceGroup.GetElementsByTagName("input").First();

                bitChoiceGroupInput.Click();

                // TODO: bypassed - BUnit 2-way bound parameters issue
                // Assert.IsTrue(element.ClassList.Contains($"bit-chgi-checked"));
            }
            else
            {
                Assert.IsTrue(element.ClassList.Contains($"bit-chgi-disabled"));
            }
        }
    }

    [DataTestMethod]
    public void BitChoiceGroupShouldTakeCorrectImageName()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroupImages = component.FindAll(".bit-chg img", true);

        foreach ((IElement item, int index) element in bitChoiceGroupImages.Select((item, index) => (item, index)))
        {
            Assert.AreEqual(element.item.GetAttribute("src"), choiceGroupItems[element.index].ImageSrc);
            Assert.AreEqual(element.item.GetAttribute("alt"), choiceGroupItems[element.index].ImageAlt);

            var bitChoiceGroup = component.Find(".bit-chgi");
            var bitChoiceGroupInput = bitChoiceGroup.GetElementsByTagName("input").First();

            bitChoiceGroupInput.Click();

            // TODO: bypassed - BUnit 2-way bound parameters issue
            // Assert.AreEqual(element.item.GetAttribute("src"), choiceGroupItems[element.index].SelectedImageName);
        }
    }

    [DataTestMethod,
      DataRow(32, 32)
    ]
    public void BitChoiceGroupShouldTakeSize(int width, int height)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
        });

        var bitChoiceGroupImages = component.FindAll(".bit-chgi-img");

        foreach (var item in bitChoiceGroupImages)
        {
            Assert.IsTrue(item.GetAttribute("style").Contains($"width:{width}px; height:{height}px;"));
        }
    }

    [DataTestMethod,
      DataRow("Detailed label")
    ]
    public void BitChoiceGroupShouldTakeCorrectLabel(string label)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
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
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
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
    public void BitChoiceGroupShouldTakeCorrectArials(string ariaLabelledBy)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.AriaLabelledBy, ariaLabelledBy);
        });

        var bitChoiceGroup = component.Find(".bit-chg").FirstElementChild;
        Assert.AreEqual(bitChoiceGroup.GetAttribute("aria-labelledby"), ariaLabelledBy);
    }

    [DataTestMethod,
      DataRow("color:red;")
    ]
    public void BitChoiceGroupShouldTakeCustomStyle(string customStyle)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
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
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Class, customClass);
        });

        var bitChoiceGroup = component.Find(".bit-chg");
        Assert.IsTrue(bitChoiceGroup.ClassList.Contains(customClass));
    }

    [DataTestMethod,
      DataRow(BitComponentVisibility.Visible),
      DataRow(BitComponentVisibility.Hidden),
      DataRow(BitComponentVisibility.Collapsed),
    ]
    public void BitChoiceGroupShouldTakeCustomVisibility(BitComponentVisibility visibility)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Visibility, visibility);
        });

        var bitChoiceGroup = component.Find($".bit-chg");

        switch (visibility)
        {
            case BitComponentVisibility.Visible:
                Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains(""));
                break;
            case BitComponentVisibility.Hidden:
                Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains("visibility:hidden"));
                break;
            case BitComponentVisibility.Collapsed:
                Assert.IsTrue(bitChoiceGroup.GetAttribute("style").Contains("display:none"));
                break;
        }
    }

    private List<BitChoiceGroupItem> GetChoiceGroupItems()
    {
        return new List<BitChoiceGroupItem>()
        {
            new()
            {
                Text = "Female",
                IconName = BitIconName.ContactHeart,
                ImageSrc = "https://bit.com/female_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-female_icon.svg.png",
                ImageAlt = "female-icon",
            },
            new()
            {
                Text = "Male",
                IconName = BitIconName.FrontCamera,
                ImageSrc = "https://bit.com/male_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-male_icon.svg.png",
                ImageAlt = "male-icon",
            },
            new()
            {
                Text = "Other",
                IconName = BitIconName.Group,
                ImageSrc = "https://bit.com/other_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-other_icon.svg.png",
                ImageAlt = "other-icon",
            },
            new()
            {
                Text = "Prefer not to say",
                IconName = BitIconName.Emoji2,
                ImageSrc = "https://bit.com/nottosay_icon.svg.png",
                SelectedImageSrc = "https://bit.com/selected-nottosay_icon.svg.png",
                ImageAlt = "nottosay-icon",
            },
        };
    }
}
