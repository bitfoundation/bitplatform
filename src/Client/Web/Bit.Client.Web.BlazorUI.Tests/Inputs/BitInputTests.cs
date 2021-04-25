using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    [TestClass]
    public class BitInputTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, false, 7, "enabled", TextFieldType.Text),
         DataRow(false, false, 0, "disabled", TextFieldType.Text),
         DataRow(true, true, 7, "readonly", TextFieldType.Text),
         DataRow(true, false, 7, "enabled", TextFieldType.Password),
         DataRow(false, false, 0, "disabled", TextFieldType.Password),
         DataRow(true, true, 7, "readonly", TextFieldType.Password)]
        public async Task BitTextFieldShouldRespectIsEnabledAndReadonly(bool isEnabled, bool isReadonly, int count, string className, TextFieldType type)
        {
            var com = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.InputType, type);
                    parameters.Add(p => p.IsReadOnly, isReadonly);
                });

            var bitTextField = com.Find("input");
            bitTextField.Click();
            bitTextField.Focus();
            bitTextField.FocusIn();
            bitTextField.FocusOut();
            bitTextField.Change(count);
            bitTextField.KeyDown(Key.Delete);
            bitTextField.KeyUp(Key.Delete);
            Assert.IsTrue(bitTextField.ParentElement.ClassList.Contains(className));
            Assert.IsTrue(bitTextField.Attributes.Any(p => p.Name.Equals("readonly")).Equals(isReadonly));
            Assert.AreEqual(count, com.Instance.CurrentCount);
        }

        [DataTestMethod, DataRow(true, false, 7, "enabled", TextFieldType.Text),
         DataRow(false, false, 0, "disabled", TextFieldType.Text),
         DataRow(true, true, 7, "readonly", TextFieldType.Text),
         DataRow(true, false, 7, "enabled", TextFieldType.Password),
         DataRow(false, false, 0, "disabled", TextFieldType.Password),
         DataRow(true, true, 7, "readonly", TextFieldType.Password)]
        public async Task BitTextFieldShouldRespectMultiLine(bool isEnabled, bool isReadonly, int count, string className, TextFieldType type)
        {
            var com = RenderComponent<BitTextFieldTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsMultiLine, true);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.InputType, type);
                    parameters.Add(p => p.IsReadOnly, isReadonly);
                });
            var bitTextField = type == TextFieldType.Text ? com.Find("textarea") : com.Find("input");
            bitTextField.Click();
            bitTextField.Focus();
            bitTextField.FocusIn();
            bitTextField.FocusOut();
            bitTextField.Change(count);
            bitTextField.KeyDown(Key.Delete);
            bitTextField.KeyUp(Key.Delete);
            Assert.IsTrue(bitTextField.ParentElement.ClassList.Contains(className));
            Assert.IsTrue(bitTextField.Attributes.Any(p => p.Name.Equals("readonly")).Equals(isReadonly));
            Assert.AreEqual(count, com.Instance.CurrentCount);
        }

    }
}
