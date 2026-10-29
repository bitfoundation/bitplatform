using System.Diagnostics;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Borm.Tests;

/// <summary>
/// Formatted text written in Markdown and rendered safely, and a signature drawn with a pointer or typed - both
/// values of their own kind, read back in a review as what they are.
/// </summary>
[TestClass]
public partial class MarkdownAndSignatureTests : ComponentTestBase
{
    [GeneratedRegex(@"<(/?)([a-z0-9]+)")]
    private static partial Regex Tags();

    private static readonly HashSet<string> Allowed = ["p", "br", "strong", "em", "del", "code", "pre", "a", "ul", "ol", "li", "blockquote", "hr", "h3", "h4", "h5", "h6"];

    private static void OnlyItsOwnElements(string html)
    {
        foreach (Match tag in Tags().Matches(html))
            Assert.Contains(tag.Groups[2].Value, Allowed, $"<{tag.Groups[2].Value}> is not one of the elements it writes: {html}");
    }

    [TestMethod]
    public void The_usual_formatting_renders_as_its_elements()
    {
        var html = BormMarkdown.ToHtml("""
            # Release notes
            Fixed **two** bugs and made *one* thing `faster`.
            Second line.

            - one
            - two

            1. first
            2) second

            > quoted

            ---
            ```
            <code> & more
            ```
            See [the docs](https://bitplatform.dev/borm) or ~~old~~.
            """);

        StringAssert.Contains(html, "<h3>Release notes</h3>");
        StringAssert.Contains(html, "<p>Fixed <strong>two</strong> bugs and made <em>one</em> thing <code>faster</code>.<br>Second line.</p>");
        StringAssert.Contains(html, "<ul><li>one</li><li>two</li></ul>");
        StringAssert.Contains(html, "<ol><li>first</li><li>second</li></ol>");
        StringAssert.Contains(html, "<blockquote><p>quoted</p></blockquote>");
        StringAssert.Contains(html, "<hr>");
        StringAssert.Contains(html, "<pre><code>&lt;code&gt; &amp; more</code></pre>");
        StringAssert.Contains(html, "<a href=\"https://bitplatform.dev/borm\" rel=\"nofollow noopener noreferrer\">the docs</a>");
        StringAssert.Contains(html, "<del>old</del>");
        OnlyItsOwnElements(html);
    }

