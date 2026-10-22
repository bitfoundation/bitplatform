using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.SourceCard;

[TestClass]
public class BitAiSourceCardTests : BitAiTestBase
{
    private static BitAiCitationSource Source(string? url = "https://learn.microsoft.com/aspnet") =>
        new("s1", "Docs") { Url = url };



    [TestMethod]
    public void BitAiSourceCardShouldRenderTheSourceOnACard()
    {
        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source() with { SiteName = "Microsoft Learn" })
            .Add(c => c.Number, 3));

        var root = component.Find(".bit-aisrc");

        Assert.AreEqual("DIV", root.TagName);
        Assert.IsTrue(root.FirstElementChild!.ClassList.Contains("bit-crd"));
        Assert.AreEqual("3", component.Find(".bit-aisrc-idx").TextContent);
        Assert.AreEqual("Docs", component.Find(".bit-aisrc-ttl").TextContent);
        StringAssert.Contains(component.Find(".bit-aisrc-sit").TextContent, "Microsoft Learn");
    }

    [TestMethod]
    public void TheSiteFallsBackToTheHostAndThenToTheKind()
    {
        var withHost = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source()));
        var withoutUrl = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source(null) with { Kind = BitAiSourceKind.Document }));

        Assert.AreEqual("learn.microsoft.com", withHost.Find(".bit-aisrc-sit").TextContent.Trim());
        Assert.AreEqual("Document", withoutUrl.Find(".bit-aisrc-sit").TextContent.Trim());
    }

    [TestMethod]
    public void ACitationWithoutANumberShowsNone()
    {
        var unnumbered = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source()));
        var last = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source()).Add(c => c.Number, int.MaxValue));

        Assert.IsEmpty(unnumbered.FindAll(".bit-aisrc-idx"));
        Assert.IsEmpty(last.FindAll(".bit-aisrc-idx"));
    }

    [TestMethod,
        DataRow("javascript:alert(document.cookie)"),
        DataRow("  JavaScript:alert(1)"),
        DataRow("data:text/html,<script>alert(1)</script>"),
        DataRow("vbscript:msgbox(1)")]
    public void ASourceWithAScriptUrlIsNotRenderedAsALink(string url)
    {
        var component = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source(url)));

        Assert.IsEmpty(component.FindAll("a"));
        Assert.IsEmpty(component.FindAll("[href]"));
        Assert.IsNotNull(component.Find(".bit-aisrc .bit-crd"));
        Assert.IsFalse(component.Markup.Contains("script:", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ASourceWithAWebUrlIsRenderedAsALink()
    {
        var component = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source()));

        var link = component.Find(".bit-aisrc a");

        Assert.AreEqual("https://learn.microsoft.com/aspnet", link.GetAttribute("href"));
        Assert.AreEqual("_blank", link.GetAttribute("target"));
        StringAssert.Contains(link.GetAttribute("rel"), "noopener");
        StringAssert.Contains(link.GetAttribute("rel"), "noreferrer");
    }

    [TestMethod]
    public void AWebUrlIsTrimmedBeforeItBecomesTheLink()
    {
        var component = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source("  https://example.com/a  ")));

        Assert.AreEqual("https://example.com/a", component.Find(".bit-aisrc a").GetAttribute("href"));
    }

    [TestMethod,
        DataRow("javascript:alert(1)", false),
        DataRow("data:text/html,<script>alert(1)</script>", false),
        DataRow("data:image/svg+xml;base64,PHN2Zz4=", false),
        DataRow("data:image/png;base64,iVBORw0KGgo=", true),
        DataRow("https://example.com/favicon.ico", true)]
    public void ASourceIconLoadsOnlyFromTheWebOrAnInlineImage(string icon, bool rendered)
    {
        var source = new BitAiCitationSource("s1", "Docs") { Url = "https://example.com", IconUrl = icon };

        var component = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, source));

        Assert.HasCount(rendered ? 1 : 0, component.FindAll("img"));

        if (rendered)
        {
            var img = component.Find("img.bit-aisrc-fav");

            Assert.AreEqual(icon, img.GetAttribute("src"));
            Assert.AreEqual(string.Empty, img.GetAttribute("alt"));
            Assert.AreEqual("no-referrer", img.GetAttribute("referrerpolicy"));
            Assert.AreEqual("lazy", img.GetAttribute("loading"));
        }
    }

    [TestMethod]
    public void AnActivatableCardIsAButtonRatherThanALink()
    {
        BitAiCitationSource? activated = null;
        var source = Source();

        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, source)
            .Add(c => c.OnActivate, s => activated = s));

        Assert.IsEmpty(component.FindAll("a"));

        component.Find(".bit-crd").Click();

        Assert.AreSame(source, activated);
    }

    [TestMethod]
    public void ADisabledCardIsNeitherALinkNorAButtonThatActs()
    {
        BitAiCitationSource? activated = null;

        var link = RenderComponent<BitAiSourceCard>(p => p.Add(c => c.Source, Source()).Add(c => c.Disabled, true));
        var button = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source())
            .Add(c => c.Disabled, true)
            .Add(c => c.OnActivate, s => activated = s));

        Assert.IsTrue(link.Find(".bit-aisrc").ClassList.Contains("bit-dis"));
        Assert.IsNull(link.Find(".bit-aisrc a").GetAttribute("href"));

        button.Find(".bit-crd").Click();
        Assert.IsNull(activated);
    }

    [TestMethod]
    public void BitAiSourceCardShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source())
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px"));

        var root = component.Find(".bit-aisrc");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiSourceCardShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source())
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aisrc").GetAttribute("style");

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
    public void BitAiSourceCardShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source())
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aisrc");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiSourceCardShouldHandTheClassesAndStylesToItsParts()
    {
        var component = RenderComponent<BitAiSourceCard>(p => p
            .Add(c => c.Source, Source())
            .Add(c => c.Number, 1)
            .Add(c => c.Classes, new() { Root = "c-root", Card = new() { Root = "c-card" }, Index = "c-index", Title = "c-title" })
            .Add(c => c.Styles, new() { Root = "color: red", Site = "color: blue" }));

        Assert.IsTrue(component.Find(".bit-aisrc").ClassList.Contains("c-root"));
        StringAssert.Contains(component.Find(".bit-aisrc").GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-crd").ClassList.Contains("c-card"));
        Assert.IsTrue(component.Find(".bit-aisrc-idx").ClassList.Contains("c-index"));
        Assert.IsTrue(component.Find(".bit-aisrc-ttl").ClassList.Contains("c-title"));
        Assert.AreEqual("color: blue", component.Find(".bit-aisrc-sit").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiSourceCardShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiSourceCardParams { Class = "cascaded", Classes = new() { Title = "cascaded-title" } }
            });
            p.AddChildContent<BitAiSourceCard>(c => c.Add(x => x.Source, Source()));
        });

        Assert.IsTrue(component.Find(".bit-aisrc").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aisrc-ttl").ClassList.Contains("cascaded-title"));
    }

    [TestMethod]
    public void BitAiSourceCardParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiSourceCard", BitAiSourceCardParams.ParamName);
        Assert.AreEqual(BitAiSourceCardParams.ParamName, new BitAiSourceCardParams().Name);
    }
}
