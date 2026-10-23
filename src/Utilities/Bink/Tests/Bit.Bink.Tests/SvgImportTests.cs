using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Importing SVG: shapes, styles, transforms, what is refused and what is reported.</summary>
[TestClass]
public sealed class SvgImportTests
{
    private static BinkSvgImportResult Import(string body, string root = "") => BinkSvgImporter.Import($"""<svg xmlns="http://www.w3.org/2000/svg" {root}>{body}</svg>""");

    [TestMethod]
    public void Basic_shapes_become_editable_elements_with_their_styles()
    {
        BinkSvgImportResult result = Import("""
            <rect x="10" y="20" width="100" height="50" rx="8" fill="#ff0000" stroke="#0000ff" stroke-width="3"/>
            <circle cx="50" cy="50" r="20" fill="none" stroke="green"/>
            <ellipse cx="0" cy="0" rx="30" ry="10"/>
            <line x1="0" y1="0" x2="100" y2="0" stroke="black" stroke-linecap="square" stroke-dasharray="4 2"/>
            <polyline points="0,0 10,10 20,0" stroke="black" fill="none"/>
            <polygon points="0 0 10 0 5 8" fill="gold" opacity="0.5"/>
            <path d="M0 0 L10 0 L10 10 Z" fill-rule="evenodd" stroke-linejoin="round" stroke="black"/>
            """);

        Assert.IsEmpty(result.Warnings);
        Assert.HasCount(7, result.Elements);
        var rect = (BinkRectangleShape)result.Elements[0];
        Assert.AreEqual(new BinkRect(10, 20, 100, 50), rect.Rect);
        Assert.AreEqual(8f, rect.CornerRadius);
        Assert.AreEqual(BinkColor.Parse("#ff0000"), rect.Style.FillColor);
        Assert.AreEqual(BinkColor.Parse("#0000ff"), rect.Style.StrokeColor);
        Assert.AreEqual(3f, rect.Style.StrokeWidth);
        Assert.AreEqual(BinkLineJoin.Miter, rect.Style.LineJoin, "SVG's default join is miter, unlike Bink's.");

        var circle = (BinkEllipseShape)result.Elements[1];
        Assert.IsNull(circle.Style.FillColor);
        Assert.AreEqual(new BinkRect(30, 30, 40, 40), circle.Rect);
        Assert.AreEqual(0f, ((BinkEllipseShape)result.Elements[2]).Style.StrokeWidth, "No stroke unless one is given.");

        var line = (BinkLineShape)result.Elements[3];
        Assert.AreEqual(BinkLineCap.Square, line.Style.LineCap);
        CollectionAssert.AreEqual(new[] { 4f, 2f }, line.Style.Dash);

        Assert.IsFalse(((BinkPolygonShape)result.Elements[4]).IsClosed);
        var polygon = (BinkPolygonShape)result.Elements[5];
        Assert.IsTrue(polygon.IsClosed);
        Assert.AreEqual(0.5f, polygon.Style.Opacity);

        var path = (BinkPathElement)result.Elements[6];
        Assert.AreEqual(BinkFillRule.EvenOdd, path.FillRule);
        Assert.AreEqual(BinkLineJoin.Round, path.Style.LineJoin);
    }

    [TestMethod]
    public void The_view_box_maps_onto_the_stated_size()
    {
        BinkSvgImportResult result = Import("""<rect x="1" y="1" width="8" height="8"/>""", """width="100" height="100" viewBox="0 0 10 10" """);
        Assert.AreEqual(100f, result.Width);
        var rect = (BinkRectangleShape)result.Elements.Single();
        Assert.AreEqual(10f, rect.Transform.M11, 1e-5f);
        Assert.AreEqual(new Vector2(10, 10), Vector2.Transform(rect.Rect.TopLeft, rect.Transform));
        Assert.AreEqual(new Vector2(90, 90), Vector2.Transform(rect.Rect.TopLeft + rect.Rect.Size, rect.Transform));

        // Proportions are kept and the content centred, as a browser does by default.
        var wide = (BinkRectangleShape)Import("""<rect width="10" height="10"/>""", """width="200" height="100" viewBox="0 0 10 10" """).Elements.Single();
        Assert.AreEqual(new Vector2(50, 0), Vector2.Transform(wide.Rect.TopLeft, wide.Transform));
        Assert.AreEqual(new Vector2(150, 100), Vector2.Transform(wide.Rect.TopLeft + wide.Rect.Size, wide.Transform));
    }

