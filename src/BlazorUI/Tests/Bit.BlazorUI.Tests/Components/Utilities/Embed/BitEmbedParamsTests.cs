using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Embed;

/// <summary>
/// The BitParams cascade of the embed: what a BitEmbedParams carries (what the embeds of an app agree on), that the
/// embed takes it, and that what is set on the embed itself wins over it.
/// </summary>
[TestClass]
public class BitEmbedParamsTests : BunitTestContext
{
    // What one embed frames and is called: its address, its title and the name of its placeholder.
    private static readonly string[] _notCascaded =
    [
        nameof(BitEmbed.CascadingParameters),
        nameof(BitEmbed.PlaceholderAriaLabel),
        nameof(BitEmbed.Src),
        nameof(BitEmbed.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitEmbedParams embedParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { embedParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitEmbed>(0);
                builder.AddComponentParameter(1, nameof(BitEmbed.Src), "https://example.com/embed");
                builder.AddComponentParameter(2, nameof(BitEmbed.Title), "Example");
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitEmbedParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitEmbed", BitEmbedParams.ParamName);
        Assert.AreEqual(BitEmbedParams.ParamName, new BitEmbedParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitEmbedParams());
    }

    [TestMethod]
    public void BitEmbedParamsShouldCarryEveryParameterButWhatOneEmbedFramesAndIsCalled()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitEmbed).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                         .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitEmbedParams).GetProperty(name), $"BitEmbedParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitEmbedParams).GetProperty(name), $"BitEmbedParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitEmbedShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitEmbedParams
        {
            Class = "cascaded-root",
            Allow = "autoplay",
            AllowFullScreen = true,
            AspectRatio = "4 / 3",
            Loading = BitImageLoading.Eager,
            MaxWidth = "640px",
            ReferrerPolicy = BitImageReferrerPolicy.NoReferrer,
            Sandbox = "allow-scripts",
            Classes = new() { Frame = "cascaded-frame" },
            Styles = new() { Frame = "color:red" },
        });

        var root = component.Find(".bit-emb");
        var frame = component.Find(".bit-emb-frm");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-emb-ar:4 / 3");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-emb-mxw:640px");
        Assert.AreEqual("autoplay", frame.GetAttribute("allow"));
        Assert.IsTrue(frame.HasAttribute("allowfullscreen"));
        Assert.AreEqual("eager", frame.GetAttribute("loading"));
        Assert.AreEqual("no-referrer", frame.GetAttribute("referrerpolicy"));
        Assert.AreEqual("allow-scripts", frame.GetAttribute("sandbox"));
        Assert.IsTrue(frame.ClassList.Contains("cascaded-frame"));
        Assert.AreEqual("color:red", frame.GetAttribute("style"));
    }

    [TestMethod]
    public void BitEmbedShouldTakeACascadedPlaceholderTemplate()
    {
        var component = RenderWithParams(new BitEmbedParams
        {
            PlaceholderTemplate = builder => builder.AddMarkupContent(0, "<span class=\"cascaded-consent\">Load external content</span>"),
        });

        Assert.IsNotNull(component.Find(".bit-emb-plh > .cascaded-consent"));
        Assert.AreEqual(0, component.FindAll("iframe").Count);
    }

    [TestMethod]
    public void BitEmbedShouldNotTakeAnEmptyCascadedSandbox()
    {
        // The cascade lets go of an empty string like any other, so one it wrote could never be put back: an empty
        // sandbox - every restriction - is set on the embed itself.
        var component = RenderWithParams(new BitEmbedParams { Sandbox = "" });

        Assert.IsFalse(component.Find(".bit-emb-frm").HasAttribute("sandbox"));
    }

    [TestMethod]
    public void BitEmbedShouldLetGoOfACascadedSandboxOnceTheCascadeStopsGivingIt()
    {
        var component = RenderWithParams(new BitEmbedParams { Sandbox = "allow-scripts" });

        Assert.AreEqual("allow-scripts", component.Find(".bit-emb-frm").GetAttribute("sandbox"));

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitEmbedParams() }));

        Assert.IsFalse(component.Find(".bit-emb-frm").HasAttribute("sandbox"));
    }

    [TestMethod]
    public void BitEmbedShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitEmbedParams { AspectRatio = "4 / 3", Loading = BitImageLoading.Eager }, builder =>
        {
            builder.AddComponentParameter(3, nameof(BitEmbed.AspectRatio), "1");
        });

        StringAssert.Contains(component.Find(".bit-emb").GetAttribute("style"), "--bit-emb-ar:1");
        Assert.AreEqual("eager", component.Find(".bit-emb-frm").GetAttribute("loading"), "the cascaded value is taken where the embed sets none");
    }
}
