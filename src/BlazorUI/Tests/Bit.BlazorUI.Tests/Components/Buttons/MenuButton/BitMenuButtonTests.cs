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

    [TestMethod]
    public void BitMenuButtonShouldRenderSeparatorItems()
    {
        var separatorItems = new List<BitMenuButtonItem>()
        {
            new() { Text = "Item A", Key = "A" },
            new() { IsSeparator = true },
            new() { Text = "Item B", Key = "B" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, separatorItems);
        });

        var separators = com.FindAll(".bit-mnb-isp");

        Assert.AreEqual(1, separators.Count);
        Assert.AreEqual("separator", separators[0].GetAttribute("role"));

        var itemButtons = com.FindAll(".bit-mnb-itm");
        Assert.AreEqual(2, itemButtons.Count);
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderLinkItems()
    {
        var linkItems = new List<BitMenuButtonItem>()
        {
            new() { Text = "Link item", Key = "A", Href = "https://bitplatform.dev", Target = "_blank", Title = "the title" },
            new() { Text = "Button item", Key = "B" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, linkItems);
        });

        var anchor = com.Find("a.bit-mnb-itm");

        Assert.AreEqual("https://bitplatform.dev", anchor.GetAttribute("href"));
        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("noopener noreferrer", anchor.GetAttribute("rel"));
        Assert.AreEqual("the title", anchor.GetAttribute("title"));
        Assert.AreEqual("menuitem", anchor.GetAttribute("role"));

        var buttons = com.FindAll("button.bit-mnb-itm");
        Assert.AreEqual(1, buttons.Count);
    }

    [TestMethod]
    public void BitMenuButtonShouldHaveCorrectAriaAttributes()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var operatorButton = com.Find(".bit-mnb-opb");

        Assert.AreEqual("menu", operatorButton.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", operatorButton.GetAttribute("aria-expanded"));
        Assert.IsNotNull(operatorButton.GetAttribute("aria-controls"));

        var menu = com.Find(".bit-mnb-cul");
        Assert.AreEqual("menu", menu.GetAttribute("role"));
        Assert.IsNotNull(menu.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitMenuButtonSplitChevronButtonShouldHaveCorrectAriaAttributes()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.ChevronDownAriaLabel, "Open menu");
        });

        var chevronButton = com.Find(".bit-mnb-chb");

        Assert.AreEqual("menu", chevronButton.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", chevronButton.GetAttribute("aria-expanded"));
        Assert.IsNotNull(chevronButton.GetAttribute("aria-controls"));
        Assert.AreEqual("Open menu", chevronButton.GetAttribute("aria-label"));

        var operatorButton = com.Find(".bit-mnb-opb");
        Assert.IsNull(operatorButton.GetAttribute("aria-haspopup"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonFullWidthTest(bool fullWidth)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var root = com.Find(".bit-mnb");

        Assert.AreEqual(fullWidth, root.ClassList.Contains("bit-mnb-flw"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonIsLoadingTest(bool isLoading)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, "loading test");
            parameters.Add(p => p.IconName, "Add");
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var spinners = com.FindAll(".bit-mnb-spn");
        var icons = com.FindAll(".bit-mnb-opb .bit-icon--Add");

        Assert.AreEqual(isLoading ? 1 : 0, spinners.Count);
        Assert.AreEqual(isLoading ? 0 : 1, icons.Count);
    }

    [TestMethod]
    public void BitMenuButtonTitleTest()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Title, "menu button title");
        });

        var operatorButton = com.Find(".bit-mnb-opb");

        Assert.AreEqual("menu button title", operatorButton.GetAttribute("title"));
    }
}