    [TestMethod]
    public void Groups_pass_down_paint_and_transforms_and_the_style_attribute_wins()
    {
        BinkSvgImportResult result = Import("""
            <g fill="red" stroke="blue" transform="translate(10,20) scale(2)" opacity="0.5">
              <rect width="5" height="5" style="fill: #00ff00; stroke-width: 2px"/>
              <g transform="rotate(90)"><circle r="1" opacity="0.5"/></g>
            </g>
            """);
        var rect = (BinkRectangleShape)result.Elements[0];
        Assert.AreEqual(BinkColor.Parse("#00ff00"), rect.Style.FillColor);
        Assert.AreEqual(BinkColor.Parse("#0000ff"), rect.Style.StrokeColor);
        Assert.AreEqual(2f, rect.Style.StrokeWidth);
        Assert.AreEqual(new Vector2(10, 20), Vector2.Transform(Vector2.Zero, rect.Transform));
        Assert.AreEqual(new Vector2(20, 20), Vector2.Transform(new Vector2(5, 0), rect.Transform));
        Assert.AreEqual(0.5f, rect.Style.Opacity);

        var circle = (BinkEllipseShape)result.Elements[1];
        Assert.AreEqual(BinkColor.Parse("#ff0000"), circle.Style.FillColor);
        Assert.AreEqual(0.25f, circle.Style.Opacity, 1e-5f, "A group's opacity multiplies into what it holds.");
        Vector2 rotated = Vector2.Transform(new Vector2(1, 0), circle.Transform);
        Assert.AreEqual(10f, rotated.X, 1e-4f);
        Assert.AreEqual(22f, rotated.Y, 1e-4f, "Rotated a quarter turn inside the group's scale and translation.");
    }

    [TestMethod]
    public void Text_keeps_its_content_size_colour_and_anchor()
    {
        var text = (BinkTextElement)Import("""<text x="50" y="40" font-size="20" fill="blue" font-weight="bold" text-anchor="middle">Hello <tspan>world</tspan></text>""").Elements.Single();
        Assert.AreEqual("Hello world", text.Text);
        Assert.AreEqual(20f, text.Style.FontSize);
        Assert.AreEqual(BinkColor.Parse("#0000ff"), text.Style.Color);
        Assert.IsTrue(text.Style.Bold);
        Assert.AreEqual(BinkTextAlign.Center, text.Style.Align);
        Assert.AreEqual(50f, text.Position.X);
        Assert.IsLessThan(40f, text.Position.Y, "The baseline is below the top of the line.");
    }

