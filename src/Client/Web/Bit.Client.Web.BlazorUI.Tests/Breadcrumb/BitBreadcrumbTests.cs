using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Breadcrumb
{
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
                parameters.Add(p => p.Visual, visual);
            });
            var bitBreadcrumb = component.Find(".bit-brc");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitBreadcrumb.ClassList.Contains($"bit-brc-{visualClass}"));
        }

        [DataTestMethod,
            DataRow(BitIconName.Separator),
            DataRow(null)
        ]
        public void BitBreadcrumbShouldTakeDividerIcon(BitIconName icon)
        {
            var component = RenderComponent<BitBreadcrumb>(parameters =>
            {
                parameters.Add(p => p.Items, GetBreadcrumbItems());
                if (icon != BitIconName.NotSet)
                {
                    parameters.Add(p => p.DividerAs, icon);
                }
            });

            var breadcrumbDividerIcon = component.Find(".bit-brc ul li i");

            if (icon != BitIconName.NotSet)
                Assert.IsTrue(breadcrumbDividerIcon.ClassList.Contains($"bit-icon--{icon}"));
            else
                Assert.IsTrue(breadcrumbDividerIcon.ClassList.Contains($"bit-icon--{BitIconName.ChevronRight}"));
        }

        [DataTestMethod,
         DataRow(null),
         DataRow(3)
       ]
        public void BitBreadcrumbShouldRespectMaxDisplayeItems(int maxDisplayedItems)
        {
            var breadcrumbItems = GetBreadcrumbItems();

            var component = RenderComponent<BitBreadcrumb>(parameters =>
            {
                parameters.Add(p => p.Items, breadcrumbItems);
                if (maxDisplayedItems > 0)
                {
                    parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
                }
            });

            var breadcrumbElements = component.FindAll(".bit-brc ul li");

            if (maxDisplayedItems > 0)
                Assert.AreEqual(breadcrumbElements.Count, maxDisplayedItems + 1);
            else
                Assert.AreEqual(breadcrumbItems.Count, breadcrumbElements.Count);
        }

        [DataTestMethod,
          DataRow(BitIconName.ChevronDown, 2, null),
          DataRow(BitIconName.ChevronDown, 3, 1),
          DataRow(null, 1, null),
          DataRow(null, 2, 1)
        ]
        public void BitBreadcrumbShouldRespectOverflowChanges(BitIconName icon, int maxDisplayedItems, int overflowIndex)
        {
            var component = RenderComponent<BitBreadcrumb>(parameters =>
            {
                parameters.Add(p => p.Items, GetBreadcrumbItems());
                parameters.Add(p => p.OverflowIndex, overflowIndex);

                if (icon != BitIconName.NotSet)
                {
                    parameters.Add(p => p.OnRenderOverflowIcon, icon);
                }
                if (maxDisplayedItems > 0)
                {
                    parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
                }
            });

            var breadcrumbOverflowIcon = component.Find(".bit-brc ul li button span i");

            if (icon != BitIconName.NotSet)
                Assert.IsTrue(breadcrumbOverflowIcon.ClassList.Contains($"bit-icon--{icon}"));
            else
                Assert.IsTrue(breadcrumbOverflowIcon.ClassList.Contains($"bit-icon--{BitIconName.More}"));

            var breadcrumbElements = component.FindAll(".bit-brc ul li");
            var overflowItem = breadcrumbElements[overflowIndex];

            Assert.AreEqual(breadcrumbElements.Count, maxDisplayedItems + 1);
            Assert.IsTrue(overflowItem.InnerHtml.Contains("button"));
        }

        [DataTestMethod,
          DataRow(3),
          DataRow(2),
        ]
        public void BitBreadcrumbShouldTakeCorrectAriaCurrent(int itemIndex)
        {
            var breadcrumbItems = GetBreadcrumbItems();

            var component = RenderComponent<BitBreadcrumb>(parameters =>
            {
                parameters.Add(p => p.Items, breadcrumbItems);
            });

            var breadcrumbElements = component.FindAll(".bit-brc ul li a");

            var activeItemIndex = breadcrumbItems.FindIndex(item => item.IsCurrentItem);

            if (activeItemIndex == itemIndex)
                Assert.IsTrue(breadcrumbElements[itemIndex].GetAttribute("aria-current").Contains("page"));
            else
                Assert.IsTrue(breadcrumbElements[itemIndex].GetAttribute("aria-current").Contains("undefined"));
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

            Assert.AreEqual(breadcrumb.GetAttribute("style"), customStyle);
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

            if (visibility == BitComponentVisibility.Visible)
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains(""));

            else if (visibility == BitComponentVisibility.Hidden)
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("visibility:hidden"));

            else if (visibility == BitComponentVisibility.Collapsed)
                Assert.IsTrue(breadcrumb.GetAttribute("style").Contains("display:none"));
        }

        private List<BitBreadcrumbItem> GetBreadcrumbItems()
        {
            return new List<BitBreadcrumbItem>()
            {
                new()
                {
                    Text = "Folder 1",
                    Key = "f1",
                    href = "/components/breadcrumb",
                },
                new()
                {
                    Text = "Folder 2 ",
                    Key = "f2",
                    href = "/components/breadcrumb",
                },
                new()
                {
                    Text = "Folder 3",
                    Key = "f3",
                    href = "/components/breadcrumb",
                },
                new()
                {
                    Text = "Folder 4",
                    Key = "f3",
                    href = "/components/breadcrumb",
                    IsCurrentItem = true,
                }
            };
        }
    }
}
