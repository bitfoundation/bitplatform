namespace Bit.BlazorUI;

public static partial class BitVirtualKeyboardLayouts
{
    /// <summary>
    /// The phone pad (ITU-T E.161): 1-2-3 on top, the letters of each digit as its hint, + on a long press of 0.
    /// </summary>
    public static BitVirtualKeyboardLayout Phone => Pad("x-phone", "Phone",
        ["1 2 3 {bksp}", "4 5 6 {space*1}", "7 8 9 {hide}", "* 0 # {enter}"],
        keys: new()
        {
            ["2"] = new() { Hint = "ABC" },
            ["3"] = new() { Hint = "DEF" },
            ["4"] = new() { Hint = "GHI" },
            ["5"] = new() { Hint = "JKL", Class = "bit-vkb-nub" },
            ["6"] = new() { Hint = "MNO" },
            ["7"] = new() { Hint = "PQRS" },
            ["8"] = new() { Hint = "TUV" },
            ["9"] = new() { Hint = "WXYZ" },
            ["0"] = new() { Hint = "+", LongPress = ["+"], LongPressDefault = "+" },
            ["*"] = new() { LongPress = [","] },
            ["#"] = new() { LongPress = [";"] },
        });

    /// <summary>
    /// The PIN pad: the digits in phone order, without the preview bubble.
    /// </summary>
    public static BitVirtualKeyboardLayout Pin => Pad("x-pin", "PIN",
        ["1 2 3", "4 5 6", "7 8 9", "{bksp} 0 {enter}"],
        keys: Enumerable.Range(0, 10).ToDictionary(d => d.ToString(System.Globalization.CultureInfo.InvariantCulture), _ => new BitVirtualKeyboardKey { Preview = false }));

    /// <summary>
    /// The numeric pad: the digits in phone order.
    /// </summary>
    public static BitVirtualKeyboardLayout Numeric => Pad("x-numeric", "Numeric", ["1 2 3", "4 5 6", "7 8 9", "{bksp} 0 {enter}"]);

    /// <summary>
    /// The decimal pad: the digits, the decimal separator of the culture and a tall Enter.
    /// </summary>
    public static BitVirtualKeyboardLayout Decimal => Pad("x-decimal", "Decimal",
        ["1 2 3 {bksp}", "4 5 6 {enter}", "7 8 9", "0*2 {decimal}"],
        keys: new() { ["enter"] = new() { Height = 3 } });

    /// <summary>
    /// The signed decimal pad: the decimal pad with a minus key.
    /// </summary>
    public static BitVirtualKeyboardLayout SignedDecimal => Pad("x-signed-decimal", "Signed decimal",
        ["1 2 3 {bksp}", "4 5 6 {enter}", "7 8 9", "- 0 {decimal}"],
        keys: new() { ["enter"] = new() { Height = 3 } });

    /// <summary>
    /// The calculator pad: 7-8-9 on top and the four operators, which type * / - + and show × ÷ −.
    /// </summary>
    public static BitVirtualKeyboardLayout Calculator => Pad("x-calculator", "Calculator",
        ["{clear} ( ) /", "7 8 9 *", "4 5 6 -", "1 2 3 +", "0*2 {decimal} ="],
        displays: new() { ["*"] = "×", ["/"] = "÷", ["-"] = "−" });

    /// <summary>
    /// The point-of-sale pad: 7-8-9 on top, 00 and 000, Qty and Void command keys (OnCommand), and a Total Enter.
    /// </summary>
    public static BitVirtualKeyboardLayout Pos => Pad("x-pos", "POS",
        ["7 8 9 {qty}", "4 5 6 {void}", "1 2 3 {clear}", "0 00 000 {decimal}", "{total*4}"]);

    /// <summary>
    /// The hexadecimal pad.
    /// </summary>
    public static BitVirtualKeyboardLayout Hex => Pad("x-hex", "Hex",
        ["7 8 9 F", "4 5 6 E", "1 2 3 D", "0 A B C", "{bksp*2} {enter*2}"]);

    /// <summary>
    /// The date pad: the digits and the date separator of the culture.
    /// </summary>
    public static BitVirtualKeyboardLayout Date => Pad("x-date", "Date", ["1 2 3", "4 5 6", "7 8 9", "{datesep} 0 {bksp}"]);

