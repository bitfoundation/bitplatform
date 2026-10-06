namespace Bit.BlazorUI;

/// <summary>
/// What a prompt shown through the <see cref="BitMessageBoxService"/> shares between the field in its body and the service
/// waiting on its answer: the value typed so far, and the error the last check of it found.
/// </summary>
internal sealed class BitMessageBoxPromptState(BitMessageBoxPromptParameters parameters, string id, BitMessageBoxResult affirmative)
{
    public BitMessageBoxPromptParameters Parameters { get; } = parameters;

    public BitMessageBoxResult Affirmative { get; } = affirmative;

    public string MessageId { get; } = $"{id}-msg";

    public bool HasMessage => Parameters.Body.HasValue() || Parameters.BodyTemplate is not null;

    /// <summary>
    /// The accessible name of a field that has no visible label: the question where it is text, else the title.
    /// </summary>
    public string? FieldAriaLabel => Parameters.Label.HasValue()
                                        ? null
                                        : Parameters.BodyTemplate is null && Parameters.Body.HasValue() ? Parameters.Body : Parameters.Title;

    /// <summary>
    /// Whether the question is what names the field, in which case it does not describe the dialog as well.
    /// </summary>
    public bool QuestionNamesTheField => Parameters.Label.HasNoValue() && Parameters.BodyTemplate is null && Parameters.Body.HasValue();

    public string? Value { get; set; } = parameters.Value;

    public string? Error { get; private set; }

    /// <summary>
    /// Whether the <see cref="BitMessageBoxPromptParameters.AsyncValidator"/> is still working out its answer.
    /// </summary>
    public bool Validating { get; private set; }

    /// <summary>
    /// The value the last accepted answer was checked with, which is the one handed back: an edit made while an
    /// asynchronous check runs is not what that check said yes to.
    /// </summary>
    public string? AcceptedValue { get; private set; }

    /// <summary>
    /// Faults with what a validator threw, which the service closes the box on and rethrows to the caller of the prompt -
    /// rather than letting it escape into the click or the key that was being handled, where nothing is waiting for it.
    /// </summary>
    public Task Failed => _failure.Task;

    // Set by the first refused answer: from then on every edit re-checks the value.
    private bool _refused;

    // Cancelled once the showing is over, however it ended, so a check still running is told it is no longer wanted.
    private readonly CancellationTokenSource _closed = new();

    private readonly TaskCompletionSource _failure = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Raised when <see cref="Error"/> changes outside the field's own events, with whether the field should take the focus.
    /// </summary>
    public event Action<bool>? Changed;

    /// <summary>
    /// Gives up on a check that is still running, once the box is no longer on the screen.
    /// </summary>
    public void Close() => _closed.Cancel();

    /// <summary>
    /// Checks the value before the box is answered with it, and moves the focus back onto the field when it is refused -
    /// which is where the error that explains why is read out. A check that throws is reported through <see cref="Failed"/>
    /// and refuses the answer, and one given up on through the <paramref name="cancellationToken"/> refuses it quietly.
    /// </summary>
    public async Task<bool> TryAcceptAsync(CancellationToken cancellationToken)
    {
        var value = Value ?? string.Empty;

        if (TryValidate(out var error) is false) return false;

        Error = error;

        if (Error is null && Parameters.AsyncValidator is not null)
        {
            Validating = true;
            Changed?.Invoke(false);

            using var abandoned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closed.Token);

            try
            {
                error = await Parameters.AsyncValidator(value, abandoned.Token);
            }
            catch (Exception ex)
            {
                // A validator that throws does not leave the field busy behind the failure it reports.
                Validating = false;
                Changed?.Invoke(false);

                if (ex is OperationCanceledException && abandoned.IsCancellationRequested) return false;

                _failure.TrySetException(ex);
                return false;
            }

            Validating = false;

            // An answer taken back while it was checked is neither accepted nor refused, whatever the check said.
            if (abandoned.IsCancellationRequested)
            {
                Changed?.Invoke(false);
                return false;
            }

            Error = error;

            // A refusal of a value that was edited while the check ran is about a value that is no longer there: the
            // answer is still refused, but what the field shows is what the synchronous checks say about its current value.
            if (Error is not null && (Value ?? string.Empty) != value)
            {
                _refused = true;
                if (TryValidate(out error) is false) return false;
                Error = error;
                Changed?.Invoke(false);
                return false;
            }
        }

