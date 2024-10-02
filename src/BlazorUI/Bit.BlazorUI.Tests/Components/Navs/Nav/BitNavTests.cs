using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

[TestClass]
public class BitNavTests : BunitTestContext
{
    [DataTestMethod,
      DataRow(false, true),
      DataRow(false, false),
      DataRow(true, true),
      DataRow(true, false)
  ]
    public void BitNavLinkItemMainClassTest(bool isEnabled, bool hasUrl)
    {
        var url = hasUrl ? "https://www.google.com/" : null;
        var navLinkItems = new List<BitNavItem> { new BitNavItem { Text = "test", IsEnabled = isEnabled, Url = url } };

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, navLinkItems);
        });

        if (isEnabled is false)
        {
            var element = component.Find(".bit-nav-ict");
            Assert.IsTrue(element.ClassList.Contains("bit-nav-dis"));
        }

        if (hasUrl)
        {
            var linkElement = component.Find("a");
            Assert.IsNotNull(linkElement);
        }
        else
        {
            var buttonElement = component.Find("button");
            Assert.IsNotNull(buttonElement);
        }
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitNavAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitNav = component.Find(".bit-nav");

        Assert.IsTrue(bitNav.GetAttribute("aria-label").Equals(ariaLabel));
    }

    [DataTestMethod,
        DataRow("collapseAriaLabel", "expandAriaLabel", false),
        DataRow("collapseAriaLabel", "expandAriaLabel", true)
    ]
    public void BitNavLinkItemAriaLabelTest(string collapseAriaLabel, string expandAriaLabel, bool isExpanded)
    {
        List<BitNavItem> navLinkItems = new()
        {
            new()
            {
                Text = "Home",
                CollapseAriaLabel = collapseAriaLabel,
                ExpandAriaLabel = expandAriaLabel,
                IsExpanded = isExpanded,
                ChildItems = new()
                {
                    new() { Text = "Activity", Url = "http://msn.com", Title = "Activity" }
                }
            }
        };

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, navLinkItems);
            parameters.Add(p => p.Mode, BitNavMode.Manual);
        });

        var button = component.Find(".bit-nav-cbt");
        var expectedResult = isExpanded ? collapseAriaLabel : expandAriaLabel;

        Assert.AreEqual(expectedResult, button.GetAttribute("aria-label"));
    }

    [DataTestMethod,
        DataRow(BitNavRenderType.Grouped),
        DataRow(BitNavRenderType.Normal)
    ]
    public void BitNavShouldRespectGroupItems(BitNavRenderType type)
    {
        List<BitNavItem> navLinkItems = new() { new() { Text = "test", Url = "https://www.google.com/" } };
        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, navLinkItems);
            parameters.Add(p => p.RenderType, type);
        });

        if (type == BitNavRenderType.Grouped)
        {
            Assert.IsNotNull(component.Find(".bit-nav-gcb"));
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-nav-gcb"));
        }
    }

    [DataTestMethod,
     DataRow(0),
     DataRow(1),
     DataRow(2),
    ]
    public void BitNavSelectedItemTest(int selectedItemIndex)
    {
        var selectedItem = BasicNavLinks[selectedItemIndex];

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, BasicNavLinks);
            parameters.Add(p => p.SelectedItem, selectedItem);
        });

        //TODO

        //var selectedItemTxt = component.Find(".selected .link-txt");
        //var expectedResult = BasicNavLinks.Find(i => i == selectedItem).Name;

        //Assert.AreEqual(expectedResult, selectedItemTxt.TextContent);
    }

    [DataTestMethod]
    public void BitNavOnLinkExpandClickTest()
    {
        var items = new List<BitNavItem>()
        {
            new() { Text = "Test1", ChildItems = new List<BitNavItem>() { new() { Text = "Test2" } } }
        };

        var componenet = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var button = componenet.Find(".bit-nav-cbt");

        //TODO: bypassed - BUnit or Blazor issue
        //button.Click();
        //Assert.AreEqual(items[0].Key, componenet.Instance.OnLinkExpandClickValue);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitNavLinkItemForceAnchorTest(bool isForced)
    {
        var items = new List<BitNavItem>()
        {
            new() {
                Text = "Test1",
                IsExpanded = true,
                ChildItems = new List<BitNavItem>() { new() { Text = "Test2", ForceAnchor = isForced } }
            }
        };

        var componenet = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        if (isForced)
        {
            var anchorTag = componenet.Find("a");
            Assert.IsNotNull(anchorTag);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => componenet.Find("a"));
        }
    }

    [DataTestMethod,
        DataRow(null, "name"),
        DataRow("", "name"),
        DataRow("title", "name")
    ]
    public void BitNavLinkItemTitleTest(string title, string name)
    {
        var items = new List<BitNavItem>()
        {
            new() { Text = name, Title = title, IsExpanded = true }
        };

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var navLinkItem = component.Find(".bit-nav-ict");

        if (title is null)
        {
            Assert.AreEqual(name, navLinkItem.GetAttribute("title"));
        }
        else
        {
            Assert.AreEqual(title, navLinkItem.GetAttribute("title"));
        }
    }

    private List<BitNavItem> BasicNavLinks = new List<BitNavItem>()
    {
        new BitNavItem { Text = "Activity", Url = "http://msn.com", Target = "_blank" },
        new BitNavItem { Text = "MSN", Url = "http://msn.com", IsEnabled = false, Target = "_blank" },
        new BitNavItem { Text = "Documents", Url = "http://example.com", Target = "_blank", IsExpanded = true },
        new BitNavItem { Text = "Pages", Url = "http://msn.com", Target = "_parent" },
        new BitNavItem { Text = "Notebook", Url = "http://msn.com", IsEnabled = false },
        new BitNavItem { Text = "Communication and Media", Url = "http://msn.com", Target = "_top" },
        new BitNavItem { Text = "News", Title = "News", Url = "http://msn.com", IconName = "News", Target = "_self" },
    };
}
