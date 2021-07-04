using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitInputTests : BunitTestContext
    {
        [DataTestMethod, 
            DataRow(true, 2, "enabled"),
            DataRow(false, 0, "disabled")]
        public void BitChoiceOptionShouldRespectIsEnabled(bool isEnabled, int count, string enabledClass)
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
            Assert.AreEqual(count, com.Instance.Count1);
        }

        [DataTestMethod, DataRow(true, 2, "enabled"),
         DataRow(false, 0, "disabled")]
        public void BitChoiceGroupShouldRespectIsEnabled(bool isEnabled, int count, string className)
        {
            var com = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.Value, count.ToString());
                });
            var bitChoiceGroup = com.Find(".bit-chg");
            var bitChoiceOptionInput = com.Find("input");
            bitChoiceOptionInput.Click();
            Assert.AreEqual(count, com.Instance.CurrentCount);
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-chg-{className}-fluent"));
        }
    }
}
