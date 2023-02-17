using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.NumericTextField;

[TestClass]
public class BitNumericTextFieldGeneralTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod,
     DataRow("The placeholder")]
    public void BitNumericTextFieldShouldHaveCorrectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitNumericTextField<byte>>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var numericTextFieldPlaceholder = component.Find(".input");

        Assert.IsTrue(numericTextFieldPlaceholder.HasAttribute("placeholder"));
        Assert.AreEqual(numericTextFieldPlaceholder.GetAttribute("placeholder"), placeholder);
    }
}
