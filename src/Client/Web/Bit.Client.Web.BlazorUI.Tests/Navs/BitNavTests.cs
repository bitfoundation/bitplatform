using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Navs
{
    [TestClass]
    public class BitNavTests : BunitTestContext
    {
        private string BitNavSelectedKey;

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

        [DataTestMethod]
        public void BitNavSelectedKeyTwoWayBindTest()
        {
            var navLinkItems = new List<BitNavLinkItem> { new BitNavLinkItem { Name = "test", Key = "key", IsEnabled = true, Url = "example.com" } };
            var component = RenderComponent<BitNav>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.Mode, BitNavMode.Manual);
                parameters.Add(p => p.SelectedKeyChanged, HandleSelectedKeyChange);
            });

            var navItem = component.Find(".bit-nav-composite-link > a");
            //navItem.Click();
            //Assert.AreEqual("key", BitNavSelectedKey);
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
            var navLinkItems = new List<BitNavLinkItem> { new BitNavLinkItem { Name = "test", Key = "key", IsEnabled = isEnabled, Url = url } };

            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.Visual, visual);
            });

            var enabledClass = isEnabled ? "enabled" : "disabled";
            var hasUrlClass = hasUrl ? "hasurl" : "nourl";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            if (hasUrl)
            {
                var element = component.Find("a");
                Assert.IsTrue(element.ClassList.Contains($"bit-nav-link-{enabledClass}-{hasUrlClass}-{visualClass}"));
            }
            else
            {
                var element = component.Find("button");
                Assert.IsTrue(element.ClassList.Contains($"bit-nav-link-{enabledClass}-{hasUrlClass}-{visualClass}"));
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
            List<BitNavLinkItem> navLinkItems = new()
            {
                new BitNavLinkItem()
                {
                    Name = "Home",
                    Key = "key1",
                    CollapseAriaLabel = collapseAriaLabel,
                    ExpandAriaLabel = expandAriaLabel,
                    IsExpanded = isExpanded,
                    Links = new List<BitNavLinkItem>()
                    {
                        new BitNavLinkItem() { Name = "Activity", Url = "http://msn.com", Key = "key1-1", Title = "Activity" }
                    }
                }
            };

            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
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
            List<BitNavLinkItem> navLinkItems = new() { new() { Name = "test", Key = "key", Url = "https://www.google.com/" } };
            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.RenderType, type);
            });

            if (type == BitNavRenderType.Grouped)
            {
                Assert.IsNotNull(component.Find(".bit-nav-grp-chevron-btn"));
            }
            else
            {
                Assert.ThrowsException<ElementNotFoundException>(() => component.Find(".bit-nav-grp-chevron-btn"));
            }
        }

        [DataTestMethod,
         DataRow(Visual.Fluent, "key1"),
         DataRow(Visual.Cupertino, "key2"),
         DataRow(Visual.Material, "key3"),
        ]
        public void BitNavInitialSelectedKeyTest(Visual visual, string initialSelectedKey)
        {
            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, BasicNavLinks);
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.InitialSelectedKey, initialSelectedKey);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var selectedItemTxt = component.Find($".bit-nav-selected-{visualClass} > .bit-nav-link-container > .bit-nav-link-txt");
            var expectedResult = BasicNavLinks.Find(i => i.Key == initialSelectedKey).Name;
            Assert.AreEqual(expectedResult, selectedItemTxt.TextContent);
        }

        [DataTestMethod]
        public void BitNavOnLinkExpandClickTest()
        {
            var items = new List<BitNavLinkItem>()
            {
                new() { Name = "Test1", Key = "key1", Links = new List<BitNavLinkItem>() { new() { Name = "Test2", Key = "key2" } } }
            };

            var componenet = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, items);
            });

            var button = componenet.Find(".bit-nav-chevron-btn");

            //TODO: bypassed - BUnit or Blazor issue
            //button.Click();
            //Assert.AreEqual(items[0].Key, componenet.Instance.OnLinkExpandClickValue);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitNavLinkItemIsCollapsedByDefaultTest(bool isCollapseByDefault)
        {
            var items = new List<BitNavLinkItem>()
            {
                new() {
                    Name = "Test1",
                    Key = "key1",
                    IsExpanded = true,
                    IsCollapseByDefault = isCollapseByDefault,
                    Links = new List<BitNavLinkItem>()
                        { new() { Name = "Test2", Key = "key2" } } }
            };

            var componenet = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, items);
                parameters.Add(p => p.RenderType, BitNavRenderType.Grouped);
            });

            var groupHeaderBtn = componenet.Find(".bit-nav-grp-chevron-btn");
            var groupHeaderBtnIcon = componenet.Find(".bit-nav-grp-chevron-btn > i");

            if (isCollapseByDefault)
            {
                Assert.AreEqual("false", groupHeaderBtn.GetAttribute("aria-expanded"));
                Assert.IsFalse(groupHeaderBtnIcon.ClassList.Contains("bit-nav-expand-fluent"));
            }
            else
            {
                Assert.AreEqual("true", groupHeaderBtn.GetAttribute("aria-expanded"));
                Assert.IsTrue(groupHeaderBtnIcon.ClassList.Contains("bit-nav-expand-fluent"));
            }
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitNavLinkItemForceAnchorTest(bool isForced)
        {
            var items = new List<BitNavLinkItem>()
            {
                new() {
                    Name = "Test1",
                    Key = "key1",
                    IsExpanded = true,
                    Links = new List<BitNavLinkItem>()
                        { new() { Name = "Test2", Key = "key2", ForceAnchor = isForced } } }
            };

            var componenet = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, items);
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
            var items = new List<BitNavLinkItem>()
            {
                new() { Name = name, Title = title, Key = "key1", IsExpanded = true }
            };

            var componenet = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, items);
            });

            var navLinkItem = componenet.Find(".bit-nav-link-enabled-nourl-fluent");

            if (title is null)
            {
                Assert.AreEqual(name, navLinkItem.GetAttribute("title"));
            }
            else
            {
                Assert.AreEqual(title, navLinkItem.GetAttribute("title"));
            }
        }

        private void HandleSelectedKeyChange(string key)
        {
            BitNavSelectedKey = key;
        }

        private readonly List<BitNavLinkItem> BasicNavLinks = new()
        {
            new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target = "_blank" },
            new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" },
            new BitNavLinkItem { Name = "Documents", Url = "http://example.com", Key = "key3", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", IconName = BitIconName.News, Target = "_self" },
        };
    }
}
