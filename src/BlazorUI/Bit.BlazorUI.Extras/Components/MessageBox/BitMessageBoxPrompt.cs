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

    // Set by the first refused answer: from then on every edit re-checks the value.
    private bool _refused;

    /// <summary>
    /// Raised when <see cref="Error"/> changes outside the field's own events, with whether the field should take the focus.
    /// </summary>
    public event Action<bool>? Changed;

    /// <summary>
    /// Checks the value before the box is answered with it, and moves the focus back onto the field when it is refused -
    /// which is where the error that explains why is read out.
    /// </summary>
    public async Task<bool> TryAcceptAsync()
    {
        var value = Value;

        Error = Validate();

        if (Error is null && Parameters.AsyncValidator is not null)
        {
            Validating = true;
            Changed?.Invoke(false);

            try
            {
                Error = await Parameters.AsyncValidator(value);
            }
            catch
            {
                // A validator that throws does not leave the field busy behind the failure it reports.
                Validating = false;
                Changed?.Invoke(false);
                throw;
            }

            Validating = false;

            // A refusal of a value that was edited while the check ran is about a value that is no longer there: the
            // answer is still refused, but what the field shows is what the synchronous checks say about its current value.
            if (Error is not null && Value != value)
            {
                _refused = true;
                Error = Validate();
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

        var error = Error;

        Error = Validate();

        return error != Error;
    }

    private string? Validate()
    {
        if (Parameters.Required is true && string.IsNullOrWhiteSpace(Value))
        {
            return Parameters.RequiredMessage ?? "A value is required.";
        }

        return Parameters.Validator?.Invoke(Value);
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



    public BitMessageBoxPrompt()
    {
        // Created once and with no receiver, so an edit re-renders the field alone - the prompt only re-renders when
        // the error under the field changes. A render of the prompt for every keystroke hands the field back a value
        // the user has already typed past, which costs characters when typing fast.
        _valueChanged = new EventCallback<string?>(null, (Action<string?>)HandleValueChanged);
    }



    [Parameter, EditorRequired] public BitMessageBoxPromptState State { get; set; } = default!;

    [CascadingParameter] public BitMessageBox? MessageBox { get; set; }



    public void Dispose() => State.Changed -= HandleStateChanged;



    protected override void OnInitialized() => State.Changed += HandleStateChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending is false || _field is null || State.Validating) return;

        _focusPending = false;

        await _field.FocusAsync();
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
        if (parameters.Size.HasValue) builder.AddComponentParameter(26, nameof(BitTextField.Size), parameters.Size);
        builder.AddComponentParameter(27, nameof(BitTextField.OnKeyDown), EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
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
    // and a key pressed to compose a character with an IME is not an answer at all.
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Enter" || MessageBox is null) return;

#if NET9_0_OR_GREATER
        // The event only carries the composition state from .NET 9 on.
        if (e.IsComposing) return;
#endif

        if (State.Parameters.Multiline is true && (e.CtrlKey || e.MetaKey) is false) return;

        await MessageBox.AnswerAsync(State.Affirmative);
    }
}
