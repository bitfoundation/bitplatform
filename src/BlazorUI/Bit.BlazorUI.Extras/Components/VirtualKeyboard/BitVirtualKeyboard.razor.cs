using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// An on-screen keyboard: a language layout (Full or Compact) or a purpose pad (phone, PIN, decimal, POS, ...) that types
/// into a bound Value, into one input, or into whichever opted-in input has the focus.
/// </summary>
/// <remarks>
/// The layouts are data (<see cref="BitVirtualKeyboardLayout"/>): the built-in ones of <see cref="BitVirtualKeyboardLayouts"/>,
/// or any layout written in the shorthand of <see cref="BitVirtualKeyboardShorthand"/>, as objects, or as JSON. The keys
/// are drawn by the component and act in the browser, so typing never waits for a round trip.
/// </remarks>
public partial class BitVirtualKeyboard : BitComponentBase
{
    private static readonly BitVirtualKeyboardStrings _defaultStrings = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;

    private BitVirtualKeyboardCompiledLayout _compiled = default!;
    private BitVirtualKeyboardPurpose _purpose;

    // What the scrambled layout shown was compiled from, so a render that changes none of it keeps it.
    private BitVirtualKeyboardCompileOptions? _scrambledOptions;
    private BitVirtualKeyboardLayout? _scrambledLayout;
    private string? _scrambledLayoutJson;

    // The form an Auto form shows, as the script measures the keyboard's width (Compact until it has).
    private bool _compact = true;

    // The index of the layout of Languages the language key has moved to.
    private int _languageIndex;

    // The layer the script last reported as active.
    private string? _activeLayer;



    /// <summary>
    /// The default values of the parameters of the virtual keyboard, cascaded through a <see cref="BitParams"/>. A value
    /// set on the keyboard itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitVirtualKeyboardParams.ParamName)]
    public BitVirtualKeyboardParams? CascadingParameters { get; set; }



    /// <summary>
    /// The characters the keyboard may type, as the body of a regular-expression character class (e.g. "0-9.,"). A
    /// character outside it is not typed.
    /// </summary>
    [Parameter] public string? AllowedCharacters { get; set; }

    /// <summary>
    /// Gives a layout that has a Shift key but no shift layer one derived from its letters. Default: true.
    /// </summary>
    [Parameter] public bool AutoShift { get; set; } = true;

    /// <summary>
    /// Custom CSS classes for the different parts of the virtual keyboard.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitVirtualKeyboardClassStyles? Classes { get; set; }

    /// <summary>
    /// The width of the keyboard, in CSS pixels, under which an Auto <see cref="Form"/> renders the Compact layout.
    /// Default: 640.
    /// </summary>
    [Parameter] public double CompactBreakpoint { get; set; } = 640;

    /// <summary>
    /// The culture of the decimal and date separators, the case mapping and the default language. Default: the current
    /// culture.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Whether a docked keyboard starts open, when <see cref="Open"/> is not bound.
    /// </summary>
    [Parameter] public bool? DefaultOpen { get; set; }

    /// <summary>
    /// The text the keyboard starts with, when <see cref="Value"/> is not bound.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// The digits the purpose pads type (and the decimal separator they use). Default: Latin.
    /// </summary>
    [Parameter] public BitVirtualKeyboardDigits Digits { get; set; }

    /// <summary>
    /// What the Enter key does. Default: Auto.
    /// </summary>
    [Parameter] public BitVirtualKeyboardEnterBehavior EnterBehavior { get; set; }

    /// <summary>
    /// Types into whichever input matching <see cref="TargetSelector"/> has the focus (by default the inputs carrying a
    /// data-bit-vkb attribute), opening a docked keyboard as one gets it and closing it as it leaves.
    /// </summary>
    [Parameter] public bool FollowFocus { get; set; }

    /// <summary>
    /// The form of the language layouts: Full (hardware-like), Compact (phone-like), or Auto, Compact under
    /// <see cref="CompactBreakpoint"/>. Default: Auto.
    /// </summary>
    [Parameter] public BitVirtualKeyboardForm Form { get; set; }

