using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.CitationMarker;

[TestClass]
public class BitAiCitationMarkerTests : BitAiTestBase
{
    private sealed class GermanStrings : BitAiStrings
    {
        public override string CitationLabel(int? number, string title) => $"Quelle {number}: {title}";
    }

    private static BitAiCitation Citation(string? quote = null) =>
        new("c1", new BitAiCitationSource("s1", "Handbook")) { Number = 2, Quote = quote };



    [TestMethod]
    public void BitAiCitationMarkerShouldBeANumberedButton()
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p.Add(c => c.Citation, Citation()));

        var root = component.Find(".bit-aicmk");

        Assert.AreEqual("BUTTON", root.TagName);
        Assert.AreEqual("button", root.GetAttribute("type"));
        Assert.AreEqual("2", root.TextContent);
        Assert.AreEqual("Source 2: Handbook", root.GetAttribute("aria-label"));
        Assert.AreEqual("Handbook", root.GetAttribute("title"));
        Assert.IsFalse(root.HasAttribute("disabled"));
        Assert.IsNull(root.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void TheTooltipPreviewsTheQuoteCutShort()
    {
        var quote = new string('a', 300);

        var component = RenderComponent<BitAiCitationMarker>(p => p.Add(c => c.Citation, Citation(quote)));

        var title = component.Find(".bit-aicmk").GetAttribute("title")!;

        Assert.StartsWith("Handbook\n\n", title);
        Assert.EndsWith("...", title);
        Assert.AreEqual("Handbook\n\n".Length + 220 + 3, title.Length);
    }

    [TestMethod]
    public void AccessibleNamesComeFromTheStringTable()
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Strings = new GermanStrings() }));

        Assert.AreEqual("Quelle 2: Handbook", component.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void AnAriaLabelReplacesTheOneFromTheStringTable()
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.AriaLabel, "Open the handbook"));

        Assert.AreEqual("Open the handbook", component.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ActivatingTheMarkerHandsOverItsCitation()
    {
        var citation = Citation();
        BitAiCitation? activated = null;

        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, citation)
            .Add(c => c.OnActivate, c => activated = c));

        component.Find("button").Click();

        Assert.AreSame(citation, activated);
    }

    [TestMethod]
    public void ADisabledMarkerIsADisabledButtonThatHandsOverNothing()
    {
        BitAiCitation? activated = null;

        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.Disabled, true)
            .Add(c => c.OnActivate, c => activated = c));

        var root = component.Find("button");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.HasAttribute("disabled"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        root.Click();

        Assert.IsNull(activated);
    }

    [TestMethod]
    public void BitAiCitationMarkerShouldRespectClassStyleAndTabIndex()
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.TabIndex, "-1"));

        var root = component.Find(".bit-aicmk");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiCitationMarkerShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aicmk").GetAttribute("style");

        if (expected is null)
        {
            Assert.IsNull(style);
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiCitationMarkerShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aicmk");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiCitationMarkerShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiCitationMarker>(p => p
            .Add(c => c.Citation, Citation())
            .Add(c => c.Classes, new() { Root = "c-root" })
            .Add(c => c.Styles, new() { Root = "color: red" }));

        var root = component.Find(".bit-aicmk");

        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitAiCitationMarkerShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiCitationMarkerParams { Class = "cascaded", Classes = new() { Root = "cascaded-root" } }
            });
            p.AddChildContent<BitAiCitationMarker>(c => c.Add(x => x.Citation, Citation()));
        });

        var root = component.Find(".bit-aicmk");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
    }

    [TestMethod]
    public void BitAiCitationMarkerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiCitationMarker", BitAiCitationMarkerParams.ParamName);
        Assert.AreEqual(BitAiCitationMarkerParams.ParamName, new BitAiCitationMarkerParams().Name);
    }
}
