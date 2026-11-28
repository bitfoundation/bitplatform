using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A layout of a <see cref="BitVirtualKeyboard"/>: a bag of keys, the layers they are arranged in, and the tables of
/// long-press alternates, dead keys and keycap labels the keys share.
/// </summary>
/// <remarks>
/// A record: a built-in layout of <see cref="BitVirtualKeyboardLayouts"/> is customized with <c>with { ... }</c>.
/// A <c>with</c> copy is shallow and shares the collections of the original, so a collection that is to change is
/// copied first: <c>English with { Displays = new(English.Displays) { ["q"] = "Q!" } }</c>. The built-in layouts are
/// built afresh on every access, so changing one never reaches another.
/// </remarks>
public sealed record BitVirtualKeyboardLayout
{
    /// <summary>
    /// The id of the layout: a BCP 47 language tag, with a CLDR "-t-k0-" variant where a language has more than one
    /// layout ("fa", "fr-t-k0-windows"), or "x-..." for a purpose pad ("x-phone").
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// The name of the layout, as its own language writes it ("فارسی (ISIRI 9147)"). It also names the keyboard for
    /// assistive technologies where the keyboard has no AriaLabel.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The short text the language key shows for this layout ("FA").
    /// </summary>
    public string? Indicator { get; init; }

    /// <summary>
    /// The culture whose case mapping the derived shift and caps layers use ("fa-IR", "tr-TR").
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The direction of the text the layout types. The grid of keys itself is never mirrored.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<BitDir>))]
    public BitDir Direction { get; init; } = BitDir.Ltr;

    /// <summary>
    /// Whether this is the Full (hardware-like) or the Compact (phone-like) form of a language layout.
    /// </summary>
    public BitVirtualKeyboardForm Form { get; init; } = BitVirtualKeyboardForm.Full;

    /// <summary>
    /// For a Compact layout, the id of the Full layout it is the compact form of.
    /// </summary>
    public string? CompactOf { get; init; }

    /// <summary>
    /// The key bag: keys by id, which the rows reference by name (<c>{id}</c>) or, for a character key, by the text it
    /// types. A bag entry with the id of a built-in key (bksp, enter, comma, ...) overrides it.
    /// </summary>
    public Dictionary<string, BitVirtualKeyboardKey> Keys { get; init; } = [];

    /// <summary>
    /// The layers of the layout; the first is the base layer.
    /// </summary>
    public List<BitVirtualKeyboardLayer> Layers { get; init; } = [];

    /// <summary>
    /// The long-press alternates of the character keys, by the text the key types, as a space-separated list
    /// (<c>["e"] = "é è ê ë"</c>). A key's own LongPress wins over it.
    /// </summary>
    public Dictionary<string, string> LongPress { get; init; } = [];

    /// <summary>
    /// The dead keys, by marker: what each character types after the dead key (<c>["acute"] = { ["e"] = "é" }</c>),
    /// with the entry of a space (" ") being what the dead key types on its own.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> DeadKeys { get; init; } = [];

    /// <summary>
    /// The keycap labels, by key id or by the text a key types, for keys that show something other than what they type
    /// (<c>["*"] = "×"</c>).
    /// </summary>
    public Dictionary<string, string> Displays { get; init; } = [];



    /// <summary>
    /// Reads a layout from JSON. A row is either a shorthand string or an object with its keys; enums are written by
    /// name; property names are matched case-insensitively.
    /// </summary>
    public static BitVirtualKeyboardLayout FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize(json, BitVirtualKeyboardJsonContext.Default.BitVirtualKeyboardLayout)
            ?? throw new JsonException("The JSON holds no layout.");
    }

    /// <summary>
    /// Writes the layout as JSON (camelCase, enums by name, unset values left out).
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, BitVirtualKeyboardJsonContext.Default.BitVirtualKeyboardLayout);
}