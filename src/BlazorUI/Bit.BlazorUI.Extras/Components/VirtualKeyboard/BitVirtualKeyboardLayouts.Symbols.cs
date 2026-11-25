namespace Bit.BlazorUI;

public static partial class BitVirtualKeyboardLayouts
{
    // The two symbol layers of a Compact layout, every row 10 units wide. The {cur} key types the currency of the
    // layout's bag ($ by default).
    private static IEnumerable<BitVirtualKeyboardLayer> SymbolLayers(BitVirtualKeyboardSymbolSet set)
    {
        if (set is BitVirtualKeyboardSymbolSet.Latin)
        {
            const string bottom = "{abc*1.5} {lang} , {space} . {enter*1.5}";

            yield return new()
            {
                Id = "symbols",
                Rows =
                [
                    "1 2 3 4 5 6 7 8 9 0",
                    "@ # {cur} _ & - + ( ) /",
                    "{layer:symbols2*1.5} * \" ' : ; ! ? {bksp*1.5}",
                    bottom,
                ],
            };

            yield return new()
            {
                Id = "symbols2",
                Rows =
                [
                    "~ ` | • √ π ÷ × ¶ ∆",
                    @"£ ¢ € ¥ ^ ° = { } \\",
                    "{sym*1.5} % © ® ™ ✓ [ ] {bksp*1.5}",
                    bottom,
                ],
            };

            yield break;
        }

        // Persian and Arabic: their own digits and punctuation; brackets typed as their code points, which bidi
        // mirroring shows the right way round in right-to-left text.
        const string arabicBottom = "{abc*1.5} {lang} ، {space} . {enter*1.5}";

        yield return new()
        {
            Id = "symbols",
            Rows =
            [
                set is BitVirtualKeyboardSymbolSet.Persian ? "۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ ۰" : "١ ٢ ٣ ٤ ٥ ٦ ٧ ٨ ٩ ٠",
                "! @ # {cur} ٪ ^ & * ( )",
                "{layer:symbols2*1.5} - _ + « » : ؛ {bksp*1.5}",
                arabicBottom,
            ],
        };

        yield return new()
        {
            Id = "symbols2",
            Rows =
            [
                "~ ` | • ÷ × ٫ ٬ [ ]",
                @"{ } < > / \\ = "" ' ؟",
                "{sym*1.5} … ـ $ € £ ¥ ¢ {bksp*1.5}",
                arabicBottom,
            ],
        };
    }

    private static Dictionary<string, string> SymbolDisplays(BitVirtualKeyboardSymbolSet set) => set switch
    {
        BitVirtualKeyboardSymbolSet.Persian => new() { ["abc"] = "ابپ", ["sym"] = "؟۱۲۳", ["layer:symbols2"] = @"=\<" },
        BitVirtualKeyboardSymbolSet.Arabic => new() { ["abc"] = "أبت", ["sym"] = "؟١٢٣", ["layer:symbols2"] = @"=\<" },
        _ => new() { ["layer:symbols2"] = @"=\<" },
    };
}