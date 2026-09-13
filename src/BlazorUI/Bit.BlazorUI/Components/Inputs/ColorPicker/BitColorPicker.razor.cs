using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The color picker (ColorPicker) is used to browse through and select colors. It lets people navigate a
/// saturation-brightness area and a hue slider, dial the transparency in with an alpha slider, type the
/// color into hexadecimal or Red-Green-Blue (RGB) text fields, pick it from a row of presets, or sample it
/// from anywhere on the screen with the browser's eyedropper.
/// </summary>
/// <remarks>
/// The value is read and written in whichever CSS notation it arrives in - hexadecimal, <c>rgb()</c>,
/// <c>hsl()</c>, a named color - unless a <see cref="Format"/> pins it to one, and the whole picker is
/// operable from the keyboard.
/// </remarks>
public partial class BitColorPicker : BitComponentBase
{
    private bool _dragged;
    // Whether the picker is part-way through publishing a color it has just moved to, in which case the
    // parameters arriving at it are its own half-applied output rather than anything a consumer chose.
    // A binding callback is not a place the picker gets to finish its work in: a consumer re-renders from
    // inside it - which is what @bind does - so the picker is back in OnParametersSet before the callback it
    // is awaiting has even returned, holding the string it has just written and, for one line longer, a
    // stale record of having written it. Read as a color pushed in from outside, that string is parsed
    // again, and parsing a hexadecimal or rgb() notation re-derives the hue from its eight-bit channels -
    // coarse enough at low saturation to move the hue a degree or two on every frame of a drag, which walks
    // the hue slider out from under the pointer and repaints the whole gradient with it. Nothing the picker
    // publishes is worth re-reading, so while it is publishing, none of it is.
    private bool _publishing;
    private bool _initialized;
    private double _alphaParam = 1;
    private string? _colorParam;
    private string? _abortControllerId;
    private bool _eyeDropperChecked;
    private bool _eyeDropperSupported;
    private BitColorFormat? _formatEmitted;
    private string? _contrastParam;
    private BitInternalColor? _contrastColor;
    private readonly BitInternalColor _color = new();
    private BitColorFormat _format = BitColorFormat.Rgb;
    private ElementReference _saturationPickerRef;
    private ElementReference _hexInputRef;
    private ElementReference _alphaInputRef;
    private readonly ElementReference[] _channelInputRefs = new ElementReference[3];
    private DotNetObjectReference<BitColorPicker>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Indicates the Alpha value, from 0 (fully transparent) to 1 (fully opaque).
    /// </summary>
    /// <remarks>
    /// The alpha is tracked whether or not <see cref="ShowAlphaSlider"/> renders a control for it, and a
    /// color string that carries its own alpha - an <c>rgba()</c>, an <c>hsla()</c> or an eight-digit hex -
    /// overrides this parameter, since it is the more specific answer of the two.
    /// </remarks>
    [Parameter, TwoWayBound] public double Alpha { get; set; } = 1;

    /// <summary>
    /// Whether the saturation-brightness area takes the focus on the first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitColorPicker.
    /// </summary>
    [Parameter] public BitColorPickerClassStyles? Classes { get; set; }

    /// <summary>
    /// CSS-compatible string to describe the color.
    /// </summary>
    /// <remarks>
    /// Hexadecimal in three, four, six or eight digits, <c>rgb()</c> and <c>rgba()</c>, <c>hsl()</c> and
    /// <c>hsla()</c>, <c>hsv()</c>, a CSS color keyword such as "tomato", and <c>transparent</c> are all
    /// understood, in both the comma-separated and the modern space-separated syntax. Unless a
    /// <see cref="Format"/> says otherwise the picker answers in the same notation it was given.
    /// </remarks>
    [Parameter, TwoWayBound] public string Color { get; set; } = "rgb(255,255,255)";

    /// <summary>
    /// The color the contrast readout measures the picked color against - the background it is going to be
    /// read on. It accepts any of the notations <see cref="Color"/> does, and defaults to white.
    /// </summary>
    /// <remarks>
    /// A semi-transparent picked color is composited onto this one before the ratio is taken, since that is
    /// what the eye will actually see. Only rendered where <see cref="ShowContrast"/> asks for it.
    /// </remarks>
    [Parameter] public string? ContrastColor { get; set; }

    /// <summary>
    /// Gets or sets the icon of the eye dropper button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="EyeDropperIconName"/> when both are set.
    /// </summary>
    /// <example>
    /// Bootstrap: EyeDropperIcon="BitIconInfo.Bi("eyedropper")"
    /// FontAwesome: EyeDropperIcon="BitIconInfo.Fa("solid eye-dropper")"
    /// Custom CSS: EyeDropperIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? EyeDropperIcon { get; set; }

    /// <summary>
    /// Custom icon name for the eye dropper button. If unset, default will be the Eyedropper icon.
    /// The icon name should be from the Fluent UI icon set. For external icon libraries, use <see cref="EyeDropperIcon"/> instead.
    /// </summary>
    [Parameter] public string? EyeDropperIconName { get; set; }

    /// <summary>
    /// The CSS notation the color value is written in. When left unset the picker answers in the same
    /// notation the <see cref="Color"/> arrived in, so a hexadecimal binding stays hexadecimal and an
    /// <c>rgb()</c> one stays <c>rgb()</c>.
    /// </summary>
    [Parameter] public BitColorFormat? Format { get; set; }