        _refused |= Error is not null;

        if (Error is null)
        {
            AcceptedValue = value;
        }

        Changed?.Invoke(Error is not null);

        return Error is null;
    }

    /// <summary>
    /// Re-checks the value after an edit, but only once an answer has been refused: a field is not told it is wrong
    /// while it is still being typed into for the first time. Only the synchronous checks run, so a message the asynchronous
    /// one gave - about a value that is no longer there - goes away. Returns whether the error changed.
    /// </summary>
    public bool Revalidate()
    {
        if (_refused is false) return false;

        var previous = Error;

        if (TryValidate(out var error) is false) return false;

        Error = error;

        return previous != Error;
    }

    // The synchronous checks. A Validator that throws is reported through Failed, and false is returned.
    private bool TryValidate(out string? error)
    {
        error = null;

        var value = Value ?? string.Empty;

        if (Parameters.Required is true && string.IsNullOrWhiteSpace(value))
        {
            error = Parameters.RequiredMessage ?? "A value is required.";
            return true;
        }

        if (Parameters.Validator is null) return true;

        try
        {
            error = Parameters.Validator(value);
            return true;
        }
        catch (Exception ex)
        {
            _failure.TrySetException(ex);
            return false;
        }
    }
}

/// <summary>
/// The body of a prompt: its question, and the text field the answer is typed into.
/// </summary>
internal sealed class BitMessageBoxPrompt : ComponentBase, IDisposable
{
    private bool _focusPending;
    private BitTextField? _field;
    private readonly EventCallback<string?> _valueChanged;
    private readonly EventCallback<KeyboardEventArgs> _keyDown;



    public BitMessageBoxPrompt()
    {
        // Created once and with no receiver, so an edit re-renders the field alone - the prompt only re-renders when
        // the error under the field changes. A render of the prompt for every keystroke hands the field back a value
        // the user has already typed past, which costs characters when typing fast.
        _valueChanged = new EventCallback<string?>(null, (Action<string?>)HandleValueChanged);
        _keyDown = new EventCallback<KeyboardEventArgs>(null, (Func<KeyboardEventArgs, Task>)HandleKeyDown);
    }



    [Parameter, EditorRequired] public BitMessageBoxPromptState State { get; set; } = default!;

    [CascadingParameter] public BitMessageBox? MessageBox { get; set; }



    public void Dispose() => State.Changed -= HandleStateChanged;



