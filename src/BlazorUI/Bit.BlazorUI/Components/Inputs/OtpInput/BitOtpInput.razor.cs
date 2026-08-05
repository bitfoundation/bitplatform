using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The OTP input is used for MFA procedure of authenticating users by a one-time password.
/// </summary>
public partial class BitOtpInput : BitInputBase<string?>
{
    private int _length;
    private bool _isSetup;
    private bool _autoFocused;
    private bool _setupSmsAutoFill;
    private string _labelId = default!;
    private string? _lastFilledValue;
    private Regex? _patternRegex;
    private string? _patternSource;
    private string _descriptionId = default!;
    private string?[] _inputIds = [];
    private string?[] _inputValues = [];
    private bool[] _inputFocusStates = [];
    private ElementReference[] _inputRefs = [];
    private DotNetObjectReference<BitOtpInput> _dotnetObj = default!;

    // The rules that decide which characters the code may hold. They are plain parameters, so they can
    // change at any time, and the characters that are already in the inputs have to follow them.
    private (bool Uppercase, bool Lowercase, bool NormalizeDigits, string? Pattern, BitInputType? Type) _restrictions;

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
    /// The description (helper text) rendered under the inputs, which the group of the inputs references
    /// through its aria-describedby so that screen readers announce it along with the name of the group.
    /// It is where the sentence that turns a row of empty boxes into a question the user can answer
    /// belongs: where the code was sent, how long it is good for, or what a server that rejected it said.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Custom template for the description (helper text) rendered under the inputs, which takes precedence
    /// over the <see cref="Description"/>. It is referenced the very same way, so a "resend the code"
    /// button or a countdown put in here is announced with the group as well.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

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
    /// Paints the inputs with the error state without an EditContext taking part in it, which is what
    /// reports a code that the server has rejected ("that code is not correct, try again"): the failure
    /// only becomes known once the code has been submitted, so there is nothing for a validator to see.
    /// It also marks the inputs with aria-invalid, and a failing validation of an EditContext still shows
    /// the very same state on its own.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Puts the component into the busy state of a code that has been submitted and is being checked, which
    /// is the step between the <see cref="OnFill"/> and the answer that either lets the user through or sets
    /// the <see cref="Invalid"/>. It paints an indeterminate progress bar under the inputs, marks the group
    /// with aria-busy so that the wait is announced rather than only shown, and holds the code still the way
    /// the <see cref="BitInputBase{TValue}.ReadOnly"/> does, so that nothing can be typed over a code whose
    /// answer is already on its way.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsLoading { get; set; }

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
    /// Turns every character of the code into its lower case form as it is typed or pasted, the mirror of
    /// the <see cref="Uppercase"/> and applied under the very same rules: before the <see cref="Pattern"/>
    /// is applied, so an expression restricted to lower case letters accepts an upper case keystroke, and
    /// to a code that is assigned to the component as much as to one that is typed into it. The
    /// <see cref="Uppercase"/> wins when both are set.
    /// </summary>
    [Parameter] public bool Lowercase { get; set; }

    /// <summary>
    /// The text rendered in place of every filled input, which hides the code without turning the inputs
    /// into password inputs, so a masking character of its own (a bullet, an asterisk, an emoji) can be
    /// used. The value of the component stays the code that was typed.
    /// </summary>
    [Parameter] public string? Mask { get; set; }

    /// <summary>
    /// Glues the inputs of each group together into a single field instead of leaving them standing next to
    /// each other: the gaps between them are closed, the rule they share is drawn once, and only the two
    /// ends of every group keep their rounding, which is the look of a code printed in a single box. The
    /// groups are the ones the <see cref="Separator"/> makes, so a separator with a
    /// <see cref="SeparatorInterval"/> of 3 renders a six character code as two joined boxes of three.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Merged { get; set; }

