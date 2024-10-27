using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbTests : BunitTestContext
{
    [DataTestMethod,
      DataRow("Separator")
    ]
    public void BitBreadcrumbShouldTakeDividerIcon(string icon)
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerIconName, icon);
        });

        var breadcrumbDividerIcon = component.Find(".bit-brc ul i");

        Assert.IsTrue(breadcrumbDividerIcon.ClassList.Contains($"bit-icon--{icon}"));
    }

    [DataTestMethod,
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

        var breadcrumbElements = component.FindAll(".bit-brc ul.bit-brc-icn li a");

        if (maxDisplayedItems > 0)
        {
            Assert.AreEqual((uint)breadcrumbElements.Count, maxDisplayedItems);
        }
        else
        {
            Assert.AreEqual(breadcrumbItems.Count, breadcrumbElements.Count);
        }
    }

    [DataTestMethod,
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

        var breadcrumbOverflowIcon = component.Find(".bit-brc ul.bit-brc-icn li button i");

        Assert.IsTrue(breadcrumbOverflowIcon.ClassList.Contains($"bit-icon--{icon}"));

        var breadcrumbElements = component.FindAll(".bit-brc ul.bit-brc-icn li a");
        var overflowItem = breadcrumbElements[(int)overflowIndex];

        Assert.AreEqual((uint)breadcrumbElements.Count, maxDisplayedItems);
    }

    [DataTestMethod]
    public void BitBreadcrumbShouldTakeCorrectAriaCurrent()
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, breadcrumbItems);
        });

        var breadcrumbElements = component.FindAll(".bit-brc ul li a");

        var lastIndex = breadcrumbItems.Count - 1;

        Assert.IsTrue(breadcrumbElements[lastIndex].GetAttribute("aria-current").Contains("page"));
    }

    [DataTestMethod,
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

        var breadcrumbButton = component.Find(".bit-brc ul li button");

        Assert.IsTrue(breadcrumbButton.GetAttribute("aria-label").Contains(overflowAriaLabel));
    }

    [DataTestMethod,
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

        Assert.IsTrue(breadcrumb.GetAttribute("style").Contains(customStyle));
    }

    [DataTestMethod,
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

    [DataTestMethod,
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
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("visibility:hidden"));
                break;
            case BitVisibility.Collapsed:
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("display:none"));
                break;
        }
    }

    private static List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 2 ",
                Href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb",
                IsSelected = true,
            }
        };
    }
}
