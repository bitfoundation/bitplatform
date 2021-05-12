using System.Threading.Tasks;
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
            DataRow(Visual.Material, false, ButtonStyle.Standard),
        ]
        public Task BitCompoundButtonTest(Visual visual, bool isEnabled, ButtonStyle style)
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

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed description")]
        public Task BitCompoundButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.IsTrue(bitCompoundButton.HasAttribute("aria-describedby"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitCompoundButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.IsTrue(bitCompoundButton.HasAttribute("aria-label"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public Task BitCompoundButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitCompoundButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitCompoundButton = com.Find(".bit-cmp-btn");

            Assert.AreEqual(bitCompoundButton.HasAttribute("aria-hidden"), expectedResult);

            return Task.CompletedTask;
        }
    }
}
