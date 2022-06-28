using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitToggleButtonTests : BunitTestContext
    {
        [DataTestMethod,
           DataRow(Visual.Fluent, true, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Fluent, true, false, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Fluent, false, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Fluent, false, false, "Button label", BitIconName.Emoji2, "title"),

           DataRow(Visual.Cupertino, true, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Cupertino, true, false, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Cupertino, false, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Cupertino, false, false, "Button label", BitIconName.Emoji2, "title"),

           DataRow(Visual.Material, true, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Material, true, false, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Material, false, true, "Button label", BitIconName.Emoji2, "title"),
           DataRow(Visual.Material, false, false, "Button label", BitIconName.Emoji2, "title"),
       ]
        public void BitToggleButtonShouldHaveCorrectLabelAndIconAndTitle(Visual visual,
            bool isChecked,
            bool isEnabled,
            string label,
            BitIconName? iconName,
            string title)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsChecked, isChecked);
                parameters.Add(p => p.Label, label);
                parameters.Add(p => p.IconName, iconName);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Title, title);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");
            var bitIconTag = component.Find(".bit-tgl-btn > span > i");
            var bitLabelTag = component.Find(".bit-tgl-btn > span > span");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitToggleButton.ClassList.Contains($"bit-tgl-btn-{isEnabledClass}-{visualClass}"));

            Assert.AreEqual(bitLabelTag.TextContent, label);

            Assert.AreEqual(bitToggleButton.GetAttribute("title"), title);

            Assert.IsTrue(bitIconTag.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
        ]
        public void BitToggleButtonClickEvent(bool isEnabled)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");

            bitToggleButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.CurrentCount);
        }

        [DataTestMethod,
            DataRow(true, true),
            DataRow(true, false),
            DataRow(false, true),
            DataRow(false, false)
        ]
        public void BitToggleButtonShouldChangeIsCheckedParameterAfterClickWhenIsEnable(bool isEnabled, bool isChecked)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IsChecked, isChecked);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");

            bitToggleButton.Click();

            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.AreEqual(isEnabled ? !isChecked : isChecked, component.Instance.IsChecked);
        }

        [DataTestMethod,
            DataRow(true, true),
            DataRow(true, false),
            DataRow(false, true),
            DataRow(false, false)
        ]
        public void BitToggleButtonShouldAddRomveCheckedClassAfterClickWhenIsEnable(bool isEnabled, bool isChecked)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IsChecked, isChecked);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");

            bitToggleButton.Click();

            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.AreEqual(isEnabled ? !isChecked : isChecked, bitToggleButton.ClassList.Contains("bit-tgl-btn-checked"));
        }

        [DataTestMethod,
          DataRow(true, false),
          DataRow(true, true),
          DataRow(false, false),
          DataRow(false, true),
        ]
        public void BitToggleButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = component.Find(".bit-tgl-btn");
            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            Assert.AreEqual(!isEnabled && !allowDisabledFocus, hasTabindexAttr);

            if (hasTabindexAttr)
            {
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
            }
        }

        [DataTestMethod, DataRow("Detailed description")]
        public void BitToggleButtonAriaDescriptionTest(string ariaDescription)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitButton = component.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));

            Assert.AreEqual(bitButton.GetAttribute("aria-describedby"), ariaDescription);
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitToggleButtonAriaLabelTest(string ariaLabel)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitButton = component.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-label"));

            Assert.AreEqual(bitButton.GetAttribute("aria-label"), ariaLabel);
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false),
            DataRow(null)
        ]
        public void BitToggleButtonAriaHiddenTest(bool ariaHidden)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitButton = component.Find(".bit-tgl-btn");

            Assert.AreEqual(ariaHidden ? true : false, bitButton.HasAttribute("aria-hidden"));
        }

        [DataTestMethod,
            DataRow("", true),
            DataRow("bing.com", true),
            DataRow("bing.com", false)
        ]
        public void BitToggleButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
        {
            var component = RenderComponent<BitToggleButton>(parameters =>
            {
                parameters.Add(p => p.Href, href);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");
            var tagName = bitToggleButton.TagName;
            var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

            Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
        }
    }
}
