using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Navs
{
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
          DataRow(true),
          DataRow(false),
        ]
        public void BitNavIsOnTopTest(bool isOnTop)
        {
            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.IsOnTop, isOnTop);
            });

            var bitNav = component.Find(".bit-nav");
            var isOnTopClass = isOnTop ? "top" : "no-top";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{isOnTopClass}"));
        }

        [DataTestMethod,
         DataRow(Visual.Fluent, "key1"),
         DataRow(Visual.Cupertino, "key2"),
         DataRow(Visual.Material, "key3"),
        ]
        public void BitNavSelectedKeyTest(Visual visual, string selectedKey)
        {
            var component = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, BasicNavLinks);
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.SelectedKey, selectedKey);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var selectedItemTxt = component.Find($".bit-nav-selected-{visualClass} > .bit-nav-link-container > .bit-nav-link-txt");
            var expectedResult = BasicNavLinks.Find(i => i.Key == selectedKey).Name;
            Assert.AreEqual(expectedResult, selectedItemTxt.TextContent);
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
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitNav = com.Find(".bit-nav");

            Assert.IsTrue(bitNav.GetAttribute("aria-label").Equals(ariaLabel));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitNavLinkItemsAriaLabelTest(string ariaLabel)
        {
            List<BitNavLinkItem> navLinkItems = new()
            {
                new()
                {
                    Name = "Home",
                    Key = "key1",
                    CollapseAriaLabel = ariaLabel,
                    Links = new List<BitNavLinkItem>()
                    {
                        new()
                        {
                            Name = "Activity",
                            Url = "http://msn.com",
                            Key = "key1-1",
                            Title = "Activity"
                        }
                    }
                },
                new()
                {
                    Name = "Documents",
                    Title = "Documents",
                    Url = "http://example.com",
                    Key = "key2",
                    CollapseAriaLabel = ariaLabel,
                    Links = new List<BitNavLinkItem>()
                    {
                        new()
                        {
                            Name = "Activity",
                            Url = "http://example.com",
                            Key = "key2-1",
                            Title = "Activity"
                        }
                    }
                }
            };

            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitNavLinksItems = com.FindAll(".bit-nav button");

            foreach (var bitNavLinksItem in bitNavLinksItems)
            {
                var hasAttribute = bitNavLinksItem.HasAttribute("aria-label");
                //TODO: This part of the code has been commented on due to a GitHub pipeline problem.
                //Assert.IsTrue(hasAttribute);
                //Assert.IsTrue(hasAttribute ? bitNavLinksItem.GetAttribute("aria-label").Equals(ariaLabel) : true);
            }
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
            DataRow(true, "CollapseAriaLabel", "ExpandAriaLabel"),
            DataRow(false, "CollapseAriaLabel", "ExpandAriaLabel")
        ]
        public void BitNavShouldRespectGroupItems(bool isExpanded, string collapseAriaLabel, string ExpandAriaLabel)
        {
            List<BitNavLinkItem> navLinkItems = new()
            {
                new()
                {
                    Name = "Home",
                    Key = "key1",
                    CollapseAriaLabel = collapseAriaLabel,
                    ExpandAriaLabel = ExpandAriaLabel,
                    Links = new List<BitNavLinkItem>()
                    {
                        new()
                        {
                            Name = "Activity",
                            Url = "http://msn.com",
                            Key = "key1-1",
                            Title = "Activity"
                        }
                    }
                },
            };

            var components = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
            });

            var navLinksItem = components.Find(".bit-nav button");

            Assert.IsTrue(navLinksItem.HasAttribute("aria-label"));
            Assert.AreEqual(isExpanded ? ExpandAriaLabel : collapseAriaLabel, navLinksItem.GetAttribute("aria-label"));

            navLinksItem.Click();
            Assert.AreEqual(isExpanded ? collapseAriaLabel : ExpandAriaLabel, navLinksItem.GetAttribute("aria-label"));
        }

        [DataTestMethod,
            DataRow("key"),
        ]
        public void BitNav_ShouldRespectInitialSelectedKey(string initialSelectedKey)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, new() { new() { Name = "Test", Key = "key" } });
                parameters.Add(p => p.InitialSelectedKey, initialSelectedKey);
            });

            var selectedNav = com.Find($".bit-nav-selected-fluent");

            Assert.IsNotNull(selectedNav);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitNavShoulRespondToClickEvents(bool itemIsEnabled)
        {
            var componenet = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, new()
                {
                    new()
                    {
                        Name = "Test1",
                        Key = "key1",
                        IsEnabled = itemIsEnabled,
                        Links = new List<BitNavLinkItem>() { new() { Name = "Test2", Key = "key2" } }
                    }
                });
            });

            var navItem = componenet.Find($".bit-nav .bit-nav-link-{(itemIsEnabled ? "enabled" : "disabled")}-nourl-fluent");

            //TODO: bypassed - BUnit or Blazor issue
            //navItem.Click();
            //Assert.AreEqual(isEnabled && !itemDisabled ? "Test1" : null, componenet.Instance.OnlinkClickValue);
            //Assert.AreEqual(isEnabled && !itemDisabled ? "key1" : null, componenet.Instance.OnLinkExpandClickValue);

        }

        private readonly List<BitNavLinkItem> BasicNavLinks = new()
        {
            new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target = "_blank" },
            new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" },
            new BitNavLinkItem { Name = "Documents", Url = "http://example.com", Key = "key3", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", Icon = "News", Target = "_self" },
        };
    }
}