    /// <summary>
    /// Vibrates briefly on a key press, where the device can.
    /// </summary>
    [Parameter] public bool Haptics { get; set; }

    /// <summary>
    /// Renders the content of every key (the key itself stays the keyboard's).
    /// </summary>
    [Parameter] public RenderFragment<BitVirtualKeyboardKeyContext>? KeyTemplate { get; set; }

    /// <summary>
    /// The id of the language layout ("en", "fa", ...). Default: the language of the culture, if there is a layout of
    /// it, else "en".
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// The ids of the language layouts the language key cycles through; with fewer than two there is no language key.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? Languages { get; set; }

    /// <summary>
    /// A layout of the page's own, which wins over <see cref="Language"/> and the purpose pads.
    /// </summary>
    [Parameter] public BitVirtualKeyboardLayout? Layout { get; set; }

    /// <summary>
    /// Picks the layout for what is being typed; a null result falls back to the built-in choice.
    /// </summary>
    [Parameter] public Func<BitVirtualKeyboardLayoutContext, BitVirtualKeyboardLayout?>? LayoutSelector { get; set; }

    /// <summary>
    /// The milliseconds a key is held before its long-press alternates open. Default: 500.
    /// </summary>
    [Parameter] public int LongPressDelay { get; set; } = 500;

    /// <summary>
    /// The longest text the keyboard types into its own Value, in UTF-16 code units. An input it types into keeps to
    /// its own maxlength.
    /// </summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>
    /// Where the keyboard is shown: Inline in the page, or Docked to the bottom of the viewport. Default: Inline.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitVirtualKeyboardMode Mode { get; set; }

    /// <summary>
    /// The milliseconds within which a repeated tap cycles a multitap key. Default: 1000.
    /// </summary>
    [Parameter] public int MultiTapTimeout { get; set; } = 1000;

    /// <summary>
    /// Raised by a key whose action is Custom (the Qty and Void keys of the POS pad, a {cmd:...} key).
    /// </summary>
    [Parameter] public EventCallback<BitVirtualKeyboardCommandArgs> OnCommand { get; set; }

    /// <summary>
    /// Raised when Enter is pressed, after what it does.
    /// </summary>
    [Parameter] public EventCallback OnEnter { get; set; }

    /// <summary>
    /// Raised when the text changes: the Value, or the value of the input the keyboard types into. Never raised in
    /// secure mode.
    /// </summary>
    [Parameter] public EventCallback<string?> OnInput { get; set; }

    /// <summary>
    /// Raised before a key acts; a handler can cancel it or replace what it types. While it has a handler, every key waits
    /// for it.
    /// </summary>
    [Parameter] public EventCallback<BitVirtualKeyboardKeyPressArgs> OnKeyPress { get; set; }

    /// <summary>
    /// Raised when the active layer changes, with its id.
    /// </summary>
    [Parameter] public EventCallback<string> OnLayerChange { get; set; }

    /// <summary>
    /// Raised when the layout changes (the language key, the purpose, the form).
    /// </summary>
    [Parameter] public EventCallback<BitVirtualKeyboardLayout> OnLayoutChange { get; set; }

    /// <summary>
    /// Whether a docked keyboard is shown.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder]
    public bool Open { get; set; }

    /// <summary>
    /// What is being typed, which picks the layout. Default: what the input typed into says (its data-bit-vkb, inputmode
    /// and type attributes), else Text.
    /// </summary>
    [Parameter] public BitVirtualKeyboardPurpose? Purpose { get; set; }

    /// <summary>
    /// The milliseconds a repeating key is held before it starts repeating; 0 turns repeating off. Default: 500.
    /// </summary>
    [Parameter] public int RepeatDelay { get; set; } = 500;

    /// <summary>
    /// The milliseconds between the repeats of a held key. Default: 50.
    /// </summary>
    [Parameter] public int RepeatInterval { get; set; } = 50;

    /// <summary>
    /// Keeps the height of an open docked keyboard in the page, so the page can be scrolled above it. Default: true.
    /// </summary>
    [Parameter] public bool ReserveSpace { get; set; } = true;

