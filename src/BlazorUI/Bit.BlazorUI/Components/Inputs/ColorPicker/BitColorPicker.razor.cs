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
    private int _inputsRevision;
    private double _alphaParam = 1;
    private string? _colorParam;
    private string? _abortControllerId;
    private bool _eyeDropperChecked;
    private bool _eyeDropperSupported;
    private BitColorFormat? _formatEmitted;
    private readonly BitInternalColor _color = new();
    private BitColorFormat _format = BitColorFormat.Rgb;
    private ElementReference _saturationPickerRef;
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
    /// Makes the color picker read-only: the value is still shown at full contrast, but nothing about it
    /// can be changed.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether to show a slider for editing alpha value.
    /// </summary>
    [Parameter] public bool ShowAlphaSlider { get; set; }

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

        if (firstRender is false && (_eyeDropperChecked || ShowEyeDropper is false)) return;

        try
        {
            if (firstRender)
            {
                _dotnetObj = DotNetObjectReference.Create(this);

                _abortControllerId = await _js.BitColorPickerSetup(_dotnetObj, _saturationPickerRef, nameof(HandlePointerMove), nameof(HandlePointerUp));
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
    private string _AriaLabel
    {
        get
        {
            if (AriaLabel.HasValue()) return AriaLabel!;

            var label = FormattableString.Invariant($"Color picker, Red {_color.R} Green {_color.G} Blue {_color.B}");

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

            return FormattableString.Invariant($"Saturation {Math.Round(saturation * 100)}%, Brightness {Math.Round(value * 100)}%, {_color.Hex}");
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

    private bool _IsInputDisabled => IsEnabled is false || ReadOnly;

    /// <summary>
    /// Whether the sliders refuse the gesture at the element rather than in their handler. A native range
    /// moves its own thumb before any handler is reached, so a picker that is not going to accept the change
    /// has to stop it there - otherwise the thumb is left sitting on a color the picker is not on. The
    /// disabled and the read-only picker are covered by the state they are in; the one-way bound one is not,
    /// since it looks like an ordinary picker and only has nowhere to report a change to.
    /// </summary>
    private bool _IsSliderDisabled => _IsInteractive is false;

    /// <summary>
    /// The three channel fields of the inputs row, each carrying the index the change handler switches on,
    /// the id and caption it is labelled with, and its current value.
    /// </summary>
    private (int Index, string Name, string Title, byte Value)[] _RgbFields =>
    [
        (0, "r", "Red", _color.R),
        (1, "g", "Green", _color.G),
        (2, "b", "Blue", _color.B)
    ];

    private List<(string Value, string Css, BitInternalColor Color)> _presetItems = [];

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
            }

            return _presetItems;
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
            RestoreInputs();
            return;
        }

        // Parsed onto the current alpha, so typing a six-digit hex into a semi-transparent color does not
        // silently make it opaque; an eight-digit one still brings its own alpha with it.
        _color.Parse(text, _color.A);

        RestoreInputs(text, ShowAlphaSlider ? _color.HexAlpha : _color.Hex);

        await ChangeAsync(final: true);
    }

    private async Task HandleOnChannelInput(ChangeEventArgs args, int channel)
    {
        if (_IsInteractive is false) return;

        if (TryReadNumber(args.Value, out var number) is false)
        {
            RestoreInputs();
            return;
        }

        var value = (byte)Math.Clamp(Math.Round(number), 0, 255);

        _color.SetRgb(channel == 0 ? value : _color.R,
                      channel == 1 ? value : _color.G,
                      channel == 2 ? value : _color.B);

        RestoreInputs(args.Value as string, value.ToString(CultureInfo.InvariantCulture));

        await ChangeAsync(final: true);
    }

    private async Task HandleOnAlphaFieldInput(ChangeEventArgs args)
    {
        if (_IsInteractive is false) return;

        if (TryReadNumber(args.Value, out var percent) is false)
        {
            RestoreInputs();
            return;
        }

        _color.A = percent / 100;

        RestoreInputs(args.Value as string, _AlphaPercentValue);

        await ChangeAsync(final: true);
    }

    /// <summary>
    /// Puts the committed color back into the text fields when what the user left in one of them is not
    /// what the picker ended up on - a value it refused, or one it normalized ("#0f0" into "#00FF00").
    /// </summary>
    /// <remarks>
    /// Re-rendering alone would not do it: the renderer compares the new value against the last one it
    /// rendered, not against what the user typed, so an unchanged value leaves their text sitting in the
    /// field. Bumping the revision the fields are keyed by replaces the elements instead, which is the one
    /// way to overwrite text the renderer believes is already correct.
    /// </remarks>
    private void RestoreInputs(string? typed = null, string? committed = null)
    {
        if (typed is not null && string.Equals(typed, committed, StringComparison.OrdinalIgnoreCase)) return;

        _inputsRevision++;

        StateHasChanged();
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
                Hsv = _color.Hsv
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
