using System.Globalization;

namespace Bit.BlazorUI;

// The shapes a BitVirtualKeyboardLayout is compiled into (BitVirtualKeyboardLayoutCompiler): every key merged with its
// bag entry and built-in key, filtered for the purpose and the mode, and placed on the grid. Internal: they are what the
// component renders and sends its script, never part of the public API.

internal enum BitVirtualKeyboardKeyKind
{
    Character,
    Function,
    Modifier,
    Primary
}

internal sealed record BitVirtualKeyboardCompileOptions
{
    public BitVirtualKeyboardPurpose Purpose { get; init; }
    public BitVirtualKeyboardMode Mode { get; init; }
    public int LanguagesCount { get; init; } = 1;
    public string? NextIndicator { get; init; }
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    public bool AutoShift { get; init; } = true;
    public BitVirtualKeyboardDigits Digits { get; init; }
    public bool NumberTarget { get; init; }
    public int? ScrambleSeed { get; init; }
}

internal sealed class BitVirtualKeyboardCompiledKey
{
    // The index of the key in the layout, across its layers: what the script knows it by (data-bit-vkb-k).
    public int Index { get; set; }

    // The bag id, the built-in name, or the text it types.
    public string KeyId { get; init; } = "";

    // The built-in key it is (bksp, enter, layer, abc, ...), for its icon and its words.
    public string? BuiltIn { get; init; }

    public BitVirtualKeyboardKeyAction Action { get; init; }
    public BitVirtualKeyboardKeyKind Kind { get; init; }
    public string? Output { get; init; }

    // The keycap; null for an icon key or a key whose words come from the Strings.
    public string? Label { get; init; }

    public string? Hint { get; init; }
    public BitIconInfo? Icon { get; init; }
    public string? AriaLabel { get; init; }
    public string? Layer { get; init; }
    public string? NextLayer { get; init; }
    public string? DeadKey { get; init; }
    public string? Command { get; init; }

    // The grid cell, in rows and quarter key units, 0-based.
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 4;

    public bool Gap { get; init; }
    public bool Blank { get; init; }
    public IReadOnlyList<string> LongPress { get; init; } = [];
    public string? LongPressDefault { get; init; }
    public IReadOnlyList<string> MultiTap { get; init; } = [];
    public bool Repeat { get; init; }
    public bool Preview { get; init; }
    public bool Scramble { get; init; }
    public string? Class { get; init; }
    public string? Style { get; init; }

    // The key as merged, which a KeyTemplate renders from.
    public BitVirtualKeyboardKey Source { get; init; } = default!;
}

internal sealed class BitVirtualKeyboardCompiledLayer
{
    public string Id { get; init; } = "";
    public string? Name { get; init; }
    public BitVirtualKeyboardModifiers Modifiers { get; init; }
    public int Rows { get; init; }
    public IReadOnlyList<BitVirtualKeyboardCompiledKey> Keys { get; init; } = [];
}

internal sealed class BitVirtualKeyboardCompiledLayout
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Indicator { get; init; }
    public BitDir Direction { get; init; }
    public BitVirtualKeyboardForm Form { get; init; }

    // The width of the grid, in quarter key units, the same for every layer.
    public int Columns { get; init; }

    public IReadOnlyList<BitVirtualKeyboardCompiledLayer> Layers { get; init; } = [];
    public IReadOnlyDictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = new Dictionary<string, Dictionary<string, string>>();

    public string? ShiftLayer { get; init; }
    public string? CapsLayer { get; init; }
    public string? AltGrLayer { get; init; }
    public string? ShiftAltGrLayer { get; init; }

    public BitVirtualKeyboardLayout Source { get; init; } = default!;

    public BitVirtualKeyboardCompiledLayer? GetLayer(string id) => Layers.FirstOrDefault(l => l.Id == id);

    public IEnumerable<BitVirtualKeyboardCompiledKey> AllKeys => Layers.SelectMany(l => l.Keys);
}