    /// <summary>
    /// Shuffles the digit keys each time the keyboard opens or attaches to an input (for a PIN).
    /// </summary>
    [Parameter] public bool Scramble { get; set; }

    /// <summary>
    /// Secure mode: no pressed paint, no preview, no sound, no announcement, and no typed text in <see cref="OnKeyPress"/>
    /// (whose KeyId is empty for a key that types a character) or <see cref="OnInput"/>. A bound <see cref="Value"/> still
    /// takes the text typed into it, through ValueChanged. Also on for a password input.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Secure { get; set; }

    /// <summary>
    /// Shows an enlarged copy of a character key while it is pressed. Default: true.
    /// </summary>
    [Parameter] public bool ShowKeyPreview { get; set; } = true;

    /// <summary>
    /// The size of the keys: Small (24px, the smallest WCAG target), Medium (44px) or Large (64px, for kiosks).
    /// </summary>
    [Parameter, ResetClassBuilder] public BitSize? Size { get; set; }

    /// <summary>
    /// Clicks on a key press.
    /// </summary>
    [Parameter] public bool Sound { get; set; }

    /// <summary>
    /// The words of the keyboard: the names of its keys and what it announces.
    /// </summary>
    [Parameter] public BitVirtualKeyboardStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the virtual keyboard.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitVirtualKeyboardClassStyles? Styles { get; set; }

    /// <summary>
    /// The input or textarea the keyboard types into.
    /// </summary>
    /// <remarks>
    /// An @ref is taken as its element renders, so a reference to an input rendered beside the keyboard reaches it on
    /// the page's next render (a page with no other reason to render can call StateHasChanged once after its first one),
    /// or use <see cref="FollowFocus"/>.
    /// </remarks>
    [Parameter] public ElementReference? Target { get; set; }

    /// <summary>
    /// The CSS selector of the inputs <see cref="FollowFocus"/> types into. Default: the inputs carrying a data-bit-vkb
    /// attribute, whose value may name a purpose (data-bit-vkb="pin").
    /// </summary>
    [Parameter] public string? TargetSelector { get; set; }

    /// <summary>
    /// The text the keyboard types into when it has no input to type into.
    /// </summary>
    [Parameter, TwoWayBound]
    public string? Value { get; set; }



    /// <summary>
    /// The id of the active layer, as last reported by the keyboard.
    /// </summary>
    public string? ActiveLayer => _activeLayer ?? _compiled?.Layers[0].Id;

    /// <summary>
    /// The layout the keyboard shows.
    /// </summary>
    public BitVirtualKeyboardLayout? ActiveLayout => _compiled?.Source;



    protected override string RootElementClass => "bit-vkb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Mode is BitVirtualKeyboardMode.Docked ? "bit-vkb-dck" : string.Empty);

        ClassBuilder.Register(() => Mode is BitVirtualKeyboardMode.Docked && Open ? "bit-vkb-opn" : string.Empty);

        ClassBuilder.Register(() => Secure ? "bit-vkb-sec" : string.Empty);

        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-vkb"));
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        if (OpenHasBeenSet is false && DefaultOpen.HasValue)
        {
            await AssignOpen(DefaultOpen.Value);
        }

        if (ValueHasBeenSet is false && DefaultValue is not null)
        {
            await AssignValue(DefaultValue);
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // A scrambled keyboard is shuffled as it first shows (a PIN pad typing into its own Value never opens or attaches),
        // and again every time it opens or attaches to an input.
        if (Scramble is false) _scrambleSeed = null;
        else _scrambleSeed ??= System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue);

        // a shorter list of Languages keeps the language key on one of them, so the layout shown, the next one's indicator
        // and the next one the key moves to are read off the same index
        if (Languages is { Count: > 0 } languages && _languageIndex >= languages.Count) _languageIndex = languages.Count - 1;

        Compile();
        RefreshScriptState();

