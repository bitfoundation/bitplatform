using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

[TestClass]
public class BitMenuButtonTests : BunitTestContext
{
    private readonly List<BitMenuButtonItem> items = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            Key = "A"
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            Key = "B"
        }
    };

    [TestMethod,
       DataRow(true, BitVariant.Fill),
       DataRow(true, BitVariant.Outline),
       DataRow(false, BitVariant.Fill),
       DataRow(false, BitVariant.Outline)
    ]
    public void BitMenuButtonTest(bool isEnabled, BitVariant variant)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
            parameters.Add(p => p.Items, items);
        });

        var bitMenuButton = com.Find(".bit-mnb");

        if (isEnabled)
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-dis"));
        }


        if (variant == BitVariant.Fill)
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-fil"));
            Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-otl"));
        }
        if (variant == BitVariant.Outline)
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-fil"));
            Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-otl"));
        }
    }

    [TestMethod,
        DataRow("A", "Add"),
        DataRow("B", "Edit")
    ]
    public void BitMenuButtonShouldHaveTextAndIcon(string text, string iconName)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, text);
            parameters.Add(p => p.IconName, iconName);
        });

        var iconNameClass = $"bit-icon--{iconName}";

        var menuButtonIcon = com.Find(".bit-mnb .bit-icon");

        var menuButtonText = com.Find(".bit-mnb .bit-mnb-btx");

        Assert.IsTrue(menuButtonIcon.ClassList.Contains(iconNameClass));

        Assert.AreEqual(text, menuButtonText.TextContent);
    }

    [TestMethod,
        DataRow("A", "Add"),
        DataRow("B", "Edit")
    ]
    public void BitMenuButtonShouldHaveTextAndIconInItem(string itemText, string itemIconName)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>()
            {
                new BitMenuButtonItem()
                {
                    Text = itemText,
                    IconName = itemIconName
                }
            });
        });

        var itemIconNameClass = $"bit-icon--{itemIconName}";
        var menuButtonItemText = com.Find(".bit-mnb-itm .bit-mnb-btx");
        var menuButtonItemIcon = com.Find(".bit-mnb-itm .bit-icon");

        Assert.AreEqual(itemText, menuButtonItemText.TextContent);
        Assert.IsTrue(menuButtonItemIcon.ClassList.Contains(itemIconNameClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonShouldBeItemClickIfEnabled(bool itemIsEnabled)
    {
        BitMenuButtonItem? clickedItem = null;

        items.Last().IsEnabled = itemIsEnabled;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .bit-mnb-itm");
        lastItem.Click();

        Assert.AreEqual(itemIsEnabled, lastItem.HasAttribute("disabled") is false);

        if (itemIsEnabled)
        {
            Assert.AreEqual(clickedItem, items.Last());
        }
        else
        {
            Assert.IsNull(clickedItem);
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonOpenMenu(bool isEnabled)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitMenuButton = com.Find(".bit-mnb");
        var operatorButton = com.Find(".bit-mnb-opb");
        Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        operatorButton.Click();

        if (isEnabled)
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        }
        else
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonStickyTest(bool isSticky)
    {
        BitMenuButtonItem? clickedItem = null;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Sticky, isSticky);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .bit-mnb-itm");
        lastItem.Click();

        var operatorButton = com.Find(".bit-mnb-opb");
        operatorButton.Click();

        if (isSticky)
        {
            Assert.AreEqual(clickedItem, items.Last());
        }
        else
        {
            Assert.IsNull(clickedItem);
        }
    }

    [TestMethod,
        DataRow(true)
    ]
    public void BitMenuButtonSplitTest(bool isSplit)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, isSplit);
        });

        var seperator = com.Find(".bit-mnb > .bit-mnb-spb");
        var chevronDownButton = com.Find(".bit-mnb > .bit-mnb-chb");

        if (isSplit)
        {
            Assert.IsNotNull(seperator);
            Assert.IsNotNull(chevronDownButton);
        }
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-mnb-bpg"),
        DataRow(BitColorKind.Secondary, "bit-mnb-bsg"),
        DataRow(BitColorKind.Tertiary, "bit-mnb-btg"),
        DataRow(BitColorKind.Transparent, "bit-mnb-brg")
    ]
    public void BitMenuButtonShouldRespectBackground(BitColorKind background, string expectedClass)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Background, background);
        });

        var callout = com.Find(".bit-mnb-cal");
        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitMenuButtonNoIconShouldRemoveIcon()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconName, "Add");
            parameters.Add(p => p.NoIcon, true);
        });

        var icons = com.FindAll(".bit-mnb-opb .bit-icon--Add");
        Assert.AreEqual(0, icons.Count);
    }

    [TestMethod]
    public void BitMenuButtonNoIconFalseShouldShowIcon()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IconName, "Add");
            parameters.Add(p => p.NoIcon, false);
        });

        var icon = com.Find(".bit-mnb-opb .bit-icon--Add");
        Assert.IsNotNull(icon);
    }

    [TestMethod]
    public void BitMenuButtonToggleShouldSetIsToggledOnClick()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Toggle, true);
        });

        var bitMenuButton = com.Find(".bit-mnb");
        Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-tgl"));

        var operatorButton = com.Find(".bit-mnb-opb");
        operatorButton.Click();

        Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-tgl"));

        operatorButton.Click();
        Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-tgl"));
    }

    [TestMethod]
    public void BitMenuButtonToggleShouldRespectDefaultIsToggled()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Toggle, true);
            parameters.Add(p => p.DefaultIsToggled, true);
        });

        var bitMenuButton = com.Find(".bit-mnb");
        Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-tgl"));
    }

    [TestMethod]
    public void BitMenuButtonToggleShouldFireOnToggleChange()
    {
        bool? toggledValue = null;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Toggle, true);
            parameters.Add(p => p.OnToggleChange, (v) => toggledValue = v);
        });

        var operatorButton = com.Find(".bit-mnb-opb");
        operatorButton.Click();

        Assert.IsTrue(toggledValue == true);

        operatorButton.Click();
        Assert.IsTrue(toggledValue == false);
    }

    [TestMethod]
    public void BitMenuButtonToggleShouldNotOpenCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Toggle, true);
        });

        var bitMenuButton = com.Find(".bit-mnb");
        var operatorButton = com.Find(".bit-mnb-opb");
        operatorButton.Click();

        Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
    }

    [TestMethod]
    public void BitMenuButtonSelectedItemShouldBeRemovedFromCalloutInStickyMode()
    {
        var stickyItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Item A", Key = "A" },
            new() { Text = "Item B", Key = "B", IsSelected = true }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, stickyItems);
            parameters.Add(p => p.Sticky, true);
        });

        var itemButtons = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual(stickyItems.Count - 1, itemButtons.Count);
        Assert.IsFalse(itemButtons.Any(b => b.TextContent.Contains("Item B")));
    }

    [TestMethod]
    public void BitMenuButtonSelectedItemShouldNotHaveSelClassInNonStickyMode()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Sticky, false);
        });

        var selectedButtons = com.FindAll(".bit-mnb-sel");
        Assert.AreEqual(0, selectedButtons.Count);
    }
}
