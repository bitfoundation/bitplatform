using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitMenuButtonTests : BunitTestContext
{
    private List<BitMenuButtonItem> items = new()
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

    [DataTestMethod,
       DataRow(true, BitButtonStyle.Primary),
       DataRow(true, BitButtonStyle.Standard),
       DataRow(false, BitButtonStyle.Primary),
       DataRow(false, BitButtonStyle.Standard)
    ]
    public void BitMenuButtonTest(bool isEnabled, BitButtonStyle bitButtonStyle)
    {
        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, bitButtonStyle);
            parameters.Add(p => p.Items, items);
        });

        var bitMenuButton = com.Find(".bit-mnb");

        if (isEnabled)
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("disabled"));
        }

        var buttonStyle = bitButtonStyle is BitButtonStyle.Primary ? "primary" : "standard";
        Assert.AreEqual(isEnabled, bitMenuButton.ClassList.Contains(buttonStyle));
    }

    [DataTestMethod,
        DataRow("A", BitIconName.Add),
        DataRow("B", BitIconName.Edit)
    ]
    public void BitMenuButtonShouldHasTextAndIcon(string text, BitIconName iconName)
    {
        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, text);
            parameters.Add(p => p.IconName, iconName);
        });

        var iconNameClass = $"bit-icon--{iconName.GetName()}";

        var menuButtonIcon = com.Find(".menu-btn .bit-icon");

        var menuButtonText = com.Find(".menu-btn .text");

        Assert.IsTrue(menuButtonIcon.ClassList.Contains(iconNameClass));

        Assert.AreEqual(text, menuButtonText.TextContent);
    }

    [DataTestMethod,
        DataRow("A", BitIconName.Add),
        DataRow("B", BitIconName.Edit)
    ]
    public void BitMenuButtonShouldHasTextAndIconInItem(string itemText, BitIconName itemIconName)
    {
        var items = new List<BitMenuButtonItem>()
        {
            new BitMenuButtonItem()
            {
                Text = itemText,
                IconName = itemIconName
            }
        };

        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var itemIconNameClass = $"bit-icon--{itemIconName.GetName()}";

        var menuButtonItemIcon = com.Find("li .item .bit-icon");

        var menuButtonItemText = com.Find("li .item span");

        Assert.IsTrue(menuButtonItemIcon.ClassList.Contains(itemIconNameClass));

        Assert.AreEqual(itemText, menuButtonItemText.TextContent);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonShouldBeItemClickIfEnabled(bool itemIsEnabled)
    {
        BitMenuButtonItem clickedItem = default!;

        items.Last().IsEnabled = itemIsEnabled;

        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnItemClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .item");
        lastItem.Click();

        Assert.AreEqual(itemIsEnabled, lastItem.ClassList.Contains("disabled") is false);

        if (itemIsEnabled)
        {
            Assert.AreEqual(clickedItem, items.Last());
        }
        else
        {
            Assert.AreEqual(clickedItem, null);
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMenuButtonOpenMenu(bool isEnabled)
    {
        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = com.Find("button.menu-btn");
        var bitMenuButton = com.Find(".bit-mnb");
        Assert.IsFalse(bitMenuButton.ClassList.Contains("open-menu"));
        button.Click();

        if (isEnabled)
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("open-menu"));
        }
        else
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("open-menu"));
        }
    }
}