        base.OnParametersSet();
    }



    private BitVirtualKeyboardStrings ActiveStrings => Strings ?? _defaultStrings;

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    // With no input to type into, the keyboard types into its own Value, and its keys are a grid the keyboard focus moves
    // through; typing into an input, the keys never take the focus away from it.
    private bool IsValueMode => Target is null && FollowFocus is false;

    // A keyboard typing into its own Value acts on every plain Escape pressed in it; one typing into an input claims the
    // key on the input itself, from its script.
    private string? EscapeClaim => IsValueMode ? "claim" : null;

    private BitVirtualKeyboardForm EffectiveForm => Form is BitVirtualKeyboardForm.Auto
        ? (_compact ? BitVirtualKeyboardForm.Compact : BitVirtualKeyboardForm.Full)
        : Form;

    private string? CurrentLanguage => Languages is { Count: > 0 } languages
        ? languages[Math.Clamp(_languageIndex, 0, languages.Count - 1)]
        : Language;

    private void Compile()
    {
        _purpose = ResolvePurpose();

        var layout = ResolveLayout();

        var languagesCount = Languages?.Count ?? 0;
        string? nextIndicator = null;
        if (languagesCount > 1)
        {
            var next = Languages![(_languageIndex + 1) % languagesCount];
            nextIndicator = BitVirtualKeyboardLayouts.Get(next)?.Indicator ?? next.ToUpperInvariant();
        }

        BitVirtualKeyboardCompileOptions options = new()
        {
            Purpose = _purpose,
            Mode = Mode,
            LanguagesCount = languagesCount,
            NextIndicator = nextIndicator,
            Culture = EffectiveCulture,
            AutoShift = AutoShift,
            Digits = Digits,
            NumberTarget = IsNumberTarget,
            ScrambleSeed = Scramble ? _scrambleSeed : null,
        };

        // The compiler caches no scrambled layout, so the one compiled for this seed is kept while nothing it is compiled
        // from changes: a new instance would move the keys' version on and hold the presses back until the script is told.
        if (options.ScrambleSeed.HasValue && _compiled is not null && options == _scrambledOptions
            && (ReferenceEquals(layout, _scrambledLayout) || layout.ToJson() == _scrambledLayoutJson))
        {
            _scrambledLayout = layout;
            return;
        }

        _compiled = BitVirtualKeyboardLayoutCompiler.Compile(layout, options);

        if (options.ScrambleSeed.HasValue)
        {
            _scrambledOptions = options;
            _scrambledLayout = layout;
            _scrambledLayoutJson = layout.ToJson();
        }
        else
        {
            _scrambledOptions = null;
            _scrambledLayout = null;
            _scrambledLayoutJson = null;
        }
    }

    private BitVirtualKeyboardLayout ResolveLayout()
    {
        if (Layout is not null) return Layout;

        var form = EffectiveForm;

        if (LayoutSelector is not null)
        {
            var selected = LayoutSelector(new() { Purpose = _purpose, Culture = EffectiveCulture, Form = form });
            if (selected is not null) return selected;
        }

        if (BitVirtualKeyboardLayouts.IsLanguagePurpose(_purpose) is false) return BitVirtualKeyboardLayouts.ForPurpose(_purpose);

        var suffix = form is BitVirtualKeyboardForm.Compact ? "-compact" : "";

        if (CurrentLanguage is { Length: > 0 } language && BitVirtualKeyboardLayouts.Get(language + suffix) is { } named)
        {
            return named;
        }

        return BitVirtualKeyboardLayouts.ForCulture(EffectiveCulture, form)
               ?? (form is BitVirtualKeyboardForm.Compact ? BitVirtualKeyboardLayouts.EnglishCompact : BitVirtualKeyboardLayouts.English);
    }

    private string GetLayerName(BitVirtualKeyboardCompiledLayer layer) => layer.Name ?? GetLayerName(layer.Id);

    private string GetLayerName(string? layerId)
    {
        if (layerId is null) return "";

        var named = _compiled.GetLayer(layerId)?.Name;
        if (named is not null) return named;

        return layerId switch
        {
            "base" => ActiveStrings.Letters,
            "shift" => ActiveStrings.Shift,
            "caps" => ActiveStrings.CapsLock,
            "altgr" or "shift-altgr" => ActiveStrings.AltGr,
            "symbols" => ActiveStrings.Symbols,
            "symbols2" => ActiveStrings.MoreSymbols,
            _ => layerId,
        };
    }

    // The keycap: the layout's label, else the words of the Strings for a key that takes them, else nothing (an icon).
    private string? GetLabel(BitVirtualKeyboardCompiledKey key)
    {
        if (key.Label is not null) return key.Label;

        return key.BuiltIn switch
        {
            "clear" => ActiveStrings.Clear,
            "accept" => ActiveStrings.Accept,
            "cancel" => ActiveStrings.Cancel,
            "qty" => ActiveStrings.Qty,
            "void" => ActiveStrings.Void,
            "total" => ActiveStrings.Total,
            "home" => ActiveStrings.Home,
            "end" => ActiveStrings.End,
            "layer" => GetLayerName(key.Layer),
            _ => null,
        };
    }

    // The accessible name: the layout's own, else the label, else the words of the Strings; a label that says something
    // other than those words comes first, so the name always holds what the key shows (WCAG 2.5.3).
    private string GetAriaLabel(BitVirtualKeyboardCompiledKey key)
    {
        if (key.AriaLabel is not null) return key.AriaLabel;

        var label = GetLabel(key);
        var words = GetWords(key);

        if (string.IsNullOrEmpty(label)) return words ?? key.KeyId;
        if (words is null || string.Equals(label, words, StringComparison.CurrentCultureIgnoreCase)) return label;

        return $"{label}, {words}";
    }

    // The words of the Strings a key stands for, if it is one of the keys they name.
    private string? GetWords(BitVirtualKeyboardCompiledKey key)
    {
        if (key.Action is BitVirtualKeyboardKeyAction.Character)
        {
            return key.Output switch
            {
                "\U0000200C" => ActiveStrings.Zwnj,
                "\U0000200D" => ActiveStrings.Zwj,
                "\U0000200E" => ActiveStrings.Lrm,
                "\U0000200F" => ActiveStrings.Rlm,
                "\U000000A0" => ActiveStrings.Nbsp,
                _ => null,
            };
        }

        return key.Action switch
        {
            BitVirtualKeyboardKeyAction.Backspace => ActiveStrings.Backspace,
            BitVirtualKeyboardKeyAction.Delete => ActiveStrings.Delete,
            BitVirtualKeyboardKeyAction.Enter => key.BuiltIn == "total" ? ActiveStrings.Total : GetEnterWords(),
            BitVirtualKeyboardKeyAction.Space => ActiveStrings.Space,
            BitVirtualKeyboardKeyAction.Tab => ActiveStrings.Tab,
            BitVirtualKeyboardKeyAction.ShiftTab => ActiveStrings.ShiftTab,
            BitVirtualKeyboardKeyAction.Shift => ActiveStrings.Shift,
            BitVirtualKeyboardKeyAction.CapsLock => ActiveStrings.CapsLock,
            BitVirtualKeyboardKeyAction.AltGr => ActiveStrings.AltGr,
            BitVirtualKeyboardKeyAction.SwitchLayer => GetLayerName(key.Layer),
            BitVirtualKeyboardKeyAction.NextLayout => ActiveStrings.NextLanguage,
            BitVirtualKeyboardKeyAction.Hide => ActiveStrings.Hide,
            BitVirtualKeyboardKeyAction.Clear => ActiveStrings.Clear,
            BitVirtualKeyboardKeyAction.Accept => ActiveStrings.Accept,
            BitVirtualKeyboardKeyAction.Cancel => ActiveStrings.Cancel,
            BitVirtualKeyboardKeyAction.ArrowLeft => ActiveStrings.ArrowLeft,
            BitVirtualKeyboardKeyAction.ArrowRight => ActiveStrings.ArrowRight,
            BitVirtualKeyboardKeyAction.ArrowUp => ActiveStrings.ArrowUp,
            BitVirtualKeyboardKeyAction.ArrowDown => ActiveStrings.ArrowDown,
            BitVirtualKeyboardKeyAction.Home => ActiveStrings.Home,
            BitVirtualKeyboardKeyAction.End => ActiveStrings.End,
            _ => null,
        };
    }

    // The name of Enter follows the enterkeyhint of the input typed into (done, go, next, previous, search, send).
    private string GetEnterWords() => _target?.EnterKeyHint?.ToLowerInvariant() switch
    {
        "done" => ActiveStrings.Done,
        "go" => ActiveStrings.Go,
        "next" => ActiveStrings.Next,
        "previous" => ActiveStrings.Previous,
        "search" => ActiveStrings.Search,
        "send" => ActiveStrings.Send,
        _ => _purpose is BitVirtualKeyboardPurpose.Search ? ActiveStrings.Search : ActiveStrings.Enter,
    };

    private static bool IsModifier(BitVirtualKeyboardCompiledKey key) =>
        key.Action is BitVirtualKeyboardKeyAction.Shift or BitVirtualKeyboardKeyAction.CapsLock or BitVirtualKeyboardKeyAction.AltGr
                   or BitVirtualKeyboardKeyAction.DeadKey;

    private string GetKeyClass(BitVirtualKeyboardCompiledKey key)
    {
        var (kind, part) = key.Kind switch
        {
            BitVirtualKeyboardKeyKind.Function => ("bit-vkb-fnc", Classes?.FunctionKey),
            BitVirtualKeyboardKeyKind.Modifier => ("bit-vkb-mod", Classes?.ModifierKey),
            BitVirtualKeyboardKeyKind.Primary => ("bit-vkb-pri", Classes?.PrimaryKey),
            _ => ("bit-vkb-chr", Classes?.CharacterKey),
        };

        // the left and right arrows point where they move the caret, which a right-to-left layout does not turn around
        var arrow = key.Action is BitVirtualKeyboardKeyAction.ArrowLeft or BitVirtualKeyboardKeyAction.ArrowRight ? "bit-vkb-arw" : null;

        return JoinClasses("bit-vkb-key", kind, arrow, Classes?.Key, part, key.Class);
    }

    private string GetKeyStyle(BitVirtualKeyboardCompiledKey key)
    {
        var part = key.Kind switch
        {
            BitVirtualKeyboardKeyKind.Function => Styles?.FunctionKey,
            BitVirtualKeyboardKeyKind.Modifier => Styles?.ModifierKey,
            BitVirtualKeyboardKeyKind.Primary => Styles?.PrimaryKey,
            _ => Styles?.CharacterKey,
        };

        return JoinStyleList(GetGridArea(key), Styles?.Key, part, key.Style);
    }

    private static string GetGridArea(BitVirtualKeyboardCompiledKey key) =>
        FormattableString.Invariant($"grid-area:{key.Row + 1} / {key.Column + 1} / span {key.RowSpan} / span {key.ColumnSpan}");

    private string GetLayerStyle(BitVirtualKeyboardCompiledLayer layer) =>
        JoinStyleList(FormattableString.Invariant($"grid-template-columns:repeat({_compiled.Columns}, minmax(0, 1fr));grid-template-rows:repeat({layer.Rows}, var(--bit-vkb-kh-r))"), Styles?.Layer);

    // The tab stop of a key: none while typing into an input, whose focus the keys never take; the first key of the
    // first layer while typing into the Value, the script moving the stop as the arrow keys move the focus.
    private string GetKeyTabIndex(BitVirtualKeyboardCompiledLayer layer, BitVirtualKeyboardCompiledKey key)
    {
        if (IsValueMode is false || Disabled) return "-1";

        var first = _compiled.Layers[0] == layer && layer.Keys.FirstOrDefault(k => k.Gap is false && k.Blank is false) == key;
        return first ? "0" : "-1";
    }

    private static string JoinClasses(params string?[] classes) => string.Join(' ', classes.Where(c => string.IsNullOrWhiteSpace(c) is false));

    private static string JoinStyleList(params string?[] styles) =>
        string.Join(';', styles.Where(s => string.IsNullOrWhiteSpace(s) is false).Select(s => s!.Trim().TrimEnd(';')));

    private BitVirtualKeyboardKeyContext GetKeyContext(BitVirtualKeyboardCompiledKey key) => new()
    {
        Key = key.Source,
        Label = GetLabel(key) ?? "",
        Hint = key.Hint,
    };
}