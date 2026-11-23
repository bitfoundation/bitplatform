using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ScrollToTop;

[TestClass, DoNotParallelize]
public class BitScrollToTopTests : BunitTestContext
{
    private sealed class GermanStrings : BitStrings
    {
        public override string ScrollToTop => "Nach oben";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    private IRenderedComponent<BitScrollToTop> Render(Action<ComponentParameterCollectionBuilder<BitScrollToTop>>? parameters = null)
        => RenderComponent<BitScrollToTop>(p => parameters?.Invoke(p));

    [TestMethod]
    public void BitScrollToTopShouldBeAnIconOnlyButtonNamedByTheStringsWhenGivenNothing()
    {
        var com = Render();

        var root = com.Find(".bit-stt");

        // A button, not a link: nothing is navigated, and it never submits a form it sits in.
        Assert.AreEqual("BUTTON", root.TagName);
        Assert.AreEqual("button", root.GetAttribute("type"));
        Assert.AreEqual("Scroll to top", root.GetAttribute("aria-label"));
        Assert.IsTrue(root.ClassList.Contains("bit-stt-ion"));

        // The script works off the markup alone: the marker with the behavior, and nothing else while nothing is asked.
        Assert.AreEqual("smooth", root.GetAttribute("data-bit-stt"));
        Assert.IsFalse(root.HasAttribute("data-bit-stt-offset"));
        Assert.IsFalse(root.HasAttribute("data-bit-stt-target"));
        Assert.IsFalse(root.HasAttribute("data-bit-stt-shown"));
        Assert.IsFalse(root.ClassList.Contains("bit-stt-rvl"));

        // In the flow of the page: neither floating nor pinned anywhere.
        Assert.IsFalse(root.ClassList.Any(c => c is "bit-stt-ffx" or "bit-stt-fab" or "bit-stt-ben"));

        var icon = com.Find(".bit-stt-ico");
        Assert.AreEqual("I", icon.TagName);
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Up"));
        Assert.AreEqual(0, com.FindAll(".bit-stt-txt").Count);
    }

    [TestMethod]
    public void BitScrollToTopShouldBeNamedByTheTranslatedStringsAndTheAriaLabelOverThem()
    {
        BitStrings.Current = new GermanStrings();

        var com = Render();

        Assert.AreEqual("Nach oben", com.Find(".bit-stt").GetAttribute("aria-label"));

        com.Render(p => p.Add(x => x.AriaLabel, "Zum Anfang"));

        Assert.AreEqual("Zum Anfang", com.Find(".bit-stt").GetAttribute("aria-label"), "the AriaLabel of the app wins over the strings");
    }

    [TestMethod]
    public void BitScrollToTopWithATextShouldBeNamedByItAndShowNoIconUnlessGivenOne()
    {
        var com = Render(p => p.Add(x => x.Text, "Back to top"));

        var root = com.Find(".bit-stt");

        Assert.AreEqual("Back to top", com.Find(".bit-stt-txt").TextContent);
        Assert.IsNull(root.GetAttribute("aria-label"), "the text names the button");
        Assert.IsFalse(root.ClassList.Contains("bit-stt-ion"));
        Assert.AreEqual(0, com.FindAll(".bit-stt-ico").Count);

        com.Render(p => p.Add(x => x.IconName, "ChevronUpMed"));

        var parts = com.Find(".bit-stt").Children.Select(c => c.ClassList[0]).ToArray();
        CollectionAssert.AreEqual(new[] { "bit-stt-ico", "bit-stt-txt" }, parts);
        Assert.IsTrue(com.Find(".bit-stt-ico").ClassList.Contains("bit-icon--ChevronUpMed"));
    }

    [TestMethod]
    public void BitScrollToTopShouldPreferTheIconOverTheIconName()
    {
        var com = Render(p =>
        {
            p.Add(x => x.IconName, "DoubleChevronUp");
            p.Add(x => x.Icon, BitIconInfo.Fa("solid arrow-up"));
        });

        var icon = com.Find(".bit-stt-ico");

        Assert.IsTrue(icon.ClassList.Contains("fa-arrow-up"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--DoubleChevronUp"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Up"));
    }

