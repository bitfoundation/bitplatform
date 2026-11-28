namespace Bit.BlazorUI;

// The Latin language layouts, from layout-data.md (docs/plans/BitVirtualKeyboard): rows in scan-code order, E the 13 keys
// of the number row, D the 12 keys from Q plus the scan-2B key, C the 11 keys from A, B the scan-56 (ISO) key plus the
// 10 keys from Z. In the shorthand a lone backslash is written "\\" here, as C# needs.
public static partial class BitVirtualKeyboardLayouts
{
    /// <summary>
    /// UK English (QWERTY, ISO), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout EnglishUk => CreateFull(EnglishUkData());

    /// <summary>
    /// German (QWERTZ, ISO), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout German => CreateFull(GermanData());

    /// <summary>
    /// French (legacy Windows AZERTY, ISO), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout French => CreateFull(FrenchData());

    /// <summary>
    /// Spanish (QWERTY, ISO), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Spanish => CreateFull(SpanishData());

    /// <summary>
    /// Turkish (Q, ISO), Full form.
    /// </summary>
    public static BitVirtualKeyboardLayout Turkish => CreateFull(TurkishData());

    // US English (KBDUS), ANSI.
    private static BitVirtualKeyboardLanguage EnglishData() => new()
    {
        Id = "en",
        Name = "English (US)",
        Indicator = "EN",
        Culture = "en-US",
        Base = new(
            E: "` 1 2 3 4 5 6 7 8 9 0 - =",
            D: "q w e r t y u i o p [ ] \\",
            C: "a s d f g h j k l ; '",
            B: "\\ z x c v b n m , . /"),
        Shift = new(
            E: "~ ! @ # $ % ^ & * ( ) _ +",
            D: "Q W E R T Y U I O P { } |",
            C: "A S D F G H J K L : \"",
            B: "| Z X C V B N M < > ?"),
        LongPress = new()
        {
            ["a"] = "à á â ä æ ã å ā",
            ["c"] = "ç",
            ["e"] = "è é ê ë ē",
            ["i"] = "ì í î ï",
            ["n"] = "ñ",
            ["o"] = "ò ó ô ö õ ø œ",
            ["u"] = "ù ú û ü",
            ["y"] = "ý ÿ",
            ["s"] = "ß",
            ["."] = ", ? ! ' \" : ;",
        },
    };

