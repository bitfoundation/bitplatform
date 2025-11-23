using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

[TestClass]
public class BitFlagTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitFlagShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-flg");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitFlagShouldRespectTitle()
    {
        const string title = "Netherlands Flag";

        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        var root = component.Find(".bit-flg");

        Assert.AreEqual(title, root.GetAttribute("title"));
    }

    [TestMethod]
    public void BitFlagShouldRenderFromCountry()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Netherlands);
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("flags/NL-flat-16.webp"));
    }

    [TestMethod]
    public void BitFlagShouldRenderFromIso2()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso2, "ca");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("flags/CA-flat-16.webp"));
    }

    [TestMethod]
    public void BitFlagShouldRenderFromIso3()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Iso3, "usa");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("flags/US-flat-16.webp"));
    }

    [TestMethod]
    public void BitFlagShouldRenderFromCode()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Code, "81"); // Japan
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("flags/JP-flat-16.webp"));
    }

    [TestMethod]
    public void BitFlagShouldRenderEmptyWhenCountryNotFound()
    {
        var component = RenderComponent<BitFlag>(parameters =>
        {
            parameters.Add(p => p.Code, "0000");
        });

        var style = component.Find(".bit-flg").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("background-image"));
    }
}
