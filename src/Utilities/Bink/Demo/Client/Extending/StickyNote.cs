using System.Numerics;
using System.Text.Json;

namespace Bit.Bink.Demo.Client.Extending;

/// <summary>
/// An element type of the demo's own: a note with some text and a vote count, as a retrospective board has.
/// </summary>
/// <remarks>
/// Deriving from <see cref="BinkElement"/> is all it takes for the note to draw, hit test, move, resize, rotate,
/// group, lock and undo like any built-in element: it supplies its bounds, its drawing and its hit test, in its own
/// coordinates, and the transform does the rest. <see cref="StickyNoteConverter"/> is what lets it be saved.
/// </remarks>
public sealed record StickyNote : BinkElement
{
    /// <summary>The most text a note holds, which the converter also enforces on what it reads.</summary>
    public const int MaxText = 200;

    private static readonly BinkColor Paper = BinkColor.Parse("#fef08a");
    private static readonly BinkColor Edge = BinkColor.Parse("#ca8a04");
    private static readonly BinkTextStyle TextStyle = BinkTextStyle.Default with { FontSize = 15f, Color = BinkColor.Parse("#422006") };
    private static readonly BinkTextStyle VoteStyle = BinkTextStyle.Default with { FontSize = 13f, Bold = true, Color = BinkColor.Parse("#854d0e"), Align = BinkTextAlign.Right };

    // The outline, built when the rectangle is set and never changed after. The canvas keeps a path it has drawn and
    // names it again on later frames, so handing it the same geometry is what keeps a redraw cheap; a copy made by
    // moving the note or voting on it keeps its rectangle, and so shares the path.
    private readonly BinkRect _rect;
    private readonly BinkPathGeometry _body = BinkPathGeometry.Empty;

    /// <summary>Creates a note.</summary>
    public StickyNote(BinkRect rect, string text)
    {
        Rect = rect;
        Text = text;
    }

    /// <summary>Where the note is, in its own coordinates.</summary>
    public BinkRect Rect
    {
        get => _rect;
        init
        {
            _rect = value;
            _body = new BinkPathBuilder(16).AddRoundedRect(value, 6f).ToGeometry();
        }
    }

    /// <summary>What is written on it; line breaks start new lines.</summary>
    public string Text { get; init; }

    /// <summary>How many votes it has.</summary>
    public int Votes { get; init; }

    /// <inheritdoc />
    public override BinkRect LocalBounds => Rect;

    /// <inheritdoc />
    public override string TypeName => "Sticky note";

    /// <inheritdoc />
    public override string GetAccessibleDescription()
        => Description ?? $"Sticky note: {Text.Replace('\n', ' ')}, {Votes} {(Votes == 1 ? "vote" : "votes")}";

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.FillPath(_body, new BinkFillPaint(Paper));
        context.StrokePath(_body, new BinkStrokePaint(Edge, 1.5f));
        context.DrawText(Text, new Vector2(Rect.X + 12f, Rect.Y + 10f), TextStyle, 1f);
        context.DrawText($"▲ {Votes}", new Vector2(Rect.Right - 12f, Rect.Bottom - 26f), VoteStyle, 1f);
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => Rect.Inflate(tolerance).Contains(localPoint);
}

/// <summary>
/// Saves and reads <see cref="StickyNote"/>. Registered in a <see cref="BinkElementRegistry"/>, it lets notes be saved,
/// loaded, pasted and sent to collaborators.
/// </summary>
/// <remarks>
/// The serializer writes the type and every field all elements share (id, version, transform, description, metadata,
/// lock, groups) itself, and the element's bounds; the converter writes only the note's own fields. A reader without the
/// registration keeps the note as an unknown element, in place, and writes it back unchanged.
/// </remarks>
public sealed class StickyNoteConverter : BinkElementConverter<StickyNote>
{
    /// <summary>The type written into saved documents.</summary>
    public const string Name = "demo-sticky-note";

    /// <inheritdoc />
    public override string TypeName => Name;

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, StickyNote element, BinkSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        writer.WriteStartArray("rect");
        writer.WriteNumberValue(element.Rect.X);
        writer.WriteNumberValue(element.Rect.Y);
        writer.WriteNumberValue(element.Rect.Width);
        writer.WriteNumberValue(element.Rect.Height);
        writer.WriteEndArray();
        writer.WriteString("text", element.Text);
        if (element.Votes != 0) writer.WriteNumber("votes", element.Votes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// What is read is untrusted - a file anyone could have written, a message from a collaborator - so every value is
    /// checked and bounded. A <see cref="FormatException"/> is reported as a malformed document, naming the type.
    /// </remarks>
    public override StickyNote Read(JsonElement json, BinkDeserializerOptions options)
    {
        if (!json.TryGetProperty("rect", out JsonElement rect) || rect.ValueKind != JsonValueKind.Array || rect.GetArrayLength() != 4)
            throw new FormatException("A sticky note needs a rectangle of four numbers.");
        float[] r = new float[4];
        int i = 0;
        foreach (JsonElement value in rect.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out r[i]) || !float.IsFinite(r[i]))
                throw new FormatException("A sticky note's rectangle holds something that is not a number.");
            i++;
        }
        if (r[2] <= 0f || r[3] <= 0f || r[2] > 4000f || r[3] > 4000f) throw new FormatException("A sticky note's size is out of range.");

        string text = json.TryGetProperty("text", out JsonElement t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "";
        if (text.Length > StickyNote.MaxText) throw new FormatException($"A sticky note holds at most {StickyNote.MaxText} characters.");

        int votes = json.TryGetProperty("votes", out JsonElement v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int count)
            ? Math.Clamp(count, 0, 9999)
            : 0;
        return new StickyNote(new BinkRect(r[0], r[1], r[2], r[3]), text) { Votes = votes };
    }
}
