using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Navs;

[TestClass]
public class BitNavTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(Visual.Fluent),
       DataRow(Visual.Cupertino),
       DataRow(Visual.Material),
    ]
    public void BitNavVisualClassTest(Visual visual)
    {
        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
        });

        var bitNav = component.Find(".bit-nav");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{visualClass}"));
    }

    [DataTestMethod,
      DataRow(Visual.Fluent, false, true),
      DataRow(Visual.Fluent, false, false),
      DataRow(Visual.Fluent, true, true),
      DataRow(Visual.Fluent, true, false),

      DataRow(Visual.Cupertino, false, true),
      DataRow(Visual.Cupertino, false, false),
      DataRow(Visual.Cupertino, true, true),
      DataRow(Visual.Cupertino, true, false),

      DataRow(Visual.Material, false, true),
      DataRow(Visual.Material, false, false),
      DataRow(Visual.Material, true, true),
      DataRow(Visual.Material, true, false),
  ]
    public void BitNavLinkItemMainClassTest(Visual visual, bool isEnabled, bool hasUrl)
    {
        string url = hasUrl ? "https://www.google.com/" : null;
        var navLinkItems = new List<BitNavItem> { new BitNavItem { Text = "test", IsEnabled = isEnabled, Url = url } };

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, navLinkItems);
            parameters.Add(p => p.Visual, visual);
        });

        if (isEnabled is false)
        {
            var element = component.Find(".item-container");
            Assert.IsTrue(element.ClassList.Contains("disabled"));
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
            new BitNavItem()
            {
                Text = "Home",
                CollapseAriaLabel = collapseAriaLabel,
                ExpandAriaLabel = expandAriaLabel,
                IsExpanded = isExpanded,
                Items = new List<BitNavItem>()
                {
                    new BitNavItem() { Text = "Activity", Url = "http://msn.com", Title = "Activity" }
                }
            }
        };

        var component = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, navLinkItems);
            parameters.Add(p => p.Mode, BitNavMode.Manual);
        });

        var button = component.Find("button");
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
            Assert.IsNotNull(component.Find(".group-chevron-btn"));
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".group-chevron-btn"));
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
            new() { Text = "Test1", Items = new List<BitNavItem>() { new() { Text = "Test2" } } }
        };

        var componenet = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var button = componenet.Find(".chevron-btn");

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
                Items = new List<BitNavItem>() { new() { Text = "Test2", ForceAnchor = isForced } }
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

        var componenet = RenderComponent<BitNavTest>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var navLinkItem = componenet.Find(".item");

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
        new BitNavItem { Text = "News", Title = "News", Url = "http://msn.com", IconName = BitIconName.News, Target = "_self" },
    };
}
