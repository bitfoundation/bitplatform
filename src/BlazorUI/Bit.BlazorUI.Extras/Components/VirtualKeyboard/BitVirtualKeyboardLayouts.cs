using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The built-in layouts of <see cref="BitVirtualKeyboard"/>: the language layouts in their Full (hardware-like) and
/// Compact (phone-like) forms, and the purpose pads.
/// </summary>
/// <remarks>
/// Every access builds the layout afresh, so a page that customizes one (with <c>with { ... }</c>, copying the
/// collections it changes) never changes it for another page.
/// </remarks>
public static partial class BitVirtualKeyboardLayouts
{
    private const string CompactSuffix = "-compact";

    /// <summary>
    /// US English (QWERTY), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout English => CreateFull(EnglishData());

    /// <summary>
    /// US English (QWERTY), Compact form.
    /// </summary>
    public static BitVirtualKeyboardLayout EnglishCompact => CreateCompact(EnglishData());

    /// <summary>
    /// The ids of every built-in layout; a Compact form's id is its Full form's id followed by "-compact".
    /// </summary>
    public static IReadOnlyList<string> Ids => [.. Languages.Keys.SelectMany(id => new[] { id, id + CompactSuffix }), .. Pads.Keys];

    /// <summary>
    /// The built-in layout of an id (case-insensitive), or null.
    /// </summary>
    public static BitVirtualKeyboardLayout? Get(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var compact = id.EndsWith(CompactSuffix, StringComparison.OrdinalIgnoreCase);
        var languageId = compact ? id[..^CompactSuffix.Length] : id;

        if (Languages.TryGetValue(languageId, out var data))
        {
            var language = data();
            return compact ? CreateCompact(language) : CreateFull(language);
        }

        return compact is false && Pads.TryGetValue(id, out var pad) ? pad() : null;
    }

    /// <summary>
    /// The built-in language layout of a culture: its full name ("en-GB") if there is one, else its language ("fa"),
    /// else null.
    /// </summary>
    public static BitVirtualKeyboardLayout? ForCulture(CultureInfo culture, BitVirtualKeyboardForm form)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var suffix = form is BitVirtualKeyboardForm.Compact ? CompactSuffix : "";

        if (culture.Name.Length > 0 && Languages.ContainsKey(culture.Name)) return Get(culture.Name + suffix);
        if (culture.TwoLetterISOLanguageName == "fr") return Get("fr-t-k0-windows" + suffix);
        if (Languages.ContainsKey(culture.TwoLetterISOLanguageName)) return Get(culture.TwoLetterISOLanguageName + suffix);