    /// <summary>
    /// Which channels the text fields are written in: the hexadecimal field with the Red-Green-Blue
    /// channels beside it, either of those on its own, or the hue-saturation-lightness and
    /// hue-saturation-brightness triplets.
    /// </summary>
    /// <remarks>
    /// This is how the color is typed, not how it is published - a picker edited in HSL still answers in
    /// whatever <see cref="Format"/> says. It only takes effect where <see cref="ShowInputs"/> renders the
    /// fields at all, and <see cref="ShowInputsModeSwitch"/> lets the user move it themselves.
    /// </remarks>
    [Parameter, TwoWayBound] public BitColorInputsMode InputsMode { get; set; }

    /// <summary>
    /// Gets or sets the icon of the inputs mode switch button using custom CSS classes for external icon
    /// libraries. Takes precedence over <see cref="InputsModeSwitchIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? InputsModeSwitchIcon { get; set; }

    /// <summary>
    /// Custom icon name for the inputs mode switch button. If unset, default will be the Sort icon.
    /// The icon name should be from the Fluent UI icon set. For external icon libraries, use
    /// <see cref="InputsModeSwitchIcon"/> instead.
    /// </summary>
    [Parameter] public string? InputsModeSwitchIconName { get; set; }

    /// <summary>
    /// The text that names the picker. It is not a <c>label</c> element: with no single input to point a
    /// <c>for</c> at, one would label nothing, so the panel is named through <c>aria-labelledby</c> instead.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom markup in place of the plain <see cref="Label"/> text, for when the name needs more than a
    /// string - an icon beside it, a required marker, a link.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Callback for when the value changed. It fires on every step of a drag.
    /// </summary>
    [Parameter] public EventCallback<BitColorChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// Callback for when the user finishes changing the value: the drag ends, the slider is released, a
    /// text field is committed, or a preset is picked.
    /// </summary>
    /// <remarks>
    /// This is the callback to hang expensive work off - saving, recoloring a document, a network call -
    /// since <see cref="OnChange"/> fires for every frame of a drag while this one fires once at the end of it.
    /// </remarks>
    [Parameter] public EventCallback<BitColorChangeEventArgs> OnChangeEnd { get; set; }

    /// <summary>
    /// The colors offered as a row of one-click swatches under the picker, in any of the notations the
    /// <see cref="Color"/> parameter accepts.
    /// </summary>
    /// <remarks>
    /// A swatch that carries its own alpha - an <c>rgba()</c> or an eight-digit hex - applies that alpha too,
    /// so a palette can offer the same hue at several transparencies.
    /// </remarks>
    [Parameter] public IEnumerable<string>? Presets { get; set; }

    /// <summary>
    /// How many preset swatches are laid out per row. Left unset they simply wrap, filling the width of
    /// the picker; setting it lays them out on a grid instead, which is what keeps a palette meant to be
    /// read in columns - a hue per column, a shade per row - in the arrangement it was written in.
    /// </summary>
    [Parameter] public int? PresetsPerRow { get; set; }

    /// <summary>
    /// Makes the color picker read-only: the value is still shown at full contrast, but nothing about it
    /// can be changed.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether to show a slider for editing alpha value.
    /// </summary>
    [Parameter] public bool ShowAlphaSlider { get; set; }

    /// <summary>
    /// Whether to show the contrast readout: how far the picked color stands from the
    /// <see cref="ContrastColor"/> it will be read on, and whether that clears the WCAG bar for text.
    /// </summary>
    /// <remarks>
    /// It answers the question a color is usually being picked to settle - "can this be read?" - at the
    /// moment it is being picked, rather than after the page has shipped and an audit has failed.
    /// </remarks>
    [Parameter] public bool ShowContrast { get; set; }

    /// <summary>
    /// Whether to show the button that opens the browser's eyedropper to sample a color from anywhere on
    /// the screen.
    /// </summary>
    /// <remarks>
    /// The eyedropper is a Chromium-only browser feature. The button is only rendered where the browser
    /// actually provides one, so the picker never offers a control that cannot work.
    /// </remarks>
    [Parameter] public bool ShowEyeDropper { get; set; }

    /// <summary>
    /// Whether to show the hexadecimal and Red-Green-Blue text fields, which is how an exact color is
    /// entered or read off without hunting for it on the gradient.
    /// </summary>
    [Parameter] public bool ShowInputs { get; set; }

    /// <summary>
    /// Whether to show the button that moves the text fields from one set of channels to the next -
    /// hexadecimal with RGB, hexadecimal alone, RGB alone, HSL, HSV - so the user can type the color in
    /// whichever model they are thinking in rather than the one the page picked for them.
    /// </summary>
    [Parameter] public bool ShowInputsModeSwitch { get; set; }

    /// <summary>
    /// Whether to show color preview box.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; }

    /// <summary>
    /// The size of the color picker.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitColorPicker.
    /// </summary>
    [Parameter] public BitColorPickerClassStyles? Styles { get; set; }



    /// <summary>
    /// The current color in six-digit hexadecimal notation, e.g. <c>#FF0000</c>.
    /// </summary>
    public string Hex => _color.Hex;

