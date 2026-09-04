using System;
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

    /// <summary>The CSS variables the component registers on its root, which all scale with the size.</summary>
    protected virtual string[] ScaledVariables => [];

    /// <summary>The size the drawing was authored at, which is what <c>Convert</c> scales away from.</summary>
    protected virtual int OriginalSize => 80;

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
        Assert.AreEqual(ChildCount, container.GetElementsByClassName(ChildClass).Length);
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
        DataRow(BitSide.Top, "bit-ldn-ltp"),
        DataRow(BitSide.Bottom, "bit-ldn-lbm"),
        DataRow(BitSide.Start, "bit-ldn-lst"),
        DataRow(BitSide.End, "bit-ldn-led")]
    public void ShouldRespectLabelPosition(BitSide? position, string expectedClass)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, position);
        });

        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains(expectedClass));
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
        DataRow(null, "var(--bit-clr-pri)")]
    public void ShouldHonorColor(BitColor? color, string expectedColor)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        StringAssert.Contains(StyleOf(component), $"--bit-ldn-color: {expectedColor}");
    }

    [TestMethod]
    public void ShouldHonorCustomColorWhenColorIsNotSet()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.CustomColor, "hotpink");
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-color: hotpink");
    }

    [TestMethod]
    public void ShouldPreferColorOverCustomColor()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.CustomColor, "hotpink");
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-color: var(--bit-clr-err)");
    }

    [TestMethod,
        DataRow(null, 64, "14"),
        DataRow(BitSize.Small, 40, "10"),
        DataRow(BitSize.Medium, 64, "14"),
        DataRow(BitSize.Large, 88, "18")]
    public void ShouldScaleEverythingWithTheSize(BitSize? size, int expectedSize, string expectedFontSize)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var style = StyleOf(component);

        StringAssert.Contains(style, $"--bit-ldn-size:{expectedSize}px");
        StringAssert.Contains(style, $"--bit-ldn-font-size:{expectedFontSize}px");

        foreach (var variable in ScaledVariables)
        {
            var suffix = variable[(variable.LastIndexOf('-') + 1)..];
            if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var basis) is false)
            {
                // A variable whose name does not carry its own basis, e.g. --bit-ldn-xbx-25f. Its presence
                // is all this test can claim, which the assertion below still covers.
                StringAssert.Contains(style, $"{variable}:");
                continue;
            }

            var expected = Math.Round(basis * (double)expectedSize / OriginalSize, 4).ToString(CultureInfo.InvariantCulture);
            StringAssert.Contains(style, $"{variable}:{expected}px");
        }
    }

    [TestMethod,
        DataRow(16, "3.5"),
        DataRow(24, "5.25"),
        DataRow(64, "14"),
        DataRow(100, "21.88"),
        DataRow(128, "28")]
    public void ShouldScaleTheLabelWithACustomSize(int customSize, string expectedFontSize)
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.CustomSize, customSize);
        });

        var style = StyleOf(component);

        StringAssert.Contains(style, $"--bit-ldn-size:{customSize}px");
        StringAssert.Contains(style, $"--bit-ldn-font-size:{expectedFontSize}px");
    }

    [TestMethod]
    public void ShouldPreferSizeOverCustomSize()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.CustomSize, 128);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-size:88px");
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

        StringAssert.Contains(StyleOf(component), $"--bit-ldn-mot-factor:calc(var(--bit-mot-loop-factor, 1) / {expected})");
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

        StringAssert.DoesNotMatch(StyleOf(component), new Regex("--bit-ldn-mot-factor"));
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
    public void ShouldRespectIsEnabled()
    {
        var component = RenderComponent<TLoading>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
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

        StringAssert.Contains(StyleOf(component), "--bit-ldn-color: var(--bit-clr-err)");
        StringAssert.Contains(StyleOf(component), "--bit-ldn-size:40px");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Success);
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.LabelPosition, BitSide.End);
        });

        StringAssert.Contains(StyleOf(component), "--bit-ldn-color: var(--bit-clr-suc)");
        StringAssert.Contains(StyleOf(component), "--bit-ldn-size:88px");
        Assert.IsTrue(component.Find(".bit-ldn").ClassList.Contains("bit-ldn-led"));
    }
}
