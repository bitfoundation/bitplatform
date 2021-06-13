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
        public Task BitToggleButtonShouldHaveCorrectLableAndIcon(Visual visual,
        bool isChecked, bool isEnabled,
        string checkedLabel, string unCheckedLabel, string checkedIconName, string unCheckedIconName)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Checked, isChecked);
                parameters.Add(p => p.ChekedLabel, checkedLabel);
                parameters.Add(p => p.UnChekedLabel, unCheckedLabel);
                parameters.Add(p => p.CheckedIconName, checkedIconName);
                parameters.Add(p => p.UnCheckedIconName, unCheckedIconName);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");
            var bitIconTag = component.Find(".bit-tgl-btn > span > i");
            var bitLabelTag = component.Find(".bit-tgl-btn > span > span.bit-tgl-lbl ");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitToggleButton.ClassList.Contains($"bit-tgl-btn-{isEnabledClass}-{visualClass}"));

            var lable = (isChecked) ? checkedLabel : unCheckedLabel;
            Assert.AreEqual(bitLabelTag.TextContent, lable);

            var iconName = (isChecked) ? checkedIconName : unCheckedIconName;
            Assert.IsTrue(bitIconTag.ClassList.Contains($"bit-icon--{iconName}"));

            return Task.CompletedTask;
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, true),
            DataRow(Visual.Fluent, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, false),
            DataRow(Visual.Fluent, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, true),
            DataRow(Visual.Fluent, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, false),

            DataRow(Visual.Cupertino, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, true),
            DataRow(Visual.Cupertino, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, false),
            DataRow(Visual.Cupertino, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, false),
            DataRow(Visual.Cupertino, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, true),

            DataRow(Visual.Material, true, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, true),
            DataRow(Visual.Material, true, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, false),
            DataRow(Visual.Material, false, true, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", true, false),
            DataRow(Visual.Material, false, false, "Button checked", "Button unchecked", "EmojiNeutral", "Emoji2", false, true),

        ]
        public Task BitToggleButtonClickEvent(Visual visual,
            bool isChecked, bool isEnabled,
            string checkedLabel, string unCheckedLabel, string checkedIconName, string unCheckedIconName,
            bool expectedResult, bool afterClickresult)
        {
            var component = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Checked, isChecked);
                parameters.Add(p => p.ChekedLabel, checkedLabel);
                parameters.Add(p => p.UnChekedLabel, unCheckedLabel);
                parameters.Add(p => p.CheckedIconName, checkedIconName);
                parameters.Add(p => p.UnCheckedIconName, unCheckedIconName);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitToggleButton = component.Find(".bit-tgl-btn");

            bitToggleButton.Click();

            Assert.AreEqual(expectedResult, bitToggleButton.ClassList.Contains("bit-tgl-btn-checked"));

            var bitIconTag = component.Find(".bit-tgl-btn > span > i");

            Assert.AreEqual(isEnabled, bitIconTag.ClassList.Contains($"bit-icon--{(isChecked ? unCheckedIconName : checkedIconName)}"));

            var bitLabelTag = component.Find(".bit-tgl-btn > span > span.bit-tgl-lbl ");
            if (isEnabled)
            {
                Assert.AreEqual(bitLabelTag.TextContent, (isChecked ? unCheckedLabel : checkedLabel));
            }
            else
            {
                Assert.AreEqual(bitLabelTag.TextContent, (isChecked ? checkedLabel : unCheckedLabel));
            }

            return Task.CompletedTask;
        }

        [DataTestMethod,
          DataRow(true, ButtonStyle.Primary, false, false),
          DataRow(true, ButtonStyle.Standard, true, false),
          DataRow(false, ButtonStyle.Primary, false, true),
          DataRow(false, ButtonStyle.Standard, true, false),
      ]
        public Task BitToggleButtonDisabledFocusTest(bool isEnabled, ButtonStyle style,
            bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            var hasTabindexAttr = bitButton.HasAttribute("tabindex");

            Assert.AreEqual(hasTabindexAttr, expectedResult);

            if (hasTabindexAttr)
                Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));

            return Task.CompletedTask;
        }


        [DataTestMethod, DataRow("Detailed description")]
        public Task BitToggleButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitToggleButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.IsTrue(bitButton.HasAttribute("aria-label"));

            return Task.CompletedTask;
        }

        [DataTestMethod,
            DataRow(true, true), 
            DataRow(false, false), 
            DataRow(null, false)
        ]
        public Task BitToggleButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitToggleButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitButton = com.Find(".bit-tgl-btn");

            Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);

            return Task.CompletedTask;
        }
    }
}

