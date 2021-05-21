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
        public Task BitIconButtonTest(Visual visual, bool isEnabled, string iconName, string toolTip, bool expectedResult)
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

            if (string.IsNullOrEmpty(toolTip) is false)
                Assert.IsTrue(bitIconButton.GetAttribute("title").Contains(toolTip));

            bitIconButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);

            return Task.CompletedTask;
        }

        [DataTestMethod,
          DataRow(true, false, false),
          DataRow(true, true, false),
          DataRow(false, false, true),
          DataRow(false, true, false),
      ]
        public Task BitIconButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-ico-btn");

            Assert.AreEqual(bitButton.HasAttribute("tabindex"), expectedResult);

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed description")]
        public Task BitIconButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.IsTrue(bitIconButton.GetAttribute("aria-describedby").Contains(ariaDescription));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitIconButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.IsTrue(bitIconButton.GetAttribute("aria-label").Contains(ariaLabel));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public Task BitIconButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitIconButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitIconButton = com.Find(".bit-ico-btn");

            Assert.AreEqual(bitIconButton.HasAttribute("aria-hidden"), expectedResult);

            return Task.CompletedTask;
        }
    }
}