    protected override void OnInitialized() => State.Changed += HandleStateChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending is false || _field is not { } field || State.Validating) return;

        _focusPending = false;

        await FocusSafely.RunAsync(field.FocusAsync);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var parameters = State.Parameters;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "bit-msb-pmt");

        if (State.HasMessage)
        {
            // The question keeps the line breaks written into it, as the body of any other message box does.
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "id", State.MessageId);
            builder.AddAttribute(4, "class", parameters.BodyTemplate is null ? "bit-msb-pmg bit-msb-txt" : "bit-msb-pmg");
            if (parameters.BodyTemplate is not null)
            {
                builder.AddContent(5, parameters.BodyTemplate);
            }
            else
            {
                builder.AddContent(6, parameters.Body);
            }
            builder.CloseElement();
        }

        builder.OpenComponent<BitTextField>(7);
        builder.AddComponentParameter(8, nameof(BitTextField.Value), State.Value);
        builder.AddComponentParameter(9, nameof(BitTextField.ValueChanged), _valueChanged);
        builder.AddComponentParameter(10, nameof(BitTextField.Immediate), true);
        builder.AddComponentParameter(11, nameof(BitTextField.AutoFocus), true);
        builder.AddComponentParameter(12, nameof(BitTextField.SelectOnFocus), true);
        builder.AddComponentParameter(13, nameof(BitTextField.ErrorMessage), State.Error);
        builder.AddComponentParameter(14, nameof(BitTextField.Loading), State.Validating);

        // Only what the caller set is handed over, so a BitParams around the container still has the last word on the rest.
        if (parameters.Required is true) builder.AddComponentParameter(15, nameof(BitTextField.Required), true);
        if (parameters.Label.HasValue()) builder.AddComponentParameter(16, nameof(BitTextField.Label), parameters.Label);
        if (parameters.Placeholder.HasValue()) builder.AddComponentParameter(17, nameof(BitTextField.Placeholder), parameters.Placeholder);
        if (parameters.Description.HasValue()) builder.AddComponentParameter(18, nameof(BitTextField.Description), parameters.Description);
        if (parameters.InputType.HasValue) builder.AddComponentParameter(19, nameof(BitTextField.Type), parameters.InputType);
        if (parameters.CanRevealPassword is true) builder.AddComponentParameter(20, nameof(BitTextField.CanRevealPassword), true);
        if (parameters.AutoComplete.HasValue()) builder.AddComponentParameter(21, nameof(BitTextField.AutoComplete), parameters.AutoComplete);
        if (parameters.MaxLength.HasValue) builder.AddComponentParameter(22, nameof(BitTextField.MaxLength), parameters.MaxLength.Value);
        if (parameters.Multiline is true)
        {
            builder.AddComponentParameter(23, nameof(BitTextField.Multiline), true);
            builder.AddComponentParameter(24, nameof(BitTextField.Rows), 3);
        }
        else
        {
            // Enter answers a single-line prompt, so an on-screen keyboard labels its return key as the end of the task.
            builder.AddComponentParameter(25, nameof(BitTextField.EnterKeyHint), "done");
        }
        // The field is the size of the box it is in, which a BitMessageBoxParams around the container may be what set.
        if ((MessageBox?.Size ?? parameters.Size) is { } size) builder.AddComponentParameter(26, nameof(BitTextField.Size), size);
        builder.AddComponentParameter(27, nameof(BitTextField.OnKeyDown), _keyDown);
        if (parameters.Dir.HasValue) builder.AddComponentParameter(28, nameof(BitTextField.Dir), parameters.Dir);
        builder.AddComponentParameter(29, nameof(BitTextField.Class), "bit-msb-pfl");

        // A field with no visible label of its own is named by the question above it - or, where that is markup, by the
        // title of the box - so it is never announced as a bare "edit text". The name is the words themselves: the field
        // writes its own aria-labelledby, which leaves no room for one pointing at the question.
        if (State.FieldAriaLabel is { } ariaLabel)
        {
            builder.AddComponentParameter(30, nameof(BitTextField.AriaLabel), ariaLabel);
        }

        builder.AddComponentReferenceCapture(31, field => _field = (BitTextField)field);
        builder.CloseComponent();

        builder.CloseElement();
    }



    private void HandleStateChanged(bool focus)
    {
        _focusPending |= focus;

        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleValueChanged(string? value)
    {
        State.Value = value;

        if (State.Revalidate())
        {
            StateHasChanged();
        }
    }

    // Enter answers the box the way its affirmative button does - the answer still goes through the check of the value
    // and the OnBeforeResult of the caller. A multi-line field keeps Enter for its new lines and answers on Ctrl+Enter,
    // and a key pressed to compose a character with an IME is not an answer at all. On every target that last one is the
    // field's to see to: Immediate turns its composition guard on, which stops the Enter that commits a candidate (flagged
    // by isComposing, by the legacy 229 key code, or arriving between compositionstart and compositionend) before Blazor
    // sees it - so no keydown of a composition reaches this handler on .NET 8, whose event does not carry the state.
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Enter" || MessageBox is null) return;

#if NET9_0_OR_GREATER
        // A second line of defence where the event carries the composition state.
        if (e.IsComposing) return;
#endif

        if (State.Parameters.Multiline is true && (e.CtrlKey || e.MetaKey) is false) return;

        await MessageBox.AnswerAsync(State.Affirmative);
    }
}
