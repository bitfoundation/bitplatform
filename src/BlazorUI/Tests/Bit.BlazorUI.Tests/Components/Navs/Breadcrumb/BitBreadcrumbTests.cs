using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbTests : BunitTestContext
{
    [TestMethod,
      DataRow("Separator")
    ]
    public void BitBreadcrumbShouldTakeDividerIcon(string icon)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerIconName, icon);
        });

        var breadcrumbDividerIcon = component.Find(".bit-brc ol i");

        Assert.IsTrue(breadcrumbDividerIcon.ClassList.Contains($"bit-icon--{icon}"));
    }

    [TestMethod,
      DataRow((uint)0),
      DataRow((uint)3)
    ]
    public void BitBreadcrumbShouldRespectMaxDisplayItems(uint maxDisplayedItems)
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, breadcrumbItems);
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadcrumbElements = component.FindAll(".bit-brc ol.bit-brc-icn li a");

        if (maxDisplayedItems > 0)
        {
            Assert.AreEqual((uint)breadcrumbElements.Count, maxDisplayedItems);
        }
        else
        {
            Assert.AreEqual(breadcrumbItems.Count, breadcrumbElements.Count);
        }
    }

    [TestMethod,
      DataRow("ChevronDown", (uint)2, (uint)0),
      DataRow("ChevronDown", (uint)3, (uint)1)
    ]
    public void BitBreadcrumbShouldRespectOverflowChanges(string icon, uint maxDisplayedItems, uint overflowIndex)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.OverflowIndex, overflowIndex);
            parameters.Add(p => p.OverflowIconName, icon);
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadcrumbOverflowIcon = component.Find(".bit-brc ol.bit-brc-icn li button i");

        Assert.IsTrue(breadcrumbOverflowIcon.ClassList.Contains($"bit-icon--{icon}"));

        var breadcrumbElements = component.FindAll(".bit-brc ol.bit-brc-icn li a");

        Assert.AreEqual((uint)breadcrumbElements.Count, maxDisplayedItems);

        // The items that did not fit are the ones rendered in the overflow callout.
        var overflowElements = component.FindAll(".bit-brc-cal .bit-brc-ofi");

        Assert.AreEqual(GetBreadcrumbItems().Count - (int)maxDisplayedItems, overflowElements.Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldClampOverflowIndexOutOfTheDisplayedRange()
    {
        // An overflow index that is not inside the displayed items has no place to render the
        // overflow button, so it falls back to the first position.
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.OverflowIndex, (uint)5);
        });

        var firstItem = component.Find(".bit-brc-icn > li:first-child > *");

        Assert.IsTrue(firstItem.ClassList.Contains("bit-brc-obt"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotRenderTheOverflowButtonWhenEverythingFits()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)10);
        });

        Assert.AreEqual(0, component.FindAll(".bit-brc-obt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-brc-cal .bit-brc-ofi").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldAlternateItemAndDividerListItemsWhileOverflowing()
    {
        // The automatic collapsing reads the widths of the list items in DOM order and assumes they
        // alternate between an item wrapper and the divider that goes with it, the overflow button
        // taking the place of an item wrapper, so the order has to hold in the overflow state too.
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.OverflowIndex, (uint)1);
        });

        var listItems = component.FindAll(".bit-brc-icn > li");

        Assert.AreEqual(1, component.FindAll(".bit-brc-obt").Count);

        for (var i = 0; i < listItems.Count; i++)
        {
            var isDivider = listItems[i].GetAttribute("aria-hidden") == "true";

            Assert.AreEqual(i % 2 == 1, isDivider);
        }

        // The button sits at the overflow index, which is an item position of that alternation.
        Assert.IsNotNull(listItems[2].QuerySelector(".bit-brc-obt"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldTakeCorrectAriaCurrent()
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, breadcrumbItems);
        });

        var breadcrumbElements = component.FindAll(".bit-brc ol li a");

        var lastIndex = breadcrumbItems.Count - 1;

        Assert.AreEqual("page", breadcrumbElements[lastIndex].GetAttribute("aria-current"));

        // Only the selected item carries aria-current.
        Assert.AreEqual(1, breadcrumbElements.Count(e => e.HasAttribute("aria-current")));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderTheNavigationLandmarkOfTheBreadcrumbPattern()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        var root = component.Find(".bit-brc");

        Assert.AreEqual("NAV", root.TagName);
        Assert.AreEqual("Breadcrumb", root.GetAttribute("aria-label"));

        var list = component.Find(".bit-brc-icn");

        Assert.AreEqual("OL", list.TagName);
        Assert.AreEqual("list", list.GetAttribute("role"));
    }

    [TestMethod,
      DataRow("Site navigation")
    ]
    public void BitBreadcrumbShouldTakeCustomAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        Assert.AreEqual(ariaLabel, component.Find(".bit-brc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldHideTheDividersFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        var dividers = component.FindAll(".bit-brc-icn > li").Where(li => li.QuerySelector(".bit-brc-div") is not null).ToList();

        Assert.AreEqual(GetBreadcrumbItems().Count - 1, dividers.Count);
        Assert.IsTrue(dividers.All(d => d.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod,
      DataRow("/"),
      DataRow("›")
    ]
    public void BitBreadcrumbShouldRenderTheTextDividerInPlaceOfTheIcon(string dividerText)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerText, dividerText);
        });

        var dividers = component.FindAll(".bit-brc-dtx");

        Assert.AreEqual(GetBreadcrumbItems().Count - 1, dividers.Count);
        Assert.AreEqual(dividerText, dividers[0].TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-brc-div").Count);
    }

    [TestMethod]
    public void BitBreadcrumbDividerIconTemplateShouldWinOverTheTextDivider()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerText, "/");
            parameters.Add(p => p.DividerIconTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-divider");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(GetBreadcrumbItems().Count - 1, component.FindAll(".custom-divider").Count);
        Assert.AreEqual(0, component.FindAll(".bit-brc-dtx").Count);
    }

    [TestMethod,
      DataRow("Detailed label", (uint)3)
    ]
    public void BitBreadcrumbShouldTakeOverflowAriaLabel(string overflowAriaLabel, uint maxDisplayedItems)
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, breadcrumbItems);
            parameters.Add(p => p.OverflowAriaLabel, overflowAriaLabel);
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadcrumbButton = component.Find(".bit-brc ol li button");

        Assert.AreEqual(overflowAriaLabel, breadcrumbButton.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBreadcrumbOverflowButtonShouldExposeTheMenuItHas()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        var button = component.Find(".bit-brc-obt");

        Assert.AreEqual("More items", button.GetAttribute("aria-label"));
        Assert.AreEqual("menu", button.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.AreEqual(component.Find(".bit-brc-cal").Id, button.GetAttribute("aria-controls"));

        button.Click();

        Assert.AreEqual("true", component.Find(".bit-brc-obt").GetAttribute("aria-expanded"));

        // The overlay dismisses the menu.
        component.Find(".bit-brc-ovl").Click();

        Assert.AreEqual("false", component.Find(".bit-brc-obt").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitBreadcrumbOverflowButtonShouldCloseTheMenuOnEscape()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        component.Find(".bit-brc-obt").Click();

        Assert.AreEqual("true", component.Find(".bit-brc-obt").GetAttribute("aria-expanded"));

        component.Find(".bit-brc-cal").KeyDown(Key.Escape);

        Assert.AreEqual("false", component.Find(".bit-brc-obt").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldCloseTheMenuWhenAnOverflowItemIsClicked()
    {
        var clicked = 0;

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems(withHref: false));
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.OnItemClick, (BitBreadcrumbItem _) => { clicked++; });
        });

        component.Find(".bit-brc-obt").Click();

        component.Find(".bit-brc-cal .bit-brc-ofi").Click();

        Assert.AreEqual(1, clicked);
        Assert.AreEqual("false", component.Find(".bit-brc-obt").GetAttribute("aria-expanded"));
    }

    [TestMethod,
      DataRow("color:red;")
    ]
    public void BitBreadcrumbShouldTakeCustomStyle(string customStyle)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Style, customStyle);
        });

        var breadcrumb = component.Find(".bit-brc");

        Assert.IsTrue(breadcrumb?.GetAttribute("style")?.Contains(customStyle));
    }

    [TestMethod,
      DataRow("custom-class")
    ]
    public void BitBreadcrumbShouldTakeCustomClass(string customClass)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Class, customClass);
        });

        var breadcrumb = component.Find($".bit-brc");

        Assert.IsTrue(breadcrumb.ClassList.Contains($"{customClass}"));
    }

    [TestMethod,
      DataRow(BitVisibility.Visible),
      DataRow(BitVisibility.Hidden),
      DataRow(BitVisibility.Collapsed),
    ]
    public void BitBreadcrumbShouldTakeCustomVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Visibility, visibility);
        });

        var breadcrumb = component.Find($".bit-brc");

        switch (visibility)
        {
            case BitVisibility.Visible:
                Assert.IsFalse(breadcrumb.HasAttribute("style"));
                break;
            case BitVisibility.Hidden:
                Assert.IsTrue(breadcrumb?.GetAttribute("style")?.Contains("visibility:hidden"));
                break;
            case BitVisibility.Collapsed:
                Assert.IsTrue(breadcrumb?.GetAttribute("style")?.Contains("display:none"));
                break;
        }
    }

    [TestMethod,
      DataRow(BitColor.Primary, "bit-brc-pri"),
      DataRow(BitColor.Error, "bit-brc-err"),
      DataRow(BitColor.TertiaryBorder, "bit-brc-tbr")
    ]
    public void BitBreadcrumbShouldTakeColor(BitColor color, string cssClass)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-brc").ClassList.Contains(cssClass));
        // The callout is moved out of the root element, so it repeats the look-and-feel classes.
        Assert.IsTrue(component.Find(".bit-brc-cal").ClassList.Contains(cssClass));
    }

    [TestMethod,
      DataRow(BitSize.Small, "bit-brc-sm"),
      DataRow(BitSize.Medium, "bit-brc-md"),
      DataRow(BitSize.Large, "bit-brc-lg")
    ]
    public void BitBreadcrumbShouldTakeSize(BitSize size, string cssClass)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-brc").ClassList.Contains(cssClass));
        Assert.IsTrue(component.Find(".bit-brc-cal").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotAddAnyColorOrSizeClassByDefault()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        string[] classes = ["bit-brc-pri", "bit-brc-err", "bit-brc-sm", "bit-brc-md", "bit-brc-lg"];

        Assert.IsFalse(classes.Any(component.Find(".bit-brc").ClassList.Contains));
        Assert.IsFalse(classes.Any(component.Find(".bit-brc-cal").ClassList.Contains));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitBreadcrumbShouldTakeWrap(bool wrap)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Wrap, wrap);
        });

        Assert.AreEqual(wrap, component.Find(".bit-brc").ClassList.Contains("bit-brc-wrp"));
    }

    [TestMethod,
      DataRow("10rem")
    ]
    public void BitBreadcrumbShouldTakeMaxItemWidth(string maxItemWidth)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.MaxItemWidth, maxItemWidth);
        });

        Assert.IsTrue(component.Find(".bit-brc").GetAttribute("style")!.Contains($"--bit-brc-itm-max-width:{maxItemWidth}"));
        Assert.IsTrue(component.Find(".bit-brc-cal").GetAttribute("style")!.Contains($"--bit-brc-itm-max-width:{maxItemWidth}"));
        Assert.IsTrue(component.Find(".bit-brc-itm > span").ClassList.Contains("bit-brc-itx"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldTakeItemTitleAndTargetAndAriaLabel()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1", Title = "The first folder", Target = "_blank", AriaLabel = "Go to the first folder" },
            new() { Text = "Folder 2", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var link = component.Find(".bit-brc-itm");

        Assert.AreEqual("The first folder", link.GetAttribute("title"));
        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.AreEqual("Go to the first folder", link.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderIconsAndRespectReversedIcon()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", IconName = "Home" },
            new() { Text = "Folder 2", IconName = "Folder", ReversedIcon = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var renderedItems = component.FindAll(".bit-brc-nii");

        Assert.IsTrue(renderedItems[0].QuerySelector("i")!.ClassList.Contains("bit-icon--Home"));
        Assert.IsFalse(renderedItems[0].ClassList.Contains("bit-brc-rvi"));
        Assert.IsTrue(renderedItems[1].ClassList.Contains("bit-brc-rvi"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderAnItemWithOnlyAnItemLevelClickHandlerAsAButton()
    {
        var clicked = 0;
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", OnClick = _ => clicked++ },
            new() { Text = "Folder 2" }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var button = component.Find("button.bit-brc-itm");

        button.Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitBreadcrumbShouldInvokeOnItemClickForALinkItemToo()
    {
        BitBreadcrumbItem? clickedItem = null;

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.OnItemClick, (BitBreadcrumbItem item) => { clickedItem = item; });
        });

        component.FindAll("a.bit-brc-itm")[1].Click();

        Assert.IsNotNull(clickedItem);
        Assert.AreEqual("Folder 2", clickedItem.Text);
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotInvokeTheClickHandlersOfADisabledItem()
    {
        var clicked = 0;
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", IsEnabled = false, OnClick = _ => clicked++ },
            new() { Text = "Folder 2" }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnItemClick, (BitBreadcrumbItem _) => { clicked++; });
        });

        var button = component.Find("button.bit-brc-itm");

        Assert.IsTrue(button.ClassList.Contains("bit-brc-dis"));
        Assert.IsTrue(button.HasAttribute("disabled"));

        button.Click();

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotRenderTheHrefOfADisabledItem()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1", IsEnabled = false },
            new() { Text = "Folder 2", Href = "/folder-2" }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.AreEqual(1, component.FindAll("a.bit-brc-itm").Count);
        Assert.IsTrue(component.Find(".bit-brc-nii").ClassList.Contains("bit-brc-dis"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldDisableEverythingWhenTheComponentIsDisabled()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-brc").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.Find(".bit-brc-obt").HasAttribute("disabled"));
        // A disabled breadcrumb has no navigable links.
        Assert.AreEqual(0, component.FindAll("a.bit-brc-itm").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldClearTheRenderedItemsWhenTheItemsAreCleared()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        Assert.AreEqual(4, component.FindAll(".bit-brc-icn > li a").Count);

        component.Render(parameters => parameters.Add(p => p.Items, new List<BitBreadcrumbItem>()));

        Assert.AreEqual(0, component.FindAll(".bit-brc-icn > li").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldFollowTheOrderOfTheItemsCollection()
    {
        var items = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        CollectionAssert.AreEqual(new[] { "Folder 1", "Folder 2", "Folder 3", "Folder 4" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.Items, items.AsEnumerable().Reverse().ToList()));

        CollectionAssert.AreEqual(new[] { "Folder 4", "Folder 3", "Folder 2", "Folder 1" }, GetItemTexts(component));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRespectTheItemTemplates()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1" },
            new() { Text = "Folder 2", Template = item => (RenderFragment)(builder => builder.AddMarkupContent(0, $"<span class='item-template'>{item.Text}</span>")) },
            new() { Text = "Folder 3" },
            new() { Text = "Folder 4" }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.OverflowIndex, (uint)2);
            parameters.Add(p => p.ItemTemplate, item => (RenderFragment)(builder =>
                builder.AddMarkupContent(0, $"<span class='default-template'>{item.Text}</span>")));
            parameters.Add(p => p.OverflowTemplate, item => (RenderFragment)(builder =>
                builder.AddMarkupContent(0, $"<span class='overflow-template'>{item.Text}</span>")));
        });

        // The template of an item wins over the ItemTemplate of the component.
        Assert.AreEqual(1, component.FindAll(".bit-brc-icn .item-template").Count);
        Assert.AreEqual(2, component.FindAll(".bit-brc-icn .default-template").Count);
        Assert.AreEqual(1, component.FindAll(".bit-brc-cal .overflow-template").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotSelectAnyCustomItemWithoutNameSelectors()
    {
        // Without NameSelectors there is no way to tell which custom item is the current one, so
        // none of them may claim the aria-current of the pattern.
        var component = RenderComponent<BitBreadcrumb<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetCustomItems());
        });

        Assert.AreEqual(0, component.FindAll("[aria-current]").Count);
        Assert.AreEqual(0, component.FindAll(".bit-brc-sel").Count);
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitBreadcrumbShouldRespectReversedIconForCustomItemsWithoutNameSelectors(bool reversedIcon)
    {
        var component = RenderComponent<BitBreadcrumb<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetCustomItems());
            parameters.Add(p => p.ReversedIcon, reversedIcon);
        });

        Assert.AreEqual(reversedIcon, component.FindAll(".bit-brc-nii")[0].ClassList.Contains("bit-brc-rvi"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldReadTheCustomItemsThroughTheNameSelectors()
    {
        var component = RenderComponent<BitBreadcrumb<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetCustomItems());
            parameters.Add(p => p.NameSelectors, new BitBreadcrumbNameSelectors<CustomItem>()
            {
                Text = { Selector = i => i.Name },
                Href = { Selector = i => i.Address },
                Title = { Selector = i => i.Tooltip },
                Target = { Selector = i => i.Window },
                IsSelected = { Selector = i => i.IsCurrent },
                IsEnabled = { Selector = i => i.Active },
            });
        });

        var links = component.FindAll("a.bit-brc-itm");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("Custom 1", links[0].TextContent.Trim());
        Assert.AreEqual("/custom-1", links[0].GetAttribute("href"));
        Assert.AreEqual("The first one", links[0].GetAttribute("title"));
        Assert.AreEqual("_blank", links[0].GetAttribute("target"));
        Assert.AreEqual("page", component.Find(".bit-brc-sel").GetAttribute("aria-current"));
        // The third item is disabled through the IsEnabled selector, so it is not a link.
        Assert.AreEqual(1, component.FindAll(".bit-brc-nii.bit-brc-dis").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldReadTheCustomItemsThroughTheNameSelectorPropertyNames()
    {
        var component = RenderComponent<BitBreadcrumb<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetCustomItems());
            parameters.Add(p => p.NameSelectors, new BitBreadcrumbNameSelectors<CustomItem>()
            {
                Text = { Name = nameof(CustomItem.Name) },
                Href = { Name = nameof(CustomItem.Address) },
                IsSelected = { Name = nameof(CustomItem.IsCurrent) },
            });
        });

        var links = component.FindAll("a.bit-brc-itm");

        Assert.AreEqual(3, links.Count);
        Assert.AreEqual("Custom 1", links[0].TextContent.Trim());
        Assert.AreEqual("page", component.Find(".bit-brc-sel").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldInvokeTheNameSelectorsOnClickOfACustomItem()
    {
        CustomItem? clicked = null;

        var component = RenderComponent<BitBreadcrumb<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetCustomItems());
            parameters.Add(p => p.NameSelectors, new BitBreadcrumbNameSelectors<CustomItem>()
            {
                Text = { Selector = i => i.Name },
                OnClick = item => clicked = item,
            });
        });

        component.FindAll("button.bit-brc-itm")[0].Click();

        Assert.IsNotNull(clicked);
        Assert.AreEqual("Custom 1", clicked.Name);
    }

    [TestMethod]
    public void BitBreadcrumbShouldTakeCustomClassesAndStyles()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.OverflowIndex, (uint)2);
            parameters.Add(p => p.Classes, new BitBreadcrumbClassStyles
            {
                Root = "custom-root",
                ItemContainer = "custom-container",
                ItemWrapper = "custom-wrapper",
                Item = "custom-item",
                SelectedItem = "custom-selected",
                Divider = "custom-divider",
                OverflowButton = "custom-overflow-button",
                OverflowItem = "custom-overflow-item",
                Callout = "custom-callout",
            });
            parameters.Add(p => p.Styles, new BitBreadcrumbClassStyles
            {
                Item = "color:red",
                SelectedItem = "font-style:italic",
            });
        });

        Assert.IsTrue(component.Find(".bit-brc").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-brc-icn").ClassList.Contains("custom-container"));
        Assert.IsTrue(component.Find(".bit-brc-icn > li").ClassList.Contains("custom-wrapper"));
        Assert.IsTrue(component.Find(".bit-brc-itm").ClassList.Contains("custom-item"));
        Assert.IsTrue(component.Find(".bit-brc-obt").ClassList.Contains("custom-overflow-button"));
        Assert.IsTrue(component.Find(".bit-brc-cal").ClassList.Contains("custom-callout"));
        Assert.IsTrue(component.Find(".bit-brc-ofi").ClassList.Contains("custom-overflow-item"));

        var selected = component.Find(".bit-brc-sel");

        Assert.IsTrue(selected.ClassList.Contains("custom-selected"));
        Assert.IsTrue(selected.GetAttribute("style")!.Contains("color:red"));
        Assert.IsTrue(selected.GetAttribute("style")!.Contains("font-style:italic"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldTakeTheDirOfTheComponentOnTheCalloutToo()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-brc").GetAttribute("dir"));
        Assert.AreEqual("rtl", component.Find(".bit-brc-cal").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotOverrideAnAriaLabelOfTheHtmlAttributes()
    {
        var component = RenderComponent<BitBreadcrumbHtmlAttributesTest>(
            parameters => parameters.Add(p => p.Items, GetBreadcrumbItems()));

        Assert.AreEqual("Where you are", component.Find(".bit-brc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderTheExternalIconsOfTheItems()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Home", Icon = BitIconInfo.Css("fa-solid fa-house") },
            new() { Text = "Laptops", Icon = "fa-solid fa-laptop", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var icons = component.FindAll(".bit-brc-nii i");

        Assert.AreEqual(2, icons.Count);
        Assert.IsTrue(icons[0].ClassList.Contains("fa-house"));
        Assert.IsTrue(icons[1].ClassList.Contains("fa-laptop"));
        // The icons carry no information of their own next to the text of their item.
        Assert.IsTrue(icons.All(i => i.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldTakeTheDividerIconOfTheIconInfo()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerIcon, BitIconInfo.Css("fa-solid fa-angle-right"));
            parameters.Add(p => p.OverflowIcon, BitIconInfo.Css("fa-solid fa-ellipsis"));
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        Assert.IsTrue(component.Find(".bit-brc-div").ClassList.Contains("fa-angle-right"));
        Assert.IsTrue(component.Find(".bit-brc-obt i").ClassList.Contains("fa-ellipsis"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderTheOverflowIconTemplate()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
            parameters.Add(p => p.OverflowIconTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-overflow-icon");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(1, component.FindAll(".bit-brc-obt .custom-overflow-icon").Count);
        Assert.AreEqual(0, component.FindAll(".bit-brc-obt i").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderEveryDividerIconWithTheClassThatMirrorsItInRtl()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        var divider = component.Find(".bit-brc-icn i");

        // The mirroring hangs on the class of the divider, so a custom icon is turned around in a RTL
        // layout the same way the default chevron is.
        Assert.IsTrue(divider.ClassList.Contains("bit-brc-div"));
        Assert.IsTrue(divider.ClassList.Contains("bit-icon--ChevronRight"));

        component.Render(parameters => parameters.Add(p => p.DividerIconName, "CaretRightSolid8"));

        divider = component.Find(".bit-brc-icn i");

        Assert.IsTrue(divider.ClassList.Contains("bit-brc-div"));
        Assert.IsTrue(divider.ClassList.Contains("bit-icon--CaretRightSolid8"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotRenderAnOptionsContainerWithoutOptions()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        Assert.AreEqual(0, component.FindAll("div[hidden]").Count);
    }

    [TestMethod]
    public void BitBreadcrumbOverflowCalloutShouldCarryTheRolesOfAMenu()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1" },
            new() { Text = "Folder 2" },
            new() { Text = "Folder 3", Href = "/folder-3", IsEnabled = false },
            new() { Text = "Folder 4", Href = "/folder-4", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MaxDisplayedItems, (uint)1);
        });

        var menu = component.Find(".bit-brc-scn");

        Assert.AreEqual("UL", menu.TagName);
        Assert.AreEqual("menu", menu.GetAttribute("role"));
        // The menu is labelled by the button that owns it rather than by a label of its own.
        Assert.AreEqual(component.Find(".bit-brc-obt").Id, menu.GetAttribute("aria-labelledby"));
        Assert.IsFalse(menu.HasAttribute("aria-label"));

        var wrappers = component.FindAll(".bit-brc-scn > li");

        Assert.AreEqual(3, wrappers.Count);
        Assert.IsTrue(wrappers.All(w => w.GetAttribute("role") == "presentation"));

        // Every step of the collapsed trail is a menu item, whether it is actionable or not.
        var menuItems = component.FindAll(".bit-brc-scn .bit-brc-ofi, .bit-brc-scn .bit-brc-ofn");

        Assert.AreEqual(3, menuItems.Count);
        Assert.IsTrue(menuItems.All(i => i.GetAttribute("role") == "menuitem"));
        Assert.AreEqual("-1", menuItems.First(i => i.ClassList.Contains("bit-brc-ofn")).GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldMarkTheDisabledItemsWithAriaDisabled()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1", IsEnabled = false },
            new() { Text = "Folder 2", Href = "/folder-2" },
            new() { Text = "Folder 3", Href = "/folder-3", IsEnabled = false },
            new() { Text = "Folder 4", Href = "/folder-4", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.OverflowIndex, (uint)1);
        });

        Assert.AreEqual("true", component.Find(".bit-brc-icn .bit-brc-nii").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", component.Find(".bit-brc-scn .bit-brc-ofn").GetAttribute("aria-disabled"));
        // The items that are enabled say nothing about it.
        Assert.AreEqual(0, component.FindAll(".bit-brc-itm[aria-disabled], .bit-brc-ofi[aria-disabled]").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldTurnDownTheOpenerOfALinkOpenedInAnotherContext()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1", Target = "_blank" },
            new() { Text = "Folder 2", Href = "/folder-2", Target = "_self" },
            new() { Text = "Folder 3", Href = "/folder-3", Target = "_blank" },
            new() { Text = "Folder 4", Href = "/folder-4", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.OverflowIndex, (uint)1);
        });

        var links = component.FindAll(".bit-brc-icn a.bit-brc-itm");

        Assert.AreEqual("noopener noreferrer", links[0].GetAttribute("rel"));
        Assert.IsFalse(links[1].HasAttribute("rel"));
        // Folder 2 and Folder 3 are the collapsed ones, only the second of them opens in a new context.
        var overflowLinks = component.FindAll(".bit-brc-scn a.bit-brc-ofi");

        Assert.IsFalse(overflowLinks[0].HasAttribute("rel"));
        Assert.AreEqual("noopener noreferrer", overflowLinks[1].GetAttribute("rel"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldCloseTheMenuWhenTheOverflowItemsAreGone()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        component.Find(".bit-brc-obt").Click();

        Assert.IsTrue(component.Find(".bit-brc-ovl").GetAttribute("style")!.Contains("display:block"));

        // Everything fits again: the button the menu belongs to is gone, so the menu and the overlay that
        // catches the clicks outside of it may not stay behind.
        component.Render(parameters => parameters.Add(p => p.MaxDisplayedItems, (uint)0));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-brc-ovl").GetAttribute("style")!.Contains("display:none")));
    }

    [TestMethod]
    public void BitBreadcrumbShouldCloseTheMenuWhenTheComponentGetsDisabled()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        component.Find(".bit-brc-obt").Click();

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

        component.WaitForAssertion(() =>
            Assert.AreEqual("false", component.Find(".bit-brc-obt").GetAttribute("aria-expanded")));

        Assert.IsTrue(component.Find(".bit-brc-ovl").GetAttribute("style")!.Contains("display:none"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldMoveTheFocusInsideTheOverflowMenuWithTheKeyboard()
    {
        var handler = Context.JSInterop.SetupVoid("BitBlazorUI.Utils.focusItem", _ => true);

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)3);
        });

        component.Find(".bit-brc-obt").KeyDown(Key.Down);

        var callout = component.Find(".bit-brc-cal");

        callout.KeyDown(Key.Down);
        callout.KeyDown(Key.Up);
        callout.KeyDown(Key.Home);
        callout.KeyDown(Key.End);
        // A printable character is the typeahead of the menu pattern.
        callout.KeyDown("f");
        // A space activates the focused item instead of jumping anywhere.
        callout.KeyDown(Key.Space);

        var modes = handler.Invocations.Select(i => (string)i.Arguments[2]!).ToArray();

        CollectionAssert.AreEqual(new[] { "first", "next", "prev", "first", "last", "char" }, modes);
        Assert.AreEqual("f", handler.Invocations.Last().Arguments[3]);
        // The items that are not actionable are part of the rotation too.
        Assert.AreEqual(".bit-brc-ofi, .bit-brc-ofn", handler.Invocations.Last().Arguments[1]);
    }

    [TestMethod]
    public void BitBreadcrumbShouldRenderTheStructuredDataOfTheWholeHierarchy()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            // The collapsed items are part of the hierarchy the search engines are told about.
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.StructuredData, true);
        });

        var script = component.Find("script[type='application/ld+json']");

        using var json = JsonDocument.Parse(script.TextContent);

        var root = json.RootElement;

        Assert.AreEqual("https://schema.org", root.GetProperty("@context").GetString());
        Assert.AreEqual("BreadcrumbList", root.GetProperty("@type").GetString());

        var elements = root.GetProperty("itemListElement").EnumerateArray().ToList();

        Assert.AreEqual(4, elements.Count);
        Assert.AreEqual("ListItem", elements[0].GetProperty("@type").GetString());
        Assert.AreEqual(1, elements[0].GetProperty("position").GetInt32());
        Assert.AreEqual("Folder 1", elements[0].GetProperty("name").GetString());
        Assert.AreEqual(4, elements[3].GetProperty("position").GetInt32());
        Assert.AreEqual("Folder 4", elements[3].GetProperty("name").GetString());
        // The relative addresses of the items become absolute ones.
        Assert.IsTrue(elements[0].GetProperty("item").GetString()!.StartsWith("http"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotRenderAnyStructuredDataByDefault()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        Assert.AreEqual(0, component.FindAll("script[type='application/ld+json']").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldEscapeTheStructuredDataOfTheItems()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Tom & \"Jerry\" </script><script>alert(1)</script>", Href = "/a?x=1&y=2" },
            new() { Text = "Folder 2", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.StructuredData, true);
        });

        var script = component.Find("script[type='application/ld+json']");

        // Nothing in the values may close the script element or be read back as markup.
        Assert.IsFalse(script.InnerHtml.Contains('<'));
        Assert.IsFalse(script.InnerHtml.Contains('&'));

        using var json = JsonDocument.Parse(script.TextContent);

        var elements = json.RootElement.GetProperty("itemListElement").EnumerateArray().ToList();

        Assert.AreEqual(items[0].Text, elements[0].GetProperty("name").GetString());
        Assert.IsTrue(elements[0].GetProperty("item").GetString()!.EndsWith("/a?x=1&y=2"));
    }

    [TestMethod]
    public void BitBreadcrumbShouldLeaveTheAddresslessItemsOutOfTheStructuredData()
    {
        var items = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1" },
            new() { Text = null, Href = "/nameless" },
            new() { Text = "Folder 3", IsSelected = true }
        };

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.StructuredData, true);
        });

        using var json = JsonDocument.Parse(component.Find("script[type='application/ld+json']").TextContent);

        var elements = json.RootElement.GetProperty("itemListElement").EnumerateArray().ToList();

        // An item without a name is nothing to write, and the current page carries no address of its own.
        Assert.AreEqual(2, elements.Count);
        Assert.AreEqual("Folder 3", elements[1].GetProperty("name").GetString());
        Assert.AreEqual(2, elements[1].GetProperty("position").GetInt32());
        Assert.IsFalse(elements[1].TryGetProperty("item", out _));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitBreadcrumb<BitBreadcrumbItem>> component)
    {
        return component.FindAll(".bit-brc-icn .bit-brc-itm, .bit-brc-icn .bit-brc-nii")
                        .Select(e => e.TextContent.Trim()).ToArray();
    }

    private static List<BitBreadcrumbItem> GetBreadcrumbItems(bool withHref = true)
    {
        return
        [
            new() { Text = "Folder 1", Href = withHref ? "/components/breadcrumb" : null },
            new() { Text = "Folder 2", Href = withHref ? "/components/breadcrumb" : null },
            new() { Text = "Folder 3", Href = withHref ? "/components/breadcrumb" : null },
            new() { Text = "Folder 4", Href = withHref ? "/components/breadcrumb" : null, IsSelected = true }
        ];
    }

    private static List<CustomItem> GetCustomItems()
    {
        return
        [
            new() { Name = "Custom 1", Address = "/custom-1", Tooltip = "The first one", Window = "_blank" },
            new() { Name = "Custom 2", Address = "/custom-2", IsCurrent = true },
            new() { Name = "Custom 3", Address = "/custom-3", Active = false }
        ];
    }

    private class CustomItem
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Tooltip { get; set; }
        public string? Window { get; set; }
        public bool IsCurrent { get; set; }
        public bool Active { get; set; } = true;
    }
}
