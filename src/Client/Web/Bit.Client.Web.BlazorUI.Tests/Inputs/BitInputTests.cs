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
            var bitChoiceGroup = com.Find(".bit-grp");
            var bitChoiceOptionInput = com.Find("input");
            bitChoiceOptionInput.Click();
            Assert.AreEqual(count, com.Instance.CurrentCount);
            Assert.IsTrue(bitChoiceGroup.ClassList.Contains($"bit-grp-{className}-fluent"));
        }
    }
}
