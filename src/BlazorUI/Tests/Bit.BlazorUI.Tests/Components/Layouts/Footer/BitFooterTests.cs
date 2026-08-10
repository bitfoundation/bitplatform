using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Footer;

[TestClass]
public class BitFooterTests : BunitTestContext
{
    [TestMethod]
    public void BitFooterShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitFooter>();

        component.MarkupMatches(@"
<footer class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    [DataRow("<div>Footer Content</div>")]
    [DataRow("I'm a Footer")]
    public void BitFooterShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches($@"
<footer class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
        {childContent}
    </div>
</footer>");
    }

    [TestMethod]
    [DataRow(40)]
    [DataRow(100)]
    public void BitFooterShouldRespectHeight(int height)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        component.MarkupMatches($@"
<footer style=""height:{height}px"" class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void BitFooterShouldAddTheSafeAreaInsetToTheHeightOfAPinnedFooter(bool @fixed, bool sticky)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
            parameters.Add(p => p.Fixed, @fixed);
            parameters.Add(p => p.Sticky, sticky);
        });

        // The pinned footer pads itself with the inset, so the asked for height has to grow by it or the
        // content of the footer would lose that much of its own room.
        Assert.AreEqual("height:calc(56px + env(safe-area-inset-bottom, 0px))",
                        component.Find(".bit-ftr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitFooterShouldSwitchTheHeightWhenTheFooterIsPinnedDynamically()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
        });

        Assert.AreEqual("height:56px", component.Find(".bit-ftr").GetAttribute("style"));

        component.Render(parameters => parameters.Add(p => p.Sticky, true));

