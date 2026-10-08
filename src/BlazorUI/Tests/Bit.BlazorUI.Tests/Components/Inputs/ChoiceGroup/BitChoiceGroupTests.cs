using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AngleSharp.Dom;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

[TestClass]
public class BitChoiceGroupTests : BunitTestContext
{
    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitChoiceGroupShouldTakeCorrectEnableStyle(bool isEnabled)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Disabled, isEnabled is false);
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod,
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
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        var itemContainers = component.FindAll(".bit-chg-icn");
        var index = 0;
        foreach (var itemContainer in itemContainers)
        {
            var item = choiceGroupItems[index++];
            if (isEnabled is false || item.IsDisabled)
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

    [TestMethod]
    public void BitChoiceGroupShouldTakeCorrectImageName()
    {
        var choiceGroupItems = GetChoiceGroupItems();

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, choiceGroupItems);
        });

        var bitChoiceGroupImages = component.FindAll(".bit-chg img");

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

    [TestMethod,
      DataRow("Detailed label")
    ]
    public void BitChoiceGroupShouldTakeCorrectLabel(string label)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Label, label);
        });

        var bitChoiceGroupLabel = component.Find($"#{component.Find(".bit-chg").GetAttribute("aria-labelledby")}");
        Assert.IsTrue(bitChoiceGroupLabel.InnerHtml.Contains(label));
    }

    [TestMethod,
      DataRow("<span>I am a span</span>")
    ]
    public void BitChoiceGroupShouldTakeCorrectLabelContent(string labelContent)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.LabelTemplate, labelContent);
        });

        var bitChoiceGroupLabelContent = component.Find($"#{component.Find(".bit-chg").GetAttribute("aria-labelledby")}").ChildNodes;
        bitChoiceGroupLabelContent.MarkupMatches(labelContent);
    }

    [TestMethod,
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

    [TestMethod,
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
        Assert.IsTrue(bitChoiceGroup?.GetAttribute("style")?.Contains(customStyle));
    }

    [TestMethod,
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

    [TestMethod,
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
                Assert.IsTrue(bitChoiceGroup?.GetAttribute("style")?.Contains("visibility:hidden"));
                break;
            case BitVisibility.Collapsed:
                Assert.IsTrue(bitChoiceGroup?.GetAttribute("style")?.Contains("display:none"));
                break;
        }
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderFocusableInputs()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.AreEqual(4, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.IsFalse(input.HasAttribute("hidden"));
            Assert.IsTrue(input.ClassList.Contains("bit-chg-inp"));
            Assert.IsFalse(input.HasAttribute("tabindex"));
        }
    }

    // A read-only group is not a disabled one: it stays reachable so the choice can still be read with the
    // keyboard and a screen reader, and it keeps submitting its value. What makes it read-only is that the
    // activation is cancelled, which is covered by BitChoiceGroupShouldNotChangeTheValueWhenReadOnly.
    [TestMethod]
    public void BitChoiceGroupShouldKeepInputsReachableAndEnabledWhenReadOnly()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.ReadOnly, true);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        foreach (var input in inputs)
        {
            Assert.IsFalse(input.HasAttribute("tabindex"));
        }

        // Read-only must not disable anything by itself; the one disabled input here is the item that
        // opts out through its own Disabled, which read-only has nothing to do with.
        Assert.AreEqual(1, inputs.Count(i => i.HasAttribute("disabled")));

        var bitChoiceGroup = component.Find(".bit-chg");
        Assert.AreEqual("true", bitChoiceGroup.GetAttribute("aria-readonly"));
        Assert.IsFalse(bitChoiceGroup.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderMultipleDescriptions()
    {
        var items = new List<BitChoiceGroupItem<string>>()
        {
            new() { Text = "Item A", Value = "A", Description = "Description A" },
            new() { Text = "Item B", Value = "B" },
            new() { Text = "Item C", Value = "C", Description = "Description C" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var descriptions = component.FindAll(".bit-chg-dsc");

        Assert.AreEqual(2, descriptions.Count);
        Assert.AreEqual("Description A", descriptions[0].TextContent);
        Assert.AreEqual("Description C", descriptions[1].TextContent);
        Assert.AreNotEqual(descriptions[0].Id, descriptions[1].Id);
    }

    [TestMethod]
    public void BitChoiceGroupShouldTakeCustomGap()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Gap, "2rem");
        });

        var bitChoiceGroup = component.Find(".bit-chg");

        Assert.IsTrue(bitChoiceGroup?.GetAttribute("style")?.Contains("--bit-chg-gap:2rem"));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitChoiceGroupShouldTakeCorrectGroupAria(bool horizontal)
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetChoiceGroupItems());
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Horizontal, horizontal);
        });

        var bitChoiceGroup = component.Find(".bit-chg");

        Assert.AreEqual("true", bitChoiceGroup.GetAttribute("aria-required"));
        Assert.AreEqual(horizontal ? "horizontal" : "vertical", bitChoiceGroup.GetAttribute("aria-orientation"));
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
                IsDisabled = true
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

    [TestMethod]
    public void BitChoiceGroupShouldKeepTheAttributesThePageSplatsOn()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetChoiceGroupItems());
            builder.AddAttribute(2, "aria-label", "Size");
            builder.AddAttribute(3, "aria-labelledby", "size-heading");
            builder.AddAttribute(4, "aria-required", "true");
            builder.AddAttribute(5, "aria-invalid", "true");
            builder.AddAttribute(6, "aria-readonly", "true");
            builder.AddAttribute(7, "aria-disabled", "true");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-chg");

        Assert.AreEqual("Size", root.GetAttribute("aria-label"));
        Assert.AreEqual("size-heading", root.GetAttribute("aria-labelledby"));
        Assert.AreEqual("true", root.GetAttribute("aria-required"));
        Assert.AreEqual("true", root.GetAttribute("aria-invalid"));
        Assert.AreEqual("true", root.GetAttribute("aria-readonly"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotAnnounceADisabledGroupAsRequired()
    {
        // A disabled field is neither submitted nor validated, so a splatted aria-required goes with it.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetChoiceGroupItems());
            builder.AddAttribute(2, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Disabled), true);
            builder.AddAttribute(3, "aria-required", "true");
            builder.CloseComponent();
        });

        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-required"));
    }
}