    // UK English (KBDUK), ISO.
    private static BitVirtualKeyboardLanguage EnglishUkData() => EnglishData() with
    {
        Id = "en-GB",
        Name = "English (UK)",
        Indicator = "UK",
        Culture = "en-GB",
        Iso = true,
        Base = new(
            E: "` 1 2 3 4 5 6 7 8 9 0 - =",
            D: "q w e r t y u i o p [ ] #",
            C: "a s d f g h j k l ; '",
            B: "\\ z x c v b n m , . /"),
        Shift = new(
            E: "¬ ! \" £ $ % ^ & * ( ) _ +",
            D: "Q W E R T Y U I O P { } ~",
            C: "A S D F G H J K L : @",
            B: "| Z X C V B N M < > ?"),
        AltGr = new(
            E: "¦ {blank} {blank} {blank} € {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank}",
            D: "{blank} {blank} é {blank} {blank} {blank} ú í ó {blank} {blank} {blank} \\",
            C: "á {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank}",
            B: Blanks(11)),
        ShiftAltGr = new(
            E: Blanks(13),
            D: "{blank} {blank} É {blank} {blank} {blank} Ú Í Ó {blank} {blank} {blank} |",
            C: "Á {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank}",
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "£" } },
    };

    // German (KBDGR), ISO.
    private static BitVirtualKeyboardLanguage GermanData() => new()
    {
        Id = "de",
        Name = "Deutsch",
        Indicator = "DE",
        Culture = "de-DE",
        Iso = true,
        Base = new(
            E: "{dead:circumflex} 1 2 3 4 5 6 7 8 9 0 ß {dead:acute}",
            D: "q w e r t z u i o p ü + #",
            C: "a s d f g h j k l ö ä",
            B: "< y x c v b n m , . -"),
        Shift = new(
            E: "° ! \" § $ % & / ( ) = ? {dead:grave}",
            D: "Q W E R T Z U I O P Ü * '",
            C: "A S D F G H J K L Ö Ä",
            B: "> Y X C V B N M ; : _"),
        AltGr = new(
            E: "{blank} {blank} ² ³ {blank} {blank} {blank} { [ ] } \\ {blank}",
            D: "@ {blank} € {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} ~ {blank}",
            C: Blanks(11),
            B: "| {blank} {blank} {blank} {blank} {blank} {blank} µ {blank} {blank} {blank}"),
        ShiftAltGr = new(
            E: "{blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} ẞ {blank}",
            D: Blanks(13),
            C: Blanks(11),
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "€" } },
        DeadKeys = DeadKeyTables("circumflex", "acute", "grave"),
        LongPress = new()
        {
            ["a"] = "ä à á",
            ["o"] = "ö ó ò",
            ["u"] = "ü ú ù",
            ["s"] = "ß",
            ["e"] = "é è ê",
        },
    };

    // French, legacy Windows AZERTY (KBDFR), ISO.
    private static BitVirtualKeyboardLanguage FrenchData() => new()
    {
        Id = "fr-t-k0-windows",
        Name = "Français (AZERTY)",
        Indicator = "FR",
        Culture = "fr-FR",
        Iso = true,
        Base = new(
            E: "² & é \" ' ( - è _ ç à ) =",
            D: "a z e r t y u i o p {dead:circumflex} $ *",
            C: "q s d f g h j k l m ù",
            B: "< w x c v b n , ; : !"),
        Shift = new(
            E: "{blank} 1 2 3 4 5 6 7 8 9 0 ° +",
            D: "A Z E R T Y U I O P {dead:diaeresis} £ µ",
            C: "Q S D F G H J K L M %",
            B: "> W X C V B N ? . / §"),
        AltGr = new(
            E: "{blank} {blank} {dead:tilde} # { [ | {dead:grave} \\ ^ @ ] }",
            D: "{blank} {blank} € {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} ¤ {blank}",
            C: Blanks(11),
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "€" } },
        DeadKeys = DeadKeyTables("circumflex", "diaeresis", "tilde", "grave"),
        LongPress = new()
        {
            ["e"] = "é è ê ë",
            ["a"] = "à â æ",
            ["u"] = "ù û ü",
            ["i"] = "î ï",
            ["o"] = "ô œ",
            ["c"] = "ç",
            ["y"] = "ÿ",
        },
    };

    // Spanish (KBDSP), ISO.
    private static BitVirtualKeyboardLanguage SpanishData() => new()
    {
        Id = "es",
        Name = "Español",
        Indicator = "ES",
        Culture = "es-ES",
        Iso = true,
        Base = new(
            E: "º 1 2 3 4 5 6 7 8 9 0 ' ¡",
            D: "q w e r t y u i o p {dead:grave} + ç",
            C: "a s d f g h j k l ñ {dead:acute}",
            B: "< z x c v b n m , . -"),
        Shift = new(
            E: "ª ! \" · $ % & / ( ) = ? ¿",
            D: "Q W E R T Y U I O P {dead:circumflex} * Ç",
            C: "A S D F G H J K L Ñ {dead:diaeresis}",
            B: "> Z X C V B N M ; : _"),
        AltGr = new(
            E: "\\ | @ # {dead:tilde} € ¬ {blank} {blank} {blank} {blank} {blank} {blank}",
            D: "{blank} {blank} € {blank} {blank} {blank} {blank} {blank} {blank} {blank} [ ] }",
            C: "{blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {",
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "€" } },
        DeadKeys = DeadKeyTables("grave", "acute", "circumflex", "diaeresis", "tilde"),
        LongPress = new()
        {
            ["a"] = "á",
            ["e"] = "é",
            ["i"] = "í",
            ["o"] = "ó",
            ["u"] = "ú ü",
            ["n"] = "ñ",
            ["?"] = "¿",
            ["!"] = "¡",
        },
    };

    // Turkish Q (KBDTUQ), ISO.
    private static BitVirtualKeyboardLanguage TurkishData() => new()
    {
        Id = "tr",
        Name = "Türkçe (Q)",
        Indicator = "TR",
        Culture = "tr-TR",
        Iso = true,
        Base = new(
            E: "\" 1 2 3 4 5 6 7 8 9 0 * -",
            D: "q w e r t y u ı o p ğ ü ,",
            C: "a s d f g h j k l ş i",
            B: "< z x c v b n m ö ç ."),
        Shift = new(
            E: "é ! ' {dead:circumflex} + % & / ( ) = ? _",
            D: "Q W E R T Y U I O P Ğ Ü ;",
            C: "A S D F G H J K L Ş İ",
            B: "> Z X C V B N M Ö Ç :"),
        AltGr = new(
            E: "< > £ # $ ½ {blank} { [ ] } \\ |",
            D: "@ {blank} € {blank} ₺ {blank} {blank} i {blank} {blank} {dead:diaeresis} {dead:tilde} {dead:grave}",
            C: "æ ß {blank} {blank} {blank} {blank} {blank} {blank} {blank} {dead:acute} {blank}",
            B: "| {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank}"),
        ShiftAltGr = new(
            E: Blanks(13),
            D: "{blank} {blank} {blank} {blank} {blank} {blank} {blank} İ {blank} {blank} {blank} {blank} {blank}",
            C: "Æ {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank} {blank}",
            B: Blanks(11)),
        Keys = new() { ["cur"] = new() { Output = "₺" } },
        DeadKeys = TurkishDeadKeyTables(),
        LongPress = new()
        {
            ["a"] = "â",
            ["i"] = "î",
            ["u"] = "û",
        },
    };

    private static string Blanks(int count) => string.Join(' ', Enumerable.Repeat("{blank}", count));
}