    [TestMethod]
    public void BitScrollToTopShouldWriteTheLigatureOfALigatureIconSet()
    {
        var com = Render(p => p.Add(x => x.Icon, BitIconInfo.Mi("arrow_upward")));

        var icon = com.Find(".bit-stt-ico");

        Assert.AreEqual("arrow_upward", icon.TextContent);
        Assert.AreEqual("no", icon.GetAttribute("translate"), "a translated ligature draws nothing");
    }

    [TestMethod]
    [DataRow(300, "300")]
    [DataRow(0, "0")]
    [DataRow(-50, "0")]
    public void BitScrollToTopShouldBeHiddenFromTheFirstRenderUntilScrolledPastTheRevealOffset(int offset, string expected)
    {
        var com = Render(p => p.Add(x => x.RevealOffset, offset));

        var root = com.Find(".bit-stt");

        // Hidden by the class until the script marks it shown, so a prerendered page shows no button at its top.
        Assert.IsTrue(root.ClassList.Contains("bit-stt-rvl"));
        Assert.IsFalse(root.HasAttribute("data-bit-stt-shown"));
        Assert.AreEqual(expected, root.GetAttribute("data-bit-stt-offset"), "a negative offset would keep the button revealed at the top");

        com.Render(p => p.Add(x => x.RevealOffset, (int?)null));

        Assert.IsFalse(com.Find(".bit-stt").ClassList.Contains("bit-stt-rvl"));
        Assert.IsFalse(com.Find(".bit-stt").HasAttribute("data-bit-stt-offset"));
    }

    [TestMethod]
    public void BitScrollToTopShouldWriteItsRevealOffsetTheSameWayInEveryCulture()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");

            var com = Render(p => p.Add(x => x.RevealOffset, 1200));

            Assert.AreEqual("1200", com.Find(".bit-stt").GetAttribute("data-bit-stt-offset"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public void BitScrollToTopShouldNameItsScrollTargetAndItsBehaviorToTheScript()
    {
        var com = Render(p =>
        {
            p.Add(x => x.ScrollTarget, "#main-pane");
            p.Add(x => x.Smooth, false);
        });

        var root = com.Find(".bit-stt");

        Assert.AreEqual("#main-pane", root.GetAttribute("data-bit-stt-target"));
        Assert.AreEqual("instant", root.GetAttribute("data-bit-stt"));

        com.Render(p => p.Add(x => x.ScrollTarget, ""));

        Assert.IsFalse(com.Find(".bit-stt").HasAttribute("data-bit-stt-target"), "an empty selector names nothing");
    }

    [TestMethod]
    [DataRow(BitPosition.TopLeft, "bit-stt-tlf")]
    [DataRow(BitPosition.TopCenter, "bit-stt-tcr")]
    [DataRow(BitPosition.TopRight, "bit-stt-trg")]
    [DataRow(BitPosition.TopStart, "bit-stt-tst")]
    [DataRow(BitPosition.TopEnd, "bit-stt-ten")]
    [DataRow(BitPosition.CenterLeft, "bit-stt-clf")]
    [DataRow(BitPosition.Center, "bit-stt-ctr")]
    [DataRow(BitPosition.CenterRight, "bit-stt-crg")]
    [DataRow(BitPosition.CenterStart, "bit-stt-cst")]
    [DataRow(BitPosition.CenterEnd, "bit-stt-cen")]
    [DataRow(BitPosition.BottomLeft, "bit-stt-blf")]
    [DataRow(BitPosition.BottomCenter, "bit-stt-bcr")]
    [DataRow(BitPosition.BottomRight, "bit-stt-brg")]
    [DataRow(BitPosition.BottomStart, "bit-stt-bst")]
    [DataRow(BitPosition.BottomEnd, "bit-stt-ben")]
    [DataRow(null, "bit-stt-ben")]
    public void BitScrollToTopShouldFloatWherePositioned(BitPosition? position, string expected)
    {
        var com = Render(p =>
        {
            p.Add(x => x.Float, true);
            p.Add(x => x.FloatPosition, position);
        });

        var root = com.Find(".bit-stt");

        Assert.IsTrue(root.ClassList.Contains("bit-stt-ffx"));
        Assert.IsTrue(root.ClassList.Contains(expected));

        com.Render(p => p.Add(x => x.Float, false));

        Assert.IsFalse(com.Find(".bit-stt").ClassList.Contains(expected), "a button in the flow is pinned nowhere");
    }

    [TestMethod]
    public void BitScrollToTopShouldFloatInItsContainerWithFloatAbsoluteOverFloat()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Float, true);
            p.Add(x => x.FloatAbsolute, true);
        });

