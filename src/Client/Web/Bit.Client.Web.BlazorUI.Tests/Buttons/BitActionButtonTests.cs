using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitActionButtonTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, null, false),
            DataRow(Visual.Fluent, true, "AddFriend", true),
            DataRow(Visual.Fluent, false, "AddFriend", true),

            DataRow(Visual.Cupertino, true, null, false),
            DataRow(Visual.Cupertino, true, "AddFriend", true),
            DataRow(Visual.Cupertino, false, "AddFriend", true),

            DataRow(Visual.Material, true, null, false),
            DataRow(Visual.Material, true, "AddFriend", true),
            DataRow(Visual.Material, false, "AddFriend", true),
        ]
        public Task BitActionButtonTest(Visual visual, bool isEnabled, string iconName, bool expectedResult)
        {
            var com = RenderComponent<BitActionButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IconName, iconName);
            });

            var bitButton = com.Find(".bit-act-btn");
            var bitIconITag = com.Find(".bit-act-btn > span > i");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitButton.ClassList.Contains($"bit-act-btn-{isEnabledClass}-{visualClass}"));

            if (string.IsNullOrEmpty(iconName) is false)
                Assert.AreEqual(bitIconITag.ClassList.Contains($"bit-icon--{iconName}"), expectedResult);

            bitButton.Click();

            Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);

            return Task.CompletedTask;
        }

        [DataTestMethod,
           DataRow(true, false, false),
           DataRow(true, true, false),
           DataRow(false, false, true),
           DataRow(false, true, false),
       ]
        public Task BitActionButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
        {
            var com = RenderComponent<BitActionButtonTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
            });

            var bitButton = com.Find(".bit-act-btn");

            if (bitButton.HasAttribute("tabindex"))
                Assert.AreEqual(bitButton.GetAttribute("tabindex").Equals("-1"), expectedResult);

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed description")]
        public Task BitActionButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitActionButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitButton = com.Find(".bit-act-btn");

            Assert.IsTrue(bitButton.GetAttribute("aria-describedby").Contains(ariaDescription));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitActionButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitActionButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitButton = com.Find(".bit-act-btn");

            Assert.IsTrue(bitButton.GetAttribute("aria-label").Contains(ariaLabel));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public Task BitActionButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitActionButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitButton = com.Find(".bit-act-btn");

            Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);

            return Task.CompletedTask;
        }
    }
}
