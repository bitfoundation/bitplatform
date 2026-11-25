namespace Bit.BlazorUI;

// The data of a built-in language layout, in the scan-code order of layout-data.md: row E is the 13 keys of the number
// row, D the 12 keys from Q plus the scan-2B key, C the 11 keys from A, and B the scan-56 (ISO) key plus the 10 keys from
// Z. A key with nothing on a layer is "{blank}". BitVirtualKeyboardLayouts builds the Full and the Compact form of the
// layout from it.
internal sealed record BitVirtualKeyboardLanguage
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Indicator { get; init; }
    public required string Culture { get; init; }
    public BitDir Direction { get; init; } = BitDir.Ltr;

    // An ISO board (the scan-2B key left of a two-row Enter, and the scan-56 key by the left Shift), else ANSI.
    public bool Iso { get; init; }

    // Whether the script has upper and lower case (no Shift in the Compact form of Arabic, Persian or Hebrew).
    public bool HasCase { get; init; } = true;

    public required BitVirtualKeyboardLanguageRows Base { get; init; }
    public required BitVirtualKeyboardLanguageRows Shift { get; init; }
    public BitVirtualKeyboardLanguageRows? AltGr { get; init; }
    public BitVirtualKeyboardLanguageRows? ShiftAltGr { get; init; }

    public Dictionary<string, BitVirtualKeyboardKey> Keys { get; init; } = [];
    public Dictionary<string, string> LongPress { get; init; } = [];
    public Dictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = [];
    public Dictionary<string, string> Displays { get; init; } = [];

    public BitVirtualKeyboardSymbolSet Symbols { get; init; } = BitVirtualKeyboardSymbolSet.Latin;

    // Tokens the third letter row of the Compact form starts with (Arabic: the ذ of row E).
    public string? CompactRow3Prefix { get; init; }

    // The width of the Backspace of the third letter row of the Compact form.
    public double CompactBackspaceWidth { get; init; } = 1.5;

    // Whether the Compact form has a ZWNJ key beside the space bar (Persian).
    public bool CompactZwnj { get; init; }
}

internal sealed record BitVirtualKeyboardLanguageRows(string E, string D, string C, string B);

internal enum BitVirtualKeyboardSymbolSet
{
    Latin,
    Persian,
    Arabic
}