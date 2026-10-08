using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

/// <summary>
/// The behaviour every loading component inherits from <see cref="BitLoadingBase"/>.
/// </summary>
/// <remarks>
/// The eighteen loaders differ only in the shape they draw: one root element, one animation container
/// and a fixed number of child elements, all of them driven by the same parameters. Running the whole
/// suite against every one of them - rather than a copy of a handful of tests per component - is what
/// catches a variant that quietly stops registering a CSS variable, drops the live region or renders a
/// container the base can no longer reach.
/// </remarks>
public abstract class BitLoadingTestsBase<TLoading> : BunitTestContext where TLoading : BitLoadingBase
{
    /// <summary>The root class of the component under test, e.g. "bit-ldn-bar".</summary>
    protected abstract string RootClass { get; }

    /// <summary>How many child elements the animation container holds. Zero for the loaders drawn purely with pseudo-elements.</summary>
    protected abstract int ChildCount { get; }

    private string ContainerClass => $"{RootClass}-ccn";

    private string ChildClass => $"{RootClass}-chl";



    private static string StyleOf(IRenderedComponent<TLoading> component)
    {
        return component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty;
    }



    [TestMethod]
    public void ShouldRenderStructure()
    {
        var component = RenderComponent<TLoading>();

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains(RootClass));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-ltp"));

