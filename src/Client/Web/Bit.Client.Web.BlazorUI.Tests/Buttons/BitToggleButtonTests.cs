using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitToggleButtonTests : BunitTestContext
    {
        [DataTestMethod,
           DataRow(Visual.Fluent, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Fluent, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Fluent, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Fluent, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),

           DataRow(Visual.Cupertino, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Cupertino, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Cupertino, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Cupertino, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),

           DataRow(Visual.Material, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Material, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Material, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
           DataRow(Visual.Material, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
       ]
        public void BitToggleButtonShouldHaveCorrectLabelAndIcon(Visual visual,
            bool isChecked,
            bool isEnabled,
            string checkedLabel,
            string unCheckedLabel,
            string checkedIconName,
            string unCheckedIconName)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsChecked, isChecked);
                parameters.Add(p => p.ChekedLabel, checkedLabel);
                parameters.Add(p => p.UnChekedLabel, unCheckedLabel);
                parameters.Add(p => p.CheckedIconName, checkedIconName);
                parameters.Add(p => p.UnCheckedIconName, unCheckedIconName);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");
            var bitIconTag = component.Find(".bit-tgl-btn > span > i");
            var bitLabelTag = component.Find(".bit-tgl-btn > span > span");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitToggleButton.ClassList.Contains($"bit-tgl-btn-{isEnabledClass}-{visualClass}"));

            var label = (isChecked) ? checkedLabel : unCheckedLabel;
            Assert.AreEqual(bitLabelTag.TextContent, label);

            var iconName = (isChecked) ? checkedIconName : unCheckedIconName;
            Assert.IsTrue(bitIconTag.ClassList.Contains($"bit-icon--{iconName}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Fluent, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Fluent, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Fluent, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),

            DataRow(Visual.Cupertino, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Cupertino, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Cupertino, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Cupertino, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),

            DataRow(Visual.Material, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Material, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Material, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2"),
            DataRow(Visual.Material, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2")
        ]
        public void BitToggleButtonClickEvent(Visual visual,
            bool isChecked,
            bool isEnabled,
            string checkedLabel,
            string unCheckedLabel,
            string checkedIconName,
            string unCheckedIconName)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsChecked, isChecked);
                parameters.Add(p => p.ChekedLabel, checkedLabel);
                parameters.Add(p => p.UnChekedLabel, unCheckedLabel);
                parameters.Add(p => p.CheckedIconName, checkedIconName);
                parameters.Add(p => p.UnCheckedIconName, unCheckedIconName);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.OnClick, OnToggleButtonClick);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");

            bitToggleButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, IncrementValueOnClick);

            Assert.AreEqual(isChecked ^ isEnabled, bitToggleButton.ClassList.Contains("bit-tgl-btn-checked"));

            var bitIconTag = component.Find(".bit-tgl-btn > span > i");

            Assert.AreEqual(isEnabled, bitIconTag.ClassList.Contains($"bit-icon--{(isChecked ? unCheckedIconName : checkedIconName)}"));

            var bitLabelTag = component.Find(".bit-tgl-btn > span > span");
            if (isEnabled)
            {
                Assert.AreEqual(bitLabelTag.TextContent, (isChecked ? unCheckedLabel : checkedLabel));
            }
            else
            {
                Assert.AreEqual(bitLabelTag.TextContent, (isChecked ? checkedLabel : unCheckedLabel));
            }
        }

        [DataTestMethod,
          DataRow(true, ButtonStyle.Primary, false),
          DataRow(true, ButtonStyle.Standard, true),
          DataRow(false, ButtonStyle.Primary, false),
          DataRow(false, ButtonStyle.Standard, true),
      ]
        public void BitToggleButtonDisabledFocusTest(bool isEnabled, ButtonStyle style, bool allowDisabledFocus)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            Assert.AreEqual(hasTabindexAttr, !isEnabled && !allowDisabledFocus);

            if (hasTabindexAttr)
            {
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
            }
        }


        [DataTestMethod, DataRow("Detailed description")]
        public void BitToggleButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));

            Assert.AreEqual(ariaDescription, bitButton.GetAttribute("aria-describedby"));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitToggleButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-label"));

            Assert.AreEqual(ariaLabel, bitButton.GetAttribute("aria-label"));
        }

        [DataTestMethod,
            DataRow(true, true),
            DataRow(false, false),
            DataRow(null, false)
        ]
        public void BitToggleButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);
        }

        private int IncrementValueOnClick = 0;
        public void OnToggleButtonClick()
        {
            IncrementValueOnClick++;
        }
    }
}

