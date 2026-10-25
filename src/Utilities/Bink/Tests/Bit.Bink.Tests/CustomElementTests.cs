using System.Numerics;
using System.Text.Json;

namespace Bit.Bink.Tests;

/// <summary>An application's own element: a sticky note with a colour and a vote count.</summary>
internal sealed record StickyNote : BinkElement
{
    public StickyNote(BinkRect rect, string text)
    {
        Rect = rect;
        Text = text;
    }

    public BinkRect Rect { get; init; }

    public string Text { get; init; }

    public int Votes { get; init; }

    public override BinkRect LocalBounds => Rect;

    public override string TypeName => "Sticky note";

    protected override void DrawCore(IBinkRenderContext context)
        => context.FillPath(new BinkPathBuilder(6).AddRect(Rect).ToGeometry(), new BinkFillPaint(new BinkColor(254, 240, 138)));

    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => Rect.Inflate(tolerance).Contains(localPoint);
}

internal sealed class StickyNoteConverter : BinkElementConverter<StickyNote>
{
    public override string TypeName => "acme-sticky";

    public override void Write(Utf8JsonWriter writer, StickyNote element, BinkSerializerOptions options)
    {
        writer.WriteStartArray("rect");
        writer.WriteNumberValue(element.Rect.X);
        writer.WriteNumberValue(element.Rect.Y);
        writer.WriteNumberValue(element.Rect.Width);
        writer.WriteNumberValue(element.Rect.Height);
        writer.WriteEndArray();
        writer.WriteString("text", element.Text);
        if (element.Votes != 0) writer.WriteNumber("votes", element.Votes);
    }

    public override StickyNote Read(JsonElement json, BinkDeserializerOptions options)
    {
        float[] r = json.GetProperty("rect").EnumerateArray().Select(v => v.GetSingle()).ToArray();
        if (r.Length != 4) throw new FormatException("A sticky note needs a rectangle.");
        string text = json.TryGetProperty("text", out JsonElement t) ? t.GetString() ?? "" : "";
        if (text.Length > 1000) throw new FormatException("Too much text for a sticky note.");
        return new StickyNote(new BinkRect(r[0], r[1], r[2], r[3]), text)
        {
            Votes = json.TryGetProperty("votes", out JsonElement v) ? Math.Clamp(v.GetInt32(), 0, 1000) : 0,
        };
    }
}

/// <summary>Custom element types saved, loaded, pasted and synchronized through a registered converter.</summary>
[TestClass]
public sealed class CustomElementTests
{
    private static readonly BinkElementRegistry Registry = CreateRegistry();

    private static BinkElementRegistry CreateRegistry()
    {
        var registry = new BinkElementRegistry();
        registry.Register(new StickyNoteConverter());
        return registry;
    }

    private static readonly BinkSerializerOptions Write = new() { ElementRegistry = Registry };
    private static readonly BinkDeserializerOptions Read = new() { ElementRegistry = Registry };

    private static (BinkDocument Document, StickyNote Note) WithNote()
    {
        var document = new BinkDocument();
        var note = new StickyNote(new BinkRect(10, 20, 120, 80), "Ship it") { Votes = 3, Transform = Matrix3x2.CreateRotation(0.1f), Description = "A note", IsLocked = true };
        document.AddElement(note);
        return (document, note);
    }

