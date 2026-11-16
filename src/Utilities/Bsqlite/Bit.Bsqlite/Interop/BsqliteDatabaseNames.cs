using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Interop;

/// <summary>What a database name may hold where the browser keeps it.</summary>
/// <remarks>
/// <para>
/// Every browser VFS, and the admin modules next to them, turn a name into a storage key the way a URL path is
/// read: <c>new URL(name, 'file://').pathname</c>. That reading is not one to one. It ends the path at <c>#</c>
/// and <c>?</c>, drops tabs and line breaks, reads <c>\</c> as <c>/</c>, resolves <c>.</c> and <c>..</c>
/// segments (spelled <c>%2e</c> too), takes a leading <c>name:</c> for a URL scheme, and escapes a space or a
/// non-ASCII character while leaving an escape already in the name alone - so <c>a#1.db</c> and its journal
/// <c>a#1.db-journal</c> would both be the file <c>/a</c>, and <c>a b.db</c> and <c>a%20b.db</c> would both be
/// <c>/a%20b.db</c>.
/// </para>
/// <para>
/// A name is kept only when it can be read back from its key the way the OPFS admin lists a file
/// (<c>decodeName</c> in <c>bit-bsqlite-opfs-admin.ts</c>): one key then stands for one name, and no two names
/// share a file. A <c>%</c> is fine where it is not an escape that the key would carry for another name -
/// <c>50%off.db</c> and <c>a%41.db</c> keep their names; <c>a%20b.db</c> is <c>a b.db</c>'s key and is refused.
/// </para>
/// </remarks>
internal static partial class BsqliteDatabaseNames
{
    /// <summary>Throws when a browser backend cannot keep a database under <paramref name="databaseName"/>.</summary>
    public static void ThrowIfInvalidInBrowser(string databaseName, string? paramName = null)
    {
        if (WhyInvalidInBrowser(databaseName) is { } reason)
        {
            throw new ArgumentException(
                $"The database name '{databaseName}' cannot be kept by a browser backend: {reason} The browser "
                + "VFSes read a name as a URL path, so two databases, or a database and its journal, would end up in "
                + "one file. Choose another name.",
                paramName);
        }
    }

    /// <summary>Why a browser backend cannot keep <paramref name="databaseName"/>, or null when it can.</summary>
    internal static string? WhyInvalidInBrowser(string databaseName)
    {
        ArgumentNullException.ThrowIfNull(databaseName);

        foreach (var c in databaseName)
        {
            switch (c)
            {
                case '#' or '?':
                    return $"'{c}' ends a URL path.";
                case '\\':
                    return "'\\' is read as '/'.";
                case < ' ':
                    return $"it holds the control character U+{(int)c:X4}, which a URL drops or escapes.";
            }
        }

        if (databaseName[0] == ' ' || databaseName[^1] == ' ')
        {
            return "a URL drops the spaces it starts or ends with.";
        }

        if (Scheme().IsMatch(databaseName))
        {
            return "what comes before its ':' reads as a URL scheme.";
        }

        var segments = databaseName.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
            {
                return "it has an empty path segment - it starts or ends with '/', or holds '//'.";
            }

            if (IsDotSegment(segment))
            {
                return $"its segment '{segment}' is resolved away, as '.' or '..' is.";
            }

            if (i == 0 && segment.Length == 2 && char.IsAsciiLetter(segment[0]) && segment[1] == '|')
            {
                return $"its first segment '{segment}' reads as a Windows drive letter.";
            }

            // Whether '^' is escaped in a path differs between browsers, so a name must read back either way.
            if (!ReadsBack(segment, escapeCaret: false) || !ReadsBack(segment, escapeCaret: true))
            {
                return $"its segment '{segment}' holds an escape a URL would give another name ('{Decode(segment)}').";
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the key segment of <paramref name="segment"/> reads back as <paramref name="segment"/> the way
    /// <c>decodeName</c> reads it: decoded when the decoded text maps to that same key, as it is otherwise.
    /// </summary>
    private static bool ReadsBack(string segment, bool escapeCaret)
    {
        var key = Encode(segment, escapeCaret);
        return Decode(key) is not { } decoded || Encode(decoded, escapeCaret) != key ? key == segment : decoded == segment;
    }

    private static bool IsDotSegment(string segment) =>
        segment.ToLowerInvariant() is "." or "%2e" or ".." or ".%2e" or "%2e." or "%2e%2e";

    /// <summary>A path segment as a URL writes it, with the path percent-encode set escaped.</summary>
    private static string Encode(string segment, bool escapeCaret)
    {
        var builder = new StringBuilder(segment.Length);
        Span<byte> bytes = stackalloc byte[4];
        foreach (var rune in segment.EnumerateRunes())
        {
            var value = rune.Value;
            if (value is <= 0x20 or > 0x7E or '"' or '#' or '<' or '>' or '?' or '`' or '{' or '}'
                || (escapeCaret && value == '^'))
            {
                var length = rune.EncodeToUtf8(bytes);
                foreach (var b in bytes[..length])
                {
                    builder.Append('%').Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else
            {
                builder.Append((char)value);
            }
        }

        return builder.ToString();
    }

    /// <summary><c>decodeURIComponent</c>: null where it throws - a malformed escape, or bytes that are not UTF-8.</summary>
    private static string? Decode(string text)
    {
        if (!text.Contains('%', StringComparison.Ordinal))
        {
            return text;
        }

        var bytes = new List<byte>(text.Length);
        Span<byte> utf8 = stackalloc byte[4];
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '%')
            {
                if (Rune.DecodeFromUtf16(text.AsSpan(i), out var rune, out var consumed) is not System.Buffers.OperationStatus.Done)
                {
                    return null;
                }

                bytes.AddRange(utf8[..rune.EncodeToUtf8(utf8)]);
                i += consumed - 1;
                continue;
            }

            if (i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2]))
            {
                return null;
            }

            bytes.Add(Convert.FromHexString(text.AsSpan(i + 1, 2))[0]);
            i += 2;
        }

        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString([.. bytes]);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.CultureInvariant)]
    private static partial Regex Scheme();
}
