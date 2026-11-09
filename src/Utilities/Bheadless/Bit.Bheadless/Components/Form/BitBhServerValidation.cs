using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bheadless;

/// <summary>
/// Shows errors a server returned (an API's validation problem details, a uniqueness check) on the fields of an
/// <c>EditForm</c>, the way client validation shows its own - Radix Form's <c>serverInvalid</c>, Base UI Form's
/// <c>errors</c>. Each error is cleared when its field is edited, and all of them when the form is validated again.
///
/// <para>The messages go into the form's <see cref="EditContext"/>, so every control bound with <c>@bind-Value</c> picks
/// them up as usual: <c>aria-invalid</c>, <c>data-invalid</c>, and the field's <see cref="BitBhErrorMessage"/>.</para>
///
/// <code>
/// &lt;EditForm Model="model" OnValidSubmit="SaveAsync"&gt;
///     &lt;DataAnnotationsValidator /&gt;
///     &lt;BitBhServerValidation Errors="serverErrors" /&gt;
///     &lt;BitBhField&gt;&lt;BitBhLabel&gt;User name&lt;/BitBhLabel&gt;&lt;BitBhInput @bind-Value="model.UserName" /&gt;&lt;BitBhErrorMessage /&gt;&lt;/BitBhField&gt;
/// &lt;/EditForm&gt;
///
/// @code {
///     private Dictionary&lt;string, string[]&gt;? serverErrors;
///     private async Task SaveAsync()
///     {
///         var response = await Http.PostAsJsonAsync("api/users", model);
///         if (!response.IsSuccessStatusCode)
///             serverErrors = (await response.Content.ReadFromJsonAsync&lt;ValidationProblemDetails&gt;())?.Errors;
///     }
/// }
/// </code>
/// </summary>
public sealed class BitBhServerValidation : ComponentBase, IDisposable
{
    private EditContext? _subscribed;
    private ValidationMessageStore? _store;
    private object? _shownErrors;
    private object? _shownFieldErrors;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>
    /// Errors by the name of a property of the form's model (an empty name is an error of the whole form). Keys are matched
    /// ignoring case, the way ASP.NET Core writes them in a <c>ValidationProblemDetails</c>.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }

    /// <summary>Errors by field, for properties of nested objects: <c>new FieldIdentifier(model.Address, "City")</c>.</summary>
    [Parameter]
    public IReadOnlyDictionary<FieldIdentifier, string[]>? FieldErrors { get; set; }

    /// <summary>Clears a field's errors when it is edited. Default: true.</summary>
    [Parameter]
    public bool ClearOnChange { get; set; } = true;

    /// <summary>Fires when the errors were cleared by an edit or a new validation.</summary>
    [Parameter]
    public EventCallback OnCleared { get; set; }

    protected override void OnParametersSet()
    {
        if (EditContext is null)
            throw new InvalidOperationException($"{nameof(BitBhServerValidation)} goes inside an EditForm (it needs a cascading EditContext).");

        if (!ReferenceEquals(EditContext, _subscribed))
        {
            Unsubscribe();
            _subscribed = EditContext;
            _store = new ValidationMessageStore(EditContext);
            EditContext.OnFieldChanged += HandleFieldChanged;
            EditContext.OnValidationRequested += HandleValidationRequested;
            _shownErrors = _shownFieldErrors = null;
        }

        // Only new errors replace what is shown: a re-render with the same dictionaries keeps the fields that were
        // cleared by an edit cleared.
        if (ReferenceEquals(Errors, _shownErrors) && ReferenceEquals(FieldErrors, _shownFieldErrors)) return;
        _shownErrors = Errors;
        _shownFieldErrors = FieldErrors;
        Show();
    }

    private void Show()
    {
        if (_store is null || EditContext is null) return;
        _store.Clear();
        if (Errors is not null)
        {
            foreach (var (name, messages) in Errors)
            {
                var property = ResolveName(EditContext.Model, name);
                _store.Add(new FieldIdentifier(EditContext.Model, property), messages);
            }
        }
        if (FieldErrors is not null)
        {
            foreach (var (field, messages) in FieldErrors) _store.Add(field, messages);
        }
        EditContext.NotifyValidationStateChanged();
    }

    // The property as the model spells it ("userName" from a camel-cased JSON payload is "UserName"); a dotted path
    // ("Address.City") keeps its last segment on the model only when that is a property of it.
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "A property the form binds is referenced by its @bind expression and kept; one trimmed away has no control to show an error on, and the name is used as given.")]
    private static string ResolveName(object model, string name)
    {
        if (name.Length == 0) return name;
        foreach (var property in model.GetType().GetProperties())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Name;
        return name;
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (!ClearOnChange || _store is null || EditContext is null) return;
        if (!EditContext.GetValidationMessages(e.FieldIdentifier).Any()) return;
        _store.Clear(e.FieldIdentifier);
        EditContext.NotifyValidationStateChanged();
        _ = InvokeAsync(() => OnCleared.InvokeAsync());
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // A new submit validates afresh: the server is asked again.
        _store?.Clear();
        _ = InvokeAsync(() => OnCleared.InvokeAsync());
    }

    private void Unsubscribe()
    {
        if (_subscribed is null) return;
        _subscribed.OnFieldChanged -= HandleFieldChanged;
        _subscribed.OnValidationRequested -= HandleValidationRequested;
        _store?.Clear();
        _subscribed = null;
    }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();
}