    [TestMethod]
    public void What_has_no_equivalent_is_reported_not_imported_silently()
    {
        BinkSvgImportResult result = Import("""
            <defs><linearGradient id="g"><stop offset="0" stop-color="red"/></linearGradient></defs>
            <style>rect { fill: red }</style>
            <rect width="10" height="10" fill="url(#g)"/>
            <rect width="10" height="10" fill="url(#g) #123456"/>
            <rect width="10" height="10" filter="url(#f)"/>
            <use href="#a"/>
            <rect width="10" height="10" style="display:none"/>
            <rect width="10" height="10" visibility="hidden"/>
            """);
        Assert.HasCount(3, result.Elements, "Hidden shapes and the use element are left out.");
        Assert.AreEqual(new BinkColor(128, 128, 128), ((BinkRectangleShape)result.Elements[0]).Style.FillColor);
        Assert.AreEqual(BinkColor.Parse("#123456"), ((BinkRectangleShape)result.Elements[1]).Style.FillColor, "The fallback colour of a paint reference is used.");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("Gradients")));
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("stylesheets")));
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("Filters")));
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("use elements")));
    }

    [TestMethod]
    public void Hostile_svg_is_refused_and_nothing_is_fetched()
    {
        const string entities = """<?xml version="1.0"?><!DOCTYPE svg [<!ENTITY a "aaaaaaaaaa"><!ENTITY b "&a;&a;&a;&a;&a;">]><svg xmlns="http://www.w3.org/2000/svg"><text>&b;</text></svg>""";
        Assert.IsFalse(BinkSvgImporter.TryImport(entities, out _, out string? error), "The DTD is skipped unread, so its entities are never declared, let alone expanded.");
        Assert.Contains("XML", error!);

        BinkSvgImportResult result = Import("""
            <script>alert(1)</script>
            <foreignObject><div xmlns="http://www.w3.org/1999/xhtml">x</div></foreignObject>
            <image href="https://example.com/tracker.png" width="10" height="10"/>
            <image href="file:///etc/passwd" width="10" height="10"/>
            <image href="data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=" width="10" height="10"/>
            <rect width="10" height="10" onclick="alert(1)"/>
            """);
        Assert.HasCount(1, result.Elements, "Only the rectangle: scripts, foreign content and images by reference are not imported.");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("Images")));

        Assert.IsFalse(BinkSvgImporter.TryImport("<html/>", out _, out error));
        Assert.Contains("root", error!);
        Assert.IsFalse(BinkSvgImporter.TryImport("<svg", out _, out _));
    }

    [TestMethod]
    public void A_doctype_as_illustrators_write_it_is_skipped()
    {
        const string svg = """
            <?xml version="1.0" encoding="utf-8"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"><rect width="10" height="10"/></svg>
            """;
        Assert.IsTrue(BinkSvgImporter.TryImport(svg, out BinkSvgImportResult? result, out string? error), error);
        Assert.HasCount(1, result!.Elements);
    }

    [TestMethod]
    public void Content_after_skipped_foreign_markup_keeps_its_place_in_the_tree()
    {
        // Minified, as editors save it: the foreign element is the group's last child, right before its end tag.
        BinkSvgImportResult result = BinkSvgImporter.Import("""<svg xmlns="http://www.w3.org/2000/svg" xmlns:x="urn:x"><g transform="translate(100 0)" fill="red"><x:meta/></g><rect id="b" width="10" height="10"/><x:meta/><rect width="5" height="5"/></svg>""");

        Assert.HasCount(2, result.Elements, "Neither rectangle is swallowed by the skip.");
        var b = (BinkRectangleShape)result.Elements[0];
        Assert.IsTrue(b.Transform.IsIdentity, "It is outside the group, so not moved by it.");
        Assert.AreEqual(BinkColor.Black, b.Style.FillColor, "Nor painted by it.");
    }

    [TestMethod]
    public void Content_hidden_by_a_collapsing_transform_is_not_imported()
    {
        BinkSvgImportResult result = Import("""<rect width="10" height="10" transform="scale(0)"/><g transform="scale(1 0)"><circle r="5"/></g><rect width="4" height="4"/>""");
        Assert.HasCount(1, result.Elements, "SVG does not render an element whose transform cannot be inverted.");
        Assert.AreEqual(new BinkRect(0, 0, 4, 4), ((BinkRectangleShape)result.Elements[0]).Rect);
    }

    [TestMethod]
    public void Numbers_run_together_split_as_path_data_does()
    {
        var polygon = (BinkPolygonShape)Import("""<polygon points="0,0 1.5.5 10-10 .5.5"/>""").Elements.Single();
        CollectionAssert.AreEqual(new[] { new Vector2(0, 0), new Vector2(1.5f, 0.5f), new Vector2(10, -10), new Vector2(0.5f, 0.5f) }, polygon.Points.ToArray());
    }

    [TestMethod]
    public void A_path_longer_than_a_path_element_holds_is_left_out_alone()
    {
        string huge = "M0 0" + string.Concat(Enumerable.Repeat(" L1 1", BinkPathElement.MaxSegments + 1));
        BinkSvgImportResult result = BinkSvgImporter.Import($"""<svg xmlns="http://www.w3.org/2000/svg"><path d="{huge}"/><rect width="4" height="4"/></svg>""");

        Assert.HasCount(1, result.Elements, "The rectangle is still imported.");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains($"{BinkPathElement.MaxSegments} segments")));
    }

    [TestMethod]
    public void An_embedded_raster_image_is_imported()
    {
        string png = Convert.ToBase64String(TestData.CreatePng(4, 4, (_, _) => BinkColor.White));
        var image = (BinkImageElement)Import($"""<image x="5" y="6" width="40" height="30" href="data:image/png;base64,{png}"/>""").Elements.Single();
        Assert.AreEqual(new BinkRect(5, 6, 40, 30), image.Rect);
        Assert.IsTrue(image.Source.IsEmbedded);
    }

    [TestMethod]
    public void Size_limits_are_enforced()
    {
        string many = string.Concat(Enumerable.Repeat("""<rect width="1" height="1"/>""", 20));
        Assert.IsFalse(BinkSvgImporter.TryImport($"""<svg xmlns="http://www.w3.org/2000/svg">{many}</svg>""", out _, out string? error, new BinkSvgImportOptions { MaxElements = 10 }));
        Assert.Contains("10 elements", error!);

        string path = "M0 0" + string.Concat(Enumerable.Repeat(" L1 1", 100));
        Assert.IsFalse(BinkSvgImporter.TryImport($"""<svg xmlns="http://www.w3.org/2000/svg"><path d="{path}"/></svg>""", out _, out error, new BinkSvgImportOptions { MaxSegments = 50 }));
        Assert.Contains("50 segments", error!);

        Assert.IsFalse(BinkSvgImporter.TryImport(new string(' ', 100) + "<svg/>", out _, out error, new BinkSvgImportOptions { MaxLength = 50 }));
    }

    [TestMethod]
    public void An_imported_drawing_saves_and_exports_like_any_other()
    {
        BinkSvgImportResult result = Import("""<g transform="translate(5 5)"><path d="M0 0 C 10 0 10 10 0 10 Z" fill="#336699"/><rect width="4" height="4" stroke="black"/></g>""");
        var document = new BinkDocument();
        foreach (BinkElement element in result.Elements) document.AddElement(element);

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));
        Assert.AreEqual(2, loaded.ElementCount);
        Assert.AreEqual(result.Elements[0].Bounds, loaded.AllElements.First().Bounds);
        Assert.Contains("#336699", BinkSvgExporter.Export(document));
    }
}
