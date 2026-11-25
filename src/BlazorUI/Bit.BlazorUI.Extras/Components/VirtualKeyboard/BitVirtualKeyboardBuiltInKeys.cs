using System.Globalization;

namespace Bit.BlazorUI;

// The built-in named keys a shorthand row can name ({bksp}, {layer:symbols}, ...), and what each one is before the bag
// entry of the layout and the row's own values are merged over it.
internal static class BitVirtualKeyboardBuiltInKeys
{
    // The keys whose words come from the Strings of the keyboard rather than from the layout.
    public static readonly HashSet<string> StringsLabeled = ["clear", "accept", "cancel", "qty", "void", "total", "home", "end"];

    // The keys drawn with an icon.
    public static readonly HashSet<string> Iconed = ["bksp", "del", "enter", "tab", "shifttab", "shift", "caps", "hide", "left", "right", "up", "down", "lang"];

    public static bool IsBuiltIn(string name) => Get(name, null, new BitVirtualKeyboardCompileOptions()) is not null;

    public static BitVirtualKeyboardKey? Get(string name, string? argument, BitVirtualKeyboardCompileOptions options)
    {
        return name switch
        {
            "bksp" => new() { Action = BitVirtualKeyboardKeyAction.Backspace, Repeat = true },
            "del" => new() { Action = BitVirtualKeyboardKeyAction.Delete, Repeat = true },
            "enter" => new() { Action = BitVirtualKeyboardKeyAction.Enter },
            "space" => new() { Action = BitVirtualKeyboardKeyAction.Space, Output = " ", Width = 2, Stretch = true, Repeat = true },
            "tab" => new() { Action = BitVirtualKeyboardKeyAction.Tab },
            "shifttab" => new() { Action = BitVirtualKeyboardKeyAction.ShiftTab },
            "shift" => new() { Action = BitVirtualKeyboardKeyAction.Shift },
            "caps" => new() { Action = BitVirtualKeyboardKeyAction.CapsLock },
            "altgr" => new() { Action = BitVirtualKeyboardKeyAction.AltGr, Label = "AltGr" },
            "layer" when argument is not null => new() { Action = BitVirtualKeyboardKeyAction.SwitchLayer, Layer = argument },
            "abc" => new() { Action = BitVirtualKeyboardKeyAction.SwitchLayer, Label = "ABC" },
            "sym" => new() { Action = BitVirtualKeyboardKeyAction.SwitchLayer, Layer = "symbols", Label = "?123" },
            "dead" when argument is not null => new() { Action = BitVirtualKeyboardKeyAction.DeadKey, DeadKey = argument },
            "cmd" when argument is not null => new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = argument, Label = argument },
            "lang" => new() { Action = BitVirtualKeyboardKeyAction.NextLayout, Label = options.NextIndicator },
            "hide" => new() { Action = BitVirtualKeyboardKeyAction.Hide },
            "clear" => new() { Action = BitVirtualKeyboardKeyAction.Clear },
            "accept" => new() { Action = BitVirtualKeyboardKeyAction.Accept },
            "cancel" => new() { Action = BitVirtualKeyboardKeyAction.Cancel },
            "left" => new() { Action = BitVirtualKeyboardKeyAction.ArrowLeft, Repeat = true },
            "right" => new() { Action = BitVirtualKeyboardKeyAction.ArrowRight, Repeat = true },
            "up" => new() { Action = BitVirtualKeyboardKeyAction.ArrowUp, Repeat = true },
            "down" => new() { Action = BitVirtualKeyboardKeyAction.ArrowDown, Repeat = true },
            "home" => new() { Action = BitVirtualKeyboardKeyAction.Home },
            "end" => new() { Action = BitVirtualKeyboardKeyAction.End },
            "zwnj" => new() { Output = "\U0000200C", Label = "ZWNJ" },
            "zwj" => new() { Output = "\U0000200D", Label = "ZWJ" },
            "lrm" => new() { Output = "\U0000200E", Label = "LRM" },
            "rlm" => new() { Output = "\U0000200F", Label = "RLM" },
            "nbsp" => new() { Output = "\U000000A0", Label = "NBSP" },
            "gap" => new() { Gap = true },
            "blank" => new() { Blank = true },
            "decimal" => new() { Output = DecimalSeparator(options) },
            "datesep" => new() { Output = DateSeparator(options.Culture) },
            "comma" => new()
            {
                Output = ",",
                Variations = new()
                {
                    [BitVirtualKeyboardPurpose.Email] = new() { Output = "@" },
                    [BitVirtualKeyboardPurpose.Url] = new() { Output = "/" },
                },
            },
            "tld" => new()
            {
                Output = ".com",
                Width = 1.5,
                Purposes = [BitVirtualKeyboardPurpose.Email, BitVirtualKeyboardPurpose.Url],
                LongPress = [".net", ".org", ".edu", ".io"],
            },
            "cur" => new() { Output = "$" },
            "qty" => new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = "qty" },
            "void" => new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = "void" },
            "total" => new() { Action = BitVirtualKeyboardKeyAction.Enter },
            _ => null,
        };
    }

    // The decimal separator a key types: ICU gives fa-IR and the ar cultures the Arabic decimal separator even for Latin
    // digits, which a Latin number would not parse with, so Latin digits take the culture's separator only if it is a
    // period or a comma. A type=number input takes only a period.
    public static string DecimalSeparator(BitVirtualKeyboardCompileOptions options)
    {
        if (options.NumberTarget) return ".";

        if (options.Digits is BitVirtualKeyboardDigits.Latin)
        {
            var separator = options.Culture.NumberFormat.NumberDecimalSeparator;
            return separator is "." or "," ? separator : ".";
        }

        return "\U0000066B";
    }

    // The date separator of the culture, without the bidi marks some cultures wrap it in (ar-SA: RLM + "/").
    public static string DateSeparator(CultureInfo culture)
    {
        var separator = new string([.. culture.DateTimeFormat.DateSeparator.Where(c => c is not ('\x200E' or '\x200F' or '\x061C'))]);
        return separator.Length > 0 ? separator : "/";
    }
}