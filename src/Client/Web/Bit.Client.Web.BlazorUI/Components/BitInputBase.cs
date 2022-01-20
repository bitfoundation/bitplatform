using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Client.Web.BlazorUI
{
    public abstract class BitInputBase<TValue> : BitComponentBase, IDisposable
    {
        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
        private bool _previousParsingAttemptFailed;
        private ValidationMessageStore? _parsingValidationMessages;
        private Type? _nullableUnderlyingType;

        [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter]
        public TValue? Value { get; set; }

        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets an expression that identifies the bound value.
        /// </summary>
        [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

        /// <summary>
        /// Gets the associated .
        /// This property is uninitialized if the input does not have a parent <see cref="EditForm"/>.
        /// </summary>
        protected EditContext EditContext { get; set; } = default!;

        /// <summary>
        /// Gets the <see cref="FieldIdentifier"/> for the bound value.
        /// </summary>
        protected internal FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the current value of the input.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current value of the input, represented as a string.
        /// </summary>
        protected string? CurrentValueAsString
        {
            get => FormatValueAsString(CurrentValue);
            set
            {
                _parsingValidationMessages?.Clear();

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

        /// <summary>
        /// Constructs an instance of <see cref="InputBase{TValue}"/>.
        /// </summary>
        protected BitInputBase()
        {
            _validationStateChangedHandler = OnValidateStateChanged;
        }

        /// <summary>
        /// Formats the value as a string. Derived classes can override this to determine the formating used for <see cref="CurrentValueAsString"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value.</returns>
        protected virtual string? FormatValueAsString(TValue? value)
            => value?.ToString();

        /// <summary>
        /// Parses a string to create an instance of <typeparamref name="TValue"/>. Derived classes can override this to change how
        /// <see cref="CurrentValueAsString"/> interprets incoming values.
        /// </summary>
        /// <param name="value">The string value to be parsed.</param>
        /// <param name="result">An instance of <typeparamref name="TValue"/>.</param>
        /// <param name="validationErrorMessage">If the value could not be parsed, provides a validation error message.</param>
        /// <returns>True if the value could be parsed; otherwise false.</returns>
        protected abstract bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage);


        public override Task SetParametersAsync(ParameterView parameters)
        {
            var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;
            foreach (var parameter in parametersDictionary!)
            {
                switch (parameter.Key)
                {
                    case nameof(EditContext):
                        EditContext = (EditContext)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(CurrentValue):
                        CurrentValue = (TValue)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(FieldIdentifier):
                        FieldIdentifier = (FieldIdentifier)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(ValueExpression):
                        ValueExpression = (Expression<Func<TValue>>?)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(ValueChanged):
                        ValueChanged = (EventCallback<TValue>)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(CascadedEditContext):
                        CascadedEditContext = (EditContext)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(Value):
                        Value = (TValue)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;
                }
            }
            if (EditContext is not null || CascadedEditContext is not null)
            {
                if (ValueExpression is null)
                {
                    Console.WriteLine($"{GetType()} requires a value for the 'ValueExpression' " +
                        $"parameter. Normally this is provided automatically when using 'bind-Value'.");
                }
                else
                {
                    FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                }

                if (CascadedEditContext is not null)
                {
                    EditContext = CascadedEditContext;
                    EditContext.OnValidationStateChanged += _validationStateChangedHandler;
                }

                _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));
            }

            // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
            return base.SetParametersAsync(ParameterView.Empty);
        }

        private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
        {
            StateHasChanged();
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

            if (EditContext is not null)
            {
                EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            _disposed = true;
        }
    }
}