    [TestMethod]
    [DataRow("<script>alert(1)</script>")]
    [DataRow("<img src=x onerror=alert(1)>")]
    [DataRow("[click](javascript:alert(1))")]
    [DataRow("[click](JaVaScRiPt:alert(1))")]
    [DataRow("[click](data:text/html,<script>alert(1)</script>)")]
    [DataRow("[click](https://a.b/\"onmouseover=alert(1))")]
    [DataRow("[click](https://a.b onmouseover=alert(1))")]
    [DataRow("**<svg onload=alert(1)>**")]
    [DataRow("`<iframe src=//evil>`")]
    [DataRow("&lt;script&gt;")]
    [DataRow("[<img src=x onerror=alert(1)>](https://a.b)")]
    public void Nothing_the_user_writes_reaches_the_page_as_markup(string hostile)
    {
        var html = BormMarkdown.ToHtml(hostile);

        OnlyItsOwnElements(html);
        // Left as the text it is - but never an address a link goes to.
        Assert.DoesNotContain("href=\"javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(Regex.IsMatch(html, @"<[^>]*\son[a-z]+\s*=", RegexOptions.IgnoreCase), html);
        Assert.IsFalse(Regex.IsMatch(html, "href=\"(?!https?://|mailto:)", RegexOptions.IgnoreCase), html);
    }

    [TestMethod]
    public void Hostile_text_costs_no_more_than_its_length()
    {
        var clock = Stopwatch.StartNew();
        BormMarkdown.ToHtml(new string('*', 50_000));
        BormMarkdown.ToHtml(string.Concat(Enumerable.Repeat("[a](", 10_000)));
        BormMarkdown.ToHtml(new string('>', 20_000) + " deep");
        BormMarkdown.ToHtml(string.Concat(Enumerable.Repeat("**_~~`", 10_000)));
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(2), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void Brackets_in_front_of_an_address_that_is_refused_cost_no_more_than_their_length()
    {
        var clock = Stopwatch.StartNew();
        var html = BormMarkdown.ToHtml(new string('[', 200_000) + "](javascript:x)");
        BormMarkdown.ToHtml(string.Concat(Enumerable.Repeat("[", 100_000)) + "](" + new string('a', 100_000) + ")");

        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(2), $"took {clock.Elapsed}");
        Assert.DoesNotContain("href", html);
        // A link that is one still is.
        StringAssert.Contains(BormMarkdown.ToHtml("[[a](javascript:x) [b](https://x.y)"), "<a href=\"https://x.y\"");
    }

    [TestMethod]
    public void Link_openers_that_share_one_far_parenthesis_cost_no_more_than_their_length()
    {
        var clock = Stopwatch.StartNew();
        BormMarkdown.ToHtml(string.Concat(Enumerable.Repeat("[a](x", 40_000)) + ")");
        BormMarkdown.ToHtml(string.Concat(Enumerable.Repeat("[a](https://x", 20_000)) + new string(' ', 100_000) + ")");

        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(2), $"took {clock.Elapsed}");
        StringAssert.Contains(BormMarkdown.ToHtml("[a](  https://x.y  )"), "<a href=\"https://x.y\"");
        Assert.DoesNotContain("href", BormMarkdown.ToHtml("[a](https://x y)"));
    }

    [TestMethod]
    public void Plain_text_is_the_words_without_the_formatting()
        => Assert.AreEqual("Fixed two bugs. See the docs .", BormMarkdown.ToPlainText("Fixed **two** bugs.\n\nSee [the docs](https://x.y)."));

    [TestMethod]
    public void A_markdown_field_is_a_text_area_with_a_preview_and_reads_back_formatted()
    {
        var form = BormBuilder.Create()
            .Text("notes", f => f.Label("Notes").AsMarkdown())
            .Review("summary")
            .Build();
        Assert.AreEqual(BormUpdateTrigger.Input, form.Fields[0].UpdateOn);
        var data = new Dictionary<string, object?> { ["notes"] = "Hello **world**" };
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        Assert.IsNotNull(cut.Find("textarea#borm_notes"));
        var preview = cut.Find("#borm_notes_preview");
        Assert.IsTrue(preview.HasAttribute("hidden"));

        var toggle = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Show preview");
        Assert.AreEqual("borm_notes_preview", toggle.GetAttribute("aria-controls"));
        toggle.Click();

        Assert.IsFalse(cut.Find("#borm_notes_preview").HasAttribute("hidden"));
        Assert.AreEqual("<p>Hello <strong>world</strong></p>", cut.Find("#borm_notes_preview").InnerHtml.Trim());
        Assert.AreEqual("true", cut.FindAll("button").Single(b => b.TextContent.Trim() == "Hide preview").GetAttribute("aria-expanded"));
        StringAssert.Contains(cut.Find(".bit-borm-review").InnerHtml, "<strong>world</strong>");

        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        Assert.AreEqual(BormFieldType.Markdown, back.Fields[0].Type);
    }

    [TestMethod]
    public void A_drawn_signature_is_an_svg_image_cropped_to_what_was_drawn()
    {
        var value = BormSignature.FromStrokes([[(100, 50), (110, 60), (120, 50)], [(130, 55)]]);

        Assert.IsTrue(BormSignature.IsImage(value));
        var svg = BormSignature.ToSvg(value)!;
        StringAssert.Contains(svg, "viewBox='0 0 46 26'");
        StringAssert.Contains(svg, "d='M8 8L18 18L28 8M38 13L38.5 13'");
        Assert.IsNull(BormSignature.FromStrokes([]));
    }

