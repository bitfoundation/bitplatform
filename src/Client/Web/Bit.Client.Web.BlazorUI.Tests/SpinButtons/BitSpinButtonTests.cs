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
        [TestInitialize]
        public void SetupJsInteropMode()
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [DataTestMethod,
            DataRow("cm"),
            DataRow("Inch"),
            DataRow("foot")]
        public void SpinButtonShouldHaveSuffixWhenItsPropertySet(string suffix)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Suffix, suffix));

            var input = component.Find("input");
            var inputValue = input.GetAttribute("value");

            Assert.IsTrue(inputValue.Contains(suffix));
        }

        [DataTestMethod,
            DataRow(2, 4, 2),
            DataRow(20, 22, 20)]
        public void SpinButtonShouldRespectMaxValue(double max, double countOfClick, double expectedResult)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Max, max));

            var input = component.Find("input");
            var upButton = component.FindAll("button").First();
            for (int i = 0; i < countOfClick; i++)
            {
                upButton.Click();
            }
            var inputValue = double.Parse(input.GetAttribute("value"));

            Assert.AreEqual(inputValue, expectedResult);
        }

        [DataTestMethod,
            DataRow(0, 4, 0),
            DataRow(-2, 22, -2)]
        public void SpinButtonShouldRespectMinValue(double min, double countOfClick, double expectedResult)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.Min, min));

            var input = component.Find("input");
            var downButton = component.FindAll("button").Last();
            for (int i = 0; i < countOfClick; i++)
            {
                downButton.Click();
            }
            var inputValue = double.Parse(input.GetAttribute("value"));

            Assert.AreEqual(inputValue, expectedResult);
        }

        [DataTestMethod,
            DataRow(LabelPosition.left),
            DataRow(LabelPosition.top)]
        public void SpinButtonShouldHaveLabelPositionClassName(LabelPosition labelPosition)
        {
            var component = RenderComponent<BitSpinButtonTest>(parameters => parameters.Add(p => p.LabelPosition, labelPosition));

            var container = component.Find(".bit-spb");
            var hasLabelClass = container.ClassList
                .Any(className => className.Contains($"label-{labelPosition}"));
            Assert.IsTrue(hasLabelClass);
        }
    }
}