    /// <summary>
    /// Turns the digits of the other numbering systems (the Persian ۰۱۲۳, the Arabic-Indic ٠١٢٣, the
    /// full width ０１２３ and the rest) into their ASCII form as they are typed or pasted, which is what
    /// lets a code that arrives in a message written in the language of the user be typed on the keyboard
    /// of that language rather than being rejected as if it were not a number at all. The conversion
    /// happens before the <see cref="Pattern"/> is applied and before the <see cref="Type"/> rejects what
    /// is not a digit, and the value of the component is the ASCII form that a server expects.
    /// </summary>
    [Parameter] public bool NormalizeDigits { get; set; }

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
    /// Callback for when what was typed, pasted or auto filled is rejected in full by the
    /// <see cref="Type"/> or the <see cref="Pattern"/>, so that nothing of it reaches the inputs. It
    /// receives the rejected text along with the index of the input that received it, and it is what
    /// turns a silent rejection into a visible one, a message or a shake of the row. A paste that only
    /// loses some of its characters, like a code copied with the dashes in it, is not a rejection and
    /// does not raise it.
    /// </summary>
    [Parameter] public EventCallback<(string Value, int Index)> OnInvalid { get; set; }

    /// <summary>
    /// onkeydown event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(KeyboardEventArgs Event, int Index)> OnKeyDown { get; set; }

    /// <summary>
    /// onpaste event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(ClipboardEventArgs Event, int Index)> OnPaste { get; set; }

    /// <summary>
    /// A function applied to a chunk of characters that reaches the component in one go (a paste, an SMS
    /// auto fill, or a multi character input event) before anything else is done with it, which is what
    /// pulls the code out of the text it was copied inside of. The per character filtering of the
    /// <see cref="Type"/> and the <see cref="Pattern"/> cannot do that on its own for a code of letters,
    /// since the letters of the words around it match just as well as the ones of the code:
    /// "your code is A1B2C3" would fill the inputs with "YOURCODEI". Returning an empty string rejects the
    /// chunk, which raises <see cref="OnInvalid"/>. It is not applied to a single typed character, and an
    /// exception thrown out of it leaves the chunk untouched rather than breaking the input.
    /// </summary>
    [Parameter] public Func<string, string>? PasteTransformer { get; set; }

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
    /// Keeps the code free of holes: giving the focus to an input that sits after the first empty one, by
    /// clicking it or with an arrow key, moves the focus to that first empty input instead, and a chunk of
    /// characters that arrives at once (a paste or an auto fill) cannot land past it either, so the code is
    /// always filled from its start onwards. Without it a character typed into the middle of an empty row
    /// is reported as if it were the first one of the code, since the value is the characters of the inputs
    /// joined together and an empty input contributes nothing to it. A complete code is left alone, so any
    /// of its characters can still be clicked and corrected.
    /// </summary>
    [Parameter] public bool Sequential { get; set; }

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
    /// Removes the focus from the input of the BitOtpInput that currently holds it, which is what dismisses
    /// the virtual keyboard of a phone once there is nothing left to type. Nothing happens when the focus
    /// is somewhere else on the page, so a component that filled itself in the background never takes it
    /// away from what the user is doing.
    /// </summary>
    public async ValueTask BlurAsync()
    {
        // The javascript side resolves the focused element from the root, which is only bound once the
        // component has been rendered.
        if (IsRendered is false) return;

        try
        {
            await _js.BitOtpInputBlur(RootElement);
        }
        catch (JSDisconnectedException) { } // the circuit may already be gone at this point.
    }

    /// <summary>
    /// Clears the value of all of the inputs of the BitOtpInput. It does nothing while the component is
    /// disabled or read-only, and it is deliberately not refused by the <see cref="IsLoading"/>: that state
    /// belongs to the consumer calling this, whose answer from the server is usually what clears the code.
    /// </summary>
    public async Task Clear()
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        // A Backspace or a Delete that is still waiting for the input event the browser raises after it
        // would otherwise write its result over the cleared inputs.
        _handledClearIndex = -1;
        _pendingShiftIndex = -1;

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
        if (IsEnabled is false || IsReadOnly || InvalidValueBinding()) return;
        if (value.HasNoValue()) return;

        var sanitized = SanitizeValue(TransformPastedValue(value));
        if (sanitized.HasNoValue())
        {
            await OnInvalid.InvokeAsync((value, Math.Clamp(index, 0, _length - 1)));
            return;
        }

