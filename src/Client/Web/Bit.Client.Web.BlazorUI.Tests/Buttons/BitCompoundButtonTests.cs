﻿using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitCompoundButtonTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, ButtonStyle.Primary),
            DataRow(Visual.Fluent, true, ButtonStyle.Standard),
            DataRow(Visual.Fluent, false, ButtonStyle.Primary),
            DataRow(Visual.Fluent, false, ButtonStyle.Standard),

            DataRow(Visual.Cupertino, true, ButtonStyle.Primary),
            DataRow(Visual.Cupertino, true, ButtonStyle.Standard),
            DataRow(Visual.Cupertino, false, ButtonStyle.Primary),
            DataRow(Visual.Cupertino, false, ButtonStyle.Standard),

            DataRow(Visual.Material, true, ButtonStyle.Primary),
            DataRow(Visual.Material, true, ButtonStyle.Standard),
            DataRow(Visual.Material, false, ButtonStyle.Primary),
            DataRow(Visual.Material, false, ButtonStyle.Standard)
        ]
        public void BitCompoundButtonTest(Visual visual, bool isEnabled, ButtonStyle style)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.ButtonStyle, style);
                });

            var bitButton = com.Find(".bit-cmp-btn");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            Assert.IsTrue(bitButton.ClassList.Contains($"bit-cmp-btn-{isEnabledClass}-{visualClass}"));

            bitButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);
        }

        [DataTestMethod,
            DataRow(true, ButtonStyle.Primary, false, false),
            DataRow(true, ButtonStyle.Standard, true, false),
            DataRow(false, ButtonStyle.Primary, false, true),
            DataRow(false, ButtonStyle.Standard, true, false),
        ]
        public void BitCompoundButtonDisabledFocusTest(bool isEnabled, ButtonStyle style, bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-cmp-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            Assert.AreEqual(hasTabindexAttr, expectedResult);

            if (hasTabindexAttr)
            {
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
            }
        }

        [DataTestMethod, DataRow("Detailed description")]
        public void BitCompoundButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.IsTrue(bitCompoundButton.GetAttribute("aria-describedby").Contains(ariaDescription));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitCompoundButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.IsTrue(bitCompoundButton.GetAttribute("aria-label").Contains(ariaLabel));
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public void BitCompoundButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.AreEqual(bitCompoundButton.HasAttribute("aria-hidden"), expectedResult);
        }

        [DataTestMethod,
            DataRow("", true),
            DataRow("bing.com", true),
            DataRow("bing.com", false)
        ]
        public void BitCompoundButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
        {
            var component = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.Href, href);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitCompoundButton = component.Find(".bit-cmp-btn");
            var tagName = bitCompoundButton.TagName;
            var expectedElement = href.HasValue() && isEnabled ? "a" : "button" ;

            Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, ButtonStyle.Primary, false),
            DataRow(Visual.Fluent, ButtonStyle.Standard, false),
            DataRow(Visual.Cupertino, ButtonStyle.Primary, false),
            DataRow(Visual.Cupertino, ButtonStyle.Standard, false),
            DataRow(Visual.Material, ButtonStyle.Primary, false),
            DataRow(Visual.Material, ButtonStyle.Standard, false)
        ]
        public void BitCompoundButtonShouldHaveCorrectDisabledClassBasedOnButtonStyle(Visual visual, ButtonStyle buttonStyle, bool isEnabled)
        {
            var component = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.ButtonStyle, buttonStyle);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitCompoundButton = component.Find(".bit-cmp-btn");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            var buttonStyleStr = buttonStyle == ButtonStyle.Primary ? "primary" : "standard";
            Assert.IsTrue(bitCompoundButton.ClassList.Contains($"bit-cmp-btn-{buttonStyleStr}-disabled-{visualClass}"));
        }
    }
}
