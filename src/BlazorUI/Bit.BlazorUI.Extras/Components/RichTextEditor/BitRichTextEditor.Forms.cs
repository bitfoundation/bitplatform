using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

// EditForm / EditContext integration, enabling validation for the bound model field.
public partial class BitRichTextEditor
{
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// Identifies the bound model field, enabling <c>EditForm</c> validation. Set automatically
    /// when using <c>@bind-Value</c> on a model property.
    /// </summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>
    /// Shows the messages the cascading EditContext holds for the bound field - the ones of its data annotations or
    /// those of any other validator of the form - under the editor, where <see cref="ErrorMessage"/> goes, so an
    /// <c>EditForm</c> needs no <c>ValidationMessage</c> component beside it. The editing surface (and the source view
    /// while it is open) references them through its aria-describedby attribute, so they are read out whenever it
    /// takes the focus.
    /// </summary>
    /// <remarks>
    /// An <see cref="ErrorMessage"/> of the app's own wins the place while it is set. Unlike an ErrorMessage, the
    /// messages are not announced through the status region of the editor: a submit validates every field of the form
    /// at once, and a message from each of them read out in a row would bury the form under its own errors - the field
    /// that takes the focus says what is wrong with it. An editor bound to nothing (no <see cref="ValueExpression"/>)
    /// shows none.
    /// </remarks>
    [Parameter] public bool ShowValidationMessage { get; set; }

    private FieldIdentifier _fieldIdentifier;
    private bool _hasField;

    private void EnsureField()
    {
        if (ValueExpression is null)
        {
            _hasField = false;
            return;
        }

        // Rebuilt on every parameter set and before every change notification, never cached on the
        // expression instance: FieldIdentifier.Create(ValueExpression) can resolve to a different
        // model instance even when the same expression is reused (e.g. the bound model was swapped),
        // so caching on the expression alone can notify a stale field.
        _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
        _hasField = true;
    }

    private EditContext? _trackedEditContext;

    /// <summary>
    /// Whether the content is invalid - marked so by <see cref="Invalid"/> or <see cref="ErrorMessage"/>, or failing
    /// the validation of its bound field: the frame takes the error color and the editing surface reports
    /// <c>aria-invalid</c>, the way every other input of an EditForm does.
    /// </summary>
    private bool IsInvalid
    {
        get
        {
            if (Invalid || ErrorMessage.HasValue()) return true;
            // Read several times per render, so it reads the field OnParametersSet last resolved rather than
            // walking the expression again on each read.
            if (CascadedEditContext is null) return false;
            return _hasField && CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();
        }
    }

    /// <summary>
    /// Every message the cascading EditContext holds for the bound field, in the order the validators added them and
    /// joined the way the inputs of the library join theirs, or null while it holds none. The editor re-renders on every
    /// change of the validation state (see <see cref="HandleValidationStateChanged"/>), so reading it at render time is
    /// enough to follow it.
    /// </summary>
    private string? FieldValidationMessage
    {
        get
        {
            if (CascadedEditContext is null || _hasField is false) return null;

            var message = string.Join(' ', CascadedEditContext.GetValidationMessages(_fieldIdentifier).Where(m => m.HasValue()));
            return message.HasValue() ? message : null;
        }
    }

    // The messages of the EditContext the error line shows (ShowValidationMessage), which only take it while the app
    // has no message of its own there. They mark nothing themselves: IsInvalid already reads the EditContext.
    private string? ShownValidationMessage => ShowValidationMessage && ErrorMessage.HasNoValue() ? FieldValidationMessage : null;

    // Whether the error line under the editor is rendered, for the app's own message or for the EditContext's.
    private bool HasErrorLine => ErrorMessage.HasValue() || ShownValidationMessage is not null;

    // Follows the EditContext the editor sits in, so a submit that fails (or a later edit that passes) re-renders the
    // invalid state without waiting for the editor's own next render.
    private void TrackEditContext()
    {
        if (ReferenceEquals(_trackedEditContext, CascadedEditContext)) return;

        UntrackEditContext();

        _trackedEditContext = CascadedEditContext;
        if (_trackedEditContext is not null)
        {
            _trackedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    private void UntrackEditContext()
    {
        if (_trackedEditContext is null) return;
        _trackedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        _trackedEditContext = null;
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        ClassBuilder.Reset();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Notifies the cascaded EditContext that the bound field changed.</summary>
    private void NotifyEditContextChanged()
    {
        if (CascadedEditContext is null) return;
        EnsureField();
        if (_hasField) CascadedEditContext.NotifyFieldChanged(_fieldIdentifier);
    }
}