        var startIndex = GetChunkStartIndex(sanitized.Length, index);

        SetInputsValue(sanitized, startIndex, clearRest: false);

        CurrentValueAsString = string.Join(string.Empty, _inputValues);

        await FocusFirstEmptyInput(startIndex);

        if (IsDisposed) return;

        await CallOnFill();

        if (IsDisposed) return;

        // Unlike an event callback, a JSInvokable method does not re-render the component on its own,
        // so an uncontrolled component would keep showing the inputs it had before the paste.
        StateHasChanged();
    }

    [JSInvokable("ClearValue")]
    public async Task _ClearValue()
    {
        // Unlike the Clear of the consumer, a cut is the user editing the code, so it is refused by the
        // states that refuse a keystroke as well.
        if (IsEnabled is false || IsReadOnly) return;

        // A cut is a copy that takes the code with it, and the javascript side has already put the whole
        // code on the clipboard by the time this is called, so all that is left of it is the clearing.
        await Clear();

        if (IsDisposed) return;

        // The whole code was taken out of the inputs at once, wherever the caret happened to be, so the
        // typing carries on at the start of an empty code rather than in the middle of one.
        await FocusAsync();
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

        ClassBuilder.Register(() => Merged ? "bit-otp-mrg" : string.Empty);

        // The base class registers the very same class for a failing validation, so it is only added here
        // when it is not already there, otherwise it would end up in the class attribute twice.
        ClassBuilder.Register(() => Invalid && ValueInvalid is not true ? "bit-inv" : string.Empty);

        ClassBuilder.Register(() => IsReadOnly ? "bit-otp-rdl" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-otp-ldg" : string.Empty);

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
        _descriptionId = $"BitOtpInput-{UniqueId}-description";

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

        // Narrowing the set of characters that the code may hold has to reach the characters that are
        // already in the inputs too, otherwise the component would keep showing (and reporting) a code
        // that it would now reject, and widening it has to give a code that was cut down on its way in a
        // chance to be applied again.
        var restrictions = (Uppercase, Lowercase, NormalizeDigits, Pattern, Type);
        var restrictionsChanged = _restrictions != restrictions;
        _restrictions = restrictions;

        // A null CurrentValue is a request to clear the inputs, which is why it is compared as an empty
        // string here instead of being skipped like it used to be.
        if (restrictionsChanged || (CurrentValue ?? string.Empty) != string.Join(string.Empty, _inputValues))
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

        // The base class carries a single InputElement along with the FocusAsync overloads that address
        // it, and an OTP input has one element per character rather than one altogether. The input holding
        // the first character of the code stands in for it, so that the inherited members keep working
        // instead of running against an element reference that was never bound. It is re-bound on every
        // render because a Length that changes replaces the whole array of references.
        if (_inputRefs.Length > 0)
        {
            InputElement = _inputRefs[0];
        }

        // A disabled input cannot take the focus, so a component that starts out disabled is focused on
        // the first render that finds it enabled rather than losing the auto focus altogether.
        if (IsEnabled && AutoFocus && _autoFocused is false)
        {
            _autoFocused = true;

            // The first input left to fill rather than the first input of the row, so that a component
            // seeded with a partial code puts the caret where the typing is meant to carry on. A complete
            // code has none, in which case the first input is the one to land on.
            var emptyIndex = Array.FindIndex(_inputValues, v => v.HasNoValue());

            await _inputRefs[emptyIndex < 0 ? 0 : emptyIndex].FocusAsync();
        }

        // A read-only or a disabled component is showing a code rather than waiting for one, so it must
        // not ask the browser for the code that arrives by SMS, and one that is turned off while the
        // request is pending has to drop it rather than leaving the permission prompt of the browser up
        // over a component that would refuse the code anyway.
        var smsAutoFill = NoSmsAutoFill is false && IsReadOnly is false && IsEnabled;

        // The javascript side listens on the root element and resolves the input from the event target, so
        // it does not care how many inputs there are and only has to be set up again when the SMS request
        // itself has to be made or dropped. Repeating it for a Length that changed would tear the pending
        // WebOTP request down and ask for it again, which is what puts the permission prompt of the
        // browser back up over a component the user is already typing in.
        if (_isSetup && _setupSmsAutoFill == smsAutoFill) return;

        _isSetup = true;
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

    // The code is held still by the read-only state that the consumer asked for and by the busy state of a
    // code that has already been submitted alike, so everything the user could edit it with consults the
    // two together. The programmatic API is deliberately left out of it: the busy state belongs to the
    // consumer, so a Clear of its own must not be refused by the very state it has just switched on.
    private bool IsReadOnly => ReadOnly || IsLoading;

    private bool HasFocusedStyling => Classes?.Focused is not null || Styles?.Focused is not null;

    // The Length is a plain parameter, so a component that shrank between the render that laid the inputs
    // out and the event that one of them raises leaves that event addressing an input which is no longer
    // part of it, and every array of the component is sized by the Length.
    private bool IsStaleIndex(int index) => index < 0 || index >= _inputValues.Length;

    private bool HasSeparator => SeparatorTemplate is not null || Separator.HasValue();

    private int NormalizedSeparatorInterval => Math.Max(1, SeparatorInterval);

    // The groups that the separators cut the row of inputs into, which is what the merged layout glues
    // together: an input opens a group when it is the first of the row or the first after a separator, and
    // it closes one when it is the last of the row or the last before a separator. A group of a single
    // input opens and closes at once, which is what keeps all of its corners rounded.
    private bool IsGroupStart(int index) => index == 0 || (HasSeparator && index % NormalizedSeparatorInterval == 0);

    private bool IsGroupEnd(int index) => index == _length - 1 || (HasSeparator && (index + 1) % NormalizedSeparatorInterval == 0);

    // The characters of a code that is not shown must not reach the clipboard either, the very same way a
    // password input refuses to be copied, so the javascript side is told to leave the copy alone.
    private bool IsCodeHidden => Mask.HasValue() || Type is BitInputType.Password;

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

    // Only the types whose native element can actually hold a single character are rendered as they are.
    // A number typed input is the wrong element for a single character box: it reports an empty value for
    // the characters a number accepts but a code does not (e, +, -, .), it carries spin buttons, and the
    // mouse wheel changes it. The email and the url typed ones carry a constraint validation that a single
    // character can never satisfy, which would leave every box permanently invalid and keep a plain html
    // form from ever submitting. All three are rendered as text inputs instead: the keyboard they were
    // picked for comes from the inputmode below and the characters are enforced by IsAllowedChar, so
    // nothing of what the type was asked for is lost.
    private string GetInputType() => Type switch
    {
        BitInputType.Text => "text",
        BitInputType.Number => "text",
        BitInputType.Password => "password",
        BitInputType.Email => "text",
        BitInputType.Tel => "tel",
        BitInputType.Url => "text",
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

    // The aria-invalid that the base class manages for a failing validation travels in the
    // InputHtmlAttributes, which are splatted before this attribute is rendered, so the value that would
    // have been splatted has to be carried over here rather than being dropped.
    private string? GetAriaInvalid()
    {
        if (Invalid || ValueInvalid is true) return "true";

        return InputHtmlAttributes?.TryGetValue("aria-invalid", out var ariaInvalid) is true
                ? ariaInvalid?.ToString()
                : null;
    }

    // The tab order of a group of inputs holding a single value is decided by two things at once: the
    // SingleTabStop, which takes every input but the first one out of it, and the TabIndex of the
    // consumer, which places the component itself in an order of its own. The first one wins for the
    // inputs it removes, since a removed input has no position to be placed at, and the second one is
    // applied to the inputs that are still reachable.
    private string? GetTabIndex(int index)
    {
        if (SingleTabStop && index > 0) return "-1";

        return TabIndex;
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

        // A Styles object that carries none of the slots of an input leaves nothing to declare, and an
        // empty style attribute on every one of them is noise in the markup rather than a style.
        return cssStyles.Length > 0 ? cssStyles.ToString() : null;
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
        // The inputs are re-rendered on every keystroke and there is one of these per input, so the common
        // case of a component with neither custom classes nor the merged layout builds no string at all.
        if (Classes is null && Merged is false)
        {
            return _inputValues[index].HasValue() ? "bit-otp-inp bit-otp-fld" : "bit-otp-inp";
        }

        StringBuilder cssClasses = new();

        cssClasses.Append("bit-otp-inp");

        // Only the two ends of a group keep their rounding while the inputs of it are glued together, and
        // only the inputs inside of one share their rule with the input before them.
        if (Merged)
        {
            if (IsGroupStart(index))
            {
                cssClasses.Append(" bit-otp-gst");
            }

            if (IsGroupEnd(index))
            {
                cssClasses.Append(" bit-otp-gnd");
            }
        }

        if (Classes?.Input is not null)
        {
            cssClasses.Append(' ').Append(Classes.Input);
        }

        if (_inputValues[index].HasValue())
        {
            cssClasses.Append(" bit-otp-fld");

            if (Classes?.Filled is not null)
            {
                cssClasses.Append(' ').Append(Classes.Filled);
            }
        }

        if (Classes?.Focused is not null && _inputFocusStates[index])
        {
            cssClasses.Append(' ').Append(Classes.Focused);
        }

        return cssClasses.ToString();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e, int index)
    {
        if (IsEnabled is false || IsStaleIndex(index)) return;

        _inputFocusStates[index] = true;
        // Re-render so the Focused class/style is applied. This must not rely on the implicit
        // re-render of EventCallback.InvokeAsync because that only happens when the consumer has
        // attached an OnFocusIn delegate. Nothing else is painted from the focus state, so a component
        // without a Focused class or style skips the render that the auto advancing focus would
        // otherwise cause on every single keystroke.
        if (HasFocusedStyling) StateHasChanged();
        await OnFocusIn.InvokeAsync((e, index));

        // The pull back runs after the callback so that the consumer is told about the input the user
        // actually reached before the focus is corrected, and it is skipped for a component that is only
        // showing a code, where clicking a character is a way of reading it rather than of typing it.
        if (Sequential is false || IsReadOnly || IsDisposed) return;

        var emptyIndex = Array.FindIndex(_inputValues, v => v.HasNoValue());

        // A complete code has no empty input to fall back to, so any of its characters stays clickable,
        // and the first empty input itself is exactly where the focus is meant to be.
        if (emptyIndex < 0 || emptyIndex >= index) return;

        await _inputRefs[emptyIndex].FocusAsync();
    }

    private async Task HandleOnFocusOut(FocusEventArgs e, int index)
    {
        if (IsEnabled is false || IsStaleIndex(index)) return;

        _inputFocusStates[index] = false;
        // Re-render so the Focused class/style is removed regardless of whether an OnFocusOut
        // delegate is attached, and for the same reason as above only when there is one to remove.
        if (HasFocusedStyling) StateHasChanged();
        await OnFocusOut.InvokeAsync((e, index));
    }

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        if (IsStaleIndex(index)) return;

        var oldValue = _inputValues[index];
        var newValue = e.Value?.ToString()?.Trim() ?? string.Empty;

        // What the input showed before this event, which is the masking text while a Mask is set. The
        // diff below has to subtract exactly that from the new value to end up with what was typed.
        var oldRendered = GetInputValue(index) ?? string.Empty;

        var handledClear = _handledClearIndex == index;
        var pendingShift = _pendingShiftIndex == index;
        _handledClearIndex = -1;
        _pendingShiftIndex = -1;

        // Emptying the input before the wait is what lets the browser apply its own default behavior
        // before a new value is written over it. The one case it must not be done in is the clear that the
        // keydown handler owns: that handler writes the resulting values itself, and it may have written
        // them already (the browser does not promise to raise the input event within the millisecond that
        // the handler waits), in which case this would take a character of the shifted code back out.
        if (handledClear is false)
        {
            _inputValues[index] = string.Empty;
        }

        await Task.Delay(1); // waiting for input default behavior before setting a new value.

        // The component may have been resized (or taken down altogether) while the delay above was
        // pending, in which case everything below would run past the end of the arrays that the resize
        // replaced.
        if (IsDisposed || IsStaleIndex(index)) return;

        if (IsEnabled is false || IsReadOnly || InvalidValueBinding())
        {
            _inputValues[index] = oldValue;

            // Nothing was written, so there is no value to commit and no code that has just been
            // completed either: raising OnFill here would submit the code a read-only component is
            // merely showing.
            await OnInput.InvokeAsync((e, index));
            return;
        }

        if (newValue.HasValue())
        {
            var rawDiff = DiffValues(oldRendered, newValue);

            if (rawDiff.Length > 1)
            {
                // A chunk of characters arrives here when the browser fills the inputs itself or when
                // a paste slips past the clipboard handler. It goes through the very same steps as one
                // that arrives through the clipboard, so that the two can never disagree: the transformer
                // of the consumer first, then the filtering, and it is filtered rather than rejected.
                var sanitized = SanitizeValue(TransformPastedValue(rawDiff));

                if (sanitized.HasNoValue())
                {
                    _inputValues[index] = oldValue;

                    await OnInvalid.InvokeAsync((rawDiff, index));
                }
                else
                {
                    var startIndex = GetChunkStartIndex(sanitized.Length, index);

                    SetInputsValue(sanitized, startIndex, clearRest: false);

                    CurrentValueAsString = string.Join(string.Empty, _inputValues);

                    await FocusFirstEmptyInput(startIndex);

                    await OnInput.InvokeAsync((e, index));
                    await CallOnFill();
                    return;
                }
            }
            else
            {
                // A single keystroke is not a chunk, so the transformer of the consumer has no part in it
                // and only the casing and the digit normalization are applied.
                var diff = TransformValue(rawDiff);

                if (diff.HasNoValue())
                {
                    // An input event that did not change what the input is showing (a step of an IME, or a
                    // keystroke the browser took back on its own) leaves nothing to write, and writing the
                    // nothing it left would delete the character that the input still shows.
                    _inputValues[index] = oldValue;
                }
                else if (IsAllowedValue(diff) is false)
                {
                    _inputValues[index] = oldValue;

                    await OnInvalid.InvokeAsync((diff, index));
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

        if (IsEnabled is false || IsStaleIndex(index)) return;

        // A keystroke that identifies itself with neither of the two is nothing the navigation can act
        // on, but it is still a keystroke the consumer asked to be told about.
        if (e.Code is not null || e.Key is not null)
        {
            await NavigateInput(e.Code ?? string.Empty, e.Key ?? string.Empty, index);
        }

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
            if (IsReadOnly || InvalidValueBinding()) return;

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
            if (IsReadOnly || InvalidValueBinding()) return;

            // Unlike Backspace, Delete on a collapsed caret is a no-op in the browser, so the input is
            // cleared here and the (possible) input event is told to keep this result. The flag is set
            // before the first await because the input event is dispatched while this method is
            // suspended, and it reads the flag as soon as it starts.
            _handledClearIndex = index;

            await Task.Delay(1);

            // The component may be gone (or resized into one that no longer has this input) by the time the
            // delay above resumes, in which case writing the value and asking the browser for the focus
            // would run against a disposed component or past the end of the arrays of the resized one.
            if (IsDisposed || IsStaleIndex(index)) return;

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

        // Asking the browser to focus the input that already has the focus raises no event and changes
        // nothing, so the round trip is skipped for the keys that land back where they started: an arrow
        // pressed at either end of the code, or the vertical pair of arrows in a horizontal row.
        if (targetIndex is not -1 && targetIndex != index)
        {
            await _inputRefs[targetIndex].FocusAsync();
        }
    }

    /// <summary>
    /// The input that a chunk of characters arriving at once (a paste, an SMS auto fill, or a multi
    /// character input event) starts at.
    /// </summary>
    private int GetChunkStartIndex(int chunkLength, int index)
    {
        // A code that fills the whole component always lands on the first input, no matter which one
        // received it, since anything else would drop its leading characters. A shorter one is inserted
        // where the user pasted it.
        var startIndex = chunkLength >= _length ? 0 : Math.Clamp(index, 0, _length - 1);

        if (Sequential is false) return startIndex;

        // A component asked to keep the code free of holes cannot let a chunk land past the first input
        // left to fill either, otherwise a paste would punch the very hole the typing is kept away from.
        // A complete code has none, in which case the chunk simply overwrites where it arrived.
        var emptyIndex = Array.FindIndex(_inputValues, v => v.HasNoValue());

        return emptyIndex < 0 ? startIndex : Math.Min(startIndex, emptyIndex);
    }

    private void ShiftInputValues(int index)
    {
        for (int i = index; i < _length; i++)
        {
            _inputValues[i] = i < _length - 1 ? _inputValues[i + 1] : null;
        }
    }

    /// <param name="value">The characters to write into the inputs, which are filtered on their way in.</param>
    /// <param name="startIndex">The index of the input that the first of those characters lands in.</param>
    /// <param name="clearRest">
    /// Whether the inputs the value does not reach are emptied. A value assigned to the component is the
    /// whole code, so everything it leaves over has to go; a chunk that arrives at one input (a paste or an
    /// auto fill) only replaces as many characters as it brings, so pasting a single character into the
    /// middle of a code must not take the rest of it down with it.
    /// </param>
    private void SetInputsValue(string? value, int startIndex = 0, bool clearRest = true)
    {
        var chars = SanitizeValue(value).ToCharArray();

        for (int i = startIndex; i < _length; i++)
        {
            var charIndex = i - startIndex;

            if (charIndex < chars.Length)
            {
                _inputValues[i] = chars[charIndex].ToString();
            }
            else
            {
                if (clearRest is false) return;

                _inputValues[i] = null;
            }
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
            await BlurAsync();
        }

        if (IsDisposed) return;

        await OnFill.InvokeAsync(value);
    }

    private bool IsAllowedValue(string value) => value.All(IsAllowedChar);

    private bool IsAllowedChar(char value)
    {
        // A code copied out of a message brings more than its own characters along: the bidi marks that a
        // right-to-left message is written with, the zero width joiners of the scripts that need them and
        // the byte order mark that a few clipboards prepend are all invisible, so they would fill the boxes
        // with characters the user cannot see and hand the server a code it never issued. The control
        // characters are dropped for the very same reason. Neither of them is a character a code is ever
        // made of, so no input type and no pattern has to be consulted about them.
        if (char.IsControl(value) || char.GetUnicodeCategory(value) is UnicodeCategory.Format) return false;

        // A character outside of the basic plane (an emoji above all) is a pair of chars rather than one,
        // and every input of the component holds a single char, so letting one through would split it into
        // the two halves of a character that no font can draw and no server can read back.
        if (char.IsSurrogate(value)) return false;

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

    private string TransformPastedValue(string value)
    {
        if (PasteTransformer is null) return value;

        try
        {
            return PasteTransformer(value) ?? string.Empty;
        }
        catch
        {
            // The transformer of the consumer runs in the middle of a paste, so an exception thrown out of
            // it would take the whole component down with it. The chunk is left untouched instead, which
            // falls back to the filtering that the component would have done without a transformer at all.
            return value;
        }
    }

    private string TransformValue(string value)
    {
        // The invariant casing on purpose: a code is a sequence of symbols rather than a word, so the
        // casing rules of the current culture (the dotless i of Turkish above all) must not take part.
        // Asking for both cases at once is a contradiction rather than an order to apply, so the upper
        // case one is picked and the other is ignored instead of the two undoing each other.
        if (Uppercase)
        {
            value = value.ToUpperInvariant();
        }
        else if (Lowercase)
        {
            value = value.ToLowerInvariant();
        }

        if (NormalizeDigits)
        {
            value = ToAsciiDigits(value);
        }

        return value;
    }

    private static string ToAsciiDigits(string value)
    {
        // Every decimal digit of Unicode carries the number it stands for, whichever numbering system it
        // belongs to, so the whole set of them is covered without a table of the single alphabets. The
        // common case of a code that is already written in ASCII allocates nothing.
        if (value.All(c => char.IsDigit(c) is false || char.IsAsciiDigit(c))) return value;

        return new string([.. value.Select(c => char.IsDigit(c) && char.IsAsciiDigit(c) is false
                                                ? (char)('0' + (int)char.GetNumericValue(c))
                                                : c)]);
    }

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
