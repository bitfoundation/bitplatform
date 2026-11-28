using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

// The Cyrillic and right-to-left language layouts, from layout-data.md (docs/plans/BitVirtualKeyboard), and the dead-key
// tables. The grid of a right-to-left layout is never mirrored: it keeps the places of the keys, as the hardware layouts
// do, and its paired brackets are already swapped in the data. A combining mark is written as its shorthand escape
// ("\\u{64E}"), so the source shows which mark it is.
public static partial class BitVirtualKeyboardLayouts
{
    /// <summary>
    /// Russian (JCUKEN, ANSI), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Russian => CreateFull(RussianData());

    /// <summary>
    /// Persian (ISIRI 9147, ANSI), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Persian => CreateFull(PersianData());

    /// <summary>
    /// Persian (ISIRI 9147), Compact form, with a ZWNJ key beside the space bar.
    /// </summary>
    public static BitVirtualKeyboardLayout PersianCompact => CreateCompact(PersianData());

    /// <summary>
    /// Arabic (101, ANSI), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Arabic => CreateFull(ArabicData());

    /// <summary>
    /// Hebrew (ANSI), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Hebrew => CreateFull(HebrewData());

    // Russian JCUKEN (KBDRU), ANSI.
    private static BitVirtualKeyboardLanguage RussianData() => new()
    {
        Id = "ru",
        Name = "Русский",
        Indicator = "RU",
        Culture = "ru-RU",
        Base = new(
            E: "ё 1 2 3 4 5 6 7 8 9 0 - =",
            D: "й ц у к е н г ш щ з х ъ \\",
            C: "ф ы в а п р о л д ж э",
            B: "\\ я ч с м и т ь б ю ."),
        Shift = new(
            E: "Ё ! \" № ; % : ? * ( ) _ +",
            D: "Й Ц У К Е Н Г Ш Щ З Х Ъ /",
            C: "Ф Ы В А П Р О Л Д Ж Э",
            B: "/ Я Ч С М И Т Ь Б Ю ,"),
        AltGr = new(
            E: "{blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} ₽ {blank} {blank} {blank} {blank}",
            D: Blanks(13),
            C: Blanks(11),
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "₽" } },
        LongPress = new() { ["е"] = "ё" },
    };

    // Persian, the national standard ISIRI 9147, ANSI. ISIRI has no scan-56 key; its \ | come from KBDFAR.
    private static BitVirtualKeyboardLanguage PersianData() => new()
    {
        Id = "fa",
        Name = "فارسی (ISIRI 9147)",
        Indicator = "فا",
        Culture = "fa-IR",
        Direction = BitDir.Rtl,
        HasCase = false,
        Symbols = BitVirtualKeyboardSymbolSet.Persian,
        CompactZwnj = true,
        Base = new(
            E: "{zwj} ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ ۰ - =",
            D: "ض ص ث ق ف غ ع ه خ ح ج چ \\",
            C: "ش س ی ب ل ا ت ن م ک گ",
            B: "\\ ظ ط ز ر ذ د پ و . /"),
        Shift = new(
            E: "÷ ! ٬ ٫ ﷼ ٪ × ، * ) ( ـ +",
            D: "\\u{652} \\u{64C} \\u{64D} \\u{64B} \\u{64F} \\u{650} \\u{64E} \\u{651} ] [ } { |",
            C: "ؤ ئ ي إ أ آ ة » « : ؛",
            B: "| ك \\u{653} ژ \\u{670} {zwnj} \\u{654} ء > < ؟"),
        AltGr = new(
            E: "~ ` @ # $ % ^ & • {lrm} {rlm} _ −",
            D: "° {blank} € {blank} {blank} {blank} {blank} \\u{202D} \\u{202E} \\u{202C} \\u{202A} \\u{202B} \\u{2010}",
            C: "{blank} {blank} ى {blank} {blank} ٱ {blank} ﴾ ﴿ ; \"",
            B: "{blank} {blank} {blank} {blank} \\u{656} {zwj} \\u{655} … , ' ?"),
        Keys = new()
        {
            ["comma"] = new() { Output = "،" },
            ["cur"] = new() { Output = "﷼" },
        },
        Displays = new()
        {
            ["\U0000202D"] = "LRO",
            ["\U0000202E"] = "RLO",
            ["\U0000202C"] = "PDF",
            ["\U0000202A"] = "LRE",
            ["\U0000202B"] = "RLE",
        },
        LongPress = new()
        {
            ["ا"] = "آ أ إ ء",
            ["ی"] = "ئ ى",
            ["ه"] = "ۀ ة",
            ["و"] = "ؤ",
            ["ز"] = "ژ",
            ["."] = "، ؛ ؟ ! « »",
        },
    };

    // Arabic 101 (KBDA1), ANSI. Its digits are European, as on the hardware; the symbol layers of the Compact form type
    // the Arabic-Indic ones.
    private static BitVirtualKeyboardLanguage ArabicData() => new()
    {
        Id = "ar",
        Name = "العربية (101)",
        Indicator = "ع",
        Culture = "ar-SA",
        Direction = BitDir.Rtl,
        HasCase = false,
        Symbols = BitVirtualKeyboardSymbolSet.Arabic,
        CompactRow3Prefix = "ذ",
        CompactBackspaceWidth = 1,
        Base = new(
            E: "ذ 1 2 3 4 5 6 7 8 9 0 - =",
            D: "ض ص ث ق ف غ ع ه خ ح ج د \\",
            C: "ش س ي ب ل ا ت ن م ك ط",
            B: "\\ ئ ء ؤ ر لا ى ة و ز ظ"),
        Shift = new(
            E: "\\u{651} ! @ # $ % ^ & * ) ( _ +",
            D: "\\u{64E} \\u{64B} \\u{64F} \\u{64C} لإ إ ‘ ÷ × ؛ < > |",
            C: "\\u{650} \\u{64D} ] [ لأ أ ـ ، / : \"",
            B: "| ~ \\u{652} } { لآ آ ’ , . ؟"),
        Keys = new() { ["comma"] = new() { Output = "،" } },
        LongPress = new()
        {
            ["ا"] = "آ أ إ ء",
            ["ي"] = "ئ ى",
            ["ه"] = "ة",
            ["و"] = "ؤ",
            ["."] = "، ؛ ؟ !",
        },
    };

    // Hebrew (KBDHEB), ANSI. Shift types the Latin capitals, as on the hardware; the brackets are swapped.
    private static BitVirtualKeyboardLanguage HebrewData() => new()
    {
        Id = "he",
        Name = "עברית",
        Indicator = "עב",
        Culture = "he-IL",
        Direction = BitDir.Rtl,
        HasCase = false,
        Base = new(
            E: "; 1 2 3 4 5 6 7 8 9 0 - =",
            D: "/ ' ק ר א ט ו ן ם פ ] [ \\",
            C: "ש ד ג כ ע י ח ל ך ף ,",
            B: "\\ ז ס ב ה נ מ צ ת ץ ."),
        Shift = new(
            E: "~ ! @ # $ % ^ & * ) ( _ +",
            D: "Q W E R T Y U I O P } { |",
            C: "A S D F G H J K L : \"",
            B: "| Z X C V B N M > < ?"),
        AltGr = new(
            E: "{blank} {blank} {blank} {blank} ₪ {blank} {blank} {blank} {blank} {blank} {blank} \\u{5BF} {blank}",
            D: "{blank} {blank} € {blank} {blank} {blank} װ {blank} {blank} {blank} {blank} {blank} {blank}",
            C: "{blank} {blank} {blank} {blank} {blank} ײ ױ {blank} {blank} {blank} {blank}",
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "₪" } },
        LongPress = new() { ["."] = "׳ ״" },
    };

    // A dead key's table from Unicode composition: every letter a-z, A-Z that composes with the mark into one code point,
    // and a space for the dead key on its own. Turkish adds its dotless and dotted i.
    private static Dictionary<string, Dictionary<string, string>> DeadKeyTables(params string[] markers)
    {
        var tables = new Dictionary<string, Dictionary<string, string>>();

        foreach (var marker in markers)
        {
            var (mark, spacing) = marker switch
            {
                "grave" => ('\x0300', "`"),
                "acute" => ('\x0301', "\U000000B4"),
                "circumflex" => ('\x0302', "^"),
                "tilde" => ('\x0303', "~"),
                "diaeresis" => ('\x0308', "\U000000A8"),
                _ => throw new ArgumentOutOfRangeException(nameof(markers), marker, "An unknown dead key."),
            };

            var table = new Dictionary<string, string> { [" "] = spacing };

            foreach (var letter in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                var composed = string.Concat(letter, mark).Normalize(NormalizationForm.FormC);
                if (composed.Length == 1) table[letter.ToString()] = composed;
            }

            tables[marker] = table;
        }

        return tables;
    }

    // Turkish adds its dotless i and dotted capital I to the tables (KBDTUQ).
    private static Dictionary<string, Dictionary<string, string>> TurkishDeadKeyTables()
    {
        var tables = DeadKeyTables("circumflex", "diaeresis", "tilde", "grave", "acute");
        tables["circumflex"]["\x0131"] = "î";
        tables["diaeresis"]["\x0131"] = "ï";
        tables["grave"]["\x0131"] = "ì";
        tables["acute"]["\x0131"] = "í";
        tables["circumflex"]["\x0130"] = "Î";
        tables["diaeresis"]["\x0130"] = "Ï";
        tables["grave"]["\x0130"] = "Ì";
        tables["acute"]["\x0130"] = "Í";
        return tables;
    }

    // The culture of a language id, for the case mapping of the long-press capitals.
    private static CultureInfo CultureOf(string name)
    {
        try { return CultureInfo.GetCultureInfo(name); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }
}
