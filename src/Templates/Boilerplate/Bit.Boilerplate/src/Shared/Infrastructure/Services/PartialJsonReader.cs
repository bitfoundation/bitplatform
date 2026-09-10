using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Boilerplate.Shared.Infrastructure.Services;

/// <summary>
/// Deserializes a json document arriving bit by bit, so what has landed is usable before the rest of it does. No
/// parser accepts a cut off document, so each chunk is finished into a whole one first: drop a half written escape,
/// close an open string, drop a dangling comma, close everything still open. What hasn't arrived keeps its default.
/// </summary>
/// <remarks>
/// Two edges: a chunk nothing can finish - <c>{"answer":</c> - returns the previous chunk's result rather than null,
/// so a caller rendering this doesn't blank the screen; and a number can read wrongly in passing, since <c>125</c>
/// arriving as <c>12</c> is a number too (strings and arrays cannot).
/// </remarks>
public sealed class PartialJsonReader<T>(JsonTypeInfo<T> typeInfo) where T : class
{
    private readonly StringBuilder json = new();

    /// <summary>The '}' and ']' owed to everything opened so far, outermost first.</summary>
    private readonly List<char> closers = [];

    private int scanned;
    private bool inString;

    /// <summary>Where the '"' that opened the string being read is. Only meaningful while one is open.</summary>
    private int stringStart;

    /// <summary>Where the '\' of an escape sequence that has not finished arriving is, or -1.</summary>
    private int openEscape = -1;

    private bool? isDocument;
    private T? lastCompleted;

    /// <summary>Everything that has arrived, exactly as it arrived.</summary>
    public string Json => json.ToString();

    /// <summary>Whether this is json at all. False once something else arrives, which the caller may use as it came.</summary>
    public bool IsDocument => isDocument is not false;

    /// <summary>Whether the document has ended - everything that was opened is closed again.</summary>
    public bool IsComplete => isDocument is true && inString is false && closers.Count is 0;

    /// <summary>
    /// The characters that would make what arrived a whole document, for a caller that has already forwarded them and
    /// can only append - splicing a streamed document into a larger one. Empty once <see cref="IsComplete"/>.
    /// <para>
    /// Append-only is why this differs from what the reader does for itself: half an escape is padded out to a
    /// character rather than dropped. A caller that can rewrite what it has should use <see cref="Append"/>'s result.
    /// </para>
    /// </summary>
    public string Completion()
    {
        if (IsComplete)
            return string.Empty;

        var closing = new string([.. Enumerable.Reverse(closers)]);

        // Decided by trying, rather than tracking whether every open string is a property name or a value - a parse
        // tells the two candidates apart anyway.
        string[] candidates = inString ? ["\"", "\":null"] : ["", "null"];

        foreach (var owed in candidates)
        {
            var tail = Padding() + owed + closing;

            if (TryDeserialize(Json + tail, out _))
                return tail;
        }

        return Padding() + (inString ? "\"" : string.Empty) + closing;
    }

    /// <summary>Finishes an escape sequence that stopped half way, since it cannot be taken back.</summary>
    private string Padding()
    {
        if (openEscape is -1)
            return string.Empty;

        var written = json.Length - openEscape;

        // A lone backslash becomes an escaped one; "\uXX" gets the digits it is still owed.
        return written is 1 ? "\\" : new string('0', 6 - written);
    }

    /// <summary>Adds the next chunk and returns what the document says so far, or null while it says nothing yet.</summary>
    public T? Append(string? chunk)
    {
        if (string.IsNullOrEmpty(chunk) is false)
        {
            json.Append(chunk);
        }

        Scan();

        // An open string is closed and kept, as a value being written. If that doesn't parse it was a property name
        // waiting on a value nothing can invent, so it and the comma before it are dropped instead.
        if (TryDeserialize(Complete(keepOpenString: true), out var completed)
            || (inString && TryDeserialize(Complete(keepOpenString: false), out completed)))
        {
            lastCompleted = completed;
        }

        return lastCompleted;
    }

    /// <summary>Walks what arrived since the last chunk, keeping only what <see cref="Complete"/> needs.</summary>
    private void Scan()
    {
        for (; scanned < json.Length; scanned++)
        {
            var c = json[scanned];

            if (isDocument is null && char.IsWhiteSpace(c) is false)
            {
                isDocument = c is '{' or '[';
            }

            if (openEscape is not -1)
            {
                // "\uXXXX" is the only escape that is longer than the character after the backslash.
                var length = scanned - openEscape;

                if (length is 1 && c is 'u') continue;

                if (length is 1 or 5)
                {
                    openEscape = -1;
                }

                continue;
            }

            if (inString)
            {
                if (c is '\\')
                {
                    openEscape = scanned;
                }
                else if (c is '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    stringStart = scanned;
                    break;
                case '{':
                    closers.Add('}');
                    break;
                case '[':
                    closers.Add(']');
                    break;
                case '}' or ']' when closers.Count > 0:
                    closers.RemoveAt(closers.Count - 1);
                    break;
            }
        }
    }

    /// <summary>What has arrived, finished into a document a parser will accept.</summary>
    private string Complete(bool keepOpenString)
    {
        var keeping = inString && keepOpenString;

        // Half an escape sequence is not a character yet, so neither half of it can stay.
        var end = keeping ? openEscape is -1 ? json.Length : openEscape
                : inString ? stringStart
                : json.Length;

        // Outside a string only: inside one these are text, not punctuation.
        while (keeping is false && end > 0 && (json[end - 1] is ',' || char.IsWhiteSpace(json[end - 1])))
        {
            end--;
        }

        if (end is 0 && keeping is false)
            return string.Empty;

        StringBuilder completed = new(end + closers.Count + 1);

        completed.Append(json, 0, end);

        if (keeping)
        {
            completed.Append('"');
        }

        for (var i = closers.Count - 1; i >= 0; i--)
        {
            completed.Append(closers[i]);
        }

        return completed.ToString();
    }

    private bool TryDeserialize(string completed, out T? result)
    {
        if (completed.Length is 0)
        {
            result = null;
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize(completed, typeInfo);
            return result is not null;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }
    }
}
