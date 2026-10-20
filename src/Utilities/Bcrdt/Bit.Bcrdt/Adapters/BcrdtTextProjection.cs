using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A plain-text view of a <see cref="BcrdtText"/>, together with the mapping between its character
/// positions and the document's.
/// </summary>
/// <remarks>
/// <para>
/// A collaborative text field and the string a control displays are not index-compatible in
/// general. Embedded objects (images, mentions) occupy one position in the document but have no
/// character in <see cref="BcrdtText.ToString"/>, and a browser <c>&lt;textarea&gt;</c> normalizes
/// <c>"\r\n"</c> and lone <c>"\r"</c> to <c>"\n"</c>, so its value is shorter than the document
/// whenever the text came from a Windows client. Applying an index computed against one to the
/// other edits the wrong characters.
/// </para>
/// <para>
/// This type keeps the two straight: <see cref="Display"/> is what a plain-text control shows, and
/// the <c>To*</c> methods translate positions between it and the document. When the text contains
/// neither embeds nor (with normalization on) carriage returns the mapping is the identity and costs
/// nothing.
/// </para>
/// </remarks>
public sealed class BcrdtTextProjection
{
    // Document index of each display character, and the index just past it. Null for the identity.
    private readonly int[]? _starts;
    private readonly int[]? _ends;

    private BcrdtTextProjection(string display, int[]? starts, int[]? ends, int documentLength)
    {
        Display = display;
        _starts = starts;
        _ends = ends;
        DocumentLength = documentLength;
    }

    /// <summary>The text as a plain-text control should display it.</summary>
    public string Display { get; }

    /// <summary>The length of the underlying text in document units (<see cref="BcrdtText.Count"/>).</summary>
    public int DocumentLength { get; }

    /// <summary>Whether display positions and document positions coincide.</summary>
    public bool IsIdentity => _starts is null;

    /// <summary>Projects a collaborative text field.</summary>
    /// <param name="text">The text to project.</param>
    /// <param name="normalizeLineEndings">
    /// Whether to present <c>"\r\n"</c> and lone <c>"\r"</c> as <c>"\n"</c>, as a browser text control
    /// does. Leave off when the caller compares against strings taken from <see cref="BcrdtText.ToString"/>.
    /// </param>
    public static BcrdtTextProjection Create(BcrdtText text, bool normalizeLineEndings = false)
    {
        ArgumentNullException.ThrowIfNull(text);

        string plain = text.ToString();
        int count = text.Count;
        bool hasCarriageReturn = normalizeLineEndings && plain.Contains('\r', StringComparison.Ordinal);

        if (plain.Length == count && !hasCarriageReturn)
        {
            return new BcrdtTextProjection(plain, null, null, count);
        }

        // Pair every visible character with its document index. Embeds and other non-text content
        // advance the document position by their length - one - without producing a character. An
        // embed can itself be a string, so it is recognized by IsEmbed rather than by its content.
        var raw = new StringBuilder(plain.Length);
        var rawIndex = new List<int>(plain.Length);
        int position = 0;
        foreach (BcrdtDeltaOperation operation in text.ToDelta())
        {
            if (!operation.IsEmbed && operation.Content is string run)
            {
                foreach (char c in run)
                {
                    raw.Append(c);
                    rawIndex.Add(position++);
                }
            }
            else
            {
                position += operation.Length;
            }
        }

        var display = new StringBuilder(raw.Length);
        var starts = new List<int>(raw.Length);
        var ends = new List<int>(raw.Length);

        for (int k = 0; k < raw.Length; k++)
        {
            char c = raw[k];
            int start = rawIndex[k];

            if (normalizeLineEndings && c == '\r')
            {
                bool pairedWithLineFeed = k + 1 < raw.Length && raw[k + 1] == '\n' && rawIndex[k + 1] == start + 1;
                display.Append('\n');
                starts.Add(start);
                ends.Add(pairedWithLineFeed ? start + 2 : start + 1);
                if (pairedWithLineFeed) k++;
                continue;
            }

            display.Append(c);
            starts.Add(start);
            ends.Add(start + 1);
        }

        return new BcrdtTextProjection(display.ToString(), [.. starts], [.. ends], Math.Max(count, position));
    }

