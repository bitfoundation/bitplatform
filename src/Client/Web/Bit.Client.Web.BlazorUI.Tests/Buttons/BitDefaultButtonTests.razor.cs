using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitDefaultButtonTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(Visual.Fluent, true, ButtonStyle.Standard, "bit", "_blank"),
            DataRow(Visual.Fluent, false, ButtonStyle.Primary, null, null),

            DataRow(Visual.Cupertino, true, ButtonStyle.Standard, "bit", "_blank"),
            DataRow(Visual.Cupertino, false, ButtonStyle.Primary, null, null),

            DataRow(Visual.Material, true, ButtonStyle.Standard, "bit", "_blank"),
            DataRow(Visual.Material, false, ButtonStyle.Primary, null, null),
        ]
        public Task BitDefaultButtonTest(Visual visual, bool isEnabled, ButtonStyle style, string title, string target)
        {
            var com = RenderComponent<BitDefaultButtonTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.ButtonStyle, style);
                parameters.Add(p => p.Title, title);
                parameters.Add(p => p.Target, target);
            });

            var bitDefaultButton = com.Find(".bit-def-btn");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitDefaultButton.ClassList.Contains($"bit-def-btn-{isEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitDefaultButton.HasAttribute("href"));

            Assert.IsTrue(bitDefaultButton.HasAttribute("title"));

            Assert.IsTrue(bitDefaultButton.HasAttribute("target"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed description")]
        public Task BitDefaultButtonAriaDescriptionTest(string ariaDescription)
        {
            var com = RenderComponent<BitDefaultButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaDescription, ariaDescription);
            });

            var bitDefaultButton = com.Find(".bit-def-btn");

            Assert.IsTrue(bitDefaultButton.HasAttribute("aria-describedby"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow("Detailed label")]
        public Task BitDefaultButtonAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitDefaultButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitDefaultButton = com.Find(".bit-def-btn");

            Assert.IsTrue(bitDefaultButton.HasAttribute("aria-label"));

            return Task.CompletedTask;
        }

        [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
        public Task BitDefaultButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
        {
            var com = RenderComponent<BitDefaultButtonTest>(parameters =>
            {
                parameters.Add(p => p.AriaHidden, ariaHidden);
            });

            var bitDefaultButton = com.Find(".bit-def-btn");

            Assert.AreEqual(bitDefaultButton.HasAttribute("aria-hidden"), expectedResult);

            return Task.CompletedTask;
        }
    }
}
