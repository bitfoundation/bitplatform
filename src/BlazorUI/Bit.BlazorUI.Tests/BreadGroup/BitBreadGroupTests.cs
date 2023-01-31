using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.BreadGroup;

[TestClass]
public class BitBreadGroupTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true),
        DataRow(Visual.Fluent, false),
        DataRow(Visual.Cupertino, true),
        DataRow(Visual.Cupertino, false),
        DataRow(Visual.Material, true),
        DataRow(Visual.Material, false),
    ]
    public void BitBreadGroupTest(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitBreadGroupTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Visual, visual);
        });

        var breadList = component.Find(".bit-brg");

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(breadList.ClassList.Contains($"bit-brg-{visualClass}"));

        if (isEnabled)
        {
            Assert.IsTrue(breadList.ClassList.Contains($"bit-brg-enabled-{visualClass}"));
        }
        else
        {
            Assert.IsTrue(breadList.ClassList.Contains($"bit-brg-disabled-{visualClass}"));
        }
    }

    [DataTestMethod,
        DataRow((uint)1),
        DataRow((uint)2),
        DataRow((uint)3),
        DataRow((uint)4),
        DataRow((uint)9),
    ]
    public void BitBreadGroupShouldRespectMaxDisplayedOptions(uint maxDisplayedOptions)
    {
        var component = RenderComponent<BitBreadGroupTest>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedOptions, maxDisplayedOptions);
        });

        var breadGroupOptions = component.FindAll(".bit-brg .options-wrapper ul li");

        if (maxDisplayedOptions < 5)
        {
            Assert.AreEqual((uint)breadGroupOptions.Count, maxDisplayedOptions + 1);
        }
        else
        {
            Assert.AreEqual(breadGroupOptions.Count, 5);
        }
    }

    [DataTestMethod,
        DataRow((uint)3, (uint)0),
        DataRow((uint)3, (uint)1),
        DataRow((uint)3, (uint)3),
        DataRow((uint)3, (uint)4),
    ]
    public void BitBreadGroupShouldRespectOverflowIndex(uint maxDisplayedOptions, uint overflowIndex)
    {
        var component = RenderComponent<BitBreadGroupTest>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedOptions, maxDisplayedOptions);
            parameters.Add(p => p.OverflowIndex, overflowIndex);
        });

        var breadGroupOptions = component.FindAll(".bit-brg .options-wrapper ul li");

        if (overflowIndex < maxDisplayedOptions)
        {
            Assert.IsTrue(breadGroupOptions[(int)overflowIndex].InnerHtml.Contains("span"));
        }
        else if (overflowIndex >= maxDisplayedOptions)
        {
            Assert.IsTrue(breadGroupOptions[0].InnerHtml.Contains("span"));
        }
    }

    [DataTestMethod]
    public void BitBreadGroupShouldRespectOptionIsEnabled()
    {
        var component = RenderComponent<BitBreadGroupTest>();

        var breadGroupOptions = component.FindAll(".bit-brg .options-wrapper ul li");

        Assert.IsTrue(breadGroupOptions[1].ClassList.Contains("bit-bro-disabled-fluent"));
    }

    [DataTestMethod]
    public void BitBreadGroupShouldRespectIsSelected()
    {
        var component = RenderComponent<BitBreadGroupTest>();

        var breadGroupOptions = component.FindAll(".bit-brg .options-wrapper ul li a");

        Assert.IsTrue(breadGroupOptions[2].GetAttribute("aria-current").Contains("page"));
        Assert.IsTrue(breadGroupOptions[2].ClassList.Contains("selected-option"));
    }

    [DataTestMethod]
    public void BitBreadGroupShouldRespectOptionsClassAndStyle()
    {
        var component = RenderComponent<BitBreadGroupTest>();

        var breadGroupOptions = component.FindAll(".bit-brg .options-wrapper ul li");

        Assert.IsTrue(breadGroupOptions[3].ClassList.Contains("custom-class-name"));
        Assert.IsTrue(breadGroupOptions[4].GetAttribute("style").Contains("background-color: red;"));
    }
}
