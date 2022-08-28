using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitSplitButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true, BitButtonStyle.Primary),
        DataRow(Visual.Fluent, true, BitButtonStyle.Standard),
        DataRow(Visual.Fluent, false, BitButtonStyle.Primary),
        DataRow(Visual.Fluent, false, BitButtonStyle.Standard),

        DataRow(Visual.Cupertino, true, BitButtonStyle.Primary),
        DataRow(Visual.Cupertino, true, BitButtonStyle.Standard),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Primary),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Standard),

        DataRow(Visual.Material, true, BitButtonStyle.Primary),
        DataRow(Visual.Material, true, BitButtonStyle.Standard),
        DataRow(Visual.Material, false, BitButtonStyle.Primary),
        DataRow(Visual.Material, false, BitButtonStyle.Standard),
    ]
    public void BitSplitButtonTest(Visual visual, bool isEnabled, BitButtonStyle style)
    {
        List<BitSplitButtonItem> items = new()
        {
            new BitSplitButtonItem()
            {
                Text = "Item A",
                key = "A"
            }
        };

        var com = RenderComponent<BitSplitButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Items, items);
        });

        var bitSplitButton = com.Find(".bit-splb");
        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        var operatorButtonStyle = style is BitButtonStyle.Primary ? "primary" : "standard";
        var operatorButton = com.Find(".operator-btn");

        Assert.IsTrue(bitSplitButton.ClassList.Contains($"bit-splb-{isEnabledClass}-{visualClass}"));

        Assert.AreEqual(isEnabled, operatorButton.ClassList.Contains(operatorButtonStyle));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitSplitButtonShouldBeClickIfEnabled(bool isEnabled)
    {
        BitSplitButtonItem clickedItem = default!;
        List<BitSplitButtonItem> items = new()
        {
            new BitSplitButtonItem()
            {
                Text = "Item A",
                key = "A"
            }
        };

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
        List<BitSplitButtonItem> items = new()
        {
            new BitSplitButtonItem()
            {
                Text = "Item A",
                key = "A"
            },
            new BitSplitButtonItem()
            {
                Text = "Item B",
                key = "B",
                IsEnabled = itemIsEnabled
            }
        };

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
        List<BitSplitButtonItem> items = new()
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
}