        var root = com.Find(".bit-stt");

        Assert.IsTrue(root.ClassList.Contains("bit-stt-fab"));
        Assert.IsFalse(root.ClassList.Contains("bit-stt-ffx"));
        Assert.IsTrue(root.ClassList.Contains("bit-stt-ben"));
    }

    [TestMethod]
    [DataRow("24", "--bit-stt-flo:24px")]
    [DataRow(" 1.5 ", "--bit-stt-flo:1.5px")]
    [DataRow("1rem", "--bit-stt-flo:1rem")]
    [DataRow("calc(1rem + 4px)", "--bit-stt-flo:calc(1rem + 4px)")]
    public void BitScrollToTopShouldWriteItsFloatOffsetAsALength(string offset, string expected)
    {
        var com = Render(p =>
        {
            p.Add(x => x.Float, true);
            p.Add(x => x.FloatOffset, offset);
        });

        StringAssert.Contains(com.Find(".bit-stt").GetAttribute("style"), expected);
    }

    [TestMethod]
    public void BitScrollToTopShouldPublishNoFloatOffsetWhileItIsUnset()
    {
        var com = Render(p => p.Add(x => x.Float, true));

        Assert.IsFalse((com.Find(".bit-stt").GetAttribute("style") ?? "").Contains("--bit-stt-flo"));
    }

    [TestMethod]
    public void BitScrollToTopShouldBeNativelyDisabled()
    {
        var com = Render(p => p.Add(x => x.Disabled, true));

        var root = com.Find(".bit-stt");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.HasAttribute("disabled"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        com.Render(p => p.Add(x => x.Disabled, false));

        root = com.Find(".bit-stt");
        Assert.IsFalse(root.HasAttribute("disabled"));
        Assert.IsNull(root.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitScrollToTopShouldKeepTheDirAndAriaDisabledThePageWritesWhereNoParameterSaysOtherwise()
    {
        // Blazor matches parameter names regardless of case, so the splatted dir goes through HtmlAttributes itself.
        var com = Render(p => p.Add(x => x.HtmlAttributes, new Dictionary<string, object> { ["dir"] = "rtl", ["aria-disabled"] = "true" }));

        var root = com.Find(".bit-stt");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        com.Render(p => p.Add(x => x.Dir, BitDir.Ltr));

        Assert.AreEqual("ltr", com.Find(".bit-stt").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitScrollToTopShouldKeepTheTabIndexItIsGiven()
    {
        var com = Render(p => p.Add(x => x.TabIndex, "-1"));

        Assert.AreEqual("-1", com.Find(".bit-stt").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitScrollToTopShouldApplyTheClassesAndStylesOfEachOfItsParts()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Text, "Back to top");
            p.Add(x => x.IconName, "Up");
            p.Add(x => x.Class, "own-class");
            p.Add(x => x.Style, "margin:1px");
            p.Add(x => x.Classes, new BitScrollToTopClassStyles { Root = "c-root", Icon = "c-icon", Text = "c-text" });
            p.Add(x => x.Styles, new BitScrollToTopClassStyles { Root = "padding:2px", Icon = "color:red", Text = "color:blue" });
        });

        var root = com.Find(".bit-stt");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("own-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        Assert.IsTrue(com.Find(".bit-stt-ico").ClassList.Contains("c-icon"));
        Assert.AreEqual("color:red", com.Find(".bit-stt-ico").GetAttribute("style"));
        Assert.IsTrue(com.Find(".bit-stt-txt").ClassList.Contains("c-text"));
        Assert.AreEqual("color:blue", com.Find(".bit-stt-txt").GetAttribute("style"));
    }

    [TestMethod]
    public void BitScrollToTopShouldNeedNoScriptCallOfItsOwn()
    {
        // The script acts on the markup through its delegated listeners, so the component calls nothing - which is what
        // lets it work before the app is interactive. A strict JSInterop throws on any call it is not set up for.
        Context.JSInterop.Mode = JSRuntimeMode.Strict;

        var com = Render(p =>
        {
            p.Add(x => x.Float, true);
            p.Add(x => x.RevealOffset, 200);
        });

        com.Render(p => p.Add(x => x.Text, "Back to top"));

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count);
    }
}
