using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Client.Web.BlazorUI
{
    public abstract class BitInputBase<TValue> : BitComponentBase, IDisposable
    {
        private Type? _nullableUnderlyingType;
        private bool _hasInitializedParameters;
        private bool _previousParsingAttemptFailed;
        private ValidationMessageStore? _parsingValidationMessagesStore;
        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

        [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the created element.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter] public TValue? Value { get; set; }

        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets an expression that identifies the bound value.
        /// </summary>
        [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

        /// <summary>
        /// Gets or sets the display name for this field.
        /// <para>This value is used when generating error messages when the input value fails to parse correctly.</para>
        /// </summary>
        [Parameter] public string? DisplayName { get; set; }

        [Parameter] public bool? AriaInvalid { get; set; }

        protected EditContext EditContext { get; set; } = default!;
        protected internal FieldIdentifier FieldIdentifier { get; set; }

        protected TValue? CurrentValue
        {
            get => Value;
            set
            {
                var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, Value);
                if (hasChanged)
                {
                    Value = value;
                    _ = ValueChanged.InvokeAsync(Value);
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        protected string? CurrentValueAsString
        {
            get => FormatValueAsString(CurrentValue);
            set
            {
                _parsingValidationMessagesStore?.Clear();

                bool parsingFailed;

                if (_nullableUnderlyingType is not null && value.HasNoValue())
                {
                    // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
                    // Then all subclasses get nullable support almost automatically (they just have to
                    // not reject Nullable<T> based on the type itself).
                    parsingFailed = false;
                    CurrentValue = default!;
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
                        _parsingValidationMessagesStore ??= new ValidationMessageStore(EditContext);
                        _parsingValidationMessagesStore.Add(FieldIdentifier, validationErrorMessage);

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

        protected BitInputBase()
        {
            _validationStateChangedHandler = OnValidateStateChanged;
        }

        protected virtual string? FormatValueAsString(TValue? value) => value?.ToString();

        protected abstract bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage);

        protected string CssClass => EditContext?.FieldCssClass(FieldIdentifier) ?? string.Empty;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;
            foreach (var parameter in parametersDictionary!)
            {
                switch (parameter.Key)
                {
                    case nameof(CascadedEditContext):
                        CascadedEditContext = (EditContext?)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(InputAttributes):
                        InputAttributes = (IReadOnlyDictionary<string, object>?)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(Value):
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

                    case nameof(DisplayName):
                        DisplayName = (string?)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(AriaInvalid):
                        AriaInvalid = (bool?)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;
                }
            }

            if (_hasInitializedParameters is false)
            {
                if (ValueExpression is null)
                {
                    throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' parameter. Normally this is provided automatically when using 'bind-Value'.");
                }

                FieldIdentifier = FieldIdentifier.Create(ValueExpression);

                if (CascadedEditContext is not null)
                {
                    EditContext = CascadedEditContext;
                    EditContext.OnValidationStateChanged += _validationStateChangedHandler;
                }

                _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));
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

            UpdateAriaInvalid();

            // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
            return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
        }

        private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
        {
            UpdateAriaInvalid();

            StateHasChanged();
        }

        private void UpdateAriaInvalid()
        {
            if (EditContext is null) return;

            if (EditContext.GetValidationMessages(FieldIdentifier).Any())
            {
                if (AriaInvalid.HasValue) return; // Do not overwrite the attribute value

                AriaInvalid = true;
            }
            else
            {
                AriaInvalid = false;
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            // When initialization in the SetParametersAsync method fails, the EditContext property can remain equal to null
            if (EditContext is not null)
            {
                EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            _disposed = true;
        }
    }
}
