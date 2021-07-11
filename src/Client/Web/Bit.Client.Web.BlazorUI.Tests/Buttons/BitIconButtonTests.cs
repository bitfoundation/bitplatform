using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitIconButtonTests : BunitTestContext
    {
        [DataTestMethod,
               DataRow(Visual.Fluent, true, "Emoji2", null, false),
               DataRow(Visual.Fluent, false, "Emoji2", null, false),
               DataRow(Visual.Fluent, true, "Emoji2", "I'm Happy", true),
               DataRow(Visual.Fluent, false, "Emoji2", "I'm Happy", false),

               DataRow(Visual.Cupertino, true, "Emoji2", null, false),
               DataRow(Visual.Cupertino, false, "Emoji2", null, false),
               DataRow(Visual.Cupertino, true, "Emoji2", "I'm Happy", true),
               DataRow(Visual.Cupertino, false, "Emoji2", "I'm Happy", false),

               DataRow(Visual.Material, true, "Emoji2", null, false),
               DataRow(Visual.Material, false, "Emoji2", null, false),
               DataRow(Visual.Material, true, "Emoji2", "I'm Happy", true),
               DataRow(Visual.Material, false, "Emoji2", "I'm Happy", false),
           ]
        public void BitIconButtonTest(Visual visual, bool isEnabled, string iconName, string toolTip, bool expectedResult)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IconName, iconName);
                parameters.Add(p => p.ToolTip, toolTip);
            });

            var bitIconButton = com.Find(".bit-ico-btn");
            var bitIconITag = com.Find(".bit-ico-btn > span > i");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitIconButton.ClassList.Contains($"bit-ico-btn-{isEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName}"));

            if (toolTip.HasValue())
            {
                Assert.IsTrue(bitIconButton.GetAttribute("title").Contains(toolTip));
            }

            bitIconButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);
        }

        [DataTestMethod,
          DataRow(true, false, false),
          DataRow(true, true, false),
          DataRow(false, false, true),
          DataRow(false, true, false),
        ]
        public void BitIconButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-ico-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

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

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public void BitIconButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.AreEqual(bitIconButton.HasAttribute("aria-hidden"), expectedResult);
        }
    }
}
