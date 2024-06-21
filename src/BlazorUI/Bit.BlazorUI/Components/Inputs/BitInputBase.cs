// Inspired by
// 1- Blazor InputBase class: https://github.com/dotnet/aspnetcore/blob/release/8.0/src/Components/Web/src/Forms/InputBase.cs
// 2- Fluent Blazor base input class: https://github.com/microsoft/fluentui-blazor/blob/dev/src/Core/Components/Base/FluentInputBase.cs

using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// A base class for bit BlazorUI input components. This base class automatically
/// integrates with an <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>, which must be supplied
/// as a cascading parameter.
/// </summary>
public abstract class BitInputBase<TValue> : BitComponentBase, IDisposable
{
    protected bool IsDisposed;
    protected bool ValueHasBeenSet;

    private TValue? value;
    private bool? valueInvalid;

    private bool _parsingFailed;
    private bool _isUnderlyingTypeNullable;
    private bool _hasInitializedParameters;
    private bool _previousParsingAttemptFailed;
    private string? _incomingValueBeforeParsing;
    private readonly BitDebouncer _debouncer = new();
    private readonly BitThrottler _throttler = new();
    private ChangeEventArgs _lastThrottleEventArgs = default!;
    private ValidationMessageStore? _parsingValidationMessages;
    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

    protected event EventHandler OnValueChanged = default!;



