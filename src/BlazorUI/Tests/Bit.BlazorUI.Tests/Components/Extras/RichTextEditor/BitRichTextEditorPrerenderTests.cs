using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// The editing surface is filled by the bridge once the page is interactive, so before that - a prerender, or a static
/// SSR page for good - it is the component that has to put the value into it, through the same allowlist the bridge
/// applies to everything it writes into the surface.
/// </summary>
[TestClass]
public class BitRichTextEditorPrerenderTests : BunitTestContext
{
    private static async Task<IDocument> PrerenderAsync(string? value, BitRichTextEditorSanitizationPolicy? policy = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            [nameof(BitRichTextEditor.Value)] = value,
        };
        if (policy is not null)
        {
            parameters[nameof(BitRichTextEditor.SanitizationPolicy)] = policy;
        }

        var html = await Prerenderer.RenderAsync<BitRichTextEditor>(parameters);

        // Parsed the way a browser parses the page, so markup that would break out of the surface does so here too.
        return new HtmlParser().ParseDocument($"<!DOCTYPE html><html><body><main>{html}</main><footer id=\"after-editor\"></footer></body></html>");
    }

    private static IElement Surface(IDocument document) => document.QuerySelector(".bit-rte-edt")!;

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderItsValue()
    {
        var document = await PrerenderAsync("<h2>Title</h2><p>Hello <strong>world</strong>, <em>again</em>.</p><ul><li>one</li><li>two</li></ul>");

        Assert.AreEqual("<h2>Title</h2><p>Hello <strong>world</strong>, <em>again</em>.</p><ul><li>one</li><li>two</li></ul>", Surface(document).InnerHtml);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderAnEmptySurfaceWithoutAValue()
    {
        var document = await PrerenderAsync(null);

        Assert.AreEqual(string.Empty, Surface(document).InnerHtml);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldSanitizeThePrerenderedValue()
    {
        var document = await PrerenderAsync(
            "<p onclick=\"steal()\">text<script>alert(1)</script></p>" +
            "<img src=\"photo.png\" onerror=\"alert(1)\" alt=\"Photo\">" +
            "<a href=\"javascript:alert(1)\">bad</a><a href=\" jav&#x09;ascript:alert(1)\">worse</a><a href=\"https://bitplatform.dev\">good</a>" +
            "<iframe src=\"https://evil.test/embed\"></iframe>" +
            "<svg><g onload=\"alert(1)\"></g></svg><math><mi>x</mi></math>" +
            "<style>body{display:none}</style><form action=\"/x\"><input value=\"in\"></form>" +
            "<span style=\"color: red; background: url(https://evil.test/x.png); position: fixed\">styled</span>" +
            "<!-- <script>alert(1)</script> -->" +
            "<font color=\"red\">unwrapped</font>");

        var surface = Surface(document);

        Assert.IsNull(surface.QuerySelector("script, style, svg, math, iframe, form, input, font"));
        Assert.IsFalse(surface.Descendants<IElement>().Any(e => e.Attributes.Any(a => a.Name.StartsWith("on"))));
        Assert.IsFalse(surface.Descendants<IComment>().Any());

        var links = surface.QuerySelectorAll("a").ToArray();
        Assert.AreEqual(3, links.Length);
        Assert.IsFalse(links[0].HasAttribute("href"));
        Assert.IsFalse(links[1].HasAttribute("href"));
        Assert.AreEqual("https://bitplatform.dev", links[2].GetAttribute("href"));

        var image = surface.QuerySelector("img")!;
        Assert.AreEqual("photo.png", image.GetAttribute("src"));
        Assert.AreEqual("Photo", image.GetAttribute("alt"));

        Assert.AreEqual("color: red", surface.QuerySelector("span")!.GetAttribute("style"));
        StringAssert.Contains(surface.TextContent, "unwrapped");
        StringAssert.Contains(surface.TextContent, "text");
        Assert.IsFalse(surface.TextContent.Contains("alert"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderTheValueThroughItsOwnPolicy()
    {
        var policy = new BitRichTextEditorSanitizationPolicy
        {
            AllowedTags = new HashSet<string> { "p", "a" },
            AllowedAttributes = new Dictionary<string, ISet<string>> { ["a"] = new HashSet<string> { "href" } },
            AllowedUriSchemes = new HashSet<string> { "https" },
        };

        var document = await PrerenderAsync("<p><strong>bold</strong> <a href=\"mailto:a@b.c\" title=\"t\">mail</a> <a href=\"https://b.c\">web</a></p>", policy);

        Assert.AreEqual("<p>bold <a>mail</a> <a href=\"https://b.c\">web</a></p>", Surface(document).InnerHtml);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderAnApprovedEmbedAndHardenABlankTarget()
    {
        var document = await PrerenderAsync(
            "<iframe src=\"https://www.youtube-nocookie.com/embed/abc\" width=\"560\" onload=\"x()\">fallback</iframe>" +
            "<p><a href=\"https://bitplatform.dev\" target=\"_blank\">site</a></p>");

        var surface = Surface(document);

        var frame = surface.QuerySelector("iframe")!;
        Assert.AreEqual("https://www.youtube-nocookie.com/embed/abc", frame.GetAttribute("src"));
        Assert.AreEqual("560", frame.GetAttribute("width"));
        Assert.IsFalse(frame.HasAttribute("onload"));
        Assert.AreEqual(string.Empty, frame.InnerHtml);

        Assert.AreEqual("noopener noreferrer", surface.QuerySelector("a")!.GetAttribute("rel"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderEveryTargetThatOpensANewContextHardened()
    {
        var document = await PrerenderAsync(
            "<p><a href=\"https://a.dev\" target=\" _blank\">a</a></p>" +
            "<p><a href=\"https://b.dev\" target=\"_blank \">b</a></p>" +
            "<p><a href=\"https://c.dev\" target=\"named\">c</a></p>" +
            "<p><a href=\"https://d.dev\" target=\"_self\">d</a></p>" +
            "<p><a href=\"https://e.dev\" target=\"_TOP\">e</a></p>");

        var anchors = Surface(document).QuerySelectorAll("a");

        // A padded "_blank" or any name opens another browsing context, which gets window.opener without noopener.
        Assert.AreEqual("noopener noreferrer", anchors[0].GetAttribute("rel"));
        Assert.AreEqual("noopener noreferrer", anchors[1].GetAttribute("rel"));
        Assert.AreEqual("noopener noreferrer", anchors[2].GetAttribute("rel"));
        // The link's own context and its ancestors are not new ones.
        Assert.IsFalse(anchors[3].HasAttribute("rel"));
        Assert.IsFalse(anchors[4].HasAttribute("rel"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderAPosterOnlyFromAnAllowedUrl()
    {
        var document = await PrerenderAsync(
            "<video src=\"https://bitplatform.dev/a.mp4\" poster=\"javascript:alert(1)\"></video>" +
            "<video src=\"https://bitplatform.dev/b.mp4\" poster=\"https://bitplatform.dev/b.png\"></video>");

        var videos = Surface(document).QuerySelectorAll("video");

        Assert.IsFalse(videos[0].HasAttribute("poster"));
        Assert.AreEqual("https://bitplatform.dev/b.png", videos[1].GetAttribute("poster"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderNoStyleThatFetchesThroughAnImageSet()
    {
        var document = await PrerenderAsync(
            "<p style=\"background:image-set('https://evil.dev/t.png' 1x); color: red\">a</p>" +
            "<p style=\"background:-webkit-image-set('https://evil.dev/t.png' 1x)\">b</p>");

        var paragraphs = Surface(document).QuerySelectorAll("p");

        Assert.IsFalse((paragraphs[0].GetAttribute("style") ?? string.Empty).Contains("image-set"));
        Assert.IsFalse((paragraphs[1].GetAttribute("style") ?? string.Empty).Contains("image-set"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderEscapedTextAsText()
    {
        var document = await PrerenderAsync("<p>a &amp; b &lt;img src=x onerror=alert(1)&gt; &quot;c&quot;&nbsp;d</p>");

        var surface = Surface(document);

        Assert.IsNull(surface.QuerySelector("img"));
        Assert.AreEqual("a & b <img src=x onerror=alert(1)> \"c\" d", surface.QuerySelector("p")!.TextContent);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldKeepAMalformedPrerenderedValueInsideTheSurface()
    {
        // Each fragment is one the HTML parser closes something on its own for - a list item a second one ends, a
        // paragraph a block ends, a cell another cell ends, a closing tag nothing opened - which, written back
        // naively, would close the surface itself and spill the rest of the value into the page around it.
        var document = await PrerenderAsync(
            "<ul><li><div>a<li>b</div></li></ul>" +
            "<p><span>c<div>d</div></span></p>" +
            "<li>stray item</li>" +
            "<table><tr><td>e<td>f</tr><div>g</div><tr><th>h</table>" +
            "<h1>i<h2>j</h1>" +
            "</div></div></div><p>tail");

        var surface = Surface(document);
        var root = document.QuerySelector(".bit-rte")!;

        StringAssert.Contains(surface.TextContent, "tail");
        StringAssert.Contains(surface.TextContent, "stray item");
        Assert.IsTrue(root.Contains(surface));
        Assert.IsNotNull(root.QuerySelector(".bit-rte-hint"));
        Assert.IsFalse(surface.Contains(root.QuerySelector(".bit-rte-hint")));
        Assert.AreEqual("MAIN", root.ParentElement!.TagName);
        Assert.IsNull(document.QuerySelector("main + *:not(footer)"));
    }

    [TestMethod]
    [DataRow("<p>a<p>b<div>c</p>d</div>")]
    [DataRow("<b><i>a</b>b</i><p><b>c<div>d</b>e</div>")]
    [DataRow("<ol><li>a<ul><li>b<li>c</ul><li>d</ol><li>e")]
    [DataRow("<h1>a<h2>b</h3>c<p>d<h4>e")]
    [DataRow("<a href=\"https://a.b\">a<a href=\"https://c.d\">b</a>c</a>")]
    [DataRow("<table><td>a<td>b<tr><th>c<caption>d</caption><tbody><col><colgroup><col></table><table><table>e")]
    [DataRow("<table><tr><td><table><td>a</table>b</td>c<td>d</tr>e</table>f")]
    [DataRow("<td>a</td><tr>b</tr><caption>c</caption>")]
    [DataRow("<blockquote><p>a<pre>b</blockquote>c</pre>")]
    [DataRow("<span><p>a</span>b</p><mark><div>c</mark>d")]
    [DataRow("<svg><p>a</p></svg>b<math><mi>c</math><template><p>d</template>e<form><p>f</form>g")]
    [DataRow("<textarea><b>a</b> &amp;</textarea><noframes><i>b</i></noframes><title><p>c</title>d")]
    [DataRow("<p>a</p></li></td></table></div></div></body></html><p>b")]
    [DataRow("<ul><li><p>a<li>b<hr><li><div><span>c<li>d</ul>")]
    [DataRow("<p><img src=\"a.png\"><br><hr><img src=\"b.png\"></p><video src=\"v.mp4\"><source src=\"s.mp4\"></video>")]
    public async Task BitRichTextEditorShouldPrerenderAValueTheBrowserParsesAsWritten(string value)
    {
        var parameters = new Dictionary<string, object?> { [nameof(BitRichTextEditor.Value)] = value };
        var html = await Prerenderer.RenderAsync<BitRichTextEditor>(parameters);

        var surface = new HtmlParser().ParseDocument($"<!DOCTYPE html><html><body>{html}</body></html>").QuerySelector(".bit-rte-edt")!;

        // The tree a browser builds from the output, written back, is the output itself: nothing in it was closed,
        // moved or reopened by the parser, which is what keeps the value inside the surface whatever it holds.
        StringAssert.Contains(html, $">{surface.InnerHtml}</div>");
        Assert.AreNotEqual(string.Empty, surface.InnerHtml);
    }

    [TestMethod]
    public void BitRichTextEditorShouldLeaveTheSurfaceToTheBridgeOnceRendered()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", i => (string?)i.Arguments[1] == "<p>first</p>").SetResult("<p>first</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", i => (string?)i.Arguments[1] == "<p>second</p>").SetResult("<p>second</p>");

        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Value, "<p>first</p>"));

        // The first interactive render carries the value too, and the bridge's first set finds it already there.
        Assert.AreEqual("<p>first</p>", component.Find(".bit-rte-edt").InnerHtml);
        Assert.AreEqual("<p>first</p>", Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.setHtml"].Last().Arguments[1]);

        component.Render(parameters => parameters.Add(p => p.Value, "<p>second</p>"));

        // A later value goes through the bridge alone: Blazor rendering it as well would fight the bridge over the
        // children of a contenteditable surface.
        Assert.AreEqual("<p>second</p>", Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.setHtml"].Last().Arguments[1]);
        Assert.AreEqual("<p>first</p>", component.Find(".bit-rte-edt").InnerHtml);
    }

    private static string Repeat(string value, int count) => string.Concat(Enumerable.Repeat(value, count));

    // Each about 400 KB, and each one that once made the sanitizer scan everything it had read so far for every tag
    // (or every attribute, or every comment) - which on the server, on every prerender, is a request that takes a
    // minute to render. Built in the test, so the test's name does not carry 400 KB of markup.
    private static string AdversarialValue(string name) => name switch
    {
        "deep nesting" => Repeat("<div>", 80_000),
        "blocks under inline elements" => Repeat("<span>", 33_000) + Repeat("<div>", 33_000),
        "end tags nothing opened" => Repeat("<span>", 40_000) + Repeat("</x>", 40_000),
        "list items under inline elements" => "<ul>" + Repeat("<span>", 33_000) + Repeat("<li>", 33_000),
        "list items opened and closed" => "<ul>" + Repeat("<span>", 200) + Repeat("<li></li>", 44_000),
        "distinct attributes" => "<p " + string.Join(" ", Enumerable.Range(0, 60_000).Select(i => $"a{i}")) + ">x</p>",
        "empty comments" => Repeat("<!---->", 57_000),
        "unclosed comment openers" => Repeat("<!--<!-- -", 40_000),
        "links in links" => Repeat("<span><a>", 40_000),
        "cells outside a table" => Repeat("<td>", 100_000),
        _ => throw new System.ArgumentOutOfRangeException(nameof(name)),
    };

    [TestMethod]
    [DataRow("deep nesting")]
    [DataRow("blocks under inline elements")]
    [DataRow("end tags nothing opened")]
    [DataRow("list items under inline elements")]
    [DataRow("list items opened and closed")]
    [DataRow("distinct attributes")]
    [DataRow("empty comments")]
    [DataRow("unclosed comment openers")]
    [DataRow("links in links")]
    [DataRow("cells outside a table")]
    public async Task BitRichTextEditorShouldPrerenderAnAdversarialValueInLinearTime(string name)
    {
        var value = AdversarialValue(name);

        // Warmed up first, so the bound measures the sanitizer and not the JIT.
        await PrerenderAsync("<p>warm</p>");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var document = await PrerenderAsync(value);
        watch.Stop();

        Assert.IsTrue(watch.Elapsed < System.TimeSpan.FromSeconds(10), $"{name}: {watch.Elapsed}");

        var root = document.QuerySelector(".bit-rte")!;
        Assert.IsTrue(root.Contains(Surface(document)), name);
        Assert.IsFalse(Surface(document).Contains(root.QuerySelector(".bit-rte-hint")), name);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderOnlyTheBeginningOfAnOversizedValue()
    {
        var document = await PrerenderAsync("<p>" + new string('x', 1_100_000) + "</p>");

        // Bounded to about a megabyte, the rest left to the bridge - but never an empty surface, which the
        // placeholder would claim is a blank document.
        var html = Surface(document).InnerHtml;
        StringAssert.StartsWith(html, "<p>xxx");
        StringAssert.EndsWith(html, "x</p>");
        Assert.IsLessThanOrEqualTo(1024 * 1024 + "<p></p>".Length, html.Length);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldCutAnOversizedValueBeforeATag()
    {
        var blocks = string.Concat(Enumerable.Repeat("<p>" + new string('x', 1000) + "</p>", 1100));

        var document = await PrerenderAsync(blocks);

        // The cut lands before the last tag that starts within the limit, so no element is split in the middle.
        var html = Surface(document).InnerHtml;
        StringAssert.EndsWith(html, new string('x', 1000) + "</p>");
        Assert.IsLessThan(blocks.Length, html.Length);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPrerenderThroughAPolicyWithMissingSets()
    {
        var policy = new BitRichTextEditorSanitizationPolicy
        {
            AllowedTags = new HashSet<string> { "p", "a", null! },
            AllowedAttributes = new Dictionary<string, ISet<string>> { ["a"] = null!, ["*"] = new HashSet<string> { null!, "title" } },
            AllowedUriSchemes = null!,
            AllowedIframeHosts = [null!],
        };

        var document = await PrerenderAsync("<p title=\"t\"><a href=\"https://b.c\" title=\"l\">web</a></p>", policy);

        // A missing set allows nothing, as an empty one would.
        Assert.AreEqual("<p title=\"t\"><a title=\"l\">web</a></p>", Surface(document).InnerHtml);
    }
}
