// Document information dictionary (/Info) and XMP metadata reading.

using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Document-level metadata: the standard <c>/Info</c> dictionary fields plus any
/// custom entries and the raw XMP packet (<c>/Metadata</c>), when present.
/// </summary>
public sealed class BitPdfMetadata
{
    /// <summary>Document title.</summary>
    public string? Title { get; init; }

    /// <summary>Document author.</summary>
    public string? Author { get; init; }

    /// <summary>Document subject.</summary>
    public string? Subject { get; init; }

    /// <summary>Associated keywords.</summary>
    public string? Keywords { get; init; }

    /// <summary>The application that created the original document.</summary>
    public string? Creator { get; init; }

    /// <summary>The application that produced the PDF.</summary>
    public string? Producer { get; init; }

    /// <summary>Creation date, parsed from a PDF date string when possible.</summary>
    public DateTimeOffset? CreationDate { get; init; }

    /// <summary>Last-modification date, parsed from a PDF date string when possible.</summary>
    public DateTimeOffset? ModificationDate { get; init; }

    /// <summary>The raw <c>/CreationDate</c> string, exactly as stored.</summary>
    public string? RawCreationDate { get; init; }

    /// <summary>The raw <c>/ModDate</c> string, exactly as stored.</summary>
    public string? RawModificationDate { get; init; }

    /// <summary>Non-standard <c>/Info</c> entries (keys other than the well-known ones).</summary>
    public IReadOnlyDictionary<string, string> Custom { get; init; }
        = new Dictionary<string, string>();

    /// <summary>The raw XMP metadata XML from the catalog's <c>/Metadata</c> stream, if any.</summary>
    public string? XmpXml { get; init; }

    private static readonly HashSet<string> KnownKeys = new(StringComparer.Ordinal)
    {
        "Title", "Author", "Subject", "Keywords", "Creator", "Producer",
        "CreationDate", "ModDate", "Trapped",
    };

    internal static BitPdfMetadata Build(IBitPdfXRef xref, BitPdfDict? info, BitPdfDict catalog)
    {
        info ??= BitPdfDict.Empty;
        var custom = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string key in info.Keys)
        {
            if (!KnownKeys.Contains(key) && ReadString(info.Get(key)) is { } value)
            {
                custom[key] = value;
            }
        }

        string? rawCreation = ReadString(info.Get("CreationDate"));
        string? rawMod = ReadString(info.Get("ModDate"));

        return new BitPdfMetadata
        {
            Title = ReadString(info.Get("Title")),
            Author = ReadString(info.Get("Author")),
            Subject = ReadString(info.Get("Subject")),
            Keywords = ReadString(info.Get("Keywords")),
            Creator = ReadString(info.Get("Creator")),
            Producer = ReadString(info.Get("Producer")),
            RawCreationDate = rawCreation,
            RawModificationDate = rawMod,
            CreationDate = ParseDate(rawCreation),
            ModificationDate = ParseDate(rawMod),
            Custom = custom,
            XmpXml = ReadXmp(xref, catalog),
        };
    }

    private static string? ReadString(object? value)
        => value switch
        {
            BitPdfString s => s.AsText(),
            BitPdfName n => n.Value,
            _ => null,
        };

    private static string? ReadXmp(IBitPdfXRef xref, BitPdfDict catalog)
    {
        if (xref.FetchIfRef(catalog.Get("Metadata")) is BitPdfStream stream)
        {
            try
            {
                byte[] bytes = BitPdfStreamDecoder.Decode(stream);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Parses a PDF date string of the form <c>D:YYYYMMDDHHmmSSOHH'mm</c>
    /// (all components after the year optional), returning <c>null</c> on failure.
    /// </summary>
    internal static DateTimeOffset? ParseDate(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        string s = raw.Trim();
        if (s.StartsWith("D:", StringComparison.Ordinal))
        {
            s = s[2..];
        }

        // Normalise the timezone part: "Z", "+HH'mm'", "-HH'mm'" or none.
        int i = 0;
        int Read(int count, int fallback)
        {
            if (i + count > s.Length)
            {
                return fallback;
            }
            if (int.TryParse(s.AsSpan(i, count), NumberStyles.None, CultureInfo.InvariantCulture, out int val))
            {
                i += count;
                return val;
            }
            return fallback;
        }

        int year = Read(4, -1);
        if (year < 0)
        {
            return null;
        }
        int month = Clamp(Read(2, 1), 1, 12);
        int day = Clamp(Read(2, 1), 1, 31);
        int hour = Clamp(Read(2, 0), 0, 23);
        int minute = Clamp(Read(2, 0), 0, 59);
        int second = Clamp(Read(2, 0), 0, 59);

        TimeSpan offset = TimeSpan.Zero;
        if (i < s.Length)
        {
            char sign = s[i];
            if (sign is 'Z')
            {
                offset = TimeSpan.Zero;
            }
            else if (sign is '+' or '-')
            {
                i++;
                int oh = Read(2, 0);
                // Optional apostrophe separator before minutes.
                if (i < s.Length && s[i] == '\'')
                {
                    i++;
                }
                int om = Read(2, 0);
                offset = new TimeSpan(oh, om, 0);
                if (sign == '-')
                {
                    offset = -offset;
                }
            }
        }

        try
        {
            // Guard day-of-month against short months.
            int maxDay = DateTime.DaysInMonth(year, month);
            if (day > maxDay)
            {
                day = maxDay;
            }
            return new DateTimeOffset(year, month, day, hour, minute, second, offset);
        }
        catch
        {
            return null;
        }
    }

    private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
}
