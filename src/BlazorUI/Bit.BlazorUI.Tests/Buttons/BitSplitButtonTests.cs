using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitSplitButtonTests : BunitTestContext
{
    private List<BitSplitButtonItem> items = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            key = "A"
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            key = "B"
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
        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, bitButtonStyle);
            parameters.Add(p => p.Items, items);
        });

        var bitSplitButton = com.Find(".bit-splb");

        if (isEnabled)
        {
            Assert.IsFalse(bitSplitButton.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitSplitButton.ClassList.Contains("disabled"));
        }

        var buttonStyle = bitButtonStyle is BitButtonStyle.Primary ? "primary" : "standard";
        Assert.AreEqual(isEnabled, bitSplitButton.ClassList.Contains(buttonStyle));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSplitButtonShouldBeClickIfEnabled(bool isEnabled)
    {
        BitSplitButtonItem clickedItem = default!;

        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var operatorButton = com.Find(".operator-btn");
        operatorButton.Click();

        if (isEnabled)
        {
            Assert.AreEqual(clickedItem, items.First());
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
    public void BitSplitButtonShouldBeItemClickIfEnabled(bool itemIsEnabled)
    {
        BitSplitButtonItem clickedItem = default!;

        items.Last().IsEnabled = itemIsEnabled;

        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
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
    public void BitSplitButtonIsStickyTest(bool isSticky)
    {
        BitSplitButtonItem clickedItem = default!;

        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsSticky, isSticky);
            parameters.Add(p => p.OnClick, (item) => clickedItem = item);
        });

        var lastItem = com.Find("li:last-child .item");
        lastItem.Click();

        var operatorButton = com.Find(".operator-btn");
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
        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = com.Find("button.chevron-btn");
        var bitSplitButton = com.Find(".bit-splb");
        Assert.IsFalse(bitSplitButton.ClassList.Contains("open-menu"));
        button.Click();

        if (isEnabled)
        {
            Assert.IsTrue(bitSplitButton.ClassList.Contains("open-menu"));
        }
        else
        {
            Assert.IsFalse(bitSplitButton.ClassList.Contains("open-menu"));
        }
    }
}
