using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

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

    [DataTestMethod,
       DataRow(true, BitButtonStyle.Primary),
       DataRow(true, BitButtonStyle.Standard),
       DataRow(false, BitButtonStyle.Primary),
       DataRow(false, BitButtonStyle.Standard)
    ]
    public void BitMenuButtonTest(bool isEnabled, BitButtonStyle bitButtonStyle)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, bitButtonStyle);
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

        var buttonStyle = bitButtonStyle is BitButtonStyle.Primary ? "bit-mnb-pri" : "bit-mnb-std";
        Assert.AreEqual(isEnabled, bitMenuButton.ClassList.Contains(buttonStyle));
    }

    [DataTestMethod,
        DataRow("A", BitIconName.Add),
        DataRow("B", BitIconName.Edit)
    ]
    public void BitMenuButtonShouldHasTextAndIcon(string text, BitIconName iconName)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Text, text);
            parameters.Add(p => p.IconName, iconName);
        });

        var iconNameClass = $"bit-icon--{iconName.GetName()}";

        var menuButtonIcon = com.Find(".bit-mnb-mbt .bit-icon");

        var menuButtonText = com.Find(".bit-mnb-mbt .bit-mnb-txt");

        Assert.IsTrue(menuButtonIcon.ClassList.Contains(iconNameClass));

        Assert.AreEqual(text, menuButtonText.TextContent);
    }

    [DataTestMethod,
        DataRow("A", BitIconName.Add),
        DataRow("B", BitIconName.Edit)
    ]
    public void BitMenuButtonShouldHasTextAndIconInItem(string itemText, BitIconName itemIconName)
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

        var itemIconNameClass = $"bit-icon--{itemIconName.GetName()}";

        var menuButtonItemIcon = com.Find("li .bit-mnb-itm .bit-icon");

        var menuButtonItemText = com.Find("li .bit-mnb-itm span");

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

        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnItemClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .bit-mnb-itm");
        lastItem.Click();

        Assert.AreEqual(itemIsEnabled, lastItem.ClassList.Contains("bit-dis") is false);

        if (itemIsEnabled)
        {
            Assert.AreEqual(clickedItem, items.Last());
        }
        else
        {
            Assert.IsNull(clickedItem);
        }
    }

    [DataTestMethod,
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

        var button = com.Find("button.bit-mnb-mbt");
        var bitMenuButton = com.Find(".bit-mnb");
        Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        button.Click();

        if (isEnabled)
        {
            Assert.IsTrue(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        }
        else
        {
            Assert.IsFalse(bitMenuButton.ClassList.Contains("bit-mnb-omn"));
        }
    }

    [DataTestMethod,
        DataRow(BitButtonSize.Small),
        DataRow(BitButtonSize.Medium),
        DataRow(BitButtonSize.Large),
        DataRow(null)
    ]
    public void BitMenuButtonSizeTest(BitButtonSize? size)
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.ButtonSize, size.Value);
            }
        });

        var sizeClass = size switch
        {
            BitButtonSize.Small => "bit-mnb-sm",
            BitButtonSize.Medium => "bit-mnb-md",
            BitButtonSize.Large => "bit-mnb-lg",
            _ => "bit-mnb-md",
        };

        var bitMenuButton = com.Find(".bit-mnb");

        Assert.IsTrue(bitMenuButton.ClassList.Contains(sizeClass));
    }
}