    /// <summary>
    /// The time pad: the digits and a colon.
    /// </summary>
    public static BitVirtualKeyboardLayout Time => Pad("x-time", "Time", ["1 2 3", "4 5 6", "7 8 9", ": 0 {bksp}"]);

    /// <summary>
    /// The IBAN pad: the digits and the capital letters.
    /// </summary>
    public static BitVirtualKeyboardLayout Iban => Pad("x-iban", "IBAN",
        ["1 2 3 4 5 6 7 8 9 0", "Q W E R T Y U I O P", "A S D F G H J K L {gap}", "Z X C V B N M {bksp*3}", "{space*8} {enter*2}"]);

    /// <summary>
    /// The alphabetical grid of a kiosk or a TV: the letters in order, with Shift.
    /// </summary>
    public static BitVirtualKeyboardLayout Alphabetic => Pad("x-abc", "ABC",
        ["a b c d e f g", "h i j k l m n", "o p q r s t u", "{shift} v w x y z {bksp}", "{space*5} {enter*2}"]);

    /// <summary>
    /// The ATM pad: the digits with Cancel, Clear and Enter beside them, and blank keys around the 0.
    /// </summary>
    public static BitVirtualKeyboardLayout Atm => Pad("x-atm", "ATM",
        ["1 2 3 {cancel}", "4 5 6 {clear}", "7 8 9 {enter}", "{blank} 0 {blank} {blank}"]);

    /// <summary>
    /// The pad of a purpose, or the English layout for the purposes typed with a language layout (Text, Search, Email,
    /// Url).
    /// </summary>
    public static BitVirtualKeyboardLayout ForPurpose(BitVirtualKeyboardPurpose purpose) => purpose switch
    {
        BitVirtualKeyboardPurpose.Tel => Phone,
        BitVirtualKeyboardPurpose.Numeric => Numeric,
        BitVirtualKeyboardPurpose.Decimal => Decimal,
        BitVirtualKeyboardPurpose.SignedDecimal => SignedDecimal,
        BitVirtualKeyboardPurpose.Pin => Pin,
        BitVirtualKeyboardPurpose.Calculator => Calculator,
        BitVirtualKeyboardPurpose.Pos => Pos,
        BitVirtualKeyboardPurpose.Hex => Hex,
        BitVirtualKeyboardPurpose.Date => Date,
        BitVirtualKeyboardPurpose.Time => Time,
        BitVirtualKeyboardPurpose.Iban => Iban,
        BitVirtualKeyboardPurpose.Alphabetic => Alphabetic,
        BitVirtualKeyboardPurpose.Atm => Atm,
        _ => English,
    };

    // Whether a purpose is typed with a language layout rather than a pad.
    internal static bool IsLanguagePurpose(BitVirtualKeyboardPurpose purpose) =>
        purpose is BitVirtualKeyboardPurpose.Text or BitVirtualKeyboardPurpose.Search or BitVirtualKeyboardPurpose.Email or BitVirtualKeyboardPurpose.Url;

    // The pads by id, each built on demand.
    private static readonly Dictionary<string, Func<BitVirtualKeyboardLayout>> Pads = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x-phone"] = () => Phone,
        ["x-pin"] = () => Pin,
        ["x-numeric"] = () => Numeric,
        ["x-decimal"] = () => Decimal,
        ["x-signed-decimal"] = () => SignedDecimal,
        ["x-calculator"] = () => Calculator,
        ["x-pos"] = () => Pos,
        ["x-hex"] = () => Hex,
        ["x-date"] = () => Date,
        ["x-time"] = () => Time,
        ["x-iban"] = () => Iban,
        ["x-abc"] = () => Alphabetic,
        ["x-atm"] = () => Atm,
    };

    private static BitVirtualKeyboardLayout Pad(string id, string name, string[] rows,
                                               Dictionary<string, BitVirtualKeyboardKey>? keys = null,
                                               Dictionary<string, string>? displays = null) => new()
    {
        Id = id,
        Name = name,
        Keys = keys ?? [],
        Displays = displays ?? [],
        Layers = [new() { Id = "base", Rows = [.. rows.Select(r => (BitVirtualKeyboardRow)r)] }],
    };
}
