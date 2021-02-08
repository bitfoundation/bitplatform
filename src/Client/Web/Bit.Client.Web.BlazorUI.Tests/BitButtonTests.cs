using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Tests
{
    [TestClass]
    public class BitButtonTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 1, "enabled"), DataRow(false, 0, "disabled")]
        public async Task BitButtonShouldRespectIsEnabled(bool isEnabled, int count, string className)
        {
            var com = RenderComponent<BitButtonCounterTest>(parameters => parameters.Add(p => p.BitButtonIsEnabled, isEnabled));

            var bitButton = com.Find(".bit-button");

            bitButton.Click();

            Assert.IsTrue(bitButton.ClassList.Contains(className));

            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }

    [TestClass]
    public class BitCompoundButtonTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 1, "enabled"), DataRow(false, 0, "disabled")]
        public async Task BitCompoundButtonShouldRespectIsEnabled(bool isEnabled, int count, string className)
        {
            var com = RenderComponent<BitCompoundButtonCounterTest>(parameters => parameters.Add(p => p.BitCompoundButtonIsEnabled, isEnabled));

            var bitButton = com.Find(".bit-compoundbutton");

            bitButton.Click();

            Assert.IsTrue(bitButton.ClassList.Contains(className));

            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }
}
