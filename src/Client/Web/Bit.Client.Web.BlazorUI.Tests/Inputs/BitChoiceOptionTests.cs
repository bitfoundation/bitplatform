using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    public class BitChoiceOptionTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 2, "enabled"),
         DataRow(false, 0, "disabled")]
        public async Task BitChoiceOptionShouldRespectIsEnabled(bool isEnabled, int count, string enabledClass)
        {
            var com = RenderComponent<BitChoiceOptionTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.Value, count.ToString());
                });
            var bitChoiceOptionInput = com.Find("input");
            bitChoiceOptionInput.Change(count);
            bitChoiceOptionInput.Click();
            Assert.IsTrue(bitChoiceOptionInput.ParentElement.ClassList.Contains($"bit-cho-{enabledClass}-fluent"));
            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }
}
