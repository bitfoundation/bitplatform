using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// Moves the focus to the first invalid field of the <c>EditForm</c> it sits in when a submit of that form fails, so
/// a keyboard or screen reader user lands on what has to be fixed instead of staying on the submit button.
/// </summary>
/// <remarks>
/// Put it anywhere inside an <c>EditForm</c>, next to its validator. It renders nothing visible - a hidden element it
/// finds its form by - and changes nothing about how the form validates. After a validation of the whole form, which
/// every submit runs, leaves a field invalid, the focus goes to the first element of the form, in document order, that
/// reports <c>aria-invalid="true"</c>: every bit BlazorUI input does when its field fails, and so do the inputs of the
/// framework. An element that cannot take the focus itself hands it to the first one inside it that can - a
/// ChoiceGroup to its checked radio, or its first one when none is checked, a Rating to the item the Tab key lands on.
/// <br />
/// Only a validation of the whole form - the one a submit runs, or one the app asks the <c>EditContext</c> for - moves
/// the focus, and only once it is over: a validator that finishes asynchronously is waited for. A field validated as it
/// is edited never moves it, and neither does a validation still running when the user starts editing a field again.
/// The focus is moved by the browser, so it takes an interactive render mode: a statically rendered form leaves it
/// where it is.
/// <br />
/// Paired with the <c>ShowValidationMessage</c> parameter of the inputs, the field the focus lands on reads out what
/// is wrong with it as it takes the focus.
/// </remarks>
public class BitFocusOnInvalidSubmit : ComponentBase, IDisposable
{
    private EditContext? _editContext;
    private ElementReference _marker;
    private bool _validationRequested;
    private bool _focusPending;
    private bool _disposed;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// The EditContext of the form the component sits in, cascaded by the <c>EditForm</c>. The component requires one: it
    /// throws an <see cref="InvalidOperationException"/> when rendered outside a form.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }



    protected override void OnParametersSet()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException($"{nameof(BitFocusOnInvalidSubmit)} requires a cascading parameter of type {nameof(EditContext)}. For example, you can use {nameof(BitFocusOnInvalidSubmit)} inside an EditForm.");
        }

        if (ReferenceEquals(EditContext, _editContext)) return;

        // A form given a new model hands down a new EditContext, and the validations that matter are that one's.
        Untrack();

        _editContext = EditContext;
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnValidationStateChanged += HandleValidationStateChanged;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Hidden, so it takes no place in the layout of the form - a grid or a flex row of fields included - and only
        // tells the script which form to look in.
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "hidden", true);
        builder.AddElementReferenceCapture(2, element => _marker = element);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending is false) return;

        _focusPending = false;

        // A validation that passed leaves the focus where the user put it.
        if (_editContext is null || _editContext.GetValidationMessages().Any() is false) return;

        try
        {
            await _js.BitUtilsFocusFirstInvalid(_marker);
        }
        catch (JSDisconnectedException) { } // the circuit is gone (e.g. the user navigated away), there is nothing to focus
        catch (TaskCanceledException) { } // the same, while the call was on its way
    }



    // The request is raised before the validators have run - they answer the same event, some of them after this
    // handler, and some only register work that finishes later - so it is only remembered here, and acted on once the
    // validation is over.
    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        _validationRequested = true;

        CheckOnceTheValidationIsOver();
    }

    // Every validator reports what it found through this event, and a validation of the whole form that waits for
    // asynchronous validators raises it once more when it is over, with nothing pending any longer.
    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (_validationRequested is false) return;

        CheckOnceTheValidationIsOver();
    }

    // A user editing a field while a validation is still running has moved on from the submit; the focus is not taken
    // away from the field being typed in when that validation ends.
    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        _validationRequested = false;
    }

    // Looks after the current work, so a validation that runs to its end without waiting - every validator answering
    // the request right away - is over by then, and the fields have rendered what it found. A validation still waiting
    // for a validator is left to the change of state its end raises. The render asked for here is what the check is
    // made after.
    private void CheckOnceTheValidationIsOver()
    {
        _ = InvokeAsync(async () =>
        {
            await Task.Yield();

            if (_disposed || _validationRequested is false || _editContext is null) return;

#if NET11_0_OR_GREATER
            // Validators that finish asynchronously (ValidationRequestedEventArgs.AddAsyncValidator) came with .NET 11;
            // before it, a validation is over once its request has been answered.
            if (_editContext.IsValidationPending()) return;
#endif

            _validationRequested = false;
            _focusPending = true;
            StateHasChanged();
        });
    }

    private void Untrack()
    {
        if (_editContext is null) return;

        _editContext.OnValidationRequested -= HandleValidationRequested;
        _editContext.OnValidationStateChanged -= HandleValidationStateChanged;
        _editContext.OnFieldChanged -= HandleFieldChanged;
        _editContext = null;
        _validationRequested = false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        _disposed = true;

        if (disposing)
        {
            Untrack();
        }
    }
}
