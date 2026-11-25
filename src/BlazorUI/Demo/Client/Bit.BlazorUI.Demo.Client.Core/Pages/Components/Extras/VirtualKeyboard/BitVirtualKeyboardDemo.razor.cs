namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.VirtualKeyboard;

public partial class BitVirtualKeyboardDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowedCharacters",
            Type = "string?",
            DefaultValue = "null",
            Description = "The characters the keyboard may type, as the body of a regular-expression character class (e.g. \"0-9.,\"). A character outside it is not typed.",
        },
        new()
        {
            Name = "AutoShift",
            Type = "bool",
            DefaultValue = "true",
            Description = "Gives a layout that has a Shift key but no shift layer one derived from its letters, upper-cased in the culture of the layout.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitVirtualKeyboardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the virtual keyboard.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CompactBreakpoint",
            Type = "double",
            DefaultValue = "640",
            Description = "The width of the keyboard, in CSS pixels, under which an Auto Form renders the Compact layout.",
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo?",
            DefaultValue = "null",
            Description = "The culture of the decimal and date separators, the case mapping and the default language. Default: the current culture.",
        },
        new()
        {
            Name = "DefaultOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Whether a docked keyboard starts open, when Open is not bound.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the keyboard starts with, when Value is not bound.",
        },
        new()
        {
            Name = "Digits",
            Type = "BitVirtualKeyboardDigits",
            DefaultValue = "BitVirtualKeyboardDigits.Latin",
            Description = "The digits the purpose pads type, and the decimal separator they use.",
            LinkType = LinkType.Link,
            Href = "#digits-enum",
        },
        new()
        {
            Name = "EnterBehavior",
            Type = "BitVirtualKeyboardEnterBehavior",
            DefaultValue = "BitVirtualKeyboardEnterBehavior.Auto",
            Description = "What the Enter key does.",
            LinkType = LinkType.Link,
            Href = "#enter-behavior-enum",
        },
        new()
        {
            Name = "FollowFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Types into whichever input matching TargetSelector has the focus (by default the inputs carrying a data-bit-vkb attribute), opening a docked keyboard as one gets it and closing it as it leaves.",
        },
        new()
        {
            Name = "Form",
            Type = "BitVirtualKeyboardForm",
            DefaultValue = "BitVirtualKeyboardForm.Auto",
            Description = "The form of the language layouts: Full (hardware-like), Compact (phone-like), or Auto, Compact under CompactBreakpoint.",
            LinkType = LinkType.Link,
            Href = "#form-enum",
        },
        new()
        {
            Name = "Haptics",
            Type = "bool",
            DefaultValue = "false",
            Description = "Vibrates briefly on a key press, where the device can (not in Safari or desktop Firefox).",
        },
        new()
        {
            Name = "KeyTemplate",
            Type = "RenderFragment<BitVirtualKeyboardKeyContext>?",
            DefaultValue = "null",
            Description = "Renders the content of every key; the key itself stays the keyboard's.",
        },
        new()
        {
            Name = "Language",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the language layout (\"en\", \"fa\", ...). Default: the language of the culture, if there is a layout of it, else \"en\".",
        },
        new()
        {
            Name = "Languages",
            Type = "IReadOnlyList<string>?",
            DefaultValue = "null",
            Description = "The ids of the language layouts the language key cycles through; with fewer than two there is no language key.",
        },
        new()
        {
            Name = "Layout",
            Type = "BitVirtualKeyboardLayout?",
            DefaultValue = "null",
            Description = "A layout of the page's own, which wins over Language and the purpose pads.",
            LinkType = LinkType.Link,
            Href = "#layout",
        },
        new()
        {
            Name = "LayoutSelector",
            Type = "Func<BitVirtualKeyboardLayoutContext, BitVirtualKeyboardLayout?>?",
            DefaultValue = "null",
            Description = "Picks the layout for what is being typed; a null result falls back to the built-in choice.",
        },
        new()
        {
            Name = "LongPressDelay",
            Type = "int",
            DefaultValue = "500",
            Description = "The milliseconds a key is held before its long-press alternates open.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int?",
            DefaultValue = "null",
            Description = "The longest text the keyboard types into its own Value, in UTF-16 code units. An input it types into keeps to its own maxlength.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitVirtualKeyboardMode",
            DefaultValue = "BitVirtualKeyboardMode.Inline",
            Description = "Where the keyboard is shown: Inline in the page, or Docked to the bottom of the viewport.",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "MultiTapTimeout",
            Type = "int",
            DefaultValue = "1000",
            Description = "The milliseconds within which a repeated tap cycles a multitap key.",
        },
        new()
        {
            Name = "OnCommand",
            Type = "EventCallback<BitVirtualKeyboardCommandArgs>",
            DefaultValue = "",
            Description = "Raised by a key whose action is Custom (the Qty and Void keys of the POS pad, a {cmd:...} key).",
        },
        new()
        {
            Name = "OnEnter",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised when Enter is pressed, after what it does.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Raised when the text changes: the Value, or the value of the input the keyboard types into. Never raised in secure mode.",
        },
        new()
        {
            Name = "OnKeyPress",
            Type = "EventCallback<BitVirtualKeyboardKeyPressArgs>",
            DefaultValue = "",
            Description = "Raised before a key acts; a handler can cancel it or replace what it types. While it has a handler, every key waits for it.",
        },
        new()
        {
            Name = "OnLayerChange",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised when the active layer changes, with its id.",
        },
        new()
        {
            Name = "OnLayoutChange",
            Type = "EventCallback<BitVirtualKeyboardLayout>",
            DefaultValue = "",
            Description = "Raised when the layout changes (the language key, the purpose, the form).",
        },
        new()
        {
            Name = "Open",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a docked keyboard is shown. Two-way bindable.",
        },
        new()
        {
            Name = "Purpose",
            Type = "BitVirtualKeyboardPurpose?",
            DefaultValue = "null",
            Description = "What is being typed, which picks the layout. Default: what the input typed into says (its data-bit-vkb, inputmode and type attributes), else Text.",
            LinkType = LinkType.Link,
            Href = "#purpose-enum",
        },
        new()
        {
            Name = "RepeatDelay",
            Type = "int",
            DefaultValue = "500",
            Description = "The milliseconds a repeating key is held before it starts repeating; 0 turns repeating off.",
        },
        new()
        {
            Name = "RepeatInterval",
            Type = "int",
            DefaultValue = "50",
            Description = "The milliseconds between the repeats of a held key.",
        },
        new()
        {
            Name = "ReserveSpace",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the height of an open docked keyboard in the page, so the page can be scrolled above it.",
        },
        new()
        {
            Name = "Scramble",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shuffles the digit keys each time the keyboard opens or attaches to an input (for a PIN).",
        },
        new()
        {
            Name = "Secure",
            Type = "bool",
            DefaultValue = "false",
            Description = "Secure mode: no pressed paint, no preview, no sound, no announcement, and no typed text in OnKeyPress or OnInput. A bound Value still takes the text typed into it, through ValueChanged. Also on for a password input.",
        },
        new()
        {
            Name = "ShowKeyPreview",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows an enlarged copy of a character key while it is pressed.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the keys: Small (24px, the smallest WCAG target), Medium (44px) or Large (64px, for kiosks). The Size wins over --bit-VirtualKeyboard-key-height.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Sound",
            Type = "bool",
            DefaultValue = "false",
            Description = "Clicks on a key press.",
        },
        new()
        {
            Name = "Strings",
            Type = "BitVirtualKeyboardStrings?",
            DefaultValue = "null",
            Description = "The words of the keyboard: the names of its keys and what it announces.",
            LinkType = LinkType.Link,
            Href = "#strings",
        },
        new()
        {
            Name = "Styles",
            Type = "BitVirtualKeyboardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the virtual keyboard.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Target",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The input or textarea the keyboard types into.",
        },
        new()
        {
            Name = "TargetSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the inputs FollowFocus types into. Default: the inputs carrying a data-bit-vkb attribute, whose value may name a purpose (data-bit-vkb=\"pin\").",
        },
        new()
        {
            Name = "Value",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the keyboard types into when it has no input to type into. Two-way bindable.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "layout",
            Title = "BitVirtualKeyboardLayout",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "\"\"", Description = "The id of the layout: a BCP 47 language tag, with a CLDR \"-t-k0-\" variant where a language has more than one layout, or \"x-...\" for a purpose pad." },
                new() { Name = "Name", Type = "string", DefaultValue = "\"\"", Description = "The name of the layout, as its own language writes it; it also names the keyboard where it has no AriaLabel." },
                new() { Name = "Indicator", Type = "string?", DefaultValue = "null", Description = "The short text the language key shows for this layout." },
                new() { Name = "Culture", Type = "string?", DefaultValue = "null", Description = "The culture whose case mapping the derived shift and caps layers use." },
                new() { Name = "Direction", Type = "BitDir", DefaultValue = "BitDir.Ltr", Description = "The direction of the text the layout types. The grid of keys itself is never mirrored.", LinkType = LinkType.Link, Href = "#component-dir" },
                new() { Name = "Form", Type = "BitVirtualKeyboardForm", DefaultValue = "BitVirtualKeyboardForm.Full", Description = "Whether this is the Full or the Compact form of a language layout.", LinkType = LinkType.Link, Href = "#form-enum" },
                new() { Name = "CompactOf", Type = "string?", DefaultValue = "null", Description = "For a Compact layout, the id of the Full layout it is the compact form of." },
                new() { Name = "Keys", Type = "Dictionary<string, BitVirtualKeyboardKey>", DefaultValue = "[]", Description = "The key bag: keys by id, which the rows reference by name ({id}) or, for a character key, by the text it types.", LinkType = LinkType.Link, Href = "#key" },
                new() { Name = "Layers", Type = "List<BitVirtualKeyboardLayer>", DefaultValue = "[]", Description = "The layers of the layout; the first is the base layer.", LinkType = LinkType.Link, Href = "#layer" },
                new() { Name = "LongPress", Type = "Dictionary<string, string>", DefaultValue = "[]", Description = "The long-press alternates of the character keys, by the text the key types, as a space-separated list." },
                new() { Name = "DeadKeys", Type = "Dictionary<string, Dictionary<string, string>>", DefaultValue = "[]", Description = "The dead keys, by marker: what each character types after the dead key, the entry of a space being what the dead key types on its own." },
                new() { Name = "Displays", Type = "Dictionary<string, string>", DefaultValue = "[]", Description = "The keycap labels, by key id or by the text a key types." },
            ]
        },
        new()
        {
            Id = "layer",
            Title = "BitVirtualKeyboardLayer",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "\"\"", Description = "The id of the layer (base, shift, caps, altgr, symbols, ...), which layer keys switch to." },
                new() { Name = "Name", Type = "string?", DefaultValue = "null", Description = "The name of the layer, announced when it is switched to and shown on the keys that switch to it." },
                new() { Name = "Modifiers", Type = "BitVirtualKeyboardModifiers", DefaultValue = "None", Description = "The modifier state that selects this layer.", LinkType = LinkType.Link, Href = "#modifiers-enum" },
                new() { Name = "Rows", Type = "List<BitVirtualKeyboardRow>", DefaultValue = "[]", Description = "The rows of the layer, top to bottom; a row can be given as a shorthand string." },
            ]
        },
        new()
        {
            Id = "key",
            Title = "BitVirtualKeyboardKey",
            Parameters =
            [
                new() { Name = "Id", Type = "string?", DefaultValue = "null", Description = "The id the key is referenced by: a bag id, or the name of a built-in key." },
                new() { Name = "Output", Type = "string?", DefaultValue = "null", Description = "The text the key types; it can be more than one character." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "The keycap. Default: the layout's Displays entry of the key, else the text it types." },
                new() { Name = "Hint", Type = "string?", DefaultValue = "null", Description = "A small second label in a corner of the key." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon of the key, in place of a text label." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the bit icon of the key, in place of a text label." },
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "The accessible name of the key; it has to contain the visible label." },
                new() { Name = "Action", Type = "BitVirtualKeyboardKeyAction?", DefaultValue = "null", Description = "What the key does. Default: Character, which types its Output.", LinkType = LinkType.Link, Href = "#key-action-enum" },
                new() { Name = "Layer", Type = "string?", DefaultValue = "null", Description = "The layer a SwitchLayer key switches to." },
                new() { Name = "NextLayer", Type = "string?", DefaultValue = "null", Description = "The layer to go back to after the key has typed." },
                new() { Name = "DeadKey", Type = "string?", DefaultValue = "null", Description = "The marker a DeadKey key sets." },
                new() { Name = "Command", Type = "string?", DefaultValue = "null", Description = "The command a Custom key raises OnCommand with." },
                new() { Name = "Width", Type = "double?", DefaultValue = "null", Description = "The width of the key, in key units, in steps of 0.25. Default: 1." },
                new() { Name = "Height", Type = "int?", DefaultValue = "null", Description = "The number of rows the key spans downwards. Default: 1." },
                new() { Name = "Offset", Type = "double?", DefaultValue = "null", Description = "The empty room before the key, in key units. Default: 0." },
                new() { Name = "Stretch", Type = "bool?", DefaultValue = "null", Description = "Whether the key takes the room its row leaves." },
                new() { Name = "Gap", Type = "bool?", DefaultValue = "null", Description = "Whether the key is an invisible, inert spacer of its width." },
                new() { Name = "Blank", Type = "bool?", DefaultValue = "null", Description = "Whether the key is a visible but inert key." },
                new() { Name = "LongPress", Type = "List<string>?", DefaultValue = "null", Description = "The alternates a long press of the key offers." },
                new() { Name = "LongPressDefault", Type = "string?", DefaultValue = "null", Description = "The alternate the long-press popup starts on." },
                new() { Name = "MultiTap", Type = "List<string>?", DefaultValue = "null", Description = "The texts repeated taps of the key cycle through." },
                new() { Name = "Repeat", Type = "bool?", DefaultValue = "null", Description = "Whether the key repeats while held." },
                new() { Name = "Preview", Type = "bool?", DefaultValue = "null", Description = "Whether a press of the key shows the enlarged-key bubble." },
                new() { Name = "Purposes", Type = "List<BitVirtualKeyboardPurpose>?", DefaultValue = "null", Description = "The purposes the key is there for; it is left out for any other." },
                new() { Name = "Variations", Type = "Dictionary<BitVirtualKeyboardPurpose, BitVirtualKeyboardKey>?", DefaultValue = "null", Description = "What changes on the key for a purpose." },
                new() { Name = "Scramble", Type = "bool?", DefaultValue = "null", Description = "Whether the key takes part in scrambling. Default: the keys typing a single digit do." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes of the key." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "Custom CSS styles of the key." },
            ]
        },
        new()
        {
            Id = "key-press-args",
            Title = "BitVirtualKeyboardKeyPressArgs",
            Parameters =
            [
                new() { Name = "KeyId", Type = "string", DefaultValue = "\"\"", Description = "The id of the key: its bag id, its built-in name, or the text it types. Empty for a key that types a character in secure mode." },
                new() { Name = "Action", Type = "BitVirtualKeyboardKeyAction", DefaultValue = "Character", Description = "What the key does.", LinkType = LinkType.Link, Href = "#key-action-enum" },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "What the key will type; a handler may replace it. Null in secure mode." },
                new() { Name = "Layer", Type = "string", DefaultValue = "\"\"", Description = "The id of the layer the key is on." },
                new() { Name = "Cancel", Type = "bool", DefaultValue = "false", Description = "Set it to true to make the key do nothing." },
            ]
        },
        new()
        {
            Id = "command-args",
            Title = "BitVirtualKeyboardCommandArgs",
            Parameters =
            [
                new() { Name = "Command", Type = "string", DefaultValue = "\"\"", Description = "The command of the key." },
                new() { Name = "KeyId", Type = "string", DefaultValue = "\"\"", Description = "The id of the key." },
            ]
        },
        new()
        {
            Id = "strings",
            Title = "BitVirtualKeyboardStrings",
            Parameters =
            [
                new() { Name = "RoleDescription", Type = "string", DefaultValue = "\"virtual keyboard\"", Description = "What the keyboard is announced as, after its name." },
                new() { Name = "Backspace", Type = "string", DefaultValue = "\"Backspace\"", Description = "The name of the Backspace key." },
                new() { Name = "Delete", Type = "string", DefaultValue = "\"Delete\"", Description = "The name of the Delete key." },
                new() { Name = "Enter", Type = "string", DefaultValue = "\"Enter\"", Description = "The name of the Enter key." },
                new() { Name = "Done", Type = "string", DefaultValue = "\"Done\"", Description = "The name of the Enter key of an input whose enterkeyhint is done." },
                new() { Name = "Go", Type = "string", DefaultValue = "\"Go\"", Description = "The name of the Enter key of an input whose enterkeyhint is go." },
                new() { Name = "Next", Type = "string", DefaultValue = "\"Next\"", Description = "The name of the Enter key of an input whose enterkeyhint is next." },
                new() { Name = "Previous", Type = "string", DefaultValue = "\"Previous\"", Description = "The name of the Enter key of an input whose enterkeyhint is previous." },
                new() { Name = "Search", Type = "string", DefaultValue = "\"Search\"", Description = "The name of the Enter key of a search, or of an input whose enterkeyhint is search." },
                new() { Name = "Send", Type = "string", DefaultValue = "\"Send\"", Description = "The name of the Enter key of an input whose enterkeyhint is send." },
                new() { Name = "Space", Type = "string", DefaultValue = "\"Space\"", Description = "The name of the space bar." },
                new() { Name = "Tab", Type = "string", DefaultValue = "\"Tab\"", Description = "The name of the Tab key." },
                new() { Name = "ShiftTab", Type = "string", DefaultValue = "\"Shift Tab\"", Description = "The name of the Shift+Tab key." },
                new() { Name = "Shift", Type = "string", DefaultValue = "\"Shift\"", Description = "The name of the Shift key." },
                new() { Name = "CapsLock", Type = "string", DefaultValue = "\"Caps lock\"", Description = "The name of the Caps Lock key." },
                new() { Name = "AltGr", Type = "string", DefaultValue = "\"AltGr\"", Description = "The name of the AltGr key." },
                new() { Name = "Symbols", Type = "string", DefaultValue = "\"Symbols\"", Description = "The name of the symbols layer, and of the key that switches to it." },
                new() { Name = "MoreSymbols", Type = "string", DefaultValue = "\"More symbols\"", Description = "The name of the second symbols layer." },
                new() { Name = "Letters", Type = "string", DefaultValue = "\"Letters\"", Description = "The name of the base layer, and of the key that switches back to it." },
                new() { Name = "NextLanguage", Type = "string", DefaultValue = "\"Next language\"", Description = "The name of the key that moves to the next language." },
                new() { Name = "Hide", Type = "string", DefaultValue = "\"Hide keyboard\"", Description = "The name of the key that closes a docked keyboard." },
                new() { Name = "Clear", Type = "string", DefaultValue = "\"Clear\"", Description = "The name and label of the key that empties the value." },
                new() { Name = "Accept", Type = "string", DefaultValue = "\"Accept\"", Description = "The name and label of the key that commits the value." },
                new() { Name = "Cancel", Type = "string", DefaultValue = "\"Cancel\"", Description = "The name and label of the key that puts the value back." },
                new() { Name = "ArrowLeft", Type = "string", DefaultValue = "\"Left\"", Description = "The name of the left arrow key." },
                new() { Name = "ArrowRight", Type = "string", DefaultValue = "\"Right\"", Description = "The name of the right arrow key." },
                new() { Name = "ArrowUp", Type = "string", DefaultValue = "\"Up\"", Description = "The name of the up arrow key." },
                new() { Name = "ArrowDown", Type = "string", DefaultValue = "\"Down\"", Description = "The name of the down arrow key." },
                new() { Name = "Home", Type = "string", DefaultValue = "\"Home\"", Description = "The name and label of the Home key." },
                new() { Name = "End", Type = "string", DefaultValue = "\"End\"", Description = "The name and label of the End key." },
                new() { Name = "Qty", Type = "string", DefaultValue = "\"Qty\"", Description = "The name and label of the quantity key of the POS pad." },
                new() { Name = "Void", Type = "string", DefaultValue = "\"Void\"", Description = "The name and label of the void key of the POS pad." },
                new() { Name = "Total", Type = "string", DefaultValue = "\"Total\"", Description = "The name and label of the total key of the POS pad." },
                new() { Name = "Zwnj", Type = "string", DefaultValue = "\"zero-width non-joiner\"", Description = "The name of the zero-width non-joiner key." },
                new() { Name = "Zwj", Type = "string", DefaultValue = "\"zero-width joiner\"", Description = "The name of the zero-width joiner key." },
                new() { Name = "Lrm", Type = "string", DefaultValue = "\"left-to-right mark\"", Description = "The name of the left-to-right mark key." },
                new() { Name = "Rlm", Type = "string", DefaultValue = "\"right-to-left mark\"", Description = "The name of the right-to-left mark key." },
                new() { Name = "Nbsp", Type = "string", DefaultValue = "\"no-break space\"", Description = "The name of the no-break space key." },
                new() { Name = "ShiftOn", Type = "string", DefaultValue = "\"Shift on\"", Description = "What is announced when Shift is latched." },
                new() { Name = "ShiftLocked", Type = "string", DefaultValue = "\"Shift locked\"", Description = "What is announced when Shift is locked." },
                new() { Name = "ShiftOff", Type = "string", DefaultValue = "\"Shift off\"", Description = "What is announced when Shift is released." },
                new() { Name = "CapsLockOn", Type = "string", DefaultValue = "\"Caps lock on\"", Description = "What is announced when Caps Lock is turned on." },
                new() { Name = "CapsLockOff", Type = "string", DefaultValue = "\"Caps lock off\"", Description = "What is announced when Caps Lock is turned off." },
                new() { Name = "LayerAnnouncement", Type = "string", DefaultValue = "\"{0} keyboard\"", Description = "What is announced when the layer changes; {0} is the name of the layer." },
                new() { Name = "Alternates", Type = "string", DefaultValue = "\"Alternates for {0}\"", Description = "The name of the popup of long-press alternates; {0} is the key it is opened from." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitVirtualKeyboardClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitVirtualKeyboard." },
                new() { Name = "Panel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the panel of the keys (fixed to the bottom of the viewport in Docked mode) of the BitVirtualKeyboard." },
                new() { Name = "Layers", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the wrapper of the layers of the BitVirtualKeyboard." },
                new() { Name = "Layer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each layer of keys of the BitVirtualKeyboard." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each key of the BitVirtualKeyboard." },
                new() { Name = "CharacterKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each key that types a character (and the space bar) of the BitVirtualKeyboard." },
                new() { Name = "FunctionKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each function key (Backspace, Tab, the layer keys, ...) of the BitVirtualKeyboard." },
                new() { Name = "ModifierKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each modifier key (Shift, Caps Lock, AltGr) of the BitVirtualKeyboard." },
                new() { Name = "ActiveKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a key while it is active (a latched or locked modifier, a pending dead key, a held physical key); applied by the script of the BitVirtualKeyboard." },
                new() { Name = "PrimaryKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the Enter key of the BitVirtualKeyboard." },
                new() { Name = "Gap", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each invisible spacer of the BitVirtualKeyboard." },
                new() { Name = "Blank", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each visible but inert key of the BitVirtualKeyboard." },
                new() { Name = "KeyLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of each key of the BitVirtualKeyboard." },
                new() { Name = "KeyHint", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the small second label of each key of the BitVirtualKeyboard." },
                new() { Name = "KeyIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of each key of the BitVirtualKeyboard." },
                new() { Name = "Popup", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the popup of long-press alternates of the BitVirtualKeyboard." },
                new() { Name = "PopupKey", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each alternate of the long-press popup; applied by the script of the BitVirtualKeyboard." },
                new() { Name = "Preview", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the enlarged-key bubble shown while a key is pressed of the BitVirtualKeyboard." },
                new() { Name = "LiveRegion", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the visually hidden live region that announces the layer and modifier changes of the BitVirtualKeyboard." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "mode-enum",
            Name = "BitVirtualKeyboardMode",
            Description = "Where a BitVirtualKeyboard is shown.",
            Items =
            [
                new() { Name = "Inline", Value = "0", Description = "Rendered in place, in the flow of the page, and always shown." },
                new() { Name = "Docked", Value = "1", Description = "Fixed to the bottom of the viewport, shown while it is Open." },
            ]
        },
        new()
        {
            Id = "form-enum",
            Name = "BitVirtualKeyboardForm",
            Description = "The form of a language layout.",
            Items =
            [
                new() { Name = "Auto", Value = "0", Description = "Compact under the compact breakpoint of the keyboard's width, Full above it." },
                new() { Name = "Full", Value = "1", Description = "The hardware-like form: the number row, every symbol on its own key, the AltGr layer." },
                new() { Name = "Compact", Value = "2", Description = "The phone-like form: the letters, with the digits and symbols on layers of their own." },
            ]
        },
        new()
        {
            Id = "purpose-enum",
            Name = "BitVirtualKeyboardPurpose",
            Description = "What is being typed, which picks the layout.",
            Items =
            [
                new() { Name = "Text", Value = "0", Description = "Text: the language layout." },
                new() { Name = "Search", Value = "1", Description = "A search: the language layout, with a Search key." },
                new() { Name = "Email", Value = "2", Description = "An email address: the language layout with @ and .com keys." },
                new() { Name = "Url", Value = "3", Description = "A URL: the language layout with / and .com keys." },
                new() { Name = "Tel", Value = "4", Description = "A telephone number: the phone pad." },
                new() { Name = "Numeric", Value = "5", Description = "Digits: the numeric pad." },
                new() { Name = "Decimal", Value = "6", Description = "A decimal number: the decimal pad." },
                new() { Name = "SignedDecimal", Value = "7", Description = "A signed decimal number: the decimal pad with a minus key." },
                new() { Name = "Pin", Value = "8", Description = "A PIN: the PIN pad." },
                new() { Name = "Calculator", Value = "9", Description = "A calculation: the calculator pad." },
                new() { Name = "Pos", Value = "10", Description = "An amount at a point of sale: the POS pad." },
                new() { Name = "Hex", Value = "11", Description = "A hexadecimal number: the hex pad." },
                new() { Name = "Date", Value = "12", Description = "A date: the date pad." },
                new() { Name = "Time", Value = "13", Description = "A time: the time pad." },
                new() { Name = "Iban", Value = "14", Description = "An IBAN: digits and capital letters." },
                new() { Name = "Alphabetic", Value = "15", Description = "Letters in alphabetical order: the kiosk grid." },
                new() { Name = "Atm", Value = "16", Description = "An ATM-style PIN: the ATM pad." },
            ]
        },
        new()
        {
            Id = "digits-enum",
            Name = "BitVirtualKeyboardDigits",
            Description = "The digits the purpose pads type.",
            Items =
            [
                new() { Name = "Latin", Value = "0", Description = "0-9." },
                new() { Name = "ArabicIndic", Value = "1", Description = "The Arabic-Indic digits (U+0660-U+0669)." },
                new() { Name = "Persian", Value = "2", Description = "The Persian (extended Arabic-Indic) digits (U+06F0-U+06F9)." },
            ]
        },
        new()
        {
            Id = "enter-behavior-enum",
            Name = "BitVirtualKeyboardEnterBehavior",
            Description = "What the Enter key does.",
            Items =
            [
                new() { Name = "Auto", Value = "0", Description = "A new line in a textarea; the next or previous field for an enterkeyhint of next or previous; submits the form of any other input; nothing else." },
                new() { Name = "NewLine", Value = "1", Description = "Always types a new line." },
                new() { Name = "Submit", Value = "2", Description = "Always submits the form of the input." },
                new() { Name = "Next", Value = "3", Description = "Always moves the focus to the next field." },
                new() { Name = "Close", Value = "4", Description = "Closes a docked keyboard." },
                new() { Name = "None", Value = "5", Description = "Does nothing but raise OnEnter." },
            ]
        },
        new()
        {
            Id = "key-action-enum",
            Name = "BitVirtualKeyboardKeyAction",
            Description = "What a key does.",
            Items =
            [
                new() { Name = "Character", Value = "0", Description = "Types the key's Output." },
                new() { Name = "Backspace", Value = "1", Description = "Deletes backwards." },
                new() { Name = "Delete", Value = "2", Description = "Deletes forwards." },
                new() { Name = "Enter", Value = "3", Description = "The Enter key." },
                new() { Name = "Space", Value = "4", Description = "Types a space." },
                new() { Name = "Tab", Value = "5", Description = "Moves the focus to the next field." },
                new() { Name = "ShiftTab", Value = "6", Description = "Moves the focus to the previous field." },
                new() { Name = "Shift", Value = "7", Description = "Shift: one character from the shift layer; a double tap locks it." },
                new() { Name = "CapsLock", Value = "8", Description = "Caps Lock: toggles the caps layer." },
                new() { Name = "AltGr", Value = "9", Description = "AltGr: one character from the AltGr layer." },
                new() { Name = "SwitchLayer", Value = "10", Description = "Switches to the key's Layer, and stays there." },
                new() { Name = "DeadKey", Value = "11", Description = "Sets the key's dead-key marker; the next key is typed with it." },
                new() { Name = "NextLayout", Value = "12", Description = "Moves to the next of the keyboard's Languages." },
                new() { Name = "Hide", Value = "13", Description = "Closes a docked keyboard." },
                new() { Name = "Clear", Value = "14", Description = "Empties the value." },
                new() { Name = "Accept", Value = "15", Description = "Commits the value and closes a docked keyboard." },
                new() { Name = "Cancel", Value = "16", Description = "Puts the value back to what it was and closes a docked keyboard." },
                new() { Name = "ArrowLeft", Value = "17", Description = "Moves the caret left." },
                new() { Name = "ArrowRight", Value = "18", Description = "Moves the caret right." },
                new() { Name = "ArrowUp", Value = "19", Description = "Moves the caret up a line." },
                new() { Name = "ArrowDown", Value = "20", Description = "Moves the caret down a line." },
                new() { Name = "Home", Value = "21", Description = "Moves the caret to the start of the line." },
                new() { Name = "End", Value = "22", Description = "Moves the caret to the end of the line." },
                new() { Name = "Custom", Value = "23", Description = "Raises OnCommand with the key's Command." },
            ]
        },
        new()
        {
            Id = "modifiers-enum",
            Name = "BitVirtualKeyboardModifiers",
            Description = "The modifier state that selects a layer (flags).",
            Items =
            [
                new() { Name = "None", Value = "0", Description = "No modifier: the base layer, or a layer reached through a layer key." },
                new() { Name = "Shift", Value = "1", Description = "The shift layer." },
                new() { Name = "AltGr", Value = "2", Description = "The AltGr layer (with Shift, the Shift+AltGr layer)." },
                new() { Name = "Caps", Value = "4", Description = "The caps lock layer." },
            ]
        },
        new()
        {
            Id = "row-align-enum",
            Name = "BitVirtualKeyboardRowAlign",
            Description = "How a narrow row is placed.",
            Items =
            [
                new() { Name = "Center", Value = "0", Description = "Centered, as the letter rows of a keyboard are." },
                new() { Name = "Start", Value = "1", Description = "At the start (the left; the key grid is never mirrored)." },
                new() { Name = "End", Value = "2", Description = "At the end (the right)." },
            ]
        },
        DemoSharedEnums.BitSize(),
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new() { Name = "ActiveLayer", Type = "string?", DefaultValue = "", Description = "The id of the active layer, as last reported by the keyboard." },
        new() { Name = "ActiveLayout", Type = "BitVirtualKeyboardLayout?", DefaultValue = "", Description = "The layout the keyboard shows." },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-VirtualKeyboard-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Fill of the panel the keys sit on." },
        new() { Name = "--bit-VirtualKeyboard-padding", DefaultValue = "spacing(1)", Description = "Room around the keys." },
        new() { Name = "--bit-VirtualKeyboard-gap", DefaultValue = "spacing(0.75)", Description = "Room between the keys." },
        new() { Name = "--bit-VirtualKeyboard-key-height", DefaultValue = "max(44px, spacing(5.5))", Description = "Height of a row of keys. The Size parameter wins over it." },
        new() { Name = "--bit-VirtualKeyboard-radius", DefaultValue = "var(--bit-shp-radius-button)", Description = "Corner radius of a key." },
        new() { Name = "--bit-VirtualKeyboard-font-size", DefaultValue = "var(--bit-tg-fs-lg)", Description = "Size of a key's label." },
        new() { Name = "--bit-VirtualKeyboard-key-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Fill of a character key." },
        new() { Name = "--bit-VirtualKeyboard-key-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Label of a key." },
        new() { Name = "--bit-VirtualKeyboard-key-hover-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Fill of a key under the pointer." },
        new() { Name = "--bit-VirtualKeyboard-key-pressed-background", DefaultValue = "var(--bit-clr-bg-pri-active)", Description = "Fill of a key while it is pressed." },
        new() { Name = "--bit-VirtualKeyboard-function-key-background", DefaultValue = "var(--bit-clr-bg-ter)", Description = "Fill of a function or modifier key." },
        new() { Name = "--bit-VirtualKeyboard-active-key-background", DefaultValue = "var(--bit-clr-pri-light)", Description = "Fill of a latched or locked modifier, and of the selected long-press alternate." },
        new() { Name = "--bit-VirtualKeyboard-primary-key-background", DefaultValue = "var(--bit-clr-pri)", Description = "Fill of the Enter key." },
        new() { Name = "--bit-VirtualKeyboard-primary-key-color", DefaultValue = "var(--bit-clr-pri-text)", Description = "Label of the Enter key." },
        new() { Name = "--bit-VirtualKeyboard-shadow", DefaultValue = "var(--bit-box-shadow-sheet)", Description = "Shadow of a docked panel." },
        new() { Name = "--bit-VirtualKeyboard-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Focus ring of a key." },
    ];



    private string? basicValue;

    private ElementReference targetInput;
    private string? targetValue;

    private bool followOpen;

    private static readonly BitVirtualKeyboardPurpose[] purposes =
    [
        BitVirtualKeyboardPurpose.Tel, BitVirtualKeyboardPurpose.Pin, BitVirtualKeyboardPurpose.Decimal, BitVirtualKeyboardPurpose.Calculator,
        BitVirtualKeyboardPurpose.Pos, BitVirtualKeyboardPurpose.Hex, BitVirtualKeyboardPurpose.Date, BitVirtualKeyboardPurpose.Iban,
        BitVirtualKeyboardPurpose.Alphabetic, BitVirtualKeyboardPurpose.Atm, BitVirtualKeyboardPurpose.Email, BitVirtualKeyboardPurpose.Url,
    ];
    private BitVirtualKeyboardPurpose currentPurpose = BitVirtualKeyboardPurpose.Tel;
    private string? purposeValue;

    private string? languagesValue;

    private string? shorthandValue;
    private readonly BitVirtualKeyboardLayout shorthandLayout = new()
    {
        Id = "my-pad",
        Name = "My pad",
        Layers =
        [
            new() { Id = "base", Rows = ["a b c {bksp*1.5}", "d e f {layer:symbols*1.5}", "{space} {enter*1.5}"] },
            new() { Id = "symbols", Rows = ["1 2 3 {bksp*1.5}", "4 5 6 {abc*1.5}", "{space} {enter*1.5}"] },
        ],
    };

    private string? objectValue;
    private string? objectCommand;
    private readonly BitVirtualKeyboardLayout objectLayout = new()
    {
        Id = "my-objects",
        Name = "Objects",
        Keys = new()
        {
            ["e"] = new() { LongPress = ["é", "è", "ê", "ë"] },
            ["2"] = new() { Hint = "ABC", MultiTap = ["a", "b", "c"] },
            ["ok"] = new() { Action = BitVirtualKeyboardKeyAction.Enter, Height = 2, Label = "OK" },
            ["star"] = new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = "favorite", Label = "★" },
        },
        DeadKeys = new() { ["acute"] = new() { ["a"] = "á", ["e"] = "é", [" "] = "´" } },
        Layers =
        [
            new() { Id = "base", Rows = ["e 2 {dead:acute} {ok}", "a {star} {bksp}"] },
        ],
    };

    private readonly BitVirtualKeyboardLayout customizedLayout = BitVirtualKeyboardLayouts.EnglishCompact with
    {
        Id = "my-english",
        Displays = new(BitVirtualKeyboardLayouts.EnglishCompact.Displays) { ["sym"] = "#+=" },
    };

    private string? jsonValue;
    private readonly BitVirtualKeyboardLayout jsonLayout = BitVirtualKeyboardLayout.FromJson("""
        {
          "id": "json-pad",
          "name": "JSON pad",
          "layers": [
            { "id": "base", "rows": [ "7 8 9", "4 5 6", "1 2 3", "{bksp} 0 {enter}" ] }
          ]
        }
        """);

    private string? eventsValue;
    private string? eventsLayer;

    private static void HandleKeyPress(BitVirtualKeyboardKeyPressArgs args)
    {
        if (args.Action is BitVirtualKeyboardKeyAction.Backspace) args.Cancel = true;
        if (args.Text is not null) args.Text = args.Text.ToUpperInvariant();
    }

    private string? pinValue;

    // The input of the Target example is taken by @ref in the first render, so the keyboard is handed it in the next one.
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) StateHasChanged();

        base.OnAfterRender(firstRender);
    }
}