    /// <summary>The document index of the display character at <paramref name="displayIndex"/>.</summary>
    /// <remarks>An index at or past the end maps to <see cref="DocumentLength"/>.</remarks>
    public int ToDocument(int displayIndex)
    {
        if (_starts is null) return Math.Clamp(displayIndex, 0, DocumentLength);
        return displayIndex >= _starts.Length ? DocumentLength : _starts[Math.Max(0, displayIndex)];
    }

    /// <summary>
    /// Where text typed at <paramref name="displayIndex"/> belongs in the document: immediately after
    /// the character to its left.
    /// </summary>
    public int ToDocumentInsertion(int displayIndex)
    {
        if (displayIndex <= 0) return 0;
        if (_ends is null) return Math.Min(displayIndex, DocumentLength);
        return _ends[Math.Min(displayIndex, _ends.Length) - 1];
    }

    /// <summary>The display position corresponding to a document index.</summary>
    /// <remarks>
    /// A document index that falls on content with no display character - an embed, or the line feed
    /// of a normalized <c>"\r\n"</c> - maps to the next display position.
    /// </remarks>
    public int ToDisplay(int documentIndex)
    {
        if (_starts is null) return Math.Clamp(documentIndex, 0, Display.Length);
        if (documentIndex <= 0) return 0;

        int low = 0;
        int high = _starts.Length;
        while (low < high)
        {
            int middle = (low + high) >>> 1;
            if (_starts[middle] < documentIndex) low = middle + 1;
            else high = middle;
        }
        return low;
    }

    /// <summary>
    /// The document ranges holding the display characters in <c>[displayStart, displayEnd)</c>,
    /// ascending and coalesced.
    /// </summary>
    /// <remarks>
    /// Content with no display character that lies between those characters - an embed in the middle
    /// of a selection - is not included, so deleting the returned ranges never removes it.
    /// </remarks>
    public IReadOnlyList<(int Start, int Length)> ToDocumentRanges(int displayStart, int displayEnd)
    {
        displayStart = Math.Clamp(displayStart, 0, Display.Length);
        displayEnd = Math.Clamp(displayEnd, displayStart, Display.Length);
        if (displayEnd == displayStart) return [];

        if (_starts is null) return [(displayStart, displayEnd - displayStart)];

        List<(int Start, int Length)> ranges = [];
        for (int j = displayStart; j < displayEnd; j++)
        {
            int start = _starts[j];
            int end = _ends![j];
            if (ranges.Count > 0 && ranges[^1].Start + ranges[^1].Length == start)
            {
                ranges[^1] = (ranges[^1].Start, ranges[^1].Length + end - start);
            }
            else
            {
                ranges.Add((start, end - start));
            }
        }
        return ranges;
    }

    /// <summary>The display-to-document mapping as runs, for <see cref="BcrdtTextInputBinding"/>.</summary>
    internal List<BcrdtTextSegment> CreateSegments()
    {
        List<BcrdtTextSegment> segments = [];
        if (_starts is null)
        {
            if (Display.Length > 0) segments.Add(new BcrdtTextSegment(Display.Length, 0, Display.Length));
            return segments;
        }

        for (int j = 0; j < _starts.Length; j++)
        {
            int start = _starts[j];
            int length = _ends![j] - start;
            if (length == 1 &&
                segments.Count > 0 &&
                segments[^1].IsOneToOne &&
                segments[^1].DocumentStart + segments[^1].DocumentLength == start)
            {
                BcrdtTextSegment last = segments[^1];
                segments[^1] = last with { DisplayLength = last.DisplayLength + 1, DocumentLength = last.DocumentLength + 1 };
            }
            else
            {
                segments.Add(new BcrdtTextSegment(1, start, length));
            }
        }
        return segments;
    }
}

/// <summary>
/// A run of displayed characters and the document range they occupy.
/// </summary>
/// <param name="DisplayLength">How many displayed characters the run covers.</param>
/// <param name="DocumentStart">Where the run starts in the document.</param>
/// <param name="DocumentLength">
/// How many document units the run covers: equal to <paramref name="DisplayLength"/> for ordinary
/// text, zero for characters whose content was deleted by somebody else, and two for one normalized
/// <c>"\r\n"</c>.
/// </param>
internal readonly record struct BcrdtTextSegment(int DisplayLength, int DocumentStart, int DocumentLength)
{
    public bool IsOneToOne => DisplayLength == DocumentLength;

    public bool IsCollapsed => DocumentLength == 0;

    public int DocumentEnd => DocumentStart + DocumentLength;
}
