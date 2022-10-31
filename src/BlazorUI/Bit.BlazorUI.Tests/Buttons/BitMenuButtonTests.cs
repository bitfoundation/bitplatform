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
            key = "A"
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            key = "B"
        }
    };

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
    public void BitMenuButtonTest(Visual visual, bool isEnabled, BitButtonStyle style)
    {
        var com = RenderComponent<BitMenuButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Items, items);
        });

        var bitSplitButton = com.Find(".bit-mnb");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var buttonStyle = style is BitButtonStyle.Primary ? "primary" : "standard";

        Assert.IsTrue(bitSplitButton.ClassList.Contains($"bit-mnb-{isEnabledClass}-{visualClass}"));

        Assert.AreEqual(isEnabled, bitSplitButton.ClassList.Contains(buttonStyle));
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
}
