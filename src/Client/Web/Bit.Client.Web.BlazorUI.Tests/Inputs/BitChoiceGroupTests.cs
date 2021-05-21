using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
   public class BitChoiceGroupTests : BunitTestContext
   {
       [DataTestMethod, DataRow(true, 2, "enabled"),
        DataRow(false, 0, "disabled")]
       public async Task BitChoiceGroupShouldRespectIsEnabled(bool isEnabled, int count, string className)
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
