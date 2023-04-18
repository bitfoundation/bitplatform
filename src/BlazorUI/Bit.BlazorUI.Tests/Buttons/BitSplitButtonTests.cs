using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitSplitButtonTests : BunitTestContext
{
    private readonly List<BitSplitButtonItem> items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            Key = "A"
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            Key = "B"
        }
    };

    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary),
        DataRow(true, BitButtonStyle.Standard),
        DataRow(false, BitButtonStyle.Primary),
        DataRow(false, BitButtonStyle.Standard),
    ]
    public void BitSplitButtonTest(bool isEnabled, BitButtonStyle bitButtonStyle)
    {
        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, bitButtonStyle);
            parameters.Add(p => p.Items, items);
        });

        var bitSplitButton = com.Find(".bit-spl");

        if (isEnabled)
        {
            Assert.IsFalse(bitSplitButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitSplitButton.ClassList.Contains("bit-dis"));
        }

        var buttonStyle = bitButtonStyle is BitButtonStyle.Primary ? "bit-spl-pri" : "bit-spl-std";
        Assert.AreEqual(isEnabled, bitSplitButton.ClassList.Contains(buttonStyle));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSplitButtonShouldBeClickIfEnabled(bool isEnabled)
    {
        BitSplitButtonItem clickedItem = default!;

        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var operatorButton = com.Find(".bit-spl-opb");
        operatorButton.Click();

        if (isEnabled)
        {
            Assert.AreEqual(clickedItem, items.First());
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
    public void BitSplitButtonShouldBeItemClickIfEnabled(bool itemIsEnabled)
    {
        BitSplitButtonItem clickedItem = default!;

        items.Last().IsEnabled = itemIsEnabled;

        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .bit-spl-itm");
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
    public void BitSplitButtonIsStickyTest(bool isSticky)
    {
        BitSplitButtonItem clickedItem = default!;

        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsSticky, isSticky);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .bit-spl-itm");
        lastItem.Click();

        var operatorButton = com.Find(".bit-spl-opb");
        operatorButton.Click();

        if (isSticky)
        {
            Assert.AreEqual(clickedItem, items.Last());
        }
        else
        {
            Assert.AreEqual(clickedItem, items.First());
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSplitButtonOpenMenu(bool isEnabled)
    {
        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = com.Find("button.bit-spl-chb");
        var bitSplitButton = com.Find(".bit-spl");
        Assert.IsFalse(bitSplitButton.ClassList.Contains("bit-spl-omn"));
        button.Click();

        if (isEnabled)
        {
            Assert.IsTrue(bitSplitButton.ClassList.Contains("bit-spl-omn"));
        }
        else
        {
            Assert.IsFalse(bitSplitButton.ClassList.Contains("bit-spl-omn"));
        }
    }

    [DataTestMethod,
        DataRow(BitButtonSize.Small),
        DataRow(BitButtonSize.Medium),
        DataRow(BitButtonSize.Large),
        DataRow(null)
    ]
    public void BitSplitButtonSizeTest(BitButtonSize? size)
    {
        var com = RenderComponent<BitSplitButton<BitSplitButtonItem>>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.ButtonSize, size.Value);
            }
        });

        var sizeClass = size switch
        {
            BitButtonSize.Small => "bit-spl-sm",
            BitButtonSize.Medium => "bit-spl-md",
            BitButtonSize.Large => "bit-spl-lg",
            _ => "bit-spl-md",
        };

        var bitSplitButton = com.Find(".bit-spl");

        Assert.IsTrue(bitSplitButton.ClassList.Contains(sizeClass));
    }
}