    [TestMethod]
    public void A_typed_signature_is_the_name_in_a_handwriting_face_and_its_markup_is_escaped()
    {
        var svg = BormSignature.ToSvg(BormSignature.FromTypedName("Ada <script>alert(1)</script>"))!;

        StringAssert.Contains(svg, "Ada &lt;script&gt;alert(1)&lt;/script&gt;");
        Assert.DoesNotContain("<script", svg, StringComparison.Ordinal);
        Assert.IsNull(BormSignature.FromTypedName("   "));
    }

    [TestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("data:text/html;base64,PHNjcmlwdD4=")]
    [DataRow("https://evil.example/x.svg")]
    [DataRow("")]
    public void Only_a_value_in_the_shape_it_writes_is_shown_as_a_signature(string value)
        => Assert.IsFalse(BormSignature.IsImage(value));

    [TestMethod]
    [DataRow("<svg onload=alert(1)>")]
    [DataRow("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
    [DataRow("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10' width='10' height='10'><path d='M1 1' fill='none' stroke='#1a1a1a' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' onload='alert(1)'/></svg>")]
    [DataRow("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 120 72' width='120' height='72'><text x='12' y='48' font-family='Segoe Script, Brush Script MT, Lucida Handwriting, cursive' font-size='34' fill='#1a1a1a'>Ada</text><image href='https://evil.example/x.svg'/></svg>")]
    [DataRow("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 120 72' width='120' height='72'><text x='12' y='48' font-family='Segoe Script, Brush Script MT, Lucida Handwriting, cursive' font-size='34' fill='#1a1a1a'><tspan onclick='alert(1)'>Ada</tspan></text></svg>")]
    public void An_svg_that_only_starts_like_a_signature_is_not_one(string svg)
    {
        var value = BormSignature.DataUrlPrefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));

        Assert.IsFalse(BormSignature.IsImage(value));
        Assert.IsNull(BormSignature.ToSvg(value));
        // What this library writes is still what it shows.
        Assert.IsTrue(BormSignature.IsImage(BormSignature.FromTypedName("Ada \"Countess\" O'Lovelace & co <3")));
        Assert.IsTrue(BormSignature.IsImage(BormSignature.FromStrokes([[(0.25, 1.75), (300.5, 120)]])));
    }

    [TestMethod]
    public void A_signature_is_drawn_stroke_by_stroke_typed_instead_or_cleared()
    {
        var form = BormBuilder.Create().Text("sign", f => f.Label("Signature").AsSignature().Required()).Review("summary").Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        // A group labelled by the field's label: there is no single control for a <label for> to name.
        var group = cut.Find("#borm_sign");
        Assert.AreEqual("group", group.GetAttribute("role"));
        Assert.AreEqual("borm_sign_label", group.GetAttribute("aria-labelledby"));

        var pad = cut.Find(".bit-borm-signature__pad");
        pad.PointerDown(new PointerEventArgs { OffsetX = 10, OffsetY = 10 });
        pad.PointerMove(new PointerEventArgs { OffsetX = 40, OffsetY = 30 });
        pad.PointerUp(new PointerEventArgs { OffsetX = 40, OffsetY = 30 });

        Assert.IsTrue(BormSignature.IsImage(data["sign"] as string));
        Assert.HasCount(1, cut.FindAll(".bit-borm-signature__pad path"));
        StringAssert.Contains(cut.Find(".bit-borm-review img").GetAttribute("alt"), "Signature: signature");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Type it instead").Click();
        cut.Find(".bit-borm-signature__typed").Change("Ada Lovelace");
        StringAssert.Contains(BormSignature.ToSvg(data["sign"] as string)!, ">Ada Lovelace</text>");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clear").Click();
        Assert.IsNull(data["sign"]);

        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        Assert.AreEqual(BormFieldType.Signature, back.Fields[0].Type);
    }
}
