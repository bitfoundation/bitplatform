using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitChoiceGroupTests : BunitTestContext
    {
        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),

          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),

          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false)
      ]
        public void BitChoiceGroupShouldTakeCorrectVisual(Visual visual, bool isEnabled)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitChoiceGroup = component.Find(".bit-chg");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-chg-{isEnabledClass}-{visualClass}"));
        }

        [DataTestMethod, DataRow("testName")]
        public void BitChoiceGroupShouldGiveNameToChpiceOptions(string name)
        {
            var component = RenderComponent<BitChoiceGroupTest>(
                parameters =>
                {
                    parameters.Add(p => p.Name, name);
                });

            var bitChoiceOption = component.Find(".bit-chg .bit-cho");

            Assert.IsTrue(bitChoiceOption.FirstElementChild.HasAttribute("name"));
            Assert.AreEqual(name, bitChoiceOption.FirstElementChild.GetAttribute("name"));
        }
    }
}
