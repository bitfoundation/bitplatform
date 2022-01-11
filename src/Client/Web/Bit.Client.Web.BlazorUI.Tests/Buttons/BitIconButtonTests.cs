using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitIconButtonTests : BunitTestContext
    {
        [DataTestMethod,
               DataRow(Visual.Fluent, true, BitIconName.Emoji2, null),
               DataRow(Visual.Fluent, false, BitIconName.Emoji2, null),
               DataRow(Visual.Fluent, true, BitIconName.Emoji2, "I'm Happy"),
               DataRow(Visual.Fluent, false, BitIconName.Emoji2, "I'm Happy"),

               DataRow(Visual.Cupertino, true, BitIconName.Emoji2, null),
               DataRow(Visual.Cupertino, false, BitIconName.Emoji2, null),
               DataRow(Visual.Cupertino, true, BitIconName.Emoji2, "I'm Happy"),
               DataRow(Visual.Cupertino, false, BitIconName.Emoji2, "I'm Happy"),

               DataRow(Visual.Material, true, BitIconName.Emoji2, null),
               DataRow(Visual.Material, false, BitIconName.Emoji2, null),
               DataRow(Visual.Material, true, BitIconName.Emoji2, "I'm Happy"),
               DataRow(Visual.Material, false, BitIconName.Emoji2, "I'm Happy"),
           ]
        public void BitIconButtonTest(Visual visual, bool isEnabled, BitIconName iconName, string title)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IconName, iconName);
                parameters.Add(p => p.Title, title);
            });

            var bitIconButton = com.Find(".bit-ico-btn");
            var bitIconITag = com.Find(".bit-ico-btn > span > i");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitIconButton.ClassList.Contains($"bit-ico-btn-{isEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

            if (title.HasValue())
            {
                Assert.IsTrue(bitIconButton.GetAttribute("title").Contains(title));
            }

            bitIconButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);
        }

        [DataTestMethod,
          DataRow(true, false),
          DataRow(true, true),
          DataRow(false, false),
          DataRow(false, true),
        ]
        public void BitIconButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-ico-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            var expectedResult = isEnabled ? false : allowDisabledFocus ? false : true;

            Assert.AreEqual(hasTabindexAttr, expectedResult);

            if (hasTabindexAttr)
            {
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
            }
        }

        [DataTestMethod, DataRow("Detailed description")]
        public void BitIconButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.IsTrue(bitIconButton.GetAttribute("aria-describedby").Contains(ariaDescription));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitIconButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.IsTrue(bitIconButton.GetAttribute("aria-label").Contains(ariaLabel));
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
            DataRow(null)
        ]
        public void BitIconButtonAriaHiddenTest(bool ariaHidden)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitIconButton = com.Find(".bit-ico-btn");
            var expectedResult = ariaHidden ? true : false;

            Assert.AreEqual(bitIconButton.HasAttribute("aria-hidden"), expectedResult);
        }

        [DataTestMethod,
            DataRow("", true),
            DataRow("bing.com", true),
            DataRow("bing.com", false)
        ]
        public void BitIconButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
        {
            var component = RenderComponent<BitIconButton>(parameters =>
            {
                parameters.Add(p => p.Href, href);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitIconButton = component.Find(".bit-ico-btn");
            var tagName = bitIconButton.TagName;
            var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

            Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
        }
    }
}
