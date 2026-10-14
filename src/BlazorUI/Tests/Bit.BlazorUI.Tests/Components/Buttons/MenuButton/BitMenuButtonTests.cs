using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

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
            parameters.Add(p => p.Disabled, isEnabled is false);
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

        items.Last().IsDisabled = itemIsEnabled is false;

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
            parameters.Add(p => p.Disabled, isEnabled is false);
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
    public void BitMenuButtonShouldTurnDownTheOpenerForAnyCaseOfTheBlankTarget()
    {
        // The browser matches the target keyword case-insensitively, so "_BLANK" opens a new context too.
        var linkItems = new List<BitMenuButtonItem>()
        {
            new() { Text = "Link item", Key = "A", Href = "https://bitplatform.dev", Target = "_BLANK" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, linkItems);
        });

        Assert.AreEqual("noopener", com.Find("a.bit-mnb-itm").GetAttribute("rel"));
    }

    [TestMethod,
        DataRow(BitLinkRels.NoFollow, "_blank", "nofollow noopener"),
        DataRow(BitLinkRels.NoReferrer, "_blank", "noreferrer"),
        DataRow(BitLinkRels.Opener, "_blank", "opener"),
        DataRow(BitLinkRels.NoFollow, null, "nofollow")]
    public void BitMenuButtonShouldRenderTheRelOfAnItem(BitLinkRels rel, string? target, string expectedRel)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "Link item", Key = "A", Href = "https://bitplatform.dev", Target = target, Rel = rel }]);
        });

        Assert.AreEqual(expectedRel, com.Find("a.bit-mnb-itm").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitMenuButtonShouldAppendTheNewTabHintToTheAriaLabelOfAnItem()
    {
        // An aria-label replaces the content, so hidden text inside the item would never be read.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { Text = "Docs", AriaLabel = "Documentation", Key = "A", Href = "https://bitplatform.dev", Target = "_blank" }]);
        });

        var anchor = com.Find("a.bit-mnb-itm");

        Assert.AreEqual("Documentation (opens in a new tab)", anchor.GetAttribute("aria-label"));
        Assert.IsNull(anchor.QuerySelector(".bit-mnb-nth"));
    }

    [TestMethod]
    public void BitMenuButtonNewTabHintShouldRewordOrRemoveTheAnnouncement()
    {
        List<BitMenuButtonItem> linkItems = [new() { Text = "Docs", Key = "A", Href = "https://bitplatform.dev", Target = "_blank" }];

        var reworded = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, linkItems);
            parameters.Add(p => p.NewTabHint, "(new window)");
        });
        Assert.AreEqual("(new window)", reworded.Find("a.bit-mnb-itm .bit-mnb-nth").TextContent);

        var suppressed = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, linkItems);
            parameters.Add(p => p.NoNewTabHint, true);
        });
        Assert.IsNull(suppressed.Find("a.bit-mnb-itm").QuerySelector(".bit-mnb-nth"));
        Assert.AreEqual("noopener", suppressed.Find("a.bit-mnb-itm").GetAttribute("rel"));
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
        Assert.AreEqual("noopener", anchor.GetAttribute("rel"));
        Assert.AreEqual("(opens in a new tab)", anchor.QuerySelector(".bit-mnb-nth")?.TextContent);
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

        var menu = com.Find(".bit-mnb-cul");
        Assert.AreEqual("menu", menu.GetAttribute("role"));
        Assert.IsNotNull(menu.GetAttribute("aria-labelledby"));

        // aria-controls names the menu itself rather than the callout that carries it (the APG pattern).
        Assert.AreEqual(menu.Id, operatorButton.GetAttribute("aria-controls"));
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
        Assert.AreEqual(com.Find(".bit-mnb-cul").Id, chevronButton.GetAttribute("aria-controls"));
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

    [TestMethod]
    public void BitMenuButtonCalloutShouldCarryTheColorAndSizeClasses()
    {
        // The callout is a sibling of the root, so nothing the root declares reaches the items inside it:
        // the two classes that size the items and color their focus ring have to be repeated on it.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var callout = com.Find(".bit-mnb-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-mnb-err"));
        Assert.IsTrue(callout.ClassList.Contains("bit-mnb-lg"));
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitMenuButtonShouldPublishNoColorSizeOrBackgroundWhileTheyAreUnset()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        // An unset Color, Size or Background publishes nothing - on the root, or on the callout that repeats the
        // classes - so the public --bit-MenuButton-* variables restyle the default while an explicit value, which
        // does publish its class, wins over them.
        var published = new[]
        {
            "bit-mnb-pri", "bit-mnb-sec", "bit-mnb-ter", "bit-mnb-inf", "bit-mnb-suc", "bit-mnb-wrn", "bit-mnb-swr", "bit-mnb-err",
            "bit-mnb-pbg", "bit-mnb-sbg", "bit-mnb-tbg", "bit-mnb-pfg", "bit-mnb-sfg", "bit-mnb-tfg", "bit-mnb-pbr", "bit-mnb-sbr", "bit-mnb-tbr",
            "bit-mnb-sm", "bit-mnb-md", "bit-mnb-lg",
            "bit-mnb-bpg", "bit-mnb-bsg", "bit-mnb-btg", "bit-mnb-brg",
        };

        foreach (var element in new[] { com.Find(".bit-mnb"), com.Find(".bit-mnb-cal") })
        {
            foreach (var cssClass in published)
            {
                Assert.IsFalse(element.ClassList.Contains(cssClass), $"An unset parameter published {cssClass}.");
            }
        }
    }

    [TestMethod]
    public void BitMenuButtonItemsShouldBeOutOfTheTabSequence()
    {
        // The menu is entered by opening it and navigated with the arrow keys (the APG menu button pattern),
        // so a tabindex of 0 would put every row of an open menu into the tab order of the page.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Item A", Key = "A" },
                new() { Text = "Link", Key = "L", Href = "https://bitplatform.dev" }
            });
        });

        foreach (var item in com.FindAll(".bit-mnb-itm"))
        {
            Assert.AreEqual("-1", item.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderHeaderItems()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Group", IsHeader = true },
                new() { Text = "Item A", Key = "A" }
            });
        });

        var header = com.Find(".bit-mnb-ihd");

        Assert.AreEqual("Group", header.TextContent.Trim());
        Assert.AreEqual("presentation", header.GetAttribute("role"));
        Assert.AreEqual(1, com.FindAll(".bit-mnb-itm").Count);
    }

    [TestMethod]
    public void BitMenuButtonStickyShouldNotSelectAHeaderItem()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Group", IsHeader = true },
                new() { Text = "Item A", Key = "A" }
            });
        });

        Assert.AreEqual("Item A", com.Find(".bit-mnb-opb .bit-mnb-btx").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderTheSecondaryTextOfAnItem()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Save", Key = "save", SecondaryText = "Ctrl+S" }
            });
        });

        Assert.AreEqual("Ctrl+S", com.Find(".bit-mnb-itm .bit-mnb-isc").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonCheckableItemsShouldHaveCheckboxSemantics()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Checked", Key = "a", Checkable = true, IsChecked = true },
                new() { Text = "Unchecked", Key = "b", Checkable = true },
                new() { Text = "Plain", Key = "c" }
            });
        });

        var menuItems = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual("menuitemcheckbox", menuItems[0].GetAttribute("role"));
        Assert.AreEqual("true", menuItems[0].GetAttribute("aria-checked"));
        Assert.IsTrue(menuItems[0].ClassList.Contains("bit-mnb-chk"));

        Assert.AreEqual("menuitemcheckbox", menuItems[1].GetAttribute("role"));
        Assert.AreEqual("false", menuItems[1].GetAttribute("aria-checked"));
        Assert.IsFalse(menuItems[1].ClassList.Contains("bit-mnb-chk"));

        // A plain item beside a checkable one stays a plain menuitem, but still reserves the check column so
        // the labels of the rows line up with each other.
        Assert.AreEqual("menuitem", menuItems[2].GetAttribute("role"));
        Assert.IsNull(menuItems[2].GetAttribute("aria-checked"));

        Assert.AreEqual(3, com.FindAll(".bit-mnb-ick").Count);
    }

    [TestMethod]
    public void BitMenuButtonShouldNotRenderACheckColumnWithoutACheckableItem()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-ick").Count);
    }

    [TestMethod]
    public void BitMenuButtonClickingACheckableItemShouldToggleIt()
    {
        var checkableItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Wrap", Key = "wrap", Checkable = true }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, checkableItems);
            parameters.Add(p => p.CloseOnItemClick, false);
        });

        com.Find(".bit-mnb-itm").Click();
        Assert.IsTrue(checkableItems[0].IsChecked);

        com.Find(".bit-mnb-itm").Click();
        Assert.IsFalse(checkableItems[0].IsChecked);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonCloseOnItemClickShouldDecideWhetherTheCalloutCloses(bool closeOnItemClick)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CloseOnItemClick, closeOnItemClick);
        });

        com.Find(".bit-mnb-opb").Click();
        Assert.IsTrue(com.Instance.IsOpen);

        com.Find("button.bit-mnb-itm").Click();
        Assert.AreEqual(closeOnItemClick, com.Instance.IsOpen is false);
    }

    [TestMethod]
    public void BitMenuButtonTriggerShouldToggleTheCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var operatorButton = com.Find(".bit-mnb-opb");

        operatorButton.Click();
        Assert.IsTrue(com.Instance.IsOpen);

        operatorButton.Click();
        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonAriaDescriptionShouldBeRenderedAsADescribedByTarget()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.AriaDescription, "Opens a menu of commands.");
        });

        var description = com.Find(".bit-mnb-dsc");
        var operatorButton = com.Find(".bit-mnb-opb");

        Assert.AreEqual("Opens a menu of commands.", description.TextContent);
        Assert.AreEqual(description.Id, operatorButton.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMenuButtonWithoutAnAriaDescriptionShouldNotRenderADescribedBy()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-dsc").Count);
        Assert.IsNull(com.Find(".bit-mnb-opb").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMenuButtonChevronShouldHaveADefaultAccessibleName()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.ChevronDownTitle, "More save options");
        });

        var chevronButton = com.Find(".bit-mnb-chb");

        Assert.AreEqual("More options", chevronButton.GetAttribute("aria-label"));
        Assert.AreEqual("More save options", chevronButton.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonLoadingShouldAnnounceItsLabel(bool isLoading)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.LoadingLabel, "Saving...");
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var status = com.Find(".bit-mnb-sts");
        var operatorButton = com.Find(".bit-mnb-opb");

        Assert.AreEqual("status", status.GetAttribute("role"));
        Assert.AreEqual(isLoading ? "Saving..." : string.Empty, status.TextContent);
        Assert.AreEqual(isLoading ? "true" : null, operatorButton.GetAttribute("aria-busy"));
        Assert.AreEqual(isLoading ? "Saving..." : "Save", com.Find(".bit-mnb-opb .bit-mnb-btx").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonMaxHeightShouldCapTheCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MaxHeight, "10rem");
        });

        var callout = com.Find(".bit-mnb-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-mnb-mxh"));
        StringAssert.Contains(callout.GetAttribute("style"), "--bit-MenuButton-callout-max-height:10rem");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonDisabledInteractiveShouldKeepDisabledItemsFocusable(bool disabledInteractive)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Disabled", Key = "d", IsDisabled = true }
            });
            parameters.Add(p => p.DisabledInteractive, disabledInteractive);
        });

        var item = com.Find(".bit-mnb-itm");

        Assert.AreEqual("true", item.GetAttribute("aria-disabled"));
        Assert.AreEqual(disabledInteractive is false, item.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMenuButtonItemAriaLabelShouldNameAnIconOnlyItem()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Key = "share", IconName = "Share", AriaLabel = "Share this page" }
            });
        });

        Assert.AreEqual("Share this page", com.Find(".bit-mnb-itm").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMenuButtonSplitHeaderShouldOnlyTakeAnExplicitButtonType()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
        });

        Assert.AreEqual("button", com.Find(".bit-mnb-opb").GetAttribute("type"));

        var submitting = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.ButtonType, BitButtonType.Submit);
        });

        Assert.AreEqual("submit", submitting.Find(".bit-mnb-opb").GetAttribute("type"));
    }

    [TestMethod]
    public void BitMenuButtonNonSplitHeaderShouldAlwaysBeAPlainButton()
    {
        // The only job of the header of a plain menu button is opening the menu, so it never submits a form.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ButtonType, BitButtonType.Submit);
        });

        Assert.AreEqual("button", com.Find(".bit-mnb-opb").GetAttribute("type"));
    }

    [TestMethod]
    public void BitMenuButtonDisabledShouldDisableBothHalves()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Disabled, true);
        });

        foreach (var selector in new[] { ".bit-mnb-opb", ".bit-mnb-chb" })
        {
            var button = com.Find(selector);
            Assert.IsTrue(button.HasAttribute("disabled"));
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
            Assert.AreEqual("-1", button.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitMenuButtonSplitChevronShouldOpenTheMenuWhileLoading()
    {
        // The loading state belongs to the header half alone: the rest of the commands stay reachable.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.IsLoading, true);
        });

        com.Find(".bit-mnb-chb").Click();

        Assert.IsTrue(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonDisabledInteractiveShouldKeepTheButtonFocusable()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.DisabledInteractive, true);
        });

        foreach (var selector in new[] { ".bit-mnb-opb", ".bit-mnb-chb" })
        {
            var button = com.Find(selector);
            Assert.IsFalse(button.HasAttribute("disabled"));
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
            Assert.AreEqual("0", button.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitMenuButtonAriaHiddenShouldTakeBothHalvesOutOfTheTabOrder()
    {
        // A control hidden from assistive technologies must not be reachable by Tab either - the disabled but
        // interactive one included, since it carries no native disabled attribute to do it.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.AriaHidden, true);
        });

        foreach (var selector in new[] { ".bit-mnb-opb", ".bit-mnb-chb" })
        {
            Assert.AreEqual("-1", com.Find(selector).GetAttribute("tabindex"));
        }

        com.Render(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.AriaHidden, true);
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.DisabledInteractive, true);
        });

        foreach (var selector in new[] { ".bit-mnb-opb", ".bit-mnb-chb" })
        {
            Assert.AreEqual("-1", com.Find(selector).GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitMenuButtonLoadingShouldNotOpenAPlainMenuFromTheKeyboard()
    {
        // The loading state takes the click of the header away, so it takes the keys that stand in for it too.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsLoading, true);
        });

        com.Find(".bit-mnb-opb").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonStickyPlainHeaderShouldOnlyOpenTheMenu()
    {
        // The header of a plain menu button opens the menu. Running the selected item's own action as well
        // would carry out a command the user only asked to choose between.
        var clicked = 0;
        var stickyItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Item A", Key = "A", OnClick = _ => clicked++ },
            new() { Text = "Item B", Key = "B" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, stickyItems);
            parameters.Add(p => p.Sticky, true);
        });

        com.Find(".bit-mnb-opb").Click();

        Assert.IsTrue(com.Instance.IsOpen);
        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitMenuButtonStickySplitHeaderShouldRunTheSelectedItemAction()
    {
        // The main half of a split button is a command of its own, so there the item's action is the click.
        var clicked = 0;
        var stickyItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Item A", Key = "A", OnClick = _ => clicked++ },
            new() { Text = "Item B", Key = "B" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, stickyItems);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.Split, true);
        });

        com.Find(".bit-mnb-opb").Click();

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitMenuButtonAutoLoadingShouldLoadWhileAwaitingTheClick()
    {
        var tcs = new TaskCompletionSource();
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.OnClick, async (BitMenuButtonItem _) => await tcs.Task);
        });

        com.Find(".bit-mnb-opb").Click();

        Assert.IsTrue(com.Instance.IsLoading);
        com.WaitForAssertion(() => Assert.IsNotNull(com.Find(".bit-mnb-spn")));

        tcs.SetResult();

        com.WaitForAssertion(() => Assert.IsFalse(com.Instance.IsLoading));
        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-mnb-spn").Count));
    }

    [TestMethod]
    public void BitMenuButtonLoadingShouldIgnoreTheHeaderClickUnlessReclickable()
    {
        var clicked = 0;
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.OnClick, (BitMenuButtonItem _) => { clicked++; });
        });

        com.Find(".bit-mnb-opb").Click();
        Assert.AreEqual(0, clicked);

        com.Render(parameters => parameters.Add(p => p.Reclickable, true));

        com.Find(".bit-mnb-opb").Click();
        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitMenuButtonLoadingDelayShouldHoldBackTheSpinner()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.LoadingDelay, 3000);
            parameters.Add(p => p.IsLoading, true);
        });

        // The class (and with it the click guard) applies at once; only the spinner waits for the delay.
        Assert.IsTrue(com.Find(".bit-mnb").ClassList.Contains("bit-mnb-lod"));
        Assert.AreEqual(0, com.FindAll(".bit-mnb-spn").Count);
    }

    [TestMethod]
    public void BitMenuButtonLoadingTemplateShouldReplaceTheSpinner()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-loading");
                builder.AddContent(2, "Working");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-spn").Count);
        Assert.AreEqual("Working", com.Find(".custom-loading").TextContent);
    }

    [TestMethod]
    public void BitMenuButtonAutoFocusShouldBeRenderedOnTheHeaderButton()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.AutoFocus, true);
        });

        Assert.IsTrue(com.Find(".bit-mnb-opb").HasAttribute("autofocus"));

        // A menu button hidden from assistive technologies must not pull the focus either.
        com.Render(parameters => parameters.Add(p => p.AriaHidden, true));

        Assert.IsFalse(com.Find(".bit-mnb-opb").HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitMenuButtonFormIdShouldAssociateTheHeaderAndTheItems()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.FormId, "the-form");
        });

        Assert.AreEqual("the-form", com.Find(".bit-mnb-opb").GetAttribute("form"));
        Assert.AreEqual("the-form", com.Find("button.bit-mnb-itm").GetAttribute("form"));
    }

    [TestMethod]
    public void BitMenuButtonEmptiedItemsShouldEmptyTheMenu()
    {
        // An empty Items is a real state: a menu whose items are taken away has to lose them.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(2, com.FindAll(".bit-mnb-itm").Count);

        com.Render(parameters => parameters.Add(p => p.Items, new List<BitMenuButtonItem>()));

        Assert.AreEqual(0, com.FindAll(".bit-mnb-itm").Count);
    }

    [TestMethod]
    public void BitMenuButtonItemsFilledInPlaceShouldBeRendered()
    {
        // The same list instance, longer than it was, is not the one already rendered.
        var live = new List<BitMenuButtonItem>();
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, live);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-itm").Count);

        live.Add(new BitMenuButtonItem { Text = "Item A", Key = "A" });
        com.Render();

        Assert.AreEqual(1, com.FindAll(".bit-mnb-itm").Count);
    }

    [TestMethod]
    public void BitMenuButtonSplitChevronShouldToggleTheCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
        });

        com.Find(".bit-mnb-chb").Click();
        Assert.IsTrue(com.Instance.IsOpen);

        com.Find(".bit-mnb-chb").Click();
        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod]
    public void BitMenuButtonRadioItemsShouldHaveRadioSemanticsAndTheirOwnMark()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Name", Key = "name", RadioGroup = "sort", IsChecked = true },
                new() { Text = "Size", Key = "size", RadioGroup = "sort" },
                new() { Text = "Wrap", Key = "wrap", Checkable = true }
            });
        });

        var menuItems = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual("menuitemradio", menuItems[0].GetAttribute("role"));
        Assert.AreEqual("true", menuItems[0].GetAttribute("aria-checked"));
        Assert.IsTrue(menuItems[0].ClassList.Contains("bit-mnb-chk"));

        Assert.AreEqual("menuitemradio", menuItems[1].GetAttribute("role"));
        Assert.AreEqual("false", menuItems[1].GetAttribute("aria-checked"));

        // A check item beside a radio one keeps its own role and its own mark.
        Assert.AreEqual("menuitemcheckbox", menuItems[2].GetAttribute("role"));

        var marks = com.FindAll(".bit-mnb-ick");

        Assert.AreEqual(3, marks.Count);
        Assert.IsTrue(marks[0].ClassList.Any(c => c.Contains("StatusCircleInner")));
        Assert.IsTrue(marks[2].ClassList.Any(c => c.Contains("Accept")));
    }

    [TestMethod]
    public void BitMenuButtonPickingARadioItemShouldClearTheRestOfItsGroup()
    {
        var radioItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Name", Key = "name", RadioGroup = "sort", IsChecked = true },
            new() { Text = "Size", Key = "size", RadioGroup = "sort" },
            new() { Text = "Ascending", Key = "asc", RadioGroup = "order", IsChecked = true },
            new() { Text = "Wrap", Key = "wrap", Checkable = true, IsChecked = true }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, radioItems);
            parameters.Add(p => p.CloseOnItemClick, false);
        });

        com.FindAll(".bit-mnb-itm")[1].Click();

        Assert.IsFalse(radioItems[0].IsChecked);
        Assert.IsTrue(radioItems[1].IsChecked);
        // Another group, and an independent check item, are left alone.
        Assert.IsTrue(radioItems[2].IsChecked);
        Assert.IsTrue(radioItems[3].IsChecked);

        // Picking one of a set of choices is picking it, not toggling it: the group is never left empty.
        com.FindAll(".bit-mnb-itm")[1].Click();

        Assert.IsTrue(radioItems[1].IsChecked);
    }

    [TestMethod]
    public void BitMenuButtonARadioGroupShouldReachIntoTheSubmenusOfTheMenuButton()
    {
        var nested = new BitMenuButtonItem { Text = "Size", Key = "size", RadioGroup = "sort" };
        var items = new List<BitMenuButtonItem>
        {
            new() { Text = "Name", Key = "name", RadioGroup = "sort", IsChecked = true },
            new() { Text = "More", Key = "more", ChildItems = [nested] }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CloseOnItemClick, false);
        });

        com.Find(".bit-mnb-sub .bit-mnb-itm").Click();

        Assert.IsTrue(nested.IsChecked);
        Assert.IsFalse(items[0].IsChecked);
    }

    [TestMethod]
    public void BitMenuButtonGenericItemsShouldTakeTheirRadioGroupFromTheNameSelectors()
    {
        var items = new List<RadioModel>
        {
            new() { Label = "Name", Group = "sort", Selected = true },
            new() { Label = "Size", Group = "sort" }
        };

        var com = RenderComponent<BitMenuButton<RadioModel>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, new BitMenuButtonNameSelectors<RadioModel>
            {
                Text = { Name = nameof(RadioModel.Label) },
                RadioGroup = { Name = nameof(RadioModel.Group) },
                IsChecked = { Name = nameof(RadioModel.Selected) }
            });
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CloseOnItemClick, false);
        });

        Assert.AreEqual("menuitemradio", com.Find(".bit-mnb-itm").GetAttribute("role"));

        com.FindAll(".bit-mnb-itm")[1].Click();

        Assert.IsFalse(items[0].Selected);
        Assert.IsTrue(items[1].Selected);
    }

    [TestMethod]
    public void BitMenuButtonOverlayClickShouldCloseTheCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        com.Find(".bit-mnb-opb").Click();
        Assert.IsTrue(com.Find(".bit-mnb").ClassList.Contains("bit-mnb-omn"));

        // The overlay takes the click that dismisses the menu, and hands the focus back to the trigger with it.
        com.Find(".bit-mnb-ovl").Click();
        Assert.IsFalse(com.Find(".bit-mnb").ClassList.Contains("bit-mnb-omn"));
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderOnlyTheAttributesItDeclares()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        // A Razor comment written inside a tag is parsed as attributes rather than as a comment, which puts
        // every word of it on the element. The rendered markup is checked for the one character that can
        // only have come from one.
        foreach (var element in com.FindAll(".bit-mnb, .bit-mnb-opb, .bit-mnb-itm"))
        {
            foreach (var attribute in element.Attributes)
            {
                Assert.IsFalse(attribute.Name.Contains('@'), $"Unexpected attribute '{attribute.Name}'.");
            }
        }
    }

    [TestMethod]
    public void BitMenuButtonIconOnlyShouldDropTheTextAndTheChevron()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, "More");
            parameters.Add(p => p.IconName, "More");
            parameters.Add(p => p.AriaLabel, "More actions");
            parameters.Add(p => p.IconOnly, true);
        });

        Assert.IsTrue(com.Find(".bit-mnb").ClassList.Contains("bit-mnb-ion"));
        Assert.IsNotNull(com.Find(".bit-mnb-opb .bit-icon--More"));
        Assert.AreEqual(0, com.FindAll(".bit-mnb-opb .bit-mnb-btx").Count);
        Assert.AreEqual(0, com.FindAll(".bit-mnb-chv").Count);

        // The button has no text left, so what says what it does is its name and its popup semantics.
        var operatorButton = com.Find(".bit-mnb-opb");
        Assert.AreEqual("More actions", operatorButton.GetAttribute("aria-label"));
        Assert.AreEqual("menu", operatorButton.GetAttribute("aria-haspopup"));
    }

    [TestMethod]
    public void BitMenuButtonIconOnlySplitShouldKeepTheChevronHalf()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.IconName, "Save");
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.Split, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-opb .bit-mnb-btx").Count);
        // The chevron half is the only way into the menu of a split button, so it stays whatever the header does.
        Assert.IsNotNull(com.Find(".bit-mnb-chb .bit-mnb-chv"));
    }

    [TestMethod]
    public void BitMenuButtonIconOnlyStickyShouldDropTheSelectedItemText()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.IconOnly, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mnb-opb .bit-mnb-btx").Count);
    }

    [TestMethod]
    public void BitMenuButtonShouldNameItsRootToTheCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Style, "--bit-MenuButton-callout-background: red;");
        });

        com.Find(".bit-mnb-opb").Click();

        // The callout is a sibling of the root, so the root is named to Callouts.toggle - its last argument -
        // for what the root declares to go on reaching the callout once it is relocated to the body.
        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(com.Find(".bit-mnb").Id, toggle.Arguments[^2]);
        Assert.IsFalse((com.Find(".bit-mnb-cal").GetAttribute("style") ?? "").Contains("--bit-MenuButton-"));
    }

    [TestMethod]
    public void BitMenuButtonShouldKeepItsToggledStyleOutOfThePopupStyle()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Toggle, true);
            parameters.Add(p => p.DefaultIsToggled, true);
            parameters.Add(p => p.Style, "--bit-MenuButton-callout-background:red");
            parameters.Add(p => p.Styles, new BitMenuButtonClassStyles { Toggled = "--bit-MenuButton-background:green" });
        });

        // The toggled look is the button's alone, so the root names the style it has without it for the chain the
        // callout is relocated into to copy instead.
        var root = com.Find(".bit-mnb");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-MenuButton-background:green");
        Assert.AreEqual("--bit-MenuButton-callout-background:red", root.GetAttribute("data-bit-popup-style"));

        com.Find(".bit-mnb-opb").Click();

        Assert.IsFalse(com.Find(".bit-mnb").HasAttribute("data-bit-popup-style"));
    }

    [TestMethod]
    public void BitMenuButtonShouldRenderTheBaseParametersOnItsRootAndItsCallout()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Id, "the-id");
            parameters.Add(p => p.Class, "the-class");
            parameters.Add(p => p.Style, "margin: 1px;");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.ForceAnimation, true);
        });

        var root = com.Find(".bit-mnb");
        var callout = com.Find(".bit-mnb-cal");

        Assert.AreEqual("the-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("the-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-fam"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");

        // The callout is a sibling of the root, so the direction and the animation opt-out are written on it as well.
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-fam"));
        Assert.AreEqual($"{root.Id}-opb", com.Find(".bit-mnb-opb").Id);
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitMenuButtonShouldFollowTheVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = com.Find(".bit-mnb").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    public void BitMenuButtonShouldKeepTheAttributesWrittenByHandOnItsRoot()
    {
        var com = Context.Render(builder =>
        {
            builder.OpenComponent<BitMenuButton<BitMenuButtonItem>>(0);
            builder.AddAttribute(1, nameof(BitMenuButton<BitMenuButtonItem>.Items), items);
            builder.AddAttribute(2, "data-test", "menu");
            builder.AddAttribute(3, "aria-label", "Commands");
            builder.CloseComponent();
        });

        Assert.AreEqual("menu", com.Find(".bit-mnb").GetAttribute("data-test"));
        // the hyphenated name cannot reach the AriaLabel parameter, so it is resolved onto the header button by hand
        Assert.AreEqual("Commands", com.Find(".bit-mnb-opb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMenuButtonDisabledShouldMarkTheRoot()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Disabled, true);
        });

        Assert.IsTrue(com.Find(".bit-mnb").ClassList.Contains("bit-dis"));

        // a disabled menu button opens nothing, from the pointer or the keyboard
        com.Find(".bit-mnb-opb").Click();
        com.Find(".bit-mnb-opb").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.IsFalse(com.Instance.IsOpen);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonStopPropagationShouldBeRenderedOnBothHalves(bool stopPropagation)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.StopPropagation, stopPropagation);
        });

        Assert.AreEqual(stopPropagation, com.Find(".bit-mnb-opb").HasAttribute("blazor:onclick:stopPropagation"));
        Assert.AreEqual(stopPropagation, com.Find(".bit-mnb-chb").HasAttribute("blazor:onclick:stopPropagation"));
    }

    [TestMethod]
    public void BitMenuButtonStickyShouldStartFromTheDefaultSelectedItemAndReportTheSelection()
    {
        var stickyItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Item A", Key = "A" },
            new() { Text = "Item B", Key = "B" }
        };

        BitMenuButtonItem? changed = null;
        BitMenuButtonItem? bound = null;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, stickyItems);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.DefaultSelectedItem, stickyItems[1]);
            parameters.Add(p => p.OnChange, (BitMenuButtonItem item) => changed = item);
            parameters.Add(p => p.SelectedItemChanged, (BitMenuButtonItem? item) => bound = item);
        });

        Assert.AreEqual("Item B", com.Find(".bit-mnb-opb .bit-mnb-btx").TextContent.Trim());
        Assert.AreSame(stickyItems[1], com.Instance.SelectedItem);

        // the selected item is left out of the menu, so the one row left is Item A
        com.Find(".bit-mnb-itm").Click();

        Assert.AreSame(stickyItems[0], changed);
        Assert.AreSame(stickyItems[0], bound);
        Assert.AreEqual("Item A", com.Find(".bit-mnb-opb .bit-mnb-btx").TextContent.Trim());
    }

    [TestMethod]
    public void BitMenuButtonShouldWriteIsOpenBack()
    {
        var isOpen = false;

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        com.Find(".bit-mnb-opb").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual("true", com.Find(".bit-mnb-opb").GetAttribute("aria-expanded"));

        com.Find("button.bit-mnb-itm").Click();

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitMenuButtonHeaderTemplateShouldReplaceTheTextAndTheIconOfTheHeader()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.IconName, "Save");
            parameters.Add(p => p.HeaderTemplate, "<span class=\"hdr\">H</span>");
        });

        var operatorButton = com.Find(".bit-mnb-opb");

        Assert.IsNotNull(operatorButton.QuerySelector(".hdr"));
        Assert.IsNull(operatorButton.QuerySelector(".bit-mnb-btx"));
        Assert.IsNull(operatorButton.QuerySelector(".bit-mnb-ico"));
        // the chevron is what says the header opens a menu, so it stays beside the template
        Assert.IsNotNull(operatorButton.QuerySelector(".bit-mnb-chv"));
    }

    [TestMethod]
    public void BitMenuButtonItemTemplateShouldRenderEveryItemAndAnItemsOwnTemplateShouldWinOverIt()
    {
        var templateItems = new List<BitMenuButtonItem>
        {
            new() { Text = "Own", Key = "own", Template = item => builder => builder.AddMarkupContent(0, $"<em class=\"own\">{item.Text}</em>") },
            new() { Text = "Shared", Key = "shared" }
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, templateItems);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<BitMenuButtonItem?>)(item => builder => builder.AddMarkupContent(0, $"<strong class=\"shared\">{item?.Text}</strong>")));
        });

        var menuItems = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual("Own", menuItems[0].QuerySelector(".own")!.TextContent);
        Assert.IsNull(menuItems[0].QuerySelector(".shared"));
        Assert.AreEqual("Shared", menuItems[1].QuerySelector(".shared")!.TextContent);
        Assert.IsEmpty(com.FindAll(".bit-mnb-itm .bit-mnb-btx"));
    }

    [TestMethod]
    public void BitMenuButtonItemShouldCarryItsOwnClassAndStyle()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Styled", Key = "s", Class = "item-class", Style = "color: red;" }
            });
        });

        var item = com.Find(".bit-mnb-itm");

        Assert.IsTrue(item.ClassList.Contains("item-class"));
        StringAssert.Contains(item.GetAttribute("style"), "color: red;");
    }

    [TestMethod]
    public void BitMenuButtonShouldApplyTheClassesAndStylesOfEveryPart()
    {
        var classes = new BitMenuButtonClassStyles
        {
            Icon = "c-icon",
            Text = "c-text",
            ChevronDown = "c-chevron",
            ChevronDownButton = "c-chevron-button",
            Separator = "c-separator",
            Callout = "c-callout",
            CalloutContainer = "c-container",
            ItemWrapper = "c-wrapper",
            ItemButton = "c-item",
            ItemIcon = "c-item-icon",
            ItemText = "c-item-text",
            ItemSecondaryText = "c-item-secondary",
            ItemSeparator = "c-item-separator",
            ItemHeader = "c-item-header",
            ItemCheckIcon = "c-item-check",
            Overlay = "c-overlay",
        };

        var styles = new BitMenuButtonClassStyles
        {
            Icon = "color: red;",
            Text = "color: blue;",
            ChevronDown = "color: green;",
            ChevronDownButton = "color: gray;",
            Separator = "color: gold;",
            Callout = "color: pink;",
            CalloutContainer = "color: teal;",
            ItemWrapper = "color: navy;",
            ItemButton = "color: olive;",
            ItemIcon = "color: lime;",
            ItemText = "color: aqua;",
            ItemSecondaryText = "color: maroon;",
            ItemSeparator = "color: silver;",
            ItemHeader = "color: purple;",
            ItemCheckIcon = "color: orange;",
            Overlay = "color: brown;",
        };

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Group", IsHeader = true },
                new() { Text = "Save", Key = "save", IconName = "Save", SecondaryText = "Ctrl+S", Checkable = true },
                new() { IsSeparator = true },
                new() { Text = "Plain", Key = "plain" }
            });
            parameters.Add(p => p.Split, true);
            parameters.Add(p => p.Text, "Save");
            parameters.Add(p => p.IconName, "Save");
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });

        void AssertPart(string selector, string cssClass, string style)
        {
            var element = com.Find(selector);

            Assert.IsTrue(element.ClassList.Contains(cssClass), $"{selector} lacks {cssClass}.");
            StringAssert.Contains(element.GetAttribute("style"), style, $"{selector} lacks the style {style}.");
        }

        AssertPart(".bit-mnb-opb .bit-mnb-ico", "c-icon", "color: red;");
        AssertPart(".bit-mnb-opb .bit-mnb-btx", "c-text", "color: blue;");
        AssertPart(".bit-mnb-chb .bit-mnb-chv", "c-chevron", "color: green;");
        AssertPart(".bit-mnb-chb", "c-chevron-button", "color: gray;");
        AssertPart(".bit-mnb-spb", "c-separator", "color: gold;");
        AssertPart(".bit-mnb-cal", "c-callout", "color: pink;");
        AssertPart(".bit-mnb-cul", "c-container", "color: teal;");
        AssertPart("li.c-wrapper", "c-wrapper", "color: navy;");
        AssertPart(".bit-mnb-itm", "c-item", "color: olive;");
        AssertPart(".bit-mnb-iic", "c-item-icon", "color: lime;");
        AssertPart(".bit-mnb-itm .bit-mnb-btx", "c-item-text", "color: aqua;");
        AssertPart(".bit-mnb-isc", "c-item-secondary", "color: maroon;");
        AssertPart(".bit-mnb-isp", "c-item-separator", "color: silver;");
        AssertPart(".bit-mnb-ihd", "c-item-header", "color: purple;");
        AssertPart(".bit-mnb-ick", "c-item-check", "color: orange;");
        AssertPart(".bit-mnb-ovl", "c-overlay", "color: brown;");

        // the wrapper is every row that is an item, never the header or the separator
        Assert.AreEqual(2, com.FindAll("li.c-wrapper").Count);
    }

    [TestMethod]
    public void BitMenuButtonShouldDrawTheCheckRadioAndSubmenuMarksWithTheGlyphsItIsGiven()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem>
            {
                new() { Text = "Wrap", Key = "wrap", Checkable = true },
                new() { Text = "Name", Key = "name", RadioGroup = "sort" },
                new() { Text = "More", Key = "more", ChildItems = [new() { Text = "Child", Key = "child" }] }
            });
            parameters.Add(p => p.CheckIconName, "CheckMark");
            parameters.Add(p => p.RadioIconName, "RadioBtnOn");
            parameters.Add(p => p.SubmenuIconName, "ChevronRightMed");
        });

        var marks = com.FindAll(".bit-mnb-cal:not(.bit-mnb-sub) > ul > li > .bit-mnb-itm > .bit-mnb-ick");

        Assert.IsTrue(marks[0].ClassList.Contains("bit-icon--CheckMark"));
        Assert.IsTrue(marks[1].ClassList.Contains("bit-icon--RadioBtnOn"));
        Assert.IsTrue(com.Find(".bit-mnb-sch").ClassList.Contains("bit-icon--ChevronRightMed"));

        // an icon of an external library wins over the built-in name
        var external = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitMenuButtonItem> { new() { Text = "Wrap", Key = "wrap", Checkable = true } });
            parameters.Add(p => p.CheckIcon, BitIconInfo.Fa("solid check"));
            parameters.Add(p => p.CheckIconName, "CheckMark");
        });

        Assert.IsTrue(external.Find(".bit-mnb-ick").ClassList.Contains("fa-check"));
        Assert.IsFalse(external.Find(".bit-mnb-ick").ClassList.Contains("bit-icon--CheckMark"));
    }

    [TestMethod]
    public async Task BitMenuButtonFocusAsyncShouldFocusTheHeaderButton()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        await com.Instance.FocusAsync();

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public async Task BitMenuButtonShouldClearItsCalloutAndItsScriptWhenItIsDisposed()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MenuButtons.setup"));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.clear"));
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MenuButtons.dispose"));
    }

    [TestMethod]
    public void BitMenuButtonGenericItemsShouldBeReadThroughTheNamesOfTheSelectors()
    {
        var models = new List<MenuModel>
        {
            new() { Label = "Group", Head = true },
            new() { Label = "Open", Glyph = "OpenFile", Tip = "Open a file", Sub = "Ctrl+O", Css = "open-item", Inline = "color: red;", Id = "open" },
            new() { Sep = true },
            new() { Label = "Locked", Off = true, Id = "locked" },
            new() { Label = "Docs", Url = "https://bitplatform.dev", Id = "docs" }
        };

        var com = RenderComponent<BitMenuButton<MenuModel>>(parameters =>
        {
            parameters.Add(p => p.NameSelectors, new BitMenuButtonNameSelectors<MenuModel>
            {
                Text = { Name = nameof(MenuModel.Label) },
                IconName = { Name = nameof(MenuModel.Glyph) },
                Href = { Name = nameof(MenuModel.Url) },
                Title = { Name = nameof(MenuModel.Tip) },
                SecondaryText = { Name = nameof(MenuModel.Sub) },
                IsDisabled = { Name = nameof(MenuModel.Off) },
                IsSeparator = { Name = nameof(MenuModel.Sep) },
                IsHeader = { Name = nameof(MenuModel.Head) },
                Class = { Name = nameof(MenuModel.Css) },
                Style = { Name = nameof(MenuModel.Inline) },
                Key = { Name = nameof(MenuModel.Id) },
            });
            parameters.Add(p => p.Items, models);
        });

        Assert.AreEqual("Group", com.Find(".bit-mnb-ihd").TextContent.Trim());
        Assert.AreEqual(1, com.FindAll(".bit-mnb-isp").Count);

        var menuItems = com.FindAll(".bit-mnb-itm");

        Assert.AreEqual(3, menuItems.Count);
        Assert.AreEqual("Open", menuItems[0].QuerySelector(".bit-mnb-btx")!.TextContent.Trim());
        Assert.IsTrue(menuItems[0].QuerySelector(".bit-mnb-iic")!.ClassList.Contains("bit-icon--OpenFile"));
        Assert.AreEqual("Open a file", menuItems[0].GetAttribute("title"));
        Assert.AreEqual("Ctrl+O", menuItems[0].QuerySelector(".bit-mnb-isc")!.TextContent.Trim());
        Assert.IsTrue(menuItems[0].ClassList.Contains("open-item"));
        StringAssert.Contains(menuItems[0].GetAttribute("style"), "color: red;");
        Assert.IsTrue(menuItems[1].HasAttribute("disabled"));
        Assert.AreEqual("A", menuItems[2].TagName);
        Assert.AreEqual("https://bitplatform.dev", menuItems[2].GetAttribute("href"));
    }

    [TestMethod]
    public void BitMenuButtonInsideAnEditFormShouldMakeItsItemsSubmitButtons()
    {
        var com = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, new object());
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<BitMenuButton<BitMenuButtonItem>>(0);
                builder.AddComponentParameter(1, nameof(BitMenuButton<BitMenuButtonItem>.Items), items);
                builder.CloseComponent();
            }));
        });

        // the items are the commands, so they take the form's default type; the header only opens the menu
        Assert.IsTrue(com.FindAll("button.bit-mnb-itm").All(b => b.GetAttribute("type") == "submit"));
        Assert.AreEqual("button", com.Find(".bit-mnb-opb").GetAttribute("type"));

        var explicitType = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, new object());
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<BitMenuButton<BitMenuButtonItem>>(0);
                builder.AddComponentParameter(1, nameof(BitMenuButton<BitMenuButtonItem>.Items), items);
                builder.AddComponentParameter(2, nameof(BitMenuButton<BitMenuButtonItem>.ButtonType), (BitButtonType?)BitButtonType.Button);
                builder.CloseComponent();
            }));
        });

        Assert.IsTrue(explicitType.FindAll("button.bit-mnb-itm").All(b => b.GetAttribute("type") == "button"));
    }

    private class RadioModel
    {
        public string? Label { get; set; }

        public string? Group { get; set; }

        public bool Selected { get; set; }
    }

    private class MenuModel
    {
        public string? Label { get; set; }

        public string? Glyph { get; set; }

        public string? Url { get; set; }

        public string? Tip { get; set; }

        public string? Sub { get; set; }

        public bool Off { get; set; }

        public bool Sep { get; set; }

        public bool Head { get; set; }

        public string? Css { get; set; }

        public string? Inline { get; set; }

        public string? Id { get; set; }
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitMenuButtonAutoFocusShouldFocusTheHeaderButtonOnceOnTheFirstRender(bool autoFocus, int expectedFocusCalls)
    {
        // A browser honors the autofocus attribute only while the document loads, so the first interactive render
        // gives the focus itself - once, never again on a later render.
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.AutoFocus, autoFocus);
        });

        com.Render();

        Assert.HasCount(expectedFocusCalls, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }
}
