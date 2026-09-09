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
    public async Task<bool> Confirm(BitMessageBoxParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);


        var result = await Show(parameters, BitMessageBoxButtons.OkCancel);

        return result is BitMessageBoxResult.Ok or BitMessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    public Task<BitMessageBoxResult> Show(BitMessageBoxParameters parameters) => Show(parameters, null);

    private async Task<BitMessageBoxResult> Show(BitMessageBoxParameters parameters, BitMessageBoxButtons? fallbackButtons)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // The id is what the ids of the title and the body are derived from, and those are what the modal
        // points its accessible name and description at - so one is made up front rather than left to the
        // message box, whose own generated id nothing outside it can read.
        var id = parameters.Id ?? $"bit-msb-{Guid.NewGuid():n}";

        // The parameters are built from the modal reference the service hands back, so the callbacks close
        // this very modal without a window where the reference isn't assigned yet.
        var modalRef = await modalService.Show<BitMessageBox>(
            mr => BuildParameters(parameters, id, mr, fallbackButtons),
            BuildModalParameters(parameters, id),
            parameters.Persistent ?? false);

        // A modal shown with no container mounted is never rendered and never closed, so its result would
        // never arrive: whoever asked the question is let go with no answer instead of left waiting.
        if (await modalRef.Rendered is false && modalRef.IsClosed is false) return BitMessageBoxResult.None;

        return (await modalRef.Result) as BitMessageBoxResult? ?? BitMessageBoxResult.None;
    }



    private static BitModalParameters BuildModalParameters(BitMessageBoxParameters parameters, string id)
    {
        // A HeaderTemplate takes the title off the message box, so an aria-labelledby pointing at it would
        // name the dialog after an element that was never rendered - the words themselves stand in for it.
        var hasTitle = parameters.Title.HasValue() && parameters.HeaderTemplate is null;
        var hasBody = parameters.Body.HasValue() || parameters.BodyTemplate is not null;

        var defaults = new BitModalParameters
        {
            Dir = parameters.Dir,

            // A dialog is named by the heading it holds where it has one, and by its own words where it does
            // not: an aria-labelledby pointing at a title that was never rendered leaves it nameless.
            TitleAriaId = hasTitle ? $"{id}-ttl" : null,
            AriaLabel = hasTitle ? null : (parameters.Title ?? parameters.Body),

            // The body is the prompt, which is what the pattern asks an alert dialog to be described by.
            SubtitleAriaId = hasBody ? $"{id}-bdy" : null,

            // The colors that carry urgency are the ones the alertdialog role exists for; everything else
            // is left to the Modal's own decision.
            IsAlert = parameters.Color is BitColor.Warning or BitColor.SevereWarning or BitColor.Error ? true : null,
        };

        // Precedence to what the caller asked for: these are only the values the service works out on its own.
        return BitModalParameters.Merge(parameters.Modal, defaults)!;
    }

    private Dictionary<string, object> BuildParameters(BitMessageBoxParameters parameters, string id, BitModalReference modalRef, BitMessageBoxButtons? fallbackButtons)
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
        Add(nameof(BitMessageBox.YesText), parameters.YesText);

        return result;

        // A parameter that was not set is left out entirely rather than handed over as null, so the default
        // the component declares is the one in force.
        void Add(string name, object? value)
        {
            if (value is null) return;

            result[name] = value;
        }
    }
}
