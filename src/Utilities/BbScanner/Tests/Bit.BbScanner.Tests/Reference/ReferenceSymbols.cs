using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BbScanner.Tests;

/// <summary>
/// Symbols written by an encoder this library has nothing to do with: BWIPP, through bwip-js. See
/// <c>generate-reference-symbols.mjs</c> next to the data for how they are produced and why.
/// </summary>
internal static class ReferenceSymbols
{
    private static readonly Lazy<ReferenceSymbolFile> File = new(Load);

    /// <summary>Which encoder, at which version, wrote the fixtures.</summary>
    public static string Generator => File.Value.Generator;

    public static IReadOnlyList<ReferenceSymbol> All => File.Value.Symbols;

    public static ReferenceSymbol Get(string id) => All.Single(s => s.Id == id);

    private static ReferenceSymbolFile Load()
    {
        using var stream = typeof(ReferenceSymbols).Assembly.GetManifestResourceStream("ReferenceSymbols.json")
            ?? throw new InvalidOperationException("ReferenceSymbols.json is not embedded in the test assembly.");
        return JsonSerializer.Deserialize<ReferenceSymbolFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("ReferenceSymbols.json is empty.");
    }

    private sealed record ReferenceSymbolFile(string Generator, ReferenceSymbol[] Symbols);
}

/// <summary>One fixture: a module grid or a run of bar widths, and what decoding it must report.</summary>
internal sealed record ReferenceSymbol
{
    public required string Id { get; init; }

    public required string Format { get; init; }

    /// <summary>The BWIPP encoder and options that wrote it, for a failure message that says how to reproduce it.</summary>
    public required string Encoder { get; init; }

    public string? Expected { get; init; }

    [JsonPropertyName("expectedBytes")]
    public string? ExpectedBytesHex { get; init; }

    public bool Gs1 { get; init; }

    /// <summary>The EAN-2 or EAN-5 add-on digits the result must carry.</summary>
    public string? Extension { get; init; }

    public int? Eci { get; init; }

    public string[] DecoderOptions { get; init; } = [];

    /// <summary>Linear symbols: element widths in modules, bar first.</summary>
    public int[]? Widths { get; init; }

    /// <summary>Matrix symbols: modules per row.</summary>
    public int Width { get; init; }

    /// <summary>Matrix symbols: one hex string per row, most significant bit first, a set bit dark.</summary>
    public string[]? Rows { get; init; }

    [JsonIgnore]
    public BbBarcodeFormat BarcodeFormat => Enum.Parse<BbBarcodeFormat>(Format);

    [JsonIgnore]
    public bool IsLinear => Widths is not null;

    [JsonIgnore]
    public byte[]? ExpectedBytes => ExpectedBytesHex is null ? null : Convert.FromHexString(ExpectedBytesHex);

    /// <summary>Applies the decoder options the fixture says it needs.</summary>
    public BbScannerOptions Configure(BbScannerOptions options)
    {
        options.Formats = BarcodeFormat;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        foreach (var option in DecoderOptions)
        {
            switch (option)
            {
                case nameof(BbScannerOptions.Code39CheckDigit):
                    options.Code39CheckDigit = true;
                    break;
                case nameof(BbScannerOptions.Code39ExtendedMode):
                    options.Code39ExtendedMode = true;
                    break;
                case nameof(BbScannerOptions.CodabarStartStop):
                    options.CodabarStartStop = true;
                    break;
                case nameof(BbScannerOptions.RequireEanExtension):
                    options.RequireEanExtension = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown decoder option '{option}' in fixture {Id}.");
            }
        }

        return options;
    }

    /// <summary>The module grid of a matrix symbol.</summary>
    public BbBitMatrix ToMatrix()
    {
        var rows = Rows ?? throw new InvalidOperationException($"{Id} is a linear symbol.");
        var matrix = new BbBitMatrix(Width, rows.Length);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var nibble = Convert.ToInt32(rows[y][x / 4].ToString(), 16);
                if ((nibble & (8 >> (x % 4))) != 0)
                {
                    matrix[x, y] = true;
                }
            }
        }

        return matrix;
    }

    /// <summary>Renders the symbol with its quiet zone, at <paramref name="scale"/> pixels per module.</summary>
    public BbLuminanceBuffer Render(int scale)
    {
        if (IsLinear)
        {
            return SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(Widths), scale, height: Math.Max(60, 40 * scale));
        }

        using var matrix = ToMatrix();
        return SyntheticImage.FromMatrix(matrix, scale);
    }

    public override string ToString() => Id;
}