        Assert.AreEqual("height:calc(56px + env(safe-area-inset-bottom, 0px))",
                        component.Find(".bit-ftr").GetAttribute("style"));
    }

    [TestMethod]
    [DataRow("1rem")]
    [DataRow("4px 8px")]
    public void BitFooterShouldRespectGap(string gap)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        component.MarkupMatches($@"
<footer style=""--bit-ftr-gap:{gap}"" class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    public void BitFooterShouldNotRenderTheGapVariableByDefault()
    {
        var component = RenderComponent<BitFooter>();

        var style = component.Find(".bit-ftr").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-ftr-gap"));
    }

    [TestMethod]
    public void BitFooterShouldRespectGapAndHeightTogether()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Height, 64);
            parameters.Add(p => p.Gap, "1rem");
        });

        var style = component.Find(".bit-ftr").GetAttribute("style")!;

        StringAssert.Contains(style, "height:64px");
        StringAssert.Contains(style, "--bit-ftr-gap:1rem");
    }

    [TestMethod]
    public void BitFooterShouldRespectGapChangedDynamically()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
        });

        StringAssert.Contains(component.Find(".bit-ftr").GetAttribute("style")!, "--bit-ftr-gap:1rem");

        component.Render(parameters => parameters.Add(p => p.Gap, "2rem"));

        StringAssert.Contains(component.Find(".bit-ftr").GetAttribute("style")!, "--bit-ftr-gap:2rem");
    }

    [TestMethod]
    public void BitFooterShouldRespectFixed()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
        });

        var footer = component.Find(".bit-ftr");

        Assert.IsTrue(footer.ClassList.Contains("bit-ftr-fix"));
        Assert.IsFalse(footer.ClassList.Contains("bit-ftr-stk"));
    }

    [TestMethod]
    public void BitFooterShouldRespectSticky()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Sticky, true);
        });

        var footer = component.Find(".bit-ftr");

        Assert.IsTrue(footer.ClassList.Contains("bit-ftr-stk"));
        Assert.IsFalse(footer.ClassList.Contains("bit-ftr-fix"));
    }

    [TestMethod]
    public void BitFooterShouldPreferFixedOverSticky()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.Sticky, true);
        });

        var footer = component.Find(".bit-ftr");

        Assert.IsTrue(footer.ClassList.Contains("bit-ftr-fix"));
        Assert.IsFalse(footer.ClassList.Contains("bit-ftr-stk"));
    }

    [TestMethod]
    public void BitFooterShouldRespectFixedChangedDynamically()
    {
        var component = RenderComponent<BitFooter>();

        Assert.IsFalse(component.Find(".bit-ftr").ClassList.Contains("bit-ftr-fix"));

        component.Render(parameters => parameters.Add(p => p.Fixed, true));

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains("bit-ftr-fix"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-ftr-pri")]
    [DataRow(BitColor.Secondary, "bit-ftr-sec")]
    [DataRow(BitColor.Tertiary, "bit-ftr-ter")]
    [DataRow(BitColor.Info, "bit-ftr-inf")]
    [DataRow(BitColor.Success, "bit-ftr-suc")]
    [DataRow(BitColor.Warning, "bit-ftr-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-ftr-swr")]
    [DataRow(BitColor.Error, "bit-ftr-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-ftr-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-ftr-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-ftr-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-ftr-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-ftr-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-ftr-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-ftr-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-ftr-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-ftr-tbr")]
    public void BitFooterShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFooterShouldNotRenderAnyColorClassByDefault()
    {
        var component = RenderComponent<BitFooter>();

        var classes = component.Find(".bit-ftr").ClassList;

        // Without a Color the footer keeps the plain page surface, so no role class is emitted.
        Assert.IsFalse(classes.Contains("bit-ftr-pri"));
        Assert.IsFalse(classes.Contains("bit-ftr-pbg"));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-ftr-fil")]
    [DataRow(BitVariant.Outline, "bit-ftr-otl")]
    [DataRow(BitVariant.Text, "bit-ftr-txt")]
    [DataRow(null, "bit-ftr-fil")]
    public void BitFooterShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-ftr-sm")]
    [DataRow(BitSize.Medium, "bit-ftr-md")]
    [DataRow(BitSize.Large, "bit-ftr-lg")]
    public void BitFooterShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFooterShouldNotRenderAnySizeClassByDefault()
    {
        var component = RenderComponent<BitFooter>();

        var classes = component.Find(".bit-ftr").ClassList;

        Assert.IsFalse(classes.Contains("bit-ftr-sm"));
        Assert.IsFalse(classes.Contains("bit-ftr-md"));
        Assert.IsFalse(classes.Contains("bit-ftr-lg"));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "bit-ftr-srt")]
    [DataRow(BitAlignment.End, "bit-ftr-end")]
    [DataRow(BitAlignment.Center, "bit-ftr-cnt")]
    [DataRow(BitAlignment.SpaceBetween, "bit-ftr-sbt")]
    [DataRow(BitAlignment.SpaceAround, "bit-ftr-sar")]
    [DataRow(BitAlignment.SpaceEvenly, "bit-ftr-sev")]
    [DataRow(BitAlignment.Baseline, "bit-ftr-bsl")]
    [DataRow(BitAlignment.Stretch, "bit-ftr-str")]
    public void BitFooterShouldRespectAlignment(BitAlignment alignment, string expectedClass)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
        });

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectBordered(bool bordered)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Bordered, bordered);
        });

        Assert.AreEqual(bordered, component.Find(".bit-ftr").ClassList.Contains("bit-ftr-brd"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectElevated(bool elevated)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Elevated, elevated);
        });

        Assert.AreEqual(elevated, component.Find(".bit-ftr").ClassList.Contains("bit-ftr-elv"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectTranslucent(bool translucent)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Translucent, translucent);
        });

        Assert.AreEqual(translucent, component.Find(".bit-ftr").ClassList.Contains("bit-ftr-trs"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectNoGutter(bool noGutter)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.NoGutter, noGutter);
        });

        Assert.AreEqual(noGutter, component.Find(".bit-ftr").ClassList.Contains("bit-ftr-ngt"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectReveal(bool reveal)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, reveal);
            parameters.Add(p => p.Fixed, true);
        });

        var footer = component.Find(".bit-ftr");

        Assert.AreEqual(reveal, footer.ClassList.Contains("bit-ftr-rvl"));
        // The footer starts revealed no matter what.
        Assert.IsFalse(footer.ClassList.Contains("bit-ftr-hdn"));
        Assert.IsTrue(component.Instance.IsRevealed);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void BitFooterShouldOnlySetTheRevealScriptUpForAPositionedFooter(bool reveal, bool positioned)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, reveal);
            parameters.Add(p => p.Sticky, positioned);
        });

        // A footer in the normal flow has nothing to slide over, so the scroll listener is only
        // attached for a Fixed or Sticky one.
        var setups = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Footers.setup");

        Assert.AreEqual((reveal && positioned) ? 1 : 0, setups);
    }

    [TestMethod]
    [DataRow(null, 0)]
    [DataRow(0, 0)]
    [DataRow(120, 120)]
    [DataRow(-5, 0)]
    public void BitFooterShouldRespectRevealOffset(int? revealOffset, int expectedOffset)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, revealOffset);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Footers.setup");

        Assert.AreEqual(component.Instance.UniqueId.ToString(), invocation.Arguments[0]);
        // A negative offset is clamped, so it can never keep the footer hidden at the top of the scroller.
        Assert.AreEqual(expectedOffset, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitFooterShouldSetTheRevealScriptUpAgainWhenTheRevealOffsetChanges()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, 50);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, 150);
        });

        var offsets = Context.JSInterop.Invocations
                             .Where(i => i.Identifier == "BitBlazorUI.Footers.setup")
                             .Select(i => i.Arguments[2])
                             .ToArray();

        CollectionAssert.AreEqual(new object[] { 50, 150 }, offsets);
    }

    [TestMethod]
    public void BitFooterShouldNotSetTheRevealScriptUpAgainOnAnUnrelatedRender()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, 50);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, 50);
            parameters.Add(p => p.Bordered, true);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Footers.setup"));
    }

    [TestMethod]
    public void BitFooterShouldDisposeTheRevealScriptWhenRevealIsTurnedOff()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Footers.dispose"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, false);
            parameters.Add(p => p.Fixed, true);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Footers.dispose"));
    }

    [TestMethod]
    public async Task BitFooterShouldToggleTheHiddenClassOnRevealChange()
    {
        var revealStates = new List<bool>();

        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.OnRevealChanged, (bool v) => revealStates.Add(v));
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains("bit-ftr-hdn"));
        Assert.IsFalse(component.Instance.IsRevealed);

        await component.InvokeAsync(() => component.Instance._OnRevealChange(false));

        Assert.IsFalse(component.Find(".bit-ftr").ClassList.Contains("bit-ftr-hdn"));
        Assert.IsTrue(component.Instance.IsRevealed);

        CollectionAssert.AreEqual(new[] { false, true }, revealStates);
    }

    [TestMethod]
    public async Task BitFooterShouldNotRaiseOnRevealChangedForTheSameState()
    {
        var callCount = 0;

        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.OnRevealChanged, (bool _) => callCount++);
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));
        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task BitFooterShouldNotStayHiddenWhenRevealIsTurnedOff()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        Assert.IsTrue(component.Find(".bit-ftr").ClassList.Contains("bit-ftr-hdn"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, false);
            parameters.Add(p => p.Fixed, true);
        });

        // The hidden state is cleared in OnAfterRenderAsync, after the interop call that detaches the
        // scroll listener, so the render that turns Reveal off is not the render that reveals the footer.
        component.WaitForAssertion(() =>
        {
            var footer = component.Find(".bit-ftr");

            Assert.IsFalse(footer.ClassList.Contains("bit-ftr-hdn"));
            Assert.IsFalse(footer.ClassList.Contains("bit-ftr-rvl"));
            Assert.IsTrue(component.Instance.IsRevealed);
        });
    }

    [TestMethod]
    [DataRow("margin: 10px;")]
    [DataRow(null)]
    public void BitFooterShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (string.IsNullOrEmpty(style) is false)
        {
            component.MarkupMatches($@"
<footer style=""{style}"" class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
        else
        {
            component.MarkupMatches(@"
<footer class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
    }

    [TestMethod]
    [DataRow("footer-class")]
    [DataRow(null)]
    public void BitFooterShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = !string.IsNullOrEmpty(@class) ? $" {@class}" : string.Empty;

        component.MarkupMatches($@"
<footer class=""bit-ftr bit-ftr-fil{cssClass}"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    public void BitFooterShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitFooterClassStyles { Root = "root-class", Container = "container-class" });
            parameters.Add(p => p.Styles, new BitFooterClassStyles { Root = "color:red", Container = "color:blue" });
        });

        var footer = component.Find(".bit-ftr");

        Assert.IsTrue(footer.ClassList.Contains("root-class"));
        Assert.IsTrue(footer.GetAttribute("style")!.Contains("color:red"));

        var container = component.Find(".bit-ftr-gut");

        Assert.IsTrue(container.ClassList.Contains("container-class"));
        Assert.IsTrue(container.GetAttribute("style")!.Contains("color:blue"));
    }

    [TestMethod]
    [DataRow("footer-id")]
    [DataRow(null)]
    public void BitFooterShouldRespectId(string id)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id ?? component.Instance.UniqueId.ToString();

        component.MarkupMatches($@"
<footer id=""{expectedId}"" class=""bit-ftr bit-ftr-fil"">
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    [DataRow("Site footer")]
    [DataRow(null)]
    public void BitFooterShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        Assert.AreEqual(ariaLabel, component.Find(".bit-ftr").GetAttribute("aria-label"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-ftr").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, null)]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitFooterShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-ftr").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.AreEqual(string.Empty, style);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl)]
    [DataRow(BitDir.Ltr)]
    [DataRow(BitDir.Auto)]
    [DataRow(null)]
    public void BitFooterShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"
<footer dir=""{dir.Value.ToString().ToLower()}"" class=""bit-ftr bit-ftr-fil{cssClass}"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
        else
        {
            component.MarkupMatches(@"
<footer class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitFooterShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        var cssClass = forceAnimation ? " bit-fam" : string.Empty;
        component.MarkupMatches($@"
<footer class=""bit-ftr bit-ftr-fil{cssClass}"" id:ignore>
    <div class=""bit-ftr-gut"">
    </div>
</footer>");
    }

    [TestMethod]
    public void BitFooterShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitFooterHtmlAttributesTest>();

        component.MarkupMatches(@"
<footer data-val-test=""bit"" class=""bit-ftr bit-ftr-fil"" id:ignore>
    <div class=""bit-ftr-gut"">
        I'm a footer
    </div>
</footer>");
    }

    [TestMethod]
    public void BitFooterShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitFooterCascadingParamsTest>();

        var footers = component.FindAll(".bit-ftr");

        // The first footer takes everything from the cascading parameters.
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-suc"));
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-otl"));
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-lg"));
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-brd"));
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-stk"));
        Assert.IsTrue(footers[0].ClassList.Contains("bit-ftr-rvl"));
        StringAssert.Contains(footers[0].GetAttribute("style")!, "--bit-ftr-gap:1rem");

        // The second one sets its own color and gap, which the cascading parameters must not overwrite.
        Assert.IsTrue(footers[1].ClassList.Contains("bit-ftr-err"));
        Assert.IsFalse(footers[1].ClassList.Contains("bit-ftr-suc"));
        Assert.IsTrue(footers[1].ClassList.Contains("bit-ftr-otl"));
        StringAssert.Contains(footers[1].GetAttribute("style")!, "--bit-ftr-gap:2rem");

        // The cascaded RevealOffset reaches the script of both footers.
        var offsets = Context.JSInterop.Invocations
                             .Where(i => i.Identifier == "BitBlazorUI.Footers.setup")
                             .Select(i => i.Arguments[2])
                             .ToArray();

        CollectionAssert.AreEqual(new object[] { 80, 80 }, offsets);
    }
}
