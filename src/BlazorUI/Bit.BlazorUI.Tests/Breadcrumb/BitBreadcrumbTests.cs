using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Breadcrumb;

[TestClass]
public class BitBreadcrumbTests : BunitTestContext
{
    [DataTestMethod,
      DataRow(Visual.Fluent),
      DataRow(Visual.Cupertino),
      DataRow(Visual.Material),
    ]
    public void BitBreadcrumbShouldTakeCorrectVisualStyle(Visual visual)
    {
        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.AddCascadingValue(visual);
        });

        var bitBreadcrumb = component.Find(".bit-brc");

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitBreadcrumb.ClassList.Contains($"bit-brc-{visualClass}"));
    }

    [DataTestMethod,
      DataRow(BitIconName.Separator)
    ]
    public void BitBreadcrumbShouldTakeDividerIcon(BitIconName icon)
    {
        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.DividerIcon, icon);
        });

        var breadcrumbDividerIcon = component.Find(".bit-brc ul li i");

        Assert.IsTrue(breadcrumbDividerIcon.ClassList.Contains($"bit-icon--{icon}"));
    }

    [DataTestMethod,
      DataRow(0),
      DataRow(3)
   ]
    public void BitBreadcrumbShouldRespectMaxDisplayeItems(int maxDisplayedItems)
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, breadcrumbItems);
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadcrumbElements = component.FindAll(".bit-brc .bit-brc-items-wrapper ul li");

        if (maxDisplayedItems > 0)
        {
            Assert.AreEqual(breadcrumbElements.Count, maxDisplayedItems + 1);
        }
        else
        {
            Assert.AreEqual(breadcrumbItems.Count, breadcrumbElements.Count);
        }
    }

    [DataTestMethod,
      DataRow(BitIconName.ChevronDown, 2, 0),
      DataRow(BitIconName.ChevronDown, 3, 1)
    ]
    public void BitBreadcrumbShouldRespectOverflowChanges(BitIconName icon, int maxDisplayedItems, int overflowIndex)
    {
        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.OverflowIndex, overflowIndex);
            parameters.Add(p => p.OnRenderOverflowIcon, icon);
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadcrumbOverflowIcon = component.Find(".bit-brc ul li button span i");

        Assert.IsTrue(breadcrumbOverflowIcon.ClassList.Contains($"bit-icon--{icon}"));

        var breadcrumbElements = component.FindAll(".bit-brc .bit-brc-items-wrapper ul li");
        var overflowItem = breadcrumbElements[overflowIndex];

        Assert.AreEqual(breadcrumbElements.Count, maxDisplayedItems + 1);
        Assert.IsTrue(overflowItem.InnerHtml.Contains("button"));
    }

    [DataTestMethod]
    public void BitBreadcrumbShouldTakeCorrectAriaCurrent()
    {
        //var breadcrumbItems = GetBreadcrumbItems();

        //var component = RenderComponent<BitBreadcrumb>(parameters =>
        //{
        //    parameters.Add(p => p.Items, breadcrumbItems);
        //});

        //var breadcrumbElements = component.FindAll(".bit-brc ul li a");

        //var activeItemIndex = breadcrumbItems.FindLastIndex(item => item.IsCurrentItem);

        //Assert.IsTrue(breadcrumbElements[activeItemIndex].GetAttribute("aria-current").Contains("page"));

        //for (int index = 0; index < breadcrumbElements.Count; index++)
        //{
        //    if (index != activeItemIndex)
        //    {
        //        Assert.IsTrue(breadcrumbElements[index].GetAttribute("aria-current").Contains("undefined"));
        //    }
        //}
    }

    [DataTestMethod,
      DataRow("Detailed label", 3)
    ]
    public void BitBreadcrumbShouldTakeOverflowAriaLabel(string overflowAriaLabel, int maxDisplayedItems)
    {
        var breadcrumbItems = GetBreadcrumbItems();

        var component = RenderComponent<BitBreadcrumb>(parameters =>
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
        var component = RenderComponent<BitBreadcrumb>(parameters =>
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
        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Class, customClass);
        });

        var breadcrumb = component.Find($".bit-brc");

        Assert.IsTrue(breadcrumb.ClassList.Contains($"{customClass}"));
    }

    [DataTestMethod,
      DataRow(BitComponentVisibility.Visible),
      DataRow(BitComponentVisibility.Hidden),
      DataRow(BitComponentVisibility.Collapsed),
    ]
    public void BitBreadcrumbShouldTakeCustomVisibility(BitComponentVisibility visibility)
    {
        var component = RenderComponent<BitBreadcrumb>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.Visibility, visibility);
        });

        var breadcrumb = component.Find($".bit-brc");

        switch (visibility)
        {
            case BitComponentVisibility.Visible:
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains(""));
                break;
            case BitComponentVisibility.Hidden:
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("visibility:hidden"));
                break;
            case BitComponentVisibility.Collapsed:
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("display:none"));
                break;
        }
    }

    private List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 2 ",
                href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 3",
                href = "/components/breadcrumb",
            },
            new()
            {
                Text = "Folder 4",
                href = "/components/breadcrumb",
            }
        };
    }
}
