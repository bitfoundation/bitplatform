using System.Text;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Bit.BbScanner.Demo.Server.Dtos;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// The symbologies Bit.BbScanner reads, described from the library rather than from a list someone keeps: the
/// flags of <see cref="BbBarcodeFormat"/> and their XML documentation, the guide's symbology table, whether the
/// pipeline actually creates each reader, and the <see cref="BbScannerOptions"/> members that steer each one -
/// with the default a freshly created instance really has.
/// <para>
/// What a symbology is called and which options matter to it is the one hand-written part; a flag the library
/// adds without being listed here still appears, with its documentation and the options every symbology shares.
/// </para>
/// </summary>
public static class BbScannerFormatCatalog
{
    private sealed record Known(string Title, string SampleText, (string Option, string Effect)[] Options);

    private static readonly (string Option, string Effect)[] _shared =
    [
        (nameof(BbScannerOptions.Formats), "Only the readers of the requested formats are created. Narrowing it is the cheapest speed-up there is, and the main guard against a misread between similar symbologies."),
        (nameof(BbScannerOptions.Region), "Restricts every stage to the part of the frame the user aims with."),
        (nameof(BbScannerOptions.DownsampleFactor), "Subsamples while converting to grayscale; quadratic in speed, and costs the smallest readable symbol."),
    ];

    private static readonly (string Option, string Effect)[] _matrix =
    [
        .. _shared,
        (nameof(BbScannerOptions.TryHarder), "Searches every row for finder patterns and many more candidate grids. For still images; off for live frames by design."),
        (nameof(BbScannerOptions.AllowInverted), "Also reads light-on-dark symbols, at roughly twice the cost of a frame that finds nothing."),
    ];

    private static readonly (string Option, string Effect)[] _linear =
    [
        .. _shared,
        (nameof(BbScannerOptions.MaxScanLines), "Scan lines per orientation. Lower for aimed scanning, higher for hands-off scanning."),
        (nameof(BbScannerOptions.TryReversedRows), "Also reads the symbol right to left, so an upside-down one is read."),
        (nameof(BbScannerOptions.TryVerticalLines), "Also scans columns and tilted lines, so a rotated symbol is read."),
    ];

    private static readonly Dictionary<BbBarcodeFormat, Known> _known = new()
    {
        [BbBarcodeFormat.QrCode] = new("QR Code", "https://bitplatform.dev", _matrix),
        [BbBarcodeFormat.DataMatrix] = new("Data Matrix", "BIT-BBSCANNER-DM", _matrix),
        [BbBarcodeFormat.Pdf417] = new("PDF417", "PDF FOUR ONE SEVEN", _matrix),
        [BbBarcodeFormat.Code128] = new("Code 128", "BIT-BBSCANNER-128", _linear),
        [BbBarcodeFormat.Code39] = new("Code 39", "CODE39 DEMO",
        [
            .. _linear,
            (nameof(BbScannerOptions.Code39CheckDigit), "Requires and strips the optional modulo 43 check character."),
            (nameof(BbScannerOptions.Code39ExtendedMode), "Interprets the symbol as full ASCII, where pairs such as +A stand for a."),
        ]),
        [BbBarcodeFormat.Ean13] = new("EAN-13", "4006381333931", _linear),
        [BbBarcodeFormat.Ean8] = new("EAN-8", "96385074", _linear),
        [BbBarcodeFormat.UpcA] = new("UPC-A", "036000291452", _linear),
        [BbBarcodeFormat.UpcE] = new("UPC-E", "01234565", _linear),
        [BbBarcodeFormat.Itf] = new("ITF", "1234567890",
        [
            .. _linear,
            (nameof(BbScannerOptions.ItfLengths), "The digit counts an ITF result may have. ITF has no check character, so this is the main guard against a truncated read."),
        ]),
    };

    private static readonly Lazy<BbScannerFormatDto[]> _formats = new(Build);

    /// <summary>Every single symbology, in the order BbBarcodeFormat declares them.</summary>
    public static BbScannerFormatDto[] Formats => _formats.Value;

