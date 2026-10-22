using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.SourceList;

[TestClass]
public class BitAiSourceListTests : BitAiTestBase
{
    private static BitAiCitation Cite(string sourceId, string title, int? number) =>
        new($"c-{sourceId}-{number}", new BitAiCitationSource(sourceId, title) { Url = $"https://example.com/{sourceId}" }) { Number = number };



    [TestMethod]
    public void NothingIsRenderedWithoutCitations()
    {
        var none = RenderComponent<BitAiSourceList>();
        var empty = RenderComponent<BitAiSourceList>(p => p.Add(c => c.Citations, []));

        Assert.AreEqual(string.Empty, none.Markup.Trim());
        Assert.AreEqual(string.Empty, empty.Markup.Trim());
    }

    [TestMethod]
    public void EverySourceIsListedOnceUnderItsLowestNumberInOrder()
    {
        var component = RenderComponent<BitAiSourceList>(p => p.Add(c => c.Citations,
        [
            Cite("b", "Second", 2),
            Cite("u", "Unnumbered", null),
            Cite("a", "First", 3),
            Cite("a", "First", 1),
        ]));

        var root = component.Find(".bit-aisrl");
        var cards = component.FindAll(".bit-aisrl-lst > .bit-aisrc");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual("Sources", component.Find(".bit-aisrl-hdr").TextContent);
        CollectionAssert.AreEqual(new[] { "First", "Second", "Unnumbered" }, cards.Select(c => c.QuerySelector(".bit-aisrc-ttl")!.TextContent).ToArray());
        CollectionAssert.AreEqual(new[] { "1", "2" }, component.FindAll(".bit-aisrc-idx").Select(i => i.TextContent).ToArray());
    }

    [TestMethod]
    public void TheHeadingCanBeLeftOut()
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.ShowHeading, false));

        Assert.IsEmpty(component.FindAll(".bit-aisrl-hdr"));
        Assert.HasCount(1, component.FindAll(".bit-aisrc"));
    }

    [TestMethod]
    public void TheCardsAreLinksUnlessTheListHandlesTheirActivation()
    {
        var links = RenderComponent<BitAiSourceList>(p => p.Add(c => c.Citations, [Cite("a", "First", 1)]));
        Assert.AreEqual("https://example.com/a", links.Find(".bit-aisrc a").GetAttribute("href"));

        BitAiCitationSource? activated = null;
        var buttons = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.OnSourceClick, s => activated = s));

        Assert.IsEmpty(buttons.FindAll("a"));
        buttons.Find(".bit-crd").Click();
        Assert.AreEqual("a", activated?.SourceId);
    }

    [TestMethod]
    public void ADisabledListDisablesItsCards()
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-aisrl").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.Find(".bit-aisrc").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitAiSourceListShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px"));

        var root = component.Find(".bit-aisrl");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiSourceListShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aisrl").GetAttribute("style");

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
    public void BitAiSourceListShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aisrl");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiSourceListShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiSourceList>(p => p
            .Add(c => c.Citations, [Cite("a", "First", 1)])
            .Add(c => c.Classes, new() { Root = "c-root", Header = "c-header", List = "c-list", SourceCard = new() { Root = "c-card" } })
            .Add(c => c.Styles, new() { Root = "color: red", List = "color: blue" }));

        Assert.IsTrue(component.Find(".bit-aisrl").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-aisrl").GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aisrl-hdr").ClassList.Contains("c-header"));
        Assert.IsTrue(component.Find(".bit-aisrl-lst").ClassList.Contains("c-list"));
        Assert.AreEqual("color: blue", component.Find(".bit-aisrl-lst").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aisrc").ClassList.Contains("c-card"));
    }

    [TestMethod]
    public void BitAiSourceListShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiSourceListParams { Class = "cascaded", ShowHeading = false }
            });
            p.AddChildContent<BitAiSourceList>(c => c.Add(x => x.Citations, [Cite("a", "First", 1)]));
        });

        Assert.IsTrue(component.Find(".bit-aisrl").ClassList.Contains("cascaded"));
        Assert.IsEmpty(component.FindAll(".bit-aisrl-hdr"));
    }

    [TestMethod]
    public void AHeadingSetOnTheListWinsOverTheCascadedOne()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams> { new BitAiSourceListParams { ShowHeading = false } });
            p.AddChildContent<BitAiSourceList>(c => c
                .Add(x => x.Citations, [Cite("a", "First", 1)])
                .Add(x => x.ShowHeading, true));
        });

        Assert.HasCount(1, component.FindAll(".bit-aisrl-hdr"));
    }

    [TestMethod]
    public void BitAiSourceListParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiSourceList", BitAiSourceListParams.ParamName);
        Assert.AreEqual(BitAiSourceListParams.ParamName, new BitAiSourceListParams().Name);
    }
}
