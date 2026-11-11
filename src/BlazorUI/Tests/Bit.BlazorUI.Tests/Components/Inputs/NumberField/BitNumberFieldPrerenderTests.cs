using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

/// <summary>
/// What a number field sends before the page is interactive: the after-render pass that registers its key
/// listeners through JS never runs on a prerender, and nothing the render itself does may reach for JS either.
/// </summary>
[TestClass]
public class BitNumberFieldPrerenderTests
{
    [TestMethod]
    public async Task BitNumberFieldShouldPrerenderItsValueAndItsButtonsWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitNumberField<int>>(new Dictionary<string, object?>
        {
            [nameof(BitNumberField<int>.DefaultValue)] = 42,
            [nameof(BitNumberField<int>.Mode)] = BitSpinButtonMode.Compact,
            [nameof(BitNumberField<int>.Label)] = "Quantity",
            [nameof(BitNumberField<int>.Min)] = "1",
            [nameof(BitNumberField<int>.Max)] = "99",
        });

        Assert.Contains("bit-nfl", html);
        Assert.Contains("role=\"spinbutton\"", html);
        Assert.Contains("value=\"42\"", html);
        Assert.Contains("aria-valuenow=\"42\"", html);
        Assert.Contains("aria-valuemin=\"1\"", html);
        Assert.Contains("aria-valuemax=\"99\"", html);
        Assert.Contains("Quantity", html);
        Assert.Contains("bit-nfl-aup", html);
        Assert.Contains("bit-nfl-adn", html);
    }

    [TestMethod]
    public async Task BitNumberFieldShouldPrerenderAFormattedValueInTheGivenCulture()
    {
        var html = await Prerenderer.RenderAsync<BitNumberField<double>>(new Dictionary<string, object?>
        {
            [nameof(BitNumberField<double>.DefaultValue)] = 1234.5,
            [nameof(BitNumberField<double>.NumberFormat)] = "N1",
            [nameof(BitNumberField<double>.Culture)] = System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
        });

        Assert.Contains("value=\"1.234,5\"", html);
    }
}
