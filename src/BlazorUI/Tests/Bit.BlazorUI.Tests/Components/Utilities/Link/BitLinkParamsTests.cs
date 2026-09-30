using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Link;

/// <summary>
/// Pins the BitParams infrastructure of the link: every value parameter is on the params object, each one travels
/// down the cascade to a link that left it unset, and none of them overrides a value the link set for itself.
/// </summary>
[TestClass]
public class BitLinkParamsTests : BunitTestContext
{
    // Content and callbacks are the link's own: a RenderFragment or an EventCallback shared by every link of a
    // subtree is one link written many times over.
    private static readonly string[] NotCascaded = [nameof(BitLink.ChildContent), nameof(BitLink.OnClick), nameof(BitLink.CascadingParameters)];

    [TestMethod]
    public void BitLinkParamsShouldCarryEveryValueParameterOfTheLink()
    {
        var linkParameters = typeof(BitLink).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                            .Select(p => p.Name)
                                            .Except(NotCascaded)
                                            .ToArray();

        var paramsProperties = typeof(BitLinkParams).GetProperties().Select(p => p.Name).ToHashSet();

        foreach (var name in linkParameters)
        {
            Assert.IsTrue(paramsProperties.Contains(name), $"BitLinkParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitLinkShouldTakeEveryCascadedValueItWasNotGiven()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Params, new BitLinkParams
            {
                Href = "https://bitplatform.dev",
                Title = "bit platform",
                AriaDescription = "The home page",
                AriaCurrent = BitNavAriaCurrent.Page,
                AutoFocus = true,
                Download = "",
                IconName = "Link",
                IconPosition = BitIconPosition.End,
                PreventDefault = true,
                AllowDisabledFocus = true,
                StopPropagation = true,
            });
        });

        var anchor = component.Find(".bit-lnk");
        var link = component.FindComponent<BitLink>().Instance;

        // The href arriving from the cascade is what turns the button into an anchor.
        Assert.AreEqual("A", anchor.TagName);
        Assert.AreEqual("https://bitplatform.dev", anchor.GetAttribute("href"));
        Assert.AreEqual("bit platform", anchor.GetAttribute("title"));
        Assert.AreEqual("page", anchor.GetAttribute("aria-current"));
        Assert.IsTrue(anchor.HasAttribute("autofocus"));

        // An empty download is a value of its own - keep the server's file name - rather than an unset one.
        Assert.AreEqual("", anchor.GetAttribute("download"));

        Assert.AreEqual("The home page", component.Find(".bit-lnk-dsc").TextContent);
        Assert.AreEqual(component.Find(".bit-lnk-dsc").Id, anchor.GetAttribute("aria-describedby"));

        Assert.AreEqual(1, component.FindAll(".bit-lnk-icn.bit-lnk-eic").Count);

        Assert.IsTrue(link.PreventDefault);
        Assert.IsTrue(link.StopPropagation);
        Assert.IsTrue(link.AllowDisabledFocus);
    }

    [TestMethod]
    public void BitLinkShouldTakeACascadedIconInfo()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Params, new BitLinkParams { Icon = BitIconInfo.Css("my-icon") });
        });

        Assert.IsTrue(component.Find(".bit-lnk-icn").ClassList.Contains("my-icon"));
    }

    [TestMethod]
    public void BitLinkShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Params, new BitLinkParams
            {
                Href = "https://bitplatform.dev",
                Title = "cascaded title",
                Download = "cascaded.svg",
                IconName = "Link",
                PreventDefault = true,
            });
            parameters.Add(p => p.Overrides, new Dictionary<string, object>
            {
                [nameof(BitLink.Href)] = "https://github.com/bitfoundation/bitplatform",
                [nameof(BitLink.Title)] = "own title",
                [nameof(BitLink.Download)] = "own.svg",
                [nameof(BitLink.IconName)] = "Home",
                [nameof(BitLink.PreventDefault)] = false,
            });
        });

        var anchor = component.Find(".bit-lnk");

        Assert.AreEqual("https://github.com/bitfoundation/bitplatform", anchor.GetAttribute("href"));
        Assert.AreEqual("own title", anchor.GetAttribute("title"));
        Assert.AreEqual("own.svg", anchor.GetAttribute("download"));
        Assert.IsTrue(component.Find(".bit-lnk-icn").ClassList.Contains("bit-icon--Home"));
        Assert.IsFalse(component.FindComponent<BitLink>().Instance.PreventDefault);
    }

    [TestMethod]
    public void BitLinkShouldRenderAButtonWhenNeitherItNorTheCascadeHasAnHref()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Params, new BitLinkParams { Underlined = true });
        });

        var root = component.Find(".bit-lnk");

        Assert.AreEqual("BUTTON", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-lnk-und"));
    }
}