    /// <summary>
    /// Resolves a symbology from the way a caller spells it: the enum member ("QrCode"), its usual name
    /// ("QR Code", "EAN-13") or either without punctuation ("qr", "ean13", "upce").
    /// </summary>
    public static BbScannerFormatDto? Find(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return null;

        var key = Normalize(format);

        return Formats.FirstOrDefault(f => Normalize(f.Name) == key || Normalize(f.Title) == key)
            ?? (key is "qr" ? Formats.FirstOrDefault(f => f.Name == nameof(BbBarcodeFormat.QrCode))
              : key is "dm" or "datamatrix" ? Formats.FirstOrDefault(f => f.Name == nameof(BbBarcodeFormat.DataMatrix))
              : key is "interleaved2of5" or "i2of5" ? Formats.FirstOrDefault(f => f.Name == nameof(BbBarcodeFormat.Itf))
              : null);
    }

    /// <summary>The table of every symbology, as GetBbScannerFormats answers when it is called without a format.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Symbologies Bit.BbScanner reads").AppendLine();
        builder.AppendLine("`BbBarcodeFormat` is a flags enum: combine members with `|`. The composites are " +
                           $"`AllLinear` ({(int)BbBarcodeFormat.AllLinear}), `AllMatrix` ({(int)BbBarcodeFormat.AllMatrix}) " +
                           $"and `All` ({(int)BbBarcodeFormat.All}), and a new `BbScannerOptions` (and the `BbScanner` component) " +
                           $"starts with `{Describe(new BbScannerOptions().Formats)}`. Call `GetBbScannerFormats(format: \"...\")` " +
                           "for one symbology with the options that steer it.").AppendLine();

        builder.AppendLine("| Format | Name | Family | Flag | Read by the pipeline | Notes |");
        builder.AppendLine("| --- | --- | --- | ---: | --- | --- |");

        foreach (var format in Formats)
        {
            builder.AppendLine($"| `{format.Name}` | {format.Title} | {format.Family} | {format.Value} | {(format.Active ? "yes" : "**no** - see the notes")} | {Cell(format.Notes ?? format.Summary)} |");
        }

