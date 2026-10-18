using ModelContextProtocol.Protocol;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, a symbology, an embedded source path.
/// Without completions a person picking <c>bbscanner://api/{typeName}</c> in a client has to go and call
/// a listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BbScannerCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "the scanner shows NotSupported instead of the camera",
        "the camera never starts and no permission prompt appears",
        "the scanner works on localhost but not on the deployed site",
        "the same barcode is reported over and over",
        "a barcode on screen is never read",
        "an EAN-13 is reported as UPC-A",
        "PDF417 barcodes are never read",
        "scanning makes the page sluggish on a phone",
        "the camera light stays on after leaving the page",
    ];

    private static readonly string[] _features =
    [
        "scan a QR code with the phone's rear camera",
        "read the EAN-13 barcode of a product",
        "scan a barcode from an uploaded photo",
        "decode barcodes on the server from images",
        "let the user switch cameras and turn the torch on",
        "aim with a centred scan region and a custom overlay",
        "scan a batch of barcodes without duplicates",
        "restyle the scanner to match the app's theme",
    ];

    /// <summary>
    /// The values matching what has been typed so far. The argument's name is enough to identify the
    /// set - no two arguments on this server mean different things by the same name - so one table
    /// serves a prompt argument and a resource template placeholder without either knowing about the
    /// other, and an argument it has nothing for is answered with an empty list rather than an error.
    /// </summary>
    public static Completion Complete(string? argumentName, string? typed)
    {
        var candidates = CandidatesFor(argumentName);
        if (candidates.Length == 0) return new Completion { Values = [] };

        var prefix = (typed ?? string.Empty).Trim();

        var matches = prefix.Length == 0
            ? candidates
            : [.. candidates
                .Where(value => value.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                // What was typed is a prefix far more often than it is a substring, so the values it
                // starts stay above the ones it merely appears inside.
                .OrderByDescending(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

        return new Completion
        {
            Values = [.. matches.Take(MaxValues)],
            Total = matches.Length,
            HasMore = matches.Length > MaxValues
        };
    }

    // "renderMode" is deliberately missing here: its values are declared with [AllowedValues], which
    // the SDK completes from on its own, and anything this returned would be merged with that rather
    // than replace it - the same value offered to someone twice.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "heading" => [.. BbScannerSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BbScannerApiCatalog.Types.Select(type => type.Name)],
        "format" => [.. BbScannerFormatCatalog.Formats.Select(format => format.Name)],
        "path" => [.. BbScannerSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
