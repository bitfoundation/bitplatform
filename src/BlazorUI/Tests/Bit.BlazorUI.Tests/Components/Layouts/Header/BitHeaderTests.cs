using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

[TestClass]
public class BitHeaderTests : BunitTestContext
{
    [TestMethod]
    public void BitHeaderShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitHeader>();

        component.MarkupMatches(@"
<header class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    [DataRow("<div>Title</div>")]
    [DataRow("I'm a Header")]
    public void BitHeaderShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches($@"
<header class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
        {childContent}
    </div>
</header>");
    }

    [TestMethod]
    [DataRow(50)]
    [DataRow(120)]
    public void BitHeaderShouldRespectHeight(int height)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        component.MarkupMatches($@"
<header style=""height:{height}px"" class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void BitHeaderShouldAddTheSafeAreaInsetToTheHeightOfAPinnedHeader(bool @fixed, bool sticky)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
            parameters.Add(p => p.Fixed, @fixed);
            parameters.Add(p => p.Sticky, sticky);
        });

        // The pinned header pads itself with the inset, so the asked for height has to grow by it or the
        // content of the header would lose that much of its own room.
        Assert.AreEqual("height:calc(56px + env(safe-area-inset-top, 0px))",
                        component.Find(".bit-hdr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitHeaderShouldSwitchTheHeightWhenTheHeaderIsPinnedDynamically()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
        });

        Assert.AreEqual("height:56px", component.Find(".bit-hdr").GetAttribute("style"));

        component.Render(parameters => parameters.Add(p => p.Sticky, true));

        Assert.AreEqual("height:calc(56px + env(safe-area-inset-top, 0px))",
                        component.Find(".bit-hdr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitHeaderShouldNotAddTheSafeAreaInsetToTheHeightOfAnAbsoluteHeader()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
            parameters.Add(p => p.Absolute, true);
        });

