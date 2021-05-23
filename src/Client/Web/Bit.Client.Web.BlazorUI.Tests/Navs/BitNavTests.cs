using System.Collections.Generic;
using System.Threading.Tasks;
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
        public Task BitNav_BaseTest(Visual visual)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
            });

            var bitNav = com.Find(".bit-nav");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{visualClass}") && bitNav.ClassList.Contains($"bit-nav"));

            return Task.CompletedTask;
        }

        [TestMethod]
        public Task BitNav_NavLinkNull_ThrowNullReferenceException()
        {
            _ = Assert.ThrowsException<System.NullReferenceException>(() =>
              {
                  _ = RenderComponent<BitNavTest>(parameters =>
                   {
                       parameters.Add(p => p.NavLinks, null);
                   });
              });

            return Task.CompletedTask;
        }

        [TestMethod]
        public Task BitNav_NavLinkEmpty_ThrowNullReferenceException()
        {
            _ = Assert.ThrowsException<System.NullReferenceException>(() =>
              {
                  _ = RenderComponent<BitNavTest>(parameters =>
                  {
                      parameters.Add(p => p.NavLinks, new List<NavLink>());
                  });
              });

            return Task.CompletedTask;
        }

        [DataTestMethod,
           DataRow(Visual.Fluent, true),
           DataRow(Visual.Fluent, false),
           DataRow(Visual.Cupertino, true),
           DataRow(Visual.Cupertino, false),
           DataRow(Visual.Material, true),
           DataRow(Visual.Material, false),
        ]
        public Task BitNav_IsEnableTest(Visual visual, bool isEnabled)
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

            return Task.CompletedTask;
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false),
        ]
        public Task BitNav_IsOnTopTest(Visual visual, bool isOnTop)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsOnTop, isOnTop);
            });

            var bitNav = com.Find(".bit-nav");
            var isOnTopClass = isOnTop ? "top" : "no-top";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitNav.ClassList.Contains($"bit-nav-{isOnTopClass}-{visualClass}"));

            return Task.CompletedTask;
        }

        [DataTestMethod,
         DataRow(Visual.Fluent, "key"),
         DataRow(Visual.Cupertino, "key"),
         DataRow(Visual.Material, "key"),
        ]
        public Task BitNav_SelectedKeyTest(Visual visual, string selectedKey)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinks, new List<NavLink> { new NavLink(name: "test", key: "key") });
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.SelectedKey, selectedKey);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var selectedNav = com.Find($".bit-nav-selected-{visualClass}");

            Assert.IsNotNull(selectedNav);
            return Task.CompletedTask;
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
        public Task BitNavChildrenTest(Visual visual, bool isEnable, bool hasUrl)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinks,
                    new List<NavLink>
                    {
                        new NavLink(name: "test", key: "key", disabled: !isEnable, url:(hasUrl? "https://www.google.com/":null))
                    });
                parameters.Add(p => p.Visual, visual);
            });

            var enabledClass = isEnable ? "enabled" : "disabled";
            var hasUrlClass = hasUrl ? "hasurl" : "nourl";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var element = com.Find($".bit-nav-link-{enabledClass}-{hasUrlClass}-{visualClass}");

            Assert.IsNotNull(element);
            return Task.CompletedTask;
        }

        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),
          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),
          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false),
        ]
        public Task BitNavChildren_IsOnTopTest(Visual visual, bool hasIcon)
        {
            var com = RenderComponent<BitNavTest>(parameters =>
            {
                parameters.Add(p => p.NavLinks,
                    new List<NavLink>
                    {
                        new NavLink(name: "test", key: "key", icon:(hasIcon? "News":null))
                    });
                parameters.Add(p => p.Visual, visual);
            });

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var hasIconClass = hasIcon ? "has-icon" : "has-not-icon";
            var element = com.Find($".bit-nav-{hasIconClass}-{visualClass}");
            
            Assert.IsNotNull(element);

            return Task.CompletedTask;
        }
    }
}
