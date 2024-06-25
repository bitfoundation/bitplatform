using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Labels;

[TestClass]
public class BitLabelTests : BunitTestContext
{
    [DataTestMethod, DataRow(true, true), DataRow(false, false)]
    public void BitLabelShouldRespectIsRequired(bool isRequired, bool expectedResult)
    {
        var component = RenderComponent<BitLabelTest>(parameters => parameters.Add(p => p.IsRequired, isRequired));
        var bitLabel = component.Find(".bit-lbl");
        Assert.AreEqual(expectedResult, bitLabel.ClassList.Contains("bit-lbl-req"));
    }


    [DataTestMethod,
    DataRow(true),
    DataRow(false)
]
    public void BitLabelShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });
    }
}
