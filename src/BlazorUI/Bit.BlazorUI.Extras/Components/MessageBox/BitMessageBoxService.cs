namespace Bit.BlazorUI;

/// <summary>
/// A wrapper service around the <see cref="BitModalService"/> to enhance showing message boxes.
/// </summary>
/// <remarks>
/// Every Show method hands back the <see cref="BitMessageBoxResult"/> the message box was answered with,
/// and the task it returns completes when the box closes rather than when it opens - which is what turns
/// a message box into a question that can be awaited:
/// <code>
/// if (await messageBoxService.Confirm("Delete", "Delete this file?")) { ... }
/// </code>
/// A message box only appears if the <see cref="BitModalContainer"/> of the service is mounted in the layout:
/// a modal shown while no container is mounted is silently not rendered. Use
/// <see cref="BitModalServiceBase{TReference, TParameters}.IsContainerAvailable"/> to check for one before showing.
/// A showing that never renders answers with <see cref="BitMessageBoxResult.None"/> rather than waiting
/// forever on a box that is not on the screen.
/// <br/>
/// A message box that asks something - a Confirm, or any set of buttons beyond a lone Ok - is shown as an
/// <c>alertdialog</c>, the role the WAI-ARIA pattern gives a dialog that interrupts to get a response, and so is
/// one whose <see cref="BitMessageBoxParameters.Color"/> is Warning, SevereWarning or Error. A notice that is only
/// acknowledged stays a plain <c>dialog</c>, and so does a <see cref="Prompt(BitMessageBoxPromptParameters)"/>, which is a
/// form. <see cref="BitModalParameters.IsAlert"/> on <see cref="BitMessageBoxParameters.Modal"/> has the last word.
/// </remarks>
public class BitMessageBoxService(BitModalService modalService)
{
    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> with a title and a body inside a <see cref="BitModal"/>.
    /// </summary>
    public Task<BitMessageBoxResult> Show(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body });
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> with a title, a body and a set of buttons.
    /// </summary>
    public Task<BitMessageBoxResult> Show(string title, string body, BitMessageBoxButtons buttons)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Buttons = buttons });
    }

    /// <summary>
    /// Shows an informational <see cref="BitMessageBox"/>, which carries the Info glyph.
    /// </summary>
    public Task<BitMessageBoxResult> ShowInfo(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Color = BitColor.Info });
    }

    /// <summary>
    /// Shows a success <see cref="BitMessageBox"/>, which carries the Completed glyph.
    /// </summary>
    public Task<BitMessageBoxResult> ShowSuccess(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Color = BitColor.Success });
    }

    /// <summary>
    /// Shows a warning <see cref="BitMessageBox"/>, which carries the Warning glyph and is announced as an alert.
    /// </summary>
    public Task<BitMessageBoxResult> ShowWarning(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Color = BitColor.Warning });
    }

    /// <summary>
    /// Shows a severe warning <see cref="BitMessageBox"/>, which carries the WarningSolid glyph and is
    /// announced as an alert.
    /// </summary>
    public Task<BitMessageBoxResult> ShowSevereWarning(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Color = BitColor.SevereWarning });
    }

    /// <summary>
    /// Shows an error <see cref="BitMessageBox"/>, which carries the ErrorBadge glyph and is announced as an alert.
    /// </summary>
    public Task<BitMessageBoxResult> ShowError(string title, string body)
    {
        return Show(new BitMessageBoxParameters { Title = title, Body = body, Color = BitColor.Error });
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a confirmation, and reports whether it was given.
    /// </summary>
    /// <remarks>
    /// Only the affirmative buttons - Ok and Yes - answer <c>true</c>. A refusal, a dismissal through the
    /// close button, the Escape key or the overlay, and a message box that never rendered all answer
    /// <c>false</c>, so the destructive branch is never the one taken by default.
    /// </remarks>
    public Task<bool> Confirm(string title, string body, BitMessageBoxButtons buttons = BitMessageBoxButtons.OkCancel)
    {
        return Confirm(new BitMessageBoxParameters { Title = title, Body = body, Buttons = buttons });
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a confirmation, and reports whether it was given.
    /// </summary>
    public Task<bool> Confirm(BitMessageBoxParameters parameters) => Confirm(parameters, CancellationToken.None);

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a confirmation, and reports whether it was given - or
    /// <c>false</c> once the <paramref name="cancellationToken"/> takes the question back.
    /// </summary>
    /// <remarks>
    /// A cancellation closes the message box the way the page closing its modal does, so it is a refusal like any
    /// other dismissal rather than an exception: the destructive branch is still never the one taken by default.
    /// </remarks>
    public async Task<bool> Confirm(BitMessageBoxParameters parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var result = await Show(parameters, parameters.Id ?? NewId(), BitMessageBoxButtons.OkCancel, isQuestion: true, prompt: null, cancellationToken);

        return result is BitMessageBoxResult.Ok or BitMessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a line of text, and returns what was typed - or <c>null</c> when
    /// the box is dismissed rather than answered.
    /// </summary>
    public Task<string?> Prompt(string title, string body, string? value = null)
    {
        return Prompt(new BitMessageBoxPromptParameters { Title = title, Body = body, Value = value });
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a line of text, and returns what was typed - or <c>null</c> when
    /// the box is dismissed rather than answered.
    /// </summary>
    public Task<string?> Prompt(BitMessageBoxPromptParameters parameters) => Prompt(parameters, CancellationToken.None);

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> asking for a line of text, and returns what was typed - or <c>null</c> when
    /// the box is dismissed rather than answered, or once the <paramref name="cancellationToken"/> takes the question back.
    /// </summary>
    /// <remarks>
    /// The box has an Ok and a Cancel button unless <see cref="BitMessageBoxParameters.Buttons"/> says otherwise. Only the
    /// affirmative answer (Ok, or Yes) returns the value - an empty string for an empty field - and only once
    /// <see cref="BitMessageBoxPromptParameters.Required"/> and <see cref="BitMessageBoxPromptParameters.Validator"/> have
    /// accepted it - then <see cref="BitMessageBoxPromptParameters.AsyncValidator"/>, if there is one: a refused value stays in
    /// the field with the reason under it, and the box stays open. Enter in the field answers the way the affirmative button does.
    /// <br/>
    /// A validator that throws closes the box, and the exception is rethrown out of this call - where the code that wrote
    /// the validator is waiting - rather than out of the click or the key that was being handled. A check still running when
    /// the box closes, however it closes, has its token cancelled.
    /// </remarks>
    public async Task<string?> Prompt(BitMessageBoxPromptParameters parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var buttons = parameters.Buttons ?? BitMessageBoxButtons.OkCancel;
        var affirmative = buttons is BitMessageBoxButtons.YesNo or BitMessageBoxButtons.YesNoCancel
                            ? BitMessageBoxResult.Yes
                            : BitMessageBoxResult.Ok;

        var id = parameters.Id ?? NewId();
        var prompt = new BitMessageBoxPromptState(parameters, id, affirmative);

        BitMessageBoxResult result;

        try
        {
            result = await Show(parameters, id, BitMessageBoxButtons.OkCancel, isQuestion: false, prompt, cancellationToken);
        }
        finally
        {
            prompt.Close();
        }

        return result == affirmative ? prompt.AcceptedValue ?? string.Empty : null;
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    public Task<BitMessageBoxResult> Show(BitMessageBoxParameters parameters) => Show(parameters, CancellationToken.None);

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitModal"/>, and closes it again if the
    /// <paramref name="cancellationToken"/> is cancelled before it is answered.
    /// </summary>
    /// <remarks>
    /// This is how a message box is taken back off the screen by the page rather than by the user - a time limit
    /// (<c>new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token</c>), a navigation, the state it asked about
    /// changing elsewhere. A cancelled showing answers with <see cref="BitMessageBoxResult.None"/>, the answer of a
    /// message box the page closed, rather than throwing; one cancelled before it was shown is never shown.
    /// </remarks>
    public Task<BitMessageBoxResult> Show(BitMessageBoxParameters parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return Show(parameters, parameters.Id ?? NewId(), null, isQuestion: false, prompt: null, cancellationToken);
    }

    // The id is what the ids of the title and the body are derived from, and those are what the modal points its
    // accessible name and description at - so one is made up front rather than left to the message box, whose own
    // generated id nothing outside it can read.
    private static string NewId() => $"bit-msb-{Guid.NewGuid():n}";

    private async Task<BitMessageBoxResult> Show(BitMessageBoxParameters parameters,
                                                 string id,
                                                 BitMessageBoxButtons? fallbackButtons,
                                                 bool isQuestion,
                                                 BitMessageBoxPromptState? prompt,
                                                 CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return BitMessageBoxResult.None;

        var buttons = parameters.Buttons ?? fallbackButtons ?? BitMessageBoxButtons.Ok;

        // A prompt is a form rather than an interruption, so it stays a plain dialog whatever its buttons and its color are.
        var isAlert = prompt is null &&
                      (isQuestion ||
                       buttons is not (BitMessageBoxButtons.Ok or BitMessageBoxButtons.None) ||
                       parameters.Color is BitColor.Warning or BitColor.SevereWarning or BitColor.Error);

        // The parameters are built from the modal reference the service hands back, so the callbacks close
        // this very modal without a window where the reference isn't assigned yet.
        var modalRef = await modalService.Show<BitMessageBox>(
            mr => BuildParameters(parameters, id, mr, fallbackButtons, prompt),
            BuildModalParameters(parameters, id, isAlert, prompt),
            parameters.Persistent ?? false);

        // The token only signals: the close itself is awaited below, so a close handler that fails (Close rethrows
        // their failures as an AggregateException) reaches the caller rather than being dropped unobserved.
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => cancelled.TrySetResult());

        var answer = WaitForAnswer(modalRef);

        // A prompt whose validator throws is closed too, and what it threw is handed to the caller rather than left to
        // escape into the event that was being handled.
        var failed = prompt?.Failed ?? cancelled.Task;

        var ended = await Task.WhenAny(answer, cancelled.Task, failed);

        if (ended != answer)
        {
            // Close rather than Dismiss: this is the page taking the box back, which a CanClose guard has no say over.
            await modalRef.Close();
        }

        if (ended == prompt?.Failed)
        {
            await ended;
        }

        return await answer;
    }

    private static async Task<BitMessageBoxResult> WaitForAnswer(BitModalReference modalRef)
    {
        // A modal shown with no container mounted is never rendered and never closed, so its result would
        // never arrive: whoever asked the question is let go with no answer instead of left waiting.
        if (await modalRef.Rendered is false && modalRef.IsClosed is false) return BitMessageBoxResult.None;

        return (await modalRef.Result) as BitMessageBoxResult? ?? BitMessageBoxResult.None;
    }



    private static BitModalParameters BuildModalParameters(BitMessageBoxParameters parameters, string id, bool isAlert, BitMessageBoxPromptState? prompt)
    {
        // A HeaderTemplate takes the title off the message box, so an aria-labelledby pointing at it would
        // name the dialog after an element that was never rendered - the words themselves stand in for it.
        var hasTitle = parameters.Title.HasValue() && parameters.HeaderTemplate is null;
        var hasBody = parameters.Body.HasValue() || parameters.BodyTemplate is not null;

        // The body of a prompt holds its field too, so the dialog is described by the question alone - and not even by
        // that where the question is what names the field, which would have it read out twice on opening.
        var describedBy = prompt is null
                            ? (hasBody ? $"{id}-bdy" : null)
                            : (hasBody && prompt.QuestionNamesTheField is false ? prompt.MessageId : null);

        var defaults = new BitModalParameters
        {
            Dir = parameters.Dir,

            // A dialog is named by the heading it holds where it has one, and by its own words where it does
            // not: an aria-labelledby pointing at a title that was never rendered leaves it nameless.
            TitleAriaId = hasTitle ? $"{id}-ttl" : null,
            AriaLabel = hasTitle ? null : (parameters.Title ?? parameters.Body),

            // The body is the prompt, which is what the pattern asks an alert dialog to be described by.
            SubtitleAriaId = describedBy,

            // The alertdialog role is for a dialog that interrupts to get a response - a question, which is what
            // the WAI-ARIA pattern's own example (a confirmation) is - and for the colors that carry urgency.
            // A notice that is only acknowledged is left to the Modal's own decision.
            IsAlert = isAlert ? true : null,
        };

        // Precedence to what the caller asked for: these are only the values the service works out on its own.
        return BitModalParameters.Merge(parameters.Modal, defaults)!;
    }

    private Dictionary<string, object> BuildParameters(BitMessageBoxParameters parameters,
                                                       string id,
                                                       BitModalReference modalRef,
                                                       BitMessageBoxButtons? fallbackButtons,
                                                       BitMessageBoxPromptState? prompt)
    {
        var result = new Dictionary<string, object>
        {
            { nameof(BitMessageBox.Id), id },

            // Every message box the service shows is inside a dialog that has just taken over the screen, so
            // the focus goes into it - which is what both the keyboard and the screen reader need, and what
            // makes Enter answer the box rather than re-press whatever opened it.
            { nameof(BitMessageBox.AutoFocus), parameters.AutoFocus ?? true },

            // The answer travels on the modal's result, so awaiting the modal is awaiting the answer. OnClose
            // is wired too, for the close button of a message box given a FooterTemplate of its own.
            { nameof(BitMessageBox.OnResult), EventCallback.Factory.Create<BitMessageBoxResult>(this, r => modalRef.CloseWith(r)) },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, modalRef.Close) }
        };

        // An EventCallback is a struct, so "not set" is a callback with no delegate behind it rather than
        // a null the Add below would leave out.
        if (parameters.OnBeforeResult.HasDelegate)
        {
            result[nameof(BitMessageBox.OnBeforeResult)] = parameters.OnBeforeResult;
        }

        Add(nameof(BitMessageBox.AutoLoading), parameters.AutoLoading);
        Add(nameof(BitMessageBox.Body), parameters.Body);
        Add(nameof(BitMessageBox.BodyTemplate), parameters.BodyTemplate);
        Add(nameof(BitMessageBox.ButtonColor), parameters.ButtonColor);
        Add(nameof(BitMessageBox.Buttons), parameters.Buttons ?? fallbackButtons);
        Add(nameof(BitMessageBox.CancelText), parameters.CancelText);
        Add(nameof(BitMessageBox.Classes), parameters.Classes);
        Add(nameof(BitMessageBox.CloseButtonTitle), parameters.CloseButtonTitle);
        Add(nameof(BitMessageBox.CloseIcon), parameters.CloseIcon);
        Add(nameof(BitMessageBox.CloseIconName), parameters.CloseIconName);
        Add(nameof(BitMessageBox.Color), parameters.Color);
        Add(nameof(BitMessageBox.DefaultButton), parameters.DefaultButton);
        Add(nameof(BitMessageBox.Dir), parameters.Dir);
        Add(nameof(BitMessageBox.FooterTemplate), parameters.FooterTemplate);
        Add(nameof(BitMessageBox.HeaderTemplate), parameters.HeaderTemplate);
        Add(nameof(BitMessageBox.HideIcon), parameters.HideIcon);
        Add(nameof(BitMessageBox.Icon), parameters.Icon);
        Add(nameof(BitMessageBox.IconAriaLabel), parameters.IconAriaLabel);
        Add(nameof(BitMessageBox.IconName), parameters.IconName);
        Add(nameof(BitMessageBox.IconTemplate), parameters.IconTemplate);
        Add(nameof(BitMessageBox.NoText), parameters.NoText);
        Add(nameof(BitMessageBox.OkText), parameters.OkText);
        Add(nameof(BitMessageBox.PrimaryButtonColor), parameters.PrimaryButtonColor);
        Add(nameof(BitMessageBox.Reversed), parameters.Reversed);
        Add(nameof(BitMessageBox.ShowCloseButton), parameters.ShowCloseButton);
        Add(nameof(BitMessageBox.Size), parameters.Size);
        Add(nameof(BitMessageBox.Styles), parameters.Styles);
        Add(nameof(BitMessageBox.Title), parameters.Title);
        Add(nameof(BitMessageBox.TitleElement), parameters.TitleElement);
        Add(nameof(BitMessageBox.YesText), parameters.YesText);

        if (prompt is not null)
        {
            AddPrompt(prompt);
        }

        return result;

        // A parameter that was not set is left out entirely rather than handed over as null, so the default
        // the component declares is the one in force.
        void Add(string name, object? value)
        {
            if (value is null) return;

            result[name] = value;
        }

        // A prompt's body is its question and the field under it, and the field - not a button - is where the focus goes.
        // The value is checked before an affirmative answer gets through, and only then is the caller's own guard asked.
        void AddPrompt(BitMessageBoxPromptState state)
        {
            result.Remove(nameof(BitMessageBox.Body));
            result[nameof(BitMessageBox.AutoFocus)] = false;
            result[nameof(BitMessageBox.BodyTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitMessageBoxPrompt>(0);
                builder.AddComponentParameter(1, nameof(BitMessageBoxPrompt.State), state);
                builder.CloseComponent();
            });
            result[nameof(BitMessageBox.OnBeforeResult)] = EventCallback.Factory.Create<BitMessageBoxBeforeResultArgs>(this, async args =>
            {
                if (args.Result == state.Affirmative && await state.TryAcceptAsync(args.CancellationToken) is false)
                {
                    args.Cancel = true;
                    return;
                }

                await parameters.OnBeforeResult.InvokeAsync(args);
            });
        }
    }
}