        var container = component.Find($".{ContainerClass}");
        Assert.IsTrue(container.ClassList.Contains("bit-ldn-ccn"));
        Assert.AreEqual(ChildCount, container.GetElementsByClassName(ChildClass).Length);
        Assert.AreEqual(ChildCount, container.GetElementsByClassName("bit-ldn-chl").Length);
    }

    [TestMethod]
    public void ShouldRenderNothingButSpans()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        // A div is not allowed where an inline loader goes - in a paragraph, a button, a label - and the HTML
        // parser closes an open paragraph at one, so prerendered markup would break the sentence apart.
        var root = component.Find(".bit-ldn");
        Assert.AreEqual("SPAN", root.TagName);
        Assert.IsTrue(root.QuerySelectorAll("*").All(e => e.TagName == "SPAN"));
    }

    [TestMethod]
    public void ShouldHideTheDrawingFromAssistiveTechnology()
    {
        var component = RenderComponent<TLoading>();

        // The geometry of the animation carries no information, so a screen reader must not walk it.
        Assert.AreEqual("true", component.Find($".{ContainerClass}").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void ShouldRenderALiveRegionWithFallbackTextByDefault()
    {
        var component = RenderComponent<TLoading>();

        var root = component.Find(".bit-ldn");
        Assert.AreEqual("status", root.GetAttribute("role"));
        Assert.AreEqual("polite", root.GetAttribute("aria-live"));
        Assert.IsNull(root.GetAttribute("aria-label"));

        Assert.AreEqual("Loading", component.Find(".bit-ldn-srt").TextContent.Trim());
    }

    [TestMethod]
    public void ShouldAnnounceTheAriaLabelWhenThereIsNoVisibleLabel()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Fetching your orders");
        });

        Assert.AreEqual("Fetching your orders", component.Find(".bit-ldn-srt").TextContent.Trim());

        // The same text is never handed to a screen reader twice.
        Assert.IsNull(component.Find(".bit-ldn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ShouldRenderLabel()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
        });

        Assert.AreEqual("Loading...", component.Find(".bit-ldn-lbl").TextContent.Trim());

        // A visible label is what the live region announces, so the hidden fallback stands down.
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
    }

    [TestMethod]
    public void ShouldKeepTheAriaLabelAsTheNameOfALabelledLoading()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
            parameters.Add(p => p.AriaLabel, "Fetching your orders");
        });

        Assert.AreEqual("Fetching your orders", component.Find(".bit-ldn").GetAttribute("aria-label"));
        Assert.AreEqual("Loading...", component.Find(".bit-ldn-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void ShouldRenderLabelTemplate()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        Assert.AreEqual("tmpl", component.Find(".tmpl").TextContent);
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
    }

    [TestMethod]
    public void ShouldPreferTheLabelTemplateOverTheLabel()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"tmpl\">tmpl</span>")));
        });

        Assert.AreEqual("tmpl", component.Find(".tmpl").TextContent);
        Assert.HasCount(0, component.FindAll(".bit-ldn-lbl"));
    }

    [TestMethod,
        DataRow(null, "bit-ldn-ltp"),
        DataRow(BitPlacement.Top, "bit-ldn-ltp"),
        DataRow(BitPlacement.Bottom, "bit-ldn-lbm"),
        DataRow(BitPlacement.Start, "bit-ldn-lst"),
        DataRow(BitPlacement.End, "bit-ldn-led")]
    public void ShouldRespectLabelPosition(BitPlacement? position, string expectedClass)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPlacement, position);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(null, "bit-ldn-led"),
        DataRow(BitPlacement.Top, "bit-ldn-ltp"),
        DataRow(BitPlacement.Start, "bit-ldn-lst")]
    public void ShouldKeepTheLabelOfAnInlineLoadingOnItsLine(BitPlacement? position, string expectedClass)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.LabelPlacement, position);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
        Assert.AreEqual(1, root.ClassList.Count(c => c is "bit-ldn-ltp" or "bit-ldn-lbm" or "bit-ldn-lst" or "bit-ldn-led"));
    }

    [TestMethod]
    public void ShouldRespectRoleAndAriaLive()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Role, "progressbar");
            parameters.Add(p => p.AriaLive, "assertive");
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual("progressbar", root.GetAttribute("role"));
        Assert.AreEqual("assertive", root.GetAttribute("aria-live"));
    }

    [TestMethod,
        DataRow(null, null, "Loading"),
        DataRow("Exporting", null, "Exporting"),
        DataRow("Exporting", "Exporting your report", "Exporting your report"),
        DataRow(null, "Exporting your report", "Exporting your report")]
    public void ShouldNameAProgressBarOnItsRoot(string? label, string? ariaLabel, string expectedName)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Role, "progressbar");
            parameters.Add(p => p.Label, label);
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        // The children of a progressbar are presentational, so neither the label nor the hidden text inside it
        // is ever read, and the role requires a name: the root has to carry it.
        Assert.AreEqual(expectedName, component.Find(".bit-ldn").GetAttribute("aria-label"));
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
    }

    [TestMethod,
        DataRow("status", "polite"),
        DataRow("progressbar", null),
        DataRow("alert", null),
        DataRow("log", null)]
    public void ShouldOnlyDefaultTheStatusRoleToAPoliteLiveRegion(string role, string? expected)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Role, role);
        });

        // Any other role keeps its own politeness: a polite written onto an alert would quieten it.
        Assert.AreEqual(expected, component.Find(".bit-ldn").GetAttribute("aria-live"));
    }

    [TestMethod]
    public void ShouldNameAProgressBarWithALabelTemplateByTheAriaLabelOrTheFallbackText()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Role, "progressbar");
            parameters.Add(p => p.Label, "Exporting");
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<b>Exporting</b>")));
        });

        // The template has no text to hand over, so the role's required name is the fallback text.
        Assert.AreEqual("Loading", component.Find(".bit-ldn").GetAttribute("aria-label"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Exporting your report");
        });

        Assert.AreEqual("Exporting your report", component.Find(".bit-ldn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ShouldLeaveTheNameOfAProgressBarWithALabelTemplateToAPassedThroughAriaLabelledBy()
    {
        RenderFragment template = b => b.AddMarkupContent(0, "<b>Exporting</b>");

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, nameof(BitLoadingBase.Role), "progressbar");
            builder.AddAttribute(2, nameof(BitLoadingBase.LabelTemplate), template);
            builder.AddAttribute(3, "aria-labelledby", "export-heading");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ldn");

        Assert.AreEqual("export-heading", root.GetAttribute("aria-labelledby"));
        Assert.IsNull(root.GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow("none"),
        DataRow("presentation")]
    public void ShouldAnnounceNothingWhenDecorative(string role)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Role, role);
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual(role, root.GetAttribute("role"));

        // A decorative loader draws a wait that its surroundings already report, so it adds nothing of
        // its own to the accessibility tree - not the hidden fallback text, and not a live region, which
        // aria-live would make of the element whatever its role.
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
        Assert.IsNull(root.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void ShouldAnnounceNothingWhenDecorativeEvenWithAnExplicitAriaLive()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, nameof(BitLoadingBase.Role), "none");
            builder.AddAttribute(2, nameof(BitLoadingBase.AriaLive), "assertive");
            builder.AddAttribute(3, "aria-live", "polite");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual("none", root.GetAttribute("role"));

        // A politeness and a decorative role contradict each other, and the role is the one that says what
        // the loader is for - so neither the parameter nor the passed-through attribute revives the live
        // region here, exactly as neither revives the hidden fallback text.
        Assert.IsNull(root.GetAttribute("aria-live"));
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
    }

    [TestMethod]
    public void ShouldLetAPassedThroughRoleAndAriaLiveWin()
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, "role", "alert");
            builder.AddAttribute(2, "aria-live", "off");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual("alert", root.GetAttribute("role"));
        Assert.AreEqual("off", root.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void ShouldLetAPassedThroughAriaLabelNameTheLiveRegionInsteadOfTheFallbackText()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, "aria-label", "Fetching your orders");
            builder.CloseComponent();
        });

        Assert.AreEqual("Fetching your orders", component.Find(".bit-ldn").GetAttribute("aria-label"));

        // The accessible name of the live region is what is read there, so the hidden text underneath it
        // would never be reached - and rendering it anyway is what used to hand a screen reader both.
        Assert.HasCount(0, component.FindAll(".bit-ldn-srt"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "var(--bit-clr-pri)"),
        DataRow(BitColor.Secondary, "var(--bit-clr-sec)"),
        DataRow(BitColor.Tertiary, "var(--bit-clr-ter)"),
        DataRow(BitColor.Info, "var(--bit-clr-inf)"),
        DataRow(BitColor.Success, "var(--bit-clr-suc)"),
        DataRow(BitColor.Warning, "var(--bit-clr-wrn)"),
        DataRow(BitColor.SevereWarning, "var(--bit-clr-swr)"),
        DataRow(BitColor.Error, "var(--bit-clr-err)"),
        DataRow(BitColor.PrimaryBackground, "var(--bit-clr-bg-pri)"),
        DataRow(BitColor.SecondaryBackground, "var(--bit-clr-bg-sec)"),
        DataRow(BitColor.TertiaryBackground, "var(--bit-clr-bg-ter)"),
        DataRow(BitColor.PrimaryForeground, "var(--bit-clr-fg-pri)"),
        DataRow(BitColor.SecondaryForeground, "var(--bit-clr-fg-sec)"),
        DataRow(BitColor.TertiaryForeground, "var(--bit-clr-fg-ter)"),
        DataRow(BitColor.PrimaryBorder, "var(--bit-clr-brd-pri)"),
        DataRow(BitColor.SecondaryBorder, "var(--bit-clr-brd-sec)"),
        DataRow(BitColor.TertiaryBorder, "var(--bit-clr-brd-ter)"),
        DataRow(null, null)]
    public void ShouldHonorColor(BitColor? color, string? expectedColor)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        if (expectedColor is null)
        {
            // Left to the stylesheet, which falls back to the primary color behind --bit-Loading-color.
            StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-clr"));
        }
        else
        {
            StringAssert.Contains(StyleOf(component), $"--bit-ldn-clr:{expectedColor}");
        }
    }

    [TestMethod]
    public void ShouldHonorCustomColorWhenColorIsNotSet()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.CustomColor, "hotpink");
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-clr:hotpink");
    }

    [TestMethod]
    public void ShouldPreferColorOverCustomColor()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.CustomColor, "hotpink");
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-clr:var(--bit-clr-err)");
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-ldn-sm"),
        DataRow(BitSize.Medium, "bit-ldn-md"),
        DataRow(BitSize.Large, "bit-ldn-lg")]
    public void ShouldSizeTheLoadingWithAClass(BitSize size, string expectedClass)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
        Assert.AreEqual(1, root.ClassList.Count(c => c is "bit-ldn-sm" or "bit-ldn-md" or "bit-ldn-lg"));

        // The size lives in the stylesheet, ahead of --bit-Loading-size, and the whole drawing is laid out in
        // CSS from it - nothing about the geometry is written into the style attribute.
        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-sz|--bit-ldn-size|px"));
    }

    [TestMethod]
    public void ShouldPublishNoSizeWhileItIsUnset()
    {
        var component = RenderComponent<TLoading>();

        // An unset Size publishes nothing - neither a class nor a style - so the public --bit-Loading-* variables
        // restyle the 64px default while an explicit value, which does publish its class, wins over them.
        var root = component.Find(".bit-ldn");
        Assert.IsFalse(root.ClassList.Any(c => c is "bit-ldn-sm" or "bit-ldn-md" or "bit-ldn-lg" or "bit-ldn-em" or "bit-ldn-csz"));
        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-"));
    }

    [TestMethod,
        DataRow(16),
        DataRow(100),
        DataRow(128)]
    public void ShouldSizeTheLoadingWithACustomSize(int customSize)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.CustomSize, customSize);
        });

        var root = component.Find(".bit-ldn");
        StringAssert.Contains(StyleOf(component), $"--bit-ldn-sz:{customSize}px");

        // No size class, so the label scales from the custom size instead of taking a step of the ramp - through a
        // class of its own, which makes that label size a choice the public variable no longer wins over.
        Assert.IsFalse(root.ClassList.Any(c => c is "bit-ldn-sm" or "bit-ldn-md" or "bit-ldn-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-csz"));
    }

    [TestMethod,
        DataRow(0),
        DataRow(-8)]
    public void ShouldIgnoreAnUnusableCustomSize(int customSize)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.CustomSize, customSize);
        });

        // Left as unset as if it had never been given: no size class, no custom size class, no inline size.
        Assert.IsFalse(component.Find(".bit-ldn").ClassList.Any(c => c is "bit-ldn-sm" or "bit-ldn-md" or "bit-ldn-lg" or "bit-ldn-csz"));
        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-sz"));
    }

    [TestMethod]
    public void ShouldPreferSizeOverCustomSize()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.CustomSize, 128);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-lg"));
        Assert.IsFalse(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-csz"));
        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-sz"));
    }

    [TestMethod,
        DataRow(2d, "2"),
        DataRow(0.5d, "0.5"),
        DataRow(4d, "4")]
    public void ShouldHonorSpeed(double speed, string expected)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Speed, speed);
        });

        StringAssert.Contains(StyleOf(component), $"--bit-ldn-spd:{expected}");
    }

    [TestMethod,
        DataRow(null),
        DataRow(0d),
        DataRow(-1d)]
    public void ShouldIgnoreAnUnusableSpeed(double? speed)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Speed, speed);
        });

        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-spd|--bit-ldn-mot-factor"));
    }

    [TestMethod,
        DataRow(1, "1"),
        DataRow(6, "6"),
        DataRow(12, "12")]
    public void ShouldHonorThickness(int thickness, string expected)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Thickness, thickness);
        });

        StringAssert.Contains(StyleOf(component), $"--bit-ldn-stroke:{expected}px");
    }

    [TestMethod]
    public void ShouldNotScaleTheThicknessWithTheSize()
    {
        // A literal number of pixels, so that a hairline stays a hairline whatever the loader is sized at.
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Thickness, 6);
            parameters.Add(p => p.Size, BitSize.Small);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-stroke:6px");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Thickness, 6);
            parameters.Add(p => p.Size, BitSize.Large);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-stroke:6px");
    }

    [TestMethod,
        DataRow(null),
        DataRow(0),
        DataRow(-1)]
    public void ShouldIgnoreAnUnusableThickness(int? thickness)
    {
        // Left unset rather than zeroed, so every stroke falls back to the width it was authored at.
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Thickness, thickness);
        });

        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-stroke"));
    }

    [TestMethod]
    public void ShouldRespectPaused()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Paused, true);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-pau"));

        // The drawing is held, never removed: the component keeps its structure and its live region.
        Assert.HasCount(1, component.FindAll($".{ContainerClass}"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Paused, false);
        });

        Assert.IsFalse(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-pau"));
    }

    [TestMethod]
    public void ShouldRespectInline()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-inl"));

        var withoutInline = RenderComponent<TLoading>();
        Assert.IsFalse(withoutInline.Find(".bit-ldn").ClassList.Contains("bit-ldn-inl"));
    }

    [TestMethod]
    public void ShouldSizeAnUnsizedInlineLoadingWithItsText()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
        });

        // The bit-ldn-em class draws it at 1em rather than the 64px of the medium default - a default of its own,
        // which the public variables still restyle.
        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-em"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-md"));
        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-sz"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
        });

        root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-em"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, null);
            parameters.Add(p => p.CustomSize, 20);
        });

        // Without the 1em class, so the label follows the custom size the way it does on any other loader.
        StringAssert.Contains(StyleOf(component), "--bit-ldn-sz:20px");
        Assert.IsFalse(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-em"));
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-csz"));
    }

    [TestMethod]
    public void ShouldHoldTheContentBackUntilTheDelayElapses()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Delay, 100);
        });

        // The root stays mounted through the window as an empty live region - text landing in a region
        // that is already in the document is what a screen reader reliably announces - and only the
        // content waits, so nothing can still flash up for work that turned out to be quick.
        var root = component.Find(".bit-ldn");
        Assert.AreEqual(0, root.ChildElementCount);
        Assert.AreEqual("status", root.GetAttribute("role"));
        Assert.AreEqual("polite", root.GetAttribute("aria-live"));

        component.WaitForAssertion(() => Assert.AreNotEqual(0, component.Find(".bit-ldn").ChildElementCount), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void ShouldRenderImmediatelyWithoutADelay()
    {
        var component = RenderComponent<TLoading>();

        Assert.HasCount(1, component.FindAll(".bit-ldn"));
    }

    [TestMethod]
    public void ShouldOpenTheDelayWindowAgainWhenTheDelayChanges()
    {
        var component = RenderComponent<TLoading>();

        Assert.HasCount(1, component.FindAll(".bit-ldn"));

        // A loader kept in the document across several waits is held back for each of them, rather than
        // being stuck with whatever delay it happened to be created with.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Delay, 100);
        });

        Assert.AreEqual(0, component.Find(".bit-ldn").ChildElementCount);

        component.WaitForAssertion(() => Assert.AreNotEqual(0, component.Find(".bit-ldn").ChildElementCount), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void ShouldLetTheComponentThroughWhenTheDelayIsTakenAway()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Delay, 10_000);
        });

        Assert.AreEqual(0, component.Find(".bit-ldn").ChildElementCount);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Delay, 0);
        });

        Assert.AreNotEqual(0, component.Find(".bit-ldn").ChildElementCount);
    }

    [TestMethod]
    public void ShouldNotShowTheLoadingOfAnAbandonedDelayWindow()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Delay, 50);
        });

        // The first window is cancelled by the second, so the one that elapses is the one in effect.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Delay, 10_000);
        });

        Thread.Sleep(250);

        Assert.AreEqual(0, component.Find(".bit-ldn").ChildElementCount);
    }

    [TestMethod,
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void ShouldRespectDir(BitDir dir, string expected)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void ShouldRespectForceAnimation()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, true);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void ShouldRespectDisabled()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-dis"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void ShouldRespectVisibility(BitVisibility visibility, string expected)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = StyleOf(component);

        if (expected.Length == 0)
        {
            StringAssert.DoesNotMatch(style, new Regex("visibility:hidden|display:none"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void ShouldRespectRootStyleAndClass()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Style, "margin:4px;");
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "margin:4px");
    }

    [TestMethod]
    public void ShouldRespectId()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Id, "the-loading");
        });

        Assert.AreEqual("the-loading", component.Find(".bit-ldn").GetAttribute("id"));
    }

    [TestMethod]
    public void ShouldFallBackToTheUniqueIdForTheRootId()
    {
        var component = RenderComponent<TLoading>();

        Assert.AreEqual(component.Instance.UniqueId, component.Find(".bit-ldn").GetAttribute("id"));
    }

    [TestMethod]
    public void ShouldSplatHtmlAttributes()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, "data-test", "loading");
            builder.AddAttribute(2, "title", "Please wait");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ldn");
        Assert.AreEqual("loading", root.GetAttribute("data-test"));
        Assert.AreEqual("Please wait", root.GetAttribute("title"));
    }

    [TestMethod]
    public void ShouldApplyClassesToEveryPart()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
            parameters.Add(p => p.Classes, new BitLoadingClassStyles
            {
                Root = "custom-root",
                Container = "custom-container",
                Child = "custom-child",
                Label = "custom-label"
            });
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find($".{ContainerClass}").ClassList.Contains("custom-container"));
        Assert.IsTrue(component.Find(".bit-ldn-lbl").ClassList.Contains("custom-label"));

        if (ChildCount > 0)
        {
            Assert.HasCount(ChildCount, component.FindAll(".custom-child"));
        }
    }

    [TestMethod]
    public void ShouldApplyStylesToEveryPart()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Label, "Loading...");
            parameters.Add(p => p.Styles, new BitLoadingClassStyles
            {
                Root = "opacity:0.5",
                Container = "outline:1px solid red",
                Child = "border-radius:0",
                Label = "color:tomato"
            });
        });

        StringAssert.Contains(StyleOf(component), "opacity:0.5");
        StringAssert.Contains(component.Find($".{ContainerClass}").GetAttribute("style") ?? string.Empty, "outline:1px solid red");
        StringAssert.Contains(component.Find(".bit-ldn-lbl").GetAttribute("style") ?? string.Empty, "color:tomato");

        if (ChildCount > 0)
        {
            var child = component.Find($".{ChildClass}");
            StringAssert.Contains(child.GetAttribute("style") ?? string.Empty, "border-radius:0");
        }
    }

    [TestMethod]
    public void ShouldApplyClassesAndStylesToTheScreenReaderText()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitLoadingClassStyles { ScreenReaderText = "custom-srt" });
            parameters.Add(p => p.Styles, new BitLoadingClassStyles { ScreenReaderText = "letter-spacing:1px" });
        });

        var text = component.Find(".bit-ldn-srt");
        Assert.IsTrue(text.ClassList.Contains("custom-srt"));
        StringAssert.Contains(text.GetAttribute("style") ?? string.Empty, "letter-spacing:1px");
    }

    [TestMethod]
    public void ShouldNotLeakParametersAsHtmlAttributes()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitLoadingClassStyles { Root = "custom-root" });
            parameters.Add(p => p.Styles, new BitLoadingClassStyles { Root = "opacity:0.5" });
            parameters.Add(p => p.Speed, 2d);
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Delay, 0);
            parameters.Add(p => p.Paused, true);
            parameters.Add(p => p.Thickness, 6);
            parameters.Add(p => p.Role, "status");
            parameters.Add(p => p.AriaLive, "polite");
        });

        // Every parameter the base handles has to be taken out of the splat; the ones that were not used
        // to end up on the root as stray attributes such as classes="Bit.BlazorUI.BitLoadingClassStyles".
        var stray = component.Find(".bit-ldn").Attributes
                             .Select(a => a.Name.ToLowerInvariant())
                             .Where(name => name is "classes" or "styles" or "speed" or "inline" or "delay" or "paused" or "thickness" or "arialive")
                             .ToArray();

        Assert.HasCount(0, stray);
    }

    [TestMethod]
    public void ShouldRerenderWhenParametersChange()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-clr:var(--bit-clr-err)");
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-sm"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Success);
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.LabelPlacement, BitPlacement.End);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-clr:var(--bit-clr-suc)");
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-lg"));
        Assert.IsFalse(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-sm"));
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-led"));
    }

    [TestMethod]
    public void ShouldDimAndHoldADisabledLoading()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        // The stylesheet dims bit-dis and holds its animation the way it holds bit-ldn-pau; the drawing stays.
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-dis"));
        Assert.HasCount(1, component.FindAll($".{ContainerClass}"));
    }

    [TestMethod]
    public void ShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitLoadingParams
                {
                    Color = BitColor.Error,
                    Size = BitSize.Large,
                    Label = "Cascaded",
                    LabelPlacement = BitPlacement.End,
                    Inline = true,
                    Paused = true,
                    Speed = 2,
                    Thickness = 3,
                    Role = "progressbar",
                    AriaLive = "assertive",
                    Classes = new() { Root = "cascaded-root", Label = "cascaded-label" },
                    Styles = new() { Root = "margin:3px" }
                }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-ldn");
        var style = root.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-clr:var(--bit-clr-err)");
        StringAssert.Contains(style, "--bit-ldn-spd:2");
        StringAssert.Contains(style, "--bit-ldn-stroke:3px");
        StringAssert.Contains(style, "margin:3px");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-led"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-inl"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-pau"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.AreEqual("progressbar", root.GetAttribute("role"));
        Assert.AreEqual("assertive", root.GetAttribute("aria-live"));

        var label = component.Find(".bit-ldn-lbl");
        Assert.AreEqual("Cascaded", label.TextContent.Trim());
        Assert.IsTrue(label.ClassList.Contains("cascaded-label"));

        // The cascade is not a parameter of the root element, so it must not leak onto it as an attribute.
        Assert.IsNull(root.GetAttribute("cascadingparameters"));
    }

    [TestMethod]
    public void ShouldLetDirectParametersWinOverCascadingParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitLoadingParams { Color = BitColor.Error, Size = BitSize.Large, Label = "Cascaded", Paused = true }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.AddAttribute(1, nameof(BitLoadingBase.Color), (BitColor?)BitColor.Success);
                builder.AddAttribute(2, nameof(BitLoadingBase.Size), (BitSize?)BitSize.Small);
                builder.AddAttribute(3, nameof(BitLoadingBase.Label), "Own");
                builder.AddAttribute(4, nameof(BitLoadingBase.Paused), false);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-ldn");

        StringAssert.Contains(root.GetAttribute("style") ?? string.Empty, "--bit-ldn-clr:var(--bit-clr-suc)");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-pau"));
        Assert.AreEqual("Own", component.Find(".bit-ldn-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void ShouldHoldTheContentBackForACascadedDelay()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Delay = 100 } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.CloseComponent();
            });
        });

        // The cascade is applied before the delay window is opened, so it holds the content back like a Delay
        // written on the loader itself.
        Assert.AreEqual(0, component.Find(".bit-ldn").ChildElementCount);

        component.WaitForAssertion(() => Assert.AreNotEqual(0, component.Find(".bit-ldn").ChildElementCount), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void ShouldApplyACascadedCustomSizeAndCustomColor()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { CustomSize = 24, CustomColor = "currentColor" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-ldn");
        var style = root.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-sz:24px");
        StringAssert.Contains(style, "--bit-ldn-clr:currentColor");
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-md"));
    }

    [TestMethod]
    public void ShouldLetTheLoadingsOwnCustomSizeAndCustomColorWinOverACascadedSizeAndColor()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Size = BitSize.Large, Color = BitColor.Error } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.AddAttribute(1, nameof(BitLoadingBase.CustomSize), (int?)24);
                builder.AddAttribute(2, nameof(BitLoadingBase.CustomColor), "hotpink");
                builder.CloseComponent();
            });
        });

        // Size outranks CustomSize and Color outranks CustomColor, so applying the cascade here would override what
        // was written on the loader itself.
        var root = component.Find(".bit-ldn");
        var style = root.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-ldn-sz:24px");
        StringAssert.Contains(style, "--bit-ldn-clr:hotpink");
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-lg"));
    }

    [TestMethod]
    public void ShouldDropWhatACascadeNoLongerSupplies()
    {
        // The loader is only handed its parameters again when something written on it changes, so each pass
        // re-renders it with a new AriaLabel to put the cascade, as it then stands, in front of it.
        static RenderFragment Loader(string ariaLabel) => builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, nameof(BitLoadingBase.AriaLabel), ariaLabel);
            builder.AddAttribute(2, nameof(BitLoadingBase.Paused), false);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitLoadingParams { Color = BitColor.Error, Size = BitSize.Large, Label = "Cascaded", Paused = true }
            });
            parameters.Add(p => p.ChildContent, Loader("first"));
        });

        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-lg"));
        Assert.AreEqual("Cascaded", component.Find(".bit-ldn-lbl").TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Size = BitSize.Small } });
            parameters.Add(p => p.ChildContent, Loader("second"));
        });

        // A value the cascade no longer carries goes back to the default instead of staying on the loader.
        root = component.Find(".bit-ldn");
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-ldn-clr"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-pau"));
        Assert.IsEmpty(component.FindAll(".bit-ldn-lbl"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.Add(p => p.ChildContent, Loader("third"));
        });

        // And so does everything once the cascade is gone altogether - back to an unset Size, which publishes no class.
        root = component.Find(".bit-ldn");
        Assert.IsFalse(root.ClassList.Any(c => c is "bit-ldn-sm" or "bit-ldn-md" or "bit-ldn-lg"));
    }

    [TestMethod]
    public void ShouldPutACascadedValueBackToWhatTheLoadingHeldBeforeIt()
    {
        static RenderFragment Loader(string ariaLabel, bool writeSpeed) => builder =>
        {
            builder.OpenComponent<TLoading>(0);
            builder.AddAttribute(1, nameof(BitLoadingBase.AriaLabel), ariaLabel);
            if (writeSpeed) builder.AddAttribute(2, nameof(BitLoadingBase.Speed), (double?)3);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams() });
            parameters.Add(p => p.ChildContent, Loader("first", writeSpeed: true));
        });

        Assert.IsTrue((component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty).Contains("--bit-ldn-spd:3"));

        // A parameter that stops being written keeps its last value, as it does on any Blazor component, and that
        // value - not a default - is what the loader holds when a cascade fills the parameter in.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Speed = 2 } });
            parameters.Add(p => p.ChildContent, Loader("second", writeSpeed: false));
        });

        Assert.IsTrue((component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty).Contains("--bit-ldn-spd:2"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams() });
            parameters.Add(p => p.ChildContent, Loader("third", writeSpeed: false));
        });

        Assert.IsTrue((component.Find(".bit-ldn").GetAttribute("style") ?? string.Empty).Contains("--bit-ldn-spd:3"));
    }

    [TestMethod]
    public void ShouldKeepTheLoadingsOwnInlineAgainstACascadedSize()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Size = BitSize.Large, CustomSize = 48 } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.AddAttribute(1, nameof(BitLoadingBase.Inline), true);
                builder.CloseComponent();
                builder.OpenComponent<TLoading>(2);
                builder.CloseComponent();
            });
        });

        // Inline is a sizing choice of the loader's own - the size of its text - so the cascade leaves it alone,
        // while a loader under the same cascade that made no such choice still takes the cascaded size.
        var roots = component.FindAll(".bit-ldn");
        Assert.IsTrue(roots[0].ClassList.Contains("bit-ldn-em"));
        Assert.IsFalse(roots[0].ClassList.Contains("bit-ldn-lg"));
        Assert.IsFalse((roots[0].GetAttribute("style") ?? string.Empty).Contains("--bit-ldn-sz"));
        Assert.IsTrue(roots[1].ClassList.Contains("bit-ldn-lg"));
    }

    [TestMethod]
    public void ShouldSizeALoadingMadeInlineByACascade()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLoadingParams { Inline = true, Size = BitSize.Small } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<TLoading>(0);
                builder.CloseComponent();
            });
        });

        // Both come from the same cascade, whose author asked for the pair, so the size applies.
        var root = component.Find(".bit-ldn");
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-inl"));
        Assert.IsTrue(root.ClassList.Contains("bit-ldn-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-ldn-em"));
    }
}
