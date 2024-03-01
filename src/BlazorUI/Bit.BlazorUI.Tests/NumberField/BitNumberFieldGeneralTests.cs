using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.NumberField;

[TestClass]
public class BitNumberFieldGeneralTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod, DataRow("The placeholder")]
    public void BitNumberFieldShouldHaveCorrectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var numericTextFieldPlaceholder = component.Find(".bit-nfl-inp");

        Assert.IsTrue(numericTextFieldPlaceholder.HasAttribute("placeholder"));
        Assert.AreEqual(numericTextFieldPlaceholder.GetAttribute("placeholder"), placeholder);
    }
}
