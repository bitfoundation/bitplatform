using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Bit.Bheadless.Tests.Testing;

/// <summary>The Tab sequence (rendering, focusability and disabled rules) of Bit.Bheadless.Testing.</summary>
[TestClass]
public class FocusOrderTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Body(string html) => Parser.ParseDocument($"<body>{html}</body>").Body!;

    private static string[] Stops(string html) => BitBhFocusOrder.TabSequence(Body(html)).Select(s => s.Element.Id ?? string.Empty).ToArray();

    [TestMethod]
    public void Aria_hidden_content_stays_in_the_tab_order()
    {
        // Focus-trap sentinels are hidden from assistive tech, yet browsers Tab into them.
        var stops = Stops("<span id='start' tabindex='0' aria-hidden='true'></span><button id='ok'>OK</button><div aria-hidden='true'><span id='end' tabindex='0'></span></div>");
        Assert.AreSequenceEqual(new[] { "start", "ok", "end" }, stops);
    }

    [TestMethod]
    public void Unrendered_content_is_skipped()
    {
        var stops = Stops(
            "<div style='display: none'><button id='display-none'>x</button></div>" +
            "<div style='visibility:hidden'><button id='visibility-hidden'>x</button></div>" +
            "<div style='visibility: hidden'><button id='visible-again' style='visibility: visible'>x</button></div>" +
            "<div hidden><button id='hidden'>x</button></div>" +
            "<div inert><button id='inert'>x</button></div>" +
            "<dialog><button id='closed-dialog'>x</button></dialog>" +
            "<dialog open><button id='open-dialog'>x</button></dialog>");
        Assert.AreSequenceEqual(new[] { "visible-again", "open-dialog" }, stops);
    }

    [TestMethod]
    public void Closed_details_expose_only_their_summary()
    {
        const string content = "<summary id='summary'>More</summary><button id='inner'>Inner</button><summary id='second'>Again</summary>";
        Assert.AreSequenceEqual(new[] { "summary" }, Stops($"<details>{content}</details>"));
        Assert.AreSequenceEqual(new[] { "summary", "inner" }, Stops($"<details open>{content}</details>"));
        Assert.IsEmpty(Stops("<summary id='lone'>Not a disclosure</summary>"));
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("-")]
    [DataRow("x1")]
    public void Invalid_tabindex_is_ignored(string value)
    {
        Assert.IsEmpty(Stops($"<div id='d' tabindex='{value}'>x</div>"));
        Assert.IsFalse(BitBhAria.IsFocusable(Body($"<div tabindex='{value}'>x</div>").QuerySelector("div")!));
        // On a natively focusable element an invalid value leaves it in the order.
        Assert.AreSequenceEqual(new[] { "b" }, Stops($"<button id='b' tabindex='{value}'>x</button>"));
    }

    [TestMethod]
    public void Tabindex_follows_html_integer_parsing()
        => Assert.AreSequenceEqual(new[] { "two", "plain" }, Stops("<div id='plain' tabindex=' 0'>a</div><div id='two' tabindex='2px'>b</div><div id='negative' tabindex='-1x'>c</div>"));

    [TestMethod]
    public void Disabled_fieldsets_disable_controls_outside_their_first_legend_child()
    {
        var stops = Stops(
            "<fieldset disabled><legend><button id='first-legend'>a</button></legend><button id='outside'>b</button>" +
            "<legend><button id='second-legend'>c</button></legend><div><legend><button id='nested-legend'>d</button></legend></div></fieldset>" +
            "<fieldset disabled><legend>L</legend><fieldset><legend><button id='inner-fieldset-legend'>e</button></legend></fieldset></fieldset>" +
            "<fieldset disabled><legend><fieldset disabled><legend><button id='both-first-legends'>f</button></legend></fieldset></legend></fieldset>");
        Assert.AreSequenceEqual(new[] { "first-legend", "both-first-legends" }, stops);
    }
}
