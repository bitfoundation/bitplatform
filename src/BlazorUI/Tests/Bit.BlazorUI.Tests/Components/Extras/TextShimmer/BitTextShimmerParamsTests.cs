using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextShimmer;

/// <summary>
/// Covers the BitParams cascade of the TextShimmer: what a BitTextShimmerParams fills in, what it leaves alone
/// because the shimmer wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitTextShimmerParamsTests : BunitTestContext
{
    // What belongs to a single shimmer rather than to a group of them: its text, its content and the length that
    // scales the band to that content.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTextShimmer.CascadingParameters),
        nameof(BitTextShimmer.ChildContent),
        nameof(BitTextShimmer.ContentLength),
        nameof(BitTextShimmer.Text),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitTextShimmerParams shimmerParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { shimmerParams });
            parameters.AddChildContent(builder => BuildShimmer(builder, extraAttributes));
        });
    }

    private static void BuildShimmer(RenderTreeBuilder builder, Action<RenderTreeBuilder>? extraAttributes)
    {
        builder.OpenComponent<BitTextShimmer>(0);
        builder.AddAttribute(1, nameof(BitTextShimmer.Text), "12345");
        extraAttributes?.Invoke(builder);
        builder.CloseComponent();
    }

    [TestMethod]
    public void BitTextShimmerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitTextShimmer", BitTextShimmerParams.ParamName);
        Assert.AreEqual(BitTextShimmerParams.ParamName, new BitTextShimmerParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitTextShimmerParams());
    }

    [TestMethod]
    public void BitTextShimmerParamsShouldCarryEveryParameterThatBelongsToAGroupOfShimmers()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitTextShimmer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .ToList();

        Assert.IsNotEmpty(parameters);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTextShimmerParams).GetProperty(name), $"BitTextShimmerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTextShimmerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitTextShimmerParams
        {
            Class = "cascaded",
            Alternate = true,
            Angle = 20,
            BaseColor = "gray",
            Color = BitColor.Primary,
            Delay = 100,
            Duration = 1000,
            Element = "span",
            GradientColor = "white",
            Iterations = 2,
            PauseOnHover = true,
            RepeatDelay = 500,
            Reversed = true,
            Spread = 3,
        });

        var root = component.Find(".bit-tsh");
        var style = root.GetAttribute("style")!;

        Assert.AreEqual("SPAN", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-alt"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-pri"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-poh"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-rev"));
        StringAssert.Contains(style, "--bit-tsh-angle:20deg");
        StringAssert.Contains(style, "--bit-tsh-base-clr:gray");
        StringAssert.Contains(style, "--bit-tsh-delay:100ms");
        StringAssert.Contains(style, "--bit-tsh-duration:1000ms");
        StringAssert.Contains(style, "--bit-tsh-gradient-clr:white");
        StringAssert.Contains(style, "--bit-tsh-iterations:2");
        StringAssert.Contains(style, "--bit-tsh-repeat-delay:500ms");
        StringAssert.Contains(style, "--bit-tsh-spread:15px");
    }

    // A cascaded Spread was asked for, so it wins over --bit-TextShimmer-spread even when it equals the default, and
    // the computed spread goes back to being a fallback once the cascade stops supplying it.
    [TestMethod]
    public void BitTextShimmerShouldPublishACascadedDefaultSpreadOverThePublicVariable()
    {
        var component = RenderWithParams(new BitTextShimmerParams { Spread = 2 });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:10px");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTextShimmerParams() });
            parameters.AddChildContent(builder => BuildShimmer(builder, null));
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-auto-spread:10px");
    }

    [TestMethod]
    public void BitTextShimmerShouldTakeTheCascadedSpreadLength()
    {
        var component = RenderWithParams(new BitTextShimmerParams { SpreadLength = "3em" });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:3em");
    }

    // A cascaded SpreadLength is a default for the region, so it gives way to a Spread the shimmer sets for itself -
    // and takes over again once the shimmer stops setting it.
    [TestMethod]
    public void BitTextShimmerShouldKeepItsOwnSpreadOverACascadedSpreadLength()
    {
        var shimmerParams = new BitTextShimmerParams { SpreadLength = "3em" };

        var component = RenderWithParams(shimmerParams, builder => builder.AddAttribute(2, nameof(BitTextShimmer.Spread), 5d));

        var style = component.Find(".bit-tsh").GetAttribute("style")!;
        StringAssert.Contains(style, "--bit-tsh-spread:25px");
        Assert.IsFalse(style.Contains("3em"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { shimmerParams });
            parameters.AddChildContent(builder => BuildShimmer(builder, null));
        });

        StringAssert.Contains(component.Find(".bit-tsh").GetAttribute("style"), "--bit-tsh-spread:3em");
    }

    // A cascaded GradientColor gives way to a Color role the shimmer asks for itself.
    [TestMethod]
    public void BitTextShimmerShouldKeepItsOwnColorOverACascadedGradientColor()
    {
        var component = RenderWithParams(new BitTextShimmerParams { GradientColor = "white" },
                                         builder => builder.AddAttribute(2, nameof(BitTextShimmer.Color), BitColor.Error));

        var root = component.Find(".bit-tsh");

        Assert.IsTrue(root.ClassList.Contains("bit-tsh-err"));
        Assert.IsFalse(root.GetAttribute("style")!.Contains("--bit-tsh-gradient-clr"));
    }

    // A value of the shimmer's own wins over a cascaded rival of it, but not over one it sets itself.
    [TestMethod]
    public void BitTextShimmerShouldKeepItsOwnSpreadLengthAndGradientColorOverItsOwnSpreadAndColor()
    {
        var component = RenderWithParams(new BitTextShimmerParams(), builder =>
        {
            builder.AddAttribute(2, nameof(BitTextShimmer.Spread), 5d);
            builder.AddAttribute(3, nameof(BitTextShimmer.SpreadLength), "3em");
            builder.AddAttribute(4, nameof(BitTextShimmer.Color), BitColor.Error);
            builder.AddAttribute(5, nameof(BitTextShimmer.GradientColor), "white");
        });

        var style = component.Find(".bit-tsh").GetAttribute("style")!;

        StringAssert.Contains(style, "--bit-tsh-spread:3em");
        StringAssert.Contains(style, "--bit-tsh-gradient-clr:white");
    }

    // A NaN is ignored by the shimmer, and it must not reset the style on every render for never equaling itself.
    [TestMethod]
    public void BitTextShimmerShouldNotReassignACascadedNaNOnEveryRender()
    {
        var shimmerParams = new BitTextShimmerParams { Angle = double.NaN, Spread = double.NaN };
        var shimmer = new BitTextShimmer();
        var builds = 0;
        shimmer.StyleBuilder.Register(() => (++builds).ToString());

        shimmerParams.UpdateParameters(shimmer);
        _ = shimmer.StyleBuilder.Value;

        shimmerParams.UpdateParameters(shimmer);
        _ = shimmer.StyleBuilder.Value;

        Assert.AreEqual(1, builds);
        Assert.IsTrue(double.IsNaN(shimmer.Angle!.Value));
        Assert.IsTrue(double.IsNaN(shimmer.Spread!.Value));
    }

    [TestMethod]
    public void BitTextShimmerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTextShimmerParams
        {
            Class = "cascaded",
            Duration = 1000,
            Element = "span",
            Reversed = true,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitTextShimmer.Duration), 3000);
            builder.AddAttribute(4, nameof(BitTextShimmer.Element), "h2");
            builder.AddAttribute(5, nameof(BitTextShimmer.Reversed), false);
        });

        var root = component.Find(".bit-tsh");

        Assert.AreEqual("H2", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("bit-tsh-rev"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-tsh-duration:3000ms");
    }

    // A "pause animations" switch bound to one params object reaches every shimmer under it, and turning it off
    // again sets them all moving.
    [TestMethod]
    public void BitTextShimmerShouldFollowACascadedPausedAndStatic()
    {
        var shimmerParams = new BitTextShimmerParams { Paused = true, Static = true };

        var component = RenderWithParams(shimmerParams);

        var root = component.Find(".bit-tsh");
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-pau"));
        Assert.IsTrue(root.ClassList.Contains("bit-tsh-sta"));

        shimmerParams.Paused = false;
        shimmerParams.Static = false;
        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { shimmerParams });
            parameters.AddChildContent(builder => BuildShimmer(builder, null));
        });

        root = component.Find(".bit-tsh");
        Assert.IsFalse(root.ClassList.Contains("bit-tsh-pau"));
        Assert.IsFalse(root.ClassList.Contains("bit-tsh-sta"));
    }

    [TestMethod]
    public void BitTextShimmerShouldDropTheCascadedValuesWithTheCascade()
    {
        var component = RenderWithParams(new BitTextShimmerParams
        {
            Duration = 1000,
            Element = "span",
            Reversed = true,
        });

        Assert.AreEqual("SPAN", component.Find(".bit-tsh").TagName);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.AddChildContent(builder => BuildShimmer(builder, null));
        });

        var shimmer = component.FindComponent<BitTextShimmer>().Instance;
        var root = component.Find(".bit-tsh");

        Assert.IsNull(shimmer.Duration);
        Assert.IsNull(shimmer.Element);
        Assert.IsFalse(shimmer.Reversed);
        Assert.AreEqual("P", root.TagName);
        Assert.IsFalse(root.ClassList.Contains("bit-tsh-rev"));
        Assert.IsFalse(root.GetAttribute("style")!.Contains("--bit-tsh-duration"));
    }
}
