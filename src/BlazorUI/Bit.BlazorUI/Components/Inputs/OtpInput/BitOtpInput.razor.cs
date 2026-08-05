using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The OTP input is used for MFA procedure of authenticating users by a one-time password.
/// </summary>
public partial class BitOtpInput : BitInputBase<string?>
{
    private int _length;
    private bool _autoFocused;
    private int _setupLength = -1;
    private bool _setupSmsAutoFill;
    private string _labelId = default!;
    private string? _lastFilledValue;
    private Regex? _patternRegex;
    private string? _patternSource;
    private string?[] _inputIds = [];
    private string?[] _inputValues = [];
    private bool[] _inputFocusStates = [];
    private ElementReference[] _inputRefs = [];
    private DotNetObjectReference<BitOtpInput> _dotnetObj = default!;

    // The index whose clearing was already applied by the keydown handler (Delete). The input event
    // that the browser raises afterwards must not clear it a second time, otherwise it would undo the
    // auto shift that the keydown handler has already performed.
    private int _handledClearIndex = -1;

    // The index that the pending input event has to auto shift instead of simply clearing, set by the
    // keydown handler when Backspace is pressed while AutoShift is enabled.
    private int _pendingShiftIndex = -1;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The accent color of the inputs, applied to the border and the focus ring of the focused input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// If true, the first input is auto focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Enables auto shifting the indexes while clearing the inputs using Delete or Backspace.
    /// </summary>
    [Parameter] public bool AutoShift { get; set; }