    protected BitInputBase()
    {
        _validationStateChangedHandler = OnValidateStateChanged;
    }



    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }



    /// <summary>
    /// The debounce time in milliseconds.
    /// </summary>
    [Parameter] public int DebounceTime { get; set; }

    /// <summary>
    /// Gets or sets the display name for this field.
    /// This value is used when generating error messages when the input value fails to parse correctly.
    /// </summary>
    [Parameter] public string? DisplayName { get; set; }

    /// <summary>
    /// Change the content of the input field when the user write text (based on 'oninput' HTML event).
    /// </summary>
    [Parameter] public bool Immediate { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, object>? InputHtmlAttributes { get; set; }

    /// <summary>
    /// Gets or sets the name of the element.
    /// Allows access by name from the associated form.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// Callback for when the input value changes.
    /// </summary>
    [Parameter] public EventCallback<TValue?> OnChange { get; set; }

    /// <summary>
    /// The throttle time in milliseconds.
    /// </summary>
    [Parameter] public int ThrottleTime { get; set; }

    /// <summary>
    /// Gets or sets the value of the input. This should be used with two-way binding.
    /// </summary>
    /// <example>
    /// @bind-Value="model.PropertyName"
    /// </example>
    [Parameter]
    public TValue? Value
    {
        get => value;
        set
        {
            if (EqualityComparer<TValue>.Default.Equals(value, Value)) return;

            this.value = value;

            OnValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets a callback that updates the bound value.
    /// </summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets an expression that identifies the bound value.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }



    /// <summary>
    /// The ElementReference of the input element.
    /// </summary>
    public ElementReference InputElement { get; internal set; }

    /// <summary>
    /// Gives focus to the input element.
    /// </summary>
    public ValueTask FocusAsync() => InputElement.FocusAsync();

    /// <summary>
    /// Gives focus to the input element.
    /// </summary>
    /// <param name="preventScroll">A Boolean value indicating whether or not the browser should scroll
    /// the document to bring the newly-focused element into view. A value of false for preventScroll (the default)
    /// means that the browser will scroll the element into view after focusing it.
    /// If preventScroll is set to true, no scrolling will occur.</param>
    public ValueTask FocusAsync(bool preventScroll) => InputElement.FocusAsync(preventScroll);



    public override Task SetParametersAsync(ParameterView parameters)
    {
        ValueHasBeenSet = false;

        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(CascadedEditContext):
                    CascadedEditContext = (EditContext?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(DebounceTime):
                    DebounceTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(DisplayName):
                    DisplayName = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Immediate):
                    Immediate = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(InputHtmlAttributes):
                    InputHtmlAttributes = (IReadOnlyDictionary<string, object>?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Name):
                    Name = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(OnChange):
                    OnChange = (EventCallback<TValue?>)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ThrottleTime):
                    ThrottleTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Value):
                    ValueHasBeenSet = true;
                    Value = (TValue?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ValueChanged):
                    ValueChanged = (EventCallback<TValue>)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ValueExpression):
                    ValueExpression = (Expression<Func<TValue>>?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
            }
        }

        if (_hasInitializedParameters is false)
        {
            // This is the first run
            // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInitialized to call base.OnInitialized()

            CreateFieldIdentifier();

            _hasInitializedParameters = true;
        }
        else if (CascadedEditContext != EditContext)
        {
            // Not the first run

            // We don't support changing EditContext because it's messy to be clearing up state and event
            // handlers for the previous one, and there's no strong use case. If a strong use case
            // emerges, we can consider changing this.
            throw new InvalidOperationException($"{GetType()} does not support changing the {nameof(EditContext)} dynamically.");
        }

        UpdateValidationAttributes();

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
    }

    protected override void OnInitialized()
    {
        ClassBuilder.Register(() => ValueInvalid is true ? "bit-inv" : string.Empty);

        base.OnInitialized();
    }



    protected bool? ValueInvalid
    {
        get => valueInvalid;
        private set
        {
            if (valueInvalid == value) return;

            valueInvalid = value;

            ClassBuilder.Reset();
        }
    }

    protected EditContext EditContext { get; set; } = default!;

    protected internal FieldIdentifier FieldIdentifier { get; set; }

    protected TValue? CurrentValue
    {
        get => Value;
        set
        {
            if (IsEnabled is false) return;

            if (EqualityComparer<TValue>.Default.Equals(value, Value)) return;

            _parsingFailed = false;

            _ = SetCurrentValueAsync(value);
        }
    }

    protected string? CurrentValueAsString
    {
        // BitInputBase-derived components can hold invalid states (e.g., an BitNumberField being blank even when bound
        // to an int value). So, if parsing fails, we keep the rejected string in the UI even though it doesn't
        // match what's on the .NET model. This avoids interfering with typing, but still notifies the EditContext
        // about the validation error message.
        get => _parsingFailed ? _incomingValueBeforeParsing : FormatValueAsString(CurrentValue);
        set => _ = SetCurrentValueAsStringAsync(value);
    }



    protected virtual void CreateFieldIdentifier()
    {
        CreateFieldIdentifier(ValueExpression, typeof(TValue));
    }

    protected void CreateFieldIdentifier<TField>(Expression<Func<TField>>? valueExpression, Type valueType)
    {
        if (valueExpression is not null)
        {
            FieldIdentifier = FieldIdentifier.Create(valueExpression);
        }
        else if (ValueChanged.HasDelegate)
        {
            FieldIdentifier = FieldIdentifier.Create(() => Value);
        }

        if (CascadedEditContext is not null)
        {
            EditContext = CascadedEditContext;
            EditContext.OnValidationStateChanged += _validationStateChangedHandler;
        }

        _isUnderlyingTypeNullable = Nullable.GetUnderlyingType(valueType) is not null || default(TValue) is null;
    }

    protected virtual string? FormatValueAsString(TValue? value) => value?.ToString();

    protected abstract bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage);

    /// <summary>
    /// Handler for the OnChange event.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        await SetCurrentValueAsStringAsync(e.Value?.ToString());
    }

    /// <summary>
    /// Handler for the OnInput event, with an optional delay to avoid to raise the <see cref="ValueChanged"/> event too often.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        if (Immediate is false) return;

        if (DebounceTime > 0)
        {
            await _debouncer.Do(DebounceTime, async () => await InvokeAsync(async () => await HandleOnStringValueChangeAsync(e)));
        }
        else if (ThrottleTime > 0)
        {
            _lastThrottleEventArgs = e;
            await _throttler.Do(ThrottleTime, async () => await InvokeAsync(async () => await HandleOnStringValueChangeAsync(_lastThrottleEventArgs)));
        }
        else
        {
            await HandleOnStringValueChangeAsync(e);
        }
    }

    protected async Task SetCurrentValueAsStringAsync(string? value, bool bypass = false)
    {
        if (bypass && IsEnabled is false) return;

        _incomingValueBeforeParsing = value;
        _parsingValidationMessages?.Clear();

        if (_isUnderlyingTypeNullable && value.HasNoValue())
        {
            // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
            // Then all subclasses get nullable support almost automatically (they just have to
            // not reject Nullable<T> based on the type itself).
            _parsingFailed = false;

            CurrentValue = default;
        }
        else if (TryParseValueFromString(value, out var parsedValue, out var parsingErrorMessage))
        {
            _parsingFailed = false;

            if (bypass)
            {
                Value = parsedValue;
            }
            else
            {
                await SetCurrentValueAsync(parsedValue);
            }
        }
        else
        {
            _parsingFailed = true;

            // EditContext may be null if the input is not a child component of EditForm.
            if (EditContext is not null)
            {
                _parsingValidationMessages ??= new ValidationMessageStore(EditContext);
                _parsingValidationMessages.Add(FieldIdentifier, parsingErrorMessage);

                // Since we're not writing to CurrentValue, we'll need to notify about modification from here
                EditContext.NotifyFieldChanged(FieldIdentifier);
            }
        }

        // We can skip the validation notification if we were previously valid and still are
        if (_parsingFailed || _previousParsingAttemptFailed)
        {
            EditContext?.NotifyValidationStateChanged();
            _previousParsingAttemptFailed = _parsingFailed;
        }
    }




    private async Task SetCurrentValueAsync(TValue? value)
    {
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        Value = value;

        await ValueChanged.InvokeAsync(value);

        EditContext?.NotifyFieldChanged(FieldIdentifier);

        await OnChange.InvokeAsync(value);
    }

    private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
    {
        UpdateValidationAttributes();

        StateHasChanged();
    }

    private void UpdateValidationAttributes()
    {
        if (EditContext is null) return;

        var hasAriaInvalidAttribute = InputHtmlAttributes is not null && InputHtmlAttributes.ContainsKey("aria-invalid");

        if (EditContext.GetValidationMessages(FieldIdentifier).Any())
        {
            if (hasAriaInvalidAttribute) return; // Do not overwrite the attribute value

            if (TryConvertingToDictionary(InputHtmlAttributes, out var inputHtmlAttributes))
            {
                InputHtmlAttributes = inputHtmlAttributes;
            }

            // To make the `Input` components accessible by default
            // we will automatically render the `aria-invalid` attribute when the validation fails
            // value must be "true" see https://www.w3.org/TR/wai-aria-1.1/#aria-invalid
            inputHtmlAttributes["aria-invalid"] = "true";

            ValueInvalid = true;
        }
        else
        {
            ValueInvalid = false;

            if (hasAriaInvalidAttribute is false) return;

            // No validation errors. Need to remove `aria-invalid` if it was rendered already

            if (InputHtmlAttributes!.Count == 1)
            {
                // Only aria-invalid argument is present which we don't need any more
                InputHtmlAttributes = null;
            }
            else
            {
                if (TryConvertingToDictionary(InputHtmlAttributes, out var inputHtmlAttributes))
                {
                    InputHtmlAttributes = inputHtmlAttributes;
                }

                inputHtmlAttributes.Remove("aria-invalid");
            }
        }
    }

    private static bool TryConvertingToDictionary(IReadOnlyDictionary<string, object>? source, out Dictionary<string, object> result)
    {
        var newDictionaryCreated = true;

        if (source is null)
        {
            result = [];
        }
        else if (source is Dictionary<string, object> currentDictionary)
        {
            result = currentDictionary;
            newDictionaryCreated = false;
        }
        else
        {
            result = [];
            foreach (var item in source)
            {
                result.Add(item.Key, item.Value);
            }
        }

        return newDictionaryCreated;
    }



    public void Dispose()
    {
        if (IsDisposed) return;

        // When initialization in the SetParametersAsync method fails, the EditContext property can remain equal to null
        if (EditContext is not null)
        {
            EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }

        IsDisposed = true;

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }
}
