using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Buttons
{
    [TestClass]
    public class BitButtonTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 1, "bit-btn-enabled-fluent", ButtonStyle.Standard), DataRow(false, 0, "bit-btn-disabled-fluent", ButtonStyle.Primary)]
        public async Task BitButtonShouldRespectIsEnabled(bool isEnabled, int count, string className, ButtonStyle style)
        {
            var com = RenderComponent<BitButtonCounterTest>(
                parameters =>
                {
                    parameters.Add(p => p.BitButtonIsEnabled, isEnabled);
                    parameters.Add(p => p.BitButtonStyle, style);
                });

            var bitButton = com.Find(".bit-btn-fluent");

            bitButton.Click();

            Assert.IsTrue(bitButton.ClassList.Contains(className));

            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }

    [TestClass]
    public class BitCompoundButtonTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 1, "bit-cmp-btn-enabled-fluent", ButtonStyle.Primary), DataRow(false, 0, "bit-cmp-btn-disabled-fluent", ButtonStyle.Standard)]
        public async Task BitCompoundButtonShouldRespectIsEnabled(bool isEnabled, int count, string className, ButtonStyle style)
        {
            var com = RenderComponent<BitCompoundButtonCounterTest>(
                parameters =>
                {
                    parameters.Add(p => p.BitCompoundButtonIsEnabled, isEnabled);
                    parameters.Add(p => p.BitCompoundButtonStyle, style);
                });

            var bitButton = com.Find(".bit-cmp-btn-fluent");

            bitButton.Click();

            Assert.IsTrue(bitButton.ClassList.Contains(className));

            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }
}
