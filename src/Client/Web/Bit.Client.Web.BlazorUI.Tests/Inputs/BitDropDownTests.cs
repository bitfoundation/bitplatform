using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitDropDownTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, 1, true, "enabled"),
         DataRow(false, 0, false, "disabled")]
        public async Task BitDropDownShouldRespectIsEnabled(bool isEnabled, int count, bool isCalloutOpen, string className)
        {
            var com = RenderComponent<BitDropDownTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.Value, count.ToString());
                });
            var bitChoiceGroup = com.Find(".bit-drp");
            bitChoiceGroup.Children.First().Click();

            Assert.AreEqual(count, com.Instance.CurrentCount);
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-drp-{className}-fluent"));
            Assert.IsTrue(bitChoiceGroup.Children.Count().Equals(isCalloutOpen ? 3 : 2));
        }

        [DataTestMethod, DataRow(true, 2),
         DataRow(false, 1)]
        public async Task BitDropDownItemsShouldRespectIsEnabled(bool isEnabled, int count)
        {
            var com = RenderComponent<BitDropDownTest>(
                parameters =>
                {
                    parameters.Add(p => p.ItemIsEnabled, isEnabled);
                    parameters.Add(p => p.Value, count.ToString());
                });
            var bitChoiceGroup = com.Find(".bit-drp");
            bitChoiceGroup.Children.First().Click();
            bitChoiceGroup.Children[2].Children[1].Click();
            Assert.AreEqual(count, com.Instance.CurrentCount);
        }

        [DataTestMethod, DataRow(true, 2),
         DataRow(false, 1)]
        public async Task BitDropDownShouldRespectIsMultiSelect(bool isEnabled,  int count)
        {
            var com = RenderComponent<BitDropDownTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsMultiSelect, true);
                    parameters.Add(p => p.ItemIsEnabled, isEnabled);
                    parameters.Add(p => p.Value, count.ToString());
                });
            var bitChoiceGroup = com.Find(".bit-drp");
            bitChoiceGroup.Children.First().Click();
            bitChoiceGroup.Children[2].Children[1].Click();
            Assert.AreEqual(count, com.Instance.CurrentCount);
        }
    }
}
