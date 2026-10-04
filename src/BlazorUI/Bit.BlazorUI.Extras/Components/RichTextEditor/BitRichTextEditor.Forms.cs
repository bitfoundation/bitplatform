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

    private FieldIdentifier _fieldIdentifier;
    private bool _hasField;

    private void EnsureField()
    {
        if (ValueExpression is null)
        {
            _hasField = false;
            return;
        }

        // Rebuild every time: FieldIdentifier.Create(ValueExpression) can resolve to a different
        // model instance even when the same expression delegate is reused (e.g. the bound model
        // was swapped), so caching on the expression instance alone can notify a stale field.
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
            if (CascadedEditContext is null || ValueExpression is null) return false;
            EnsureField();
            return _hasField && CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();
        }
    }

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