    /// <summary>
    /// The current color in eight-digit hexadecimal notation, whose last pair is the alpha channel,
    /// e.g. <c>#FF000080</c>.
    /// </summary>
    public string HexAlpha => _color.HexAlpha;

    /// <summary>
    /// The current color in functional RGB notation, e.g. <c>rgb(255,0,0)</c>.
    /// </summary>
    public string Rgb => _color.Rgb;

    /// <summary>
    /// The current color in functional RGB notation with its alpha channel, e.g. <c>rgba(255,0,0,0.5)</c>.
    /// </summary>
    public string Rgba => _color.Rgba;

    /// <summary>
    /// The current color as hue (0-360), saturation and lightness (both 0-1).
    /// </summary>
    public (double Hue, double Saturation, double Lightness) Hsl => _color.Hsl;

    /// <summary>
    /// The current color as hue (0-360), saturation and value (both 0-1).
    /// </summary>
    public (double Hue, double Saturation, double Value) Hsv => _color.Hsv;

    /// <summary>
    /// The current color as hue (0-360), whiteness and blackness (both 0-1).
    /// </summary>
    public (double Hue, double Whiteness, double Blackness) Hwb => _color.Hwb;

    /// <summary>
    /// The current color as Oklab lightness (0-1), chroma (0 to about 0.4) and hue (0-360).
    /// </summary>
    public (double Lightness, double Chroma, double Hue) Oklch => _color.Oklch;

    /// <summary>
    /// The current color said in words, e.g. <c>light vibrant blue</c>. It is what the picker announces to
    /// a screen reader, and it is public because a page showing the color elsewhere usually wants to name
    /// it the same way.
    /// </summary>
    public string ColorDescription => _color.ColorDescription;



    [JSInvokable(nameof(HandlePointerMove))]
    public Task HandlePointerMove(double x, double y) => UpdateColorAsync(x, y);

    [JSInvokable(nameof(HandlePointerUp))]
    public async Task HandlePointerUp()
    {
        if (_dragged is false) return;

        _dragged = false;

        await ChangeAsync(final: true, notifyChange: false);
    }



    protected override string RootElementClass => "bit-clp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ReadOnly ? "bit-clp-rdl" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-clp-sm",
            BitSize.Medium => "bit-clp-md",
            BitSize.Large => "bit-clp-lg",
            _ => "bit-clp-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        var first = _initialized is false;

        // A parameter is only re-applied when it actually differs from the value this instance last
        // consumed - which is also the value it last produced - so that the strings the picker writes back
        // out do not get re-parsed as if the consumer had pushed a new color in. While the picker is
        // half-way through publishing one, that comparison cannot be made at all: see _publishing.
        var alphaChanged = _publishing is false && AlphaHasBeenSet && (first || Alpha != _alphaParam);
        var colorChanged = _publishing is false && ColorHasBeenSet && (first || Color != _colorParam);

        if (alphaChanged)
        {
            _alphaParam = Alpha;
        }

        if (colorChanged)
        {
            _colorParam = Color;

            // An unrecognized string leaves the picker on white rather than throwing, the same white it
            // starts on, so a half-typed color bound from a text field cannot break the page.
            _color.Parse(Color, (alphaChanged || first) ? Alpha : _color.A);
        }
        else if (alphaChanged)
        {
            _color.A = Alpha;
        }

        _format = Format ?? BitInternalColor.DetectFormat(_colorParam) ?? BitColorFormat.Rgb;

        // With nothing bound to them, the Color and Alpha parameters still report what the picker is on, so
        // that a @ref'd or uncontrolled picker can be read without a binding - and so that the color is
        // written in the requested Format from the very first render.
        if (ColorHasBeenSet is false)
        {
            var formatted = _color.ToString(_format);

            if (Color != formatted)
            {
                Color = formatted;
                _colorParam = formatted;
            }
        }

        // A color string can carry its own alpha, which then also becomes the answer this parameter gives.
        if (AlphaHasBeenSet is false && Alpha != _color.A)
        {
            Alpha = _color.A;
            _alphaParam = Alpha;
        }

