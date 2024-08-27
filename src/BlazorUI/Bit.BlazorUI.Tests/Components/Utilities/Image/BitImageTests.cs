using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

[TestClass]
public class BitImageTests : BunitTestContext
{
    [DataTestMethod]
    public void BitImageShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-non bit-img-lan bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitImageShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-img bit-img-fde{cssClass}"" id:ignore><img class=""bit-img-img bit-img-non bit-img-lan bit-img-vis"" /></div>");
    }

    [DataTestMethod]
    public void BitImageShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitImage>();

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-non bit-img-lan bit-img-vis"" /></div>");

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-img bit-img-fde bit-dis"" id:ignore><img class=""bit-img-img bit-img-non bit-img-lan bit-img-vis"" /></div>");
    }

    [DataTestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitImageShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitImage>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-img bit-img-fde"" id:ignore><img class=""bit-img-img bit-img-non bit-img-lan bit-img-vis"" /></div>");
    }
}