        return builder.ToString();
    }

    /// <summary>One symbology in full, or null when the library has no such format.</summary>
    public static string? Render(string? format)
    {
        var dto = Find(format);
        if (dto is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# {dto.Title} (`BbBarcodeFormat.{dto.Name}`)").AppendLine();
        builder.AppendLine($"{dto.Family} symbology, flag value {dto.Value}.").AppendLine();

        if (dto.Summary is not null) builder.AppendLine(dto.Summary).AppendLine();
        if (dto.Status is not null) builder.AppendLine($"**{dto.Status}.** {dto.Notes}").AppendLine();

        builder.AppendLine(dto.EnabledByDefault
            ? "Enabled by default: a new `BbScannerOptions` and the `BbScanner` component look for it unless `Formats` says otherwise."
            : "Not enabled by default: add it to `Formats`.").AppendLine();

        if (dto.Active is false)
        {
            builder.AppendLine("**The pipeline does not create this reader.** The registered PDF417 symbol character table is the library's " +
                               "generated placeholder (`BbPdf417SymbolTable.Current.IsSpecificationTable` is false), so `BbBarcodeDecoder` " +
                               "leaves PDF417 off: enabling the flag costs nothing and finds nothing. Register the ISO/IEC 15438 table with " +
                               "`BbPdf417SymbolTable.Register(BbPdf417SymbolTable.FromPatterns(cluster0, cluster3, cluster6, isSpecificationTable: true))` " +
                               "to make it read real symbols - see `GetBbScannerGuideSection(heading: \"PDF417 status\")`.").AppendLine();
        }

        builder.AppendLine("## Options that steer it").AppendLine();
        builder.AppendLine("| `BbScannerOptions` member | Type | Default | Effect |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (var option in dto.Options)
        {
            builder.AppendLine($"| `{option.Name}` | `{option.Type}` | `{option.Default ?? "null"}` | {Cell(option.Effect)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Selecting it").AppendLine();
        builder.AppendLine("```razor");
        builder.AppendLine($"<BbScanner Formats=\"BbBarcodeFormat.{dto.Name}\" OnScan=\"HandleScan\" />");
        builder.AppendLine("```").AppendLine();
        builder.AppendLine($"Call `InspectBbScannerSymbol(format: \"{dto.Name}\")` to render one, decode it with the library and see the " +
                           "`BbBarcodeResult` it produces - its text, raw bytes, corners and metadata.");

        return builder.ToString();
    }

    private static BbScannerFormatDto[] Build()
    {
        var defaults = new BbScannerOptions();
        var guide = ReadGuideTable();

        return [.. Enum.GetValues<BbBarcodeFormat>()
            .Where(IsSingleFlag)
            .Select(format =>
            {
                var known = _known.GetValueOrDefault(format);
                var title = known?.Title ?? format.ToString();
                var row = guide.GetValueOrDefault(title);
                var matrix = (BbBarcodeFormat.AllMatrix & format) != 0;

                return new BbScannerFormatDto
                {
                    Name = format.ToString(),
                    Title = title,
                    Family = matrix ? "Matrix" : "Linear",
                    Value = (int)format,
                    Summary = BbScannerXmlDocs.GetSummary($"F:{typeof(BbBarcodeFormat).FullName}.{format}"),
                    Status = row.Status,
                    Notes = row.Notes,
                    EnabledByDefault = (defaults.Formats & format) != 0,
                    Active = format != BbBarcodeFormat.Pdf417 || BbPdf417SymbolTable.Current.IsSpecificationTable,
                    Options = [.. (known?.Options ?? _shared).Select(option => Option(defaults, option.Option, option.Effect))],
                    SampleText = known?.SampleText ?? "SAMPLE"
                };
            })];
    }

    private static bool IsSingleFlag(BbBarcodeFormat format)
    {
        var value = (int)format;

        return value != 0 && (value & (value - 1)) == 0;
    }

    private static BbScannerOptionDto Option(BbScannerOptions defaults, string name, string effect)
    {
        var property = typeof(BbScannerOptions).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!;

        return new BbScannerOptionDto
        {
            Name = name,
            Type = BbScannerApiCatalog.FriendlyName(property.PropertyType),
            Default = Describe(property.GetValue(defaults)),
            Effect = effect
        };
    }

    /// <summary>
    /// The symbology table of the guide, keyed by the name in its first column: the status and notes are the
    /// guide's own words, so they cannot drift from what the README says.
    /// </summary>
    private static Dictionary<string, (string? Status, string? Notes)> ReadGuideTable()
    {
        var table = new Dictionary<string, (string? Status, string? Notes)>(StringComparer.OrdinalIgnoreCase);

        var section = BbScannerSourceCatalog.GetGuideSection("Symbologies");
        if (section is null) return table;

        foreach (var line in section.Split('\n'))
        {
            var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            if (cells.Length < 3 || cells[0].StartsWith("---", StringComparison.Ordinal) || cells[0] == "Symbology") continue;

            table[cells[0]] = (cells[1].Replace("**", string.Empty, StringComparison.Ordinal), cells[2].Length == 0 ? null : cells[2]);
        }

        return table;
    }

    internal static string? Describe(object? value) => value switch
    {
        null => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        Enum enumValue => string.Join(" | ", enumValue.ToString().Split(", ").Select(part => $"{enumValue.GetType().Name}.{part}")),
        TimeSpan span => span == TimeSpan.Zero ? "TimeSpan.Zero" : $"TimeSpan.FromSeconds({span.TotalSeconds.ToString(CultureInfo.InvariantCulture)})",
        BbScanRegion region => region == BbScanRegion.Full ? "BbScanRegion.Full" : region.ToString(),
        IEnumerable sequence => $"[{string.Join(", ", sequence.Cast<object>().Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)))}]",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static string Cell(string? text) => (text ?? string.Empty).ReplaceLineEndings(" ").Replace("|", "\\|", StringComparison.Ordinal);

    private static string Normalize(string text) => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
}
