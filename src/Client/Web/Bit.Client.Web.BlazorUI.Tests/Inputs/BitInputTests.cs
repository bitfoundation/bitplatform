using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Inputs;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitInputTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, true, 1, "enabled", TextFieldType.Text), DataRow(false, false, 0, "disabled", TextFieldType.Password)]
        public async Task BitTextFieldShouldRespectIsEnabled(bool isEnabled, bool isReadonly, int count, string className, TextFieldType type)
        {
            var com = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.InputType, type);
                    parameters.Add(p => p.IsReadOnly, isReadonly);
                });

            var bitTextField = com.Find(".bit-text-field-input");

            bitTextField.Click();
            bitTextField.Focus();
            bitTextField.FocusIn();
            bitTextField.FocusOut();
            bitTextField.Change(count);
            bitTextField.KeyDown(Key.Delete);
            bitTextField.KeyUp(Key.Delete);

            Assert.IsTrue(bitTextField.ClassList.Contains(className));
            Assert.AreEqual(count, com.Instance.CurrentCount);
            if (isEnabled)
                Assert.IsNotNull(com.Instance.Value);
        }
    }
}