    /// <summary>
    /// Removes the focus from the inputs as soon as the code is complete, which is what dismisses the
    /// virtual keyboard of a phone once there is nothing left to type.
    /// </summary>
    [Parameter] public bool BlurOnFill { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Classes { get; set; }

    /// <summary>
    /// Label displayed above the inputs.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom template for the label displayed above the inputs.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The composite format of the aria-label rendered on each input, where {0} is the one based index
    /// of the input and {1} is the <see cref="Length"/>. Set it to localize the position that screen
    /// readers announce for each input. The default is "{0} of {1}".
    /// </summary>
    [Parameter] public string? InputAriaLabelFormat { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the inputs, which is what decides the virtual keyboard that a
    /// phone brings up without changing the element that is rendered. It defaults to the keyboard that
    /// matches the <see cref="Type"/>, so it is only needed to ask for a keyboard the type does not imply,
    /// like the telephone keypad (whose keys are larger than the numeric ones on most Android keyboards)
    /// for a code of digits.
    /// </summary>
    [Parameter] public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Length of the OTP or number of the inputs.
    /// </summary>
    [Parameter] public int Length { get; set; } = 5;

    /// <summary>
    /// The text rendered in place of every filled input, which hides the code without turning the inputs
    /// into password inputs, so a masking character of its own (a bullet, an asterisk, an emoji) can be
    /// used. The value of the component stays the code that was typed.
    /// </summary>
    [Parameter] public string? Mask { get; set; }

    /// <summary>
    /// Disables both the SMS auto fill of the OTP through the WebOTP API of the browser and the
    /// one-time-code autofill of the inputs themselves.
    /// </summary>
    [Parameter] public bool NoSmsAutoFill { get; set; }

    /// <summary>
    /// Callback for when all of the inputs are filled.
    /// </summary>
    [Parameter] public EventCallback<string?> OnFill { get; set; }

    /// <summary>
    /// onfocusin event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(FocusEventArgs Event, int Index)> OnFocusIn { get; set; }

    /// <summary>
    /// onfocusout event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(FocusEventArgs Event, int Index)> OnFocusOut { get; set; }

    /// <summary>
    /// oninput event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(ChangeEventArgs Event, int Index)> OnInput { get; set; }

    /// <summary>
    /// onkeydown event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(KeyboardEventArgs Event, int Index)> OnKeyDown { get; set; }

    /// <summary>
    /// onpaste event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(ClipboardEventArgs Event, int Index)> OnPaste { get; set; }

    /// <summary>
    /// A regular expression that every single character of the code has to match, which is what narrows
    /// the code down to a set of characters that no input type covers on its own, like upper case
    /// letters or hexadecimal digits. Characters that do not match are rejected while typing and
    /// dropped while pasting. An unusable expression is ignored rather than breaking the input.
    /// </summary>
    [Parameter] public string? Pattern { get; set; }

    /// <summary>
    /// The hint text rendered in the empty inputs. A string as long as the <see cref="Length"/> is
    /// spread over the inputs one character each, any other value is rendered in every input as is.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Defines whether to render inputs in the opposite direction.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The text rendered between the inputs, like a dash or a dot, to make a long code easier to read.
    /// </summary>
    [Parameter] public string? Separator { get; set; }

    /// <summary>
    /// The number of inputs of each group that the <see cref="Separator"/> is rendered between, which is
    /// how a long code is split into the chunks it is usually printed in, like 123-456. The default is 1,
    /// meaning a separator between every pair of inputs. Values below 1 are treated as 1.
    /// </summary>
    [Parameter] public int SeparatorInterval { get; set; } = 1;

    /// <summary>
    /// Custom template rendered between the inputs in place of the <see cref="Separator"/> text, which is
    /// what puts an icon or any other markup between the groups of a code. The context is the zero based
    /// index of the input the separator is rendered before, so a template can tell one separator of the
    /// row from another. It takes precedence over the <see cref="Separator"/>.
    /// </summary>
    [Parameter] public RenderFragment<int>? SeparatorTemplate { get; set; }

    /// <summary>
    /// Turns the whole component into a single stop of the tab order: only the first input is reachable
    /// with the Tab key and the rest are left to the auto advancing focus, the arrow keys and the mouse.
    /// Tabbing out of the code then lands on the element after it rather than on its next character,
    /// which is what a group of inputs that hold one single value is expected to do.
    /// </summary>
    [Parameter] public bool SingleTabStop { get; set; }

    /// <summary>
    /// The size of the inputs.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Turns every character of the code into its upper case form as it is typed or pasted, which is what
    /// lets a code that is printed in upper case be typed in either case. The conversion happens before
    /// the <see cref="Pattern"/> is applied, so an expression restricted to upper case letters accepts a
    /// lower case keystroke instead of rejecting it.
    /// </summary>
    [Parameter] public bool Uppercase { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Styles { get; set; }

    /// <summary>
    /// Type of the inputs.
    /// </summary>
    [Parameter] public BitInputType? Type { get; set; }

    /// <summary>
    /// The visual variant of the inputs, which decides how much of the frame around each input is
    /// painted: a full fill, only an outline, or just an underline.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render inputs vertically.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    /// <summary>
    /// The ElementReferences to the input elements of the BitOtpInput.
    /// </summary>
    public ElementReference[] InputElements => _inputRefs;

    /// <summary>
    /// Clears the value of all of the inputs of the BitOtpInput.
    /// </summary>
    public async Task Clear()
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        Array.Clear(_inputValues);

        CurrentValueAsString = string.Empty;

        await CallOnFill();

        StateHasChanged();
    }

    /// <summary>
    /// Gives focus to a specific input element of the BitOtpInput.
    /// The index is clamped into the range of the rendered inputs.
    /// </summary>
    public ValueTask FocusAsync(int index = 0)
    {
        // The element references are only bound once the inputs have been rendered, so calling this from
        // an initializing consumer would ask the browser to focus an element that is not there yet.
        if (IsRendered is false || _inputRefs.Length == 0) return ValueTask.CompletedTask;

        return _inputRefs[Math.Clamp(index, 0, _inputRefs.Length - 1)].FocusAsync();
    }



    [JSInvokable("SetValue")]
    public async Task _SetValue(string value, int index)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;
        if (value.HasNoValue()) return;

        var sanitized = SanitizeValue(value);
        if (sanitized.HasNoValue()) return;

        // A code that fills the whole component always lands on the first input, no matter which one
        // received the paste, since anything else would drop its leading characters. A shorter one is
        // inserted where the user pasted it.
        var startIndex = sanitized.Length >= _length ? 0 : Math.Clamp(index, 0, _length - 1);

        SetInputsValue(sanitized, startIndex);

        CurrentValueAsString = string.Join(string.Empty, _inputValues);

        await FocusFirstEmptyInput(startIndex);

        if (IsDisposed) return;

        await CallOnFill();

        if (IsDisposed) return;

        // Unlike an event callback, a JSInvokable method does not re-render the component on its own,
        // so an uncontrolled component would keep showing the inputs it had before the paste.
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-otp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-otp-fil",
            BitVariant.Outline => "bit-otp-otl",
            BitVariant.Text => "bit-otp-txt",
            _ => "bit-otp-otl"
        });

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-otp-pri",
            BitColor.Secondary => "bit-otp-sec",
            BitColor.Tertiary => "bit-otp-ter",
            BitColor.Info => "bit-otp-inf",
            BitColor.Success => "bit-otp-suc",
            BitColor.Warning => "bit-otp-wrn",
            BitColor.SevereWarning => "bit-otp-swr",
            BitColor.Error => "bit-otp-err",
            BitColor.PrimaryBackground => "bit-otp-pbg",
            BitColor.SecondaryBackground => "bit-otp-sbg",
            BitColor.TertiaryBackground => "bit-otp-tbg",
            BitColor.PrimaryForeground => "bit-otp-pfg",
            BitColor.SecondaryForeground => "bit-otp-sfg",
            BitColor.TertiaryForeground => "bit-otp-tfg",
            BitColor.PrimaryBorder => "bit-otp-pbr",
            BitColor.SecondaryBorder => "bit-otp-sbr",
            BitColor.TertiaryBorder => "bit-otp-tbr",
            _ => "bit-otp-pri"
        });

        ClassBuilder.Register(() => Reversed ? "bit-otp-rvs" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-otp-rdl" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-otp-req" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-otp-sm",
            BitSize.Medium => "bit-otp-md",
            BitSize.Large => "bit-otp-lg",
            _ => "bit-otp-md"
        });

        ClassBuilder.Register(() => Vertical ? "bit-otp-vrt" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _labelId = $"BitOtpInput-{UniqueId}-label";

        ResizeInputs();

        SetDefaultValue();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // The Length is a plain parameter, so it can change at any time. Everything that is sized by it
        // has to follow, otherwise the render loop below would index past the end of these arrays.
        if (_length != NormalizedLength)
        {
            ResizeInputs();
        }

        // A null CurrentValue is a request to clear the inputs, which is why it is compared as an empty
        // string here instead of being skipped like it used to be.
        if ((CurrentValue ?? string.Empty) != string.Join(string.Empty, _inputValues))
        {
            SetInputsValue(CurrentValue);

            var appliedValue = string.Join(string.Empty, _inputValues);

            // What ends up in the inputs is not always what was assigned: a code longer than the inputs
            // can hold loses its extra characters, the Uppercase turns it into its upper case form and
            // the Pattern along with the Type drop the characters they reject. Reporting a value that the
            // component is not showing would leave the two out of sync for good, so the value follows
            // what ended up in the inputs rather than the other way around.
            if ((CurrentValue ?? string.Empty) != appliedValue)
            {
                CurrentValueAsString = appliedValue;
            }
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        if (IsEnabled is false) return;

        // A disabled input cannot take the focus, so a component that starts out disabled is focused on
        // the first render that finds it enabled rather than losing the auto focus altogether.
        if (AutoFocus && _autoFocused is false)
        {
            _autoFocused = true;

            await _inputRefs[0].FocusAsync();
        }

        // A read-only component is showing a code rather than waiting for one, so it must not ask the
        // browser for the code that arrives by SMS.
        var smsAutoFill = NoSmsAutoFill is false && ReadOnly is false;

        // The javascript side listens on the root element and resolves the input from the event target,
        // so it only needs to be set up again when the number of inputs changes (or when the component
        // becomes enabled after having started out disabled), and when the SMS request itself has to be
        // made or dropped.
        if (_setupLength == _length && _setupSmsAutoFill == smsAutoFill) return;

        _setupLength = _length;
        _setupSmsAutoFill = smsAutoFill;

        if (IsDisposed) return;

        try
        {
            await _js.BitOtpInputSetup(UniqueId, _dotnetObj, RootElement, smsAutoFill);
        }
        catch (JSDisconnectedException) { } // the circuit may already be gone at this point.
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }



    private int NormalizedLength => Math.Max(1, Length);

    private bool HasFocusedStyling => Classes?.Focused is not null || Styles?.Focused is not null;

    private void ResizeInputs()
    {
        var oldValues = _inputValues;
        var oldFocusStates = _inputFocusStates;

        _length = NormalizedLength;

        // A code of another length is another code, so the completed one that OnFill was raised for last
        // must not keep the callback from firing for the code that fills the resized component.
        _lastFilledValue = null;

        _inputIds = Enumerable.Range(0, _length).Select(i => $"BitOtpInput-{UniqueId}-input-{i}").ToArray();
        _inputRefs = new ElementReference[_length];
        _inputValues = new string?[_length];
        _inputFocusStates = new bool[_length];

        // Growing or shrinking the component keeps whatever the user has already typed in the inputs that
        // survive the resize, along with the focus state of the one they are typing in: no focusout is
        // raised for the inputs that the resize removes, so a lost focus state would never be restored.
        for (int i = 0; i < Math.Min(oldValues.Length, _length); i++)
        {
            _inputValues[i] = oldValues[i];
            _inputFocusStates[i] = oldFocusStates[i];
        }
    }

    // A number typed input is the wrong element for a single character box: it reports an empty value for
    // the characters a number accepts but a code does not (e, +, -, .), it carries spin buttons, and the
    // mouse wheel changes it. The numeric keyboard it is picked for comes from the inputmode below
    // instead, and the digits are enforced by IsAllowedChar, so it is rendered as a text input.
    private string GetInputType() => Type switch
    {
        BitInputType.Text => "text",
        BitInputType.Number => "text",
        BitInputType.Password => "password",
        BitInputType.Email => "email",
        BitInputType.Tel => "tel",
        BitInputType.Url => "url",
        _ => "text"
    };

    // The inputmode is what decides the virtual keyboard on a phone, which is where a one-time code is
    // typed most of the time, so every type maps to the keyboard that matches it, unless the InputMode
    // asks for another one.
    private string GetInputMode() => InputMode?.ToString().ToLowerInvariant() ?? Type switch
    {
        BitInputType.Number => "numeric",
        BitInputType.Email => "email",
        BitInputType.Tel => "tel",
        BitInputType.Url => "url",
        _ => "text"
    };

    // What the input shows, which is the masking text rather than the character itself while a Mask is
    // set. The value of the component is unaffected: only the rendering of the inputs is.
    private string? GetInputValue(int index)
    {
        var value = _inputValues[index];

        return (Mask.HasValue() && value.HasValue()) ? Mask : value;
    }

    private string? GetPlaceholder(int index)
    {
        if (Placeholder.HasNoValue()) return null;

        return Placeholder!.Length == _length ? Placeholder[index].ToString() : Placeholder;
    }

    private string GetInputAriaLabel(int index)
    {
        // Formatted by hand rather than with string.Format so that a consumer supplied format can never
        // throw in the middle of a render.
        return (InputAriaLabelFormat.HasValue() ? InputAriaLabelFormat! : "{0} of {1}")
                .Replace("{0}", (index + 1).ToString(), StringComparison.Ordinal)
                .Replace("{1}", _length.ToString(), StringComparison.Ordinal);
    }

    private string? GetInputStyles(int index)
    {
        // The inputs are re-rendered on every keystroke and there is one of these per input, so the
        // common case of a component without custom styles does not build a string at all.
        if (Styles is null) return null;

        StringBuilder cssStyles = new();

        if (Styles.Input is not null)
        {
            cssStyles.Append(Styles.Input);
        }

        // The segments are joined with a semicolon rather than with a space, since a style that omits
        // its trailing one would otherwise swallow the declaration appended after it.
        if (Styles.Filled is not null && _inputValues[index].HasValue())
        {
            AppendStyle(cssStyles, Styles.Filled);
        }

        if (Styles.Focused is not null && _inputFocusStates[index])
        {
            AppendStyle(cssStyles, Styles.Focused);
        }

        return cssStyles.ToString();
    }

    private static void AppendStyle(StringBuilder styles, string style)
    {
        if (styles.Length > 0 && styles[^1] is not ';')
        {
            styles.Append(';');
        }

        styles.Append(style);
    }

    private string GetInputClasses(int index)
    {
        if (Classes is null)
        {
            return _inputValues[index].HasValue() ? "bit-otp-inp bit-otp-fld" : "bit-otp-inp";
        }

        StringBuilder cssClasses = new();

        cssClasses.Append("bit-otp-inp");

        if (Classes?.Input is not null)
        {
            cssClasses.Append(' ').Append(Classes?.Input);
        }

        if (_inputValues[index].HasValue())
        {
            cssClasses.Append(" bit-otp-fld");

            if (Classes?.Filled is not null)
            {
                cssClasses.Append(' ').Append(Classes?.Filled);
            }
        }

        if (Classes?.Focused is not null && _inputFocusStates[index])
        {
            cssClasses.Append(' ').Append(Classes?.Focused);
        }

        return cssClasses.ToString();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        _inputFocusStates[index] = true;
        // Re-render so the Focused class/style is applied. This must not rely on the implicit
        // re-render of EventCallback.InvokeAsync because that only happens when the consumer has
        // attached an OnFocusIn delegate. Nothing else is painted from the focus state, so a component
        // without a Focused class or style skips the render that the auto advancing focus would
        // otherwise cause on every single keystroke.
        if (HasFocusedStyling) StateHasChanged();
        await OnFocusIn.InvokeAsync((e, index));
    }

    private async Task HandleOnFocusOut(FocusEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        _inputFocusStates[index] = false;
        // Re-render so the Focused class/style is removed regardless of whether an OnFocusOut
        // delegate is attached, and for the same reason as above only when there is one to remove.
        if (HasFocusedStyling) StateHasChanged();
        await OnFocusOut.InvokeAsync((e, index));
    }

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        var oldValue = _inputValues[index];
        var newValue = e.Value?.ToString()?.Trim() ?? string.Empty;

        // What the input showed before this event, which is the masking text while a Mask is set. The
        // diff below has to subtract exactly that from the new value to end up with what was typed.
        var oldRendered = GetInputValue(index) ?? string.Empty;

        var handledClear = _handledClearIndex == index;
        var pendingShift = _pendingShiftIndex == index;
        _handledClearIndex = -1;
        _pendingShiftIndex = -1;

        _inputValues[index] = string.Empty;
        await Task.Delay(1); // waiting for input default behavior before setting a new value.

        if (IsEnabled is false || ReadOnly || InvalidValueBinding())
        {
            _inputValues[index] = oldValue;
        }
        else if (newValue.HasValue())
        {
            var diff = TransformValue(DiffValues(oldRendered, newValue));

            if (diff.Length > 1)
            {
                // A chunk of characters arrives here when the browser fills the inputs itself or when
                // a paste slips past the clipboard handler. It is filtered rather than rejected, the
                // way the same code pasted through the clipboard would be.
                var sanitized = SanitizeValue(diff);

                if (sanitized.HasNoValue())
                {
                    _inputValues[index] = oldValue;
                }
                else
                {
                    var startIndex = sanitized.Length >= _length ? 0 : index;

                    SetInputsValue(sanitized, startIndex);

                    CurrentValueAsString = string.Join(string.Empty, _inputValues);

                    await FocusFirstEmptyInput(startIndex);

                    await OnInput.InvokeAsync((e, index));
                    await CallOnFill();
                    return;
                }
            }
            else if (IsAllowedValue(diff) is false)
            {
                _inputValues[index] = oldValue;
            }
            else
            {
                _inputValues[index] = diff;

                // Commit the current value before moving the focus to the next input.
                // Auto-advancing the focus raises the focusout/focusin DOM events (and the
                // consumer's OnFocusOut/OnFocusIn callbacks) synchronously while this method
                // is awaiting FocusAsync. If the value is committed afterwards, those focus
                // callbacks observe a stale, incomplete value. So we set it here first.
                CurrentValueAsString = string.Join(string.Empty, _inputValues);

                int nextIndex = index + 1;
                if (nextIndex < _length) await _inputRefs[nextIndex].FocusAsync();

                await OnInput.InvokeAsync((e, index));
                await CallOnFill();
                return;
            }
        }
        else if (handledClear)
        {
            // The keydown handler owns this clear: it already wrote the resulting values (and ran the
            // auto shift) while this method was awaiting above, so nothing is written here.
        }
        else if (pendingShift)
        {
            ShiftInputValues(index);
        }
        else
        {
            _inputValues[index] = null;
        }

        CurrentValueAsString = string.Join(string.Empty, _inputValues);

        await OnInput.InvokeAsync((e, index));
        await CallOnFill();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        _handledClearIndex = -1;
        _pendingShiftIndex = -1;

        if (IsEnabled is false) return;
        if (e.Code is null && e.Key is null) return;

        await NavigateInput(e.Code ?? string.Empty, e.Key ?? string.Empty, index);

        await OnKeyDown.InvokeAsync((e, index));
    }

    private async Task HandleOnPaste(ClipboardEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        await OnPaste.InvokeAsync((e, index));
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = Math.Min(index + 1, _length - 1);
        int previousIndex = Math.Max(index - 1, 0);

        if (code is "Backspace" || key is "Backspace")
        {
            // Arrow navigation stays available in the states below, since it changes nothing but the
            // focus; only the two keys that clear a character bail out here.
            if (ReadOnly || InvalidValueBinding()) return;

            if (_inputValues[index].HasValue())
            {
                // The browser clears the input itself, which raises an input event right after this
                // handler, so the value is left to that event (and the auto shift is deferred to it, so
                // that the two never race over it). The focus deliberately stays where it is: moving it
                // back here would send the character that the user retypes into the wrong input, which
                // is exactly what correcting the last character of a code needs to do.
                if (AutoShift)
                {
                    _pendingShiftIndex = index;
                }

                return;
            }

            // An input that is already empty gives Backspace nothing to delete, so it deletes the
            // character before it and follows it, the way the very same key behaves in a single input
            // holding the whole code. No input event is raised for an empty input, which is why the
            // value is written right here.
            if (index == 0) return;

            if (AutoShift)
            {
                ShiftInputValues(previousIndex);
            }
            else
            {
                _inputValues[previousIndex] = null;
            }

            CurrentValueAsString = string.Join(string.Empty, _inputValues);

            await _inputRefs[previousIndex].FocusAsync();

            await CallOnFill();
            return;
        }

        if (code is "Delete" || key is "Delete")
        {
            // Arrow navigation stays available in the states below, since it changes nothing but the
            // focus; only the two keys that clear a character bail out here.
            if (ReadOnly || InvalidValueBinding()) return;

            // Unlike Backspace, Delete on a collapsed caret is a no-op in the browser, so the input is
            // cleared here and the (possible) input event is told to keep this result. The flag is set
            // before the first await because the input event is dispatched while this method is
            // suspended, and it reads the flag as soon as it starts.
            _handledClearIndex = index;

            await Task.Delay(1);

            // The component may be gone by the time the delay above resumes, in which case writing the
            // value and asking the browser for the focus would run against a disposed component.
            if (IsDisposed) return;

            if (AutoShift)
            {
                ShiftInputValues(index);
            }
            else
            {
                _inputValues[index] = null;
            }

            CurrentValueAsString = string.Join(string.Empty, _inputValues);

            await _inputRefs[index].FocusAsync();

            await CallOnFill();
            return;
        }

        // The physical key is preferred, since it is the one that identifies the arrows independently of
        // the keyboard layout, but a few mobile keyboards report the key alone, so it stands in for it.
        var navKey = code is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" or "Home" or "End"
                        ? code
                        : key;

        var targetIndex = navKey switch
        {
            "ArrowLeft" => Vertical ? index : ((Dir is BitDir.Rtl ^ Reversed) ? nextIndex : previousIndex),
            "ArrowRight" => Vertical ? index : ((Dir is BitDir.Rtl ^ Reversed) ? previousIndex : nextIndex),
            "ArrowUp" => Vertical ? (Reversed ? nextIndex : previousIndex) : index,
            "ArrowDown" => Vertical ? (Reversed ? previousIndex : nextIndex) : index,
            // Home and End address the code rather than the layout, so they always land on the input
            // holding the first and the last character of it, whichever way the inputs are rendered.
            "Home" => 0,
            "End" => _length - 1,
            _ => -1 // For Tab key
        };

        if (targetIndex is not -1)
        {
            await _inputRefs[targetIndex].FocusAsync();
        }
    }

    private void ShiftInputValues(int index)
    {
        for (int i = index; i < _length; i++)
        {
            _inputValues[i] = i < _length - 1 ? _inputValues[i + 1] : null;
        }
    }

    private void SetInputsValue(string? value, int startIndex = 0)
    {
        var chars = SanitizeValue(value).ToCharArray();

        for (int i = 0; i < _length; i++)
        {
            if (i < startIndex) continue;

            var charIndex = i - startIndex;

            _inputValues[i] = chars.Length > charIndex ? chars[charIndex].ToString() : null;
        }
    }

    /// <summary>
    /// Moves the focus to the first input left to fill after a chunk of characters landed in the
    /// component at once (a paste, an SMS auto fill, or a multi character input event), which is the
    /// input right after the ones that were just written whenever there is one.
    /// </summary>
    private async Task FocusFirstEmptyInput(int fromIndex)
    {
        var emptyIndex = Array.FindIndex(_inputValues, Math.Clamp(fromIndex, 0, _length - 1), v => v.HasNoValue());

        if (emptyIndex < 0)
        {
            emptyIndex = Array.FindIndex(_inputValues, v => v.HasNoValue());
        }

        await _inputRefs[emptyIndex < 0 ? _length - 1 : emptyIndex].FocusAsync();
    }

    private async Task CallOnFill()
    {
        var value = CurrentValue;

        if (value?.Length != _length)
        {
            _lastFilledValue = null;
            return;
        }

        // Without this guard the callback would fire again on every keystroke that lands on an already
        // complete code, which typically re-triggers the consumer's "submit the code" logic.
        if (_lastFilledValue == value) return;

        _lastFilledValue = value;

        if (BlurOnFill)
        {
            try
            {
                await _js.BitOtpInputBlur(RootElement);
            }
            catch (JSDisconnectedException) { } // the circuit may already be gone at this point.
        }

        if (IsDisposed) return;

        await OnFill.InvokeAsync(value);
    }

    private bool IsAllowedValue(string value) => value.All(IsAllowedChar);

    private bool IsAllowedChar(char value)
    {
        // int.TryParse used to stand in for the numeric check, which rejected any numeric code longer
        // than int.MaxValue has digits and accepted a leading sign.
        if (Type is BitInputType.Number && char.IsAsciiDigit(value) is false) return false;

        var regex = GetPatternRegex();
        if (regex is null) return true;

        try
        {
            return regex.IsMatch(value.ToString());
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private Regex? GetPatternRegex()
    {
        if (Pattern.HasNoValue())
        {
            _patternSource = null;
            _patternRegex = null;
            return null;
        }

        if (_patternSource == Pattern) return _patternRegex;

        _patternSource = Pattern;

        try
        {
            // The timeout keeps a pathological expression from stalling the render loop on every
            // keystroke, and an expression that does not compile is simply not applied.
            _patternRegex = new Regex(Pattern!, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException)
        {
            _patternRegex = null;
        }

        return _patternRegex;
    }

    private string SanitizeValue(string? value)
    {
        if (value.HasNoValue()) return string.Empty;

        // Codes are copied out of a message that often wraps or spaces them, so every kind of
        // whitespace is dropped rather than the space character alone.
        return new string([.. TransformValue(value!).Where(c => char.IsWhiteSpace(c) is false && IsAllowedChar(c))]);
    }

    // The invariant casing on purpose: a code is a sequence of symbols rather than a word, so the casing
    // rules of the current culture (the dotless i of Turkish above all) must not take part in it.
    private string TransformValue(string value) => Uppercase ? value.ToUpperInvariant() : value;

    private static string DiffValues(string oldValue, string newValue)
    {
        var oldLength = oldValue.Length;
        var newLength = newValue.Length;

        if (newLength == 1) return newValue;
        if (newLength < oldLength) return newValue;

        if (newValue[..oldLength] == oldValue) return newValue[oldLength..newLength];

        if (newValue.Substring(newLength - oldLength, oldLength) == oldValue) return newValue[..(newLength - oldLength)];

        return newValue;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitOtpInputDispose(UniqueId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