        return null;
    }



    // The language layouts by id, each built on demand.
    private static readonly Dictionary<string, Func<BitVirtualKeyboardLanguage>> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = EnglishData,
        ["en-GB"] = EnglishUkData,
        ["fa"] = PersianData,
        ["ar"] = ArabicData,
        ["he"] = HebrewData,
        ["de"] = GermanData,
        ["fr-t-k0-windows"] = FrenchData,
        ["es"] = SpanishData,
        ["ru"] = RussianData,
        ["tr"] = TurkishData,
    };

    internal static BitVirtualKeyboardLayout CreateFull(BitVirtualKeyboardLanguage language)
    {
        var keys = new Dictionary<string, BitVirtualKeyboardKey>(language.Keys);
        if (language.Iso)
        {
            keys["iso-enter"] = new() { Action = BitVirtualKeyboardKeyAction.Enter, Height = 2 };
        }

        var layers = new List<BitVirtualKeyboardLayer>
        {
            new() { Id = "base", Rows = FullRows(language, language.Base, "base") },
            new() { Id = "shift", Modifiers = BitVirtualKeyboardModifiers.Shift, Rows = FullRows(language, language.Shift, "shift") },
        };

        if (language.AltGr is not null)
        {
            layers.Add(new() { Id = "altgr", Modifiers = BitVirtualKeyboardModifiers.AltGr, Rows = FullRows(language, language.AltGr, "altgr") });
        }

        if (language.ShiftAltGr is not null)
        {
            layers.Add(new() { Id = "shift-altgr", Modifiers = BitVirtualKeyboardModifiers.Shift | BitVirtualKeyboardModifiers.AltGr, Rows = FullRows(language, language.ShiftAltGr, "shift-altgr") });
        }

        return new()
        {
            Id = language.Id,
            Name = language.Name,
            Indicator = language.Indicator,
            Culture = language.Culture,
            Direction = language.Direction,
            Form = BitVirtualKeyboardForm.Full,
            Keys = keys,
            Layers = layers,
            LongPress = WithUpperCase(language.LongPress, language),
            DeadKeys = language.DeadKeys.ToDictionary(p => p.Key, p => new Dictionary<string, string>(p.Value)),
            Displays = new(language.Displays),
        };
    }

    // Row A of a Full layout: no symbol layers, since every symbol is on a key of its own, as on hardware.
    private const string FullRowA = "{lang*1.5} {space} {altgr*1.5} {hide*1.5}";

    private static List<BitVirtualKeyboardRow> FullRows(BitVirtualKeyboardLanguage language, BitVirtualKeyboardLanguageRows rows, string layer)
    {
        var e = Tokens(rows.E, 13, language, layer, "E");
        var d = Tokens(rows.D, 13, language, layer, "D");
        var c = Tokens(rows.C, 11, language, layer, "C");
        var b = Tokens(rows.B, 11, language, layer, "B");

        if (language.Iso)
        {
            return
            [
                Join(e, "{bksp*2}"),
                Join(["{tab*1.75}", .. d[..12], "{iso-enter*1.25}"]),
                Join(["{caps*1.75}", .. c, d[12]]),
                Join(["{shift*1.25}", .. b, "{shift*2.75}"]),
                FullRowA,
            ];
        }

        return
        [
            Join(e, "{bksp*2}"),
            Join(["{tab*1.5}", .. d[..12], WithWidth(d[12], "1.5")]),
            Join(["{caps*1.75}", .. c, "{enter*2.25}"]),
            Join(["{shift*2.25}", .. b[1..], "{shift*2.75}"]),
            FullRowA,
        ];
    }

    internal static BitVirtualKeyboardLayout CreateCompact(BitVirtualKeyboardLanguage language)
    {
        var baseRows = new List<string>();
        var shiftRows = new List<string>();

        var rows = new[]
        {
            (Base: Tokens(language.Base.D, 13, language, "base", "D")[..12], Shift: Tokens(language.Shift.D, 13, language, "shift", "D")[..12]),
            (Base: Tokens(language.Base.C, 11, language, "base", "C"), Shift: Tokens(language.Shift.C, 11, language, "shift", "C")),
            (Base: Tokens(language.Base.B, 11, language, "base", "B"), Shift: Tokens(language.Shift.B, 11, language, "shift", "B")),
        };

        for (var r = 0; r < rows.Length; r++)
        {
            var letters = Enumerable.Range(0, rows[r].Base.Length).Where(i => IsLetterToken(rows[r].Base[i])).ToList();
            var baseLetters = letters.Select(i => rows[r].Base[i]).ToList();
            var shiftLetters = letters.Select(i => IsLetterToken(rows[r].Shift[i]) ? rows[r].Shift[i] : rows[r].Base[i]).ToList();

            if (r < 2)
            {
                baseRows.Add(Join([.. baseLetters]));
                shiftRows.Add(Join([.. shiftLetters]));
                continue;
            }

            var backspace = WithWidth("{bksp}", language.CompactBackspaceWidth.ToString(CultureInfo.InvariantCulture));
            string[] prefix = language.CompactRow3Prefix is { } p ? p.Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];

            if (language.HasCase)
            {
                baseRows.Add(Join(["{shift*1.5}", .. prefix, .. baseLetters, backspace]));
                shiftRows.Add(Join(["{shift*1.5}", .. prefix, .. shiftLetters, backspace]));
            }
            else
            {
                baseRows.Add(Join([.. prefix, .. baseLetters, backspace]));
            }
        }

        var bottom = "{sym*1.5} {lang} {comma} {space}" + (language.CompactZwnj ? " {zwnj}" : "") + " . {tld} {enter*1.5}";
        baseRows.Add(bottom);
        shiftRows.Add(bottom);

        var layers = new List<BitVirtualKeyboardLayer>
        {
            new() { Id = "base", Rows = [.. baseRows.Select(r => (BitVirtualKeyboardRow)r)] },
        };

        if (language.HasCase)
        {
            layers.Add(new() { Id = "shift", Modifiers = BitVirtualKeyboardModifiers.Shift, Rows = [.. shiftRows.Select(r => (BitVirtualKeyboardRow)r)] });
        }

        layers.AddRange(SymbolLayers(language.Symbols));

        var displays = new Dictionary<string, string>(language.Displays);
        foreach (var (key, value) in SymbolDisplays(language.Symbols))
        {
            displays.TryAdd(key, value);
        }

        return new()
        {
            Id = language.Id + CompactSuffix,
            Name = language.Name,
            Indicator = language.Indicator,
            Culture = language.Culture,
            Direction = language.Direction,
            Form = BitVirtualKeyboardForm.Compact,
            CompactOf = language.Id,
            Keys = new(language.Keys),
            Layers = layers,
            LongPress = WithUpperCase(language.LongPress, language),
            DeadKeys = language.DeadKeys.ToDictionary(p => p.Key, p => new Dictionary<string, string>(p.Value)),
            Displays = displays,
        };
    }

    // The long-press table with the upper case of every single-letter entry the table lacks, so the shift layer offers
    // the capitals of what the base layer offers.
    private static Dictionary<string, string> WithUpperCase(Dictionary<string, string> longPress, BitVirtualKeyboardLanguage language)
    {
        var table = new Dictionary<string, string>(longPress);
        if (language.HasCase is false) return table;

        var culture = CultureOf(language.Culture);
        foreach (var (key, value) in longPress)
        {
            if (BitVirtualKeyboardLayoutCompiler.IsSingleLetter(key) is false) continue;

            var upper = BitVirtualKeyboardLayoutCompiler.ToUpper(key, culture);
            if (upper != key)
            {
                table.TryAdd(upper, BitVirtualKeyboardLayoutCompiler.ToUpper(value, culture));
            }
        }

        return table;
    }

    private static string[] Tokens(string row, int count, BitVirtualKeyboardLanguage language, string layer, string name)
    {
        var tokens = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != count)
        {
            throw new InvalidOperationException($"The built-in layout \"{language.Id}\" has {tokens.Length} keys in row {name} of its {layer} layer instead of {count}.");
        }

        return tokens;
    }

    // Whether a token of the data types a letter (or a string starting with one, "لا").
    private static bool IsLetterToken(string token)
    {
        var output = BitVirtualKeyboardShorthand.ParseRow(token).Keys[0].Output;
        return output is { Length: > 0 } && System.Text.Rune.TryGetRuneAt(output, 0, out var rune) && System.Text.Rune.IsLetter(rune);
    }

    // A token with a width: inside the braces of a named key, after any other.
    private static string WithWidth(string token, string width)
    {
        return token.Length >= 3 && token[0] == '{' && token[^1] == '}'
            ? $"{token[..^1]}*{width}}}"
            : $"{token}*{width}";
    }

    private static string Join(params IEnumerable<string> tokens) => string.Join(' ', tokens);

    private static string Join(string[] tokens, string last) => string.Join(' ', tokens.Append(last));
}