    [TestMethod]
    public void A_registered_element_round_trips_with_its_own_and_its_shared_fields()
    {
        (BinkDocument document, StickyNote note) = WithNote();
        Guid group = document.GroupElements([note.Id, AddStroke(document)])!.Value;

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document, Write), Read);

        var copy = (StickyNote)loaded.GetElement(note.Id);
        Assert.AreEqual("Ship it", copy.Text);
        Assert.AreEqual(3, copy.Votes);
        Assert.AreEqual(note.Rect, copy.Rect);
        Assert.AreEqual(note.Transform.M12, copy.Transform.M12, 1e-5f);
        Assert.AreEqual("A note", copy.Description);
        Assert.IsTrue(copy.IsLocked);
        Assert.AreEqual(group, copy.GroupIds.Single());
    }

    private static Guid AddStroke(BinkDocument document)
    {
        BinkStroke stroke = TestData.Stroke(Vector2.Zero, new Vector2(10, 10));
        document.AddElement(stroke);
        return stroke.Id;
    }

    [TestMethod]
    public void A_reader_without_the_registration_keeps_the_element_in_place_and_writes_it_back()
    {
        (BinkDocument document, StickyNote note) = WithNote();
        string json = BinkSerializer.Serialize(document, Write);

        BinkDocument elsewhere = BinkSerializer.Deserialize(json);
        var unknown = (BinkUnknownElement)elsewhere.GetElement(note.Id);
        Assert.AreEqual("acme-sticky", unknown.SerializedTypeName);
        Assert.AreEqual(note.Bounds.Center.X, unknown.Bounds.Center.X, 0.01f, "Placed where it was, through its bounds and transform.");

        BinkDocument back = BinkSerializer.Deserialize(BinkSerializer.Serialize(elsewhere), Read);
        Assert.AreEqual(3, ((StickyNote)back.GetElement(note.Id)).Votes, "Nothing was lost on the way through.");
    }

    [TestMethod]
    public void An_unregistered_custom_element_still_cannot_be_written()
    {
        (BinkDocument document, _) = WithNote();
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => BinkSerializer.Serialize(document));
        Assert.Contains("BinkElementConverter", ex.Message);
    }

    [TestMethod]
    public void A_converter_s_complaint_is_a_malformed_document()
    {
        const string json = """{"format":"bink","version":1,"layers":[{"elements":[{"type":"acme-sticky","rect":[1,2]}]}]}""";
        Assert.IsFalse(BinkSerializer.TryDeserialize(json, out _, out string? error, Read));
        Assert.Contains("acme-sticky", error!);
        Assert.Contains("rectangle", error!);

        const string wrongType = """{"format":"bink","version":1,"layers":[{"elements":[{"type":"acme-sticky","rect":"oops"}]}]}""";
        Assert.IsFalse(BinkSerializer.TryDeserialize(wrongType, out _, out _, Read), "Whatever the converter throws is reported, not raised.");

        const string far = """{"format":"bink","version":1,"layers":[{"elements":[{"type":"acme-sticky","rect":[9000000,0,10,10]}]}]}""";
        Assert.IsFalse(BinkSerializer.TryDeserialize(far, out _, out _, Read), "Custom elements are held to the document's limits too.");
    }

    [TestMethod]
    public void Built_in_type_names_cannot_be_taken_over()
    {
        var registry = new BinkElementRegistry();
        Assert.ThrowsExactly<ArgumentException>(() => registry.Register(new RenamedConverter("stroke")));
        Assert.ThrowsExactly<ArgumentException>(() => registry.Register(new RenamedConverter(" ")));
        registry.Register(new RenamedConverter("first"));
        registry.Register(new RenamedConverter("second"));
        Assert.IsFalse(registry.Contains("first"), "Registering the type again under a new name replaces the old name.");
        Assert.IsTrue(registry.Contains("second"));
        Assert.IsTrue(registry.Unregister("second"));
        Assert.IsEmpty(registry.TypeNames);
    }

    [TestMethod]
    public void A_custom_element_pastes_and_travels_to_collaborators()
    {
        (BinkDocument document, StickyNote note) = WithNote();

        string payload = BinkClipboard.Serialize([document.GetElement(note.Id)], Write);
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> pasted, out string? error, Read), error);
        Assert.AreEqual("Ship it", ((StickyNote)pasted.Single()).Text);
        Assert.AreNotEqual(note.Id, pasted[0].Id);

        string operation = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, note with { Id = Guid.NewGuid() }), Write);
        var received = (BinkAddElementOperation)BinkOperationCodec.Deserialize(operation, Read);
        Assert.AreEqual(3, ((StickyNote)received.Element).Votes);
    }

    private sealed class RenamedConverter(string name) : BinkElementConverter<StickyNote>
    {
        public override string TypeName => name;

        public override void Write(Utf8JsonWriter writer, StickyNote element, BinkSerializerOptions options) { }

        public override StickyNote Read(JsonElement json, BinkDeserializerOptions options) => new(BinkRect.Empty, "");
    }
}
