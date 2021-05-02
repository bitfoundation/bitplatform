using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.SpinButtons
{
    [TestClass]
    public class BitSpinButtonTests : BunitTestContext
    {
        [DataTestMethod, DataRow(1.5), DataRow(55)]
        public void SpinButton_DefaultValue_MeetValue(double defaultValue)
        {
            //Context.JSInterop.SetupVoid("Bit.setProperty", refrence , "value", "1.5");
            //Context.JSInterop.SetupVoid("Bit.setProperty");
            var component = RenderComponent<BitSpinButtonTest>();
            //Context.JSInterop.SetupVoid("Bit.setProperty", component.Instance.InputElement, "value", "1.5 ");
            var input = component.Find("input");

            var inputPlaceholder = input.GetAttribute("value");

            Assert.AreEqual(defaultValue, inputPlaceholder);
        }
    }
}
