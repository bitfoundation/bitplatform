using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitButtonTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, ButtonStyle.Primary, "title"),
            DataRow(Visual.Fluent, true, ButtonStyle.Standard, "title"),
            DataRow(Visual.Fluent, false, ButtonStyle.Primary, "title"),
            DataRow(Visual.Fluent, false, ButtonStyle.Standard, "title"),

            DataRow(Visual.Cupertino, true, ButtonStyle.Primary, "title"),
            DataRow(Visual.Cupertino, true, ButtonStyle.Standard, "title"),
            DataRow(Visual.Cupertino, false, ButtonStyle.Primary, "title"),
            DataRow(Visual.Cupertino, false, ButtonStyle.Standard, "title"),

            DataRow(Visual.Material, true, ButtonStyle.Primary, "title"),
            DataRow(Visual.Material, true, ButtonStyle.Standard, "title"),
            DataRow(Visual.Material, false, ButtonStyle.Primary, "title"),
            DataRow(Visual.Material, false, ButtonStyle.Standard, "title"),
        ]
        public void BitButtonTest(Visual visual, bool isEnabled, ButtonStyle style, string title)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.Title, title);
            });

            var bitButton = com.Find(".bit-btn");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitButton.ClassList.Contains($"bit-btn-{isEnabledClass}-{visualClass}"));

            Assert.AreEqual(bitButton.GetAttribute("title"), title);

            bitButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);
        }

        [DataTestMethod,
            DataRow(true, ButtonStyle.Primary, false, false),
            DataRow(true, ButtonStyle.Standard, true, false),
            DataRow(false, ButtonStyle.Primary, false, true),
            DataRow(false, ButtonStyle.Standard, true, false),
        ]
        public void BitButtonDisabledFocusTest(bool isEnabled, ButtonStyle style, bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            Assert.AreEqual(hasTabindexAttr, expectedResult);

            if (hasTabindexAttr)
            {
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
            }
        }

        [DataTestMethod,
             DataRow(Visual.Fluent, true, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Fluent, true, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Fluent, false, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Fluent, false, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),

             DataRow(Visual.Cupertino, true, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Cupertino, true, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Cupertino, false, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Cupertino, false, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),

             DataRow(Visual.Material, true, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Material, true, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Material, false, ButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
             DataRow(Visual.Material, false, ButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
        ]
        public void BitAnchorButtonTest(Visual visual, bool isEnabled, ButtonStyle style, string href, string title, string target)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.Href, href);
                parameters.Add(p => p.Title, title);
                parameters.Add(p => p.Target, target);
            });

            var bitButton = com.Find(".bit-btn");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitButton.ClassList.Contains($"bit-btn-{isEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitButton.HasAttribute("href"));

            Assert.AreEqual(bitButton.GetAttribute("title"), title);

            Assert.AreEqual(bitButton.GetAttribute("target"), target);
        }

        [DataTestMethod, DataRow("Detailed description")]
        public void BitButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitButton = com.Find(".bit-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitButton = com.Find(".bit-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-label"));
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public void BitButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitButton = com.Find(".bit-btn");

            Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);
        }
    }
}