        // An absolute header is pinned to its container, not to the top of the screen, so there is no
        // device inset above it to make room for.
        Assert.AreEqual("height:56px", component.Find(".bit-hdr").GetAttribute("style"));
    }

    [TestMethod]
    [DataRow("1rem")]
    [DataRow("4px 8px")]
    public void BitHeaderShouldRespectGap(string gap)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        component.MarkupMatches($@"
<header style=""--bit-hdr-gap:{gap}"" class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderTheGapVariableByDefault()
    {
        var component = RenderComponent<BitHeader>();

        var style = component.Find(".bit-hdr").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-hdr-gap"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectGapAndHeightTogether()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 64);
            parameters.Add(p => p.Gap, "1rem");
        });

        var style = component.Find(".bit-hdr").GetAttribute("style")!;

        StringAssert.Contains(style, "height:64px");
        StringAssert.Contains(style, "--bit-hdr-gap:1rem");
    }

    [TestMethod]
    public void BitHeaderShouldRespectGapChangedDynamically()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
        });

        StringAssert.Contains(component.Find(".bit-hdr").GetAttribute("style")!, "--bit-hdr-gap:1rem");

        component.Render(parameters => parameters.Add(p => p.Gap, "2rem"));

        StringAssert.Contains(component.Find(".bit-hdr").GetAttribute("style")!, "--bit-hdr-gap:2rem");
    }

    [TestMethod]
    public void BitHeaderShouldRespectFixed()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
        });

        var header = component.Find(".bit-hdr");

        Assert.IsTrue(header.ClassList.Contains("bit-hdr-fix"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-stk"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectSticky()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Sticky, true);
        });

        var header = component.Find(".bit-hdr");

        Assert.IsTrue(header.ClassList.Contains("bit-hdr-stk"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-fix"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectAbsolute()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Absolute, true);
        });

        var header = component.Find(".bit-hdr");

        Assert.IsTrue(header.ClassList.Contains("bit-hdr-abs"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-fix"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-stk"));
    }

    [TestMethod]
    // An element has one position, so the widest reach wins: Fixed, then Absolute, then Sticky.
    [DataRow(true, true, true, "bit-hdr-fix")]
    [DataRow(true, true, false, "bit-hdr-fix")]
    [DataRow(true, false, true, "bit-hdr-fix")]
    [DataRow(false, true, true, "bit-hdr-abs")]
    public void BitHeaderShouldRespectThePrecedenceOfThePositions(bool @fixed, bool absolute, bool sticky, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Fixed, @fixed);
            parameters.Add(p => p.Absolute, absolute);
            parameters.Add(p => p.Sticky, sticky);
        });

        var classes = component.Find(".bit-hdr").ClassList;

        Assert.IsTrue(classes.Contains(expectedClass));

        foreach (var other in new[] { "bit-hdr-fix", "bit-hdr-abs", "bit-hdr-stk" }.Where(c => c != expectedClass))
        {
            Assert.IsFalse(classes.Contains(other));
        }
    }

    [TestMethod]
    public void BitHeaderShouldRespectFixedChangedDynamically()
    {
        var component = RenderComponent<BitHeader>();

        Assert.IsFalse(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-fix"));

        component.Render(parameters => parameters.Add(p => p.Fixed, true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-fix"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-hdr-pri")]
    [DataRow(BitColor.Secondary, "bit-hdr-sec")]
    [DataRow(BitColor.Tertiary, "bit-hdr-ter")]
    [DataRow(BitColor.Info, "bit-hdr-inf")]
    [DataRow(BitColor.Success, "bit-hdr-suc")]
    [DataRow(BitColor.Warning, "bit-hdr-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-hdr-swr")]
    [DataRow(BitColor.Error, "bit-hdr-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-hdr-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-hdr-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-hdr-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-hdr-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-hdr-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-hdr-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-hdr-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-hdr-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-hdr-tbr")]
    public void BitHeaderShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderAnyColorClassByDefault()
    {
        var component = RenderComponent<BitHeader>();

        var classes = component.Find(".bit-hdr").ClassList;

        // Without a Color the header keeps the plain page surface, so no role class is emitted.
        Assert.IsFalse(classes.Contains("bit-hdr-pri"));
        Assert.IsFalse(classes.Contains("bit-hdr-pbg"));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-hdr-fil")]
    [DataRow(BitVariant.Outline, "bit-hdr-otl")]
    [DataRow(BitVariant.Text, "bit-hdr-txt")]
    [DataRow(null, "bit-hdr-fil")]
    public void BitHeaderShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-hdr-sm")]
    [DataRow(BitSize.Medium, "bit-hdr-md")]
    [DataRow(BitSize.Large, "bit-hdr-lg")]
    public void BitHeaderShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderAnySizeClassByDefault()
    {
        var component = RenderComponent<BitHeader>();

        var classes = component.Find(".bit-hdr").ClassList;

        Assert.IsFalse(classes.Contains("bit-hdr-sm"));
        Assert.IsFalse(classes.Contains("bit-hdr-md"));
        Assert.IsFalse(classes.Contains("bit-hdr-lg"));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "bit-hdr-srt")]
    [DataRow(BitAlignment.End, "bit-hdr-end")]
    [DataRow(BitAlignment.Center, "bit-hdr-cnt")]
    [DataRow(BitAlignment.SpaceBetween, "bit-hdr-sbt")]
    [DataRow(BitAlignment.SpaceAround, "bit-hdr-sar")]
    [DataRow(BitAlignment.SpaceEvenly, "bit-hdr-sev")]
    [DataRow(BitAlignment.Baseline, "bit-hdr-bsl")]
    [DataRow(BitAlignment.Stretch, "bit-hdr-str")]
    public void BitHeaderShouldRespectAlignment(BitAlignment alignment, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "bit-hdr-vst")]
    [DataRow(BitAlignment.End, "bit-hdr-ved")]
    [DataRow(BitAlignment.Center, "bit-hdr-vcn")]
    [DataRow(BitAlignment.Baseline, "bit-hdr-bsl")]
    [DataRow(BitAlignment.Stretch, "bit-hdr-str")]
    public void BitHeaderShouldRespectVerticalAlign(BitAlignment verticalAlign, string expectedClass)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, verticalAlign);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitAlignment.SpaceBetween)]
    [DataRow(BitAlignment.SpaceAround)]
    [DataRow(BitAlignment.SpaceEvenly)]
    public void BitHeaderShouldIgnoreTheSpaceDistributionsOfVerticalAlign(BitAlignment verticalAlign)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, verticalAlign);
        });

        // Distributing free space says nothing about how a line is aligned on the cross axis, so those
        // three members have no class of their own here and must not leak the horizontal one either.
        var classes = component.Find(".bit-hdr").ClassList;

        Assert.IsFalse(classes.Contains("bit-hdr-sbt"));
        Assert.IsFalse(classes.Contains("bit-hdr-sar"));
        Assert.IsFalse(classes.Contains("bit-hdr-sev"));
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderAnyCrossAxisClassByDefault()
    {
        var component = RenderComponent<BitHeader>();

        var classes = component.Find(".bit-hdr").ClassList;

        Assert.IsFalse(classes.Contains("bit-hdr-vst"));
        Assert.IsFalse(classes.Contains("bit-hdr-ved"));
        Assert.IsFalse(classes.Contains("bit-hdr-vcn"));
        Assert.IsFalse(classes.Contains("bit-hdr-bsl"));
        Assert.IsFalse(classes.Contains("bit-hdr-str"));
    }

    [TestMethod]
    public void BitHeaderShouldPreferVerticalAlignOverTheCrossAxisMembersOfAlignment()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Baseline);
            parameters.Add(p => p.VerticalAlign, BitAlignment.End);
        });

        var classes = component.Find(".bit-hdr").ClassList;

        // Both spellings drive the same CSS variable, so only one of them may reach the markup.
        Assert.IsTrue(classes.Contains("bit-hdr-ved"));
        Assert.IsFalse(classes.Contains("bit-hdr-bsl"));
    }

    [TestMethod]
    public void BitHeaderShouldNotEmitAHorizontalClassForTheCrossAxisMembersOfAlignment()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Stretch);
        });

        var classes = component.Find(".bit-hdr").ClassList;

        Assert.IsTrue(classes.Contains("bit-hdr-str"));
        Assert.IsFalse(classes.Contains("bit-hdr-srt"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectWrap(bool wrap)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Wrap, wrap);
        });

        Assert.AreEqual(wrap, component.Find(".bit-hdr").ClassList.Contains("bit-hdr-wrp"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectBordered(bool bordered)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Bordered, bordered);
        });

        Assert.AreEqual(bordered, component.Find(".bit-hdr").ClassList.Contains("bit-hdr-brd"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectElevated(bool elevated)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Elevated, elevated);
        });

        Assert.AreEqual(elevated, component.Find(".bit-hdr").ClassList.Contains("bit-hdr-elv"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectTranslucent(bool translucent)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Translucent, translucent);
        });

        Assert.AreEqual(translucent, component.Find(".bit-hdr").ClassList.Contains("bit-hdr-trs"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectNoGutter(bool noGutter)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.NoGutter, noGutter);
        });

        Assert.AreEqual(noGutter, component.Find(".bit-hdr").ClassList.Contains("bit-hdr-ngt"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectReveal(bool reveal)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, reveal);
            parameters.Add(p => p.Fixed, true);
        });

        var header = component.Find(".bit-hdr");

        Assert.AreEqual(reveal, header.ClassList.Contains("bit-hdr-anm"));
        // The header starts revealed no matter what.
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-hdn"));
        Assert.IsTrue(component.Instance.IsRevealed);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void BitHeaderShouldOnlySetTheScrollScriptUpForAPositionedHeader(bool reveal, bool positioned)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, reveal);
            parameters.Add(p => p.Sticky, positioned);
        });

        // A header in the normal flow has nothing to slide over, so the scroll listener is only
        // attached for a Fixed or Sticky one.
        var setups = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup");

        Assert.AreEqual((reveal && positioned) ? 1 : 0, setups);
    }

    [TestMethod]
    public void BitHeaderShouldNotSetUpTheScrollScriptForAnAbsoluteHeader()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Absolute, true);
        });

        // An absolute header scrolls away with its container, so it has nothing to slide over.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    [DataRow(null, 0)]
    [DataRow(0, 0)]
    [DataRow(120, 120)]
    [DataRow(-5, 0)]
    public void BitHeaderShouldRespectRevealOffset(int? revealOffset, int expectedOffset)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.RevealOffset, revealOffset);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        Assert.AreEqual(component.Instance.UniqueId.ToString(), invocation.Arguments[0]);
        // A negative offset is clamped, so it can never keep the header hidden at the top of the scroller.
        Assert.AreEqual(expectedOffset, invocation.Arguments[2]);
        Assert.AreEqual(true, invocation.Arguments[4]);
        Assert.AreEqual(false, invocation.Arguments[5]);
    }

    [TestMethod]
    [DataRow(null, 0)]
    [DataRow(0, 0)]
    [DataRow(90, 90)]
    [DataRow(-7, 0)]
    public void BitHeaderShouldRespectElevateOffset(int? elevateOffset, int expectedOffset)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.ElevateOffset, elevateOffset);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        Assert.AreEqual(expectedOffset, invocation.Arguments[3]);
        Assert.AreEqual(false, invocation.Arguments[4]);
        Assert.AreEqual(true, invocation.Arguments[5]);
    }

    [TestMethod]
    public void BitHeaderShouldSetTheScrollScriptUpAgainWhenTheRevealOffsetChanges()
    {
        var component = RenderComponent<BitHeader>(parameters =>
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
                             .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                             .Select(i => i.Arguments[2])
                             .ToArray();

        CollectionAssert.AreEqual(new object[] { 50, 150 }, offsets);
    }

    [TestMethod]
    public void BitHeaderShouldNotSetTheScrollScriptUpAgainOnAnUnrelatedRender()
    {
        var component = RenderComponent<BitHeader>(parameters =>
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

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    public void BitHeaderShouldMoveTheScrollScriptWhenTheIdChanges()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.Id, "header-one");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.Id, "header-two");
        });

        // The listeners of the script are keyed by the id, so the old registration has to go down and a
        // new one has to come up under the new id, or the reveal would be driven by an element that is gone.
        var setups = Context.JSInterop.Invocations
                            .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                            .Select(i => i.Arguments[0])
                            .ToArray();

        CollectionAssert.AreEqual(new object[] { "header-one", "header-two" }, setups);

        var disposes = Context.JSInterop.Invocations
                              .Where(i => i.Identifier == "BitBlazorUI.Headers.dispose")
                              .Select(i => i.Arguments[0])
                              .ToArray();

        CollectionAssert.AreEqual(new object[] { "header-one" }, disposes);
    }

    [TestMethod]
    public void BitHeaderShouldNotCallTheScrollScriptAtAllWithoutRevealOrElevateOnScroll()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.Bordered, true);
        });

        // A header that never asked for a scroll behavior must not pay for a single interop call, on any render.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.StartsWith("BitBlazorUI.Headers")));
    }

    [TestMethod]
    public void BitHeaderShouldDisposeTheScrollScriptWhenRevealIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, false);
            parameters.Add(p => p.Fixed, true);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));
    }

    [TestMethod]
    public async Task BitHeaderShouldToggleTheHiddenClassOnRevealChange()
    {
        var revealStates = new List<bool>();

        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.OnRevealChanged, (bool v) => revealStates.Add(v));
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));
        Assert.IsFalse(component.Instance.IsRevealed);

        await component.InvokeAsync(() => component.Instance._OnRevealChange(false));

        Assert.IsFalse(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));
        Assert.IsTrue(component.Instance.IsRevealed);

        CollectionAssert.AreEqual(new[] { false, true }, revealStates);
    }

    [TestMethod]
    public async Task BitHeaderShouldNotMakeAScrollHiddenHeaderInert()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        var header = component.Find(".bit-hdr");

        // The header brings itself back as soon as anything inside it takes the focus, which an inert
        // header could never receive - so only the explicit Hidden takes its content out of reach.
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-hdn"));
        Assert.IsFalse(header.HasAttribute("inert"));
    }

    [TestMethod]
    public async Task BitHeaderShouldNotRaiseOnRevealChangedForTheSameState()
    {
        var callCount = 0;

        var component = RenderComponent<BitHeader>(parameters =>
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
    public async Task BitHeaderShouldNotStayHiddenWhenRevealIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        await component.InvokeAsync(() => component.Instance._OnRevealChange(true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, false);
            parameters.Add(p => p.Fixed, true);
        });

        // The hidden state is cleared in OnAfterRenderAsync, after the interop call that detaches the
        // scroll listener, so the render that turns Reveal off is not the render that reveals the header.
        component.WaitForAssertion(() =>
        {
            var header = component.Find(".bit-hdr");

            Assert.IsFalse(header.ClassList.Contains("bit-hdr-hdn"));
            Assert.IsFalse(header.ClassList.Contains("bit-hdr-anm"));
            Assert.IsTrue(component.Instance.IsRevealed);
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectElevateOnScroll(bool elevateOnScroll)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, elevateOnScroll);
            parameters.Add(p => p.Sticky, true);
        });

        var header = component.Find(".bit-hdr");

        // The transition is in place from the start so the very first shadow fades in, and the header
        // starts flat because the scroller starts at its top.
        Assert.AreEqual(elevateOnScroll, header.ClassList.Contains("bit-hdr-esc"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-elv"));
        Assert.IsFalse(component.Instance.IsScrolled);
    }

    [TestMethod]
    public async Task BitHeaderShouldToggleTheElevatedClassOnScrollChange()
    {
        var scrolledStates = new List<bool>();

        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.OnScrolledChanged, (bool v) => scrolledStates.Add(v));
        });

        await component.InvokeAsync(() => component.Instance._OnScrollChange(true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-elv"));
        Assert.IsTrue(component.Instance.IsScrolled);

        await component.InvokeAsync(() => component.Instance._OnScrollChange(false));

        Assert.IsFalse(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-elv"));
        Assert.IsFalse(component.Instance.IsScrolled);

        CollectionAssert.AreEqual(new[] { true, false }, scrolledStates);
    }

    [TestMethod]
    public async Task BitHeaderShouldNotRaiseOnScrolledChangedForTheSameState()
    {
        var callCount = 0;

        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.OnScrolledChanged, (bool _) => callCount++);
        });

        await component.InvokeAsync(() => component.Instance._OnScrollChange(true));
        await component.InvokeAsync(() => component.Instance._OnScrollChange(true));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void BitHeaderShouldPreferElevatedOverElevateOnScroll()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Elevated, true);
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
        });

        var header = component.Find(".bit-hdr");

        // A header that is always lifted has nothing left to gain from a scroll, so it stays elevated
        // and no shadow transition is armed for it.
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-elv"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-esc"));

        // The shadow is settled, but the scroll state is still a documented output of its own, so the
        // listener stays attached and OnScrolledChanged/IsScrolled keep working.
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldOnlySetTheScrollScriptUpForAPositionedElevateOnScrollHeader(bool positioned)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Fixed, positioned);
        });

        // A header in the normal flow scrolls away with the content it would have to lift itself above.
        Assert.AreEqual(positioned ? 1 : 0,
                        Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    public void BitHeaderShouldDisposeTheScrollScriptWhenElevateOnScrollIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, false);
            parameters.Add(p => p.Sticky, true);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));
    }

    [TestMethod]
    public void BitHeaderShouldKeepTheScrollScriptWhenOnlyOneOfTheTwoBehaviorsIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, false);
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
        });

        // The script is handed the two behaviors as flags, so dropping one of them has to reach it -
        // as a fresh setup under the same id rather than as a listener left driving a dead state.
        var setups = Context.JSInterop.Invocations
                            .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                            .ToArray();

        Assert.AreEqual(2, setups.Length);
        CollectionAssert.AreEqual(new object[] { true, false }, setups.Select(i => i.Arguments[4]).ToArray());
        CollectionAssert.AreEqual(new object[] { true, true }, setups.Select(i => i.Arguments[5]).ToArray());
    }

    [TestMethod]
    public async Task BitHeaderShouldDisposeTheScrollScriptWhenTheComponentGoesAway()
    {
        RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.Id, "header-disposed");
        });

        // Disposed through the renderer rather than by calling DisposeAsync on the instance, so this
        // goes down the same path a component removed from the render tree does.
        await Context.DisposeComponentsAsync();

        var disposes = Context.JSInterop.Invocations
                              .Where(i => i.Identifier == "BitBlazorUI.Headers.dispose")
                              .Select(i => i.Arguments[0])
                              .ToArray();

        CollectionAssert.AreEqual(new object[] { "header-disposed" }, disposes);
    }

    [TestMethod]
    public async Task BitHeaderShouldNotStayElevatedWhenElevateOnScrollIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
        });

        await component.InvokeAsync(() => component.Instance._OnScrollChange(true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-elv"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, false);
            parameters.Add(p => p.Sticky, true);
        });

        component.WaitForAssertion(() =>
        {
            var header = component.Find(".bit-hdr");

            Assert.IsFalse(header.ClassList.Contains("bit-hdr-elv"));
            Assert.IsFalse(header.ClassList.Contains("bit-hdr-esc"));
            Assert.IsFalse(component.Instance.IsScrolled);
        });
    }

    [TestMethod]
    public void BitHeaderShouldSetTheScrollScriptUpOnceForBothScrollBehaviors()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Fixed, true);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        // One listener drives both states, so the two behaviors never cost two scroll subscriptions.
        Assert.AreEqual(true, invocation.Arguments[4]);
        Assert.AreEqual(true, invocation.Arguments[5]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectHidden(bool hidden)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Hidden, hidden);
        });

        var header = component.Find(".bit-hdr");

        Assert.AreEqual(hidden, header.ClassList.Contains("bit-hdr-hdn"));
        // A hidden header is only translated out of the view, so it is made inert to keep its content
        // out of reach of the pointer and the keyboard while it is gone.
        Assert.AreEqual(hidden, header.HasAttribute("inert"));
        // A bound Hidden makes the header animatable whichever way it is bound, so the first slide runs too.
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-anm"));
    }

    [TestMethod]
    public void BitHeaderShouldNotBeAnimatableWithoutRevealOrHidden()
    {
        var component = RenderComponent<BitHeader>();

        var header = component.Find(".bit-hdr");

        Assert.IsFalse(header.ClassList.Contains("bit-hdr-anm"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-hdn"));
        Assert.IsFalse(header.HasAttribute("inert"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectHiddenChangedDynamically()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Hidden, false);
        });

        Assert.IsFalse(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));

        component.Render(parameters => parameters.Add(p => p.Hidden, true));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));

        component.Render(parameters => parameters.Add(p => p.Hidden, false));

        var header = component.Find(".bit-hdr");

        Assert.IsFalse(header.ClassList.Contains("bit-hdr-hdn"));
        // The transition has to outlive the hidden state or the way back in would not be animated.
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-anm"));
    }

    [TestMethod]
    public void BitHeaderShouldNotSetUpTheScrollScriptForAHiddenHeader()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Hidden, true);
            parameters.Add(p => p.Fixed, true);
        });

        // Hidden is driven by the application alone, so it needs no scroll listener of its own.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    public async Task BitHeaderShouldKeepTheHeaderHiddenWhileHiddenIsSetEvenAfterAReveal()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Hidden, true);
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Sticky, true);
        });

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));

        // The scroll says reveal, but the application says hidden, and the explicit parameter wins.
        await component.InvokeAsync(() => component.Instance._OnRevealChange(false));

        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-hdn"));
        Assert.IsTrue(component.Instance.IsRevealed);
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderTheSkipLinkByDefault()
    {
        var component = RenderComponent<BitHeader>();

        Assert.AreEqual(0, component.FindAll(".bit-hdr-skp").Count);
    }

    [TestMethod]
    public void BitHeaderShouldRespectSkipLinkHref()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.SkipLinkHref, "#main");
            parameters.AddChildContent("<span>content</span>");
        });

        var skipLink = component.Find(".bit-hdr-skp");

        Assert.AreEqual("#main", skipLink.GetAttribute("href"));
        Assert.AreEqual("Skip to main content", skipLink.TextContent);

        // It is only a shortcut past the header if it comes before everything the header holds.
        Assert.AreEqual(skipLink, component.Find(".bit-hdr-gut").FirstElementChild);
        Assert.AreEqual(skipLink, component.Find(".bit-hdr").QuerySelector("a, button, input"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectSkipLinkText()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.SkipLinkHref, "#main");
            parameters.Add(p => p.SkipLinkText, "Jump to the content");
        });

        Assert.AreEqual("Jump to the content", component.Find(".bit-hdr-skp").TextContent);
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderTheSkipLinkForATextOnlySkipLinkText()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.SkipLinkText, "Jump to the content");
        });

        // The href is what the link is for, so a text without one renders nothing.
        Assert.AreEqual(0, component.FindAll(".bit-hdr-skp").Count);
    }

    [TestMethod]
    [DataRow("padding: 1rem;")]
    [DataRow(null)]
    public void BitHeaderShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (string.IsNullOrEmpty(style) is false)
        {
            component.MarkupMatches($@"
<header style=""{style}"" class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
        else
        {
            component.MarkupMatches(@"
<header class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
    }

    [TestMethod]
    [DataRow("test-class")]
    [DataRow(null)]
    public void BitHeaderShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = !string.IsNullOrEmpty(@class) ? $" {@class}" : string.Empty;

        component.MarkupMatches($@"
<header class=""bit-hdr bit-hdr-fil{cssClass}"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.SkipLinkHref, "#main");
            parameters.Add(p => p.Classes, new BitHeaderClassStyles { Root = "root-class", Container = "container-class", SkipLink = "skip-class" });
            parameters.Add(p => p.Styles, new BitHeaderClassStyles { Root = "color:red", Container = "color:blue", SkipLink = "color:green" });
        });

        var header = component.Find(".bit-hdr");

        Assert.IsTrue(header.ClassList.Contains("root-class"));
        Assert.IsTrue(header.GetAttribute("style")!.Contains("color:red"));

        var container = component.Find(".bit-hdr-gut");

        Assert.IsTrue(container.ClassList.Contains("container-class"));
        Assert.IsTrue(container.GetAttribute("style")!.Contains("color:blue"));

        var skipLink = component.Find(".bit-hdr-skp");

        Assert.IsTrue(skipLink.ClassList.Contains("skip-class"));
        Assert.IsTrue(skipLink.GetAttribute("style")!.Contains("color:green"));
    }

    [TestMethod]
    [DataRow("test-id")]
    [DataRow(null)]
    public void BitHeaderShouldRespectId(string id)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id ?? component.Instance.UniqueId.ToString();

        component.MarkupMatches($@"
<header id=""{expectedId}"" class=""bit-hdr bit-hdr-fil"">
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    [DataRow("Site header")]
    [DataRow(null)]
    public void BitHeaderShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        Assert.AreEqual(ariaLabel, component.Find(".bit-hdr").GetAttribute("aria-label"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-hdr").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, null)]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitHeaderShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-hdr").GetAttribute("style") ?? string.Empty;

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
    public void BitHeaderShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : string.Empty;
            component.MarkupMatches($@"
<header dir=""{dir.Value.ToString().ToLower()}"" class=""bit-hdr bit-hdr-fil{cssClass}"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
        else
        {
            component.MarkupMatches(@"
<header class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        var cssClass = forceAnimation ? " bit-fam" : string.Empty;
        component.MarkupMatches($@"
<header class=""bit-hdr bit-hdr-fil{cssClass}"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitHeaderHtmlAttributesTest>();

        component.MarkupMatches(@"
<header data-val-test=""bit"" class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
        I'm a header
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitHeaderCascadingParamsTest>();

        var headers = component.FindAll(".bit-hdr");

        // The first header takes everything from the cascading parameters.
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-suc"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-otl"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-lg"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-brd"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-stk"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-anm"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-esc"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-wrp"));
        Assert.IsTrue(headers[0].ClassList.Contains("bit-hdr-ved"));
        StringAssert.Contains(headers[0].GetAttribute("style")!, "--bit-hdr-gap:1rem");

        // The second one sets its own color and gap, which the cascading parameters must not overwrite.
        Assert.IsTrue(headers[1].ClassList.Contains("bit-hdr-err"));
        Assert.IsFalse(headers[1].ClassList.Contains("bit-hdr-suc"));
        Assert.IsTrue(headers[1].ClassList.Contains("bit-hdr-otl"));
        StringAssert.Contains(headers[1].GetAttribute("style")!, "--bit-hdr-gap:2rem");

        // The cascaded skip link reaches both headers.
        var skipLinks = component.FindAll(".bit-hdr-skp");

        Assert.AreEqual(2, skipLinks.Count);
        Assert.AreEqual("#main", skipLinks[0].GetAttribute("href"));
        Assert.AreEqual("Jump to the content", skipLinks[0].TextContent);

        // The cascaded offsets reach the script of both headers.
        var setups = Context.JSInterop.Invocations
                            .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                            .ToArray();

        Assert.AreEqual(2, setups.Length);
        CollectionAssert.AreEqual(new object[] { 80, 80 }, setups.Select(i => i.Arguments[2]).ToArray());
        CollectionAssert.AreEqual(new object[] { 30, 30 }, setups.Select(i => i.Arguments[3]).ToArray());
        CollectionAssert.AreEqual(new object[] { "#pane", "#pane" }, setups.Select(i => i.Arguments[6]).ToArray());
        CollectionAssert.AreEqual(new object[] { true, true }, setups.Select(i => i.Arguments[7]).ToArray());

        // The cascaded max width reaches both headers as the variable the container reads.
        StringAssert.Contains(headers[0].GetAttribute("style")!, "--bit-hdr-max-width:60rem");
        StringAssert.Contains(headers[1].GetAttribute("style")!, "--bit-hdr-max-width:60rem");
    }

    [TestMethod]
    [DataRow("40rem")]
    [DataRow("1200px")]
    public void BitHeaderShouldRespectMaxWidth(string maxWidth)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.MaxWidth, maxWidth);
        });

        component.MarkupMatches($@"
<header style=""--bit-hdr-max-width:{maxWidth}"" class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderTheMaxWidthVariableByDefault()
    {
        var component = RenderComponent<BitHeader>();

        var style = component.Find(".bit-hdr").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-hdr-max-width"));
    }

    [TestMethod]
    public void BitHeaderShouldRespectMaxWidthChangedDynamically()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.MaxWidth, "40rem");
        });

        StringAssert.Contains(component.Find(".bit-hdr").GetAttribute("style")!, "--bit-hdr-max-width:40rem");

        component.Render(parameters => parameters.Add(p => p.MaxWidth, "60rem"));

        StringAssert.Contains(component.Find(".bit-hdr").GetAttribute("style")!, "--bit-hdr-max-width:60rem");
    }

    [TestMethod]
    public void BitHeaderShouldRespectMaxWidthAndGapAndHeightTogether()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 64);
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.MaxWidth, "75rem");
        });

        var style = component.Find(".bit-hdr").GetAttribute("style")!;

        StringAssert.Contains(style, "height:64px");
        StringAssert.Contains(style, "--bit-hdr-gap:1rem");
        StringAssert.Contains(style, "--bit-hdr-max-width:75rem");
    }

    [TestMethod]
    public void BitHeaderShouldNotAddTheSafeAreaInsetToTheHeightOfAnAbsoluteHeaderThatIsAlsoSticky()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Height, 56);
            parameters.Add(p => p.Absolute, true);
            parameters.Add(p => p.Sticky, true);
        });

        // Absolute outranks Sticky, so the header is rendered pinned to its container - where there is no
        // device inset above it - and the height must follow the position that actually won.
        Assert.IsTrue(component.Find(".bit-hdr").ClassList.Contains("bit-hdr-abs"));
        Assert.AreEqual("height:56px", component.Find(".bit-hdr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitHeaderShouldPassTheScrollTargetToTheScrollScript()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.Sticky, true);
            parameters.Add(p => p.ScrollTarget, "#pane");
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        Assert.AreEqual("#pane", invocation.Arguments[6]);
        Assert.AreEqual(false, invocation.Arguments[7]);
    }

    [TestMethod]
    public void BitHeaderShouldPassANullScrollTargetWhenNoneIsSet()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        // No selector means the script walks up from the header to find its own scroller.
        Assert.IsNull(invocation.Arguments[6]);
    }

    [TestMethod]
    public void BitHeaderShouldSetTheScrollScriptUpAgainWhenTheScrollTargetChanges()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.ScrollTarget, "#first");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.Fixed, true);
            parameters.Add(p => p.ScrollTarget, "#second");
        });

        // The scroller is resolved once at setup time, so a new one has to reach the script as a fresh setup.
        var targets = Context.JSInterop.Invocations
                             .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                             .Select(i => i.Arguments[6])
                             .ToArray();

        CollectionAssert.AreEqual(new object[] { "#first", "#second" }, targets);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitHeaderShouldOnlySetTheScrollScriptUpForAPositionedScrollPaddingHeader(bool positioned)
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, true);
            parameters.Add(p => p.Sticky, positioned);
        });

        // The scroll padding is the room a pinned header takes at the top of the scroller, so a header in
        // the normal flow - which covers nothing - has none to reserve and needs no script at all.
        var setups = Context.JSInterop.Invocations
                            .Where(i => i.Identifier == "BitBlazorUI.Headers.setup")
                            .ToArray();

        Assert.AreEqual(positioned ? 1 : 0, setups.Length);

        if (positioned)
        {
            // The two scroll behaviors stay off: the script is only there for the padding.
            Assert.AreEqual(false, setups[0].Arguments[4]);
            Assert.AreEqual(false, setups[0].Arguments[5]);
            Assert.AreEqual(true, setups[0].Arguments[7]);
        }
    }

    [TestMethod]
    public void BitHeaderShouldNotSetUpTheScrollScriptForAnAbsoluteScrollPaddingHeader()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, true);
            parameters.Add(p => p.Absolute, true);
            parameters.Add(p => p.Sticky, true);
        });

        // An absolute header scrolls away with its container, so it never covers the top of the scroller.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    public void BitHeaderShouldDisposeTheScrollScriptWhenScrollPaddingIsTurnedOff()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, true);
            parameters.Add(p => p.Fixed, true);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, false);
            parameters.Add(p => p.Fixed, true);
        });

        // The script is what put the padding on the scroller, so it has to be taken down to give it back.
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.dispose"));
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Headers.setup"));
    }

    [TestMethod]
    public void BitHeaderShouldSetTheScrollScriptUpOnceForAllThreeScrollFeatures()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.Reveal, true);
            parameters.Add(p => p.ElevateOnScroll, true);
            parameters.Add(p => p.ScrollPadding, true);
            parameters.Add(p => p.Fixed, true);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Headers.setup");

        Assert.AreEqual(true, invocation.Arguments[4]);
        Assert.AreEqual(true, invocation.Arguments[5]);
        Assert.AreEqual(true, invocation.Arguments[7]);
    }

    [TestMethod]
    public void BitHeaderShouldNotRenderAnyExtraMarkupForTheScrollParameters()
    {
        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.Add(p => p.ScrollPadding, true);
            parameters.Add(p => p.ScrollTarget, "#pane");
        });

        // Both of them are handed to the script alone, so neither leaks into the markup of the header.
        component.MarkupMatches(@"
<header class=""bit-hdr bit-hdr-fil"" id:ignore>
    <div class=""bit-hdr-gut"">
    </div>
</header>");
    }
}
