using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// The document surface keeps paths and long strings in the browser and refers to them by slot. These tests read
/// the command buffer back opcode by opcode, which also pins the wire format the browser layer replays.
/// </summary>
[TestClass]
public sealed class RetainedRenderingTests
{
    private static BinkDocument Document(int strokes)
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 1000, Height = 1000 });
        for (int i = 0; i < strokes; i++)
        {
            document.AddElement(TestData.Stroke(new Vector2(i * 7 % 900, i * 13 % 900), new Vector2(i * 7 % 900 + 40, i * 13 % 900 + 25)));
        }
        return document;
    }

    private static BinkCanvasRenderContext Record(BinkDocument document, BinkCanvasRenderContext context)
    {
        context.Reset();
        context.Clear(BinkColor.White);
        BinkDocumentRenderer.Render(document, context);
        context.EndFrame();
        return context;
    }

    /// <summary>Counts opcodes, walking the buffer exactly the way the browser layer does.</summary>
    private static Dictionary<BinkCanvasCommand, int> Opcodes(BinkCanvasRenderContext context, List<int>? usedSlots = null)
    {
        ReadOnlySpan<double> c = context.Commands;
        var counts = new Dictionary<BinkCanvasCommand, int>();
        int i = 0;
        while (i < c.Length)
        {
            var op = (BinkCanvasCommand)(byte)c[i++];
            counts[op] = counts.GetValueOrDefault(op) + 1;
            switch (op)
            {
                case BinkCanvasCommand.BeginPath or BinkCanvasCommand.ClosePath or BinkCanvasCommand.Save or BinkCanvasCommand.Restore
                    or BinkCanvasCommand.PopGroup or BinkCanvasCommand.EndPath:
                    break;
                case BinkCanvasCommand.MoveTo or BinkCanvasCommand.LineTo or BinkCanvasCommand.PushGroup or BinkCanvasCommand.Clear
                    or BinkCanvasCommand.RetainString:
                    i += 2;
                    break;
                case BinkCanvasCommand.QuadTo or BinkCanvasCommand.ClipRect or BinkCanvasCommand.ClearRect:
                    i += 4;
                    break;
                case BinkCanvasCommand.Fill:
                    i += 5;
                    break;
                case BinkCanvasCommand.CubicTo or BinkCanvasCommand.Transform or BinkCanvasCommand.SetTransform:
                    i += 6;
                    break;
                case BinkCanvasCommand.Stroke:
                    i += 7;
                    break;
                case BinkCanvasCommand.DrawText:
                    i += 10;
                    break;
                case BinkCanvasCommand.DrawImage:
                    i += 11;
                    break;
                case BinkCanvasCommand.DefinePath:
                    i += 1;
                    break;
                case BinkCanvasCommand.UsePath:
                    usedSlots?.Add((int)c[i]);
                    i += 1;
                    break;
                case BinkCanvasCommand.SetDash or BinkCanvasCommand.FreePaths or BinkCanvasCommand.FreeStrings:
                    i += 1 + (int)c[i];
                    break;
                default:
                    Assert.Fail($"Unknown opcode {op} at {i - 1}.");
                    break;
            }
        }
        Assert.AreEqual(c.Length, i, "The buffer ended part way through a command.");
        return counts;
    }

    [TestMethod]
    public void A_redraw_refers_to_paths_the_browser_already_holds()
    {
        BinkDocument document = Document(50);
        var context = new BinkCanvasRenderContext(1024, new BinkCanvasResourceCache());

        Dictionary<BinkCanvasCommand, int> first = Opcodes(Record(document, context));
        int firstSize = context.Count;
        Assert.AreEqual(50, first[BinkCanvasCommand.DefinePath]);
        Assert.AreEqual(50, first[BinkCanvasCommand.UsePath]);
        Assert.IsFalse(first.ContainsKey(BinkCanvasCommand.BeginPath));

        Dictionary<BinkCanvasCommand, int> second = Opcodes(Record(document, context));
        Assert.IsFalse(second.ContainsKey(BinkCanvasCommand.DefinePath));
        Assert.IsFalse(second.ContainsKey(BinkCanvasCommand.QuadTo));
        Assert.AreEqual(50, second[BinkCanvasCommand.UsePath]);
        Assert.IsLessThan(firstSize / 5, context.Count, "A redraw should cost a few numbers per element.");
    }

    [TestMethod]
    public void A_context_without_a_cache_records_every_path_in_full()
    {
        // Exports and custom hosts record with the public constructor, which must stay self-contained.
        BinkDocument document = Document(3);
        var context = new BinkCanvasRenderContext();
        Dictionary<BinkCanvasCommand, int> counts = Opcodes(Record(document, context));
        Assert.AreEqual(3, counts[BinkCanvasCommand.BeginPath]);
        Assert.IsFalse(counts.ContainsKey(BinkCanvasCommand.DefinePath));
        Assert.IsFalse(counts.ContainsKey(BinkCanvasCommand.UsePath));
    }

    [TestMethod]
    public void Moving_an_element_keeps_its_path_and_editing_it_defines_a_new_one()
    {
        var document = new BinkDocument();
        var rect = new BinkRectangleShape(new BinkRect(10, 10, 100, 50));
        document.AddElement(rect);
        var context = new BinkCanvasRenderContext(1024, new BinkCanvasResourceCache());
        Record(document, context);

        document.ReplaceElement(document.GetElement(rect.Id).Translate(new Vector2(30, 0)));
        Assert.IsFalse(Opcodes(Record(document, context)).ContainsKey(BinkCanvasCommand.DefinePath), "A move only changes the transform.");

        document.ReplaceElement((BinkRectangleShape)document.GetElement(rect.Id) with { Rect = new BinkRect(10, 10, 200, 50) });
        Assert.AreEqual(1, Opcodes(Record(document, context))[BinkCanvasCommand.DefinePath], "A new rectangle is new geometry.");
    }

    [TestMethod]
    public void Shapes_build_their_path_once_per_geometry()
    {
        var rect = new BinkRectangleShape(new BinkRect(0, 0, 10, 10)) { CornerRadius = 2 };
        Assert.AreSame(rect.GetPath(), rect.GetPath());
        BinkRectangleShape moved = rect with { Transform = Matrix3x2.CreateTranslation(5, 5) };
        Assert.AreSame(rect.GetPath(), moved.GetPath());
        BinkRectangleShape resized = rect with { Rect = new BinkRect(0, 0, 20, 10) };
        Assert.AreNotSame(rect.GetPath(), resized.GetPath());
        Assert.AreEqual(20, resized.GetPath().Bounds.Width, 0.001f);
        BinkRectangleShape squared = rect with { CornerRadius = 0 };
        Assert.AreNotSame(rect.GetPath(), squared.GetPath());

        var line = new BinkLineShape(Vector2.Zero, new Vector2(50, 0)) { EndArrow = true };
        BinkPathGeometry arrow = line.GetPath();
        Assert.AreSame(arrow, line.GetPath());
        // The arrow head is sized from the stroke width, so a heavier line is different geometry.
        Assert.AreNotSame(arrow, (line with { Style = line.Style with { StrokeWidth = 6 } }).GetPath());
        Assert.AreNotSame(arrow, (line with { EndArrow = false }).GetPath());

        var ellipse = new BinkEllipseShape(new BinkRect(0, 0, 10, 20));
        Assert.AreSame(ellipse.GetPath(), (ellipse with { Description = "x" }).GetPath());
        Assert.AreNotSame(ellipse.GetPath(), (ellipse with { Rect = new BinkRect(0, 0, 30, 20) }).GetPath());
    }

    [TestMethod]
    public void An_embedded_image_crosses_the_boundary_once()
    {
        var document = new BinkDocument();
        BinkImageSource source = BinkImageSource.FromBytes(new byte[4096], "image/png", 32, 32);
        document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 32, 32)));
        var context = new BinkCanvasRenderContext(1024, new BinkCanvasResourceCache());

        Record(document, context);
        Assert.Contains(source.Uri, context.Strings);
        Assert.AreEqual(1, Opcodes(context)[BinkCanvasCommand.RetainString]);

        Record(document, context);
        Assert.DoesNotContain(source.Uri, context.Strings);
        Assert.IsFalse(Opcodes(context).ContainsKey(BinkCanvasCommand.RetainString));
        Assert.AreEqual(1, Opcodes(context)[BinkCanvasCommand.DrawImage]);
    }

    [TestMethod]
    public void Paths_no_longer_drawn_are_released_and_their_slots_reused()
    {
        var cache = new BinkCanvasResourceCache();
        var context = new BinkCanvasRenderContext(1024, cache);
        var document = new BinkDocument();
        int freed = 0;
        int maxSlot = 0;
        // Every frame draws a different set of strokes, as panning across a large page does.
        for (int frame = 0; frame < 120; frame++)
        {
            if (!document.IsEmpty) document.Clear();
            for (int i = 0; i < 100; i++) document.AddElement(TestData.Stroke(new Vector2(i, frame), new Vector2(i + 20, frame + 20)));
            var used = new List<int>();
            Dictionary<BinkCanvasCommand, int> counts = Opcodes(Record(document, context), used);
            freed += counts.GetValueOrDefault(BinkCanvasCommand.FreePaths);
            maxSlot = Math.Max(maxSlot, used.Max());
        }

        Assert.IsGreaterThan(0, freed, "Paths nobody draws any more must be released.");
        Assert.IsLessThanOrEqualTo(4096, cache.PathCount, "The cache stays within its budget.");
        Assert.IsLessThan(4096 + 100, maxSlot, "Released slots are reused, so slot numbers stay small.");
    }

    [TestMethod]
    public void Clearing_the_cache_defines_everything_again()
    {
        BinkDocument document = Document(10);
        var cache = new BinkCanvasResourceCache();
        var context = new BinkCanvasRenderContext(1024, cache);
        Record(document, context);
        Record(document, context);
        cache.Clear();
        Assert.AreEqual(10, Opcodes(Record(document, context))[BinkCanvasCommand.DefinePath]);
    }

    [TestMethod]
    public void Every_value_survives_the_trip_as_a_32_bit_float()
    {
        // Off WebAssembly the buffer travels as packed floats. A packed ARGB colour does not fit one exactly, which is
        // why colours are split into RGB and alpha.
        var document = new BinkDocument(new BinkDocumentSettings { Background = new BinkColor(250, 251, 252, 253) });
        document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(1.25f, 2.5f), new Vector2(300.75f, 400.125f)]),
            new BinkPenBrush { Color = new BinkColor(255, 254, 253, 252), Opacity = 0.37f }));
        document.AddElement(new BinkRectangleShape(new BinkRect(5, 5, 50, 50))
        {
            Style = new BinkShapeStyle { StrokeColor = new BinkColor(1, 2, 3, 4), FillColor = new BinkColor(255, 255, 255, 1), Dash = [3f, 2f] },
        });
        document.AddElement(new BinkTextElement(new Vector2(20, 20), "Hello") { Style = BinkTextStyle.Default with { Color = new BinkColor(200, 100, 50, 25) } });
        var context = new BinkCanvasRenderContext(1024, new BinkCanvasResourceCache());
        Record(document, context);
        Opcodes(context);

        byte[] bytes = context.ToSingleBytes();
        Assert.AreEqual(context.Count * 4, bytes.Length);
        ReadOnlySpan<double> values = context.Commands;
        for (int i = 0; i < values.Length; i++)
        {
            float packed = BitConverter.ToSingle(bytes, i * 4);
            Assert.AreEqual(values[i], (double)packed, $"Value {i} ({values[i]}) does not survive as a float.");
        }
    }
}