        _initialized = true;

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A Format switched at runtime rewrites the value that is already bound, rather than leaving it in
        // the old notation until the user happens to touch the picker again.
        if (_formatEmitted != _format)
        {
            var emitted = _formatEmitted;

            _formatEmitted = _format;

            if (emitted is not null)
            {
                await ChangeAsync(notifyChange: false);
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        var presetsToRegister = _presetItems.Count > 0 && _presetKeysRegistered is false;

        if (_presetItems.Count == 0)
        {
            // The container the registration lives on is gone with the palette, so the next one has to ask
            // for it again.
            _presetKeysRegistered = false;
        }

        if (firstRender is false && presetsToRegister is false && (_eyeDropperChecked || ShowEyeDropper is false)) return;

        try
        {
            if (firstRender)
            {
                _dotnetObj = DotNetObjectReference.Create(this);

                _abortControllerId = await _js.BitColorPickerSetup(_dotnetObj, _saturationPickerRef, nameof(HandlePointerMove), nameof(HandlePointerUp));

                // The autofocus attribute is only honored for elements that are in the initial document, so
                // a picker rendered into a page that is already up has to ask for the focus itself.
                if (AutoFocus && IsEnabled)
                {
                    await _saturationPickerRef.FocusAsync();
                }
            }

            if (presetsToRegister)
            {
                _presetKeysRegistered = true;

                await _js.BitUtilsRegisterPreventKeys(_presetsRef, _presetNavigationKeys);
            }

            if (ShowEyeDropper && _eyeDropperChecked is false)
            {
                // Asked once, on the first render that asks for the eyedropper, because the answer is a browser
                // capability rather than a parameter: it cannot be known while prerendering, and it cannot change
                // afterwards. A picker that only turns the button on later still gets its answer then.
                _eyeDropperChecked = true;

                _eyeDropperSupported = await _js.BitColorPickerIsEyeDropperSupported();

                if (_eyeDropperSupported)
                {
                    StateHasChanged();
                }
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    /// <summary>
    /// Whether a gesture is allowed to move the color. A one-way bound Color has nowhere to report a change
    /// to, so the picker becomes a display of that value instead of pretending to accept edits.
    /// </summary>
    private bool _IsInteractive => IsEnabled
                                && ReadOnly is false
                                && (ColorHasBeenSet is false || ColorChanged.HasDelegate || OnChange.HasDelegate || OnChangeEnd.HasDelegate);

    /// <summary>
    /// Only the disabled picker leaves the tab order. A read-only one is still showing a color, and the
    /// saturation area is the only element that announces it, so taking it out of the tab order would put
    /// that color out of reach of a keyboard or a screen reader. The gestures are refused in the handlers
    /// instead.
    /// </summary>
    private string _TabIndex => IsEnabled ? (TabIndex ?? "0") : "-1";

    /// <summary>
    /// The accessible name of the picker as a whole. An explicit AriaLabel wins; otherwise the color itself
    /// is spelled out, since a gradient carries no text a screen reader could read instead.
    /// </summary>
    /// <remarks>
    /// The color is both named and spelled out in channels. The name is what actually tells someone which
    /// color the picker has landed on - "Red 77 Green 127 Blue 179" is three numbers nobody can picture -
    /// while the channels are what they need once they are adjusting it rather than reading it.
    /// </remarks>
    private string _AriaLabel
    {
        get
        {
            if (AriaLabel.HasValue()) return AriaLabel!;

            var label = FormattableString.Invariant($"Color picker, {_color.ColorDescription}, Red {_color.R} Green {_color.G} Blue {_color.B}");

            if (ShowAlphaSlider)
            {
                label += FormattableString.Invariant($" and Alpha {Math.Round(_color.A * 100)}%");
            }

            return label + " selected.";
        }
    }

    /// <summary>
    /// The saturation-brightness area is a two-dimensional slider, which no ARIA role describes on its own.
    /// It is exposed as a slider whose value is the saturation, with the brightness - and the resulting color -
    /// carried in the value text, and named as a 2D slider so that assistive technologies announce both axes.
    /// </summary>
    private string _SaturationValueText
    {
        get
        {
            var (_, saturation, value) = _color.Hsv;

            return FormattableString.Invariant($"{_color.ColorDescription}, Saturation {Math.Round(saturation * 100)}%, Brightness {Math.Round(value * 100)}%, {_color.Hex}");
        }
    }

    private string _SaturationValueNow => Math.Round(_color.Hsv.Saturation * 100).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The fully saturated, fully bright form of the current hue, which is the background the white and
    /// black gradients of the area are laid over.
    /// </summary>
    private string _SaturationPickerStyle
    {
        get
        {
            var (r, g, b) = BitInternalColor.ToRgb(_color.Hsv.Hue, 1, 1);

            return FormattableString.Invariant($"background-color:rgb({r},{g},{b});{Styles?.SaturationPicker}");
        }
    }

    /// <summary>
    /// The position of the thumb, as a percentage of the area rather than a pixel offset, so that it stays
    /// on its color when the picker is resized without anything having to measure the element.
    /// The inline-start edge is the unsaturated one, which puts it on the right in a right-to-left picker.
    /// </summary>
    private string _SaturationThumbStyle
    {
        get
        {
            var (_, saturation, value) = _color.Hsv;

            return FormattableString.Invariant($"inset-inline-start:{saturation * 100}%;top:{(1 - value) * 100}%;background-color:{_color.Rgb};{Styles?.SaturationThumb}");
        }
    }

    private string _HueValue => _color.Hsv.Hue.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// What the hue slider announces. A bare number would be read as a position on an unnamed scale, so the
    /// unit the scale is actually in - degrees around the color wheel - is spelled out with it.
    /// </summary>
    private string _HueValueText => FormattableString.Invariant($"Hue {Math.Round(_color.Hsv.Hue)} degrees");

    private string _AlphaValue => _color.A.ToString(CultureInfo.InvariantCulture);

    private string _AlphaPercentValue => Math.Round(_color.A * 100).ToString(CultureInfo.InvariantCulture);

    private bool _ShowEyeDropper => ShowEyeDropper && _eyeDropperSupported;

    /// <summary>
    /// Whether every control on the picker refuses the gesture at the element rather than in its handler.
    /// A native range moves its own thumb, and a text field keeps whatever was typed into it, before any
    /// handler is reached - so a picker that is not going to accept the change has to stop it there, or be
    /// left showing a value it is not on. The disabled and the read-only picker are covered by the state
    /// they are in; the one-way bound one is not, since it looks like an ordinary picker and only has
    /// nowhere to report a change to.
    /// </summary>
    private bool _IsInputDisabled => _IsInteractive is false;

    private string _HexValue => ShowAlphaSlider ? _color.HexAlpha : _color.Hex;

    /// <summary>
    /// The three channel fields of the inputs row, each carrying the index the change handler switches on,
    /// the id and caption it is labelled with, its current value and the top of its range. Which three they
    /// are is what <see cref="InputsMode"/> decides; the hexadecimal field is rendered on its own since it
    /// is a text field rather than a number one.
    /// </summary>
    private (int Index, string Name, string Title, string Value, int Max)[] _ChannelFields
    {
        get
        {
            switch (InputsMode)
            {
                case BitColorInputsMode.Hex:
                    return [];

                case BitColorInputsMode.Hsl:
                    {
                        var (hue, saturation, lightness) = _color.Hsl;

                        return
                        [
                            (0, "h", "Hue", Rounded(hue), 360),
                            (1, "s", "Saturation", Percent(saturation), 100),
                            (2, "l", "Lightness", Percent(lightness), 100)
                        ];
                    }

                case BitColorInputsMode.Hsv:
                    {
                        var (hue, saturation, value) = _color.Hsv;

                        return
                        [
                            (0, "h", "Hue", Rounded(hue), 360),
                            (1, "s", "Saturation", Percent(saturation), 100),
                            (2, "v", "Brightness", Percent(value), 100)
                        ];
                    }

                default:
                    return
                    [
                        (0, "r", "Red", _color.R.ToString(CultureInfo.InvariantCulture), 255),
                        (1, "g", "Green", _color.G.ToString(CultureInfo.InvariantCulture), 255),
                        (2, "b", "Blue", _color.B.ToString(CultureInfo.InvariantCulture), 255)
                    ];
            }
        }
    }

    private bool _ShowHexField => InputsMode is BitColorInputsMode.HexRgb or BitColorInputsMode.Hex;

    private bool _HasLabel => LabelTemplate is not null || Label.HasValue();

    private string _LabelId => $"{_Id}-label";

    /// <summary>
    /// An explicit AriaLabel still wins, and a Label names the panel through the element it is rendered
    /// into. Only a picker with neither falls back to spelling the color out on the group itself.
    /// </summary>
    private string? _AriaLabelledBy => AriaLabel.HasValue() is false && _HasLabel ? _LabelId : null;

    /// <summary>
    /// How far the picked color stands from the one it will be read on, on the WCAG 2 scale of 1 to 21.
    /// </summary>
    /// <remarks>
    /// A semi-transparent color is composited onto the background first, since a ratio taken against the
    /// color the user cannot actually see would be a reassuring answer to the wrong question.
    /// </remarks>
    private double _Contrast
    {
        get
        {
            // Parsed only when the background itself changes, since the readout is recomputed on every
            // frame of a drag and re-reading the same string on each of them would be work with nothing to
            // show for it.
            if (_contrastParam != ContrastColor || _contrastColor is null)
            {
                _contrastParam = ContrastColor;
                _contrastColor = new BitInternalColor(ContrastColor.HasValue() ? ContrastColor : "#FFFFFF");
            }

            var background = _contrastColor;

            var alpha = _color.A;

            var composited = new BitInternalColor((byte)Math.Round(_color.R * alpha + background.R * (1 - alpha)),
                                                  (byte)Math.Round(_color.G * alpha + background.G * (1 - alpha)),
                                                  (byte)Math.Round(_color.B * alpha + background.B * (1 - alpha)));

            return BitThemeColorContrast.GetContrastRatio(composited.Hex, background.Hex);
        }
    }

    // Two decimals, and none where there is nothing to say: "4.54:1" is the precision the WCAG thresholds
    // are stated to, and "21:1" reads better than "21.00:1".
    private static string FormatContrast(double ratio) => ratio.ToString("0.##", CultureInfo.InvariantCulture);

    // The fields hold whole numbers: a hue to the degree and a percentage to the point are already finer
    // than the gradient they stand next to can be dragged.
    private static string Rounded(double value) => Math.Round(value).ToString(CultureInfo.InvariantCulture);

    private static string Percent(double value) => Rounded(value * 100);

    private List<(string Value, string Css, BitInternalColor Color)> _presetItems = [];
    private ElementReference[] _presetRefs = [];
    private ElementReference _presetsRef;
    private bool _presetKeysRegistered;
    private int _presetFocusIndex;

    /// <summary>
    /// The keys the palette is walked with. They all scroll the page by default, which would carry the
    /// palette out from under the user while they are moving through it.
    /// </summary>
    private static readonly string[] _presetNavigationKeys = ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End"];

    /// <summary>
    /// The presets, each paired with the normalized color it stands for. Normalizing them is what lets a
    /// swatch be compared against the current color, and it also keeps the raw strings out of the style
    /// attribute they would otherwise be written into.
    /// </summary>
    /// <remarks>
    /// Rebuilt only when the palette itself changes, since a drag re-renders the picker on every frame and
    /// parsing the whole palette on each of them would be work with nothing to show for it. The comparison
    /// is by content rather than by reference, so a palette rebuilt into a new list of the same colors -
    /// which an inline collection in the markup does on every render - is recognized as the same one.
    /// </remarks>
    private List<(string Value, string Css, BitInternalColor Color)> _Presets
    {
        get
        {
            if (Presets is null)
            {
                if (_presetItems.Count > 0)
                {
                    _presetItems = [];
                }

                return _presetItems;
            }

            if (Presets.SequenceEqual(_presetItems.Select(p => p.Value)) is false)
            {
                _presetItems = Presets.Select(p =>
                {
                    var color = new BitInternalColor(p);

                    return (p, color.Rgba, color);
                }).ToList();

                _presetRefs = new ElementReference[_presetItems.Count];
                _presetFocusIndex = 0;
            }

            return _presetItems;
        }
    }

    /// <summary>
    /// Whether a swatch is the one the picker is currently on.
    /// </summary>
    /// <remarks>
    /// A swatch that carries no alpha of its own is picked onto whatever alpha the picker is already on, so
    /// it is the swatch the picker is on whenever the RGB matches - comparing its opaque alpha too would
    /// leave a semi-transparent picker showing nothing as selected. One that does carry an alpha names a
    /// transparency of its own, and is only on when that matches.
    /// </remarks>
    private bool IsPresetSelected(BitInternalColor preset)
    {
        return preset.R == _color.R
            && preset.G == _color.G
            && preset.B == _color.B
            && (preset.HasParsedAlpha is false || preset.A == _color.A);
    }

    /// <summary>
    /// The one swatch of the palette that is in the tab order. A palette is a set of alternatives, not a
    /// queue of controls, so it takes one Tab to reach and the arrow keys to move within - otherwise a
    /// thirty-color palette would put thirty tab stops between the picker and whatever comes after it.
    /// </summary>
    private int _PresetTabStop
    {
        get
        {
            // Entering the palette lands on the color the picker is already on wherever there is one, which
            // is both the swatch the user is most likely looking for and the one they can see is current.
            var selected = _presetItems.FindIndex(p => IsPresetSelected(p.Color));

            return selected >= 0 ? selected : Math.Clamp(_presetFocusIndex, 0, Math.Max(_presetItems.Count - 1, 0));
        }
    }



    private async Task UpdateColorAsync(double x, double y)
    {
        if (_IsInteractive is false) return;

        // The area is measured from its inline-start edge, which is the right one in a right-to-left picker,
        // so the horizontal axis is mirrored along with the gradient that is drawn on it.
        var saturation = Dir == BitDir.Rtl ? 1 - x : x;

        var before = _color.Hsv;

        _color.Update(before.Hue, saturation, 1 - y, _color.A);

        // A pointer that has not left the color it is already on - a press that lands on the thumb, or a
        // move along an edge the position is clamped against - has nothing to report.
        if (_color.Hsv == before) return;

        _dragged = true;

        await ChangeAsync();
    }

    private async Task HandleOnSaturationKeyDown(KeyboardEventArgs e)
    {
        if (_IsInteractive is false) return;

        var (hue, saturation, value) = _color.Hsv;

        // Holding Shift - and the Page keys, which need no modifier for it - moves a tenth of the area at a
        // time, so crossing it costs ten presses instead of a hundred.
        var step = (e.ShiftKey || e.Key is "PageUp" or "PageDown") ? 0.1 : 0.01;
        var rtl = Dir == BitDir.Rtl;

        var newSaturation = e.Key switch
        {
            "ArrowRight" => saturation + (rtl ? -step : step),
            "ArrowLeft" => saturation + (rtl ? step : -step),
            "Home" => 0d,
            "End" => 1d,
            _ => saturation
        };

        var newValue = e.Key switch
        {
            "ArrowUp" or "PageUp" => value + step,
            "ArrowDown" or "PageDown" => value - step,
            _ => value
        };

        // Compared after clamping, so a key pressed against the edge the color already sits on reports
        // no change at all rather than one that turns out to move nothing.
        newSaturation = Math.Clamp(newSaturation, 0, 1);
        newValue = Math.Clamp(newValue, 0, 1);

        if (newSaturation == saturation && newValue == value) return;

        _color.Update(hue, newSaturation, newValue, _color.A);

        // A keypress is a whole gesture on its own, so it commits rather than merely previewing.
        await ChangeAsync(final: true);
    }

    // A native range fires input while it is dragged and change once it is released, which is exactly the
    // OnChange / OnChangeEnd pair. The last input and the change carry the same value though, so the second
    // of them only ends the gesture - it does not report a change that has already been reported.
    private async Task HandleOnHueInput(ChangeEventArgs args, bool final)
    {
        if (_IsInteractive is false) return;

        if (TryReadNumber(args.Value, out var hue) is false) return;

        var before = _color.Hsv;

        _color.Update(hue, before.Saturation, before.Value, _color.A);

        await ChangeAsync(final, notifyChange: _color.Hsv != before);
    }

    private async Task HandleOnAlphaInput(ChangeEventArgs args, bool final)
    {
        if (_IsInteractive is false) return;

        if (TryReadNumber(args.Value, out var alpha) is false) return;

        var before = _color.A;

        _color.A = alpha;

        await ChangeAsync(final, notifyChange: _color.A != before);
    }

    private async Task HandleOnHexInput(ChangeEventArgs args)
    {
        if (_IsInteractive is false) return;

        var text = args.Value as string;

        // A field left in a state that is not a color simply does not commit, and the current color is put
        // back into it - which is less surprising than resetting the picker to white.
        if (BitInternalColor.IsValid(text) is false)
        {
            await RestoreInputAsync(_hexInputRef, text, _HexValue);
            return;
        }

        // Parsed onto the current alpha, so typing a six-digit hex into a semi-transparent color does not
        // silently make it opaque; an eight-digit one still brings its own alpha with it.
        _color.Parse(text, _color.A);

        await ChangeAsync(final: true);

        await RestoreInputAsync(_hexInputRef, text, _HexValue);
    }

    private async Task HandleOnChannelInput(ChangeEventArgs args, int channel)
    {
        if (_IsInteractive is false) return;

        var typed = args.Value as string;

        if (TryReadNumber(args.Value, out var number) is false)
        {
            await RestoreInputAsync(_channelInputRefs[channel], typed, _ChannelFields[channel].Value);
            return;
        }

        switch (InputsMode)
        {
            case BitColorInputsMode.Hsl:
                ApplyHslChannel(channel, number);
                break;

            case BitColorInputsMode.Hsv:
                ApplyHsvChannel(channel, number);
                break;

            default:
                ApplyRgbChannel(channel, number);
                break;
        }

        await ChangeAsync(final: true);

        await RestoreInputAsync(_channelInputRefs[channel], typed, _ChannelFields[channel].Value);
    }

    private void ApplyRgbChannel(int channel, double number)
    {
        var value = (byte)Math.Clamp(Math.Round(number), 0, 255);

        _color.SetRgb(channel == 0 ? value : _color.R,
                      channel == 1 ? value : _color.G,
                      channel == 2 ? value : _color.B);
    }

    /// <summary>
    /// Only the edited channel is taken from the field; the other two are taken from the color itself
    /// rather than from the rounded numbers standing in their fields, so editing one of them does not
    /// quietly nudge the other two onto whole percentages.
    /// </summary>
    private void ApplyHslChannel(int channel, double number)
    {
        var (hue, saturation, lightness) = _color.Hsl;

        if (channel == 0) hue = number;
        else if (channel == 1) saturation = Math.Clamp(number / 100, 0, 1);
        else lightness = Math.Clamp(number / 100, 0, 1);

        // HSL and HSV share a hue but not a saturation, so the pair is converted rather than copied.
        var value = lightness + saturation * Math.Min(lightness, 1 - lightness);

        _color.Update(hue, value == 0 ? 0 : 2 * (1 - lightness / value), value, _color.A);
    }

    private void ApplyHsvChannel(int channel, double number)
    {
        var (hue, saturation, value) = _color.Hsv;

        if (channel == 0) hue = number;
        else if (channel == 1) saturation = number / 100;
        else value = number / 100;

        _color.Update(hue, saturation, value, _color.A);
    }

    private async Task HandleOnAlphaFieldInput(ChangeEventArgs args)
    {
        if (_IsInteractive is false) return;

        var typed = args.Value as string;

        if (TryReadNumber(args.Value, out var percent) is false)
        {
            await RestoreInputAsync(_alphaInputRef, typed, _AlphaPercentValue);
            return;
        }

        _color.A = percent / 100;

        await ChangeAsync(final: true);

        await RestoreInputAsync(_alphaInputRef, typed, _AlphaPercentValue);
    }

    /// <summary>
    /// Moves the text fields on to the next set of channels. It changes nothing about the color, only which
    /// numbers it is being read as, so a read-only picker still answers it - there is nothing to refuse.
    /// </summary>
    private async Task HandleOnInputsModeSwitchClick()
    {
        if (IsEnabled is false) return;

        await AssignInputsMode(InputsMode switch
        {
            BitColorInputsMode.HexRgb => BitColorInputsMode.Hex,
            BitColorInputsMode.Hex => BitColorInputsMode.Rgb,
            BitColorInputsMode.Rgb => BitColorInputsMode.Hsl,
            BitColorInputsMode.Hsl => BitColorInputsMode.Hsv,
            _ => BitColorInputsMode.HexRgb
        });

        StateHasChanged();
    }

    /// <summary>
    /// Puts the committed color back into a text field when what the user left in it is not what the picker
    /// ended up on - a value it refused, or one it normalized ("#0f0" into "#00FF00").
    /// </summary>
    /// <remarks>
    /// Re-rendering alone would not do it: the renderer compares the new value against the last one it
    /// rendered, not against what the user typed, so a value that has not moved leaves their text sitting in
    /// the field. The element is written to directly instead, which - unlike replacing it - leaves the field
    /// focused, so correcting a typo does not also cost the user their place in the panel.
    /// </remarks>
    private async Task RestoreInputAsync(ElementReference element, string? typed, string committed)
    {
        if (string.Equals(typed, committed, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            await _js.BitUtilsSetProperty(element, "value", committed);
        }
        // The field is only ever written to from a handler the user has just raised on it, so the ways this
        // can fail are the ways the page itself can go away underneath one.
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Walks the palette with the arrow keys. Only the focus moves - a swatch is picked by pressing it, the
    /// same way it is picked with the pointer - so arrowing across a palette does not fire a change for every
    /// color it passes over.
    /// </summary>
    private async Task HandleOnPresetsKeyDown(KeyboardEventArgs e, int index)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey) return;

        var count = _presetItems.Count;

        if (count == 0) return;

        // A palette laid out on a grid is walked as a grid; one left to wrap has no row length anything can
        // know, so the vertical keys move along it one swatch at a time like the horizontal ones do.
        var columns = PresetsPerRow > 0 ? Math.Min(PresetsPerRow.Value, count) : 1;
        var step = Dir == BitDir.Rtl ? -1 : 1;

        var target = e.Key switch
        {
            "ArrowRight" => index + step,
            "ArrowLeft" => index - step,
            "ArrowDown" => index + columns,
            "ArrowUp" => index - columns,
            "Home" => 0,
            "End" => count - 1,
            _ => index
        };

        if (target == index) return;

        // Clamped rather than wrapped: a palette is read as a picture of the colors on offer, and a focus
        // that jumps from the end of one row to the start of the next reads as having lost its place.
        target = Math.Clamp(target, 0, count - 1);

        _presetFocusIndex = target;

        StateHasChanged();

        try
        {
            await _presetRefs[target].FocusAsync();
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }

    private async Task HandleOnPresetClick(string preset)
    {
        if (_IsInteractive is false) return;

        // A preset that carries no alpha of its own keeps the alpha the picker is already on, so picking a
        // hue out of the palette does not undo the transparency that was dialled in.
        _color.Parse(preset, _color.A);

        await ChangeAsync(final: true);
    }

    private async Task HandleOnEyeDropperClick()
    {
        if (_IsInteractive is false) return;

        string? picked;

        try
        {
            picked = await _js.BitColorPickerOpenEyeDropper();
        }
        // The eyedropper is open for as long as the user takes to aim it, which is long enough for the page
        // to go away underneath it: the circuit drops, the interop is disposed, or the call is cancelled with
        // it. Whichever way it ends, there is no color to apply and nothing left to apply it to.
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (OperationCanceledException) // TaskCanceledException among them
        {
            return;
        }

        // Null means the user dismissed the eyedropper, which leaves the color where it was.
        if (picked.HasNoValue()) return;

        _color.Parse(picked, _color.A);

        await ChangeAsync(final: true);
    }

    /// <summary>
    /// Publishes the color the picker has just moved to: the bound parameters first, then the callbacks.
    /// </summary>
    /// <param name="final">Whether this is the end of a gesture rather than a step of one.</param>
    /// <param name="notifyChange">
    /// Whether OnChange should fire. The end of a drag has nothing new to report - the last move already
    /// reported it - so it only raises OnChangeEnd.
    /// </param>
    private async Task ChangeAsync(bool final = false, bool notifyChange = true)
    {
        var value = _color.ToString(_format);
        var alpha = _color.A;

        var colorTaken = false;
        var alphaTaken = false;

        _publishing = true;

        try
        {
            // Read back rather than assumed: a consumer is free to answer with a different color - clamping
            // it to a palette, rounding it, refusing it - and it is that answer the next render brings back.
            colorTaken = await AssignColor(value);
            if (colorTaken)
            {
                _colorParam = Color;
            }

            alphaTaken = await AssignAlpha(alpha);
            if (alphaTaken)
            {
                _alphaParam = Alpha;
            }
        }
        finally
        {
            _publishing = false;
        }

        // An answer that differs from what was published is a color pushed in by the consumer like any
        // other, and the picker follows it. It could not be followed as it arrived, since it arrived while
        // the publish was still half-applied, so it is followed here instead - once, off the values that
        // came back, rather than off every render that carried them.
        if (colorTaken && Color != value)
        {
            _color.Parse(Color, alphaTaken ? Alpha : _color.A);

            value = Color;
        }
        else if (alphaTaken && Alpha != alpha)
        {
            _color.A = Alpha;
        }

        if (notifyChange || final)
        {
            var args = new BitColorChangeEventArgs
            {
                Color = value,
                Alpha = _color.A,
                Hex = _color.Hex,
                HexAlpha = _color.HexAlpha,
                Rgb = _color.Rgb,
                Rgba = _color.Rgba,
                Hsl = _color.Hsl,
                Hsv = _color.Hsv,
                Hwb = _color.Hwb,
                Oklch = _color.Oklch,
                ColorDescription = _color.ColorDescription
            };

            if (notifyChange)
            {
                await OnChange.InvokeAsync(args);
            }

            if (final)
            {
                await OnChangeEnd.InvokeAsync(args);
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// Reads a number out of a DOM event. The browser always writes them in the invariant format, whatever
    /// the culture of the application, so that is how they are read back.
    /// </summary>
    private static bool TryReadNumber(object? value, out double number)
    {
        return double.TryParse(value as string, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            // The JavaScript side owns the listeners that hold the .NET reference, so it is told to drop them
            // first. Whether that call succeeds, fails, or cannot be made at all - a torn-down circuit, or a
            // setup that never returned an id to release - the reference itself is released here, so it is
            // never left registered.
            try
            {
                if (_abortControllerId.HasValue())
                {
                    await _js.BitColorPickerDispose(_abortControllerId);
                }
            }
            catch (JSException) { } // whatever the interop answered, the reference below is still ours to release
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _dotnetObj.Dispose();
            }

            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
