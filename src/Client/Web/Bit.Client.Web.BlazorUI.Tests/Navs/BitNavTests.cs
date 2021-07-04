using System.Threading.Tasks;
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
        public void BitNav_BaseTest(Visual visual)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
            });

            var bitNav = com.Find(".bit-nav");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{visualClass}") && bitNav.ClassList.Contains($"bit-nav"));
        }

        [DataTestMethod,
           DataRow(Visual.Fluent, true),
           DataRow(Visual.Fluent, false),
           DataRow(Visual.Cupertino, true),
           DataRow(Visual.Cupertino, false),
           DataRow(Visual.Material, true),
           DataRow(Visual.Material, false),
        ]
        public void BitNav_IsEnableTest(Visual visual, bool isEnabled)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitNav = com.Find(".bit-nav");
            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{isEnabledClass}-{visualClass}"));
        }

        [DataTestMethod,
          DataRow(true),
          DataRow(false),
        ]
        public void BitNav_IsOnTopTest(bool isOnTop)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.IsOnTop, isOnTop);
            });

            var bitNav = com.Find(".bit-nav");
            var isOnTopClass = isOnTop ? "top" : "no-top";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{isOnTopClass}"));
        }

        [DataTestMethod,
         DataRow(Visual.Fluent, "key"),
         DataRow(Visual.Cupertino, "key"),
         DataRow(Visual.Material, "key"),
        ]
        public void BitNav_SelectedKeyTest(Visual visual, string selectedKey)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, new List<BitNavLinkItem> { new BitNavLinkItem { Name = "Test", Key = "key" } });
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.SelectedKey, selectedKey);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var selectedNav = com.Find($".bit-nav-selected-{visualClass}");

            Assert.IsNotNull(selectedNav);
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, true, true),
          DataRow(Visual.Fluent, true, false),
          DataRow(Visual.Fluent, false, true),
          DataRow(Visual.Fluent, false, false),

          DataRow(Visual.Cupertino, true, true),
          DataRow(Visual.Cupertino, true, false),
          DataRow(Visual.Cupertino, false, true),
          DataRow(Visual.Cupertino, false, false),

          DataRow(Visual.Material, true, true),
          DataRow(Visual.Material, true, false),
          DataRow(Visual.Material, false, true),
          DataRow(Visual.Material, false, false),
      ]
        public void BitNavChildrenTest(Visual visual, bool disabled, bool hasUrl)
        {
            string url = hasUrl ? "https://www.google.com/" : null;
            var navLinkItems = new List<BitNavLinkItem> { new BitNavLinkItem { Name = "test", Key = "key", Disabled = disabled, Url = url } };
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.Visual, visual);
            });

            var enabledClass = disabled ? "disabled" : "enabled";
            var hasUrlClass = hasUrl ? "hasurl" : "nourl";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var element = com.Find($".bit-nav-link-{enabledClass}-{hasUrlClass}-{visualClass}");

            Assert.IsNotNull(element);
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false),
        ]
        public void BitNavChildren_HasIconTest(Visual visual, bool hasIcon)
        {
            string icon = hasIcon ? "News" : null;
            var navLinkItems = new List<BitNavLinkItem> { new BitNavLinkItem { Name = "test", Key = "key", Icon = icon } };
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinkItems, navLinkItems);
                parameters.Add(p => p.Visual, visual);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var hasIconClass = hasIcon ? "has-icon" : "has-not-icon";
            var element = com.Find($".bit-nav-{hasIconClass}-{visualClass}");

            Assert.IsNotNull(element);
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitNavAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitNav = com.Find(".bit-nav");

            Assert.IsTrue(bitNav.GetAttribute("aria-label").Equals(ariaLabel));
            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitNavLinkItemsAriaLabelTest(string ariaLabel)
        {
            var navLinkItems = new List<BitNavLinkItem> {
              new BitNavLinkItem {
                Name = "Activity",
                Key = "key1-1",
                Links = new List < BitNavLinkItem > {
                  new BitNavLinkItem {
                    Name = "Activity",
                    Key = "key1-1-1"
                  },
                  new BitNavLinkItem {
                    Name = "MSN",
                    Key = "key1-1-2"
                  }
                }
              },
              new BitNavLinkItem {
                Name = "MSN",
                Key = "key1-2",
                Links = new List < BitNavLinkItem > {
                  new BitNavLinkItem {
                    Name = "Activity",
                    Key = "key1-2-1"
                  },
                  new BitNavLinkItem {
                    Name = "MSN",
                    Key = "key1-2-2"
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

            foreach ( var bitNavLinksItem in bitNavLinksItems)
            {
                Assert.IsTrue(bitNavLinksItem.GetAttribute("aria-label").Equals(ariaLabel)); 
            }
            return Task.CompletedTask;
        }
    }
}

