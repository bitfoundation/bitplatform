using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public abstract class BitInputBase<TValue> : BitComponentBase, IDisposable
{
    protected bool IsDisposed;
    protected bool ValueHasBeenSet;

    private TValue? value;
    private bool? valueInvalid;

    private bool _throttlePause;
    private TValue? _currentValue;
    private bool _isUnderlyingTypeNullable;
    private bool _hasInitializedParameters;
    private bool _previousParsingAttemptFailed;
    private ValidationMessageStore? _parsingValidationMessages;
    private CancellationTokenSource _debounceCts = new();
    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

    protected event EventHandler OnValueChanged = default!;


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
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, object>? InputHtmlAttributes { get; set; }

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
            _currentValue = value;

            if (OnValueChanged is not null)
            {
                OnValueChanged(this, EventArgs.Empty);
            }
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

                case nameof(InputHtmlAttributes):
                    InputHtmlAttributes = (IReadOnlyDictionary<string, object>?)parameter.Value;
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

            RegisterFieldIdentifier();

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



    protected BitInputBase()
    {
        _validationStateChangedHandler = OnValidateStateChanged;
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
            valueInvalid = value;
            ClassBuilder.Reset();
        }
    }

    protected EditContext EditContext { get; set; } = default!;

    protected internal FieldIdentifier FieldIdentifier { get; set; }

    protected TValue? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (EqualityComparer<TValue>.Default.Equals(value, _currentValue)) return;

            _currentValue = value;

            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            if (DebounceTime > 0)
            {
                _debounceCts.Cancel();
                _debounceCts.Dispose();
                _debounceCts = new();

                Task.Run(async () =>
                {
                    await Task.Delay(DebounceTime, _debounceCts.Token);
                    await InvokeAsync(async () =>
                    {
                        if (_debounceCts.IsCancellationRequested) return;

                        SetValue(_currentValue);
                    });
                }, _debounceCts.Token);
            }
            else if (ThrottleTime > 0)
            {
                if (_throttlePause) return;

                _throttlePause = true;

                Task.Run(async () =>
                {
                    await Task.Delay(ThrottleTime);
                    await InvokeAsync(async () =>
                    {
                        SetValue(_currentValue);

                        _throttlePause = false;
                    });
                });
            }
            else
            {
                SetValue(_currentValue);
            }

            void SetValue(TValue? value)
            {
                Value = value;
                _ = ValueChanged.InvokeAsync(value);
                EditContext?.NotifyFieldChanged(FieldIdentifier);
                _ = OnChange.InvokeAsync(value);
            }
        }
    }

    protected string? CurrentValueAsString
    {
        get => FormatValueAsString(CurrentValue);
        set
        {
            _parsingValidationMessages?.Clear();

            bool parsingFailed;

            if (_isUnderlyingTypeNullable && value.HasNoValue())
            {
                // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
                // Then all subclasses get nullable support almost automatically (they just have to
                // not reject Nullable<T> based on the type itself).
                parsingFailed = false;
                CurrentValue = default;
            }
            else if (TryParseValueFromString(value, out var parsedValue, out var validationErrorMessage))
            {
                parsingFailed = false;
                CurrentValue = parsedValue!;
            }
            else
            {
                parsingFailed = true;

                // EditContext may be null if the input is not a child component of EditForm.
                if (EditContext is not null)
                {
                    _parsingValidationMessages ??= new ValidationMessageStore(EditContext);
                    _parsingValidationMessages.Add(FieldIdentifier, validationErrorMessage);

                    // Since we're not writing to CurrentValue, we'll need to notify about modification from here
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }

            // We can skip the validation notification if we were previously valid and still are
            if (parsingFailed || _previousParsingAttemptFailed)
            {
                EditContext?.NotifyValidationStateChanged();
                _previousParsingAttemptFailed = parsingFailed;
            }
        }
    }

    protected virtual string? FormatValueAsString(TValue? value) => value?.ToString();

    protected abstract bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage);

    protected virtual void RegisterFieldIdentifier()
    {
        RegisterFieldIdentifier(ValueExpression, typeof(TValue));
    }

    protected void RegisterFieldIdentifier<TField>(Expression<Func<TField>>? valueExpression, Type valueType)
    {
        if (CascadedEditContext is not null && valueExpression is not null)
        {
            FieldIdentifier = FieldIdentifier.Create(valueExpression);
            EditContext = CascadedEditContext;
            EditContext.OnValidationStateChanged += _validationStateChangedHandler;
        }

        if (Nullable.GetUnderlyingType(valueType) is not null)
        {
            _isUnderlyingTypeNullable = true;
            return;
        }

        _isUnderlyingTypeNullable = default(TValue) is null;
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

            if (ConvertToDictionary(InputHtmlAttributes, out var inputHtmlAttributes))
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

            if (hasAriaInvalidAttribute)
            {
                // No validation errors. Need to remove `aria-invalid` if it was rendered already

                if (InputHtmlAttributes!.Count == 1)
                {
                    // Only aria-invalid argument is present which we don't need any more
                    InputHtmlAttributes = null;
                }
                else
                {
                    if (ConvertToDictionary(InputHtmlAttributes, out var inputHtmlAttributes))
                    {
                        InputHtmlAttributes = inputHtmlAttributes;
                    }

                    inputHtmlAttributes.Remove("aria-invalid");
                }
            }
        }
    }

    private static bool ConvertToDictionary(IReadOnlyDictionary<string, object>? source, out Dictionary<string, object> result)
    {
        var newDictionaryCreated = true;
        if (source == null)
        {
            result = new Dictionary<string, object>();
        }
        else if (source is Dictionary<string, object> currentDictionary)
        {
            result = currentDictionary;
            newDictionaryCreated = false;
        }
        else
        {
            result = new Dictionary<string, object>();
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
