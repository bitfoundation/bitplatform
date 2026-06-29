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

    /// <summary>Notifies the cascaded EditContext that the bound field changed.</summary>
    private void NotifyEditContextChanged()
    {
        if (CascadedEditContext is null) return;
        EnsureField();
        if (_hasField) CascadedEditContext.NotifyFieldChanged(_fieldIdentifier);
    }
}
